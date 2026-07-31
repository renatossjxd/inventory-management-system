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
[Route("api/categories")]
public sealed class CategoriesController(IInventoryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll(CancellationToken ct) => Ok(await db.Categories
        .AsNoTracking().OrderBy(x => x.Name).Select(x => Map(x)).ToListAsync(ct));

    [HttpPost]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<ActionResult<CategoryResponse>> Create(CategoryRequest request, CancellationToken ct)
    {
        if (await db.Categories.AnyAsync(x => x.Name == request.Name.Trim(), ct)) return Conflict();
        var category = new Category(request.Name, request.Description);
        db.AddCategory(category);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetAll), Map(category));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<ActionResult<CategoryResponse>> Update(Guid id, CategoryRequest request, CancellationToken ct)
    {
        var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (category is null) return NotFound();
        if (await db.Categories.AnyAsync(x => x.Name == request.Name.Trim() && x.Id != id, ct)) return Conflict();
        category.Update(request.Name, request.Description);
        await db.SaveChangesAsync(ct);
        return Ok(Map(category));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (category is null) return NotFound();
        if (await db.Products.AnyAsync(x => x.CategoryId == id, ct))
            return Conflict(new ProblemDetails { Title = "No se puede eliminar una categoría en uso." });
        db.RemoveCategory(category);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static CategoryResponse Map(Category x) => new(x.Id, x.Name, x.Description, x.CreatedAtUtc);
}
