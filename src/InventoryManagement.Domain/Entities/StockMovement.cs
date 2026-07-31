namespace InventoryManagement.Domain.Entities;

public sealed class StockMovement
{
    private StockMovement() { }

    internal StockMovement(Guid productId, int quantity, string reason, Guid performedByUserId)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("El motivo es obligatorio.", nameof(reason));
        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
        Reason = reason.Trim();
        PerformedByUserId = performedByUserId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public int Quantity { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid PerformedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
