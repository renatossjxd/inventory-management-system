namespace InventoryManagement.Domain.Entities;

public sealed class Category
{
    private Category() { }

    public Category(string name, string? description = null)
    {
        Id = Guid.NewGuid();
        Update(name, description);
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public ICollection<Product> Products { get; private set; } = new List<Product>();

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre es obligatorio.", nameof(name));
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
