using System.IdentityModel.Tokens.Jwt;
using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Models;
using InventoryManagement.Domain.Authorization;
using InventoryManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/purchase-orders")]
public sealed class PurchaseOrdersController(IInventoryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderResponse>>> GetAll(
        [FromQuery] PurchaseOrderStatus? status, CancellationToken ct)
    {
        IQueryable<PurchaseOrder> query = db.PurchaseOrders.AsNoTracking()
            .Include(x => x.Supplier).Include(x => x.Items).ThenInclude(x => x.Product);
        if (status is not null) query = query.Where(x => x.Status == status);
        return Ok((await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct)).Select(Map));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderResponse>> Get(Guid id, CancellationToken ct)
    {
        var order = await db.PurchaseOrders.AsNoTracking().Include(x => x.Supplier)
            .Include(x => x.Items).ThenInclude(x => x.Product).SingleOrDefaultAsync(x => x.Id == id, ct);
        return order is null ? NotFound() : Ok(Map(order));
    }

    [HttpPost]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<ActionResult<PurchaseOrderResponse>> Create(CreatePurchaseOrderRequest request, CancellationToken ct)
    {
        if (request.Items.Count == 0) return BadRequest(new ProblemDetails { Title = "La orden no tiene productos." });
        var supplier = await db.Suppliers.SingleOrDefaultAsync(x => x.Id == request.SupplierId, ct);
        if (supplier is null) return BadRequest(new ProblemDetails { Title = "El proveedor no existe." });

        var productIds = request.Items.Select(x => x.ProductId).Distinct().ToArray();
        if (productIds.Length != request.Items.Count)
            return BadRequest(new ProblemDetails { Title = "La orden contiene productos repetidos." });
        var products = await db.Products.Where(x => productIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        if (products.Count != productIds.Length)
            return BadRequest(new ProblemDetails { Title = "Uno o más productos no existen." });
        if (products.Values.Any(x => x.SupplierId is not null && x.SupplierId != supplier.Id))
            return BadRequest(new ProblemDetails { Title = "Un producto pertenece a otro proveedor." });

        var lines = request.Items.Select(item => (products[item.ProductId], item.Quantity, item.UnitCost));
        var order = new PurchaseOrder(supplier, CurrentUserId(), lines);
        db.AddPurchaseOrder(order);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, Map(order));
    }

    [HttpPost("{id:guid}/receive")]
    public async Task<ActionResult<PurchaseOrderResponse>> Receive(Guid id, CancellationToken ct)
    {
        var order = await db.PurchaseOrders.Include(x => x.Supplier)
            .Include(x => x.Items).ThenInclude(x => x.Product).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (order is null) return NotFound();
        foreach (var movement in order.Receive(CurrentUserId())) db.AddStockMovement(movement);
        await db.SaveChangesAsync(ct);
        return Ok(Map(order));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<ActionResult<PurchaseOrderResponse>> Cancel(Guid id, CancellationToken ct)
    {
        var order = await db.PurchaseOrders.Include(x => x.Supplier)
            .Include(x => x.Items).ThenInclude(x => x.Product).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (order is null) return NotFound();
        order.Cancel();
        await db.SaveChangesAsync(ct);
        return Ok(Map(order));
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);

    private static PurchaseOrderResponse Map(PurchaseOrder order) => new(order.Id, order.Number,
        order.SupplierId, order.Supplier.Name, order.Status.ToString(), order.Total, order.CreatedByUserId,
        order.ReceivedByUserId, order.CreatedAtUtc, order.ReceivedAtUtc, order.Items.Select(item =>
            new PurchaseOrderItemResponse(item.ProductId, item.Product.Sku, item.Product.Name,
                item.Quantity, item.UnitCost, item.Subtotal)).ToList());
}
