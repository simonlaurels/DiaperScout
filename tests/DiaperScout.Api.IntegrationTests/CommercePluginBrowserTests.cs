using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Commerce.Plugins.Awin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class CommercePluginBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    [Trait("Category", "Browser")]
    public async Task Moderator_sees_safe_plugin_health_and_can_toggle_at_desktop_and_mobile_widths()
    {
        using var basis = new ObservationApiFactory(fixture);
        using var api = basis.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            s.Configure<AwinAffiliateProgrammeDiscoveryOptions>(o => o.AccessToken = CommercePluginApiTests.Secret)));
        using var webBase = new PasskeyWebFactory(api);
        using var web = webBase.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["Authentication:Development:Subject"] = PostgreSqlFixture.ModeratorSubject })));
        web.UseKestrel(0);
        using var client = web.CreateClient();
        var origin = client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(origin + "/signin/development");
        await page.GotoAsync(origin + "/admin/commerce-plugins");
        var card = page.Locator("[data-plugin-id='awin.affiliate']");
        await Assertions.Expect(card).ToContainTextAsync("Configured");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Retail discovery", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Pricing", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Availability", Exact = true })).ToBeVisibleAsync();
        Assert.DoesNotContain(CommercePluginApiTests.Secret, await page.ContentAsync());
        // Wait for the interactive server connection before clicking.
        await page.WaitForFunctionAsync("() => typeof Blazor !== 'undefined' && !!Blazor._internal.navigationManager");
        await card.GetByRole(AriaRole.Button, new() { Name = "Disable", Exact = true }).ClickAsync();
        await Assertions.Expect(card.GetByRole(AriaRole.Button, new() { Name = "Enable", Exact = true })).ToBeVisibleAsync();
        using var moderator = CommercePluginApiTests.Moderator(api);
        Assert.False(Assert.Single((await moderator.GetFromJsonAsync<List<CommercePluginStatus>>("/api/v1/commerce-plugins/"))!).Enabled);
        await card.GetByRole(AriaRole.Button, new() { Name = "Enable", Exact = true }).ClickAsync();
        await Assertions.Expect(card.GetByRole(AriaRole.Button, new() { Name = "Disable", Exact = true })).ToBeVisibleAsync();
        foreach (var width in new[] { 1280, 390 })
        {
            await page.SetViewportSizeAsync(width, 900);
            Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth"));
            var artifacts = Environment.GetEnvironmentVariable("DIAPERSCOUT_TEST_ARTIFACTS");
            if (!string.IsNullOrWhiteSpace(artifacts))
            {
                Directory.CreateDirectory(artifacts);
                await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, $"commerce-plugins-{width}.png"), FullPage = true });
            }
        }
    }
}
