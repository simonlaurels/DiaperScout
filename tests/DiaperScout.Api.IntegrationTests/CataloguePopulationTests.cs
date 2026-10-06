extern alias PopulationTool;
using DiaperScout.Application;
using PopulationTool::DiaperScout.CataloguePopulation;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class CataloguePopulationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private PopulationPack Pack(string gtin) => new("TEST-" + Guid.NewGuid(), "READY", "Test research maker",
        "Test research brand", "Research product " + Guid.NewGuid(), [], [], null, ProductType.Tape,
        "Medium", 14, gtin, "Manufacturer hip circumference", null, null, 85, 120,
        "https://manufacturer.example.test/product", ["https://manufacturer.example.test/product"],
        ["https://manufacturer.example.test/logistics", "https://retailer.example.test/pack"],
        ["https://manufacturer.example.test/size"], "Exact Medium sealed bag of 14; no image permission; no images copied.");

    private PopulationPublisher Publisher(IServiceProvider services) => new(
        services.GetRequiredService<DiaperScoutDbContext>(), services.GetRequiredService<ICatalogueSubmissions>(),
        services.GetRequiredService<IEditorialAuthorisation>(), services.GetRequiredService<ICanonicalCatalogue>());

    [Fact]
    public async Task OriginalPackRetryAfterGroupingRetainsOriginalPublicationAuditAndSubmission()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();
        var publisher = Publisher(scope.ServiceProvider);
        var family = "Grouped elastic " + Guid.NewGuid();
        var six = Pack("2990000000316") with { Product = family + " 6 drops", Family = family };
        var ten = six with { ResearchId = six.ResearchId + "-10", Product = family + " 10 drops", Gtin = "2990000000323" };
        var originalSix = await publisher.PublishAsync(Actor, six);
        var originalTen = await publisher.PublishAsync(Actor, ten);
        var products = await db.Products.Where(p => p.Id == originalSix.ProductId || p.Id == originalTen.ProductId).ToListAsync();
        var plan = new ProductGroupingPlan("test-group-original-" + Guid.NewGuid(), originalSix.ProductId, family,
            products.Select(p => new GroupingProduct(p.Id, p.Name, p.Slug)).ToArray(),
            [new(originalSix.VariantId, originalSix.ProductId, "Current", "6 drops"), new(originalTen.VariantId, originalTen.ProductId, "Current", "10 drops")],
            [new(originalSix.PackId, originalSix.VariantId, six.Gtin), new(originalTen.PackId, originalTen.VariantId, ten.Gtin)],
            "Reviewed grouping", six.IdentitySources);
        var op = new ProductGroupingReconciliation(db); var preview = await op.PreviewAsync(plan);
        await op.ApplyAsync(new(fixture.AdministratorUserId, PostgreSqlFixture.AdministratorSubject), plan, preview.Fingerprint);
        var retry = await publisher.PublishAsync(Actor, ten with { Product = family, VariantName = "10 drops" });
        Assert.False(retry.Created); Assert.Equal(originalSix.ProductId, retry.ProductId);
        Assert.Equal(originalTen.VariantId, retry.VariantId); Assert.Equal(originalTen.SizeId, retry.SizeId);
        Assert.Equal(originalTen.PackId, retry.PackId); Assert.Equal(originalTen.IdentifierId, retry.IdentifierId);
        Assert.Equal(originalTen.AuditId, retry.AuditId); Assert.Equal(originalTen.SubmissionId, retry.SubmissionId);
        Assert.Equal(ten.Product, (await db.CatalogueSubmissions.SingleAsync(s => s.Id == originalTen.SubmissionId)).ProposedProductName);
    }

    [Fact]
    public async Task ReviewedAbsorbencyVariantsShareProductButKeepIndependentSizesPacksAndRetries()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var scope = factory.Services.CreateScope();
        var publisher = Publisher(scope.ServiceProvider);
        var six = Pack("2990000000088") with { VariantName = "6 drops", Quantity = 30 };
        var original = await publisher.PublishAsync(Actor, six);
        var ten = six with { ResearchId = six.ResearchId + "-10", VariantName = "10 drops", Gtin = "2990000000095", Quantity = 14 };
        var added = await publisher.PublishAsync(Actor, ten);
        Assert.Equal(original.ProductId, added.ProductId);
        Assert.NotEqual(original.VariantId, added.VariantId);
        Assert.NotEqual(original.SizeId, added.SizeId);
        Assert.NotEqual(original.PackId, added.PackId);
        var retry = await publisher.PublishAsync(Actor, ten);
        Assert.False(retry.Created); Assert.Equal(added.VariantId, retry.VariantId); Assert.Equal(added.AuditId, retry.AuditId);
        await using var db = fixture.CreateDbContext();
        Assert.Equal(2, await db.ProductVariants.CountAsync(v => v.ProductId == original.ProductId));
        Assert.Equal("6 drops", (await db.ProductVariants.SingleAsync(v => v.Id == original.VariantId)).Name);
        Assert.Equal(30, (await db.PackTypes.SingleAsync(p => p.Id == original.PackId)).QuantityPerPack);
        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAsync(Actor, ten with { VariantName = "6 drops" }));
        var large = ten with { ResearchId = ten.ResearchId + "-L", Gtin = "2990000000101", Size = "Large", HipMinimumCm = 115, HipMaximumCm = 145 };
        var sibling = await publisher.PublishAsync(Actor, large);
        Assert.Equal(added.VariantId, sibling.VariantId);
        Assert.False((await publisher.PublishAsync(Actor, large)).Created);
    }

    [Fact]
    public async Task SiblingAfterManufacturerReconciliationUsesHistoricalProductLinkWithoutOrganisationAlias()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();
        var publisher = Publisher(scope.ServiceProvider);
        var medium = Pack("2990000000064") with { Manufacturer = "Original research producer " + Guid.NewGuid(),
            Brand = "Reconciled brand " + Guid.NewGuid() };
        var original = await publisher.PublishAsync(Actor, medium);
        var product = await db.Products.SingleAsync(p => p.Id == original.ProductId);
        var maker = await db.Manufacturers.SingleAsync(m => m.Id == product.ManufacturerId);
        var legalName = "Legal research maker " + Guid.NewGuid();
        var plan = new ManufacturerReconciliationPlan("test-maker-link-" + Guid.NewGuid(), maker.Id, maker.Name,
            product.BrandId!.Value, Guid.NewGuid(), legalName, "legal-" + Guid.NewGuid(),
            "https://manufacturer.example.test/declaration.pdf", new string('A', 64), "TEST-MF-1",
            "Separate production organisation retained in provenance", ["https://manufacturer.example.test/production"],
            ["1000021343"], [new(product.Id, original.PackId, medium.Gtin, "1000021343", 5)]);
        var reconciliation = new ManufacturerReconciliation(db);
        var preview = await reconciliation.PreviewAsync(plan);
        await reconciliation.ApplyAsync(new(fixture.AdministratorUserId, PostgreSqlFixture.AdministratorSubject), plan, preview.Fingerprint);
        var large = medium with { ResearchId = medium.ResearchId + "-L", Manufacturer = legalName,
            Gtin = "2990000000071", Size = "Large", HipMinimumCm = 115, HipMaximumCm = 145 };
        var added = await publisher.PublishAsync(Actor, large);
        Assert.True(added.Created);
        Assert.Equal(original.ProductId, added.ProductId);
        Assert.Equal(original.SubmissionId, added.SubmissionId);
        Assert.Equal(medium.Manufacturer, (await db.CatalogueSubmissions.SingleAsync(s => s.Id == original.SubmissionId)).ProposedManufacturerName);
        Assert.Equal(maker.Name, (await db.Manufacturers.SingleAsync(m => m.Id == maker.Id)).Name);
        Assert.False((await publisher.PublishAsync(Actor, large)).Created);
    }

    [Fact]
    public async Task SiblingSizeEnrichesExistingProductWithAuditAndIdempotentRetry()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var scope = factory.Services.CreateScope();
        var publisher = Publisher(scope.ServiceProvider);
        var medium = Pack("4052199297033");
        var original = await publisher.PublishAsync(Actor, medium);
        var large = medium with { ResearchId = medium.ResearchId + "-L", Size = "Large", Gtin = "4052199299433", HipMinimumCm = 115, HipMaximumCm = 145 };
        var added = await publisher.PublishAsync(Actor, large);
        Assert.True(added.Created);
        Assert.Equal(original.ProductId, added.ProductId);
        Assert.Equal(original.VariantId, added.VariantId);
        Assert.Equal(original.SubmissionId, added.SubmissionId);
        Assert.NotEqual(original.SizeId, added.SizeId);
        Assert.NotEqual(original.PackId, added.PackId);
        Assert.NotEqual(original.AuditId, added.AuditId);
        var retry = await publisher.PublishAsync(Actor, large with { Gtin = "04052199299433" });
        Assert.False(retry.Created);
        Assert.Equal(added.SizeId, retry.SizeId);
        Assert.Equal(added.AuditId, retry.AuditId);
        await using var db = fixture.CreateDbContext();
        Assert.Equal(2, await db.SizeVariants.CountAsync(s => s.ProductVariantId == original.VariantId));
        Assert.Equal(1, await db.CatalogueSubmissions.CountAsync(s => s.PublishedProductId == original.ProductId));
        var audit = await db.CatalogueAuditRecords.SingleAsync(a => a.Id == added.AuditId);
        Assert.Equal(CatalogueAuditAction.ProductChanged, audit.Action);
        Assert.Contains("retailer.example.test/pack", audit.SourceReferencesJson);
        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAsync(Actor,
            large with { ResearchId = large.ResearchId + "-conflict", Gtin = "4052199296975", HipMaximumCm = 150 }));
        Assert.Equal(2, await db.SizeVariants.CountAsync(s => s.ProductVariantId == original.VariantId));
        Assert.Equal(120, (await db.SizeVariants.SingleAsync(s => s.Id == original.SizeId)).HipMaximumCm);
    }

    [Fact]
    public async Task AlternateBagAndCasePreserveSharedSizeAndEveryExistingIdentifier()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var scope = factory.Services.CreateScope();
        var publisher = Publisher(scope.ServiceProvider);
        var bag = Pack("2990000000019");
        var original = await publisher.PublishAsync(Actor, bag);
        var alternate = bag with { ResearchId = bag.ResearchId + "-alternate", Quantity = 28, Gtin = "2990000000026" };
        var carton = bag with { ResearchId = bag.ResearchId + "-case", Quantity = 56, Gtin = "2990000000033", PackagingType = PackagingType.Case };
        var addedBag = await publisher.PublishAsync(Actor, alternate);
        var addedCase = await publisher.PublishAsync(Actor, carton);
        Assert.Equal(original.SizeId, addedBag.SizeId);
        Assert.Equal(original.SizeId, addedCase.SizeId);
        Assert.Equal(original.ProductId, addedCase.ProductId);
        Assert.Equal(original.VariantId, addedCase.VariantId);
        Assert.Equal(original.SubmissionId, addedCase.SubmissionId);
        Assert.NotEqual(original.PackId, addedCase.PackId);
        var retry = await publisher.PublishAsync(Actor, carton with { Gtin = "02990000000033" });
        Assert.False(retry.Created);
        Assert.Equal(addedCase.PackId, retry.PackId);
        Assert.Equal(addedCase.AuditId, retry.AuditId);
        await using var db = fixture.CreateDbContext();
        Assert.Equal(1, await db.SizeVariants.CountAsync(s => s.ProductVariantId == original.VariantId));
        var originalPack = await db.PackTypes.SingleAsync(p => p.Id == original.PackId);
        Assert.Equal(14, originalPack.QuantityPerPack);
        Assert.Equal(PackagingType.Bag, originalPack.PackagingType);
        Assert.Equal(bag.Gtin, (await db.ProductIdentifiers.SingleAsync(i => i.Id == original.IdentifierId)).Value);
        var casePack = await db.PackTypes.SingleAsync(p => p.Id == addedCase.PackId);
        Assert.Equal(56, casePack.QuantityPerPack);
        Assert.Equal(PackagingType.Case, casePack.PackagingType);
        Assert.Null(casePack.CaseQuantity);
        Assert.Equal(3, await db.PackTypes.CountAsync(p => p.SizeVariantId == original.SizeId));
        var audit = await db.CatalogueAuditRecords.SingleAsync(a => a.Id == addedCase.AuditId);
        Assert.Equal(CatalogueAuditAction.ProductChanged, audit.Action);
        Assert.Contains(addedCase.PackId.ToString(), audit.AffectedCanonicalIdsJson);
        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAsync(Actor,
            carton with { HipMaximumCm = 130 }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAsync(Actor,
            alternate with { ResearchId = alternate.ResearchId + "-revision", Gtin = "2990000000040" }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAsync(Actor,
            carton with { Product = "Unknown carton product", Gtin = "2990000000057" }));
        Assert.Equal(3, await db.PackTypes.CountAsync(p => p.SizeVariantId == original.SizeId));
    }

    [Fact]
    public async Task AlternatePackRejectsUnauthorisedInvalidAndDraftReservedGtinsWithoutMutation()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var scope = factory.Services.CreateScope();
        var publisher = Publisher(scope.ServiceProvider);
        var original = await publisher.PublishAsync(Actor, Pack("2990000000118"));
        var catalogue = scope.ServiceProvider.GetRequiredService<ICanonicalCatalogue>();
        var command = new CreateCanonicalProductPackManagement(56, PackagingType.Case, "2990000000125",
            "Verified exact test carton", ["https://manufacturer.example.test/carton"], "Preserve existing bag", "TEST-add-pack-safety");
        var explorer = new AuthenticatedUser(fixture.ExplorerUserId, PostgreSqlFixture.ExplorerSubject);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => catalogue.AddProductPackAsync(explorer, original.ProductId, original.VariantId, original.SizeId, command));
        await Assert.ThrowsAsync<CatalogueValidationException>(() => catalogue.AddProductPackAsync(Actor, original.ProductId, original.VariantId, original.SizeId, command with { QuantityPerPack = 0 }));
        await Assert.ThrowsAsync<CatalogueValidationException>(() => catalogue.AddProductPackAsync(Actor, original.ProductId, original.VariantId, original.SizeId, command with { Gtin = "2990000000126" }));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => catalogue.AddProductPackAsync(Actor, original.ProductId, Guid.NewGuid(), original.SizeId, command));
        await using (var setup = fixture.CreateDbContext())
        {
            var draft = new CatalogueSubmission(CatalogueSubmissionSource.Moderator, fixture.ModeratorUserId,
                "Reserved test maker", "Reserved test product", "Current", "Reserved test brand", "Unpublished barcode reservation");
            draft.UpdateIdentity("02990000000125", null, "https://manufacturer.example.test/carton");
            setup.Add(draft);
            await setup.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<CatalogueValidationException>(() => catalogue.AddProductPackAsync(Actor, original.ProductId, original.VariantId, original.SizeId, command));
        await using var db = fixture.CreateDbContext();
        Assert.Equal(1, await db.PackTypes.CountAsync(p => p.SizeVariantId == original.SizeId));
        Assert.Equal(1, await db.ProductIdentifiers.CountAsync(i => i.PackTypeId == original.PackId));
        Assert.False(await db.CatalogueAuditRecords.AnyAsync(a => a.CorrelationId == command.CorrelationId));
    }

    private AuthenticatedUser Actor => new(fixture.ModeratorUserId, PostgreSqlFixture.ModeratorSubject);

    [Fact]
    public async Task PadDimensionsRemainDistinctFromBodyFitThroughPublicationAndSiblingRetry()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var scope = factory.Services.CreateScope();
        var publisher = Publisher(scope.ServiceProvider);
        var pad = Pack("5713571000441") with
        {
            ProductType = ProductType.Pad, Family = "San Premium", Size = "1", Quantity = 30,
            MeasurementBasis = "manufacturer pad size; waist/hip not applicable",
            HipMinimumCm = null, HipMaximumCm = null, LengthMm = 220, WidthMm = 100
        };
        var original = await publisher.PublishAsync(Actor, pad);
        var sibling = pad with { ResearchId = pad.ResearchId + "-1A", Size = "1A", Quantity = 28,
            Gtin = "5713571000465", LengthMm = 280 };
        var added = await publisher.PublishAsync(Actor, sibling);
        Assert.Equal(original.ProductId, added.ProductId);
        Assert.NotEqual(original.SizeId, added.SizeId);
        var retry = await publisher.PublishAsync(Actor, sibling with { Gtin = "05713571000465" });
        Assert.False(retry.Created);
        Assert.Equal(added.PackId, retry.PackId);
        await using var db = fixture.CreateDbContext();
        var size = await db.SizeVariants.SingleAsync(s => s.Id == added.SizeId);
        Assert.Equal(280, size.LengthMm);
        Assert.Equal(100, size.WidthMm);
        Assert.Null(size.WaistMinimumCm);
        Assert.Null(size.HipMinimumCm);
        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAsync(Actor,
            sibling with { LengthMm = 330 }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAsync(Actor,
            sibling with { ProductType = ProductType.Booster }));
        Assert.Throws<ArgumentException>(() => PopulationPublisher.Validate(pad with { WidthMm = 0 }));
        Assert.Equal(2, await db.SizeVariants.CountAsync(s => s.ProductVariantId == original.VariantId));
    }

    [Fact]
    public async Task PublicationUsesVerifiedEditorialPipelineAndPaddedRetryIsIdempotent()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var scope = factory.Services.CreateScope();
        var pack = Pack("4052199303413");
        var publisher = Publisher(scope.ServiceProvider);
        var first = await publisher.PublishAsync(Actor, pack);
        Assert.True(first.Created);
        var retry = await publisher.PublishAsync(Actor, pack with { Gtin = "04052199303413" });
        Assert.False(retry.Created);
        Assert.Equal(first.ProductId, retry.ProductId);
        Assert.Equal(first.PackId, retry.PackId);
        await using var db = fixture.CreateDbContext();
        Assert.Equal(CatalogueSubmissionStatus.Published,
            (await db.CatalogueSubmissions.SingleAsync(s => s.Id == first.SubmissionId)).Status);
        Assert.Equal(2, await db.CatalogueSubmissionVerifications.CountAsync(v => v.SubmissionId == first.SubmissionId));
        Assert.False(await db.CatalogueSubmissionImages.AnyAsync(i => i.SubmissionId == first.SubmissionId));
        Assert.Equal(1, await db.CatalogueAuditRecords.CountAsync(a => a.ProductId == first.ProductId && a.Action == CatalogueAuditAction.ProductCreated));
        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAsync(Actor, pack with { Quantity = 30 }));
    }

    [Fact]
    public async Task ExistingEmptyDraftIsEnrichedAndPriorNotesPreserved()
    {
        var pack = Pack("7332152207383");
        var draft = new CatalogueSubmission(CatalogueSubmissionSource.Moderator, fixture.ModeratorUserId,
            pack.Manufacturer, pack.Product, "Current", pack.Brand, "Entered notes to preserve");
        await using (var db = fixture.CreateDbContext())
        {
            db.AddRange(draft, new CatalogueSubmissionVariant(draft.Id, "Current"));
            await db.SaveChangesAsync();
        }
        using var factory = new ObservationApiFactory(fixture);
        using var scope = factory.Services.CreateScope();
        var result = await Publisher(scope.ServiceProvider).PublishAsync(Actor, pack with { ExistingSubmissionId = draft.Id });
        Assert.Equal(draft.Id, result.SubmissionId);
        await using var verify = fixture.CreateDbContext();
        Assert.Equal(1, await verify.CatalogueSubmissions.CountAsync(s => s.ProposedProductName == pack.Product));
        Assert.Contains("Entered notes to preserve", (await verify.CatalogueSubmissions.SingleAsync(s => s.Id == draft.Id)).Notes);
    }

    [Fact]
    public async Task ReviewedAttributesPreserveOriginalClaimsWithoutPublishingUnsupportedValues()
    {
        var pack = Pack("4052199296975");
        var draft = new CatalogueSubmission(CatalogueSubmissionSource.Moderator, fixture.ModeratorUserId,
            pack.Manufacturer, pack.Product, "Current", pack.Brand, "Original entered draft");
        var variant = new CatalogueSubmissionVariant(draft.Id, "Current");
        var attributes = new CatalogueSubmissionVariantOverride(variant.Id);
        attributes.Set(CatalogueVariantOverrideAttribute.WaistbandStyle, "AllAroundElastic");
        attributes.Set(CatalogueVariantOverrideAttribute.LatexFree, "true");
        await using (var db = fixture.CreateDbContext())
        {
            db.AddRange(draft, variant, attributes);
            await db.SaveChangesAsync();
        }
        using var factory = new ObservationApiFactory(fixture);
        using var scope = factory.Services.CreateScope();
        var result = await Publisher(scope.ServiceProvider).PublishAsync(Actor, pack with
        {
            ExistingSubmissionId = draft.Id, ReconcileDraftAttributes = true,
            VerifiedVariantAttributes = new() { [CatalogueVariantOverrideAttribute.WetnessIndicator] = "true" }
        });
        await using var verify = fixture.CreateDbContext();
        var journal = (await verify.CatalogueSubmissions.SingleAsync(s => s.Id == draft.Id)).Notes;
        Assert.Contains("Preserved prior variant attributes", journal);
        Assert.Contains("\"IsLatexFree\":true", journal);
        var reviewed = await verify.CatalogueSubmissionVariantOverrides.SingleAsync(v => v.VariantId == variant.Id);
        Assert.Null(reviewed.IsLatexFree);
        Assert.Null(reviewed.WaistbandStyle);
        Assert.True(reviewed.HasWetnessIndicator);
        Assert.True(result.Created);
    }

    [Fact]
    public async Task ExistingSizeHoldsImportAndRollsBackProposalChanges()
    {
        var pack = Pack("4052199297002");
        var draft = new CatalogueSubmission(CatalogueSubmissionSource.Moderator, fixture.ModeratorUserId,
            pack.Manufacturer, pack.Product, "Current", pack.Brand, "Keep original notes");
        var variant = new CatalogueSubmissionVariant(draft.Id, "Current");
        await using (var db = fixture.CreateDbContext())
        {
            db.AddRange(draft, variant);
            await db.SaveChangesAsync();
        }
        using var factory = new ObservationApiFactory(fixture);
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ICatalogueSubmissions>().AddSizeVariantAsync(Actor,
            draft.Id, variant.Id, new("Large", 100, 140, null, null, null, null, null, null, null, null, null, 20, null));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Publisher(scope.ServiceProvider).PublishAsync(Actor,
            pack with { ExistingSubmissionId = draft.Id }));
        await using var verify = fixture.CreateDbContext();
        var unchanged = await verify.CatalogueSubmissions.SingleAsync(s => s.Id == draft.Id);
        Assert.Equal("Keep original notes", unchanged.Notes);
        Assert.Equal(CatalogueSubmissionStatus.Draft, unchanged.Status);
        Assert.Equal("Large", (await verify.CatalogueSubmissionSizeVariants.SingleAsync(s => s.VariantId == variant.Id)).ManufacturerSize);
        Assert.False(await verify.Products.AnyAsync(p => p.Name == pack.Product));
    }

    [Fact]
    public async Task HeldResearchAndDiscoveryEvidenceCannotCreateRecords()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var scope = factory.Services.CreateScope();
        var publisher = Publisher(scope.ServiceProvider);
        var pack = Pack("5900516695750");
        await Assert.ThrowsAsync<ArgumentException>(() => publisher.PublishAsync(Actor, pack with { Outcome = "CONFLICT" }));
        await Assert.ThrowsAsync<ArgumentException>(() => publisher.PublishAsync(Actor, pack with { Gtin = "5900516695751" }));
        await Assert.ThrowsAsync<ArgumentException>(() => publisher.PublishAsync(Actor, pack with { IdentifierSources = ["https://diapstash.com/catalogue"] }));
        await using var db = fixture.CreateDbContext();
        Assert.False(await db.CatalogueSubmissions.AnyAsync(s => s.ProposedProductName == pack.Product));
    }
}
