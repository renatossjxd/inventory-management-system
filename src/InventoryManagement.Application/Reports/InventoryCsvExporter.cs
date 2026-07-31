using System.Globalization;
using System.Text;

namespace InventoryManagement.Application.Reports;

public sealed record InventoryReportRow(string Sku, string Name, string? Category, string? Supplier,
    decimal Price, int CurrentStock, int MinimumStock, bool IsLowStock, decimal StockValue,
    DateTime CreatedAtUtc);

public static class InventoryCsvExporter
{
    public static byte[] Build(IReadOnlyCollection<InventoryReportRow> rows)
    {
        var csv = new StringBuilder();
        csv.AppendLine("SKU;Producto;Categoría;Proveedor;Precio;Stock actual;Stock mínimo;Estado;Valor inventario;Creado UTC");

        foreach (var row in rows)
        {
            var fields = new[]
            {
                Escape(row.Sku), Escape(row.Name), Escape(row.Category), Escape(row.Supplier),
                row.Price.ToString("0.00", CultureInfo.GetCultureInfo("es-CL")),
                row.CurrentStock.ToString(CultureInfo.InvariantCulture),
                row.MinimumStock.ToString(CultureInfo.InvariantCulture),
                row.IsLowStock ? "Stock bajo" : "Disponible",
                row.StockValue.ToString("0.00", CultureInfo.GetCultureInfo("es-CL")),
                row.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            };
            csv.AppendLine(string.Join(';', fields));
        }

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return [.. encoding.GetPreamble(), .. encoding.GetBytes(csv.ToString())];
    }

    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var safe = value;
        if (safe[0] is '=' or '+' or '-' or '@') safe = $"'{safe}";
        return safe.IndexOfAny([';', '"', '\r', '\n']) >= 0
            ? $"\"{safe.Replace("\"", "\"\"")}\""
            : safe;
    }
}
