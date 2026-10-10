namespace DiaperScout.Application;

/// <summary>Public identity derived from canonical names; storage names are never rewritten.</summary>
public static class PublicProductIdentity
{
    public static string? MeaningfulVariantName(string? name, int variantCount = 1) => name?.Trim() is { Length: > 0 } value
        && !new[] { "default", "single version", "unnamed" }.Contains(value, StringComparer.OrdinalIgnoreCase)
        && !(variantCount == 1 && value.Equals("current", StringComparison.OrdinalIgnoreCase))
            ? value : null;

    public static string DisplayName(string? brand, string product, string? variant = null, int variantCount = 1)
    {
        var result = Append(brand?.Trim() ?? string.Empty, product.Trim());
        return Append(result, MeaningfulVariantName(variant, variantCount) ?? string.Empty);
    }

    // Remove overlap only at component boundaries, keeping meaningful repeated words within names.
    private static string Append(string prefix, string suffix)
    {
        var left = prefix.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var right = suffix.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var overlap = Math.Min(left.Length, right.Length); overlap > 0; overlap--)
            if (left.TakeLast(overlap).SequenceEqual(right.Take(overlap), StringComparer.OrdinalIgnoreCase))
                return string.Join(' ', left.Concat(right.Skip(overlap)));
        return string.Join(' ', left.Concat(right));
    }

    public static string ProductUrl(string slug, Guid? variantId) => variantId.HasValue
        ? $"/products/{Uri.EscapeDataString(slug)}?variantId={variantId.Value}"
        : $"/products/{Uri.EscapeDataString(slug)}";
}
