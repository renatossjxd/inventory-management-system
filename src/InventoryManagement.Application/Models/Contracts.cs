namespace InventoryManagement.Application.Models;

public sealed record RegisterRequest(string Email, string DisplayName, string Password, string? Role = null);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string AccessToken, string TokenType = "Bearer");
public sealed record UserResponse(Guid Id, string Email, string DisplayName, string Role, DateTime CreatedAtUtc);
public sealed record CreateProductRequest(string Sku, string Name, decimal Price, int MinimumStock,
    string? Description, Guid CategoryId, Guid? SupplierId);
public sealed record UpdateProductRequest(string Sku, string Name, decimal Price, int MinimumStock,
    string? Description, Guid CategoryId, Guid? SupplierId);
public sealed record AdjustStockRequest(int Quantity, string Reason);
public sealed record ProductResponse(Guid Id, string Sku, string Name, string? Description, decimal Price,
    int CurrentStock, int MinimumStock, bool IsLowStock, string? ImageUrl, DateTime CreatedAtUtc,
    Guid? CategoryId, string? CategoryName, Guid? SupplierId, string? SupplierName);
public sealed record CategoryRequest(string Name, string? Description);
public sealed record CategoryResponse(Guid Id, string Name, string? Description, DateTime CreatedAtUtc);
public sealed record SupplierRequest(string Name, string? Email, string? Phone);
public sealed record SupplierResponse(Guid Id, string Name, string? Email, string? Phone, DateTime CreatedAtUtc);
