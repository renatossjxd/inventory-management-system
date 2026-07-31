namespace InventoryManagement.Domain.Entities;

public sealed class Supplier
{
    private Supplier() { }

    public Supplier(string name, string? email = null, string? phone = null)
    {
        Id = Guid.NewGuid();
        Update(name, email, phone);
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public ICollection<Product> Products { get; private set; } = new List<Product>();

    public void Update(string name, string? email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre es obligatorio.", nameof(name));
        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
            throw new ArgumentException("El correo del proveedor no es válido.", nameof(email));
        Name = name.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    }
}
