extern alias DiaperScoutWeb;
using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class AtlasBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData(390, 844, false, true)]
    [InlineData(430, 932, true, true)]
    [InlineData(844, 390, true, true)]
    [InlineData(768, 900, false, false)]
    [InlineData(1280, 900, false, false)]
    [Trait("Category", "Browser")]
    public async Task Empty_loading_failure_and_retry_keep_an_interactive_map(int width, int height, bool webkit, bool standalone)
    {
        using var api = new ObservationApiFactory(fixture);
        var handler = new AtlasHandler([]) { Hold = true, FailFirst = true };
        using var webBase = new PasskeyWebFactory(api);
        using var web = webBase.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddHttpClient<DiaperScoutWeb::DiaperScout.Web.Services.PlaceObservationClient>().ConfigurePrimaryHttpMessageHandler(() => handler)));
        web.UseKestrel(0); using var client = web.CreateClient();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await (webkit ? playwright.Webkit : playwright.Chromium).LaunchAsync(new() { Headless = true });
        var context = await Context(browser, width, height, standalone);
        var page = await context.NewPageAsync(); await page.GotoAsync(client.BaseAddress + "atlas");
        await Assertions.Expect(page.Locator(".place-map[data-map-ready='true']")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Loading product discoveries" })).ToBeVisibleAsync();
        await Zoom(page); // The API request is still held, but the map already responds.
        handler.Release();
        await Assertions.Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("couldn’t load current discoveries");
        await Assertions.Expect(page.Locator(".place-map")).ToBeVisibleAsync();
        await Zoom(page);
        await page.GetByRole(AriaRole.Button, new() { Name = "Try again", Exact = true }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Uncharted territory", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".atlas-discovery-marker")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator(".atlas-field-note img")).ToHaveAttributeAsync("src", "/pwa/guide-map.webp");
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth"));
        Assert.True(await page.EvaluateAsync<bool>("(() => { const m=document.querySelector('.place-map').getBoundingClientRect(),n=document.querySelector('.atlas-field-note').getBoundingClientRect(); return n.width*n.height < m.width*m.height*.35; })()"));
        if (standalone) {
            await Assertions.Expect(page.Locator(".ds-app-header")).ToBeHiddenAsync();
            await Assertions.Expect(page.Locator(".pwa-mobile-nav a[aria-current]")).ToHaveAttributeAsync("href", "/atlas");
            Assert.True(await page.EvaluateAsync<bool>("(() => {const m=document.querySelector('.place-map').getBoundingClientRect(),n=document.querySelector('.pwa-mobile-nav').getBoundingClientRect();return Math.abs(m.top)<1&&Math.abs(m.bottom-n.top)<2;})()"));
        }
        await Evidence(page, $"atlas-empty-{width}");
        // Leaving and returning destroys/recreates the map without duplicate initialisation.
        await page.GotoAsync(client.BaseAddress + "atlas");
        await Assertions.Expect(page.Locator(".place-map[data-map-ready='true']")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        await context.CloseAsync();
    }

    [Theory]
    [InlineData(390, false)]
    [InlineData(430, true)]
    [InlineData(1280, false)]
    [Trait("Category", "Browser")]
    public async Task Real_projection_entries_have_neutral_markers_counts_and_exact_pack_details(int width, bool webkit)
    {
        var one = Place("One discovery", 51.5m, -2m, 1);
        var many = Place("Pharmacy name is not a category", 51.501m, -2.001m, 2);
        var candidate = Place("Unobserved supermarket", 51.502m, -2.002m, 0);
        using var api = new ObservationApiFactory(fixture);
        using var webBase = new PasskeyWebFactory(api);
        using var web = webBase.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddHttpClient<DiaperScoutWeb::DiaperScout.Web.Services.PlaceObservationClient>().ConfigurePrimaryHttpMessageHandler(() => new AtlasHandler([one, many, candidate]))));
        web.UseKestrel(0); using var client = web.CreateClient();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await (webkit ? playwright.Webkit : playwright.Chromium).LaunchAsync(new() { Headless = true });
        var context = await Context(browser, width, 900, width < 700);
        var page = await context.NewPageAsync(); await page.GotoAsync(client.BaseAddress + "atlas");
        await Assertions.Expect(page.Locator(".atlas-discovery-marker")).ToHaveCountAsync(2);
        await Assertions.Expect(page.Locator(".atlas-marker-count")).ToHaveTextAsync("2");
        await Assertions.Expect(page.Locator(".atlas-field-note")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator(".atlas-selected-place")).ToHaveCountAsync(0);
        await page.Locator($".atlas-discovery-marker[title='{many.Place.Name}']").ClickAsync();
        await Assertions.Expect(page.Locator("#place-heading")).ToHaveTextAsync(many.Place.Name);
        await Assertions.Expect(page.Locator(".atlas-evidence-summary")).ToHaveTextAsync("2 observations · 1 pack reported");
        await Assertions.Expect(page.Locator(".atlas-stock-note")).ToContainTextAsync("not a guarantee of current stock");
        await Assertions.Expect(page.Locator(".atlas-observation")).ToHaveCountAsync(2);
        await Assertions.Expect(page.Locator(".atlas-observation a").First).ToHaveAttributeAsync("href", many.Observations[0].ProductUrl);
        await Assertions.Expect(page.Locator(".atlas-observation").First).ToContainTextAsync("Pack of 12");
        await Assertions.Expect(page.Locator(".atlas-observation").First).ToContainTextAsync("18.25 GBP");
        await Assertions.Expect(page.Locator(".atlas-observation time").First).ToHaveAttributeAsync("datetime", many.Observations[0].ObservedAtUtc.ToString("O"));
        await Evidence(page, $"atlas-selected-{width}");
        await page.GetByRole(AriaRole.Button, new() { Name = "Close place details", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Discoveries (2)", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator(".atlas-place-choice")).ToHaveCountAsync(2);
        await page.Locator(".atlas-place-choice").Filter(new() { HasText = one.Place.Name }).ClickAsync();
        await Assertions.Expect(page.Locator("#place-heading")).ToHaveTextAsync(one.Place.Name);
        await Assertions.Expect(page.Locator(".atlas-evidence-summary")).ToHaveTextAsync("1 observation · 1 pack reported");
        await page.GetByRole(AriaRole.Button, new() { Name = "Close place details", Exact = true }).ClickAsync();
        if (await page.Locator(".leaflet-popup-close-button").CountAsync() > 0) await page.Locator(".leaflet-popup-close-button").ClickAsync();
        await page.Locator(".place-map").FocusAsync();
        for (var i = 0; i < 20; i++) { await page.Locator(".place-map").PressAsync("ArrowRight"); await page.WaitForTimeoutAsync(180); }
        await Assertions.Expect(page.Locator(".atlas-field-note")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".atlas-discovery-marker")).ToHaveCountAsync(2); // Panning changes the note, not the evidence dataset.
        // Deep linking preserves exact place selection across refresh.
        await page.GotoAsync(client.BaseAddress + "atlas?locationId=" + many.Place.Id);
        await Assertions.Expect(page.Locator("#place-heading")).ToHaveTextAsync(many.Place.Name);
        await Assertions.Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth"));
        await context.CloseAsync();
    }

    internal static AtlasPlace Place(string name, decimal latitude, decimal longitude, int count) {
        var place = new PlaceItem(Guid.NewGuid(), name, "1 Test Street", "Testville", "ZZ1 1ZZ", "ZZ", latitude, longitude);
        var pack = Guid.NewGuid(); var variant = Guid.NewGuid(); var product = Guid.NewGuid(); var size = Guid.NewGuid();
        var observations = Enumerable.Range(0, count).Select(i => new PlaceProductObservation(Guid.NewGuid(), product, variant, size, pack,
            "Test-only exact pack", $"/products/test-only?variantId={variant}&packTypeId={pack}", "Large", 12,
            new DateTimeOffset(2026, 10, 2, 10 + i, 0, 0, TimeSpan.Zero), 18.25m, "GBP")).ToArray();
        return new(place, count == 0 ? 0 : 1, observations.LastOrDefault()?.ObservedAtUtc ?? DateTimeOffset.UtcNow, observations);
    }
    internal static async Task<IBrowserContext> Context(IBrowser browser, int width, int height, bool standalone) {
        var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = width, Height = height }, ServiceWorkers = ServiceWorkerPolicy.Block });
        if (standalone) await context.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});");
        await UseTestTiles(context);
        return context;
    }
    // No external tile crawling. Synthetic test tiles exercise Leaflet geometry only.
    internal static Task UseTestTiles(IBrowserContext context) => context.RouteAsync("https://tile.openstreetmap.org/**", route => route.FulfillAsync(new() { Status = 200, ContentType = "image/svg+xml", Body = "<svg xmlns='http://www.w3.org/2000/svg' width='256' height='256'><rect width='256' height='256' fill='#e7ecdf'/><path d='M0 70H256M85 0V256M0 230L256 30' fill='none' stroke='#fffcf6' stroke-width='8'/><text x='8' y='20' fill='#72806d' font-size='10'>Synthetic test tile</text></svg>" }));
    private static async Task Zoom(IPage page) {
        var before = await page.Locator(".leaflet-tile").First.GetAttributeAsync("src");
        var zoom = int.Parse(new Uri(before!).AbsolutePath.Split('/')[1]);
        await page.Locator(".leaflet-control-zoom-in").ClickAsync();
        await page.WaitForFunctionAsync("zoom => [...document.querySelectorAll('.leaflet-tile')].some(t => Number(new URL(t.src).pathname.split('/')[1]) === zoom)", zoom + 1);
    }
    internal static async Task Evidence(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("DIAPERSCOUT_BROWSER_EVIDENCE");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        await page.WaitForTimeoutAsync(400); // Capture settled map movement, not an intermediate animation frame.
        await page.ScreenshotAsync(new() { Path = Path.Combine(directory, name + ".png"), FullPage = true });
    }
    private sealed class AtlasHandler(AtlasPlace[] places) : HttpMessageHandler {
        private readonly TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int requests;
        public bool Hold { get; init; }
        public bool FailFirst { get; init; }
        public void Release() => gate.TrySetResult();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) {
            if (Hold) await gate.Task.WaitAsync(ct);
            if (FailFirst && Interlocked.Increment(ref requests) == 1) return new(HttpStatusCode.ServiceUnavailable);
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(places) };
        }
    }
}
