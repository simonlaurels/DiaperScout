using System.Globalization;
using System.Text;
using DiaperScout.Domain;

namespace DiaperScout.Infrastructure;

internal sealed record CatalogueSubmissionCsvRow(
    int LineNumber,
    string Manufacturer,
    string? Brand,
    string ProductName,
    ProductType? ProductType,
    PackagingType? PackagingType,
    string? ProductFamily,
    string? Description,
    ProductStatus? ProductStatus,
    string? OfficialWebsite,
    string? VariantName,
    BackingType? BackingType,
    FastenerType? FastenerType,
    int? FastenerCount,
    CatalogueVariantAppearance? Appearance,
    string? PrimaryColour,
    bool? WetnessIndicator,
    bool? StandingLeakGuards,
    WaistbandStyle? WaistbandStyle,
    FragranceType? Fragrance,
    bool? LatexFree,
    string? DesignedFor,
    string? ConstructionNotes,
    string? ManufacturerSize,
    int? WaistMinCm,
    int? WaistMaxCm,
    int? HipMinCm,
    int? HipMaxCm,
    string? FitMeasurementBasis,
    int? AbsorbencyMl,
    string? AbsorbencyBasisMethod,
    string? AbsorbencySource,
    int? LengthMm,
    int? WidthMm,
    int? WeightGrams,
    int? ManufacturerPackQuantity,
    string? Gtin,
    string? IdentitySourceUrl,
    string? Notes,
    CatalogueContentVisibility DescriptionVisibility);

internal static class CatalogueSubmissionCsvParser
{
    private static readonly string[] RequiredHeaders = ["Manufacturer", "ProductName"];

    public static IReadOnlyList<CatalogueSubmissionCsvRow> Parse(Stream stream, bool importedDescriptionsAreModeratorOnly)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var headerLine = reader.ReadLine() ?? throw new FormatException("The CSV file is empty.");
        var headers = ParseLine(headerLine).Select(NormaliseHeader).ToArray();
        if (headers.Length == 0)
            throw new FormatException("The CSV file does not contain a header row.");

        foreach (var required in RequiredHeaders)
            if (!headers.Contains(NormaliseHeader(required), StringComparer.OrdinalIgnoreCase))
                throw new FormatException($"The CSV file must contain a '{required}' column.");

        var rows = new List<CatalogueSubmissionCsvRow>();
        var lineNumber = 1;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = ParseLine(line);
            var fields = headers.Select((header, index) => new { header, value = index < values.Count ? values[index].Trim() : string.Empty })
                .ToDictionary(x => x.header, x => x.value, StringComparer.OrdinalIgnoreCase);

            var manufacturer = Required(fields, "Manufacturer", lineNumber);
            var productName = Required(fields, "ProductName", lineNumber);
            var description = Optional(fields, "Description");

