using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Abstractions;

public interface IInventoryDbContext
{
    IQueryable<Product> Products { get; }
    IQueryable<StockMovement> StockMovements { get; }
    IQueryable<AppUser> Users { get; }
    void AddProduct(Product product);
    void AddStockMovement(StockMovement movement);
    void AddUser(AppUser user);
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
