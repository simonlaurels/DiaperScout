using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json;

namespace DiaperScout.Infrastructure;

internal sealed class PublicProductContributions(DiaperScoutDbContext db, IEditorialAuthorisation editorial,
    IPlaceObservations places) : IPublicProductContributions
{
    public async Task<PublicProductProposalReceipt> SubmitAsync(ExplorerIdentity actor, PublicProductProposal r, CancellationToken ct = default)
    {
        var gtin = RetailGtin.Normalise(r.Gtin) ?? throw Invalid("gtin", "Enter a valid retail barcode, including its check digit.");
        if (r.ContributionId == Guid.Empty) throw Invalid("contributionId", "A contribution identifier is required.");
        var brand = Text(r.BrandName, 200, "brandName"); var name = Text(r.ProductName, 250, "productName");
        var manufacturer = Optional(r.ManufacturerName, 200, "manufacturerName") ?? "Unknown — needs editorial resolution";
        var version = Optional(r.Version, 200, "version"); var size = Optional(r.Size, 64, "size");
        if (r.Quantity is < 1 or > 100000) throw Invalid("quantity", "Enter the number in this pack, or leave it empty if you do not know.");
        var existing = await db.CatalogueSubmissions.AsNoTracking().SingleOrDefaultAsync(s =>
            s.SubmittedByUserId == actor.UserId && s.PublicContributionId == r.ContributionId, ct);
        if (existing is not null)
        {
            var existingSize = await (from v in db.CatalogueSubmissionVariants
                join s in db.CatalogueSubmissionSizeVariants on v.Id equals s.VariantId
                where v.SubmissionId == existing.Id select s.ManufacturerSize).SingleOrDefaultAsync(ct);
            if (existing.ProposedGtin != gtin || existing.ProposedProductName != name || existing.ProposedBrandName != brand ||
                existing.ProposedManufacturerName != manufacturer || existing.ProposedVariantName != version || existing.ProposedPackQuantity != r.Quantity || existingSize != size)
                throw Invalid("contributionId", "This contribution identifier already belongs to another proposal.");
            return new(existing.Id);
        }
        var proposal = new CatalogueSubmission(CatalogueSubmissionSource.Explorer, actor.UserId, manufacturer, name, version, brand);
        proposal.SetPublicContribution(r.ContributionId);
        proposal.SetProposedPackQuantity(r.Quantity);
        proposal.UpdateIdentity(gtin, null, null);
        var variant = new CatalogueSubmissionVariant(proposal.Id, version);
        db.CatalogueSubmissions.Add(proposal); db.CatalogueSubmissionVariants.Add(variant);
        if (size is not null) db.CatalogueSubmissionSizeVariants.Add(new CatalogueSubmissionSizeVariant(variant.Id, size,
            manufacturerPackQuantity: r.Quantity, gtin: gtin));
        // Proposed facts only; no canonical Product, PackType, identifier or retailer listing is written.
        proposal.BeginVerification();
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505", ConstraintName: "IX_catalogue_submissions_SubmittedByUserId_PublicContributionId" })
        {
            db.ChangeTracker.Clear();
            return await SubmitAsync(actor, r, ct);
        }
        return new(proposal.Id);
    }

    public async Task ResolveAsync(AuthenticatedUser actor, Guid id, ResolveBarcodeProposal r, CancellationToken ct = default)
    {
        if (!await editorial.CanPublishAtlasAsync(actor, ct)) throw new UnauthorizedAccessException();
        var rationale = Text(r.Rationale, 2000, "rationale");
        var submission = await db.CatalogueSubmissions.SingleOrDefaultAsync(s => s.Id == id, ct) ?? throw new KeyNotFoundException();
        if (submission.Status != CatalogueSubmissionStatus.Approved) throw Invalid("status", "Complete verification and approve this proposal before resolving it to an existing pack.");
        var gtin = RetailGtin.Normalise(submission.ProposedGtin) ?? throw Invalid("gtin", "The proposal needs a valid verified barcode.");
        var pack = await places.PackAsync(r.PackTypeId, ct) ?? throw Invalid("packTypeId", "Choose a current canonical pack.");
        var equivalent = gtin.TrimStart('0');
        var identifiers = await db.ProductIdentifiers.Where(i => i.Type == IdentifierType.Gtin && i.Value.TrimStart('0') == equivalent).ToListAsync(ct);
        if (identifiers.Any(i => i.PackTypeId != r.PackTypeId)) throw Invalid("gtin", "This barcode is assigned to another pack; resolve the catalogue conflict first.");
        if (identifiers.Count == 0) db.ProductIdentifiers.Add(new ProductIdentifier(r.PackTypeId, IdentifierType.Gtin, gtin));
        submission.ResolveToExistingPack(pack.Product.Id, r.PackTypeId);
        db.CatalogueSubmissionEditorialDecisions.Add(new CatalogueSubmissionEditorialDecision(id, actor.UserId, EditorialOutcome.Accepted,
            $"Resolved barcode {gtin} to existing pack {r.PackTypeId}. {rationale}"));
        db.CatalogueAuditRecords.Add(new CatalogueAuditRecord(CatalogueAuditAction.ProductChanged, pack.Product.Id, actor.UserId,
            DateTimeOffset.UtcNow, JsonSerializer.Serialize(new { submissionId = id, gtin, packTypeId = r.PackTypeId }),
            JsonSerializer.Serialize(new { productId = pack.Product.Id, pack.ProductVariantId, pack.SizeVariantId, packTypeId = r.PackTypeId }),
            "Moderated public barcode proposal", JsonSerializer.Serialize(new { submissionId = id }), rationale, id.ToString()));
        // Save atomically: no new canonical products, variants, sizes or packs.
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505" })
        { throw Invalid("gtin", "This barcode changed while it was being resolved. Review the current mapping before continuing."); }
    }

    private static string Text(string? value, int max, string field) => string.IsNullOrWhiteSpace(value) || value.Trim().Length > max
        ? throw Invalid(field, $"Enter a value of at most {max} characters.") : value.Trim();
    private static string? Optional(string? value, int max, string field) => string.IsNullOrWhiteSpace(value) ? null : Text(value, max, field);
    private static CatalogueValidationException Invalid(string field, string message) => new(field, message);
}
