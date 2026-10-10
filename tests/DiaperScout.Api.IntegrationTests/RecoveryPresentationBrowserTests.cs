extern alias DiaperScoutWeb;
using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

// Isolated presentation/contract checks: no database, credentials or production services.
public sealed class RecoveryPresentationBrowserTests
{
    [Theory]
    [InlineData(1280, false)]
    [InlineData(1440, false)]
    [InlineData(375, false)]
    [InlineData(390, false)]
    [InlineData(430, false)]
    [InlineData(375, true)]
    [InlineData(390, true)]
    [InlineData(430, true)]
    [InlineData(1280, true)]
    [Trait("Category", "Browser")]
    public async Task Desktop_recovery_preserves_mobile_and_exact_pack_selection(int width, bool installed)
    {
        var handler = new CatalogueFixture();
        using var web = new RecoveryWebFactory(handler);
        web.UseKestrel(0);
        using var client = web.CreateClient();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = width, Height = 900 }, ServiceWorkers = ServiceWorkerPolicy.Block });
        await context.AddInitScriptAsync("localStorage.setItem('ds-welcome-complete-v1','yes');" + (installed ? "Object.defineProperty(navigator,'standalone',{value:true});" : ""));
        await context.RouteAsync("**/test-recovery-*.svg", r => r.FulfillAsync(new() { ContentType = "image/svg+xml", Body = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='160'><rect width='100' height='160' fill='#d9e9de'/><text x='2' y='80'>TEST PACK</text></svg>" }));
        var page = await context.NewPageAsync();
        var origin = client.BaseAddress!.ToString().TrimEnd('/');
        await page.GotoAsync(origin + $"/products/recovery-fixture?variantId={handler.Variant}&packTypeId={handler.Pack}");
        var desktop = width >= 1000 && !installed;
        var root = page.Locator(desktop ? ".ds-desktop-product" : ".pwa-prototype.product-page");
        await Assertions.Expect(root).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(desktop ? ".pwa-prototype.product-page" : ".ds-desktop-product")).ToBeHiddenAsync();
        // SSR controls appear before the shared InteractiveServer handlers attach.
        await page.WaitForFunctionAsync("() => document.querySelector('.gallery-viewport')?.dataset.galleryReady === 'true'");
        if (desktop)
        {
            await Assertions.Expect(page.Locator(".ds-app-header")).ToBeVisibleAsync();
            await Assertions.Expect(root.Locator(".ds-guide-scene")).ToBeVisibleAsync();
            await page.WaitForFunctionAsync("() => document.querySelector('.ds-guide-scene img')?.naturalWidth > 0");
            await Assertions.Expect(root.Locator(".ds-gallery-main img")).ToHaveAttributeAsync("src", "/test-recovery-1.svg");
            await root.Locator(".ds-gallery-arrow-right").ClickAsync();
            await Assertions.Expect(root.Locator(".ds-gallery-main img")).ToHaveAttributeAsync("src", "/test-recovery-2.svg");
            await root.Locator(".ds-gallery-main").FocusAsync();
            await page.Keyboard.PressAsync("ArrowLeft");
            await Assertions.Expect(root.Locator(".ds-gallery-main img")).ToHaveAttributeAsync("src", "/test-recovery-1.svg");
            await Assertions.Expect(root.Locator(".ds-feature-svg").First).ToHaveCSSAsync("fill", "none");
            await Assertions.Expect(root.GetByRole(AriaRole.Link, new() { Name = "View all locations and listings" })).ToHaveAttributeAsync("href", $"/products/recovery-fixture/retailers?variantId={handler.Variant}&packTypeId={handler.Pack}");
            await Assertions.Expect(root.GetByRole(AriaRole.Link, new() { Name = "Add Observation", Exact = true })).ToHaveAttributeAsync("href", "/observations/new?packTypeId=" + handler.Pack);
            await Assertions.Expect(root.Locator(".ds-product-summary h1")).ToHaveTextAsync(handler.Name);
            await Assertions.Expect(root.Locator(".ds-retail-list")).ToContainTextAsync("Fixture retailer");
        }
        else
        {
            await Assertions.Expect(page.Locator(".ds-app-header")).ToBeHiddenAsync();
            await page.WaitForFunctionAsync("() => document.querySelector('.gallery-viewport')?.dataset.galleryReady === 'true'");
            await Assertions.Expect(root.Locator(".gallery-counter")).ToHaveTextAsync("1 / 2");
        }
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth"));
        await Evidence(page, $"recovery-product-{width}-{installed}");
        var packSelect = root.GetByRole(AriaRole.Combobox, new() { Name = "Pack size", Exact = true });
        await packSelect.SelectOptionAsync(handler.OtherPack.ToString());
        await Assertions.Expect(packSelect).ToHaveValueAsync(handler.OtherPack.ToString());
        await Assertions.Expect(root.Locator("a[href='https://retailer.example.test/pack']")).ToHaveCountAsync(0);
        await Assertions.Expect(root.Locator(desktop ? ".ds-empty-retail" : ".availability-card")).ToContainTextAsync(desktop ? "No retail destinations yet" : "No locations or listings");
        await root.Locator(desktop ? "#desktop-product-variant" : "#public-product-variant").SelectOptionAsync(handler.OtherVariant.ToString());
        await Assertions.Expect(page).ToHaveURLAsync(origin + $"/products/recovery-fixture?variantId={handler.OtherVariant}");
        await Assertions.Expect(root.Locator(desktop ? ".ds-product-summary h1" : ".product-intro h1")).ToHaveTextAsync("Fixture second variant");
        handler.Empty = true;
        await page.ReloadAsync();
        await Assertions.Expect(root.Locator(desktop ? ".ds-gallery-empty" : ".gallery-empty")).ToBeVisibleAsync();
        await Assertions.Expect(root).ToContainTextAsync(desktop ? "Sizes not recorded" : "Not recorded");
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth"));
        await Evidence(page, $"recovery-empty-{width}-{installed}");
        await page.GotoAsync(origin + "/products/recovery-fixture?variantId=invalid");
        await Assertions.Expect(root).ToContainTextAsync("Product not found");
    }

    private static async Task Evidence(IPage page, string name)
    {
        var path = Environment.GetEnvironmentVariable("DIAPERSCOUT_TEST_ARTIFACTS");
        if (path is null) return;
        Directory.CreateDirectory(path);
        await page.ScreenshotAsync(new() { Path = Path.Combine(path, name + ".png"), FullPage = true });
    }

    private sealed class RecoveryWebFactory(CatalogueFixture handler) : WebApplicationFactory<DiaperScoutWeb::Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["Api:BaseUrl"] = "http://fixture.test", ["LanTesting:Enabled"] = "false" }));
            builder.ConfigureTestServices(s =>
            {
                s.AddHttpClient<DiaperScoutWeb::DiaperScout.Web.Services.ProductCatalogueClient>().ConfigurePrimaryHttpMessageHandler(() => handler);
                s.AddHttpClient<DiaperScoutWeb::DiaperScout.Web.Services.PlaceObservationClient>().ConfigurePrimaryHttpMessageHandler(() => handler);
            });
        }
    }

    private sealed class CatalogueFixture : HttpMessageHandler
    {
        public Guid Variant { get; } = Guid.NewGuid();
        public Guid OtherVariant { get; } = Guid.NewGuid();
        public Guid Pack { get; } = Guid.NewGuid();
        public Guid OtherPack { get; } = Guid.NewGuid();
        public bool Empty;
        public string Name => "Fixture grouped product with a long manufacturer and absorbency name";
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            object body;
            if (request.RequestUri!.AbsolutePath.Contains("/products/"))
            {
                var second = request.RequestUri.Query.Contains(OtherVariant.ToString());
                var variant = second ? OtherVariant : Variant;
                var offers = Empty || request.RequestUri.Query.Contains(OtherPack.ToString()) ? Array.Empty<CatalogueRetailOffer>() : new[] { new CatalogueRetailOffer(Guid.NewGuid(), Pack, "M", 10, PackagingType.Bag, null, "Fixture retailer", "https://retailer.example.test/pack", "https://retailer.example.test/pack", null, false) };
                var sizes = Empty ? Array.Empty<CatalogueProductSize>() : new[] { new CatalogueProductSize(Guid.NewGuid(), "M", 70, 110, null, null, null, null, null, null, null, null, null, [new(Pack, 10, PackagingType.Bag, ["12345678"]), new(OtherPack, 30, PackagingType.Bag, [])]) };
                body = new CatalogueProductDetails(Guid.NewGuid(), second ? "Fixture second variant" : Name, "recovery-fixture", ProductType.Tape, ProductStatus.Current, "Fixture manufacturer", "Fixture brand", "Synthetic recovery test catalogue entry.", CatalogueContentVisibility.Public, null,
                    [new(variant, second ? "10 drops" : "6 drops", BackingType.Cloth, FastenerType.Unknown, CatalogueVariantAppearance.Unknown, CatalogueVariantColour.Unknown, true, true, WaistbandStyle.Unknown, FragranceType.Unknown, true, CatalogueVariantDesignedFor.Unknown, null, null, sizes)],
                    Empty ? [] : [new(Guid.NewGuid(), CatalogueSubmissionImageRole.PackFront, true, "/test-recovery-1.svg"), new(Guid.NewGuid(), CatalogueSubmissionImageRole.PackBack, false, "/test-recovery-2.svg")], offers, variant, [new(Variant, "6 drops"), new(OtherVariant, "10 drops")]);
            }
            else body = Array.Empty<AtlasPlace>();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
        }
    }
}
