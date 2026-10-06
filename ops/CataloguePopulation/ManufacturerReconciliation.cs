using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiaperScout.CataloguePopulation;

public sealed record ManufacturerCoverage(Guid ProductId, Guid PackId, string Gtin, string Article, int DeclarationPage);
public sealed record ManufacturerReconciliationPlan(string CorrelationId, Guid PreviousManufacturerId,
    string PreviousManufacturerName, Guid BrandId, Guid LegalManufacturerId, string LegalManufacturerName,
    string LegalManufacturerSlug, string DeclarationUrl, string DeclarationSha256, string LegalManufacturerSrn,
    string ProductionProvenance, string[] ProvenanceSources, string[] DeclaredArticles, ManufacturerCoverage[] Coverage);
public sealed record ManufacturerReconciliationPreview(string Fingerprint, Guid PreviousManufacturerId,
    Guid LegalManufacturerId, Guid BrandId, Guid[] ProductIds, int PackCount, string AllowedChanges);
public sealed record ManufacturerReconciliationReceipt(bool Created, Guid ManufacturerId, Guid[] AuditIds, string PreservationFingerprint);

/// <summary>Explicit, hash-bound operational correction. Never watches research files or merges organisations.</summary>
public sealed class ManufacturerReconciliation(DiaperScoutDbContext db)
{
    public static void Validate(ManufacturerReconciliationPlan plan)
    {
        if (plan.PreviousManufacturerId == Guid.Empty || plan.LegalManufacturerId == Guid.Empty ||
            plan.PreviousManufacturerId == plan.LegalManufacturerId || plan.BrandId == Guid.Empty ||
            string.IsNullOrWhiteSpace(plan.CorrelationId) || string.IsNullOrWhiteSpace(plan.ProductionProvenance) ||
            string.IsNullOrWhiteSpace(plan.LegalManufacturerSrn) || string.IsNullOrWhiteSpace(plan.LegalManufacturerName) ||
            string.IsNullOrWhiteSpace(plan.LegalManufacturerSlug) || plan.PreviousManufacturerName == plan.LegalManufacturerName ||
            plan.DeclarationSha256.Length != 64 || !plan.DeclarationSha256.All(Uri.IsHexDigit) ||
            plan.Coverage.Length == 0 || plan.Coverage.Select(c => c.PackId).Distinct().Count() != plan.Coverage.Length)
            throw new InvalidOperationException("An explicit, distinct manufacturer plan with unique exact-pack coverage is required.");
        foreach (var source in plan.ProvenanceSources.Append(plan.DeclarationUrl))
            if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) || uri.Scheme != "https")
                throw new InvalidOperationException("Independent HTTPS declaration and provenance sources are required.");
        foreach (var coverage in plan.Coverage)
            if (!plan.DeclaredArticles.Contains(coverage.Article) || coverage.DeclarationPage < 2 ||
                RetailGtin.Normalise(coverage.Gtin) != coverage.Gtin || coverage.ProductId == Guid.Empty)
                throw new InvalidOperationException("Every exact pack must have valid GTIN and explicit signed-declaration article coverage.");
    }

    public async Task<ManufacturerReconciliationPreview> PreviewAsync(ManufacturerReconciliationPlan plan)
    {
        Validate(plan);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        await db.Database.ExecuteSqlRawAsync("SET TRANSACTION READ ONLY");
        await CheckScopeAsync(plan, false);
        var fingerprint = await FingerprintAsync();
        await transaction.RollbackAsync();
        return new(fingerprint, plan.PreviousManufacturerId, plan.LegalManufacturerId, plan.BrandId,
            plan.Coverage.Select(c => c.ProductId).Distinct().Order().ToArray(), plan.Coverage.Length,
            "Add distinct legal manufacturer; change only Brand.ManufacturerId and covered Product.ManufacturerId; append provenance audits. All existing scalar values/history preserved.");
    }

    public async Task<ManufacturerReconciliationReceipt> ApplyAsync(AuthenticatedUser actor,
        ManufacturerReconciliationPlan plan, string previewFingerprint)
    {
        Validate(plan);
        if (!await db.PrivilegedRoleAssignments.AnyAsync(r => r.UserId == actor.UserId &&
            r.Role == PrivilegedRole.Administrator && r.RevokedAtUtc == null) ||
            !await db.Users.AnyAsync(u => u.Id == actor.UserId && u.Status == UserAccountStatus.Active))
            throw new UnauthorizedAccessException();
        if ((await db.Database.GetPendingMigrationsAsync()).Any())
            throw new InvalidOperationException("Migrations must be current.");
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(683492710246301)");
        var prior = await db.CatalogueAuditRecords.Where(a => a.CorrelationId == plan.CorrelationId).ToListAsync();
        if (prior.Count > 0)
        {
            await CheckScopeAsync(plan, true);
            if (prior.Count != plan.Coverage.Select(c => c.ProductId).Distinct().Count() ||
                prior.Any(a => a.SubmittedPayloadJson != JsonSerializer.Serialize(new { plan, previewFingerprint })))
                throw new InvalidOperationException("Retry plan differs from committed reconciliation.");
            await transaction.RollbackAsync();
            return new(false, plan.LegalManufacturerId, prior.Select(a => a.Id).Order().ToArray(), await FingerprintAsync(plan));
        }
        await CheckScopeAsync(plan, false);
        if (await FingerprintAsync() != previewFingerprint)
            throw new InvalidOperationException("Catalogue changed after preview; obtain a fresh read-only preview.");
        var preserved = await FingerprintAsync(plan);
        var manufacturer = new Manufacturer(plan.LegalManufacturerId, plan.LegalManufacturerName, plan.LegalManufacturerSlug,
            "https://www.abena.com");
        db.Manufacturers.Add(manufacturer);
        var brand = await db.Brands.SingleAsync(b => b.Id == plan.BrandId);
        brand.ReassignManufacturer(plan.PreviousManufacturerId, manufacturer.Id);
        var ids = plan.Coverage.Select(c => c.ProductId).Distinct().ToArray();
        var products = await db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
        var audits = new List<CatalogueAuditRecord>();
        foreach (var product in products)
        {
            product.ReassignManufacturer(plan.PreviousManufacturerId, manufacturer.Id);
            var audit = new CatalogueAuditRecord(CatalogueAuditAction.ProductChanged, product.Id, actor.UserId,
                DateTimeOffset.UtcNow, JsonSerializer.Serialize(new { plan, previewFingerprint }),
                JsonSerializer.Serialize(new[] { product.Id, brand.Id, manufacturer.Id, plan.PreviousManufacturerId }),
                "Signed declaration establishes legal manufacturer " + plan.LegalManufacturerName + "; SRN " + plan.LegalManufacturerSrn,
                JsonSerializer.Serialize(plan.ProvenanceSources.Append(plan.DeclarationUrl)),
                plan.ProductionProvenance + " Original organisation and source/audit history retained; no alias, merge or recreation.", plan.CorrelationId);
            audits.Add(audit);
            db.CatalogueAuditRecords.Add(audit);
        }
        db.ChangeTracker.DetectChanges();
        foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State == EntityState.Modified))
            if (entry.Entity is not (Brand or Product) || entry.Properties.Any(p => p.IsModified && p.Metadata.Name != "ManufacturerId"))
                throw new InvalidOperationException("Reconciliation attempted an unrelated mutation.");
        await db.SaveChangesAsync();
        await CheckScopeAsync(plan, true);
        if (await FingerprintAsync(plan) != preserved)
            throw new InvalidOperationException("Protected catalogue/source/history values changed; transaction will roll back.");
        await transaction.CommitAsync();
        return new(true, manufacturer.Id, audits.Select(a => a.Id).Order().ToArray(), preserved);
    }

    private async Task CheckScopeAsync(ManufacturerReconciliationPlan plan, bool reconciled)
    {
        var previous = await db.Manufacturers.AsNoTracking().SingleAsync(m => m.Id == plan.PreviousManufacturerId);
        if (previous.Name != plan.PreviousManufacturerName)
            throw new InvalidOperationException("Original organisation identity changed.");
        var target = await db.Manufacturers.AsNoTracking().Where(m => m.Id == plan.LegalManufacturerId ||
            m.Name == plan.LegalManufacturerName || m.Slug == plan.LegalManufacturerSlug).ToListAsync();
        if ((!reconciled && target.Count != 0) || (reconciled && (target.Count != 1 ||
            target[0].Id != plan.LegalManufacturerId || target[0].Name != plan.LegalManufacturerName || target[0].Slug != plan.LegalManufacturerSlug)))
            throw new InvalidOperationException("Target organisation collision or identity mismatch.");
        var expected = reconciled ? plan.LegalManufacturerId : plan.PreviousManufacturerId;
        var brand = await db.Brands.AsNoTracking().SingleAsync(b => b.Id == plan.BrandId);
        var products = await db.Products.AsNoTracking().Where(p => p.BrandId == plan.BrandId).ToListAsync();
        if (brand.ManufacturerId != expected || products.Any(p => p.ManufacturerId != expected) ||
            !products.Select(p => p.Id).Order().SequenceEqual(plan.Coverage.Select(c => c.ProductId).Distinct().Order()))
            throw new InvalidOperationException("Brand scope changed or contains uncovered products; no partial brand reassignment permitted.");
        var ids = products.Select(p => p.Id).ToArray();
        var packs = await (from pack in db.PackTypes join size in db.SizeVariants on pack.SizeVariantId equals size.Id
            join variant in db.ProductVariants on size.ProductVariantId equals variant.Id
            where ids.Contains(variant.ProductId) select new { pack.Id, variant.ProductId }).ToListAsync();
        if (!packs.Select(p => p.Id).Order().SequenceEqual(plan.Coverage.Select(c => c.PackId).Order()))
            throw new InvalidOperationException("Every attached pack must be covered; pack scope changed.");
        var identifiers = await db.ProductIdentifiers.AsNoTracking().Where(i => i.Type == IdentifierType.Gtin).ToListAsync();
        foreach (var coverage in plan.Coverage)
            if (!packs.Any(p => p.Id == coverage.PackId && p.ProductId == coverage.ProductId) ||
                !identifiers.Any(i => i.PackTypeId == coverage.PackId && i.Value.PadLeft(14, '0') == coverage.Gtin.PadLeft(14, '0')))
                throw new InvalidOperationException("Exact product/pack/GTIN mapping changed.");
    }

    // All scalar catalogue properties plus source/submission/audit history, not navigation graphs or user credentials.
    private async Task<string> FingerprintAsync(ManufacturerReconciliationPlan? plan = null)
    {
        var rows = new List<string>();
        foreach (var type in new[] { typeof(Manufacturer), typeof(Brand), typeof(Product), typeof(ProductVariant),
            typeof(SizeVariant), typeof(PackType), typeof(ProductIdentifier), typeof(CatalogueAuditRecord),
            typeof(CatalogueSubmission), typeof(CatalogueSubmissionVariant), typeof(CatalogueSubmissionSizeVariant),
            typeof(CatalogueSubmissionVariantOverride), typeof(CatalogueSubmissionVerification),
            typeof(CatalogueSubmissionEditorialDecision), typeof(CatalogueSubmissionImage),
            typeof(CatalogueSubmissionRetailDestination), typeof(CatalogueSubmissionRetailAffiliate) })
        {
            var method = typeof(ManufacturerReconciliation).GetMethod(nameof(ScalarRowsAsync),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.MakeGenericMethod(type);
            rows.AddRange(await (Task<string[]>)method.Invoke(this, [plan])!);
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", rows.Order(StringComparer.Ordinal)))));
    }
    private async Task<string[]> ScalarRowsAsync<T>(ManufacturerReconciliationPlan? plan) where T : class
    {
        var values = await db.Set<T>().AsNoTracking().ToListAsync();
        var properties = db.Model.FindEntityType(typeof(T))!.GetProperties().OrderBy(p => p.Name).ToArray();
        return values.Where(value => plan is null ||
            !(value is Manufacturer manufacturer && manufacturer.Id == plan.LegalManufacturerId) &&
            !(value is CatalogueAuditRecord audit && audit.CorrelationId == plan.CorrelationId))
            .Select(value => typeof(T).Name + ":" + JsonSerializer.Serialize(properties
                .Where(p => plan is null || p.Name != "ManufacturerId" ||
                    !(value is Brand brand && brand.Id == plan.BrandId) &&
                    !(value is Product product && plan.Coverage.Any(c => c.ProductId == product.Id)))
                .ToDictionary(p => p.Name, p => p.PropertyInfo!.GetValue(value)))).ToArray();
    }
}
