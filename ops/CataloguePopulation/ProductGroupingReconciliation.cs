using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiaperScout.CataloguePopulation;

public sealed record GroupingVariant(Guid Id, Guid OriginalProductId, string OriginalName, string Name);
public sealed record GroupingProduct(Guid Id, string Name, string Slug);
public sealed record GroupingPack(Guid Id, Guid VariantId, string Gtin);
public sealed record ProductGroupingPlan(string CorrelationId, Guid TargetProductId, string ProductName,
    GroupingProduct[] Products, GroupingVariant[] Variants, GroupingPack[] Packs, string Rationale, string[] Sources);
public sealed record ProductGroupingPreview(string Fingerprint, ProductGroupingPlan Plan);
public sealed record ProductGroupingReceipt(bool Created, Guid[] AuditIds, string PreservationFingerprint);

/// <summary>Explicitly approved grouping only. Retains old product identities and all descendants/history.</summary>
public sealed class ProductGroupingReconciliation(DiaperScoutDbContext db)
{
    public async Task<ProductGroupingPreview> PreviewAsync(ProductGroupingPlan plan)
    {
        Validate(plan);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        await db.Database.ExecuteSqlRawAsync("SET TRANSACTION READ ONLY");
        await CheckScopeAsync(plan, false);
        var fingerprint = await FingerprintAsync();
        await tx.RollbackAsync();
        return new(fingerprint, plan);
    }

