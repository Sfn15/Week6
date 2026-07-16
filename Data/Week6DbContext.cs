using Microsoft.EntityFrameworkCore;
using Week6.Models;

namespace Week6.Data;

public class Week6DbContext : DbContext
{
    public Week6DbContext(DbContextOptions<Week6DbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
           e.HasIndex(c => c.Email).IsUnique();
           e.Property(c => c.RowVersion).IsRowVersion();
           e.HasQueryFilter(c => !c.IsDeleted); 
            e.Property(c => c.LastName).HasMaxLength(100);
            e.Property(c => c.FirstName).HasMaxLength(100);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasIndex(p => p.Sku).IsUnique();
            e.Property(p => p.Price).HasColumnType("decimal(10,2)");
            e.Property(p => p.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.Property(o => o.RowVersion).IsRowVersion();
            e.HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.Property(oi => oi.UnitPrice).HasColumnType("decimal(10,2)");
            e.HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId);
            e.HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId);
        });

                
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLog");
        });

        modelBuilder.Entity<ErrorLog>(e =>
        {
            e.ToTable("ErrorLog");
        });
    }
}