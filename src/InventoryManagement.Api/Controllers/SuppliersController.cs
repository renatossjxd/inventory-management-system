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
[Route("api/suppliers")]
public sealed class SuppliersController(IInventoryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupplierResponse>>> GetAll(CancellationToken ct) => Ok(await db.Suppliers
        .AsNoTracking().OrderBy(x => x.Name).Select(x => Map(x)).ToListAsync(ct));

    [HttpPost]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<ActionResult<SupplierResponse>> Create(SupplierRequest request, CancellationToken ct)
    {
        if (await db.Suppliers.AnyAsync(x => x.Name == request.Name.Trim(), ct)) return Conflict();
        var supplier = new Supplier(request.Name, request.Email, request.Phone);
        db.AddSupplier(supplier);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetAll), Map(supplier));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<ActionResult<SupplierResponse>> Update(Guid id, SupplierRequest request, CancellationToken ct)
    {
        var supplier = await db.Suppliers.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (supplier is null) return NotFound();
        if (await db.Suppliers.AnyAsync(x => x.Name == request.Name.Trim() && x.Id != id, ct)) return Conflict();
        supplier.Update(request.Name, request.Email, request.Phone);
        await db.SaveChangesAsync(ct);
        return Ok(Map(supplier));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var supplier = await db.Suppliers.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (supplier is null) return NotFound();
        if (await db.Products.AnyAsync(x => x.SupplierId == id, ct))
            return Conflict(new ProblemDetails { Title = "No se puede eliminar un proveedor en uso." });
        db.RemoveSupplier(supplier);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static SupplierResponse Map(Supplier x) => new(x.Id, x.Name, x.Email, x.Phone, x.CreatedAtUtc);
}
