using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class RetailListingApiTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task ManualJourney_CreateVerifyCorrectUnverifyAndRetireThroughApi()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var client = Moderator(factory);
        var retailer = await CreateRetailerAsync(client, true);
        var command = await SelectionAsync(client, retailer.Id, retailer.WebsiteUrl + "product/manual");
        var listing = await CreateAsync(client, command);
        Assert.Equal("Manual", listing.DiscoveryProvider);
        Assert.Equal(RetailerProductDiscoveryStatus.Discovered, listing.Status);
        Assert.Equal(command.PackTypeId, listing.PackTypeId);
        Assert.Equal(retailer.Id, listing.RetailerId);
        Assert.DoesNotContain((await ProductAsync(client)).RetailOffers, x => x.Id == listing.Id);
        await StatusAsync(client, listing.Id, RetailerProductDiscoveryStatus.Verified);
        var offer = Assert.Single((await ProductAsync(client)).RetailOffers, x => x.Id == listing.Id);
        Assert.Equal(fixture.PackTypeId, offer.PackTypeId);
        Assert.Equal("Integration Test Size", offer.ManufacturerSize);
        Assert.Equal(12, offer.QuantityPerPack);
        Assert.Equal(command.ListingUrl, offer.DestinationUrl);
        Assert.False(offer.IsAffiliateBacked);
        Assert.Null(offer.AffiliateNetwork);
        var rows = await client.GetFromJsonAsync<List<ManagedRetailerListing>>("/api/v1/retail-listings/");
        var row = Assert.Single(rows!, x => x.Listing.Id == listing.Id);
        Assert.Equal(command.ProductId, row.ProductId);
        Assert.Equal(command.ProductVariantId, row.ProductVariantId);
        Assert.Equal(command.SizeVariantId, row.SizeVariantId);
        var update = await client.PutAsJsonAsync($"/api/v1/retail-listings/{listing.Id}",
            new RetailerListingUpdate(retailer.WebsiteUrl + "product/corrected", retailer.WebsiteUrl + "evidence", "retailer-ref"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var corrected = await update.Content.ReadFromJsonAsync<RetailerProductListingItem>();
        Assert.Equal(listing.Id, corrected!.Id);
        Assert.Equal(RetailerProductDiscoveryStatus.NeedsReview, corrected.Status);
        Assert.Equal("Manual", corrected.DiscoveryProvider);
        Assert.Equal("retailer-ref", corrected.ExternalListingId);
        // PostgreSQL timestamps round to microsecond precision.
        Assert.True((listing.DiscoveredAtUtc - corrected.DiscoveredAtUtc).Duration() < TimeSpan.FromMicroseconds(1));
        Assert.DoesNotContain((await ProductAsync(client)).RetailOffers, x => x.Id == listing.Id);
        await StatusAsync(client, listing.Id, RetailerProductDiscoveryStatus.Verified);
        await StatusAsync(client, listing.Id, RetailerProductDiscoveryStatus.NeedsReview);
        Assert.DoesNotContain((await ProductAsync(client)).RetailOffers, x => x.Id == listing.Id);
        await StatusAsync(client, listing.Id, RetailerProductDiscoveryStatus.Verified);
        await StatusAsync(client, listing.Id, RetailerProductDiscoveryStatus.Inactive);
        Assert.DoesNotContain((await ProductAsync(client)).RetailOffers, x => x.Id == listing.Id);
    }

    [Fact]
    public async Task UnverifiedRetailer_PreventsConfirmationAndHidesPreviouslyVerifiedListing()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var client = Moderator(factory);
        var retailer = await CreateRetailerAsync(client, false);
        var listing = await CreateAsync(client, await SelectionAsync(client, retailer.Id, retailer.WebsiteUrl + "product/unverified"));
        var denied = await client.PutAsJsonAsync($"/api/v1/retail-listings/{listing.Id}/status", new RetailerListingStatusUpdate(RetailerProductDiscoveryStatus.Verified));
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        Assert.DoesNotContain((await ProductAsync(client)).RetailOffers, x => x.Id == listing.Id);
        await VerifyRetailerAsync(client, retailer, RetailerIdentityVerificationOutcome.Verified);
        await StatusAsync(client, listing.Id, RetailerProductDiscoveryStatus.Verified);
        Assert.Contains((await ProductAsync(client)).RetailOffers, x => x.Id == listing.Id);
        await VerifyRetailerAsync(client, retailer, RetailerIdentityVerificationOutcome.NeedsReview);
        Assert.DoesNotContain((await ProductAsync(client)).RetailOffers, x => x.Id == listing.Id);
    }

    [Theory]
    [InlineData("product")]
    [InlineData("variant")]
    [InlineData("size")]
    [InlineData("pack")]
    public async Task InvalidCanonicalSelection_IsRejected(string field)
    {
        using var factory = new ObservationApiFactory(fixture);
        using var client = Moderator(factory);
        var request = await SelectionAsync(client, fixture.RetailerId, "https://invalid.example/item");
        request = field switch
        {
            "product" => request with { ProductId = Guid.NewGuid() },
            "variant" => request with { ProductVariantId = Guid.NewGuid() },
            "size" => request with { SizeVariantId = Guid.NewGuid() },
            _ => request with { PackTypeId = Guid.NewGuid() }
        };
        var response = await client.PostAsJsonAsync("/api/v1/retail-listings/", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var db = fixture.CreateDbContext();
        Assert.False(await db.RetailerProductListings.AnyAsync(x => x.ListingUrl == request.ListingUrl));
    }

    [Fact]
    public async Task ExistingPackFromAnotherVariant_IsRejected()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var client = Moderator(factory);
        var selection = await SelectionAsync(client, fixture.RetailerId, "https://invalid.example/foreign-pack");
        var variant = new ProductVariant(fixture.ProductId, "Other variant");
        var size = new SizeVariant(variant.Id, "Other size");
        var pack = new PackType(size.Id, 10, PackagingType.Bag);
        await using (var db = fixture.CreateDbContext())
        {
            db.AddRange(variant, size, pack);
            await db.SaveChangesAsync();
        }
        var response = await client.PostAsJsonAsync("/api/v1/retail-listings/", selection with { PackTypeId = pack.Id });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("selected product, variant and size", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task NoGtinPack_WorksAndProviderOperationIsIdempotent()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var client = Moderator(factory);
        var retailer = await CreateRetailerAsync(client, true);
        var selection = await SelectionAsync(client, retailer.Id, retailer.WebsiteUrl + "product/no-gtin");
        var pack = new PackType(selection.SizeVariantId, 24, PackagingType.Bag);
        await using (var db = fixture.CreateDbContext()) { db.Add(pack); await db.SaveChangesAsync(); }
        var listing = await CreateAsync(client, selection with { PackTypeId = pack.Id });
        await StatusAsync(client, listing.Id, RetailerProductDiscoveryStatus.Verified);
        var offer = Assert.Single((await ProductAsync(client)).RetailOffers, x => x.Id == listing.Id);
        Assert.Equal(24, offer.QuantityPerPack);
        Assert.Null(offer.Gtin);
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRetailerDiscovery>();
        var provider = new UpsertRetailerListing(pack.Id, retailer.Id, retailer.WebsiteUrl + "product/feed", "Retailer feed", null, "feed-id");
        var first = await service.UpsertAsync(provider);
        var second = await service.UpsertAsync(provider);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal("Retailer feed", first.DiscoveryProvider);
    }

    [Fact]
    public async Task DuplicatesInvalidUrlsAndUnknownListingActions_ReturnUsefulFailures()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var client = Moderator(factory);
        var retailer = await CreateRetailerAsync(client, true);
        var request = await SelectionAsync(client, retailer.Id, retailer.WebsiteUrl + "product/duplicate");
        var listing = await CreateAsync(client, request);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/retail-listings/", request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/retail-listings/", request with { ListingUrl = "javascript:alert(1)" })).StatusCode);
        var other = await CreateAsync(client, request with { ListingUrl = retailer.WebsiteUrl + "product/other" });
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/v1/retail-listings/{other.Id}", new RetailerListingUpdate(request.ListingUrl))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/v1/retail-listings/{listing.Id}/status", new RetailerListingStatusUpdate((RetailerProductDiscoveryStatus)999))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/v1/retail-listings/{Guid.NewGuid()}/status", new RetailerListingStatusUpdate(RetailerProductDiscoveryStatus.Inactive))).StatusCode);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData(PostgreSqlFixture.ExplorerSubject, HttpStatusCode.Forbidden)]
    public async Task AllListingRoutes_RequireEditorialAuthority(string? subject, HttpStatusCode expected)
    {
        using var factory = new ObservationApiFactory(fixture);
        using var client = factory.CreateClient();
        if (subject is not null) client.DefaultRequestHeaders.Add("X-Development-Subject", subject);
        Assert.Equal(expected, (await client.GetAsync("/api/v1/retail-listings/catalogue")).StatusCode);
        Assert.Equal(expected, (await client.GetAsync("/api/v1/retail-listings/")).StatusCode);
        Assert.Equal(expected, (await client.PostAsJsonAsync("/api/v1/retail-listings/", new ManualRetailerListingRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "https://test.example/item"))).StatusCode);
        Assert.Equal(expected, (await client.PutAsJsonAsync($"/api/v1/retail-listings/{Guid.NewGuid()}", new RetailerListingUpdate("https://test.example/item"))).StatusCode);
        Assert.Equal(expected, (await client.PutAsJsonAsync($"/api/v1/retail-listings/{Guid.NewGuid()}/status", new RetailerListingStatusUpdate(RetailerProductDiscoveryStatus.Verified))).StatusCode);
    }

    internal static HttpClient Moderator(ObservationApiFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Development-Subject", PostgreSqlFixture.ModeratorSubject);
        client.DefaultRequestHeaders.Add("X-Development-Role", "Moderator");
        return client;
    }
    internal static async Task<RetailerManagementItem> CreateRetailerAsync(HttpClient client, bool verify)
    {
        var id = Guid.NewGuid().ToString("N");
        var response = await client.PostAsJsonAsync("/api/v1/retailer-management", new CreateRetailerManagement("Manual retailer " + id, "manual-" + id, "https://manual.example"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var retailer = (await response.Content.ReadFromJsonAsync<RetailerManagementItem>())!;
        if (verify) await VerifyRetailerAsync(client, retailer, RetailerIdentityVerificationOutcome.Verified);
        return retailer;
    }
    internal static async Task VerifyRetailerAsync(HttpClient client, RetailerManagementItem retailer, RetailerIdentityVerificationOutcome outcome)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/retailer-management/{retailer.Id}/identity-verification",
            new RetailerIdentityVerificationRequest(retailer.Name, retailer.WebsiteUrl + "about", retailer.WebsiteUrl + "product/test", outcome, "Manual evidence checked."));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }
    private async Task<ManualRetailerListingRequest> SelectionAsync(HttpClient client, Guid retailerId, string url)
    {
        var catalogue = await client.GetFromJsonAsync<List<RetailListingProductOption>>("/api/v1/retail-listings/catalogue");
        var product = catalogue!.Single(x => x.Id == fixture.ProductId);
        var variant = product.Variants.First(x => x.Sizes.Any(s => s.Packs.Any(p => p.Id == fixture.PackTypeId)));
        var size = variant.Sizes.Single(x => x.Packs.Any(p => p.Id == fixture.PackTypeId));
        return new(product.Id, variant.Id, size.Id, fixture.PackTypeId, retailerId, url);
    }
    private static async Task<RetailerProductListingItem> CreateAsync(HttpClient client, ManualRetailerListingRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/v1/retail-listings/", request);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<RetailerProductListingItem>())!;
    }
    private static async Task StatusAsync(HttpClient client, Guid id, RetailerProductDiscoveryStatus status)
    {
        var response = await client.PutAsJsonAsync($"/api/v1/retail-listings/{id}/status", new RetailerListingStatusUpdate(status));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }
    private static async Task<CatalogueProductDetails> ProductAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<CatalogueProductDetails>("/api/v1/products/integration-test-product"))!;
}
