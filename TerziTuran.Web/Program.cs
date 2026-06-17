using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TerziTuran.Web.Data;
using TerziTuran.Web.Middleware;
using TerziTuran.Web.Models;
using TerziTuran.Web.Services;

var startupLogPath = Path.Combine(AppContext.BaseDirectory, "App_Data", "startup-error.log");

try
{
    var builder = WebApplication.CreateBuilder(args);
    var appDataPath = EnsureAppDataDirectory(builder.Environment.ContentRootPath);
    EnsureRuntimeSecrets(builder.Configuration, appDataPath);

    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    {
        throw new InvalidOperationException("Jwt:Key en az 32 karakter olmali ve guvenli ortam ayarlarindan saglanmalidir.");
    }

    var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection ayarlanmalidir.");

    EnsureSqliteDirectory(defaultConnection, builder.Environment.ContentRootPath);

    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
    builder.Services.Configure<FirebasePushOptions>(builder.Configuration.GetSection("Firebase"));
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(defaultConnection));

    builder.Services.AddHttpClient();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IJwtService, JwtService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<IPdfService, PdfService>();
    builder.Services.AddScoped<IBagReceiptService, BagReceiptService>();
    builder.Services.AddScoped<IPushNotificationSender, FirebasePushService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.Name = "TerziTuranAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
        };
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("login", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 8,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        options.AddPolicy("register", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 4,
                    Window = TimeSpan.FromMinutes(10),
                    QueueLimit = 0
                }));
    });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole(UserRole.Admin.ToString()));
        options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole(UserRole.Admin.ToString(), UserRole.Staff.ToString()));
        options.AddPolicy("CustomerOnly", policy => policy.RequireRole(UserRole.Customer.ToString()));
    });

    var configuredCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("MobileAndWebClient", policy =>
        {
            policy.SetIsOriginAllowed(origin =>
                configuredCorsOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase) ||
                (builder.Environment.IsDevelopment() &&
                 Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                 (uri.IsLoopback || uri.Host == "10.0.2.2")))
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    builder.Services.AddControllersWithViews()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "TerziTuran API", Version = "v1" });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT token giriniz."
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync("UPDATE Orders SET BagCount = 1 WHERE BagCount < 1;");
        if (app.Environment.IsDevelopment())
        {
            await DbSeeder.SeedAsync(db);
        }
        else
        {
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            await DbSeeder.EnsureProductionAdminAsync(db, builder.Configuration, hasher);
        }
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseCors("MobileAndWebClient");
    app.Use(async (context, next) =>
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
        await next();
    });
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Dashboard}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    WriteStartupFailure(startupLogPath, ex);
    throw;
}

static void EnsureSqliteDirectory(string connectionString, string contentRootPath)
{
    const string prefix = "Data Source=";
    var segment = connectionString
        .Split(';', StringSplitOptions.RemoveEmptyEntries)
        .FirstOrDefault(x => x.TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    if (string.IsNullOrWhiteSpace(segment))
    {
        return;
    }

    var rawPath = segment[prefix.Length..].Trim();
    if (string.IsNullOrWhiteSpace(rawPath) || rawPath == ":memory:")
    {
        return;
    }

    var fullPath = Path.IsPathRooted(rawPath)
        ? rawPath
        : Path.Combine(contentRootPath, rawPath);

    var directory = Path.GetDirectoryName(fullPath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }
}

static string EnsureAppDataDirectory(string contentRootPath)
{
    var path = Path.Combine(contentRootPath, "App_Data");
    Directory.CreateDirectory(path);
    return path;
}

static void EnsureRuntimeSecrets(ConfigurationManager configuration, string appDataPath)
{
    if (string.IsNullOrWhiteSpace(configuration["Jwt:Key"]))
    {
        var jwtFile = Path.Combine(appDataPath, "jwt.key");
        var jwtKey = File.Exists(jwtFile)
            ? File.ReadAllText(jwtFile).Trim()
            : CreateAndPersistJwtKey(jwtFile);
        configuration["Jwt:Key"] = jwtKey;
    }

    if (string.IsNullOrWhiteSpace(configuration["BootstrapAdmin:Password"]))
    {
        var adminFile = Path.Combine(appDataPath, "bootstrap-admin.txt");
        var password = File.Exists(adminFile)
            ? ReadAdminPassword(adminFile)
            : CreateAndPersistAdminPassword(adminFile);

        if (!string.IsNullOrWhiteSpace(password))
        {
            configuration["BootstrapAdmin:Password"] = password;
        }
    }
}

static string CreateAndPersistJwtKey(string path)
{
    var bytes = RandomNumberGenerator.GetBytes(48);
    var key = Convert.ToBase64String(bytes);
    File.WriteAllText(path, key);
    return key;
}

static string? ReadAdminPassword(string path)
{
    foreach (var line in File.ReadAllLines(path))
    {
        if (line.StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
        {
            return line["Password=".Length..].Trim();
        }
    }

    return null;
}

static string CreateAndPersistAdminPassword(string path)
{
    const string password = "Admin123*Auto";
    File.WriteAllText(
        path,
        $"""
        Username=admin
        Password={password}
        Note=Bu sifre ilk acilis icin otomatik uretildi. Giris sonrasi mutlaka degistirin.
        """);
    return password;
}

static void WriteStartupFailure(string logPath, Exception ex)
{
    try
    {
        var directory = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.AppendAllText(
            logPath,
            $"""
            [{DateTime.UtcNow:O}]
            BaseDirectory: {AppContext.BaseDirectory}
            Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "(null)"}
            Jwt__Key set: {!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Jwt__Key"))}
            BootstrapAdmin__Password set: {!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BootstrapAdmin__Password"))}
            Exception:
            {ex}

            """);
    }
    catch
    {
        // Intentionally swallow logging failures so the original exception remains primary.
    }
}
