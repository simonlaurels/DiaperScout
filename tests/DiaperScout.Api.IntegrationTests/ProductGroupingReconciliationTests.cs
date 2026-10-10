extern alias PopulationTool;
using System.Text.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using PopulationTool::DiaperScout.CataloguePopulation;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class ProductGroupingReconciliationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private AuthenticatedUser Administrator => new(fixture.AdministratorUserId, PostgreSqlFixture.AdministratorSubject);
    private async Task<ProductGroupingPlan> SeedAsync()
    {
        await using var db = fixture.CreateDbContext();
        var maker = new Manufacturer("Grouping maker " + Guid.NewGuid(), "group-maker-" + Guid.NewGuid());
        var brand = new Brand(maker.Id, "Grouping brand", "group-brand-" + Guid.NewGuid());
        var products = new[] { new Product(maker.Id, brand.Id, "Elastic 6 drops", "six-" + Guid.NewGuid(), ProductType.Tape, family: "Elastic"),
            new Product(maker.Id, brand.Id, "Elastic 10 drops", "ten-" + Guid.NewGuid(), ProductType.Tape, family: "Elastic") };
        var variants = products.Select(p => new ProductVariant(p.Id, "Current")).ToArray();
        var sizes = variants.Select(v => new SizeVariant(v.Id, "Medium", hipMinimumCm: 85, hipMaximumCm: 120, fitMeasurementBasis: "hip")).ToArray();
        var packs = sizes.Select((s, i) => new PackType(s.Id, i == 0 ? 30 : 14, PackagingType.Bag)).ToArray();
        var identifiers = packs.Select(p => { var d = "299" + Random.Shared.NextInt64(1_000_000_000).ToString("D9");
            var sum = d.Select((c, i) => (c - '0') * (i % 2 == 0 ? 1 : 3)).Sum();
            return new ProductIdentifier(p.Id, IdentifierType.Gtin, d + (10 - sum % 10) % 10); }).ToArray();
        db.AddRange(maker, brand); db.AddRange(products); db.AddRange(variants); db.AddRange(sizes); db.AddRange(packs); db.AddRange(identifiers);
        await db.SaveChangesAsync();
        return new("group-test-" + Guid.NewGuid(), products[0].Id, "Elastic", products.Select(p => new GroupingProduct(p.Id, p.Name, p.Slug)).ToArray(),
            variants.Select((v, i) => new GroupingVariant(v.Id, v.ProductId, v.Name, i == 0 ? "6 drops" : "10 drops")).ToArray(),
            packs.Select((p, i) => new GroupingPack(p.Id, variants[i].Id, identifiers[i].Value)).ToArray(),
            "Verified absorbency alternatives of the same manufacturer product", ["https://manufacturer.example.test/elastic"]);
    }

    [Fact]
    public async Task GroupingPreservesDescendantsHistoryAndBothPublicRoutesWithIdempotentRetry()
    {
        var plan = await SeedAsync();
        using var factory = new ObservationApiFactory(fixture);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();
        var queries = scope.ServiceProvider.GetRequiredService<IAtlasQueries>();
        var original = await DescendantsAsync(db, plan);
        var op = new ProductGroupingReconciliation(db);
        var preview = await op.PreviewAsync(plan);
        Assert.False(await db.ProductRouteRedirects.AnyAsync(r => r.SourceProductId == plan.Products[1].Id));
        var receipt = await op.ApplyAsync(Administrator, plan, preview.Fingerprint);
        Assert.True(receipt.Created);
        var retry = await op.ApplyAsync(Administrator, plan, preview.Fingerprint);
        Assert.False(retry.Created); Assert.Equal(receipt.AuditIds, retry.AuditIds);
        Assert.Equal(original, await DescendantsAsync(db, plan));
        foreach (var identity in plan.Products)
        {
            var retained = await db.Products.AsNoTracking().SingleAsync(p => p.Id == identity.Id);
            Assert.Equal(identity.Slug, retained.Slug); Assert.Equal(ProductStatus.Current, retained.Status);
        }
        var oldPage = await queries.GetProductDetailsBySlugAsync(plan.Products[1].Slug);
        Assert.NotNull(oldPage); Assert.Equal(plan.TargetProductId, oldPage.Id);
        Assert.Equal(plan.Variants[1].Id, oldPage.ProductVariantId); Assert.Contains("10 drops", oldPage.Name);
        Assert.Equal(plan.TargetProductId, (await queries.GetProductBySlugAsync(plan.Products[1].Slug))!.Id);
        var targetPage = await queries.GetProductDetailsBySlugAsync(plan.Products[0].Slug, variantId: plan.Variants[0].Id);
        Assert.NotNull(targetPage); Assert.Equal(2, targetPage.AvailableVariants!.Count);
        Assert.Null(await queries.GetProductDetailsBySlugAsync(plan.Products[1].Slug, variantId: Guid.NewGuid()));
        foreach (var pack in plan.Packs)
        {
            var found = await queries.GetProductByGtinAsync(pack.Gtin);
            Assert.NotNull(found); Assert.Equal(plan.TargetProductId, found.Product.Id);
            Assert.Equal(pack.VariantId, found.ProductVariantId); Assert.Equal(pack.Id, found.PackTypeId);
        }
        var browse = await queries.SearchProductsAsync("Elastic", 50);
        Assert.Equal(2, browse.Count(p => p.Id == plan.TargetProductId));
        Assert.DoesNotContain(browse, p => p.Id == plan.Products[1].Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => op.ApplyAsync(Administrator, plan with { Rationale = "Changed retry" }, preview.Fingerprint));
    }

    [Fact]
    public async Task ChangedScopeStalePreviewAndNonAdminCannotMutate()
    {
        var plan = await SeedAsync(); await using var db = fixture.CreateDbContext(); var op = new ProductGroupingReconciliation(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => op.PreviewAsync(plan with { Packs = [plan.Packs[0]] }));
        var preview = await op.PreviewAsync(plan);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => op.ApplyAsync(new(fixture.ModeratorUserId, PostgreSqlFixture.ModeratorSubject), plan, preview.Fingerprint));
        db.PackTypes.Add(new PackType((await db.PackTypes.SingleAsync(p => p.Id == plan.Packs[0].Id)).SizeVariantId, 60, PackagingType.Case)); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => op.ApplyAsync(Administrator, plan, preview.Fingerprint));
        Assert.False(await db.ProductRouteRedirects.AnyAsync(r => r.SourceProductId == plan.Products[1].Id));
    }

    [Fact]
    public async Task FailureAfterSaveRollsBackGroupingRoutesAndAudits()
    {
        var plan = await SeedAsync(); await using var baseline = fixture.CreateDbContext();
        await using var db = new DiaperScoutDbContext(new DbContextOptionsBuilder<DiaperScoutDbContext>().UseNpgsql(baseline.Database.GetConnectionString()).AddInterceptors(new FailAfterSave()).Options);
        var op = new ProductGroupingReconciliation(db); var preview = await op.PreviewAsync(plan);
        await Assert.ThrowsAsync<InvalidOperationException>(() => op.ApplyAsync(Administrator, plan, preview.Fingerprint));
        await using var verify = fixture.CreateDbContext();
        Assert.False(await verify.ProductRouteRedirects.AnyAsync(r => r.SourceProductId == plan.Products[1].Id));
        Assert.False(await verify.CatalogueAuditRecords.AnyAsync(a => a.CorrelationId == plan.CorrelationId));
        foreach (var identity in plan.Variants)
        { var variant = await verify.ProductVariants.SingleAsync(v => v.Id == identity.Id); Assert.Equal(identity.OriginalProductId, variant.ProductId); Assert.Equal(identity.OriginalName, variant.Name); }
        Assert.Equal(plan.Products[0].Name, (await verify.Products.SingleAsync(p => p.Id == plan.TargetProductId)).Name);
    }
    private sealed class FailAfterSave : SaveChangesInterceptor
    { public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Injected failure before commit"); }
    private static async Task<string> DescendantsAsync(DiaperScoutDbContext db, ProductGroupingPlan plan)
    {
        var ids = plan.Packs.Select(p => p.Id).ToArray(); var sizeIds = await db.PackTypes.Where(p => ids.Contains(p.Id)).Select(p => p.SizeVariantId).ToArrayAsync();
        return JsonSerializer.Serialize(new { sizes = await db.SizeVariants.AsNoTracking().Where(s => sizeIds.Contains(s.Id)).OrderBy(s => s.Id).Select(s => new {s.Id,s.ProductVariantId,s.ManufacturerSize,s.HipMinimumCm,s.HipMaximumCm}).ToListAsync(),
            packs = await db.PackTypes.AsNoTracking().Where(p => ids.Contains(p.Id)).OrderBy(p => p.Id).Select(p => new {p.Id,p.SizeVariantId,p.QuantityPerPack,p.PackagingType}).ToListAsync(),
            ids = await db.ProductIdentifiers.AsNoTracking().Where(i => ids.Contains(i.PackTypeId)).OrderBy(i => i.Id).ToListAsync() });
    }
}
