using System.Text.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiaperScout.CataloguePopulation;

public sealed record PopulationPack(
    string ResearchId, string Outcome, string Manufacturer, string Brand, string Product,
    string[] ManufacturerAliases, string[] ProductAliases, string? Family, ProductType ProductType,
    string Size, int Quantity, string Gtin, string MeasurementBasis,
    int? WaistMinimumCm, int? WaistMaximumCm, int? HipMinimumCm, int? HipMaximumCm,
    string OfficialWebsite, string[] IdentitySources, string[] IdentifierSources,
    string[] SpecificationSources, string EvidenceNotes, Guid? ExistingSubmissionId = null,
    bool ReconcileDraftAttributes = false,
    Dictionary<CatalogueVariantOverrideAttribute, string>? VerifiedVariantAttributes = null,
    int? LengthMm = null, int? WidthMm = null, PackagingType PackagingType = PackagingType.Bag,
    string VariantName = "Current");

public sealed record PopulationMapping(string ResearchId, Guid SubmissionId, Guid ProductId,
    Guid VariantId, Guid SizeId, Guid PackId, Guid IdentifierId, Guid AuditId, string Gtin, bool Created);

/// <summary>Explicitly invoked editorial publication; research files are never watched or auto-imported.</summary>
public sealed class PopulationPublisher(DiaperScoutDbContext db, ICatalogueSubmissions submissions,
    IEditorialAuthorisation authorisation, ICanonicalCatalogue canonical)
{
    public static void Validate(PopulationPack pack)
    {
        if (pack.Outcome != "READY" || pack.Quantity <= 0 || RetailGtin.Normalise(pack.Gtin) != pack.Gtin)
            throw new ArgumentException("Only READY exact packs with positive quantities and valid GTINs may be published.");
        if (pack.LengthMm is <= 0 || pack.WidthMm is <= 0)
            throw new ArgumentException("Verified dimensions must be positive; unavailable dimensions remain null.");
        if (!Enum.IsDefined(pack.PackagingType))
            throw new ArgumentException("The verified packaging type is invalid.");
        if (string.IsNullOrWhiteSpace(pack.VariantName) || pack.VariantName.Trim().Length > 200)
            throw new ArgumentException("A reviewed variant name of 1–200 characters is required.");
        foreach (var value in new[] { pack.ResearchId, pack.Manufacturer, pack.Brand, pack.Product,
                     pack.Size, pack.EvidenceNotes, pack.MeasurementBasis })
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Required exact-pack evidence is missing.");
        foreach (var references in new[] { pack.IdentitySources, pack.IdentifierSources, pack.SpecificationSources })
        {
            if (references is null || references.Length == 0) throw new ArgumentException("Identity, identifier and specification evidence are required.");
            foreach (var reference in references)
                if (!Uri.TryCreate(reference, UriKind.Absolute, out var url) || url.Scheme != "https" ||
                    url.Host.Equals("diapstash.com", StringComparison.OrdinalIgnoreCase) || url.Host.EndsWith(".diapstash.com", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Canonical evidence must use independent HTTPS sources.");
        }
        if (pack.ReconcileDraftAttributes && (!pack.ExistingSubmissionId.HasValue || pack.VerifiedVariantAttributes is null))
            throw new ArgumentException("Draft attribute reconciliation requires an explicit existing draft and reviewed attribute mapping.");
    }

    public async Task<PopulationMapping> PublishAsync(AuthenticatedUser actor, PopulationPack pack)
    {
        Validate(pack);
        if (!await authorisation.CanPublishAtlasAsync(actor)) throw new UnauthorizedAccessException();
        if ((await db.Database.GetPendingMigrationsAsync()).Any()) throw new InvalidOperationException("Migrations must be current.");
        await db.Database.OpenConnectionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(683492710246301)");
        try
        {
            var identifiers = await db.ProductIdentifiers.AsNoTracking()
                .Where(i => i.Type == IdentifierType.Gtin).ToListAsync();
            var matches = identifiers.Where(i => Key(i.Value) == Key(pack.Gtin)).ToList();
            if (matches.Count > 1) throw new InvalidOperationException("Equivalent GTINs already identify multiple canonical packs.");
            if (matches.Count == 1) return await VerifyMapping(pack, matches[0], false);

            var allSubmissions = await db.CatalogueSubmissions.AsNoTracking().ToListAsync();
            var allVariants = await db.CatalogueSubmissionVariants.AsNoTracking().ToListAsync();
            var allSizes = await db.CatalogueSubmissionSizeVariants.AsNoTracking().ToListAsync();
            var candidates = allSubmissions.Where(s => Same(s.ProposedBrandName, pack.Brand) &&
                Match(s.ProposedManufacturerName, pack.Manufacturer, pack.ManufacturerAliases) &&
                Match(s.ProposedProductName, pack.Product, pack.ProductAliases)).ToList();
            if (candidates.Count > 1) throw new InvalidOperationException("Multiple existing submissions need editorial reconciliation.");
            var existing = candidates.SingleOrDefault();
            if (pack.ExistingSubmissionId.HasValue && existing?.Id != pack.ExistingSubmissionId)
                throw new InvalidOperationException("The expected existing draft identity has changed.");
            foreach (var s in allSubmissions)
                if (Key(s.ProposedGtin) == Key(pack.Gtin) && s.Id != existing?.Id)
                    throw new InvalidOperationException("The GTIN is already held by another submission.");
            foreach (var size in allSizes.Where(s => Key(s.Gtin) == Key(pack.Gtin)))
                if (allVariants.Single(v => v.Id == size.VariantId).SubmissionId != existing?.Id)
                    throw new InvalidOperationException("The GTIN is already held by another submission size.");

            var canonicalProducts = await db.Products.AsNoTracking().ToListAsync();
            var manufacturers = await db.Manufacturers.AsNoTracking().ToListAsync();
            var brands = await db.Brands.AsNoTracking().ToListAsync();
            var existingProducts = canonicalProducts.Where(p => Match(p.Name, pack.Product, pack.ProductAliases) &&
                manufacturers.Any(m => m.Id == p.ManufacturerId && Match(m.Name, pack.Manufacturer, pack.ManufacturerAliases)) &&
                brands.Any(b => b.Id == p.BrandId && Same(b.Name, pack.Brand))).ToList();
            if (existingProducts.Count > 1)
                throw new InvalidOperationException("Multiple canonical products require editorial reconciliation.");
            if (existingProducts.SingleOrDefault() is { } existingProduct)
            {
                if (await db.ProductRouteRedirects.AnyAsync(r => r.SourceProductId == existingProduct.Id))
                    throw new InvalidOperationException("This is a retained historical product identity; use the reviewed canonical product and variant.");
                // Published submission proposals are historical evidence. After an audited manufacturer
                // correction, follow the canonical product link without aliasing distinct organisations.
                if (existing is null)
                {
                    var linked = allSubmissions.Where(s => s.PublishedProductId == existingProduct.Id).ToList();
                    if (linked.Count != 1)
                        throw new InvalidOperationException("A unique historical published submission link is required.");
                    existing = linked[0];
                }
                if (existing?.Status != CatalogueSubmissionStatus.Published || existing.PublishedProductId != existingProduct.Id ||
                    existingProduct.Status != ProductStatus.Current || existingProduct.ProductType != pack.ProductType ||
                    !Same(existingProduct.Family, pack.Family))
                    throw new InvalidOperationException("Existing product identity or submission requires editorial reconciliation.");
                var variants = await db.ProductVariants.Where(v => v.ProductId == existingProduct.Id &&
                    v.Name.ToLower() == pack.VariantName.Trim().ToLower()).ToListAsync();
                if (variants.Count > 1)
                    throw new InvalidOperationException("Multiple canonical variants match this reviewed identity.");
                var existingSize = variants.Count == 0 ? null : await db.SizeVariants.SingleOrDefaultAsync(s =>
                    s.ProductVariantId == variants[0].Id && s.ManufacturerSize.ToLower() == pack.Size.Trim().ToLower());
                if (existingSize is not null &&
                    (existingSize.WaistMinimumCm != pack.WaistMinimumCm || existingSize.WaistMaximumCm != pack.WaistMaximumCm ||
                     existingSize.HipMinimumCm != pack.HipMinimumCm || existingSize.HipMaximumCm != pack.HipMaximumCm ||
                     !Same(existingSize.FitMeasurementBasis, pack.MeasurementBasis) ||
                     existingSize.LengthMm != pack.LengthMm || existingSize.WidthMm != pack.WidthMm))
                    throw new InvalidOperationException("Existing size measurements differ; it will not be overwritten to add a pack.");
                if (existingSize is not null && await db.PackTypes.AnyAsync(p => p.SizeVariantId == existingSize.Id &&
                        p.QuantityPerPack == pack.Quantity && p.PackagingType == pack.PackagingType))
                    throw new InvalidOperationException("An equivalent pack already exists; GTIN/revision reconciliation is required.");
                if (existingSize is null && pack.PackagingType != PackagingType.Bag)
                    throw new InvalidOperationException("A verified published size is required before adding a carton or box.");
                await using var enrichment = await db.Database.BeginTransactionAsync();
                try
                {
                    if (variants.Count == 0)
                    {
                        if (Same(pack.VariantName, "Current") || pack.VerifiedVariantAttributes?.Count > 0)
                            throw new InvalidOperationException("A new manufacturer variant needs an explicit name; construction attributes require individual enrichment.");
                        await canonical.AddProductVariantAsync(actor, existingProduct.Id,
                            new(pack.VariantName, BackingType.Unknown, FastenerType.Unknown,
                                CatalogueVariantAppearance.Unknown, CatalogueVariantColour.Unknown, null, null,
                                WaistbandStyle.Unknown, FragranceType.Unknown, null, CatalogueVariantDesignedFor.Unknown,
                                null, null, pack.EvidenceNotes, pack.IdentitySources,
                                "Add independently reviewed manufacturer variant within the existing shared product.", Marker(pack) + ":variant"));
                        variants = await db.ProductVariants.Where(v => v.ProductId == existingProduct.Id && v.Name == pack.VariantName.Trim()).ToListAsync();
                    }
                    if (existingSize is null)
                        await canonical.AddProductSizeAsync(actor, existingProduct.Id, variants[0].Id,
                        new(pack.Size, pack.WaistMinimumCm, pack.WaistMaximumCm, pack.HipMinimumCm, pack.HipMaximumCm,
                            null, pack.MeasurementBasis, null, null, pack.LengthMm, pack.WidthMm, null, pack.Quantity, PackagingType.Bag,
                            pack.Gtin, pack.EvidenceNotes,
                            pack.IdentitySources.Concat(pack.IdentifierSources).Concat(pack.SpecificationSources).Distinct().ToArray(),
                            "Add independently verified sibling size without changing existing product, variant, sizes or images.", Marker(pack)));
                    else
                        await canonical.AddProductPackAsync(actor, existingProduct.Id, variants[0].Id, existingSize.Id,
                            new(pack.Quantity, pack.PackagingType, pack.Gtin, pack.EvidenceNotes,
                                pack.IdentitySources.Concat(pack.IdentifierSources).Concat(pack.SpecificationSources).Distinct().ToArray(),
                                "Add an independently verified sellable pack; preserve the shared size and every existing pack and identifier.", Marker(pack)));
                    var identifier = await db.ProductIdentifiers.SingleAsync(i => i.Type == IdentifierType.Gtin && i.Value == pack.Gtin);
                    var mapping = await VerifyMapping(pack, identifier, true);
                    await enrichment.CommitAsync();
                    return mapping;
                }
                catch { await enrichment.RollbackAsync(); db.ChangeTracker.Clear(); throw; }
            }

            if (pack.PackagingType != PackagingType.Bag)
                throw new InvalidOperationException("A verified published product and size are required before adding a carton or box.");
            Guid submissionId;
            if (existing?.Status == CatalogueSubmissionStatus.Approved &&
                existing.Notes?.Contains(Marker(pack), StringComparison.Ordinal) == true)
            {
                submissionId = existing.Id; // Resume a committed preparation after interrupted publication.
            }
            else
            {
                if (existing is not null && existing.Status != CatalogueSubmissionStatus.Draft)
                    throw new InvalidOperationException("Existing submission must be an editable Draft or this run's prepared Approved record.");
                await using var preparation = await db.Database.BeginTransactionAsync();
                try
                {
                    var notes = Marker(pack) + "\n" + pack.EvidenceNotes;
                    if (existing is null)
                    {
                        var receipt = await submissions.CreateAsync(actor, new(CatalogueSubmissionSource.Moderator,
                            pack.Manufacturer, pack.Brand, pack.Product, pack.VariantName, notes));
                        submissionId = receipt.Id;
                    }
                    else
                    {
                        submissionId = existing.Id;
                        // Preserve the prior entered proposal in the evidence journal before replacing unsupported fields.
                        notes += "\nPreserved prior draft proposal: " + JsonSerializer.Serialize(existing);
                        var editable = await db.CatalogueSubmissions.SingleAsync(s => s.Id == submissionId);
                        editable.UpdateProposal(pack.Manufacturer, pack.Product, pack.VariantName, pack.Brand, notes);
                        await db.SaveChangesAsync();
                    }
                    if (await db.CatalogueSubmissionImages.AnyAsync(i => i.SubmissionId == submissionId))
                        throw new InvalidOperationException("Existing images need separate permission review; this importer publishes placeholders only.");
                    var variants = await db.CatalogueSubmissionVariants.Where(v => v.SubmissionId == submissionId).ToListAsync();
                    if (variants.Count == 0)
                    {
                        await submissions.AddVariantAsync(actor, submissionId, new(pack.VariantName));
                        variants = await db.CatalogueSubmissionVariants.Where(v => v.SubmissionId == submissionId).ToListAsync();
                    }
                    if (variants.Count != 1 || !Same(variants[0].Name, pack.VariantName))
                        throw new InvalidOperationException("Existing variant structure needs separate editorial enrichment.");
                    var variant = variants[0];
                    var sizes = await db.CatalogueSubmissionSizeVariants.Where(s => s.VariantId == variant.Id).ToListAsync();
                    if (sizes.Count != 0) throw new InvalidOperationException("Existing sizes must be independently reconciled; they will not be overwritten.");
                    await submissions.AddSizeVariantAsync(actor, submissionId, variant.Id,
                        new(pack.Size, pack.WaistMinimumCm, pack.WaistMaximumCm, pack.HipMinimumCm, pack.HipMaximumCm,
                            null, pack.MeasurementBasis, null, null, pack.LengthMm, pack.WidthMm, null, pack.Quantity, pack.Gtin));
                    await submissions.UpdateIdentityAsync(actor, submissionId, new(null, null, pack.IdentitySources[0]));
                    await submissions.UpdateSpecificationsAsync(actor, submissionId,
                        new(pack.ProductType, PackagingType.Bag, pack.Family, null, CatalogueContentVisibility.ModeratorOnly,
                            ProductStatus.Current, pack.OfficialWebsite, null, null, null, null, null, null, null, null, null, null));
                    var priorAttributes = await db.CatalogueSubmissionVariantOverrides.SingleOrDefaultAsync(v => v.VariantId == variant.Id);
                    if (priorAttributes is not null)
                    {
                        if (!pack.ReconcileDraftAttributes)
                            throw new InvalidOperationException("Existing variant attributes require individual provenance review.");
                        // Keep all entered values in the editorial journal; unsupported claims must not become public facts.
                        notes += "\nPreserved prior variant attributes: " + JsonSerializer.Serialize(priorAttributes);
                        (await db.CatalogueSubmissions.SingleAsync(s => s.Id == submissionId))
                            .UpdateProposal(pack.Manufacturer, pack.Product, pack.VariantName, pack.Brand, notes);
                        await db.SaveChangesAsync();
                        foreach (var attribute in Enum.GetValues<CatalogueVariantOverrideAttribute>())
                            await submissions.UpdateVariantOverrideAsync(actor, submissionId, variant.Id, new(attribute, null));
                    }
                    if (pack.VerifiedVariantAttributes is not null)
                        foreach (var attribute in pack.VerifiedVariantAttributes)
                            await submissions.UpdateVariantOverrideAsync(actor, submissionId, variant.Id, new(attribute.Key, attribute.Value));
                    await submissions.ResolveEntitiesAsync(actor, submissionId,
                        await ResolveEntities(pack));
                    await submissions.BeginVerificationAsync(actor, submissionId);
                    foreach (var area in new[] { CatalogueVerificationArea.ProductIdentity, CatalogueVerificationArea.Specifications })
                    {
                        var urls = area == CatalogueVerificationArea.ProductIdentity
                            ? pack.IdentitySources.Concat(pack.IdentifierSources).Distinct().ToArray()
                            : pack.SpecificationSources;
                        await submissions.AddVerificationAsync(actor, submissionId,
                            new(area, CatalogueVerificationStatus.Verified,
                                $"{pack.ResearchId}: {pack.Product}; {pack.Size}; sealed bag {pack.Quantity}; GTIN {pack.Gtin}",
                                "Independent catalogue research", urls[0],
                                pack.EvidenceNotes + "\nSources: " + string.Join("\n", urls) +
                                "\nVariant identity: " + pack.VariantName + ". Images remain absent.", null));
                    }
                    await submissions.MarkReadyForReviewAsync(actor, submissionId);
                    await submissions.ReviewAsync(actor, submissionId, new(EditorialOutcome.Accepted, notes));
                    await preparation.CommitAsync();
                }
                catch { await preparation.RollbackAsync(); db.ChangeTracker.Clear(); throw; }
            }
            await submissions.PublishAsync(actor, submissionId);
            var publishedIdentifier = await db.ProductIdentifiers.AsNoTracking()
                .SingleAsync(i => i.Type == IdentifierType.Gtin && i.Value == pack.Gtin);
            return await VerifyMapping(pack, publishedIdentifier, true);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(683492710246301)");
            await db.Database.CloseConnectionAsync();
        }
    }

    private async Task<ResolveCatalogueSubmissionEntities> ResolveEntities(PopulationPack pack)
    {
        var manufacturers = await db.Manufacturers.ToListAsync();
        var matching = manufacturers.Where(m => Match(m.Name, pack.Manufacturer, pack.ManufacturerAliases)).ToList();
        if (matching.Count > 1) throw new InvalidOperationException("Manufacturer aliases resolve to multiple businesses.");
        var manufacturer = matching.SingleOrDefault();
        var brand = manufacturer is null ? null : await db.Brands.SingleOrDefaultAsync(b => b.ManufacturerId == manufacturer.Id && b.Name == pack.Brand);
        return new(manufacturer?.Id, manufacturer is null ? pack.Manufacturer : null,
            brand?.Id, brand is null ? pack.Brand : null);
    }

    private async Task<PopulationMapping> VerifyMapping(PopulationPack pack, ProductIdentifier identifier, bool created)
    {
        var unit = await db.PackTypes.SingleAsync(p => p.Id == identifier.PackTypeId);
        var size = await db.SizeVariants.SingleAsync(s => s.Id == unit.SizeVariantId);
        var variant = await db.ProductVariants.SingleAsync(v => v.Id == size.ProductVariantId);
        var product = await db.Products.SingleAsync(p => p.Id == variant.ProductId);
        var manufacturer = await db.Manufacturers.SingleAsync(m => m.Id == product.ManufacturerId);
        var brand = await db.Brands.SingleOrDefaultAsync(b => b.Id == product.BrandId);
        if (!Match(manufacturer.Name, pack.Manufacturer, pack.ManufacturerAliases) || !Same(brand?.Name, pack.Brand) ||
            !Match(product.Name, pack.Product, pack.ProductAliases) || !Same(size.ManufacturerSize, pack.Size) ||
            !Same(variant.Name, pack.VariantName) || size.WaistMinimumCm != pack.WaistMinimumCm ||
            size.WaistMaximumCm != pack.WaistMaximumCm || size.HipMinimumCm != pack.HipMinimumCm ||
            size.HipMaximumCm != pack.HipMaximumCm || !Same(size.FitMeasurementBasis, pack.MeasurementBasis) ||
            size.LengthMm != pack.LengthMm || size.WidthMm != pack.WidthMm ||
            product.ProductType != pack.ProductType || !Same(product.Family, pack.Family) ||
            unit.QuantityPerPack != pack.Quantity || unit.PackagingType != pack.PackagingType || product.Status != ProductStatus.Current)
            throw new InvalidOperationException("An existing GTIN does not match this exact canonical pack.");
        var audit = await db.CatalogueAuditRecords.SingleOrDefaultAsync(a => a.ProductId == product.Id && a.CorrelationId == Marker(pack));
        var originalProductId = product.Id;
        if (audit is null)
        {
            // A reparented original variant retains its original publication evidence and submission.
            var route = await db.ProductRouteRedirects.SingleOrDefaultAsync(r => r.TargetProductId == product.Id && r.DefaultVariantId == variant.Id);
            originalProductId = route?.SourceProductId ?? product.Id;
            audit = await db.CatalogueAuditRecords.SingleAsync(a => a.ProductId == originalProductId && a.Action == CatalogueAuditAction.ProductCreated);
        }
        var submission = await db.CatalogueSubmissions.SingleAsync(s => s.PublishedProductId == originalProductId);
        return new(pack.ResearchId, submission.Id, product.Id, variant.Id, size.Id, unit.Id, identifier.Id, audit.Id, identifier.Value, created);
    }

    private static string Marker(PopulationPack pack) => "[catalogue-research:" + pack.ResearchId + "]";
    private static string? Key(string? value) => value?.Replace(" ", "").Replace("-", "").PadLeft(14, '0');
    private static bool Same(string? left, string? right) => string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    private static bool Match(string actual, string expected, string[] aliases) => Same(actual, expected) || aliases.Any(a => Same(actual, a));
}
