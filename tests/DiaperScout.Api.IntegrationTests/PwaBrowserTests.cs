using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Playwright;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class PwaBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Manifest_icons_worker_and_cache_headers_are_served()
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api);
        using var client = web.CreateClient();
        var response = await client.GetAsync("/manifest.webmanifest");
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.CacheControl!.NoCache);
        var manifest = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("DiaperScout", manifest.GetProperty("name").GetString());
        Assert.Equal("standalone", manifest.GetProperty("display").GetString());
        Assert.Equal("/", manifest.GetProperty("scope").GetString());
        Assert.Equal("/", manifest.GetProperty("start_url").GetString());
        Assert.False(manifest.TryGetProperty("orientation", out _));
        foreach (var icon in manifest.GetProperty("icons").EnumerateArray())
        {
            var bytes = await client.GetByteArrayAsync(icon.GetProperty("src").GetString());
            Assert.Equal(new byte[] { 137, 80, 78, 71 }, bytes[..4]);
        }
        var worker = await client.GetAsync("/service-worker.js");
        worker.EnsureSuccessStatusCode();
        Assert.True(worker.Headers.CacheControl!.NoCache);
        Assert.NotEmpty(await client.GetByteArrayAsync("/pwa/guide-map.webp"));
    }

    [Theory]
    [InlineData(390)]
    [InlineData(430)]
    [InlineData(768)]
    [InlineData(1280)]
    [Trait("Category", "Browser")]
    public async Task Shell_search_and_detail_remain_usable_at_realistic_widths(int width)
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api);
        web.UseKestrel(0);
        using var client = web.CreateClient();
        var origin = client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = width, Height = 900 } });
        await AtlasBrowserTests.UseTestTiles(context);
        var page = await context.NewPageAsync();
        page.PageError += (_, error) => System.Console.WriteLine("PWA browser error: " + error);
        await page.GotoAsync(origin);
        await Ready(page);
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Let’s explore." })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#pwa-startup")).ToBeHiddenAsync();
        await Assertions.Expect(page.Locator(".pwa-mobile-nav")).ToBeVisibleAsync(new() { Visible = width <= 700 });
        Assert.Equal(new[] { "Explore", "Products", "Scan", "Atlas", "Backpack" }, await page.Locator(".pwa-mobile-nav a span").AllTextContentsAsync());
        Assert.True(await Fits(page));
        await Screenshot(page, $"explore-{width}");
        await page.Locator("#explore-query").FillAsync("Integration Test Product");
        await page.GetByRole(AriaRole.Button, new() { Name = "Search the catalogue", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator(".catalogue-product-row")).ToHaveCountAsync(1);
        await Ready(page);
        Assert.Equal("Integration Test Product", await page.Locator(width <= 700 ? "#pwa-search-query" : "#catalogue-query").InputValueAsync());
        if (width <= 700) await Assertions.Expect(page.Locator(".pwa-search .search-filters")).ToHaveCountAsync(0);
        Assert.True(await Fits(page));
        await Screenshot(page, $"search-{width}");
        var detailLink = page.Locator(width <= 700 ? ".pwa-search .search-result-card" : ".catalogue-product-identity h2 a");
        await detailLink.ClickAsync();
        await Assertions.Expect(page.Locator(".ds-size-pill:visible")).ToHaveCountAsync(1);
        await Screenshot(page, $"product-{width}");
        Assert.True(await Fits(page), await page.EvaluateAsync<string>("JSON.stringify([...document.querySelectorAll('*')].filter(e=>e.getBoundingClientRect().right>innerWidth+1).map(e=>({tag:e.tagName,class:e.className,width:e.getBoundingClientRect().width})).slice(0,20))"));
        await page.GotoAsync(origin + "/atlas");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Uncharted territory", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".place-map[data-map-ready='true']")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".atlas-discovery-marker")).ToHaveCountAsync(0);
        Assert.True(await Fits(page));
        await page.GotoAsync(origin + "/backpack");
        await Assertions.Expect(page.Locator(".pwa-coming-soon")).ToHaveTextAsync("Coming soon");
        Assert.True(await Fits(page));
        await page.GotoAsync(origin + "/admin/commerce-plugins");
        Assert.DoesNotContain("data-plugin-id", await page.ContentAsync());
    }

    [Fact]
    [Trait("Category", "Browser")]
    public async Task Worker_caches_only_public_static_assets_and_serves_an_honest_offline_state()
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
        using var client = web.CreateClient();
        var origin = client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync();
        await AtlasBrowserTests.UseTestTiles(context);
        var page = await context.NewPageAsync();
        page.PageError += (_, error) => System.Console.WriteLine("PWA browser error: " + error);
        await page.GotoAsync(origin + "/products"); await Ready(page);
        await page.EvaluateAsync("async () => { await navigator.serviceWorker.ready; }");
        await page.WaitForFunctionAsync("() => !!navigator.serviceWorker.controller");
        await page.GotoAsync(origin + "/signin");
        await page.GotoAsync(origin + "/admin/commerce-plugins");
        await page.GotoAsync(origin + "/about");
        await page.GotoAsync(origin + "/atlas"); await Ready(page);
        await page.GotoAsync(origin + "/contribute/product?gtin=96385074"); await Ready(page);
        await page.EvaluateAsync("async () => { await fetch('/api/v1/places/atlas'); await fetch('/api/v1/places/?query=shop'); }");
        var urls = await page.EvaluateAsync<string[]>("async () => { const out=[]; for(const key of await caches.keys()) for(const request of await (await caches.open(key)).keys()) out.push(new URL(request.url).pathname); return out; }");
        Assert.Contains("/pwa/offline.html", urls);
        Assert.All(urls, url => Assert.True(url.StartsWith("/pwa/") || url == "/images/brand/DiaperScout.svg", url));
        Assert.DoesNotContain(urls, url => url.StartsWith("/products") || url.StartsWith("/signin") || url.StartsWith("/admin") || url.StartsWith("/api") || url.StartsWith("/_blazor"));
        await context.SetOfflineAsync(true);
        await page.GotoAsync(origin + "/products");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading)).ToHaveTextAsync("We can’t reach the map.");
        await Assertions.Expect(page.Locator("#connection")).ToContainTextAsync("offline");
        Assert.Equal(0, await page.Locator(".catalogue-product-row").CountAsync());
        await Screenshot(page, "offline");
        await context.SetOfflineAsync(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Try again" }).ClickAsync();
        await Assertions.Expect(page.Locator(".catalogue-product-row")).ToHaveCountAsync(1);
    }

    [Fact]
    [Trait("Category", "Browser")]
    public async Task Standalone_startup_long_wait_and_failure_have_no_fake_progress_or_permanent_cover()
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
        using var client = web.CreateClient();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 390, Height = 844 } });
        await page.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});");
        await page.Clock.InstallAsync();
        await page.RouteAsync("**/_framework/blazor*js", route => route.FulfillAsync(new() { ContentType = "text/javascript", Body = "window.Blazor={start:()=>new Promise(()=>{}),addEventListener:()=>{}};" }));
        await page.GotoAsync(client.BaseAddress!.GetLeftPart(UriPartial.Authority));
        await Assertions.Expect(page.Locator("#pwa-startup")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#pwa-startup-title")).ToHaveTextAsync("Preparing your map…");
        Assert.DoesNotContain("%", await page.Locator("#pwa-startup").InnerTextAsync());
        await Screenshot(page, "startup");
        await page.Clock.FastForwardAsync(9000);
        await Assertions.Expect(page.Locator("#pwa-startup-description")).ToContainTextAsync("little longer");
        await page.Clock.FastForwardAsync(40000);
        await Assertions.Expect(page.Locator("#pwa-startup-title")).ToHaveTextAsync("We couldn’t connect.");
        await Assertions.Expect(page.Locator("#pwa-startup-retry")).ToBeVisibleAsync();
        await Screenshot(page, "startup-failure");
        await page.EvaluateAsync("DiaperScoutPwa.markInteractiveReady()");
        await Assertions.Expect(page.Locator("#pwa-startup")).ToBeHiddenAsync();
    }

    [Fact]
    [Trait("Category", "Browser")]
    public async Task Install_is_explicit_dismissal_is_remembered_and_update_requires_confirmation()
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
        using var client = web.CreateClient();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.AddInitScriptAsync("""
            window.updateMessages=[];
            Object.defineProperty(navigator,'serviceWorker',{value:{
              controller:{},register:async()=>({waiting:{postMessage:m=>updateMessages.push(m)},addEventListener:()=>{}}),addEventListener:()=>{}
            }});
            """);
        await page.GotoAsync(client.BaseAddress!.GetLeftPart(UriPartial.Authority) + "/about");
        await Ready(page);
        await Assertions.Expect(page.Locator("[data-pwa-install]")).ToBeHiddenAsync();
        await page.EvaluateAsync("""
            () => { const e=new Event('beforeinstallprompt',{cancelable:true}); e.prompt=async()=>{window.installPromptCalls=(window.installPromptCalls||0)+1};e.userChoice=Promise.resolve({outcome:'dismissed'});dispatchEvent(e); }
            """);
        await Assertions.Expect(page.Locator("[data-pwa-install]")).ToBeVisibleAsync();
        Assert.Equal(0, await page.EvaluateAsync<int>("window.installPromptCalls||0"));
        await page.Locator("[data-pwa-install]").ClickAsync();
        await Assertions.Expect(page.Locator("[data-pwa-install]")).ToBeHiddenAsync();
        Assert.Equal("true", await page.EvaluateAsync<string>("localStorage.getItem('ds-install-dismissed')"));
        await Assertions.Expect(page.Locator("#pwa-update")).ToBeVisibleAsync();
        Assert.Equal(0, await page.EvaluateAsync<int>("updateMessages.length"));
        page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
        await page.Locator("[data-pwa-update]").ClickAsync();
        Assert.Equal("ACTIVATE_UPDATE", await page.EvaluateAsync<string>("updateMessages[0].type"));
    }

    private static Task Ready(IPage page) => page.WaitForFunctionAsync("() => document.documentElement.dataset.pwaState === 'ready'");
    private static Task<bool> Fits(IPage page) => page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth");
    private static async Task Screenshot(IPage page, string name)
    {
        var directory = Environment.GetEnvironmentVariable("DIAPERSCOUT_TEST_ARTIFACTS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new() { Path = Path.Combine(directory, $"pwa-{name}.png"), FullPage = true });
    }
}
