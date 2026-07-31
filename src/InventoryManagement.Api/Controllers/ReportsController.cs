using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Reports;
using InventoryManagement.Domain.Authorization;
using InventoryManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/reports")]
public sealed class ReportsController(IInventoryDbContext db) : ControllerBase
{
    [HttpGet("inventory.csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportInventory(
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? supplierId = null,
        [FromQuery] bool? lowStock = null,
        CancellationToken ct = default)
    {
        IQueryable<Product> query = db.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Sku.Contains(term) || x.Name.Contains(term) ||
                (x.Description != null && x.Description.Contains(term)));
        }
        if (categoryId is not null) query = query.Where(x => x.CategoryId == categoryId);
        if (supplierId is not null) query = query.Where(x => x.SupplierId == supplierId);
        if (lowStock is true) query = query.Where(x => x.CurrentStock <= x.MinimumStock);

        var rows = await query.OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new InventoryReportRow(x.Sku, x.Name,
                x.Category != null ? x.Category.Name : null,
                x.Supplier != null ? x.Supplier.Name : null,
                x.Price, x.CurrentStock, x.MinimumStock, x.CurrentStock <= x.MinimumStock,
                x.Price * x.CurrentStock, x.CreatedAtUtc))
            .ToListAsync(ct);

        var fileName = $"inventario-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(InventoryCsvExporter.Build(rows), "text/csv; charset=utf-8", fileName);
    }
}
