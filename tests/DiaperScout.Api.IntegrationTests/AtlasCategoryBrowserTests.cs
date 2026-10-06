extern alias DiaperScoutWeb;
using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Xunit;
namespace DiaperScout.Api.IntegrationTests;
public sealed class AtlasCategoryBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    [Trait("Category", "Browser")]
    public async Task Explicit_place_categories_have_distinct_icons_labels_and_separate_evidence_counts(bool webkit) {
        var entries = Enumerable.Range(0, 7).Select(i => {
            var entry = AtlasBrowserTests.Place(i == 0 ? "Unclassified shop" : "Observed place " + i, 51.5m + i * .005m, -2m + i * .005m, i == 1 ? 3 : 1);
            return entry with { Place = entry.Place with { Category = i == 0 ? null : (PlaceCategory)i } };
        }).ToArray();
        var candidate = AtlasBrowserTests.Place("Unobserved pharmacy", 51.52m, -2m, 0);
        candidate = candidate with { Place = candidate.Place with { Category = PlaceCategory.Pharmacy } };
        using var api = new ObservationApiFactory(fixture); using var webBase = new PasskeyWebFactory(api);
        using var web = webBase.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddHttpClient<DiaperScoutWeb::DiaperScout.Web.Services.PlaceObservationClient>().ConfigurePrimaryHttpMessageHandler(() => new ProjectionHandler([..entries, candidate]))));
        web.UseKestrel(0); using var client = web.CreateClient(); using var playwright = await Playwright.CreateAsync();
        await using var browser = await (webkit ? playwright.Webkit : playwright.Chromium).LaunchAsync(new() { Headless = true });
        var context = await AtlasBrowserTests.Context(browser, webkit ? 430 : 390, 900, true); var page = await context.NewPageAsync();
        await page.GotoAsync(client.BaseAddress + "atlas");
        await Assertions.Expect(page.Locator(".atlas-discovery-marker")).ToHaveCountAsync(7);
        foreach (var kind in new[] { "neutral", "pharmacy", "supermarket", "specialist", "retailer", "convenience", "other" })
            await Assertions.Expect(page.Locator(".atlas-category-" + kind)).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator(".atlas-marker-count")).ToHaveTextAsync("3");
        var labels = new[] { "Pharmacy", "Supermarket", "Specialist retailer", "General retailer", "Convenience store", "Other" };
        for (var i = 1; i <= 6; i++) Assert.Contains("(" + labels[i - 1] + ")", await page.Locator($".atlas-discovery-marker[title='Observed place {i}']").GetAttributeAsync("aria-label"));
        var pharmacy = page.Locator(".atlas-discovery-marker[title='Observed place 1']"); await pharmacy.FocusAsync(); await pharmacy.PressAsync("Enter");
        await Assertions.Expect(page.Locator(".atlas-place-type")).ToHaveTextAsync("Pharmacy");
        await Assertions.Expect(page.Locator(".atlas-evidence-summary")).ToHaveTextAsync("3 observations · 1 pack reported");
        await Assertions.Expect(page.Locator(".atlas-category-editor")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator(".atlas-marker-selected")).ToHaveCountAsync(1);
        await page.WaitForFunctionAsync("() => document.querySelector('.atlas-marker-selected').getBoundingClientRect().bottom < document.querySelector('.atlas-selected-place').getBoundingClientRect().top");
        Assert.Contains("atlas-category-neutral", await page.EvaluateAsync<string>("async () => (await import('/js/atlas-marker.js')).discoveryMarker(1,999)"));
        await AtlasBrowserTests.Evidence(page, "atlas-categories-" + (webkit ? "webkit" : "chromium"));
        await page.GetByRole(AriaRole.Button, new() { Name = "Close place details", Exact = true }).ClickAsync();
        var neutral = page.Locator(".atlas-discovery-marker[title='Unclassified shop']"); await neutral.FocusAsync(); await neutral.PressAsync("Space");
        await Assertions.Expect(page.Locator("#place-heading")).ToHaveTextAsync("Unclassified shop");
        await Assertions.Expect(page.Locator(".atlas-place-type")).ToHaveCountAsync(0);
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth")); await context.CloseAsync();
    }
    [Fact]
    [Trait("Category", "Browser")]
    public async Task Moderator_can_save_and_clear_category_on_a_real_shared_place_while_preserving_observation() {
        using var api = new ObservationApiFactory(fixture); using var actor = api.CreateClient();
        actor.DefaultRequestHeaders.Add("X-Development-Subject", PostgreSqlFixture.ExplorerSubject);
        var response = await NativePlaceFixtures.CreateAsync(api, new CreatePublicShopRequest("Moderator editable shop", "1 Test Street", "Testville", "ZZ3 3ZZ", "ZZ", 51.5m, -2m, true, true, PlaceCategory.Pharmacy));
        response.EnsureSuccessStatusCode(); var place = (await response.Content.ReadFromJsonAsync<PlaceItem>())!;
        var observation = await actor.PostAsJsonAsync("/api/v1/physical-observations", new CreatePhysicalObservationRequest(fixture.PackTypeId, place.Id, DateTimeOffset.UtcNow.AddMinutes(-1), 18.25m, "GBP", Guid.NewGuid())); observation.EnsureSuccessStatusCode();
        var receipt = (await observation.Content.ReadFromJsonAsync<PhysicalObservationReceipt>())!;
        using var webBase = new PasskeyWebFactory(api);
        using var web = webBase.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Authentication:Development:Subject"] = PostgreSqlFixture.ModeratorSubject })));
        web.UseKestrel(0); using var client = web.CreateClient(); using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var context = await AtlasBrowserTests.Context(browser, 390, 844, true); var page = await context.NewPageAsync(); var url = client.BaseAddress + "atlas?locationId=" + place.Id;
        await page.GotoAsync(url); await Assertions.Expect(page.Locator("#place-heading")).ToHaveTextAsync(place.Name);
        await Assertions.Expect(page.Locator(".atlas-category-editor")).ToHaveCountAsync(0);
        await page.GotoAsync(client.BaseAddress + "signin/development"); await page.GotoAsync(url);
        await Assertions.Expect(page.Locator("#atlas-category")).ToHaveValueAsync("1");
        await page.Locator("#atlas-category").SelectOptionAsync("6"); await page.GetByRole(AriaRole.Button, new() { Name = "Save place type", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator(".atlas-category-editor")).ToContainTextAsync("Place type saved.");
        await Assertions.Expect(page.Locator(".atlas-place-type")).ToHaveTextAsync("Other");
        await Assertions.Expect(page.Locator(".atlas-category-other")).ToHaveCountAsync(1);
        await page.ReloadAsync(); await Assertions.Expect(page.Locator("#atlas-category")).ToHaveValueAsync("6");
        await page.Locator("#atlas-category").SelectOptionAsync(""); await page.GetByRole(AriaRole.Button, new() { Name = "Save place type", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator(".atlas-category-editor")).ToContainTextAsync("Place type saved.");
        await Assertions.Expect(page.Locator(".atlas-place-type")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator(".atlas-category-neutral")).ToHaveCountAsync(1);
        using var scope = api.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();
        Assert.Null((await db.Locations.SingleAsync(l => l.Id == place.Id)).Category);
        var preserved = await db.Observations.SingleAsync(o => o.Id == receipt.Id); Assert.Equal(place.Id, preserved.LocationId); Assert.Equal(fixture.PackTypeId, preserved.PackTypeId); Assert.Equal(18.25m, preserved.PriceAmount);
        await context.CloseAsync();
    }
    private sealed class ProjectionHandler(AtlasPlace[] places) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(places) });
    }
}
