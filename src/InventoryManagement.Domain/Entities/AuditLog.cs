namespace InventoryManagement.Domain.Entities;

public sealed class AuditLog
{
    private AuditLog() { }

    public AuditLog(Guid? userId, string? userName, string httpMethod, string path, int statusCode,
        string? ipAddress, string? userAgent, long durationMilliseconds)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        UserName = string.IsNullOrWhiteSpace(userName) ? "Usuario anónimo" : userName.Trim();
        HttpMethod = httpMethod.Trim().ToUpperInvariant();
        Path = path.Trim();
        StatusCode = statusCode;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        DurationMilliseconds = durationMilliseconds;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string HttpMethod { get; private set; } = string.Empty;
    public string Path { get; private set; } = string.Empty;
    public int StatusCode { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public long DurationMilliseconds { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
