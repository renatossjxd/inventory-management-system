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
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    IQueryable<Product> IInventoryDbContext.Products => Products;
    IQueryable<StockMovement> IInventoryDbContext.StockMovements => StockMovements;
    IQueryable<AppUser> IInventoryDbContext.Users => Users;
    IQueryable<Category> IInventoryDbContext.Categories => Categories;
    IQueryable<Supplier> IInventoryDbContext.Suppliers => Suppliers;
    IQueryable<PurchaseOrder> IInventoryDbContext.PurchaseOrders => PurchaseOrders;
    public void AddProduct(Product product) => Products.Add(product);
    public void AddStockMovement(StockMovement movement) => StockMovements.Add(movement);
    public void AddUser(AppUser user) => Users.Add(user);
    public void AddCategory(Category category) => Categories.Add(category);
    public void AddSupplier(Supplier supplier) => Suppliers.Add(supplier);
    public void RemoveCategory(Category category) => Categories.Remove(category);
    public void RemoveSupplier(Supplier supplier) => Suppliers.Remove(supplier);
    public void AddPurchaseOrder(PurchaseOrder purchaseOrder) => PurchaseOrders.Add(purchaseOrder);

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
            entity.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Supplier).WithMany(x => x.Products).HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Phone).HasMaxLength(40);
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Number).IsUnique();
            entity.Property(x => x.Number).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Ignore(x => x.Total);
            entity.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Items).WithOne(x => x.PurchaseOrder).HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.PurchaseOrderId, x.ProductId }).IsUnique();
            entity.Property(x => x.UnitCost).HasPrecision(18, 2);
            entity.Ignore(x => x.Subtotal);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
