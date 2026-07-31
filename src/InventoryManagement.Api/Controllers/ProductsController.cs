using System.IdentityModel.Tokens.Jwt;
using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Models;
using InventoryManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Api.Validation;
using InventoryManagement.Domain.Authorization;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController(IInventoryDbContext db, IFileStorage fileStorage) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? supplierId = null,
        [FromQuery] bool? lowStock = null,
        CancellationToken ct = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        IQueryable<Product> query = db.Products.AsNoTracking().Include(x => x.Category).Include(x => x.Supplier);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Sku.Contains(term) || x.Name.Contains(term) ||
                (x.Description != null && x.Description.Contains(term)));
        }
        if (categoryId is not null) query = query.Where(x => x.CategoryId == categoryId);
        if (supplierId is not null) query = query.Where(x => x.SupplierId == supplierId);
        if (lowStock is true) query = query.Where(x => x.CurrentStock <= x.MinimumStock);
        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(x => Map(x)).ToListAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Ok(new PagedResponse<ProductResponse>(items, page, pageSize, totalCount, totalPages));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Get(Guid id, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking().Include(x => x.Category).Include(x => x.Supplier)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        return product is null ? NotFound() : Ok(Map(product));
    }

    [HttpPost]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken ct)
    {
        if (await db.Products.AnyAsync(x => x.Sku == request.Sku.ToUpper(), ct))
            return Conflict(new ProblemDetails { Title = "El SKU ya existe." });
        var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == request.CategoryId, ct);
        if (category is null) return BadRequest(new ProblemDetails { Title = "La categoría no existe." });
        Supplier? supplier = null;
        if (request.SupplierId is not null)
        {
            supplier = await db.Suppliers.SingleOrDefaultAsync(x => x.Id == request.SupplierId, ct);
            if (supplier is null) return BadRequest(new ProblemDetails { Title = "El proveedor no existe." });
        }
        var product = new Product(request.Sku, request.Name, request.Price, request.MinimumStock);
        product.Update(request.Sku, request.Name, request.Price, request.MinimumStock, request.Description);
        product.AssignClassification(category, supplier);
        db.AddProduct(product);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, Map(product));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<ActionResult<ProductResponse>> Update(Guid id, UpdateProductRequest request, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (product is null) return NotFound();
        if (await db.Products.AnyAsync(x => x.Sku == request.Sku.ToUpper() && x.Id != id, ct)) return Conflict();
        var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == request.CategoryId, ct);
        if (category is null) return BadRequest(new ProblemDetails { Title = "La categoría no existe." });
        Supplier? supplier = null;
        if (request.SupplierId is not null)
        {
            supplier = await db.Suppliers.SingleOrDefaultAsync(x => x.Id == request.SupplierId, ct);
            if (supplier is null) return BadRequest(new ProblemDetails { Title = "El proveedor no existe." });
        }
        product.Update(request.Sku, request.Name, request.Price, request.MinimumStock, request.Description);
        product.AssignClassification(category, supplier);
        await db.SaveChangesAsync(ct);
        return Ok(Map(product));
    }

    [HttpPost("{id:guid}/stock-movements")]
    public async Task<ActionResult<ProductResponse>> AdjustStock(Guid id, AdjustStockRequest request, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (product is null) return NotFound();
        var userId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
        var movement = product.AdjustStock(request.Quantity, request.Reason, userId);
        db.AddStockMovement(movement);
        await db.SaveChangesAsync(ct);
        return Ok(Map(product));
    }

    [HttpGet("{id:guid}/stock-movements")]
    public async Task<ActionResult> GetMovements(Guid id, CancellationToken ct) => Ok(await db.StockMovements
        .AsNoTracking().Where(x => x.ProductId == id).OrderByDescending(x => x.CreatedAtUtc)
        .Select(x => new { x.Id, x.Quantity, x.Reason, x.PerformedByUserId, x.CreatedAtUtc }).ToListAsync(ct));

    [HttpPost("{id:guid}/image")]
    [Authorize(Roles = UserRoles.Admin)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ProductImageValidator.MaximumFileSize)]
    public async Task<ActionResult<ProductResponse>> UploadImage(Guid id, IFormFile file, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (product is null) return NotFound();
        await ProductImageValidator.ValidateAsync(file, ct);

        var previousImageUrl = product.ImageUrl;
        await using var content = file.OpenReadStream();
        var newImageUrl = await fileStorage.UploadImageAsync(content, file.FileName, file.ContentType, ct);
        try
        {
            product.SetImageUrl(newImageUrl);
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await fileStorage.DeleteAsync(newImageUrl, ct);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(previousImageUrl))
            await fileStorage.DeleteAsync(previousImageUrl, ct);
        return Ok(Map(product));
    }

    private static ProductResponse Map(Product x) => new(x.Id, x.Sku, x.Name, x.Description, x.Price,
        x.CurrentStock, x.MinimumStock, x.IsLowStock, x.ImageUrl, x.CreatedAtUtc,
        x.CategoryId, x.Category?.Name, x.SupplierId, x.Supplier?.Name);
}
