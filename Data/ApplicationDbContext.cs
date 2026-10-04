using FurniCraft.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FurniCraft.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<CustomizationGroup> CustomizationGroups { get; set; }
        public DbSet<CustomizationOption> CustomizationOptions { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // تحديد دقة حقول الـ Decimal للأسعار لتجنب أخطاء التقريب
            builder.Entity<Product>().Property(p => p.BasePrice).HasColumnType("decimal(18,2)");
            builder.Entity<CustomizationOption>().Property(c => c.AdditionalPrice).HasColumnType("decimal(18,2)");
            builder.Entity<CartItem>().Property(c => c.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Entity<OrderItem>().Property(o => o.UnitPrice).HasColumnType("decimal(18,2)");

            // منع الحذف المتسلسل للطلبات القديمة عند حذف منتج
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // سجل حالات الطلب: فهرس لتسريع جلب سجل طلب معين
            builder.Entity<OrderStatusHistory>()
                .HasIndex(h => new { h.OrderId, h.ChangedAt });
        }
    }
}