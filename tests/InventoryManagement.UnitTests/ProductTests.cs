using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Authorization;
using InventoryManagement.Application.Models;

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

public sealed class PaginationTests
{
    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(-5, 250, 1, 100)]
    [InlineData(3, 40, 3, 40)]
    public void Normalize_EnforcesSafeLimits(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var result = Pagination.Normalize(page, pageSize);

        Assert.Equal(expectedPage, result.Page);
        Assert.Equal(expectedPageSize, result.PageSize);
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

public sealed class CatalogTests
{
    [Fact]
    public void Product_AssignsCategoryAndOptionalSupplier()
    {
        var product = new Product("SKU-CATALOG", "Monitor", 100, 1);
        var category = new Category("Monitores");
        var supplier = new Supplier("Proveedor Uno", "ventas@proveedor.cl", "+56 9 1234 5678");

        product.AssignClassification(category, supplier);

        Assert.Equal(category.Id, product.CategoryId);
        Assert.Equal(supplier.Id, product.SupplierId);
    }

    [Fact]
    public void Supplier_RejectsInvalidEmail()
    {
        Assert.Throws<ArgumentException>(() => new Supplier("Proveedor", "correo-invalido"));
    }
}

public sealed class PurchaseOrderTests
{
    [Fact]
    public void Receive_AddsStockAndCannotBeRepeated()
    {
        var supplier = new Supplier("Proveedor");
        var product = new Product("SKU-PO", "Mouse", 100, 1);
        product.AssignClassification(new Category("Periféricos"), supplier);
        var order = new PurchaseOrder(supplier, Guid.NewGuid(), [(product, 5, 60m)]);

        var movements = order.Receive(Guid.NewGuid());

        Assert.Equal(PurchaseOrderStatus.Received, order.Status);
        Assert.Equal(5, product.CurrentStock);
        Assert.Single(movements);
        Assert.Throws<InvalidOperationException>(() => order.Receive(Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_RejectsRepeatedProducts()
    {
        var supplier = new Supplier("Proveedor");
        var product = new Product("SKU-PO", "Mouse", 100, 1);
        Assert.Throws<ArgumentException>(() => new PurchaseOrder(supplier, Guid.NewGuid(),
            [(product, 1, 50m), (product, 2, 50m)]));
    }
}
