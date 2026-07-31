using System.IdentityModel.Tokens.Jwt;
using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Models;
using InventoryManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Api.Validation;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController(IInventoryDbContext db, IFileStorage fileStorage) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll([FromQuery] bool? lowStock, CancellationToken ct)
    {
        var query = db.Products.AsNoTracking();
        if (lowStock is true) query = query.Where(x => x.CurrentStock <= x.MinimumStock);
        return Ok(await query.OrderBy(x => x.Name).Select(x => Map(x)).ToListAsync(ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Get(Guid id, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return product is null ? NotFound() : Ok(Map(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken ct)
    {
        if (await db.Products.AnyAsync(x => x.Sku == request.Sku.ToUpper(), ct))
            return Conflict(new ProblemDetails { Title = "El SKU ya existe." });
        var product = new Product(request.Sku, request.Name, request.Price, request.MinimumStock);
        product.Update(request.Sku, request.Name, request.Price, request.MinimumStock, request.Description);
        db.AddProduct(product);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, Map(product));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Update(Guid id, UpdateProductRequest request, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (product is null) return NotFound();
        if (await db.Products.AnyAsync(x => x.Sku == request.Sku.ToUpper() && x.Id != id, ct)) return Conflict();
        product.Update(request.Sku, request.Name, request.Price, request.MinimumStock, request.Description);
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
        x.CurrentStock, x.MinimumStock, x.IsLowStock, x.ImageUrl, x.CreatedAtUtc);
}
