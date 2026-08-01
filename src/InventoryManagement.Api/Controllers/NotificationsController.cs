using System.IdentityModel.Tokens.Jwt;
using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(IInventoryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LowStockNotificationResponse>>> GetAll(
        [FromQuery] bool unreadOnly = false, CancellationToken ct = default)
    {
        var query = db.LowStockNotifications.AsNoTracking();
        if (unreadOnly) query = query.Where(x => !x.IsRead);
        var notifications = await query.OrderByDescending(x => x.CreatedAtUtc).Take(30)
            .Select(x => new LowStockNotificationResponse(x.Id, x.ProductId, x.Product.Name, x.Product.Sku,
                x.CurrentStock, x.MinimumStock, x.IsRead, x.CreatedAtUtc, x.ReadAtUtc)).ToListAsync(ct);
        return Ok(notifications);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var notification = await db.LowStockNotifications.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (notification is null) return NotFound();
        var userId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
        notification.MarkAsRead(userId);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
