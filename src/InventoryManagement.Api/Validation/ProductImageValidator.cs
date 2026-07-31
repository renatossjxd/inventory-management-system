namespace InventoryManagement.Api.Validation;

public static class ProductImageValidator
{
    public const long MaximumFileSize = 5 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, byte[][]> Signatures =
        new Dictionary<string, byte[][]>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = [[0xFF, 0xD8, 0xFF]],
            ["image/png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
            ["image/webp"] = [[0x52, 0x49, 0x46, 0x46]]
        };

    public static async Task ValidateAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0) throw new ArgumentException("La imagen está vacía.");
        if (file.Length > MaximumFileSize) throw new ArgumentException("La imagen no puede superar 5 MB.");
        if (!Signatures.TryGetValue(file.ContentType, out var expected))
            throw new ArgumentException("Solo se permiten imágenes JPEG, PNG o WebP.");

        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var bytesRead = await stream.ReadAsync(header, cancellationToken);
        var valid = expected.Any(signature => bytesRead >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature));
        if (file.ContentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
            valid = valid && bytesRead >= 12 && header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        if (!valid) throw new ArgumentException("El contenido del archivo no coincide con un formato de imagen permitido.");
    }
}
