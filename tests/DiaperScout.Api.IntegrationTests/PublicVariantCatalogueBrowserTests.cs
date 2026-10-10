using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.Playwright;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class PublicVariantCatalogueBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    [Trait("Category", "Browser")]
    public async Task VariantSizeAndPackChanges_InvalidateOffersAndSurviveRefreshAndHistory()
    {
        using var api = new ObservationApiFactory(fixture);
        using var moderator = RetailListingApiTests.Moderator(api);
        var receipt = await PublicVariantCatalogueApiTests.CreateProductAsync(moderator, fixture);
        await PublicVariantCatalogueApiTests.AddVariantAsync(moderator, receipt.ProductId, "Pink");
        var managed = await PublicVariantCatalogueApiTests.ManagedAsync(moderator, receipt.ProductId);
        var pink = managed.Variants.Single(v => v.Name == "Pink");
        await PublicVariantCatalogueApiTests.AddSizeAsync(moderator, receipt.ProductId, receipt.ProductVariantId, "Extra Large", 15);
        await PublicVariantCatalogueApiTests.AddSizeAsync(moderator, receipt.ProductId, pink.Id, "Medium", 20);
        // There is no management API for adding a second pack to an existing size.
        var otherPack = new PackType(receipt.SizeVariantId, 30, PackagingType.Case);
        await using (var db = fixture.CreateDbContext())
        {
            db.PackTypes.Add(otherPack);
            await db.SaveChangesAsync();
        }
        var retailer = await RetailListingApiTests.CreateRetailerAsync(moderator, true);
        var destination = retailer.WebsiteUrl + "variant-browser";
        var created = await moderator.PostAsJsonAsync("/api/v1/retail-listings/", new ManualRetailerListingRequest(
            receipt.ProductId, receipt.ProductVariantId, receipt.SizeVariantId, receipt.PackTypeId, retailer.Id, destination));
        created.EnsureSuccessStatusCode();
        var listing = (await created.Content.ReadFromJsonAsync<RetailerProductListingItem>())!;
        await RetailListingApiTests.StatusAsync(moderator, listing.Id, RetailerProductDiscoveryStatus.Verified);
        using var web = new PasskeyWebFactory(api);
        web.UseKestrel(0);
        using var client = web.CreateClient();
        var origin = client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(origin + "/products");
        await page.WaitForFunctionAsync("() => document.documentElement.dataset.pwaState === 'ready'");
        await page.Locator("#catalogue-query").FillAsync(managed.Slug);
        await page.GetByRole(AriaRole.Button, new() { Name = "Search", Exact = false }).ClickAsync();
        var blackLink = page.GetByRole(AriaRole.Link, new() { Name = "Integration Test Brand MEGAMAX Black", Exact = true });
        await Assertions.Expect(blackLink).ToHaveAttributeAsync("href", PublicProductIdentity.ProductUrl(managed.Slug, receipt.ProductVariantId));
        await blackLink.ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Integration Test Brand MEGAMAX Black");
        await Assertions.Expect(page.Locator(".ds-size-pill:visible")).ToHaveCountAsync(2);
        await page.WaitForFunctionAsync("() => document.documentElement.dataset.pwaState === 'ready'");
        var medium = page.GetByRole(AriaRole.Button, new() { Name = "Medium", Exact = false });
        await medium.ClickAsync();
        var retailerLink = page.Locator($".ds-desktop-product a[href='{destination}']");
        await Assertions.Expect(retailerLink).ToHaveCountAsync(1);
        var packSelect = page.GetByRole(AriaRole.Combobox, new() { Name = "Pack size", Exact = true });
        await packSelect.SelectOptionAsync(otherPack.Id.ToString());
        await Assertions.Expect(retailerLink).ToHaveCountAsync(0);
        await packSelect.SelectOptionAsync(receipt.PackTypeId.ToString());
        await Assertions.Expect(retailerLink).ToHaveCountAsync(1);
        await page.GetByRole(AriaRole.Button, new() { Name = "Extra Large", Exact = false }).ClickAsync();
        await Assertions.Expect(retailerLink).ToHaveCountAsync(0);
        await Assertions.Expect(packSelect.Locator("option")).ToHaveCountAsync(1);
        await Assertions.Expect(packSelect).Not.ToHaveValueAsync(receipt.PackTypeId.ToString());
        await medium.ClickAsync();
        await Assertions.Expect(retailerLink).ToHaveCountAsync(1);
        await page.Locator("#desktop-product-variant").SelectOptionAsync(pink.Id.ToString());
        await Assertions.Expect(page).ToHaveURLAsync(origin + PublicProductIdentity.ProductUrl(managed.Slug, pink.Id));
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Integration Test Brand MEGAMAX Pink");
        await Assertions.Expect(page.Locator(".ds-size-pill:visible")).ToHaveCountAsync(1);
        await Assertions.Expect(retailerLink).ToHaveCountAsync(0);
        await Assertions.Expect(packSelect.Locator("option")).ToHaveTextAsync(["20 pieces · Bag"]);
        await page.ReloadAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Integration Test Brand MEGAMAX Pink");
        await page.WaitForFunctionAsync("() => document.documentElement.dataset.pwaState === 'ready'");
        await page.GoBackAsync();
        await Assertions.Expect(page).ToHaveURLAsync(origin + PublicProductIdentity.ProductUrl(managed.Slug, receipt.ProductVariantId));
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Integration Test Brand MEGAMAX Black");
        await page.GoForwardAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Integration Test Brand MEGAMAX Pink");
        await page.GotoAsync(origin + $"/products/{managed.Slug}?variantId=invalid");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Product not found");
        await page.GotoAsync(origin + $"/products/{managed.Slug}?variantId={Guid.NewGuid()}");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Product not found");
    }
    [Fact]
    [Trait("Category", "Browser")]
    public async Task RetailerCreation_HidesSlugAndGeneratesItFromName()
    {
        using var api = new ObservationApiFactory(fixture);
        using var webBase = new PasskeyWebFactory(api);
        using var web = webBase.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            { ["Authentication:Development:Subject"] = PostgreSqlFixture.ModeratorSubject })));
        web.UseKestrel(0);
        using var client = web.CreateClient();
        var origin = client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(origin + "/signin/development");
        await page.GotoAsync(origin + "/catalogue/retailers");
        await Assertions.Expect(page.Locator("#retailer-search")).ToHaveAttributeAsync("placeholder", "Name or website…");
        await page.GetByRole(AriaRole.Button, new() { Name = "Add retailer", Exact = false }).ClickAsync();
        await Assertions.Expect(page.Locator("#retailer-slug")).ToHaveCountAsync(0);
        var name = $"Browser Retailer {Guid.NewGuid():N}";
        await page.Locator("#retailer-name").FillAsync(name);
        await page.Locator("#retailer-website").FillAsync("https://browser-retailer.example");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create retailer", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("tbody tr").Filter(new() { HasText = name })).ToHaveCountAsync(1);
        using var moderator = RetailListingApiTests.Moderator(api);
        var retailers = (await moderator.GetFromJsonAsync<List<RetailerManagementItem>>("/api/v1/retailer-management"))!;
        Assert.Equal(name.ToLowerInvariant().Replace(' ', '-'), retailers.Single(r => r.Name == name).Slug);
    }

}
