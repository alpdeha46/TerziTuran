using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Models;

namespace TerziTuran.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Measurement> Measurements => Set<Measurement>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<BagReceipt> BagReceipts => Set<BagReceipt>();
    public DbSet<AppNotification> AppNotifications => Set<AppNotification>();
    public DbSet<UserPushToken> UserPushTokens => Set<UserPushToken>();
    public DbSet<UserPasswordRequest> UserPasswordRequests => Set<UserPasswordRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(x => x.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasOne(x => x.Customer)
            .WithMany(x => x.Users)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<User>()
            .HasMany(x => x.Notifications)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(x => x.PushTokens)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(x => x.PasswordRequests)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Customer>()
            .HasMany(x => x.Measurements)
            .WithOne(x => x.Customer)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Customer>()
            .HasMany(x => x.Orders)
            .WithOne(x => x.Customer)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Customer>()
            .HasMany(x => x.Appointments)
            .WithOne(x => x.Customer)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasOne(x => x.CreatedByUser)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasMany(x => x.OrderItems)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasMany(x => x.Payments)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasMany(x => x.Appointments)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Order>()
            .HasMany(x => x.BagReceipts)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BagReceipt>()
            .HasIndex(x => x.ReceiptNumber)
            .IsUnique();

        modelBuilder.Entity<BagReceipt>()
            .HasIndex(x => x.PickupCode);

        modelBuilder.Entity<BagReceipt>()
            .HasIndex(x => x.BagNumber);

        modelBuilder.Entity<AppNotification>()
            .HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });

        modelBuilder.Entity<UserPushToken>()
            .HasIndex(x => x.Token)
            .IsUnique();

        modelBuilder.Entity<UserPushToken>()
            .HasIndex(x => new { x.UserId, x.IsActive, x.LastSeenAt });

        modelBuilder.Entity<UserPasswordRequest>()
            .HasIndex(x => new { x.UserId, x.RequestType, x.IsUsed, x.IsDispatched });
    }
}
