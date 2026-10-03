extern alias DiaperScoutWeb;

using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class PwaReliabilityTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData("chromium", false)]
    [InlineData("webkit", false)]
    [InlineData("chromium", true)]
    [InlineData("webkit", true)]
    [Trait("Category", "Browser")]
    public async Task Static_standalone_brand_is_visible_before_external_scripts_or_styles(string engine, bool withoutStyles)
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
        using var client = web.CreateClient();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await (engine == "webkit" ? playwright.Webkit : playwright.Chromium).LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 390, Height = 844 } });
        await page.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});");
        // Hold the parser at the framework script. app.js cannot run. External CSS
        // is empty so the assertion also verifies the critical inline startup layout.
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (withoutStyles) await page.RouteAsync("**/*.css", route => route.FulfillAsync(new() { ContentType = "text/css", Body = "" }));
        await page.RouteAsync("**/_framework/blazor*js", async route => { await release.Task; await route.AbortAsync(); });
        try
        {
            await page.GotoAsync(client.BaseAddress!.GetLeftPart(UriPartial.Authority), new() { WaitUntil = WaitUntilState.Commit });
            await Assertions.Expect(page.Locator("#pwa-startup-title")).ToHaveTextAsync("Preparing your map…");
            await Assertions.Expect(page.Locator("#pwa-startup")).ToBeVisibleAsync();
            Assert.True(await page.EvaluateAsync<bool>("typeof DiaperScoutPwa==='undefined' && typeof Blazor==='undefined'"));
            Assert.Equal("rgb(250, 245, 236)", await page.EvaluateAsync<string>("getComputedStyle(document.documentElement).backgroundColor"));
            var box = await page.Locator("#pwa-startup").BoundingBoxAsync();
            Assert.Equal(390, box!.Width); Assert.Equal(844, box.Height);
            Assert.False(await page.Locator("#pwa-startup-retry").IsVisibleAsync());
        }
        finally { release.TrySetResult(); }
    }

    [Theory]
    [InlineData("/products")]
    [InlineData("/atlas")]
    [Trait("Category", "Browser")]
    public async Task Initial_branded_document_streams_while_api_is_still_pending(string path)
    {
        using var api = new ObservationApiFactory(fixture);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var original = new PasskeyWebFactory(api);
        using var web = original.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient<DiaperScoutWeb::DiaperScout.Web.Services.ProductCatalogueClient>()
                .ConfigurePrimaryHttpMessageHandler(() => new PendingApiHandler(api.Server.CreateHandler(), entered, release));
            services.AddHttpClient<DiaperScoutWeb::DiaperScout.Web.Services.PlaceObservationClient>()
                .ConfigurePrimaryHttpMessageHandler(() => new PendingApiHandler(api.Server.CreateHandler(), entered, release));
        }));
        web.UseKestrel(0); using var client = web.CreateClient();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Webkit.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await AtlasBrowserTests.UseTestTiles(page.Context);
        await page.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});");
        try
        {
            var navigation = page.GotoAsync(client.BaseAddress!.GetLeftPart(UriPartial.Authority) + path, new() { WaitUntil = WaitUntilState.Commit });
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await navigation.WaitAsync(TimeSpan.FromSeconds(10));
            if (path == "/atlas") {
                // Atlas starts independently of discovery data; the usable map replaces the startup cover.
                await Assertions.Expect(page.Locator(".place-map[data-map-ready='true']")).ToBeVisibleAsync();
                await Assertions.Expect(page.Locator(".atlas-status")).ToContainTextAsync("Loading product discoveries");
                await Assertions.Expect(page.Locator("#pwa-startup")).ToBeHiddenAsync();
            } else {
                await Assertions.Expect(page.Locator("#pwa-startup")).ToBeVisibleAsync();
                await Assertions.Expect(page.Locator("#pwa-startup-title")).ToHaveTextAsync("Preparing your map…");
            }
            Assert.False(release.Task.IsCompleted);
            release.TrySetResult();
            await page.WaitForFunctionAsync("document.documentElement.dataset.pwaState==='ready'");
            await Assertions.Expect(page.Locator("#pwa-startup")).ToBeHiddenAsync();
            await Assertions.Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        }
        finally { release.TrySetResult(); }
    }

    [Theory]
    [InlineData("chromium")]
    [InlineData("webkit")]
    [Trait("Category", "Browser")]
    public async Task Lost_circuit_is_explicit_and_does_not_reload_or_retry_on_visibility(string engine)
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
        using var client = web.CreateClient(); using var playwright = await Playwright.CreateAsync();
        await using var browser = await (engine == "webkit" ? playwright.Webkit : playwright.Chromium).LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(client.BaseAddress!.GetLeftPart(UriPartial.Authority));
        await page.WaitForFunctionAsync("document.documentElement.dataset.pwaState==='ready'");
        await page.EvaluateAsync("""
            () => {
                window.reloadSentinel = 'preserved'; window.reconnectCalls=0; window.resumeCalls=0;
                Blazor.reconnect=async()=>{window.reconnectCalls++;return false;};
                Blazor.resumeCircuit=async()=>{window.resumeCalls++;return false;};
                const modal=document.getElementById('components-reconnect-modal');
                modal.className='components-reconnect-rejected';
                modal.dispatchEvent(new CustomEvent('components-reconnect-state-changed',{detail:{state:'rejected'}}));
                document.dispatchEvent(new Event('visibilitychange'));
            }
            """);
        await Assertions.Expect(page.GetByText("This session is no longer available.", new() { Exact = false })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#components-reload-button")).ToBeVisibleAsync();
        Assert.Equal("preserved", await page.EvaluateAsync<string>("reloadSentinel"));
        Assert.Equal(0, await page.EvaluateAsync<int>("reconnectCalls+resumeCalls"));
        await page.EvaluateAsync("""
            () => { const m=document.getElementById('components-reconnect-modal'); m.className='components-reconnect-failed';
            m.dispatchEvent(new CustomEvent('components-reconnect-state-changed',{detail:{state:'failed'}}));
            document.dispatchEvent(new Event('visibilitychange')); }
            """);
        Assert.Equal(0, await page.EvaluateAsync<int>("reconnectCalls+resumeCalls"));
        await page.Locator("#components-reconnect-button").ClickAsync();
        await Assertions.Expect(page.GetByText("This session is no longer available.", new() { Exact = false })).ToBeVisibleAsync();
        Assert.Equal(1, await page.EvaluateAsync<int>("reconnectCalls"));
        Assert.Equal(1, await page.EvaluateAsync<int>("resumeCalls"));
        Assert.Equal("preserved", await page.EvaluateAsync<string>("reloadSentinel"));
        await page.Locator("#components-reload-button").ClickAsync();
        await page.WaitForFunctionAsync("typeof reloadSentinel==='undefined' && document.documentElement.dataset.pwaState==='ready'");
    }

    [Theory]
    [InlineData("chromium")]
    [InlineData("webkit")]
    [Trait("Category", "Browser")]
    public async Task Proposal_does_not_accept_input_until_hydration_and_draft_restore_complete(string engine)
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
        using var client = web.CreateClient(); using var playwright = await Playwright.CreateAsync();
        await using var browser = await (engine == "webkit" ? playwright.Webkit : playwright.Chromium).LaunchAsync(new() { Headless = true });
        // Native request gates cannot reliably intercept worker-owned requests.
        // Worker-enabled behavior is covered by the separate PWA suite and production matrix.
        var page = await browser.NewPageAsync(new() { ServiceWorkers = ServiceWorkerPolicy.Block });
        var framework = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var draft = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var draftRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var browserDiagnostics = new List<string>();
        page.PageError += (_, error) => browserDiagnostics.Add(error);
        page.RequestFailed += (_, request) => browserDiagnostics.Add(request.Url + ": " + request.Failure);
        page.Response += (_, response) => { if (response.Status >= 400) browserDiagnostics.Add(response.Url + ": " + response.Status); };
        await page.RouteAsync("**/_framework/blazor*js", async route => { await framework.Task; await route.ContinueAsync(); });
        await page.RouteAsync("**/js/contribution-draft*", async route => { draftRequested.TrySetResult(); await draft.Task; await route.ContinueAsync(); });
        try
        {
            await page.GotoAsync(client.BaseAddress!.GetLeftPart(UriPartial.Authority) + "/contribute/product?gtin=96385074", new() { WaitUntil = WaitUntilState.Commit });
            var brand = page.Locator("#proposal-brand");
            await Assertions.Expect(brand).ToBeVisibleAsync();
            await Assertions.Expect(brand).ToBeDisabledAsync();
            await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Continue to pack", Exact = true })).ToBeDisabledAsync();
            framework.TrySetResult();
            // Commit only guarantees response headers. Wait for document/state completion,
            // the same prerequisite as manual Blazor startup, before timing module readiness.
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            var restored = await Task.WhenAny(draftRequested.Task, Task.Delay(TimeSpan.FromSeconds(30)));
            Assert.True(restored == draftRequested.Task, "Draft module never requested. State: " +
                await page.EvaluateAsync<string>("document.documentElement.dataset.pwaState || 'none'") + "; " + string.Join("; ", browserDiagnostics));
            // A circuit can be open while this component's asynchronous draft restore
            // is pending. Only component readiness can safely enable its controls.
            await Assertions.Expect(brand).ToBeDisabledAsync();
            draft.TrySetResult();
            await Assertions.Expect(brand).ToBeEnabledAsync();
            await brand.FillAsync("Hydration brand");
            await page.Locator("#proposal-name").FillAsync("Hydration product");
            await page.GetByRole(AriaRole.Button, new() { Name = "Continue to pack", Exact = true }).ClickAsync();
            await Assertions.Expect(page.Locator("#proposal-size")).ToBeVisibleAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Review proposal", Exact = true }).ClickAsync();
            await Assertions.Expect(page.Locator(".contribution-review")).ToContainTextAsync("Hydration brand");
            await Assertions.Expect(page.Locator(".contribution-review")).ToContainTextAsync("Hydration product");
            await Assertions.Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        }
        finally { framework.TrySetResult(); draft.TrySetResult(); }
    }

    [Fact]
    public async Task Shared_crypto_does_not_share_SignalR_connection_state_between_two_Web_processes()
    {
        using var api = new ObservationApiFactory(fixture);
        using var first = new PasskeyWebFactory(api); first.UseKestrel(0);
        using var second = new PasskeyWebFactory(api); second.UseKestrel(0);
        using var clientA = first.CreateClient(); using var clientB = second.CreateClient();
        var protectorA = first.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("routing-regression");
        var protectorB = second.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("routing-regression");
        Assert.Equal("shared crypto", protectorB.Unprotect(protectorA.Protect("shared crypto")));
        using var negotiation = await clientA.PostAsync("/_blazor/negotiate?negotiateVersion=1", null);
        negotiation.EnsureSuccessStatusCode();
        var data = await negotiation.Content.ReadFromJsonAsync<JsonElement>();
        var token = Uri.EscapeDataString(data.GetProperty("connectionToken").GetString()!);
        using var wronglyRouted = new ClientWebSocket();
        wronglyRouted.Options.CollectHttpResponseDetails = true;
        await Assert.ThrowsAsync<WebSocketException>(() => wronglyRouted.ConnectAsync(SocketUri(clientB, token), CancellationToken.None));
        Assert.Equal(HttpStatusCode.NotFound, wronglyRouted.HttpStatusCode);
        using var correctlyRouted = new ClientWebSocket();
        await correctlyRouted.ConnectAsync(SocketUri(clientA, token), CancellationToken.None);
        Assert.Equal(WebSocketState.Open, correctlyRouted.State);
        correctlyRouted.Abort();
    }

    private static Uri SocketUri(HttpClient client, string token) => new UriBuilder(client.BaseAddress!) { Scheme = "ws", Path = "/_blazor", Query = "id=" + token }.Uri;

    private sealed class PendingApiHandler(HttpMessageHandler inner, TaskCompletionSource entered, TaskCompletionSource release) : DelegatingHandler(inner)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(cancellationToken);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