    public async Task<ProductGroupingReceipt> ApplyAsync(AuthenticatedUser actor, ProductGroupingPlan plan, string previewFingerprint)
    {
        Validate(plan);
        if (!await db.Users.AnyAsync(u => u.Id == actor.UserId && u.Status == UserAccountStatus.Active) ||
            !await db.PrivilegedRoleAssignments.AnyAsync(r => r.UserId == actor.UserId && r.Role == PrivilegedRole.Administrator && r.RevokedAtUtc == null))
            throw new UnauthorizedAccessException();
        if ((await db.Database.GetPendingMigrationsAsync()).Any()) throw new InvalidOperationException("Migrations must be current.");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(683492710246301)");
        var payload = JsonSerializer.Serialize(new { plan, previewFingerprint });
        var prior = await db.CatalogueAuditRecords.Where(a => a.CorrelationId == plan.CorrelationId).ToListAsync();
        if (prior.Count > 0)
        {
            if (prior.Count != plan.Products.Length || prior.Any(a => a.SubmittedPayloadJson != payload))
                throw new InvalidOperationException("Retry plan differs from the committed grouping.");
            await CheckScopeAsync(plan, true);
            var fingerprint = await FingerprintAsync(plan);
            await tx.RollbackAsync();
            return new(false, prior.Select(a => a.Id).Order().ToArray(), fingerprint);
        }
        await CheckScopeAsync(plan, false);
        if (await FingerprintAsync() != previewFingerprint) throw new InvalidOperationException("Catalogue changed since preview.");
        var preserved = await FingerprintAsync(plan);
        var target = await db.Products.SingleAsync(p => p.Id == plan.TargetProductId);
        target.UpdateIdentity(target.ManufacturerId, target.BrandId, plan.ProductName, target.ProductType, target.Family,
            target.Description, target.DescriptionVisibility, target.OfficialWebsiteUrl);
        foreach (var identity in plan.Variants)
        {
            var variant = await db.ProductVariants.SingleAsync(v => v.Id == identity.Id);
            variant.ReconcileProduct(identity.OriginalProductId, target.Id, identity.OriginalName, identity.Name);
        }
        foreach (var source in plan.Products.Where(p => p.Id != target.Id))
        {
            var variants = plan.Variants.Where(v => v.OriginalProductId == source.Id).ToArray();
            // A bare historical route cannot choose between several original variants without another domain decision.
            if (variants.Length != 1) throw new InvalidOperationException("An unambiguous historical default variant is required.");
            db.ProductRouteRedirects.Add(new(source.Id, target.Id, variants[0].Id));
        }
        var audits = plan.Products.Select(p => new CatalogueAuditRecord(CatalogueAuditAction.ProductChanged, p.Id,
            actor.UserId, DateTimeOffset.UtcNow, payload,
            JsonSerializer.Serialize(plan.Products.Select(p => p.Id).Concat(plan.Variants.Select(v => v.Id))),
            "Reviewed manufacturer product/variant grouping", JsonSerializer.Serialize(plan.Sources),
            plan.Rationale + " All original product records, IDs, slugs, publication states and submission/source history retained.", plan.CorrelationId)).ToArray();
        db.CatalogueAuditRecords.AddRange(audits);
        db.ChangeTracker.DetectChanges();
        foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State == EntityState.Modified))
            if (entry.Entity is Product p ? p.Id != target.Id || entry.Properties.Any(x => x.IsModified && x.Metadata.Name != nameof(Product.Name)) :
                entry.Entity is ProductVariant v ? !plan.Variants.Any(i => i.Id == v.Id) || entry.Properties.Any(x => x.IsModified && x.Metadata.Name is not (nameof(ProductVariant.Name) or nameof(ProductVariant.ProductId))) : true)
                throw new InvalidOperationException("Grouping attempted an unrelated mutation.");
        await db.SaveChangesAsync();
        await CheckScopeAsync(plan, true);
        if (await FingerprintAsync(plan) != preserved) throw new InvalidOperationException("Protected values changed; grouping will roll back.");
        await tx.CommitAsync();
        return new(true, audits.Select(a => a.Id).Order().ToArray(), preserved);
    }

    private static void Validate(ProductGroupingPlan plan)
    {
        if (string.IsNullOrWhiteSpace(plan.CorrelationId) || string.IsNullOrWhiteSpace(plan.ProductName) ||
            plan.ProductName.Length > 250 || string.IsNullOrWhiteSpace(plan.Rationale) || plan.Products.Length < 2 ||
            !plan.Products.Any(p => p.Id == plan.TargetProductId) || plan.Products.Select(p => p.Id).Distinct().Count() != plan.Products.Length ||
            plan.Variants.Length < 2 || plan.Variants.Select(v => v.Id).Distinct().Count() != plan.Variants.Length ||
            plan.Variants.Select(v => v.Name.Trim().ToLowerInvariant()).Distinct().Count() != plan.Variants.Length ||
            plan.Variants.Any(v => string.IsNullOrWhiteSpace(v.Name) || v.Name.Length > 200 || !plan.Products.Any(p => p.Id == v.OriginalProductId)) ||
            plan.Packs.Length == 0 || plan.Packs.Select(p => p.Id).Distinct().Count() != plan.Packs.Length ||
            plan.Packs.Any(p => !plan.Variants.Any(v => v.Id == p.VariantId) || RetailGtin.Normalise(p.Gtin) != p.Gtin) ||
            plan.Sources.Length == 0 || plan.Sources.Any(s => !Uri.TryCreate(s, UriKind.Absolute, out var u) || u.Scheme != "https"))
            throw new InvalidOperationException("A complete, explicit product/variant/exact-pack grouping and independent evidence are required.");
    }

    private async Task CheckScopeAsync(ProductGroupingPlan plan, bool reconciled)
    {
        var ids = plan.Products.Select(p => p.Id).ToArray();
        var products = await db.Products.AsNoTracking().Where(p => ids.Contains(p.Id)).ToListAsync();
        var target = products.Single(p => p.Id == plan.TargetProductId);
        foreach (var identity in plan.Products)
        {
            var product = products.Single(p => p.Id == identity.Id);
            if (product.Name != (reconciled && product.Id == target.Id ? plan.ProductName : identity.Name) || product.Slug != identity.Slug ||
                product.ManufacturerId != target.ManufacturerId || product.BrandId != target.BrandId || product.ProductType != target.ProductType ||
                product.Family != target.Family || product.Status != ProductStatus.Current)
                throw new InvalidOperationException("Product identity, publication or shared manufacturer/type/family scope differs.");
        }
        if (await db.Products.AnyAsync(p => !ids.Contains(p.Id) && p.ManufacturerId == target.ManufacturerId && p.BrandId == target.BrandId && p.Name == plan.ProductName))
            throw new InvalidOperationException("A different canonical product already has the intended shared identity.");
        var variants = await db.ProductVariants.AsNoTracking().Where(v => ids.Contains(v.ProductId)).ToListAsync();
        if (!variants.Select(v => v.Id).Order().SequenceEqual(plan.Variants.Select(v => v.Id).Order()))
            throw new InvalidOperationException("Every existing variant must be explicitly covered.");
        foreach (var identity in plan.Variants)
        {
            var variant = variants.Single(v => v.Id == identity.Id);
            if (variant.ProductId != (reconciled ? target.Id : identity.OriginalProductId) || variant.Name != (reconciled ? identity.Name : identity.OriginalName))
                throw new InvalidOperationException("Variant identity differs from the approved plan.");
        }
        var variantIds = variants.Select(v => v.Id).ToArray();
        var packs = await (from p in db.PackTypes join s in db.SizeVariants on p.SizeVariantId equals s.Id
                           where variantIds.Contains(s.ProductVariantId) select new { p.Id, s.ProductVariantId }).ToListAsync();
        if (!packs.Select(p => p.Id).Order().SequenceEqual(plan.Packs.Select(p => p.Id).Order()))
            throw new InvalidOperationException("Every attached pack must be covered.");
        foreach (var identity in plan.Packs)
            if (!packs.Any(p => p.Id == identity.Id && p.ProductVariantId == identity.VariantId) ||
                !await db.ProductIdentifiers.AnyAsync(i => i.PackTypeId == identity.Id && i.Type == IdentifierType.Gtin && i.Value.PadLeft(14, '0') == identity.Gtin.PadLeft(14, '0')))
                throw new InvalidOperationException("Exact pack/variant/GTIN identity differs.");
        var routes = await db.ProductRouteRedirects.AsNoTracking().Where(r => ids.Contains(r.SourceProductId) || ids.Contains(r.TargetProductId)).ToListAsync();
        if (!reconciled && routes.Count != 0 || reconciled && (routes.Count != ids.Length - 1 || routes.Any(r => r.TargetProductId != target.Id ||
            !plan.Variants.Any(v => v.OriginalProductId == r.SourceProductId && v.Id == r.DefaultVariantId))))
            throw new InvalidOperationException("Historical routes conflict or would form a redirect chain.");
        // Product-specific assets/commercial proposals need an explicit scope expansion, never silently reparent them.
        var oldIds = ids.Where(id => id != target.Id).ToArray();
        if (await db.CatalogueSubmissionImages.AnyAsync(i => i.ProductId.HasValue && oldIds.Contains(i.ProductId.Value)))
            throw new InvalidOperationException("Product-specific images require individual reconciliation.");
    }

    private async Task<string> FingerprintAsync(ProductGroupingPlan? plan = null)
    {
        var rows = new List<string>();
        foreach (var type in db.Model.GetEntityTypes().Where(t => typeof(Entity).IsAssignableFrom(t.ClrType) &&
            t.ClrType != typeof(User) && !t.ClrType.Name.Contains("Passkey") && !t.ClrType.Name.Contains("Token") &&
            !t.ClrType.Name.Contains("Registration") && t.ClrType != typeof(UserEmail)).OrderBy(t => t.Name))
        {
            var method = GetType().GetMethod(nameof(ScalarRowsAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.MakeGenericMethod(type.ClrType);
            rows.AddRange(await (Task<string[]>)method.Invoke(this, [plan])!);
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", rows.Order(StringComparer.Ordinal)))));
    }

    private async Task<string[]> ScalarRowsAsync<T>(ProductGroupingPlan? plan) where T : class
    {
        var values = await db.Set<T>().AsNoTracking().ToListAsync();
        var properties = db.Model.FindEntityType(typeof(T))!.GetProperties().OrderBy(p => p.Name).ToArray();
        return values.Where(v => plan is null || !(v is CatalogueAuditRecord a && a.CorrelationId == plan.CorrelationId) &&
                !(v is ProductRouteRedirect r && plan.Products.Any(p => p.Id == r.SourceProductId)))
            .Select(v => typeof(T).Name + ":" + JsonSerializer.Serialize(properties.Where(p => plan is null ||
                !(v is Product product && product.Id == plan.TargetProductId && p.Name == nameof(Product.Name)) &&
                !(v is ProductVariant variant && plan.Variants.Any(i => i.Id == variant.Id) && p.Name is nameof(ProductVariant.Name) or nameof(ProductVariant.ProductId)))
                .ToDictionary(p => p.Name, p => p.PropertyInfo!.GetValue(v)))).ToArray();
    }
}
