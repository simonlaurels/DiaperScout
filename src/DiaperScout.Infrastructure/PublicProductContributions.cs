using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json;

namespace DiaperScout.Infrastructure;

internal sealed class PublicProductContributions(DiaperScoutDbContext db, IEditorialAuthorisation editorial,
    IPlaceObservations places, ICatalogueSubmissionImageStorage storage) : IPublicProductContributions
{
    public async Task<PublicProposalIdentity?> IdentityAsync(AuthenticatedUser actor, Guid id, CancellationToken ct = default) =>
        await db.CatalogueSubmissions.AsNoTracking().Where(s => s.Id == id && s.SubmittedByUserId == actor.UserId && s.PublicContributionId != null)
            .Select(s => new PublicProposalIdentity(s.Id, s.ProposedProductName, s.ProposedBrandName, s.Status)).SingleOrDefaultAsync(ct);

    public async Task AttachDiscoveryAsync(AuthenticatedUser actor, Guid id, PendingPhysicalDiscovery r, CancellationToken ct = default)
    {
        r = r with { ObservedAtUtc = new DateTimeOffset(r.ObservedAtUtc.UtcTicks - r.ObservedAtUtc.UtcTicks % 10, TimeSpan.Zero), CurrencyCode = r.PriceAmount.HasValue ? r.CurrencyCode?.Trim().ToUpperInvariant() : null };
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({id.ToString()}, 0))", ct);
        var proposal = await db.CatalogueSubmissions.SingleOrDefaultAsync(s => s.Id == id && s.SubmittedByUserId == actor.UserId && s.PublicContributionId != null, ct) ?? throw new KeyNotFoundException();
        if (proposal.PendingLocationId.HasValue) {
            if (proposal.PendingLocationId != r.LocationId || proposal.PendingObservedAtUtc != r.ObservedAtUtc || proposal.PendingPriceAmount != r.PriceAmount || proposal.PendingCurrencyCode != r.CurrencyCode)
                throw Invalid("discovery", "This proposal already has different discovery evidence.");
            await transaction.CommitAsync(ct); return;
        }
        if (!await db.Locations.AnyAsync(l => l.Id == r.LocationId && l.IsPublicCommercialPlace && l.Latitude != null && l.Longitude != null, ct)) throw Invalid("locationId", "Choose a confirmed public shop.");
        if (r.ObservedAtUtc < new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero) || r.ObservedAtUtc > DateTimeOffset.UtcNow.AddMinutes(5)) throw Invalid("observedAtUtc", "Choose a valid discovery time.");
        if (r.PriceAmount.HasValue && (r.PriceAmount is < 0 or > 9999999999.99m || decimal.Round(r.PriceAmount.Value, 2) != r.PriceAmount ||
            !System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.SpecificCultures).Any(c => new System.Globalization.RegionInfo(c.Name).ISOCurrencySymbol == r.CurrencyCode))) throw Invalid("price", "Enter a valid optional shelf price and currency.");
        try { proposal.AttachPendingDiscovery(r.LocationId, r.ObservedAtUtc, r.PriceAmount, r.CurrencyCode); }
        catch (InvalidOperationException e) { throw Invalid("status", e.Message); }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
    public async Task<PublicProductProposalReceipt> BeginAsync(AuthenticatedUser actor, string barcode, Guid contributionId, CancellationToken ct = default)
    {
        var gtin = RetailGtin.Normalise(barcode) ?? throw Invalid("gtin", "Enter a valid barcode.");
        if (contributionId == Guid.Empty) throw Invalid("contributionId", "A contribution identifier is required.");
        var existing = await db.CatalogueSubmissions.SingleOrDefaultAsync(s => s.SubmittedByUserId == actor.UserId && s.PublicContributionId == contributionId, ct);
        if (existing is not null) {
            if (existing.ProposedGtin != gtin) throw Invalid("contributionId", "This recovery identifier belongs to another barcode.");
            return new(existing.Id);
        }
        var draft = new CatalogueSubmission(CatalogueSubmissionSource.Explorer, actor.UserId, "Awaiting packaging evidence", "Unfinished product proposal");
        draft.SetPublicContribution(contributionId); draft.UpdateIdentity(gtin, null, null); db.CatalogueSubmissions.Add(draft);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505", ConstraintName: "IX_catalogue_submissions_SubmittedByUserId_PublicContributionId" }) {
            db.ChangeTracker.Clear(); return await BeginAsync(actor, gtin, contributionId, ct);
        }
        return new(draft.Id);
    }

    private static CatalogueSubmissionImageReceipt ImageReceipt(CatalogueSubmissionImage image) => new(image.Id, image.SubmissionId, image.Role, image.OriginalFileName, image.ContentType, image.FileSizeBytes, image.SourceType, null, image.SourceNotes,
        image.PermissionStatus, null, image.Visibility, $"/api/v1/public-product-proposals/{image.SubmissionId}/images/{image.Id}", image.CreatedAtUtc, image.UpdatedAtUtc);

    public async Task<IReadOnlyList<CatalogueSubmissionImageReceipt>> ImagesAsync(AuthenticatedUser actor, Guid id, CancellationToken ct = default) =>
        (await (from i in db.CatalogueSubmissionImages.AsNoTracking() join s in db.CatalogueSubmissions on i.SubmissionId equals s.Id
        where s.Id == id && s.SubmittedByUserId == actor.UserId && i.IsExplorerEvidence orderby i.CreatedAtUtc select i).ToListAsync(ct)).Select(ImageReceipt).ToArray();

    public async Task<CatalogueSubmissionImageReceipt> AddEvidenceAsync(AuthenticatedUser actor, Guid id, Guid uploadId, AddCatalogueSubmissionImage upload, CancellationToken ct = default)
    {
        if (uploadId == Guid.Empty) throw Invalid("uploadId", "An upload recovery identifier is required.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({id.ToString()}, 0))", ct);
        var proposal = await db.CatalogueSubmissions.SingleOrDefaultAsync(s => s.Id == id && s.Source == CatalogueSubmissionSource.Explorer && s.SubmittedByUserId == actor.UserId, ct) ?? throw new KeyNotFoundException();
        if (upload.Role is not CatalogueSubmissionImageRole.PackFront and not CatalogueSubmissionImageRole.PackBack and not CatalogueSubmissionImageRole.Other ||
            upload.FileSizeBytes is <= 0 or > 15 * 1024 * 1024 || upload.ContentType is not "image/jpeg" and not "image/png" and not "image/webp")
            throw Invalid("file", "Choose a JPEG, PNG or WebP packaging photograph up to 15 MB.");
        using var bytes = new MemoryStream();
        var buffer = new byte[64 * 1024];
        int read;
        while ((read = await upload.Content.ReadAsync(buffer, ct)) != 0) {
            if (bytes.Length + read > 15 * 1024 * 1024) throw Invalid("file", "Choose a packaging photograph up to 15 MB.");
            await bytes.WriteAsync(buffer.AsMemory(0, read), ct);
        }
        if (bytes.Length != upload.FileSizeBytes || bytes.Length > 15 * 1024 * 1024) throw Invalid("file", "The photograph size does not match its upload.");
        var body = bytes.ToArray();
        var valid = upload.ContentType switch { "image/jpeg" => body.Length >= 3 && body[0] == 255 && body[1] == 216 && body[2] == 255,
            "image/png" => body.Length >= 8 && body.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            _ => body.Length >= 12 && System.Text.Encoding.ASCII.GetString(body, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(body, 8, 4) == "WEBP" };
        if (!valid) throw Invalid("file", "The file is not the selected image format.");
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(body));
        var previous = await db.CatalogueSubmissionImages.SingleOrDefaultAsync(i => i.SubmissionId == id && i.EvidenceUploadId == uploadId, ct);
        if (previous is not null) {
            if (previous.Role != upload.Role || previous.ContentType != upload.ContentType || previous.EvidenceContentHash != hash) throw Invalid("uploadId", "This upload identifier belongs to another photograph.");
            await transaction.CommitAsync(ct); return ImageReceipt(previous);
        }
        if (proposal.Status != CatalogueSubmissionStatus.Draft) throw Invalid("status", "This proposal has already been submitted. Your existing evidence is retained.");
        if (await db.CatalogueSubmissionImages.CountAsync(i => i.SubmissionId == id, ct) >= 4) throw Invalid("file", "Up to four packaging photographs can be kept with a proposal.");
        var key = $"{id:N}/{Guid.NewGuid():N}" + (upload.ContentType == "image/jpeg" ? ".jpg" : upload.ContentType == "image/png" ? ".png" : ".webp");
        var image = new CatalogueSubmissionImage(id, upload.Role, key, Path.GetFileName(upload.OriginalFileName), upload.ContentType, upload.FileSizeBytes,
            CatalogueImageSourceType.UserCommunity, sourceNotes: "Explorer packaging evidence; not approved catalogue imagery.");
        image.MarkExplorerEvidence(uploadId, hash); bytes.Position = 0;
        try { await storage.SaveAsync(key, bytes, ct); db.CatalogueSubmissionImages.Add(image); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); }
        catch { await storage.DeleteAsync(key, ct); throw; }
        return ImageReceipt(image);
    }

    public async Task<CatalogueSubmissionImageContent?> EvidenceAsync(AuthenticatedUser actor, Guid id, Guid imageId, CancellationToken ct = default)
    {
        var image = await (from i in db.CatalogueSubmissionImages join s in db.CatalogueSubmissions on i.SubmissionId equals s.Id
            where s.Id == id && i.Id == imageId && i.IsExplorerEvidence && s.SubmittedByUserId == actor.UserId select i).SingleOrDefaultAsync(ct);
        if (image is null) return null;
        var content = await storage.OpenReadAsync(image.StorageKey, ct);
        return content is null ? null : new(content, image.ContentType, image.OriginalFileName);
    }

    public async Task ReconcileAsync(Guid id, Guid packId, CancellationToken ct = default)
    {
        await using var transaction = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(ct) : null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({id.ToString()}, 0))", ct);
        var proposal = await db.CatalogueSubmissions.SingleAsync(s => s.Id == id, ct);
        if (proposal.ResultingObservationId.HasValue && proposal.ResolvedPackTypeId != packId)
            throw Invalid("packTypeId", "This discovery has already been resolved to another exact pack.");
        if (proposal.ResultingObservationId.HasValue || !proposal.PendingLocationId.HasValue) { if (transaction is not null) await transaction.CommitAsync(ct); return; }
        if (proposal.Status != CatalogueSubmissionStatus.Published || !proposal.SubmittedByUserId.HasValue || !proposal.PendingObservedAtUtc.HasValue)
            throw Invalid("status", "Resolve the proposal to a published canonical pack before recording its discovery.");
        var pack = await places.PackAsync(packId, ct) ?? throw Invalid("packTypeId", "The resolved pack must be current.");
        if (!await db.Locations.AnyAsync(l => l.Id == proposal.PendingLocationId && l.IsPublicCommercialPlace && l.Latitude != null && l.Longitude != null, ct))
            throw Invalid("locationId", "The preserved discovery needs a confirmed public shop.");
        if (pack.Product.Id != proposal.PublishedProductId) throw Invalid("packTypeId", "The pack does not belong to the resolved product.");
        var previous = await db.Observations.SingleOrDefaultAsync(o => o.AuthorUserId == proposal.SubmittedByUserId && o.ContributionId == proposal.PublicContributionId, ct);
        if (previous is null) {
            previous = new Observation(proposal.SubmittedByUserId.Value, ObservationType.RetailAvailability, proposal.PendingObservedAtUtc.Value, pack.Product.Id, locationId: proposal.PendingLocationId);
            previous.RecordExactPack(packId, proposal.PublicContributionId!.Value, proposal.PendingPriceAmount, proposal.PendingCurrencyCode);
            previous.Submit(); db.Observations.Add(previous);
        } else if (previous.PackTypeId != packId || previous.LocationId != proposal.PendingLocationId ||
            previous.ObservedAtUtc != proposal.PendingObservedAtUtc || previous.PriceAmount != proposal.PendingPriceAmount ||
            previous.PriceCurrencyCode != proposal.PendingCurrencyCode)
            throw Invalid("contributionId", "The pending discovery conflicts with an existing observation.");
        proposal.LinkResolvedDiscovery(packId, previous.Id); await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
    }
    public async Task<PublicProductProposalReceipt> SubmitAsync(AuthenticatedUser actor, PublicProductProposal r, CancellationToken ct = default)
    {
        if (r.Discovery is { } original) r = r with { Discovery = original with { ObservedAtUtc = new DateTimeOffset(original.ObservedAtUtc.UtcTicks - original.ObservedAtUtc.UtcTicks % 10, TimeSpan.Zero) } };
        var gtin = RetailGtin.Normalise(r.Gtin) ?? throw Invalid("gtin", "Enter a valid retail barcode, including its check digit.");
        if (r.ContributionId == Guid.Empty) throw Invalid("contributionId", "A contribution identifier is required.");
        var brand = Text(r.BrandName, 200, "brandName"); var name = Text(r.ProductName, 250, "productName");
        var manufacturer = Optional(r.ManufacturerName, 200, "manufacturerName") ?? "Unknown — needs editorial resolution";
        var version = Optional(r.Version, 200, "version"); var size = Optional(r.Size, 64, "size");
        var notes = Optional(r.Notes, 2000, "notes");
        if (r.ProductType.HasValue && !Enum.IsDefined(r.ProductType.Value)) throw Invalid("productType", "Choose a supported product type.");
        if (r.SuggestedExistingProductId.HasValue && !await db.Products.AnyAsync(p => p.Id == r.SuggestedExistingProductId && p.Status == ProductStatus.Current, ct))
            throw Invalid("suggestedExistingProductId", "Choose a current public catalogue candidate.");
        if (r.Discovery is { } discovery) {
            if (!await db.Locations.AnyAsync(l => l.Id == discovery.LocationId && l.IsPublicCommercialPlace && l.Latitude != null && l.Longitude != null, ct))
                throw Invalid("locationId", "Choose a confirmed public shop.");
            if (discovery.ObservedAtUtc < new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero) || discovery.ObservedAtUtc > DateTimeOffset.UtcNow.AddMinutes(5))
                throw Invalid("observedAtUtc", "Choose a valid discovery time.");
            if (discovery.PriceAmount.HasValue && (discovery.PriceAmount is < 0 or > 9999999999.99m || decimal.Round(discovery.PriceAmount.Value, 2) != discovery.PriceAmount ||
                !System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.SpecificCultures).Any(c => new System.Globalization.RegionInfo(c.Name).ISOCurrencySymbol == discovery.CurrencyCode?.Trim().ToUpperInvariant())))
                throw Invalid("price", "Enter a valid optional shelf price and currency.");
        }
        if (r.Quantity is < 1 or > 100000) throw Invalid("quantity", "Enter the number in this pack, or leave it empty if you do not know.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var draftId = await db.CatalogueSubmissions.AsNoTracking().Where(s => s.SubmittedByUserId == actor.UserId && s.PublicContributionId == r.ContributionId).Select(s => (Guid?)s.Id).SingleOrDefaultAsync(ct);
        if (draftId.HasValue) await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({draftId.Value.ToString()}, 0))", ct);
        var existing = await db.CatalogueSubmissions.SingleOrDefaultAsync(s =>
            s.SubmittedByUserId == actor.UserId && s.PublicContributionId == r.ContributionId, ct);
        if (existing is not null && existing.Status != CatalogueSubmissionStatus.Draft)
        {
            var existingSize = await (from v in db.CatalogueSubmissionVariants
                join s in db.CatalogueSubmissionSizeVariants on v.Id equals s.VariantId
                where v.SubmissionId == existing.Id select s.ManufacturerSize).SingleOrDefaultAsync(ct);
            if (existing.ProposedGtin != gtin || existing.ProposedProductName != name || existing.ProposedBrandName != brand ||
                existing.ProposedManufacturerName != manufacturer || existing.ProposedVariantName != version || existing.ProposedPackQuantity != r.Quantity || existingSize != size ||
                existing.Notes != notes || existing.ProposedProductType != r.ProductType || existing.SuggestedExistingProductId != r.SuggestedExistingProductId ||
                existing.PendingLocationId != r.Discovery?.LocationId || existing.PendingPriceAmount != r.Discovery?.PriceAmount ||
                existing.PendingObservedAtUtc != r.Discovery?.ObservedAtUtc || existing.PendingCurrencyCode != (r.Discovery?.PriceAmount.HasValue == true ? r.Discovery.CurrencyCode?.Trim().ToUpperInvariant() : null))
                throw Invalid("contributionId", "This contribution identifier already belongs to another proposal.");
            return new(existing.Id);
        }
        var proposal = existing ?? new CatalogueSubmission(CatalogueSubmissionSource.Explorer, actor.UserId, manufacturer, name, version, brand, notes);
        if (existing is null) { proposal.SetPublicContribution(r.ContributionId); db.CatalogueSubmissions.Add(proposal); }
        else if (existing.ProposedGtin != gtin) throw Invalid("gtin", "The draft belongs to another barcode.");
        if (existing is not null && !await db.CatalogueSubmissionImages.AnyAsync(i => i.SubmissionId == proposal.Id && i.IsExplorerEvidence && i.Role == CatalogueSubmissionImageRole.PackFront, ct))
            throw Invalid("photos", "Add a front-of-pack photograph before submitting this evidence draft.");
        proposal.UpdateProposal(manufacturer, name, version, brand, notes);
        proposal.UpdateProductType(r.ProductType);
        proposal.SetExplorerEvidence(r.SuggestedExistingProductId, r.Discovery?.LocationId, r.Discovery?.ObservedAtUtc, r.Discovery?.PriceAmount, r.Discovery?.CurrencyCode?.Trim().ToUpperInvariant());
        proposal.SetProposedPackQuantity(r.Quantity);
        proposal.UpdateIdentity(gtin, null, null);
        var variant = new CatalogueSubmissionVariant(proposal.Id, version);
        db.CatalogueSubmissionVariants.Add(variant);
        if (size is not null) db.CatalogueSubmissionSizeVariants.Add(new CatalogueSubmissionSizeVariant(variant.Id, size,
            manufacturerPackQuantity: r.Quantity, gtin: gtin));
        // Proposed facts only; no canonical Product, PackType, identifier or retailer listing is written.
        proposal.BeginVerification();
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505", ConstraintName: "IX_catalogue_submissions_SubmittedByUserId_PublicContributionId" })
        {
            db.ChangeTracker.Clear();
            await transaction.RollbackAsync(ct); await transaction.DisposeAsync();
            return await SubmitAsync(actor, r, ct);
        }
        await transaction.CommitAsync(ct);
        return new(proposal.Id);
    }

    public async Task ResolveAsync(AuthenticatedUser actor, Guid id, ResolveBarcodeProposal r, CancellationToken ct = default)
    {
        if (!await editorial.CanPublishAtlasAsync(actor, ct)) throw new UnauthorizedAccessException();
        var rationale = Text(r.Rationale, 2000, "rationale");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({id.ToString()}, 0))", ct);
        var submission = await db.CatalogueSubmissions.SingleOrDefaultAsync(s => s.Id == id, ct) ?? throw new KeyNotFoundException();
        if (submission.Status == CatalogueSubmissionStatus.Published && submission.ResolvedPackTypeId == r.PackTypeId) {
            await ReconcileAsync(id, r.PackTypeId, ct); await transaction.CommitAsync(ct); return;
        }
        if (submission.Status != CatalogueSubmissionStatus.Approved) throw Invalid("status", "Complete verification and approve this proposal before resolving it to an existing pack.");
        var gtin = RetailGtin.Normalise(submission.ProposedGtin) ?? throw Invalid("gtin", "The proposal needs a valid verified barcode.");
        var pack = await places.PackAsync(r.PackTypeId, ct) ?? throw Invalid("packTypeId", "Choose a current canonical pack.");
        var equivalent = gtin.TrimStart('0');
        var identifiers = await db.ProductIdentifiers.Where(i => i.Type == IdentifierType.Gtin && i.Value.TrimStart('0') == equivalent).ToListAsync(ct);
        if (identifiers.Any(i => i.PackTypeId != r.PackTypeId)) throw Invalid("gtin", "This barcode is assigned to another pack; resolve the catalogue conflict first.");
        if (identifiers.Count == 0) db.ProductIdentifiers.Add(new ProductIdentifier(r.PackTypeId, IdentifierType.Gtin, gtin));
        submission.ResolveToExistingPack(pack.Product.Id, r.PackTypeId);
        await ReconcileAsync(id, r.PackTypeId, ct);
        db.CatalogueSubmissionEditorialDecisions.Add(new CatalogueSubmissionEditorialDecision(id, actor.UserId, EditorialOutcome.Accepted,
            $"Resolved barcode {gtin} to existing pack {r.PackTypeId}. {rationale}"));
        db.CatalogueAuditRecords.Add(new CatalogueAuditRecord(CatalogueAuditAction.ProductChanged, pack.Product.Id, actor.UserId,
            DateTimeOffset.UtcNow, JsonSerializer.Serialize(new { submissionId = id, gtin, packTypeId = r.PackTypeId }),
            JsonSerializer.Serialize(new { productId = pack.Product.Id, pack.ProductVariantId, pack.SizeVariantId, packTypeId = r.PackTypeId }),
            "Moderated public barcode proposal", JsonSerializer.Serialize(new { submissionId = id }), rationale, id.ToString()));
        // Save atomically: no new canonical products, variants, sizes or packs.
        try { await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505" })
        { throw Invalid("gtin", "This barcode changed while it was being resolved. Review the current mapping before continuing."); }
    }

    private static string Text(string? value, int max, string field) => string.IsNullOrWhiteSpace(value) || value.Trim().Length > max
        ? throw Invalid(field, $"Enter a value of at most {max} characters.") : value.Trim();
    private static string? Optional(string? value, int max, string field) => string.IsNullOrWhiteSpace(value) ? null : Text(value, max, field);
    private static CatalogueValidationException Invalid(string field, string message) => new(field, message);
}
