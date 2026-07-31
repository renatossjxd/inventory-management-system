using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Authorization;

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

public sealed class AppUserTests
{
    [Theory]
    [InlineData("admin", UserRoles.Admin)]
    [InlineData("OPERATOR", UserRoles.Operator)]
    public void Constructor_NormalizesSupportedRole(string input, string expected)
    {
        var user = new AppUser("user@example.com", "User", "hash", "salt", input);
        Assert.Equal(expected, user.Role);
    }

    [Fact]
    public void Constructor_RejectsUnknownRole()
    {
        Assert.Throws<ArgumentException>(() =>
            new AppUser("user@example.com", "User", "hash", "salt", "SuperAdmin"));
    }
}
