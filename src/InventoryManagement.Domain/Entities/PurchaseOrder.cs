namespace InventoryManagement.Domain.Entities;

public enum PurchaseOrderStatus
{
    Pending,
    Received,
    Cancelled
}

public sealed class PurchaseOrder
{
    private PurchaseOrder() { }

    public PurchaseOrder(Supplier supplier, Guid createdByUserId,
        IEnumerable<(Product Product, int Quantity, decimal UnitCost)> lines)
    {
        Id = Guid.NewGuid();
        Number = $"PO-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        Supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));
        SupplierId = supplier.Id;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = DateTime.UtcNow;
        Status = PurchaseOrderStatus.Pending;

        foreach (var line in lines)
            Items.Add(new PurchaseOrderItem(Id, line.Product, line.Quantity, line.UnitCost));
        if (Items.Count == 0) throw new ArgumentException("La orden debe contener al menos un producto.", nameof(lines));
        if (Items.Select(x => x.ProductId).Distinct().Count() != Items.Count)
            throw new ArgumentException("La orden no puede repetir productos.", nameof(lines));
    }

    public Guid Id { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public Guid SupplierId { get; private set; }
    public Supplier Supplier { get; private set; } = null!;
    public PurchaseOrderStatus Status { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? ReceivedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReceivedAtUtc { get; private set; }
    public ICollection<PurchaseOrderItem> Items { get; private set; } = new List<PurchaseOrderItem>();
    public decimal Total => Items.Sum(x => x.Subtotal);

    public IReadOnlyList<StockMovement> Receive(Guid receivedByUserId)
    {
        if (Status != PurchaseOrderStatus.Pending)
            throw new InvalidOperationException("Solo se pueden recibir órdenes pendientes.");
        var movements = Items.Select(item =>
            item.Product.AdjustStock(item.Quantity, $"Recepción de orden {Number}", receivedByUserId)).ToList();
        Status = PurchaseOrderStatus.Received;
        ReceivedByUserId = receivedByUserId;
        ReceivedAtUtc = DateTime.UtcNow;
        return movements;
    }

    public void Cancel()
    {
        if (Status != PurchaseOrderStatus.Pending)
            throw new InvalidOperationException("Solo se pueden cancelar órdenes pendientes.");
        Status = PurchaseOrderStatus.Cancelled;
    }
}

public sealed class PurchaseOrderItem
{
    private PurchaseOrderItem() { }

    internal PurchaseOrderItem(Guid purchaseOrderId, Product product, int quantity, decimal unitCost)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad debe ser positiva.");
        if (unitCost < 0) throw new ArgumentOutOfRangeException(nameof(unitCost), "El costo no puede ser negativo.");
        Id = Guid.NewGuid();
        PurchaseOrderId = purchaseOrderId;
        Product = product ?? throw new ArgumentNullException(nameof(product));
        ProductId = product.Id;
        Quantity = quantity;
        UnitCost = unitCost;
    }

    public Guid Id { get; private set; }
    public Guid PurchaseOrderId { get; private set; }
    public PurchaseOrder PurchaseOrder { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public int Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal Subtotal => Quantity * UnitCost;
}
