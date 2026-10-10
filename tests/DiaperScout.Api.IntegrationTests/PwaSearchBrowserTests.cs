extern alias DiaperScoutWeb;
using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Xunit;
using CatalogueClient = DiaperScoutWeb::DiaperScout.Web.Services.ProductCatalogueClient;

namespace DiaperScout.Api.IntegrationTests;

public sealed class PwaSearchBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData(320, false)]
    [InlineData(390, false)]
    [InlineData(430, true)]
    [Trait("Category", "Browser")]
    public async Task ApprovedSearch_RealVariantJourneyHistoryFiltersAndMobileGeometry(int width, bool webkit)
    {
        using var api = new ObservationApiFactory(fixture);
        using var moderator = RetailListingApiTests.Moderator(api);
        var receipt = await PublicVariantCatalogueApiTests.CreateProductAsync(moderator, fixture);
        var product = await PublicVariantCatalogueApiTests.ManagedAsync(moderator, receipt.ProductId);
        using var web = new PasskeyWebFactory(api);
        web.UseKestrel(0);
        using var client = web.CreateClient();
        var origin = client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await (webkit ? playwright.Webkit : playwright.Chromium).LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = width, Height = 844 }, HasTouch = true });
        await context.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});");
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        await page.GotoAsync(origin + "/products");
        await page.WaitForFunctionAsync("() => document.documentElement.dataset.pwaState === 'ready'");
        var screen = page.Locator(".pwa-search");
        await Assertions.Expect(screen.Locator("h1")).ToHaveTextAsync("Search");
        await Assertions.Expect(page.Locator(".ds-app-header")).Not.ToBeVisibleAsync();
        await Assertions.Expect(screen.Locator(".category-icon img")).ToHaveCountAsync(5);
        await Assertions.Expect(screen.Locator("h1")).ToHaveCSSAsync("font-family", new System.Text.RegularExpressions.Regex("^Inter"));
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth"));
        await CaptureAsync(page, $"search-browse-{width}.png");
        var query = screen.Locator("#pwa-search-query");
        await query.FillAsync(product.Slug);
        await query.PressAsync("Enter");
        var link = screen.Locator($"a[href='{PublicProductIdentity.ProductUrl(product.Slug, receipt.ProductVariantId)}']");
        await Assertions.Expect(link).ToHaveCountAsync(1);
        await Assertions.Expect(link).ToContainTextAsync("MEGAMAX Black");
        await Assertions.Expect(link).ToContainTextAsync("No image recorded");
        await Assertions.Expect(screen.Locator(".recent-term")).ToContainTextAsync([product.Slug]);
        await CaptureAsync(page, $"search-results-{width}.png");
        var navBox = (await page.Locator(".pwa-mobile-nav").BoundingBoxAsync())!;
        await link.ClickAsync();
        await Assertions.Expect(page.Locator(".product-intro h1")).ToHaveTextAsync("Integration Test Brand MEGAMAX Black");
        await page.GetByRole(AriaRole.Link, new() { Name = "View all", Exact = true }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/retailers.*variantId=" + receipt.ProductVariantId));
        await Assertions.Expect(page.Locator(".retailers-header h1")).ToHaveTextAsync("Available in");
        Assert.Equal(navBox.Height, (await page.Locator(".pwa-mobile-nav").BoundingBoxAsync())!.Height);
        await page.GoBackAsync();
        await Assertions.Expect(page.Locator(".product-intro h1")).ToBeVisibleAsync();
        await page.GoBackAsync();
        await Assertions.Expect(query).ToHaveValueAsync(product.Slug);
        await Assertions.Expect(link).ToHaveCountAsync(1);
        await screen.GetByRole(AriaRole.Button, new() { Name = "Diapers", Exact = true }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("productType=Tape"));
        await link.ClickAsync();
        await page.GoBackAsync();
        await Assertions.Expect(screen.GetByRole(AriaRole.Button, new() { Name = "Diapers", Exact = true })).ToHaveAttributeAsync("aria-pressed", "true");
        await query.FillAsync("no-such-product-" + Guid.NewGuid());
        await query.PressAsync("Enter");
        await Assertions.Expect(screen.GetByRole(AriaRole.Heading, new() { Name = "No products found" })).ToBeVisibleAsync();
        await screen.GetByRole(AriaRole.Button, new() { Name = "Clear", Exact = true }).ClickAsync();
        await Assertions.Expect(screen.Locator(".recent-term")).ToHaveCountAsync(0);
        await page.ReloadAsync();
        await Assertions.Expect(screen.Locator(".recent-term")).ToHaveCountAsync(0);
        await page.Locator(".pwa-mobile-nav a[href='/scan']").ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(origin + "/scan");
        Assert.Empty(errors);
    }

    [Fact]
    [Trait("Category", "Browser")]
    public async Task MoreResults_UsesRealCataloguePaginationWithoutLosingVariantIdentity()
    {
        var prefix = "Paging" + Guid.NewGuid().ToString("N");
        await using (var db = fixture.CreateDbContext())
        {
            db.ProductVariants.AddRange(Enumerable.Range(0, 52).Select(x => new ProductVariant(fixture.ProductId, prefix + x)));
            await db.SaveChangesAsync();
        }
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api);
        web.UseKestrel(0);
        using var client = web.CreateClient();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 390, Height = 844 } });
        await page.GotoAsync(client.BaseAddress!.GetLeftPart(UriPartial.Authority) + "/products?q=" + prefix);
        await page.WaitForFunctionAsync("() => document.documentElement.dataset.pwaState === 'ready'");
        var cards = page.Locator(".pwa-search .search-result-card");
        await Assertions.Expect(cards).ToHaveCountAsync(24);
        var more = page.Locator(".pwa-search").GetByRole(AriaRole.Button, new() { Name = "Show more products" });
        await more.ClickAsync();
        await Assertions.Expect(cards).ToHaveCountAsync(48);
        await more.ClickAsync();
        await Assertions.Expect(cards).ToHaveCountAsync(52);
        await Assertions.Expect(more).ToHaveCountAsync(0);
        var paths = await cards.EvaluateAllAsync<string[]>("elements=>elements.map(e=>e.getAttribute('href'))");
        Assert.Equal(52, paths.Distinct().Count());
        Assert.All(paths, path => Assert.Contains("variantId=", path));
    }

    [Fact]
    [Trait("Category", "Browser")]
    public async Task SlowCatalogue_LoadingFailureRetryAndBrokenImageRemainUsable()
    {
        using var api = new ObservationApiFactory(fixture);
        var state = new SearchTransportState();
        using var webBase = new PasskeyWebFactory(api);
        using var web = webBase.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddHttpClient<CatalogueClient>().ConfigurePrimaryHttpMessageHandler(() => new SearchTransportHandler(api.Server.CreateHandler(), state))));
        web.UseKestrel(0);
        using var client = web.CreateClient();
        var origin = client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 390, Height = 844 } });
        await page.GotoAsync(origin + "/products?q=integration-test-product", new() { WaitUntil = WaitUntilState.Commit });
        await Assertions.Expect(page.Locator(".pwa-search .search-state")).ToContainTextAsync("Searching the catalogue");
        state.Gate.TrySetResult();
        await Assertions.Expect(page.Locator(".pwa-search .search-state")).ToContainTextAsync("The catalogue is out of reach");
        await page.WaitForFunctionAsync("() => document.documentElement.dataset.pwaState === 'ready'");
        await Assertions.Expect(page.Locator(".pwa-search").GetByRole(AriaRole.Button, new() { Name = "Try again", Exact = true })).ToBeEnabledAsync();
        state.Fail = false;
        await page.Locator(".pwa-search").GetByRole(AriaRole.Button, new() { Name = "Try again", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator(".pwa-search .search-result-card")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator(".pwa-search .search-image-placeholder")).ToContainTextAsync("No image recorded");
        await Assertions.Expect(page.Locator(".pwa-search .search-result-image > img")).ToBeHiddenAsync();
        state.ImageUrl = "/search-test-image.png";
        await page.RouteAsync("**/search-test-image.png", route => route.FulfillAsync(new() { ContentType = "image/png", BodyBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "prototype-tena1.png")) }));
        await page.ReloadAsync();
        var image = page.Locator(".pwa-search .search-result-image > img");
        await Assertions.Expect(image).ToBeVisibleAsync();
        await page.WaitForFunctionAsync("() => document.querySelector('.pwa-search .search-result-image > img')?.naturalWidth > 0");
        Assert.True(state.Requests >= 2);
    }

    private static async Task CaptureAsync(IPage page, string name)
    {
        var directory = Environment.GetEnvironmentVariable("DIAPERSCOUT_TEST_ARTIFACTS");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new() { Path = Path.Combine(directory, name), FullPage = true });
    }
    private sealed class SearchTransportState
    {
        public readonly TaskCompletionSource Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Fail = true;
        public int Requests;
        public string ImageUrl = "/deliberately-missing-image.png";
    }
    private sealed class SearchTransportHandler(HttpMessageHandler inner, SearchTransportState state) : DelegatingHandler(inner)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.RequestUri?.AbsolutePath != "/api/v1/products") return await base.SendAsync(request, ct);
            Interlocked.Increment(ref state.Requests);
            await state.Gate.Task.WaitAsync(ct);
            if (state.Fail) return new(HttpStatusCode.ServiceUnavailable);
            var response = await base.SendAsync(request, ct);
            var catalogue = (await response.Content.ReadFromJsonAsync<CatalogueProductSearch>(ct))!;
            response.Content = JsonContent.Create(catalogue with { Products = catalogue.Products.Select(x => x with { ImageUrl = state.ImageUrl }).ToArray() });
            return response;
        }
    }
}
