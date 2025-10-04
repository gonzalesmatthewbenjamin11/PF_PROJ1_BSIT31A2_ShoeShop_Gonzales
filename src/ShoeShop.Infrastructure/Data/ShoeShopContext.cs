using Microsoft.EntityFrameworkCore;
using ShoeShop.Infrastructure.Entities;

namespace ShoeShop.Infrastructure   
{
    public class ShoeShopContext : DbContext
    {
        public ShoeShopContext(DbContextOptions<ShoeShopContext> options)
            : base(options)
        {
        }

        public DbSet<Shoe> Shoes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal precision for Price
            modelBuilder.Entity<Shoe>()
                .Property(s => s.Price)
                .HasPrecision(18, 2);

            // Seed initial data
            modelBuilder.Entity<Shoe>().HasData(
                new Shoe { Id = 1, Name = "Air Max 90", Brand = "Nike", Price = 120.00m, Stock = 25 },
                new Shoe { Id = 2, Name = "Stan Smith", Brand = "Adidas", Price = 80.00m, Stock = 30 },
                new Shoe { Id = 3, Name = "Chuck Taylor All Star", Brand = "Converse", Price = 65.00m, Stock = 15 },
                new Shoe { Id = 4, Name = "Old Skool", Brand = "Vans", Price = 75.00m, Stock = 20 },
                new Shoe { Id = 5, Name = "Air Jordan 1", Brand = "Nike", Price = 170.00m, Stock = 12 }
            );
        }
    }
}
