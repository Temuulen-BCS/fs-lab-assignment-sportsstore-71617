using Microsoft.EntityFrameworkCore;

namespace SportsStore.Models
{
    public class StoreDbContext : DbContext
    {

        public StoreDbContext(DbContextOptions<StoreDbContext> options)
            : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<InventoryRecord> InventoryRecords => Set<InventoryRecord>();
        public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
        public DbSet<ShipmentRecord> ShipmentRecords => Set<ShipmentRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PaymentRecord>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);
        }
    }
}