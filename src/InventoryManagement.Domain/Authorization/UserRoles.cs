namespace InventoryManagement.Domain.Authorization;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Operator = "Operator";

    public static string Normalize(string? role)
    {
        if (string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase)) return Admin;
        if (string.Equals(role, Operator, StringComparison.OrdinalIgnoreCase)) return Operator;
        throw new ArgumentException("El rol debe ser Admin u Operator.", nameof(role));
    }
}
