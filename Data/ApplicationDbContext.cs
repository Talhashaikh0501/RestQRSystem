using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestaurantQR.Models;

namespace RestaurantQR.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Restaurant> Restaurants
            => Set<Restaurant>();

        public DbSet<RestaurantTable> RestaurantTables
            => Set<RestaurantTable>();

        public DbSet<Category> Categories
            => Set<Category>();

        public DbSet<MenuItem> MenuItems
            => Set<MenuItem>();

        public DbSet<Order> Orders
            => Set<Order>();

        public DbSet<OrderItem> OrderItems
            => Set<OrderItem>();
        public DbSet<SubscriptionPlan> SubscriptionPlans
    => Set<SubscriptionPlan>();
        public DbSet<Subscription> Subscriptions
    => Set<Subscription>();

        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // =====================================================
            // TDA TABLE NAMES
            // =====================================================

            // Application tables
            builder.Entity<Restaurant>()
                .ToTable("TDA_Restaurants");

            builder.Entity<RestaurantTable>()
                .ToTable("TDA_RestaurantTables");

            builder.Entity<Category>()
                .ToTable("TDA_Categories");

            builder.Entity<MenuItem>()
                .ToTable("TDA_MenuItems");

            builder.Entity<Order>()
                .ToTable("TDA_Orders");

            builder.Entity<OrderItem>()
                .ToTable("TDA_OrderItems");

            builder.Entity<SubscriptionPlan>()
    .ToTable("TDA_SubscriptionPlans");

            builder.Entity<Subscription>()
                .ToTable("TDA_Subscriptions");


            // =====================================================
            // ASP.NET IDENTITY TABLE NAMES
            // =====================================================

            builder.Entity<ApplicationUser>()
                .ToTable("TDA_AspNetUsers");

            builder.Entity<IdentityRole>()
                .ToTable("TDA_AspNetRoles");

            builder.Entity<IdentityRoleClaim<string>>()
                .ToTable("TDA_AspNetRoleClaims");

            builder.Entity<IdentityUserClaim<string>>()
                .ToTable("TDA_AspNetUserClaims");

            builder.Entity<IdentityUserLogin<string>>()
                .ToTable("TDA_AspNetUserLogins");

            builder.Entity<IdentityUserRole<string>>()
                .ToTable("TDA_AspNetUserRoles");

            builder.Entity<IdentityUserToken<string>>()
                .ToTable("TDA_AspNetUserTokens");


            // =====================================================
            // ORDER RELATIONSHIPS
            // =====================================================

            // Order -> Restaurant
            // Do not cascade delete historical orders.
            builder.Entity<Order>()
                .HasOne(o => o.Restaurant)
                .WithMany()
                .HasForeignKey(o => o.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order -> RestaurantTable
            // Do not cascade delete orders when a table is deleted.
            builder.Entity<Order>()
                .HasOne(o => o.RestaurantTable)
                .WithMany()
                .HasForeignKey(o => o.RestaurantTableId)
                .OnDelete(DeleteBehavior.Restrict);

            // OrderItem -> MenuItem
            // Menu changes/deletion should not cascade-delete
            // historical order items.
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.MenuItem)
                .WithMany()
                .HasForeignKey(oi => oi.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order -> OrderItems may cascade safely.
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // =====================================================
            // SUBSCRIPTION RELATIONSHIPS
            // =====================================================

            // Subscription -> Restaurant
            // Do not cascade delete subscription history
            // when a restaurant is deleted.
            builder.Entity<Subscription>()
    .HasOne(s => s.Restaurant)
    .WithMany(r => r.Subscriptions)
    .HasForeignKey(s => s.RestaurantId)
    .OnDelete(DeleteBehavior.Restrict);

            // Subscription -> SubscriptionPlan
            // Do not cascade delete subscription records
            // when a subscription plan is deleted.
            builder.Entity<Subscription>()
                .HasOne(s => s.SubscriptionPlan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // SUBSCRIPTION MONEY CONFIGURATION
            // =====================================================

            builder.Entity<SubscriptionPlan>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Subscription>()
                .Property(s => s.Amount)
                .HasColumnType("decimal(18,2)");

            // =====================================================
            // DEFAULT SUBSCRIPTION PLANS
            // =====================================================

            builder.Entity<SubscriptionPlan>().HasData(
                new SubscriptionPlan
                {
                    Id = 1,
                    Name = "6 Months",
                    DurationDays = 180,
                    Price = 9999m,
                    IsActive = true,
                    IsCustom = false,
                    CreatedAt = new DateTime(2026, 1, 1)
                },
                new SubscriptionPlan
                {
                    Id = 2,
                    Name = "1 Year",
                    DurationDays = 365,
                    Price = 17999m,
                    IsActive = true,
                    IsCustom = false,
                    CreatedAt = new DateTime(2026, 1, 1)
                }
            );
        }
    }
}