            rows.Add(new CatalogueSubmissionCsvRow(
                lineNumber,
                manufacturer,
                Optional(fields, "Brand"),
                productName,
                ParseEnumAlias<ProductType>(fields, "ProductType", lineNumber, new Dictionary<string, ProductType>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Diaper"] = ProductType.Tape,
                    ["Tape"] = ProductType.Tape,
                    ["Pullup"] = ProductType.PullUp,
                    ["Pull-Up"] = ProductType.PullUp
                }),
                ParseEnum<PackagingType>(fields, "PackagingType", lineNumber),
                Optional(fields, "ProductFamily"),
                description,
                ParseEnum<ProductStatus>(fields, "ProductStatus", lineNumber),
                Optional(fields, "OfficialWebsite"),
                Optional(fields, "VariantName"),
                ParseEnum<BackingType>(fields, "BackingType", lineNumber),
                ParseEnum<FastenerType>(fields, "FastenerType", lineNumber),
                ParseInt(fields, "FastenerCount", lineNumber),
                ParseEnum<CatalogueVariantAppearance>(fields, "Appearance", lineNumber),
                Optional(fields, "PrimaryColour"),
                ParseBool(fields, "WetnessIndicator", lineNumber),
                ParseBool(fields, "StandingLeakGuards", lineNumber),
                ParseEnum<WaistbandStyle>(fields, "WaistbandStyle", lineNumber),
                ParseEnum<FragranceType>(fields, "Fragrance", lineNumber),
                ParseBool(fields, "LatexFree", lineNumber),
                Optional(fields, "DesignedFor"),
                Optional(fields, "ConstructionNotes"),
                Optional(fields, "ManufacturerSize"),
                ParseInt(fields, "WaistMinCm", lineNumber),
                ParseInt(fields, "WaistMaxCm", lineNumber),
                ParseInt(fields, "HipMinCm", lineNumber),
                ParseInt(fields, "HipMaxCm", lineNumber),
                Optional(fields, "FitMeasurementBasis"),
                ParseInt(fields, "AbsorbencyMl", lineNumber),
                Optional(fields, "AbsorbencyBasisMethod"),
                Optional(fields, "AbsorbencySource"),
                ParseInt(fields, "LengthMm", lineNumber),
                ParseInt(fields, "WidthMm", lineNumber),
                ParseInt(fields, "WeightGrams", lineNumber),
                ParseInt(fields, "ManufacturerPackQuantity", lineNumber),
                Optional(fields, "GTIN"),
                Optional(fields, "IdentitySourceUrl"),
                Optional(fields, "Notes"),
                ParseVisibility(fields, "DescriptionVisibility", lineNumber, importedDescriptionsAreModeratorOnly)));
        }

        return rows;
    }

    private static string Required(IReadOnlyDictionary<string, string> fields, string name, int lineNumber) =>
        fields.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new FormatException($"Line {lineNumber}: '{name}' is required.");

    private static string? Optional(IReadOnlyDictionary<string, string> fields, string name) =>
        fields.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static int? ParseInt(IReadOnlyDictionary<string, string> fields, string name, int lineNumber)
    {
        var value = Optional(fields, name);
        if (value is null) return null;
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)) return result;
        throw new FormatException($"Line {lineNumber}: '{name}' must be a whole number.");
    }

    private static bool? ParseBool(IReadOnlyDictionary<string, string> fields, string name, int lineNumber)
    {
        var value = Optional(fields, name);
        if (value is null) return null;
        if (value.Equals("yes", StringComparison.OrdinalIgnoreCase) || value.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
        if (value.Equals("no", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
        throw new FormatException($"Line {lineNumber}: '{name}' must be Yes/No or True/False.");
    }

    private static T? ParseEnum<T>(IReadOnlyDictionary<string, string> fields, string name, int lineNumber) where T : struct, Enum
    {
        var value = Optional(fields, name);
        if (value is null) return null;
        if (Enum.TryParse<T>(value, true, out var result) && Enum.IsDefined(result)) return result;
        throw new FormatException($"Line {lineNumber}: '{name}' contains an invalid {typeof(T).Name} value.");
    }

    private static T? ParseEnumAlias<T>(IReadOnlyDictionary<string, string> fields, string name, int lineNumber, IReadOnlyDictionary<string, T> aliases) where T : struct, Enum
    {
        var value = Optional(fields, name);
        if (value is null) return null;
        if (aliases.TryGetValue(value, out var alias)) return alias;
        return ParseEnum<T>(fields, name, lineNumber);
    }

    private static CatalogueContentVisibility ParseVisibility(IReadOnlyDictionary<string, string> fields, string name, int lineNumber, bool importedDescriptionsAreModeratorOnly)
    {
        var value = Optional(fields, name);
        if (value is null)
            return importedDescriptionsAreModeratorOnly && Optional(fields, "Description") is not null
                ? CatalogueContentVisibility.ModeratorOnly
                : CatalogueContentVisibility.Public;

        if (Enum.TryParse<CatalogueContentVisibility>(value, true, out var result) && Enum.IsDefined(result))
            return result;

        throw new FormatException($"Line {lineNumber}: '{name}' contains an invalid visibility value.");
    }

    private static string NormaliseHeader(string value) => value.Trim().Trim('\ufeff');

    private static List<string> ParseLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (c == ',' && !quoted)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        if (quoted)
            throw new FormatException("The CSV contains an unterminated quoted field.");

        result.Add(current.ToString());
        return result;
    }
}
