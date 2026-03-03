using InventoryManagementSystem.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<UserConnection> UserConnections { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Global query filter for soft delete
            modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<Category>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<Supplier>().HasQueryFilter(s => !s.IsDeleted);

            // Configure relationships
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure ChatMessage
            modelBuilder.Entity<ChatMessage>()
                .HasIndex(c => c.ConversationId)
                .HasDatabaseName("IX_ChatMessages_ConversationId");

            modelBuilder.Entity<ChatMessage>()
                .HasIndex(c => c.Timestamp)
                .HasDatabaseName("IX_ChatMessages_Timestamp");

            // Configure UserConnection
            modelBuilder.Entity<UserConnection>()
                .HasIndex(u => u.UserId)
                .HasDatabaseName("IX_UserConnections_UserId");

            modelBuilder.Entity<UserConnection>()
                .HasIndex(u => u.ConnectionId)
                .IsUnique()
                .HasDatabaseName("IX_UserConnections_ConnectionId");

            // REMOVED ALL HasData calls - let's start WITHOUT any seed data
            // We'll let DbInitializer handle all seeding at runtime

            // Configure Cart
            modelBuilder.Entity<Cart>()
                .HasIndex(c => c.UserId)
                .HasDatabaseName("IX_Carts_UserId");

            modelBuilder.Entity<Cart>()
                .HasMany(c => c.CartItems)
                .WithOne(ci => ci.Cart)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure CartItem
            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => ci.CartId)
                .HasDatabaseName("IX_CartItems_CartId");

            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => ci.ProductId)
                .HasDatabaseName("IX_CartItems_ProductId");

            // Configure Order
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique()
                .HasDatabaseName("IX_Orders_OrderNumber");

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.UserId)
                .HasDatabaseName("IX_Orders_UserId");

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderDate)
                .HasDatabaseName("IX_Orders_OrderDate");

            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderItem configurations
            modelBuilder.Entity<OrderItem>()
                .HasIndex(oi => oi.OrderId)
                .HasDatabaseName("IX_OrderItems_OrderId");

            modelBuilder.Entity<OrderItem>()
                .HasIndex(oi => oi.ProductId)
                .HasDatabaseName("IX_OrderItems_ProductId");

        }
    }
}