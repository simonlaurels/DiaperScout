using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class CatalogueDataQualityApiTests : IClassFixture<PostgreSqlFixture>, IDisposable
{
    private readonly PostgreSqlFixture _fixture;
    private readonly ObservationApiFactory _factory;

    public CatalogueDataQualityApiTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
        _factory = new ObservationApiFactory(fixture);
    }

    [Fact]
    public async Task DataQuality_CoversHierarchyAndGtinRulesIndependently()
    {
        await PrepareFixtureAsync();

        await using var db = _fixture.CreateDbContext();

        var noVariant = AddProduct(db, "DQ No Variant", "dq-no-variant");
        var noSize = AddProductWithVariant(db, "DQ No Size", "dq-no-size");
        var noPack = AddProductWithVariantAndSize(db, "DQ No Pack", "dq-no-pack");
        var missingGtin = AddProductWithVariantAndSizeAndPack(db, "DQ Missing GTIN", "dq-missing-gtin");
        var invalidGtin = AddProductWithVariantAndSizeAndPack(db, "DQ Invalid GTIN", "dq-invalid-gtin", "12345678");
        var warningOnly = AddProductWithVariantAndSizeAndPack(db, "DQ Warning Only", "dq-warning-only", "7000000000010");

        await db.SaveChangesAsync();

        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);
        var response = await client.GetAsync("/api/v1/catalogue-management/data-quality");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<CatalogueDataQualitySummary>();
        Assert.NotNull(summary);

        Assert.Equal(7, summary.CurrentProductCount);
        Assert.Equal(2, summary.AutomationReadyProductCount);
        Assert.Equal(5, summary.BlockingProductCount);
        Assert.Equal(6, summary.WarningProductCount);

        AssertRule(summary, "RetailMatching.NoVariant", 1, noVariant.Id);
        AssertRule(summary, "RetailMatching.NoSize", 2, noVariant.Id, noSize.Id);
        AssertRule(
			summary,
			"RetailMatching.NoPack",
			3,
			noVariant.Id,
			noSize.Id,
			noPack.Id);
        AssertRule(summary, "RetailMatching.MissingGtin", 1, missingGtin.Id);
        AssertRule(summary, "RetailMatching.InvalidGtin", 1, invalidGtin.Id);
        AssertRule(summary, "Catalogue.MissingPrimaryImage", 6,
            noVariant.Id, noSize.Id, noPack.Id, missingGtin.Id, invalidGtin.Id, warningOnly.Id);

        Assert.DoesNotContain(summary.Rules, rule => rule.Products.Any(product => product.ProductId == _fixture.ProductId));
        Assert.Contains(summary.Rules.SelectMany(rule => rule.Products), product => product.ProductId == warningOnly.Id);
    }

    [Fact]
    public async Task DataQuality_ForExplorer_ReturnsForbidden()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ExplorerSubject);

        var response = await client.GetAsync("/api/v1/catalogue-management/data-quality");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task PrepareFixtureAsync()
    {
        await using var db = _fixture.CreateDbContext();

        var existingIdentifier = await db.ProductIdentifiers
            .SingleAsync(value => value.PackTypeId == db.PackTypes
                .Where(pack => pack.SizeVariantId == db.SizeVariants
                    .Where(size => size.ProductVariantId == db.ProductVariants
                        .Where(variant => variant.ProductId == _fixture.ProductId)
                        .Select(variant => variant.Id)
                        .Single())
                    .Select(size => size.Id)
                    .Single())
                .Select(pack => pack.Id)
                .Single());

        existingIdentifier.UpdateValue("7000000000003");

        var submission = new CatalogueSubmission(
            CatalogueSubmissionSource.Moderator,
            _fixture.ModeratorUserId,
            "Integration Test Manufacturer",
            "Integration Test Product",
            "Integration Test Variant",
            "Integration Test Brand",
            "Data quality fixture image.");

        var image = new CatalogueSubmissionImage(
            submission.Id,
            CatalogueSubmissionImageRole.ProductFront,
            "integration/data-quality-primary.webp",
            "data-quality-primary.webp",
            "image/webp",
            1024,
            CatalogueImageSourceType.OfficialProductWebsite,
            "https://example.test/integration/data-quality-primary.webp",
            "Deterministic integration-test image.",
            CatalogueImagePermissionStatus.PermissionNotRequired,
            "Official product website.");

        image.PublishToProduct(_fixture.ProductId);
        image.SetPrimary(true);

        db.AddRange(submission, image);
        await db.SaveChangesAsync();
    }

    private Product AddProduct(DiaperScoutDbContext db, string name, string slug)
    {
        var product = new Product(
            _fixture.ManufacturerId,
            _fixture.BrandId,
            name,
            slug,
            ProductType.Tape);

        db.Products.Add(product);
        return product;
    }

    private Product AddProductWithVariant(DiaperScoutDbContext db, string name, string slug)
    {
        var product = AddProduct(db, name, slug);
        db.ProductVariants.Add(new ProductVariant(product.Id, "Original"));
        return product;
    }

    private Product AddProductWithVariantAndSize(DiaperScoutDbContext db, string name, string slug)
    {
        var product = AddProductWithVariant(db, name, slug);
        var variant = db.ProductVariants.Local.Single(value => value.ProductId == product.Id);
        db.SizeVariants.Add(new SizeVariant(variant.Id, "Medium"));
        return product;
    }

    private Product AddProductWithVariantAndSizeAndPack(
        DiaperScoutDbContext db,
        string name,
        string slug,
        string? gtin = null)
    {
        var product = AddProductWithVariantAndSize(db, name, slug);
        var variant = db.ProductVariants.Local.Single(value => value.ProductId == product.Id);
        var size = db.SizeVariants.Local.Single(value => value.ProductVariantId == variant.Id);
        var pack = new PackType(size.Id, 12, PackagingType.Bag);
        db.PackTypes.Add(pack);

        if (gtin is not null)
            db.ProductIdentifiers.Add(new ProductIdentifier(pack.Id, IdentifierType.Gtin, gtin));

        return product;
    }

    private HttpClient AuthenticatedClient(string subject)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Development-Subject", subject);

        if (subject == PostgreSqlFixture.ModeratorSubject)
            client.DefaultRequestHeaders.Add("X-Development-Role", "Moderator");
        else if (subject == PostgreSqlFixture.AdministratorSubject)
            client.DefaultRequestHeaders.Add("X-Development-Role", "Administrator");

        return client;
    }

    private static void AssertRule(
        CatalogueDataQualitySummary summary,
        string code,
        int expectedCount,
        params Guid[] productIds)
    {
        var rule = Assert.Single(summary.Rules, value => value.Code == code);
        Assert.Equal(expectedCount, rule.ProductCount);
        Assert.Equal(
            productIds.OrderBy(value => value),
            rule.Products.Select(value => value.ProductId).OrderBy(value => value));
    }

    public void Dispose() => _factory.Dispose();
}
