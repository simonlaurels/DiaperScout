extern alias PopulationTool;
using System.Text.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using PopulationTool::DiaperScout.CataloguePopulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class ManufacturerReconciliationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private AuthenticatedUser Administrator => new(fixture.AdministratorUserId, PostgreSqlFixture.AdministratorSubject);

    private async Task<ManufacturerReconciliationPlan> SeedAsync()
    {
        await using var db = fixture.CreateDbContext();
        var maker = new Manufacturer("Separate producer " + Guid.NewGuid(), "producer-" + Guid.NewGuid());
        var brand = new Brand(maker.Id, "Test brand", "brand-" + Guid.NewGuid());
        var product = new Product(maker.Id, brand.Id, "Identity preserved", "preserved-" + Guid.NewGuid(), ProductType.Pad,
            description: "Original source description");
        var variant = new ProductVariant(product.Id, "Current");
        var size = new SizeVariant(variant.Id, "Normal", fitMeasurementBasis: "Product dimensions");
        var pack = new PackType(size.Id, 34, PackagingType.Bag);
        var digits = "299" + Random.Shared.NextInt64(1_000_000_000).ToString("D9");
        var sum = digits.Select((c, i) => (c - '0') * (i % 2 == 0 ? 1 : 3)).Sum();
        var identifier = new ProductIdentifier(pack.Id, IdentifierType.Gtin, digits + ((10 - sum % 10) % 10));
        db.AddRange(maker, brand, product, variant, size, pack, identifier);
        await db.SaveChangesAsync();
        return new("test-reconciliation-" + Guid.NewGuid(), maker.Id, maker.Name, brand.Id, Guid.NewGuid(),
            "Separate legal maker " + Guid.NewGuid(), "legal-" + Guid.NewGuid(), "https://manufacturer.example.test/declaration.pdf",
            new string('A', 64), "TEST-MF-1", "Original producer remains separate; supported production provenance retained.",
            ["https://manufacturer.example.test/factory"], ["1000021343"],
            [new(product.Id, pack.Id, identifier.Value, "1000021343", 5)]);
    }

    [Fact]
    public async Task PreviewIsReadOnlyAndApplyPreservesIdentityThenRetriesWithoutDuplicateAudits()
    {
        var plan = await SeedAsync();
        await using var db = fixture.CreateDbContext();
        var product = await db.Products.AsNoTracking().SingleAsync(p => p.Id == plan.Coverage[0].ProductId);
        var originalSlug = product.Slug;
        var variant = await db.ProductVariants.AsNoTracking().SingleAsync(v => v.ProductId == product.Id);
        var size = await db.SizeVariants.AsNoTracking().SingleAsync(s => s.ProductVariantId == variant.Id);
        var pack = await db.PackTypes.AsNoTracking().SingleAsync(p => p.Id == plan.Coverage[0].PackId);
        var identifier = await db.ProductIdentifiers.AsNoTracking().SingleAsync(i => i.PackTypeId == pack.Id);
        var original = JsonSerializer.Serialize(new { variant, size, pack, identifier });
        var operation = new ManufacturerReconciliation(db);
        var preview = await operation.PreviewAsync(plan);
        Assert.False(await db.Manufacturers.AnyAsync(m => m.Id == plan.LegalManufacturerId));
        Assert.Equal(plan.PreviousManufacturerId, (await db.Products.AsNoTracking().SingleAsync(p => p.Id == product.Id)).ManufacturerId);
        var result = await operation.ApplyAsync(Administrator, plan, preview.Fingerprint);
        Assert.True(result.Created);
        var retry = await operation.ApplyAsync(Administrator, plan, preview.Fingerprint);
        Assert.False(retry.Created);
        Assert.Equal(result.AuditIds, retry.AuditIds);
        db.ChangeTracker.Clear();
        var after = await db.Products.AsNoTracking().SingleAsync(p => p.Id == product.Id);
        Assert.Equal(plan.LegalManufacturerId, after.ManufacturerId);
        Assert.Equal(originalSlug, after.Slug);
        Assert.Equal(product.Description, after.Description);
        Assert.Equal(product.Status, after.Status);
        Assert.Equal(original, JsonSerializer.Serialize(new {
            variant = await db.ProductVariants.AsNoTracking().SingleAsync(v => v.Id == variant.Id),
            size = await db.SizeVariants.AsNoTracking().SingleAsync(s => s.Id == size.Id),
            pack = await db.PackTypes.AsNoTracking().SingleAsync(p => p.Id == pack.Id),
            identifier = await db.ProductIdentifiers.AsNoTracking().SingleAsync(i => i.Id == identifier.Id) }));
        Assert.Equal(plan.PreviousManufacturerName, (await db.Manufacturers.SingleAsync(m => m.Id == plan.PreviousManufacturerId)).Name);
        Assert.Equal(plan.LegalManufacturerId, (await db.Brands.SingleAsync(b => b.Id == plan.BrandId)).ManufacturerId);
        Assert.Single(await db.CatalogueAuditRecords.Where(a => a.CorrelationId == plan.CorrelationId).ToListAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => operation.ApplyAsync(Administrator,
            plan with { ProductionProvenance = "Altered retry" }, preview.Fingerprint));
    }

    [Fact]
    public async Task StalePreviewAndUnauthorisedActorCannotMutate()
    {
        var plan = await SeedAsync();
        await using var db = fixture.CreateDbContext();
        var operation = new ManufacturerReconciliation(db);
        var preview = await operation.PreviewAsync(plan);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => operation.ApplyAsync(
            new(fixture.ModeratorUserId, PostgreSqlFixture.ModeratorSubject), plan, preview.Fingerprint));
        var product = await db.Products.SingleAsync(p => p.Id == plan.Coverage[0].ProductId);
        product.SetStatus(ProductStatus.Discontinued);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => operation.ApplyAsync(Administrator, plan, preview.Fingerprint));
        Assert.False(await db.Manufacturers.AnyAsync(m => m.Id == plan.LegalManufacturerId));
        Assert.Equal(plan.PreviousManufacturerId, product.ManufacturerId);
        Assert.False(await db.CatalogueAuditRecords.AnyAsync(a => a.CorrelationId == plan.CorrelationId));
    }

    [Fact]
    public async Task FailureAfterDatabaseSaveRollsBackOrganisationRelationshipsAndAudits()
    {
        var plan = await SeedAsync();
        await using var connectionContext = fixture.CreateDbContext();
        await using var db = new DiaperScoutDbContext(new DbContextOptionsBuilder<DiaperScoutDbContext>()
            .UseNpgsql(connectionContext.Database.GetConnectionString()).AddInterceptors(new FailAfterSave()).Options);
        var operation = new ManufacturerReconciliation(db);
        var preview = await operation.PreviewAsync(plan);
        await Assert.ThrowsAsync<InvalidOperationException>(() => operation.ApplyAsync(Administrator, plan, preview.Fingerprint));
        await using var verify = fixture.CreateDbContext();
        Assert.False(await verify.Manufacturers.AnyAsync(m => m.Id == plan.LegalManufacturerId));
        Assert.Equal(plan.PreviousManufacturerId, (await verify.Brands.SingleAsync(b => b.Id == plan.BrandId)).ManufacturerId);
        Assert.Equal(plan.PreviousManufacturerId, (await verify.Products.SingleAsync(p => p.Id == plan.Coverage[0].ProductId)).ManufacturerId);
        Assert.False(await verify.CatalogueAuditRecords.AnyAsync(a => a.CorrelationId == plan.CorrelationId));
    }

    private sealed class FailAfterSave : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default) => throw new InvalidOperationException("Injected failure after save before commit.");
    }

    [Fact]
    public async Task UncoveredArticlesAndAttachedPacksRejectWholeScope()
    {
        var plan = await SeedAsync();
        await using var db = fixture.CreateDbContext();
        var operation = new ManufacturerReconciliation(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => operation.PreviewAsync(plan with { DeclaredArticles = [] }));
        var sizeId = (await db.PackTypes.SingleAsync(p => p.Id == plan.Coverage[0].PackId)).SizeVariantId;
        db.PackTypes.Add(new PackType(sizeId, 68, PackagingType.Case));
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => operation.PreviewAsync(plan));
        Assert.False(await db.Manufacturers.AnyAsync(m => m.Id == plan.LegalManufacturerId));
    }
}
