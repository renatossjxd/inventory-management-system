using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Persistence;

namespace InventoryManagement.Api.Middleware;

public sealed class AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
{
    private static readonly HashSet<string> AuditedMethods = ["POST", "PUT", "PATCH", "DELETE"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api") || !AuditedMethods.Contains(context.Request.Method))
        {
            await next(context);
            return;
        }

        var timer = Stopwatch.StartNew();
        try { await next(context); }
        finally
        {
            timer.Stop();
            try
            {
                var userIdValue = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var userId = Guid.TryParse(userIdValue, out var parsedUserId) ? parsedUserId : (Guid?)null;
                var userName = context.User.Identity?.Name ?? context.User.FindFirst("email")?.Value;
                var userAgent = context.Request.Headers.UserAgent.ToString();
                if (userAgent.Length > 500) userAgent = userAgent[..500];

                await using var scope = context.RequestServices.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                db.AddAuditLog(new AuditLog(userId, userName, context.Request.Method,
                    context.Request.Path.Value ?? "/api", context.Response.StatusCode,
                    context.Connection.RemoteIpAddress?.ToString(), userAgent, timer.ElapsedMilliseconds));
                await db.SaveChangesAsync(context.RequestAborted.IsCancellationRequested
                    ? CancellationToken.None : context.RequestAborted);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "No fue posible registrar la operación auditada {Method} {Path}.",
                    context.Request.Method, context.Request.Path);
            }
        }
    }
}
