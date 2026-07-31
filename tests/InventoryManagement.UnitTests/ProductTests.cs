using InventoryManagement.Domain.Entities;

namespace InventoryManagement.UnitTests;

public sealed class ProductTests
{
    [Fact]
    public void AdjustStock_RejectsMovementThatWouldCreateNegativeStock()
    {
        var product = new Product("SKU-1", "Teclado", 29990, 2);
        var act = () => product.AdjustStock(-1, "Venta", Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void AdjustStock_UpdatesStockAndCreatesMovement()
    {
        var product = new Product("sku-1", "Teclado", 29990, 2);
        product.AdjustStock(5, "Compra inicial", Guid.NewGuid());
        Assert.Equal(5, product.CurrentStock);
        Assert.Single(product.Movements);
        Assert.False(product.IsLowStock);
    }
}
