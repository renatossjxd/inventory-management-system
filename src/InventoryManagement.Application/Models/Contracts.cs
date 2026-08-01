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
public sealed record CreatePurchaseOrderItemRequest(Guid ProductId, int Quantity, decimal UnitCost);
public sealed record CreatePurchaseOrderRequest(Guid SupplierId, IReadOnlyList<CreatePurchaseOrderItemRequest> Items);
public sealed record PurchaseOrderItemResponse(Guid ProductId, string Sku, string ProductName, int Quantity,
    decimal UnitCost, decimal Subtotal);
public sealed record PurchaseOrderResponse(Guid Id, string Number, Guid SupplierId, string SupplierName,
    string Status, decimal Total, Guid CreatedByUserId, Guid? ReceivedByUserId, DateTime CreatedAtUtc,
    DateTime? ReceivedAtUtc, IReadOnlyList<PurchaseOrderItemResponse> Items);
public sealed record LowStockNotificationResponse(Guid Id, Guid ProductId, string ProductName, string Sku,
    int CurrentStock, int MinimumStock, bool IsRead, DateTime CreatedAtUtc, DateTime? ReadAtUtc);
public sealed record AuditLogResponse(Guid Id, Guid? UserId, string UserName, string HttpMethod, string Path,
    int StatusCode, string? IpAddress, string? UserAgent, long DurationMilliseconds, DateTime CreatedAtUtc);
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount,
    int TotalPages);
public sealed record DashboardRecentMovementResponse(Guid Id, Guid ProductId, string ProductName,
    int Quantity, string Reason, DateTime CreatedAtUtc);
public sealed record DashboardResponse(int ProductCount, int TotalStockUnits, decimal InventoryValue,
    int LowStockCount, int PendingPurchaseOrders, int ReceivedPurchaseOrdersThisMonth,
    IReadOnlyList<ProductResponse> LowStockProducts,
    IReadOnlyList<DashboardRecentMovementResponse> RecentMovements);

public static class Pagination
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    public static (int Page, int PageSize) Normalize(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, MaximumPageSize));
}
