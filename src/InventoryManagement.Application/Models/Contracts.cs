namespace InventoryManagement.Application.Models;

public sealed record RegisterRequest(string Email, string DisplayName, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string AccessToken, string TokenType = "Bearer");
public sealed record CreateProductRequest(string Sku, string Name, decimal Price, int MinimumStock, string? Description);
public sealed record UpdateProductRequest(string Sku, string Name, decimal Price, int MinimumStock, string? Description);
public sealed record AdjustStockRequest(int Quantity, string Reason);
public sealed record ProductResponse(Guid Id, string Sku, string Name, string? Description, decimal Price,
    int CurrentStock, int MinimumStock, bool IsLowStock, string? ImageUrl, DateTime CreatedAtUtc);
