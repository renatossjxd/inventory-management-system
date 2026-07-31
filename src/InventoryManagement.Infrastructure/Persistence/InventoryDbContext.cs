using InventoryManagement.Application.Abstractions;
using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : DbContext(options), IInventoryDbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<AppUser> Users => Set<AppUser>();
    IQueryable<Product> IInventoryDbContext.Products => Products;
    IQueryable<StockMovement> IInventoryDbContext.StockMovements => StockMovements;
    IQueryable<AppUser> IInventoryDbContext.Users => Users;
    public void AddProduct(Product product) => Products.Add(product);
    public void AddStockMovement(StockMovement movement) => StockMovements.Add(movement);
    public void AddUser(AppUser user) => Users.Add(user);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Sku).IsUnique();
            entity.Property(x => x.Sku).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.Ignore(x => x.IsLowStock);
            entity.HasMany(x => x.Movements).WithOne(x => x.Product).HasForeignKey(x => x.ProductId);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reason).HasMaxLength(300).IsRequired();
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(30).IsRequired();
        });
    }
}
