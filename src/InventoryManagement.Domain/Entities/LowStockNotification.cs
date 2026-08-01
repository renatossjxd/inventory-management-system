namespace InventoryManagement.Domain.Entities;

public sealed class LowStockNotification
{
    private LowStockNotification() { }

    public LowStockNotification(Product product)
    {
        Product = product ?? throw new ArgumentNullException(nameof(product));
        Id = Guid.NewGuid();
        ProductId = product.Id;
        CurrentStock = product.CurrentStock;
        MinimumStock = product.MinimumStock;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public int CurrentStock { get; private set; }
    public int MinimumStock { get; private set; }
    public bool IsRead { get; private set; }
    public Guid? ReadByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }

    public void MarkAsRead(Guid userId)
    {
        if (IsRead) return;
        IsRead = true;
        ReadByUserId = userId;
        ReadAtUtc = DateTime.UtcNow;
    }
}
