using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Abstractions;

public interface IInventoryDbContext
{
    IQueryable<Product> Products { get; }
    IQueryable<StockMovement> StockMovements { get; }
    IQueryable<AppUser> Users { get; }
    IQueryable<Category> Categories { get; }
    IQueryable<Supplier> Suppliers { get; }
    IQueryable<PurchaseOrder> PurchaseOrders { get; }
    IQueryable<LowStockNotification> LowStockNotifications { get; }
    void AddProduct(Product product);
    void AddStockMovement(StockMovement movement);
    void AddUser(AppUser user);
    void AddCategory(Category category);
    void AddSupplier(Supplier supplier);
    void RemoveCategory(Category category);
    void RemoveSupplier(Supplier supplier);
    void AddPurchaseOrder(PurchaseOrder purchaseOrder);
    void AddLowStockNotification(LowStockNotification notification);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IPasswordService
{
    (string Hash, string Salt) Hash(string password);
    bool Verify(string password, string hash, string salt);
}

public interface ITokenService
{
    string Create(AppUser user);
}

public interface IFileStorage
{
    Task<string> UploadImageAsync(Stream content, string fileName, string contentType,
        CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default);
}
