namespace DiaperScout.Application;

public sealed record RecentCatalogueProduct(Guid Id, string Name, string Slug, string? BrandName,
    string ManufacturerName, Guid ProductVariantId, string? ImageUrl, DateTimeOffset AddedAtUtc);
