using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Models;
using InventoryManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController(IInventoryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get(CancellationToken ct)
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var products = db.Products.AsNoTracking();

        var productCount = await products.CountAsync(ct);
        var totalStockUnits = await products.SumAsync(x => (int?)x.CurrentStock, ct) ?? 0;
        var inventoryValue = await products.SumAsync(x => (decimal?)(x.CurrentStock * x.Price), ct) ?? 0;
        var lowStockCount = await products.CountAsync(x => x.CurrentStock <= x.MinimumStock, ct);
        var pendingOrders = await db.PurchaseOrders.AsNoTracking()
            .CountAsync(x => x.Status == PurchaseOrderStatus.Pending, ct);
        var receivedThisMonth = await db.PurchaseOrders.AsNoTracking()
            .CountAsync(x => x.Status == PurchaseOrderStatus.Received && x.ReceivedAtUtc >= monthStart, ct);

        var lowStockProducts = await products.Include(x => x.Category).Include(x => x.Supplier)
            .Where(x => x.CurrentStock <= x.MinimumStock)
            .OrderBy(x => x.CurrentStock).ThenBy(x => x.Name).Take(5)
            .Select(x => new ProductResponse(x.Id, x.Sku, x.Name, x.Description, x.Price,
                x.CurrentStock, x.MinimumStock, x.IsLowStock, x.ImageUrl, x.CreatedAtUtc,
                x.CategoryId, x.Category != null ? x.Category.Name : null,
                x.SupplierId, x.Supplier != null ? x.Supplier.Name : null))
            .ToListAsync(ct);

        var recentMovements = await db.StockMovements.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc).Take(5)
            .Select(x => new DashboardRecentMovementResponse(x.Id, x.ProductId, x.Product.Name,
                x.Quantity, x.Reason, x.CreatedAtUtc))
            .ToListAsync(ct);

        return Ok(new DashboardResponse(productCount, totalStockUnits, inventoryValue, lowStockCount,
            pendingOrders, receivedThisMonth, lowStockProducts, recentMovements));
    }
}
