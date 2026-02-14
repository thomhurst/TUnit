using CloudShop.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudShop.ApiService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasIndex(e => e.Category);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.HasMany(e => e.Items).WithOne().HasForeignKey(e => e.OrderId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Seed default users
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Email = "admin@cloudshop.test", PasswordHash = BCryptHash("Admin123!"), Role = "admin", Name = "Admin User" },
            new User { Id = 2, Email = "customer@cloudshop.test", PasswordHash = BCryptHash("Customer123!"), Role = "customer", Name = "Test Customer" }
        );

        // Seed some products
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Wireless Headphones", Category = "electronics", Price = 79.99m, Description = "Bluetooth over-ear headphones with noise cancellation", StockQuantity = 150 },
            new Product { Id = 2, Name = "USB-C Hub", Category = "electronics", Price = 49.99m, Description = "7-in-1 USB-C dock with HDMI, USB-A, and SD card reader", StockQuantity = 200 },
            new Product { Id = 3, Name = "Mechanical Keyboard", Category = "electronics", Price = 129.99m, Description = "RGB mechanical keyboard with Cherry MX switches", StockQuantity = 75 },
            new Product { Id = 4, Name = "Running Shoes", Category = "clothing", Price = 89.99m, Description = "Lightweight running shoes with cushioned sole", StockQuantity = 300 },
            new Product { Id = 5, Name = "Winter Jacket", Category = "clothing", Price = 149.99m, Description = "Waterproof insulated winter jacket", StockQuantity = 100 },
            new Product { Id = 6, Name = "Cotton T-Shirt", Category = "clothing", Price = 24.99m, Description = "Premium cotton crew neck t-shirt", StockQuantity = 500 },
            new Product { Id = 7, Name = "Clean Code", Category = "books", Price = 39.99m, Description = "A Handbook of Agile Software Craftsmanship by Robert C. Martin", StockQuantity = 50 },
            new Product { Id = 8, Name = "Design Patterns", Category = "books", Price = 44.99m, Description = "Elements of Reusable Object-Oriented Software", StockQuantity = 45 },
            new Product { Id = 9, Name = "The Pragmatic Programmer", Category = "books", Price = 49.99m, Description = "Your Journey To Mastery, 20th Anniversary Edition", StockQuantity = 60 }
        );
    }

    // Simple password hashing for demo purposes (NOT production-safe)
    private static string BCryptHash(string password)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "cloudshop-salt"));
        return Convert.ToBase64String(bytes);
    }
}
