namespace InventoryManagement.Domain.Entities;

using InventoryManagement.Domain.Authorization;

public sealed class AppUser
{
    private AppUser() { }

    public AppUser(string email, string displayName, string passwordHash, string passwordSalt,
        string role = UserRoles.Operator)
    {
        Id = Guid.NewGuid();
        Email = email.Trim().ToLowerInvariant();
        DisplayName = displayName.Trim();
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
        Role = UserRoles.Normalize(role);
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string PasswordSalt { get; private set; } = string.Empty;
    public string Role { get; private set; } = UserRoles.Operator;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }

    public void UpdateAccess(string role, bool isActive)
    {
        Role = UserRoles.Normalize(role);
        IsActive = isActive;
    }
}
