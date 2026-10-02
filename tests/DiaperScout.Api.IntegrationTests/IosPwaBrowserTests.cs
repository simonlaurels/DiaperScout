using Microsoft.Playwright;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class IosPwaBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Browser")]
    public async Task WebKit_iPhone_browser_and_standalone_startup_and_navigation(bool standalone)
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api);
        web.UseKestrel(0);
        using var client = web.CreateClient();
        var origin = client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Webkit.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new(playwright.Devices["iPhone 13"]) { ServiceWorkers = ServiceWorkerPolicy.Allow });
        await context.AddInitScriptAsync($"Object.defineProperty(navigator, 'standalone', {{value: {standalone.ToString().ToLowerInvariant()}}});");
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        foreach (var path in new[] { "/", "/products" })
        {
            await page.GotoAsync(origin + path);
            await page.WaitForFunctionAsync("() => document.documentElement.dataset.pwaState === 'ready'");
            await Assertions.Expect(page.Locator("#pwa-startup")).ToBeHiddenAsync();
            await Assertions.Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        }
        await page.Locator(".pwa-search").GetByRole(AriaRole.Button, new() { Name = "View all", Exact = true }).First.ClickAsync();
        await page.Locator(".pwa-search .search-result-card").First.ClickAsync();
        await Assertions.Expect(page.Locator(".ds-size-pill")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        Assert.Empty(errors);
    }
}
