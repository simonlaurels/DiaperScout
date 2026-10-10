using System.Net.Http.Json;
using DiaperScout.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class RetailListingBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    [Trait("Category", "Browser")]
    public async Task ManualListing_ProgressiveSelectionConfirmationAndPublicDestination()
    {
        using var api = new ObservationApiFactory(fixture);
        using var moderator = RetailListingApiTests.Moderator(api);
        var retailer = await RetailListingApiTests.CreateRetailerAsync(moderator, true);
        var catalogue = (await moderator.GetFromJsonAsync<List<RetailListingProductOption>>("/api/v1/retail-listings/catalogue"))!;
        var product = catalogue.Single(x => x.Id == fixture.ProductId);
        var variant = product.Variants.Single(x => x.Sizes.Any(s => s.Packs.Any(p => p.Id == fixture.PackTypeId)));
        var size = variant.Sizes.Single(x => x.Packs.Any(p => p.Id == fixture.PackTypeId));
        using var webBase = new PasskeyWebFactory(api);
        using var web = webBase.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            { ["Authentication:Development:Subject"] = PostgreSqlFixture.ModeratorSubject })));
        web.UseKestrel(0);
        using var client = web.CreateClient();
        var origin = client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1280, Height = 900 } });
        // A controlled destination proves the rendered link without depending on an external shop.
        await context.RouteAsync("https://manual.example/**", route => route.FulfillAsync(new() { Status = 200, ContentType = "text/html", Body = "<h1>Retailer product destination</h1>" }));
        var page = await context.NewPageAsync();
        await page.GotoAsync(origin + "/signin/development");
        await page.GotoAsync(origin + "/catalogue/retail-listings");
        await Assertions.Expect(page.GetByText("Retailer product URL", new() { Exact = true })).ToHaveCountAsync(1);
        await page.Locator("#listing-retailer").SelectOptionAsync(retailer.Id.ToString());
        await page.Locator("#listing-product").SelectOptionAsync(product.Id.ToString());
        await page.Locator("#listing-variant").SelectOptionAsync(variant.Id.ToString());
        await page.Locator("#listing-size").SelectOptionAsync(size.Id.ToString());
        await page.Locator("#listing-pack").SelectOptionAsync(fixture.PackTypeId.ToString());
        await page.Locator("#listing-product").SelectOptionAsync(Guid.Empty.ToString());
        await Assertions.Expect(page.Locator("#listing-variant")).ToHaveValueAsync(Guid.Empty.ToString());
        await Assertions.Expect(page.Locator("#listing-pack")).ToBeDisabledAsync();
        await page.Locator("#listing-product").SelectOptionAsync(product.Id.ToString());
        await page.Locator("#listing-variant").SelectOptionAsync(variant.Id.ToString());
        await page.Locator("#listing-size").SelectOptionAsync(size.Id.ToString());
        await page.Locator("#listing-pack").SelectOptionAsync(fixture.PackTypeId.ToString());
        var destination = "https://manual.example/product/browser";
        await page.Locator("#manual-listing-url").FillAsync(destination);
        await page.Locator("#manual-listing-url").BlurAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Save listing", Exact = true }).ClickAsync();
        var card = page.Locator(".listing-card").Filter(new() { HasText = retailer.Name });
        await Assertions.Expect(card).ToContainTextAsync("Discovered");
        var before = (await moderator.GetFromJsonAsync<CatalogueProductDetails>("/api/v1/products/integration-test-product"))!;
        Assert.DoesNotContain(before.RetailOffers, x => x.RetailerName == retailer.Name);
        await card.GetByRole(AriaRole.Button, new() { Name = "Confirm listing", Exact = true }).ClickAsync();
        await Assertions.Expect(card.Locator("p strong")).ToHaveTextAsync("Verified");
        foreach (var width in new[] { 1280, 390 })
        {
            await page.SetViewportSizeAsync(width, 900);
            Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth"));
            var artifacts = Environment.GetEnvironmentVariable("DIAPERSCOUT_TEST_ARTIFACTS");
            if (!string.IsNullOrWhiteSpace(artifacts))
            {
                Directory.CreateDirectory(artifacts);
                await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, $"retail-listings-{width}.png"), FullPage = true });
            }
        }
        var confirmed = (await moderator.GetFromJsonAsync<CatalogueProductDetails>(
            $"/api/v1/products/integration-test-product?variantId={variant.Id}&packTypeId={fixture.PackTypeId}"))!;
        var offer = Assert.Single(confirmed.RetailOffers, x => x.RetailerName == retailer.Name);
        Assert.Equal(fixture.PackTypeId, offer.PackTypeId);
        Assert.Equal(destination, offer.DestinationUrl);

        await page.GotoAsync(origin + $"/products/integration-test-product?variantId={variant.Id}&packTypeId={fixture.PackTypeId}");
        foreach (var width in new[] { 1280, 390 })
        {
            await page.SetViewportSizeAsync(width, 900);
            // Both responsive presentations share the same exact-pack offer in the DOM.
            // Only the active presentation should expose one usable retailer destination.
            var root = page.Locator(width >= 1000 ? ".ds-desktop-product" : ".pwa-prototype.product-page");
            var inactive = page.Locator(width >= 1000 ? ".pwa-prototype.product-page" : ".ds-desktop-product");
            await Assertions.Expect(root).ToBeVisibleAsync();
            await Assertions.Expect(inactive).ToBeHiddenAsync();
            var link = root.Locator($"a[href='{destination}']");
            await Assertions.Expect(link).ToHaveCountAsync(1);
            await Assertions.Expect(link).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator($"a[href='{destination}']:visible")).ToHaveCountAsync(1);
            var popup = await page.RunAndWaitForPopupAsync(() => link.ClickAsync());
            await popup.WaitForLoadStateAsync();
            Assert.Equal(destination, popup.Url);
            await Assertions.Expect(popup.GetByRole(AriaRole.Heading)).ToHaveTextAsync("Retailer product destination");
            await popup.CloseAsync();
        }
    }
}
