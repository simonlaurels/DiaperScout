using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class PublicVariantCatalogueApiTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    internal static async Task<CanonicalProductReceipt> CreateProductAsync(HttpClient client, PostgreSqlFixture fixture, string variantName = "Black")
    {
        var slug = $"variant-scope-{Guid.NewGuid():N}";
        var request = new CreateCanonicalProductRequest(fixture.ManufacturerId, fixture.BrandId, "MEGAMAX", slug,
            ProductType.Tape, ProductStatus.Current, variantName, BackingType.Plastic, "Medium", 80, 100, 10,
            PackagingType.Bag, null, "Manufacturer product sheet", ["https://example.test/specification"], "Verified test catalogue");
        var response = await client.PostAsJsonAsync("/api/v1/products", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CanonicalProductReceipt>())!;
    }

    internal static async Task AddVariantAsync(HttpClient client, Guid productId, string name)
    {
        var request = new CreateCanonicalProductVariantManagement(name, BackingType.Cloth, FastenerType.Unknown,
            CatalogueVariantAppearance.Unknown, CatalogueVariantColour.Unknown, null, null, WaistbandStyle.Unknown,
            FragranceType.Unknown, null, CatalogueVariantDesignedFor.Unknown, null, null,
            "Manufacturer sheet", ["https://example.test/specification"], "Verified variant", null);
        var response = await client.PostAsJsonAsync($"/api/v1/catalogue-management/products/{productId}/variants", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    internal static async Task AddSizeAsync(HttpClient client, Guid productId, Guid variantId, string name, int quantity)
    {
        var request = new CreateCanonicalProductSizeManagement(name, null, null, null, null, null, null, null, null,
            null, null, null, quantity, PackagingType.Bag, null,
            "Manufacturer sheet", ["https://example.test/specification"], "Verified size", null);
        var response = await client.PostAsJsonAsync($"/api/v1/catalogue-management/products/{productId}/variants/{variantId}/sizes", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    internal static async Task<CatalogueProductManagementDetails> ManagedAsync(HttpClient client, Guid productId) =>
        (await client.GetFromJsonAsync<CatalogueProductManagementDetails>($"/api/v1/catalogue-management/products/{productId}"))!;

    [Fact]
    public async Task VariantResults_CountPageFilterAndDeepLinkWithoutDuplicatingProduct()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var client = RetailListingApiTests.Moderator(factory);
        var receipt = await CreateProductAsync(client, fixture);
        await AddVariantAsync(client, receipt.ProductId, "Pink");
        var managed = await ManagedAsync(client, receipt.ProductId);
        var pink = managed.Variants.Single(v => v.Name == "Pink");
        await AddSizeAsync(client, receipt.ProductId, pink.Id, "Medium", 20);
        await AddSizeAsync(client, receipt.ProductId, receipt.ProductVariantId, "Large", 15);
        var search = (await client.GetFromJsonAsync<CatalogueProductSearch>($"/api/v1/products?q={managed.Slug}&limit=1"))!;
        Assert.Equal(2, search.TotalCount);
        var black = Assert.Single(search.Products);
        Assert.Equal(receipt.ProductId, black.Id);
        Assert.Equal(receipt.ProductVariantId, black.ProductVariantId);
        Assert.Equal("Integration Test Brand MEGAMAX Black", black.Name);
        Assert.Equal(new[] { "Large", "Medium" }, black.Sizes);
        var second = (await client.GetFromJsonAsync<CatalogueProductSearch>($"/api/v1/products?q={managed.Slug}&limit=1&offset=1"))!;
        Assert.Equal(2, second.TotalCount);
        Assert.Equal(pink.Id, Assert.Single(second.Products).ProductVariantId);
        Assert.Equal(receipt.ProductId, second.Products[0].Id);
        Assert.All(search.Facets.Single(f => f.Key == "brand").Options, o => Assert.Equal(2, o.Count));
        var filtered = (await client.GetFromJsonAsync<CatalogueProductSearch>($"/api/v1/products?q={managed.Slug}&size=Large"))!;
        Assert.Equal(1, filtered.TotalCount);
        Assert.Equal(receipt.ProductVariantId, Assert.Single(filtered.Products).ProductVariantId);
        var backing = (await client.GetFromJsonAsync<CatalogueProductSearch>($"/api/v1/products?q={managed.Slug}&backing=Cloth"))!;
        Assert.Equal(pink.Id, Assert.Single(backing.Products).ProductVariantId);
        var details = (await client.GetFromJsonAsync<CatalogueProductDetails>($"/api/v1/products/{managed.Slug}?variantId={pink.Id}"))!;
        Assert.Equal(pink.Id, details.ProductVariantId);
        Assert.Equal("Integration Test Brand MEGAMAX Pink", details.Name);
        var medium = Assert.Single(Assert.Single(details.Variants).Sizes);
        Assert.NotEqual(receipt.SizeVariantId, medium.Id);
        Assert.Equal(20, Assert.Single(medium.Packs).QuantityPerPack);
        Assert.Equal(2, details.AvailableVariants!.Count);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/products/{managed.Slug}?variantId={Guid.NewGuid()}" )).StatusCode);
        await using var db = fixture.CreateDbContext();
        Assert.Equal("MEGAMAX", (await db.Products.SingleAsync(p => p.Id == receipt.ProductId)).Name);
        Assert.Equal(1, await db.Products.CountAsync(p => p.Slug == managed.Slug));
        var hide = await client.PutAsJsonAsync($"/api/v1/catalogue-management/products/{receipt.ProductId}/status",
            new { status = ProductStatus.Discontinued, sourceSummary = "Manufacturer sheet", sourceReferences = new[] { "https://example.test/specification" }, editorialRationale = "Withdrawn product" });
        Assert.Equal(HttpStatusCode.NoContent, hide.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<CatalogueProductSearch>($"/api/v1/products?q={managed.Slug}"))!.Products);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/products/{managed.Slug}?variantId={pink.Id}" )).StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Default")]
    [InlineData("Current")]
    [InlineData("Single version")]
    public async Task StructuralVariant_HasOneSensiblePublicIdentity(string variantName)
    {
        using var factory = new ObservationApiFactory(fixture);
        using var client = RetailListingApiTests.Moderator(factory);
        // The management path accepts structural names; creation requires a nonempty variant name.
        var receipt = await CreateProductAsync(client, fixture, string.IsNullOrEmpty(variantName) ? "Default" : variantName);
        var managed = await ManagedAsync(client, receipt.ProductId);
        var search = (await client.GetFromJsonAsync<CatalogueProductSearch>($"/api/v1/products?q={managed.Slug}"))!;
        Assert.Equal(1, search.TotalCount);
        Assert.Equal("Integration Test Brand MEGAMAX", Assert.Single(search.Products).Name);
        var details = (await client.GetFromJsonAsync<CatalogueProductDetails>($"/api/v1/products/{managed.Slug}"))!;
        Assert.Equal("Integration Test Brand MEGAMAX", details.Name);
        Assert.Equal(string.Empty, Assert.Single(details.Variants).Name);
        Assert.DoesNotContain("Manufacturer", details.Name);
    }

    [Fact]
    public async Task ExactPackOffers_ExcludeOtherSizeVariantPackAndUnverifiedListings()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var client = RetailListingApiTests.Moderator(factory);
        var receipt = await CreateProductAsync(client, fixture);
        await AddVariantAsync(client, receipt.ProductId, "Pink");
        var managed = await ManagedAsync(client, receipt.ProductId);
        var pink = managed.Variants.Single(v => v.Name == "Pink");
        await AddSizeAsync(client, receipt.ProductId, receipt.ProductVariantId, "Extra Large", 10);
        await AddSizeAsync(client, receipt.ProductId, pink.Id, "Medium", 10);
        managed = await ManagedAsync(client, receipt.ProductId);
        var xl = managed.Variants.Single(v => v.Id == receipt.ProductVariantId).Sizes.Single(s => s.ManufacturerSize == "Extra Large");
        pink = managed.Variants.Single(v => v.Id == pink.Id);
        var otherPack = new PackType(receipt.SizeVariantId, 20, PackagingType.Case);
        await using (var db = fixture.CreateDbContext())
        {
            db.PackTypes.Add(otherPack);
            await db.SaveChangesAsync();
        }
        var retailer = await RetailListingApiTests.CreateRetailerAsync(client, true);
        var url = retailer.WebsiteUrl + "exact-pack";
        var response = await client.PostAsJsonAsync("/api/v1/retail-listings/", new ManualRetailerListingRequest(receipt.ProductId,
            receipt.ProductVariantId, receipt.SizeVariantId, receipt.PackTypeId, retailer.Id, url, null, null));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var listing = (await response.Content.ReadFromJsonAsync<RetailerProductListingItem>())!;
        var path = $"/api/v1/products/{managed.Slug}?variantId={receipt.ProductVariantId}&packTypeId={receipt.PackTypeId}";
        Assert.Empty((await client.GetFromJsonAsync<CatalogueProductDetails>(path))!.RetailOffers);
        await RetailListingApiTests.StatusAsync(client, listing.Id, RetailerProductDiscoveryStatus.Verified);
        var offer = Assert.Single((await client.GetFromJsonAsync<CatalogueProductDetails>(path))!.RetailOffers);
        Assert.Equal(url, offer.DestinationUrl);
        Assert.Equal(receipt.PackTypeId, offer.PackTypeId);
        Assert.Empty((await client.GetFromJsonAsync<CatalogueProductDetails>($"/api/v1/products/{managed.Slug}?variantId={receipt.ProductVariantId}&packTypeId={otherPack.Id}"))!.RetailOffers);
        Assert.Empty((await client.GetFromJsonAsync<CatalogueProductDetails>($"/api/v1/products/{managed.Slug}?variantId={receipt.ProductVariantId}&packTypeId={xl.Packs[0].Id}"))!.RetailOffers);
        Assert.Empty((await client.GetFromJsonAsync<CatalogueProductDetails>($"/api/v1/products/{managed.Slug}?variantId={pink.Id}&packTypeId={pink.Sizes[0].Packs[0].Id}"))!.RetailOffers);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/products/{managed.Slug}?variantId={pink.Id}&packTypeId={receipt.PackTypeId}")).StatusCode);
        await RetailListingApiTests.VerifyRetailerAsync(client, retailer, RetailerIdentityVerificationOutcome.NeedsReview);
        Assert.Empty((await client.GetFromJsonAsync<CatalogueProductDetails>(path))!.RetailOffers);
        Assert.Empty((await client.GetFromJsonAsync<CatalogueProductSearch>($"/api/v1/products?q={managed.Slug}"))!.Products.SelectMany(p => p.RetailDestinations));
    }

    [Fact]
    public async Task RetailerCreation_GeneratesSlugAndHandlesNameCollision()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var client = RetailListingApiTests.Moderator(factory);
        var name = $"NorthShore Care Supply {Guid.NewGuid():N}";
        var first = await client.PostAsJsonAsync("/api/v1/retailer-management", new CreateRetailerManagement(name, "", "https://example.test"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var retailer = (await first.Content.ReadFromJsonAsync<RetailerManagementItem>())!;
        Assert.Equal(name.ToLowerInvariant().Replace(' ', '-'), retailer.Slug);
        var second = await client.PostAsJsonAsync("/api/v1/retailer-management", new CreateRetailerManagement(name, "", "https://example.test"));
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(retailer.Slug + "-2", (await second.Content.ReadFromJsonAsync<RetailerManagementItem>())!.Slug);
    }
}
