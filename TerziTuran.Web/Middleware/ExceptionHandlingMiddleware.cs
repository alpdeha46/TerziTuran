using System.Net;
using System.Text.Json;
using TerziTuran.Web.DTOs;

namespace TerziTuran.Web.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Beklenmeyen bir hata olustu.");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.ContentType = "application/json";
                var payload = JsonSerializer.Serialize(ApiResponse<object>.Fail("Beklenmeyen bir hata olustu."));
                await context.Response.WriteAsync(payload);
                return;
            }

            context.Response.Redirect("/Auth/AccessDenied");
        }
    }
}
