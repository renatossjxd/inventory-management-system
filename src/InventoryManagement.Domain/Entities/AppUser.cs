namespace InventoryManagement.Domain.Entities;

public sealed class AppUser
{
    private AppUser() { }

    public AppUser(string email, string displayName, string passwordHash, string passwordSalt)
    {
        Id = Guid.NewGuid();
        Email = email.Trim().ToLowerInvariant();
        DisplayName = displayName.Trim();
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
        Role = "Admin";
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string PasswordSalt { get; private set; } = string.Empty;
    public string Role { get; private set; } = "Admin";
    public DateTime CreatedAtUtc { get; private set; }
}
