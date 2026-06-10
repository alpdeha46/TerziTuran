using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Models;

namespace TerziTuran.Web.Data;

public static class DbSeeder
{
    public static async Task EnsureProductionAdminAsync(
        AppDbContext context,
        IConfiguration configuration,
        IPasswordHasher<User> hasher)
    {
        if (await context.Users.AnyAsync(x => x.Role == UserRole.Admin))
        {
            return;
        }

        var password = configuration["BootstrapAdmin:Password"];
        if (!Services.PasswordPolicy.IsValid(password))
        {
            throw new InvalidOperationException(
                "Ilk yonetici icin BootstrapAdmin:Password ayarlanmalidir. " + Services.PasswordPolicy.ErrorMessage);
        }

        var admin = new User
        {
            FullName = configuration["BootstrapAdmin:FullName"]?.Trim() ?? "Sistem Yoneticisi",
            Username = configuration["BootstrapAdmin:Username"]?.Trim().ToLowerInvariant() ?? "admin",
            Email = configuration["BootstrapAdmin:Email"]?.Trim().ToLowerInvariant() ?? "admin@terzituran.local",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        admin.PasswordHash = hasher.HashPassword(admin, password!);
        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }

    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        var hasher = new PasswordHasher<User>();

        var customers = new List<Customer>
        {
            new() { FullName = "Ahmet Yilmaz", Phone = "05320000001", Email = "ahmet@example.com", Address = "Kadikoy / Istanbul", Notes = "Takim elbise dikimi", CreatedAt = DateTime.UtcNow.AddDays(-20) },
            new() { FullName = "Elif Kaya", Phone = "05320000002", Email = "elif@example.com", Address = "Besiktas / Istanbul", Notes = "Nisanlik prova", CreatedAt = DateTime.UtcNow.AddDays(-12) },
            new() { FullName = "Mehmet Demir", Phone = "05320000003", Email = "mehmet@example.com", Address = "Uskudar / Istanbul", Notes = "Pantolon daraltma", CreatedAt = DateTime.UtcNow.AddDays(-6) }
        };
        if (!await context.Customers.AnyAsync())
        {
            await context.Customers.AddRangeAsync(customers);
            await context.SaveChangesAsync();
        }
        else
        {
            customers = await context.Customers.OrderBy(x => x.Id).Take(3).ToListAsync();
        }

        var admin = await context.Users.FirstOrDefaultAsync(x => x.Username == "admin");
        if (admin is null)
        {
            admin = new User
            {
                FullName = "Sistem Yoneticisi",
                Username = "admin",
                Email = "admin@terzituran.local",
                Phone = "05550000001",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            admin.PasswordHash = hasher.HashPassword(admin, "Admin123*");
            await context.Users.AddAsync(admin);
        }

        var staff = await context.Users.FirstOrDefaultAsync(x => x.Username == "staff");
        if (staff is null)
        {
            staff = new User
            {
                FullName = "Atolye Personeli",
                Username = "staff",
                Email = "staff@terzituran.local",
                Phone = "05550000002",
                Role = UserRole.Staff,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            staff.PasswordHash = hasher.HashPassword(staff, "Staff123*");
            await context.Users.AddAsync(staff);
        }

        await context.SaveChangesAsync();

        var customerPortalUser = await context.Users.FirstOrDefaultAsync(x => x.Username == "ahmet");
        if (customerPortalUser is null)
        {
            customerPortalUser = new User
            {
                FullName = customers[0].FullName,
                Username = "ahmet",
                Email = customers[0].Email ?? "ahmet@terzituran.local",
                Phone = customers[0].Phone,
                Role = UserRole.Customer,
                CustomerId = customers[0].Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            customerPortalUser.PasswordHash = hasher.HashPassword(customerPortalUser, "Musteri123*");
            await context.Users.AddAsync(customerPortalUser);
            await context.SaveChangesAsync();
        }

        if (!await context.Measurements.AnyAsync())
        {
            var measurements = new List<Measurement>
            {
                new() { CustomerId = customers[0].Id, Chest = 102, Waist = 88, Hip = 98, Shoulder = 46, Sleeve = 64, Neck = 40, Height = 182, Weight = 84, Notes = "Klasik kesim", CreatedAt = DateTime.UtcNow.AddDays(-18) },
                new() { CustomerId = customers[1].Id, Chest = 92, Waist = 72, Hip = 96, Shoulder = 40, Sleeve = 59, Height = 170, Weight = 60, Notes = "Dar kalip", CreatedAt = DateTime.UtcNow.AddDays(-11) },
                new() { CustomerId = customers[2].Id, Chest = 108, Waist = 100, Hip = 104, Shoulder = 48, Sleeve = 63, Inseam = 78, Height = 176, Weight = 93, Notes = "Gundelik kullanim", CreatedAt = DateTime.UtcNow.AddDays(-5) }
            };
            await context.Measurements.AddRangeAsync(measurements);
            await context.SaveChangesAsync();
        }

        if (!await context.Orders.AnyAsync())
        {
            var orders = new List<Order>
            {
                new() { CustomerId = customers[0].Id, Title = "Lacivert Takim Elbise", Description = "Ceket ve pantolon", Category = "Takim Elbise", ServiceType = OrderServiceType.Sewing, Status = OrderStatus.Sewing, Priority = OrderPriority.High, Price = 18000, PaidAmount = 9000, DeliveryDate = DateTime.Today.AddDays(7), CreatedAt = DateTime.UtcNow.AddDays(-18), CreatedByUserId = admin!.Id },
                new() { CustomerId = customers[1].Id, Title = "Nisanlik Elbise", Description = "Iki prova planlandi", Category = "Abiye", ServiceType = OrderServiceType.Sewing, Status = OrderStatus.Fitting, Priority = OrderPriority.Urgent, Price = 14500, PaidAmount = 14500, DeliveryDate = DateTime.Today.AddDays(3), CreatedAt = DateTime.UtcNow.AddDays(-11), CreatedByUserId = staff!.Id },
                new() { CustomerId = customers[2].Id, Title = "Kumas Pantolon", Description = "Paca ve bel duzeltmesi", Category = "Pantolon", ServiceType = OrderServiceType.Repair, Status = OrderStatus.Pending, Priority = OrderPriority.Medium, Price = 2500, PaidAmount = 1000, DeliveryDate = DateTime.Today.AddDays(10), CreatedAt = DateTime.UtcNow.AddDays(-5), CreatedByUserId = staff.Id },
                new() { CustomerId = customers[0].Id, Title = "Kaban Fermuar Degisimi", Description = "Musteri portalindan iletildi", Category = "Mont / Kaban", ServiceType = OrderServiceType.Repair, Status = OrderStatus.Pending, Priority = OrderPriority.Medium, Price = 0, PaidAmount = 0, DeliveryDate = DateTime.Today.AddDays(5), CreatedAt = DateTime.UtcNow.AddDays(-1), CreatedByUserId = customerPortalUser.Id, IsCustomerRequest = true }
            };
            await context.Orders.AddRangeAsync(orders);
            await context.SaveChangesAsync();

            var items = new List<OrderItem>
            {
                new() { OrderId = orders[0].Id, ProductName = "Ceket", Quantity = 1, UnitPrice = 11000, TotalPrice = 11000 },
                new() { OrderId = orders[0].Id, ProductName = "Pantolon", Quantity = 1, UnitPrice = 7000, TotalPrice = 7000 },
                new() { OrderId = orders[1].Id, ProductName = "Nisanlik", Quantity = 1, UnitPrice = 14500, TotalPrice = 14500 },
                new() { OrderId = orders[2].Id, ProductName = "Pantolon tadilat", Quantity = 1, UnitPrice = 2500, TotalPrice = 2500 }
            };
            await context.OrderItems.AddRangeAsync(items);

            var payments = new List<Payment>
            {
                new() { OrderId = orders[0].Id, Amount = 9000, PaymentType = PaymentType.Transfer, PaymentDate = DateTime.UtcNow.AddDays(-17), Note = "Kapora" },
                new() { OrderId = orders[1].Id, Amount = 14500, PaymentType = PaymentType.Card, PaymentDate = DateTime.UtcNow.AddDays(-10), Note = "Tam odeme" },
                new() { OrderId = orders[2].Id, Amount = 1000, PaymentType = PaymentType.Cash, PaymentDate = DateTime.UtcNow.AddDays(-3), Note = "On odeme" }
            };
            await context.Payments.AddRangeAsync(payments);

            var appointments = new List<Appointment>
            {
                new() { CustomerId = customers[0].Id, OrderId = orders[0].Id, AppointmentDate = DateTime.Today.AddHours(15), Title = "Ara prova", Description = "Kol boyu kontrolu", Status = AppointmentStatus.Scheduled },
                new() { CustomerId = customers[1].Id, OrderId = orders[1].Id, AppointmentDate = DateTime.Today.AddDays(1).AddHours(11), Title = "Son prova", Description = "Bel ve etek duzeltmesi", Status = AppointmentStatus.Scheduled },
                new() { CustomerId = customers[2].Id, OrderId = orders[2].Id, AppointmentDate = DateTime.Today.AddDays(2).AddHours(14), Title = "Olcu teyidi", Description = "Bel ve paca kontrolu", Status = AppointmentStatus.Scheduled }
            };
            await context.Appointments.AddRangeAsync(appointments);
            await context.SaveChangesAsync();
        }

        var existingOrders = await context.Orders.Where(x => (int)x.ServiceType == 0 || x.CreatedByUserId == customerPortalUser.Id).ToListAsync();
        foreach (var order in existingOrders)
        {
            if ((int)order.ServiceType == 0)
            {
                order.ServiceType = order.Description?.Contains("duzelt", StringComparison.OrdinalIgnoreCase) == true ||
                                    order.Description?.Contains("fermuar", StringComparison.OrdinalIgnoreCase) == true
                    ? OrderServiceType.Repair
                    : OrderServiceType.Sewing;
            }

            if (order.CreatedByUserId == customerPortalUser.Id)
            {
                order.IsCustomerRequest = true;
            }
        }

        if (!await context.Orders.AnyAsync(x => x.Title == "Klasik Yelek Dikimi"))
        {
            var historicalOrders = new List<Order>
            {
                new() { CustomerId = customers[0].Id, Title = "Klasik Yelek Dikimi", Description = "Kruvaze yelek", Category = "Yelek", ServiceType = OrderServiceType.Sewing, Status = OrderStatus.Delivered, Priority = OrderPriority.Medium, Price = 6200, PaidAmount = 6200, DeliveryDate = DateTime.Today.AddMonths(-5).AddDays(12), CreatedAt = DateTime.UtcNow.AddMonths(-5), CreatedByUserId = admin!.Id },
                new() { CustomerId = customers[1].Id, Title = "Abiye Boy Ayari", Description = "Etek boyu ve bel ayari", Category = "Abiye", ServiceType = OrderServiceType.Repair, Status = OrderStatus.Delivered, Priority = OrderPriority.High, Price = 2800, PaidAmount = 2800, DeliveryDate = DateTime.Today.AddMonths(-4).AddDays(8), CreatedAt = DateTime.UtcNow.AddMonths(-4), CreatedByUserId = staff!.Id },
                new() { CustomerId = customers[2].Id, Title = "Yun Palto Dikimi", Description = "Kis sezonu palto", Category = "Palto", ServiceType = OrderServiceType.Sewing, Status = OrderStatus.Delivered, Priority = OrderPriority.Medium, Price = 21000, PaidAmount = 21000, DeliveryDate = DateTime.Today.AddMonths(-3).AddDays(18), CreatedAt = DateTime.UtcNow.AddMonths(-3), CreatedByUserId = admin.Id },
                new() { CustomerId = customers[0].Id, Title = "Gomlek Kol Daraltma", Description = "Iki gomlek kol daraltma", Category = "Gomlek", ServiceType = OrderServiceType.Repair, Status = OrderStatus.Delivered, Priority = OrderPriority.Low, Price = 1800, PaidAmount = 1800, DeliveryDate = DateTime.Today.AddMonths(-2).AddDays(6), CreatedAt = DateTime.UtcNow.AddMonths(-2), CreatedByUserId = staff.Id },
                new() { CustomerId = customers[1].Id, Title = "Saten Elbise Prova", Description = "Ozel gun elbisesi", Category = "Elbise", ServiceType = OrderServiceType.Sewing, Status = OrderStatus.Ready, Priority = OrderPriority.High, Price = 12500, PaidAmount = 7500, DeliveryDate = DateTime.Today.AddDays(2), CreatedAt = DateTime.UtcNow.AddMonths(-1), CreatedByUserId = staff.Id }
            };
            await context.Orders.AddRangeAsync(historicalOrders);
        }

        if (!await context.BagReceipts.AnyAsync())
        {
            var firstOrder = await context.Orders.OrderBy(x => x.Id).FirstOrDefaultAsync();
            if (firstOrder is not null)
            {
                context.BagReceipts.Add(new BagReceipt
                {
                    OrderId = firstOrder.Id,
                    BagNumber = 1,
                    ReceiptNumber = $"TT-{DateTime.Today:yyyyMMdd}-001",
                    PickupCode = "4821",
                    BagCount = 1,
                    IssuedAt = DateTime.Now.AddDays(-1),
                    Note = "Takım elbise poşeti, prova kemeri içeride.",
                    IsDelivered = false
                });
            }
        }

        var receiptsWithoutBagNumber = await context.BagReceipts
            .Where(x => x.BagNumber <= 0)
            .OrderBy(x => x.IssuedAt)
            .ThenBy(x => x.Id)
            .ToListAsync();

        if (receiptsWithoutBagNumber.Count > 0)
        {
            var usedNumbers = await context.BagReceipts
                .Where(x => x.BagNumber > 0)
                .Select(x => x.BagNumber)
                .ToListAsync();

            var nextBagNumber = 1;
            foreach (var receipt in receiptsWithoutBagNumber)
            {
                while (usedNumbers.Contains(nextBagNumber))
                {
                    nextBagNumber++;
                }

                receipt.BagNumber = nextBagNumber;
                usedNumbers.Add(nextBagNumber);
                nextBagNumber++;
            }
        }

        await context.SaveChangesAsync();
    }
}
