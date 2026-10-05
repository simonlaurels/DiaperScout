using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;
public sealed class ScanDiscoveryBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData(390, false)]
    [InlineData(430, true)]
    [Trait("Category","Browser")]
    public async Task Installed_scan_known_discovery_and_unknown_evidence_preserve_context_and_moderation(int width, bool webkit) {
        using var api = new ObservationApiFactory(fixture);
        if (webkit) {
            using var scope = api.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DiaperScout.Infrastructure.Persistence.DiaperScoutDbContext>();
            var submission = new CatalogueSubmission(CatalogueSubmissionSource.Moderator, fixture.ModeratorUserId, "Test manufacturer", "Approved image fixture");
            var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "prototype-tena1.png"));
            var storageKey = $"scan-test/{Guid.NewGuid():N}.png";
            await using var stream = new MemoryStream(bytes);
            await scope.ServiceProvider.GetRequiredService<ICatalogueSubmissionImageStorage>().SaveAsync(storageKey, stream);
            var image = new CatalogueSubmissionImage(submission.Id, CatalogueSubmissionImageRole.PackFront, storageKey, "prototype-tena1.png", "image/png", bytes.Length, CatalogueImageSourceType.Other, permissionStatus: CatalogueImagePermissionStatus.PermissionGranted, permissionEvidence: "Existing repository test image; isolated fixture only.");
            image.PublishToProduct(fixture.ProductId); image.SetPrimary(true); db.AddRange(submission, image); await db.SaveChangesAsync();
        }
        await using (var db = fixture.CreateDbContext()) {
            if (!await db.ProductIdentifiers.AnyAsync(i => i.Value == "4006381333931")) { db.ProductIdentifiers.Add(new ProductIdentifier(fixture.PackTypeId, IdentifierType.Gtin, "4006381333931")); await db.SaveChangesAsync(); }
        }
        using var apiActor = api.CreateClient(); apiActor.DefaultRequestHeaders.Add("X-Development-Subject", PostgreSqlFixture.ExplorerSubject);
        var shopResponse = await apiActor.PostAsJsonAsync("/api/v1/places/", new CreatePublicShopRequest("Scan test branch " + width, "1 Public Street", "Testville", "ZZ1 2ZZ", "ZZ", 51.9m, -2.1m, true, true, PlaceCategory.Pharmacy)); shopResponse.EnsureSuccessStatusCode(); var shop = (await shopResponse.Content.ReadFromJsonAsync<PlaceItem>())!;
        using var baseWeb = new PasskeyWebFactory(api);
        using var web = baseWeb.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string,string?> { ["Authentication:Development:Subject"] = PostgreSqlFixture.ExplorerSubject })));
        web.UseKestrel(0); using var client = web.CreateClient(); using var playwright = await Playwright.CreateAsync();
        await using var browser = await (webkit ? playwright.Webkit : playwright.Chromium).LaunchAsync(new() {Headless=true});
        await using var context = await browser.NewContextAsync(new() {ViewportSize=new(){Width=width,Height=width == 430 ? 932 : 844},ServiceWorkers=ServiceWorkerPolicy.Block});
        await context.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});localStorage.setItem('ds-welcome-complete-v1','yes');window.geoRequests=0;Object.defineProperty(navigator,'geolocation',{value:{getCurrentPosition:(success,failure)=>{window.geoRequests++;failure({code:1});}},configurable:true});");
        await context.RouteAsync("https://tile.openstreetmap.org/**", route => route.FulfillAsync(new(){Status=200,ContentType="image/png",BodyBytes=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=")}));
        var page = await context.NewPageAsync(); var errors = new List<string>(); page.PageError += (_, e) => errors.Add(page.Url + " " + e);
        var origin = client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        await page.GotoAsync(origin + "/signin/development");
        await page.WaitForFunctionAsync("() => document.documentElement.dataset.pwaState === 'ready'");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await ScanAsync(page, origin, "4006381333931");
        await Assertions.Expect(page.Locator(".scan-mobile-result header h1")).ToHaveTextAsync("Barcode recognised");
        await Assertions.Expect(page.Locator(".scan-pack-card")).ToContainTextAsync("12 pack");
        if (webkit) { await Assertions.Expect(page.Locator(".scan-pack-image img")).ToBeVisibleAsync(); await Assertions.Expect(page.Locator(".scan-pack-image img")).ToHaveCSSAsync("object-fit", "contain"); }
        await Assertions.Expect(page.Locator(".ds-app-header")).ToBeHiddenAsync();
        var details = await page.Locator(".scan-action").Filter(new(){HasText="View product details"}).GetAttributeAsync("href");
        var retailers = await page.Locator(".scan-action").Filter(new(){HasText="Where to buy"}).GetAttributeAsync("href");
        Assert.Contains("packTypeId="+fixture.PackTypeId, details); Assert.Contains("packTypeId="+fixture.PackTypeId, retailers);
        await Capture(page, $"recognised-{width}.png");
        await page.Locator(".scan-correction summary").ClickAsync(); await Assertions.Expect(page.GetByRole(AriaRole.Button,new(){Name="Scan again",Exact=true})).ToBeVisibleAsync();
        await page.Locator(".scan-action.primary").ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Where did you find it?",Exact=true}).First).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button,new(){Name="Find nearby shops",Exact=true}).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Status).Filter(new(){HasText="Location isn’t available"})).ToBeVisibleAsync();
        Assert.Equal(1, await page.EvaluateAsync<int>("geoRequests"));
        await page.Locator("#place-query").FillAsync("Scan test branch " + width); await page.GetByRole(AriaRole.Button,new(){Name="Search shops",Exact=true}).ClickAsync();
        await page.Locator(".place-options button").Filter(new(){HasText=shop.Name}).ClickAsync(); await Capture(page,$"place-{width}.png");
        await page.GetByRole(AriaRole.Button,new(){Name="Continue",Exact=true}).ClickAsync();
        await page.Locator("#observed-price").FillAsync("14.99"); await page.Locator("#observed-currency").FillAsync("GBP"); await Capture(page,$"discovery-details-{width}.png");
        await page.GetByRole(AriaRole.Button,new(){Name="Review discovery",Exact=true}).ClickAsync(); await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Review your discovery",Exact=true})).ToBeVisibleAsync(); await Capture(page,$"discovery-review-{width}.png");
        await page.ReloadAsync(); await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Review your discovery",Exact=true})).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button,new(){Name="Record discovery",Exact=true}).ClickAsync(); await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Discovery recorded!",Exact=true})).ToBeVisibleAsync(); await Capture(page,$"discovery-success-{width}.png");
        await page.GetByRole(AriaRole.Link,new(){Name="View in Atlas",Exact=true}).ClickAsync(); await Assertions.Expect(page.Locator("#place-heading")).ToHaveTextAsync(shop.Name);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.GotoAsync(origin + details); await page.WaitForFunctionAsync("() => document.documentElement.dataset.pwaState === 'ready'"); await page.WaitForLoadStateAsync(LoadState.NetworkIdle); await Assertions.Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        await page.GotoAsync(origin + retailers); await page.WaitForFunctionAsync("() => document.documentElement.dataset.pwaState === 'ready'"); await page.WaitForLoadStateAsync(LoadState.NetworkIdle); await Assertions.Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        await ScanAsync(page, origin, "96385074"); await page.GetByRole(AriaRole.Link,new(){Name="Add this product",Exact=true}).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Take photos of the pack",Exact=true})).ToBeVisibleAsync();
        await page.Locator("#proposal-photo").SetInputFilesAsync(new FilePayload {Name="pack.png",MimeType="image/png",Buffer=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aN1sAAAAASUVORK5CYII=")});
        await page.WaitForFunctionAsync("() => document.querySelector('.proposal-photos img') || document.querySelector('.proposal-flow [role=alert]')");
        Assert.True(await page.Locator(".proposal-photos img").CountAsync() == 1, await page.Locator(".proposal-flow").InnerTextAsync()); await page.GetByRole(AriaRole.Button,new(){Name="Continue",Exact=true}).ClickAsync();
        await page.Locator("#proposal-brand").FillAsync("Integration Test Brand"); await page.Locator("#proposal-name").FillAsync("Integration Test Product"); await page.Locator("#proposal-notes").FillAsync("New barcode packaging evidence.");
        await page.GetByRole(AriaRole.Button,new(){Name="Continue",Exact=true}).ClickAsync(); await Assertions.Expect(page.Locator(".proposal-candidate").First).ToBeVisibleAsync(); await Capture(page,$"duplicate-check-{width}.png");
        await page.Locator(".proposal-candidate").First.ClickAsync(); await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Review your submission",Exact=true})).ToBeVisibleAsync(); await Capture(page,$"proposal-review-{width}.png");
        await page.ReloadAsync(); await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Review your submission",Exact=true})).ToBeVisibleAsync(); await Assertions.Expect(page.Locator(".proposal-photos img")).ToHaveCountAsync(1);
        await page.GetByRole(AriaRole.Button,new(){Name="Submit for review",Exact=true}).ClickAsync(); await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Product submitted!",Exact=true})).ToBeVisibleAsync(); await Capture(page,$"proposal-success-{width}.png");
        await page.GetByRole(AriaRole.Link,new(){Name="Record where I found it",Exact=true}).ClickAsync();
        await page.Locator("#place-query").FillAsync(shop.Name); await page.GetByRole(AriaRole.Button,new(){Name="Search shops",Exact=true}).ClickAsync(); await page.Locator(".place-options button").Filter(new(){HasText=shop.Name}).ClickAsync();
        await page.GetByRole(AriaRole.Button,new(){Name="Continue",Exact=true}).ClickAsync(); await page.GetByRole(AriaRole.Button,new(){Name="Review discovery",Exact=true}).ClickAsync(); await page.GetByRole(AriaRole.Button,new(){Name="Record discovery",Exact=true}).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Discovery evidence saved",Exact=true})).ToBeVisibleAsync();
        await using var verify = fixture.CreateDbContext(); var proposal = await verify.CatalogueSubmissions.Where(s=>s.ProposedGtin=="96385074" && s.PendingLocationId==shop.Id).SingleAsync();
        Assert.Equal(fixture.ProductId, proposal.SuggestedExistingProductId); Assert.Null(proposal.PublishedProductId); Assert.Null(proposal.ResultingObservationId); Assert.Equal(shop.Id,proposal.PendingLocationId);
        Assert.False(await verify.ProductIdentifiers.AnyAsync(i=>i.Value=="96385074")); Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth<=innerWidth"));
        proposal.Reject(); await verify.SaveChangesAsync();
        await page.GotoAsync(origin + "/contribute/product?gtin=96385074");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Proposal reviewed",Exact=true})).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Link,new(){Name="Record where I found it",Exact=true})).ToHaveCountAsync(0);
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }
    private static async Task ScanAsync(IPage page,string origin,string barcode) { await page.GotoAsync(origin+"/scan"); await page.GetByRole(AriaRole.Button,new(){Name="Enter barcode number",Exact=true}).ClickAsync(); await page.Locator("#gtin").FillAsync(barcode); await page.GetByRole(AriaRole.Button,new(){Name="Look up",Exact=true}).ClickAsync(); }
    private static async Task Capture(IPage page,string name) { Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth<=innerWidth")); var folder=Environment.GetEnvironmentVariable("DIAPERSCOUT_BROWSER_EVIDENCE"); if(!string.IsNullOrWhiteSpace(folder)){Directory.CreateDirectory(folder);await page.EvaluateAsync("document.activeElement?.blur()");await page.ScreenshotAsync(new(){Path=Path.Combine(folder,name),FullPage=true});} }
}
