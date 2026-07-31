namespace InventoryManagement.Domain.Entities;

public sealed class Product
{
    private Product() { }

    public Product(string sku, string name, decimal price, int minimumStock = 0)
    {
        Id = Guid.NewGuid();
        Update(sku, name, price, minimumStock);
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int CurrentStock { get; private set; }
    public int MinimumStock { get; private set; }
    public string? ImageUrl { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public ICollection<StockMovement> Movements { get; private set; } = new List<StockMovement>();

    public bool IsLowStock => CurrentStock <= MinimumStock;

    public void Update(string sku, string name, decimal price, int minimumStock, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("El SKU es obligatorio.", nameof(sku));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("El nombre es obligatorio.", nameof(name));
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price), "El precio no puede ser negativo.");
        if (minimumStock < 0) throw new ArgumentOutOfRangeException(nameof(minimumStock), "El stock mínimo no puede ser negativo.");

        Sku = sku.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Price = price;
        MinimumStock = minimumStock;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public StockMovement AdjustStock(int quantity, string reason, Guid performedByUserId)
    {
        if (quantity == 0) throw new ArgumentException("La cantidad no puede ser cero.", nameof(quantity));
        if (CurrentStock + quantity < 0) throw new InvalidOperationException("El movimiento dejaría el stock en negativo.");

        CurrentStock += quantity;
        UpdatedAtUtc = DateTime.UtcNow;
        var movement = new StockMovement(Id, quantity, reason, performedByUserId);
        Movements.Add(movement);
        return movement;
    }

    public void SetImageUrl(string? imageUrl) => ImageUrl = imageUrl;
}
