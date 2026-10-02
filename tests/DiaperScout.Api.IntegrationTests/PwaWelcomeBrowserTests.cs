using Microsoft.Playwright;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class PwaWelcomeBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    [Trait("Category", "Browser")]
    public async Task Welcome_waits_for_real_readiness_and_blocked_storage_does_not_block_guest_entry()
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
        using var client = web.CreateClient(); using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Webkit.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true}); Object.defineProperty(window,'localStorage',{get(){throw new Error('blocked')}});");
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await page.RouteAsync("**/_framework/blazor*js", async route => { await release.Task; await route.ContinueAsync(); });
        try
        {
            await page.GotoAsync(client.BaseAddress!.GetLeftPart(UriPartial.Authority), new() { WaitUntil = WaitUntilState.Commit });
            await Assertions.Expect(page.Locator("#pwa-startup")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator("#pwa-welcome")).ToBeHiddenAsync();
            await Assertions.Expect(page.Locator("html")).ToHaveCSSAsync("background-color", "rgb(250, 245, 236)");
            await Assertions.Expect(page.Locator("#pwa-startup")).ToHaveCSSAsync("background-color", "rgb(250, 245, 236)");
            await Assertions.Expect(page.Locator("meta[name=theme-color]")).ToHaveAttributeAsync("content", "#faf5ec");
            using var manifest = System.Text.Json.JsonDocument.Parse(await client.GetStringAsync("/manifest.webmanifest"));
            Assert.Equal("#faf5ec", manifest.RootElement.GetProperty("background_color").GetString());
            Assert.Equal("#faf5ec", manifest.RootElement.GetProperty("theme_color").GetString());
            release.TrySetResult();
            await Assertions.Expect(page.Locator("#pwa-welcome")).ToBeVisibleAsync();
            await page.Locator("#welcome-continue").ClickAsync();
            await Assertions.Expect(page.Locator(".explore-page")).ToBeVisibleAsync();
        }
        finally { release.TrySetResult(); }
    }

    [Theory]
    [InlineData("webkit", 375, 667)]
    [InlineData("webkit", 390, 844)]
    [InlineData("chromium", 430, 932)]
    [Trait("Category", "Browser")]
    public async Task First_run_choices_use_real_routes_and_guest_completion_survives_launch(string engine, int width, int height)
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
        using var client = web.CreateClient();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await (engine == "webkit" ? playwright.Webkit : playwright.Chromium).LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = width, Height = height } });
        await page.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});");
        var origin = client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        await page.GotoAsync(origin);
        await Assertions.Expect(page.Locator("#pwa-welcome")).ToBeVisibleAsync();
        Assert.Equal("ready", await page.EvaluateAsync<string>("document.documentElement.dataset.pwaState"));
        await Assertions.Expect(page.Locator("meta[name='apple-mobile-web-app-status-bar-style']")).ToHaveAttributeAsync("content", "black-translucent");
        var background = await page.Locator("#pwa-welcome").EvaluateAsync<string>("element => getComputedStyle(element).backgroundImage");
        Assert.Equal("none", background);
        Assert.Equal("rgb(183, 231, 237)", await page.Locator("#pwa-welcome").EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor"));
        Assert.Equal("cover", await page.Locator(".welcome-art").EvaluateAsync<string>("element => getComputedStyle(element).objectFit"));
        await Assertions.Expect(page.Locator(".welcome-art")).ToHaveAttributeAsync("src", "/pwa/welcome-portrait.webp");
        await Assertions.Expect(page.Locator("#pwa-startup")).ToBeHiddenAsync();
        // Desktop engines do not expose an iPhone notch/home-indicator inset.
        // Exercise their occupied space explicitly without changing app behavior.
        if (width == 390) await page.AddStyleTagAsync(new() { Content = ".pwa-welcome { --welcome-safe-top: 47px; --welcome-safe-bottom: 34px; }" });
        await Assertions.Expect(page.Locator(".welcome-art")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator(".welcome-brand")).ToHaveCountAsync(0);
        foreach (var selector in new[] { ".welcome-primary", ".welcome-secondary", "#welcome-continue" })
        {
            var box = await page.Locator(selector).BoundingBoxAsync();
            Assert.NotNull(box); Assert.True(box.Y >= 0 && box.Y + box.Height <= height); Assert.True(box.Height >= 44);
        }
        var artifacts = Environment.GetEnvironmentVariable("DIAPERSCOUT_TEST_ARTIFACTS");
        if (artifacts is not null) { Directory.CreateDirectory(artifacts); await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, $"welcome-{engine}-{width}.png") }); }
        await page.Locator(".welcome-primary").ClickAsync();
        await page.WaitForURLAsync("**/join");
        await Assertions.Expect(page.Locator("#pwa-welcome")).ToHaveCountAsync(0);
        await page.GotoAsync(origin);
        await page.EvaluateAsync("Object.defineProperty(window, 'PublicKeyCredential', {value: function(){}, configurable:true}); Object.defineProperty(navigator, 'credentials', {value:{get:async()=>{throw new DOMException('Cancelled','NotAllowedError')}}, configurable:true});");
        await page.RouteAsync("**/signin/passkey/options", route => route.FulfillAsync(new() { ContentType = "application/json", Body = "{\"requestId\":\"welcome-test\",\"publicKey\":{\"challenge\":\"AQ\",\"allowCredentials\":[]}}" }));
        await page.Locator(".welcome-secondary").ClickAsync();
        await Assertions.Expect(page.Locator(".welcome-signin [data-passkey-message]")).ToContainTextAsync("cancelled");
        Assert.Equal(origin + "/", page.Url);
        await page.Locator("[data-passkey-fallback]").ClickAsync();
        await page.WaitForURLAsync("**/signin");
        await Assertions.Expect(page.Locator("[data-passkey-signin]")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("form[action='/signin/request'] input[type='email']")).ToBeVisibleAsync();
        Assert.True(await page.EvaluateAsync<bool>("!!(document.querySelector('[data-passkey-signin]').compareDocumentPosition(document.querySelector('form[action=\"/signin/request\"]')) & Node.DOCUMENT_POSITION_FOLLOWING)"));
        await page.GotoAsync(origin);
        await page.Locator("#welcome-continue").ClickAsync();
        await Assertions.Expect(page.Locator("#pwa-welcome")).ToBeHiddenAsync();
        await Assertions.Expect(page.Locator(".explore-page")).ToBeVisibleAsync();
        await page.ReloadAsync();
        await page.WaitForFunctionAsync("document.documentElement.dataset.pwaState==='ready'");
        await Assertions.Expect(page.Locator("#pwa-welcome")).ToBeHiddenAsync();
        await page.GotoAsync(origin + "/scan");
        await Assertions.Expect(page.Locator("#pwa-welcome")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }

    [Fact]
    [Trait("Category", "Browser")]
    public async Task Real_authenticated_principal_bypasses_welcome_without_client_auth_storage()
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
        using var client = web.CreateClient(); using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Webkit.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});");
        var origin = client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        await page.GotoAsync(origin + "/signin/development");
        await page.GotoAsync(origin);
        await page.WaitForFunctionAsync("document.documentElement.dataset.pwaState==='ready'");
        await Assertions.Expect(page.Locator("a[href='/signout']")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator("#pwa-welcome")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator(".explore-page")).ToBeVisibleAsync();
        Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('ds-welcome-complete-v1')"));
    }
}
