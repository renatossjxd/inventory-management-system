using InventoryManagement.Api.Validation;
using Microsoft.AspNetCore.Http;

namespace InventoryManagement.UnitTests;

public sealed class ProductImageValidatorTests
{
    [Fact]
    public async Task ValidateAsync_AcceptsPngWithValidSignature()
    {
        byte[] content = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
        var file = CreateFile(content, "image/png", "product.png");

        await ProductImageValidator.ValidateAsync(file, CancellationToken.None);
    }

    [Fact]
    public async Task ValidateAsync_RejectsContentThatDoesNotMatchDeclaredType()
    {
        var file = CreateFile("this is not an image"u8.ToArray(), "image/png", "fake.png");

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            ProductImageValidator.ValidateAsync(file, CancellationToken.None));

        Assert.Contains("no coincide", exception.Message);
    }

    private static FormFile CreateFile(byte[] content, string contentType, string fileName)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
