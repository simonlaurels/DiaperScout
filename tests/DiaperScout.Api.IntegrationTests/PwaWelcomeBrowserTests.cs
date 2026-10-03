using Microsoft.Playwright;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class PwaWelcomeBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData("webkit")]
    [InlineData("chromium")]
    [Trait("Category", "Browser")]
    public async Task Welcome_signin_acknowledges_before_cold_request_and_recovers(string engine)
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
        using var client = web.CreateClient(); using var playwright = await Playwright.CreateAsync();
        await using var browser = await (engine == "webkit" ? playwright.Webkit : playwright.Chromium).LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 390, Height = 844 }, ServiceWorkers = ServiceWorkerPolicy.Block });
        await page.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});Object.defineProperty(window,'PublicKeyCredential',{value:function(){},configurable:true});Object.defineProperty(navigator,'credentials',{value:{get:async()=>{throw new DOMException('Cancelled','NotAllowedError')}},configurable:true});");
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = 0;
        await page.RouteAsync("**/signin/passkey/options", async route => {
            Interlocked.Increment(ref requests); requested.TrySetResult(); await release.Task;
            await route.FulfillAsync(new() { Status = 503, ContentType = "application/json", Body = "{\"message\":\"Please try again.\"}" });
        });
        try
        {
            await page.GotoAsync(client.BaseAddress!.GetLeftPart(UriPartial.Authority));
            var button = page.Locator(".welcome-secondary");
            await Assertions.Expect(page.Locator("#pwa-welcome")).ToBeVisibleAsync();
            await Assertions.Expect(button).ToHaveTextAsync("Sign in");
            var before = await button.BoundingBoxAsync();
            var cardBefore = await page.Locator(".welcome-card").BoundingBoxAsync();
            await button.PressAsync("Enter");
            await Assertions.Expect(button).ToHaveTextAsync("Signing in…");
            await Assertions.Expect(button).ToHaveAttributeAsync("aria-busy", "true");
            await Assertions.Expect(button).ToBeDisabledAsync();
            await Assertions.Expect(button.Locator(".welcome-auth-spinner")).ToHaveAttributeAsync("aria-hidden", "true");
            await Assertions.Expect(page.Locator(".welcome-primary")).ToHaveTextAsync("Create account");
            await page.EvaluateAsync("document.querySelector('.welcome-secondary').dispatchEvent(new MouseEvent('click',{bubbles:true}));document.querySelector('.welcome-primary').click();");
            await Assertions.Expect(button).ToHaveTextAsync("Signing in…");
            await WaitForRequestAsync(requested.Task, page);
            Assert.Equal(1, Volatile.Read(ref requests));
            Assert.EndsWith("/", page.Url);
            var after = await button.BoundingBoxAsync();
            var cardAfter = await page.Locator(".welcome-card").BoundingBoxAsync();
            AssertSameBounds(before, after); AssertSameBounds(cardBefore, cardAfter);
            await Assertions.Expect(page.Locator(".welcome-art")).ToHaveAttributeAsync("src", "/pwa/welcome-portrait.webp");
            release.TrySetResult();
            await Assertions.Expect(button).ToHaveTextAsync("Sign in");
            await Assertions.Expect(button).ToBeEnabledAsync();
            await Assertions.Expect(button).Not.ToHaveAttributeAsync("aria-busy", "true");
            await Assertions.Expect(page.Locator(".welcome-signin [data-passkey-message]")).ToHaveTextAsync("Please try again.");
            await Assertions.Expect(page.Locator("[data-passkey-fallback]")).ToBeVisibleAsync();
            await page.Locator("#welcome-continue").ClickAsync();
            await Assertions.Expect(page.Locator("#pwa-welcome")).ToBeHiddenAsync();
            Assert.Equal("yes", await page.EvaluateAsync<string>("localStorage.getItem('ds-welcome-complete-v1')"));
        }
        finally { release.TrySetResult(); }
    }

    [Theory]
    [InlineData("webkit")]
    [InlineData("chromium")]
    [Trait("Category", "Browser")]
    public async Task Welcome_join_keeps_existing_link_and_resets_on_return(string engine)
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
        using var client = web.CreateClient(); using var playwright = await Playwright.CreateAsync();
        await using var browser = await (engine == "webkit" ? playwright.Webkit : playwright.Chromium).LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 390, Height = 844 }, ServiceWorkers = ServiceWorkerPolicy.Block });
        await page.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});");
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = 0;
        await page.RouteAsync("**/join", async route => { Interlocked.Increment(ref requests); requested.TrySetResult(); await release.Task; await route.ContinueAsync(); });
        try
        {
            var origin = client.BaseAddress!.GetLeftPart(UriPartial.Authority);
            await page.GotoAsync(origin);
            await Assertions.Expect(page.Locator("#pwa-welcome")).ToBeVisibleAsync();
            var action = page.Locator(".welcome-primary");
            await Assertions.Expect(action).ToHaveTextAsync("Create account");
            await Assertions.Expect(action).ToHaveAttributeAsync("href", "/join");
            var before = await action.BoundingBoxAsync();
            var click = action.ClickAsync();
            await Assertions.Expect(action).ToHaveTextAsync("Getting ready…");
            await Assertions.Expect(action).ToHaveAttributeAsync("aria-disabled", "true");
            await Assertions.Expect(action.Locator(".welcome-auth-spinner")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".welcome-secondary")).ToHaveTextAsync("Sign in");
            await page.EvaluateAsync("document.querySelector('.welcome-primary').click();document.querySelector('.welcome-secondary').click();");
            AssertSameBounds(before, await action.BoundingBoxAsync());
            await page.WaitForFunctionAsync("document.querySelector('.welcome-primary').getAttribute('aria-busy')==='true'");
            await WaitForRequestAsync(requested.Task, page);
            Assert.Equal(1, Volatile.Read(ref requests));
            release.TrySetResult(); await click;
            await page.WaitForURLAsync("**/join");
            await page.GoBackAsync();
            await Assertions.Expect(page.Locator("#pwa-welcome")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".welcome-primary")).ToHaveTextAsync("Create account");
            await Assertions.Expect(page.Locator(".welcome-primary")).Not.ToHaveAttributeAsync("aria-busy", "true");
            Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('ds-welcome-complete-v1')"));
        }
        finally { release.TrySetResult(); }
    }

    private static async Task WaitForRequestAsync(Task requested, IPage page)
    {
        await requested.WaitAsync(TimeSpan.FromSeconds(30));
    }

    private static void AssertSameBounds(LocatorBoundingBoxResult? before, LocatorBoundingBoxResult? after)
    {
        Assert.NotNull(before); Assert.NotNull(after);
        Assert.Equal((before.X, before.Y, before.Width, before.Height), (after.X, after.Y, after.Width, after.Height));
    }

    [Theory]
    [InlineData("webkit")]
    [InlineData("chromium")]
    [Trait("Category", "Browser")]
    public async Task Welcome_signin_keeps_feedback_until_successful_navigation_completes(string engine)
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
        using var client = web.CreateClient(); using var playwright = await Playwright.CreateAsync();
        await using var browser = await (engine == "webkit" ? playwright.Webkit : playwright.Chromium).LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 390, Height = 844 }, ServiceWorkers = ServiceWorkerPolicy.Block });
        await page.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});Object.defineProperty(window,'PublicKeyCredential',{value:function(){},configurable:true});Object.defineProperty(navigator,'credentials',{value:{get:async()=>({id:'test',rawId:new Uint8Array([1]).buffer,type:'public-key',getClientExtensionResults:()=>({}),response:{clientDataJSON:new Uint8Array([1]).buffer,authenticatorData:new Uint8Array([1]).buffer,signature:new Uint8Array([1]).buffer,userHandle:null}})},configurable:true});");
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var verified = 0;
        var captured = new TaskCompletionSource<string[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        await page.ExposeFunctionAsync("captureWelcomeFeedback", (string text, string disabled, string busy, string spinner) => captured.TrySetResult([text, disabled, busy, spinner]));
        await page.AddInitScriptAsync("document.addEventListener('diaperscout:welcome-auth-settled',event=>{if(event.detail.navigating){const button=document.querySelector('.welcome-secondary');button.click();captureWelcomeFeedback(button.textContent,button.getAttribute('aria-disabled'),button.getAttribute('aria-busy'),String(button.querySelector('.welcome-auth-spinner').getBoundingClientRect().width>0));}});");
        await page.RouteAsync("**/signin/passkey/options", route => route.FulfillAsync(new() { ContentType = "application/json", Body = "{\"requestId\":\"welcome-success\",\"publicKey\":{\"challenge\":\"AQ\",\"allowCredentials\":[]}}" }));
        await page.RouteAsync("**/signin/passkey/verify", async route => {
            Interlocked.Increment(ref verified);
            Assert.Contains("welcome-success", route.Request.PostData);
            await route.FulfillAsync(new() { ContentType = "application/json", Body = "{\"redirect\":\"/signin\"}" });
        });
        await page.RouteAsync("**/signin", async route => { requested.TrySetResult(); await release.Task; await route.ContinueAsync(); });
        try
        {
            await page.GotoAsync(client.BaseAddress!.GetLeftPart(UriPartial.Authority));
            await Assertions.Expect(page.Locator("#pwa-welcome")).ToBeVisibleAsync();
            await page.Locator(".welcome-secondary").ClickAsync();
            await WaitForRequestAsync(requested.Task, page);
            // Capture the source DOM in the completion event: automation cannot
            // inspect its execution context while full-document navigation is pending.
            var pending = await captured.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(new[] { "Signing in…", "true", "true", "true" }, pending);
            Assert.Equal(1, Volatile.Read(ref verified));
            release.TrySetResult();
            await page.WaitForURLAsync("**/signin");
            await Assertions.Expect(page.Locator("form[action='/signin/request'] input[type='email']")).ToBeVisibleAsync();
            await page.GoBackAsync();
            await Assertions.Expect(page.Locator("#pwa-welcome")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".welcome-secondary")).ToHaveTextAsync("Sign in");
            await Assertions.Expect(page.Locator(".welcome-secondary")).ToBeEnabledAsync();
        }
        finally { release.TrySetResult(); }
    }

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
        await Assertions.Expect(page.Locator(".welcome-secondary")).ToHaveTextAsync("Sign in");
        await Assertions.Expect(page.Locator(".welcome-secondary")).ToBeEnabledAsync();
        await Assertions.Expect(page.Locator(".welcome-auth-spinner")).ToHaveCountAsync(0);
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
