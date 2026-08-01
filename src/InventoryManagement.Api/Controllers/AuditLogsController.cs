using InventoryManagement.Application.Abstractions;
using InventoryManagement.Application.Models;
using InventoryManagement.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Admin)]
[Route("api/audit-logs")]
public sealed class AuditLogsController(IInventoryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AuditLogResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = Pagination.DefaultPageSize,
        [FromQuery] string? method = null, [FromQuery] int? statusCode = null,
        CancellationToken ct = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = db.AuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(method)) query = query.Where(x => x.HttpMethod == method.ToUpper());
        if (statusCode is not null) query = query.Where(x => x.StatusCode == statusCode);
        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AuditLogResponse(x.Id, x.UserId, x.UserName, x.HttpMethod, x.Path,
                x.StatusCode, x.IpAddress, x.UserAgent, x.DurationMilliseconds, x.CreatedAtUtc)).ToListAsync(ct);
        return Ok(new PagedResponse<AuditLogResponse>(items, page, pageSize, totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize)));
    }
}
