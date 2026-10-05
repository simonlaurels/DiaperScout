extern alias DiaperScoutWeb;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class PlaceObservationBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData(390, false)]
    [InlineData(430, true)]
    [InlineData(768, false)]
    [InlineData(1280, false)]
    [Trait("Category", "Browser")]
    public async Task Known_scan_exact_product_auth_resume_shop_observation_and_Atlas_are_real(int width, bool webkit)
    {
        using var api = new ObservationApiFactory(fixture);
        Guid packId;
        using (var scope = api.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();
            packId=await(from p in db.PackTypes join s in db.SizeVariants on p.SizeVariantId equals s.Id join v in db.ProductVariants on s.ProductVariantId equals v.Id where v.ProductId==fixture.ProductId select p.Id).SingleAsync();
            if(!await db.ProductIdentifiers.AnyAsync(i=>i.Value=="4006381333931")) {db.ProductIdentifiers.Add(new ProductIdentifier(packId,IdentifierType.Gtin,"4006381333931"));await db.SaveChangesAsync();}
        }
        using var webBase=new PasskeyWebFactory(api);
        using var web=webBase.WithWebHostBuilder(builder=>builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?> { ["Authentication:Development:Subject"]=PostgreSqlFixture.ExplorerSubject })));
        web.UseKestrel(0);using var client=web.CreateClient();var origin=client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        using var playwright=await Playwright.CreateAsync();await using var browser=await (webkit?playwright.Webkit:playwright.Chromium).LaunchAsync(new(){Headless=true});
        var context=await browser.NewContextAsync(new(){ViewportSize=new(){Width=width,Height=900},ServiceWorkers=ServiceWorkerPolicy.Allow});
        await context.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});Object.defineProperty(navigator,'geolocation',{value:{getCurrentPosition:s=>s({coords:{latitude:51.9,longitude:-2.1}})},configurable:true});");
        // OSM prohibits headless tile crawling; tests exercise map rendering with local synthetic tiles.
        await context.RouteAsync("https://tile.openstreetmap.org/**",route=>route.FulfillAsync(new(){Status=200,ContentType="image/png",BodyBytes=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=")}));
        var page=await context.NewPageAsync();await page.GotoAsync(origin+"/scan");await Ready(page);
        await EnterManual(page);await page.Locator("#gtin").FillAsync("4006381333931");await page.GetByRole(AriaRole.Button,new(){Name="Look up",Exact=true}).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Link,new(){Name="View product details",Exact=true})).ToHaveAttributeAsync("href",$"/products/integration-test-product?variantId={(await api.CreateClient().GetFromJsonAsync<ProductIdentification>("/api/v1/products/lookup/4006381333931"))!.ProductVariantId}&packTypeId={packId}");
        await page.GetByRole(AriaRole.Link,new(){Name="View product details",Exact=true}).ClickAsync();await Ready(page);
        await Assertions.Expect(page.GetByRole(AriaRole.Combobox,new(){Name="Pack size",Exact=true})).ToHaveValueAsync(packId.ToString());
        await page.GotoAsync(origin+"/scan");await Ready(page);await EnterManual(page);await page.Locator("#gtin").FillAsync("4006381333931");await page.GetByRole(AriaRole.Button,new(){Name="Look up",Exact=true}).ClickAsync();
        await page.GetByRole(AriaRole.Link,new(){Name="Record a discovery",Exact=true}).ClickAsync();await Ready(page);
        await page.GetByRole(AriaRole.Link,new(){Name="Sign in and continue",Exact=true}).ClickAsync();
        await page.GetByRole(AriaRole.Link,new(){Name="Sign in as development moderator",Exact=true}).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(origin+$"/observations/new?packTypeId={packId}");await Ready(page);
        await Assertions.Expect(page.GetByRole(AriaRole.Button,new(){Name="Add a physical shop",Exact=true})).ToHaveCountAsync(0);
        await page.GetByRole(AriaRole.Button,new(){Name="Find nearby shops",Exact=true}).ClickAsync();
        await page.GetByRole(AriaRole.Button,new(){Name="Nearby pharmacy",Exact=false}).ClickAsync();
        await page.GetByRole(AriaRole.Button,new(){Name="Continue",Exact=true}).ClickAsync();
        await page.Locator("#observed-price").FillAsync("18.25");await page.Locator("#observed-currency").FillAsync("GBP");
        await page.GetByRole(AriaRole.Button,new(){Name="Review discovery",Exact=true}).ClickAsync();await page.GetByRole(AriaRole.Button,new(){Name="Record discovery",Exact=true}).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Discovery recorded!",Exact=true})).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link,new(){Name="View in Atlas",Exact=true}).ClickAsync();await Ready(page);
        await Assertions.Expect(page.Locator("#place-heading")).ToHaveTextAsync("Nearby pharmacy");
        if (width == 430) await Assertions.Expect(page.Locator(".atlas-place-type")).ToHaveTextAsync("Pharmacy");
        var marker=page.Locator($".leaflet-marker-icon.atlas-marker-selected[title='Nearby pharmacy']");
        await page.Locator(".place-map").EvaluateAsync("element => element.scrollIntoView({block:'center'})");
        await Assertions.Expect(marker).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".map-attribution a[href='/places/open-data']")).ToBeVisibleAsync();
        await marker.ClickAsync();await Assertions.Expect(page.Locator(".leaflet-popup-content")).ToContainTextAsync("reported");
        await Assertions.Expect(page.Locator(".atlas-observation").First).ToContainTextAsync("18.25 GBP");
        var overflow = await page.EvaluateAsync<string>("JSON.stringify([...document.querySelectorAll('body *')].filter(e=>e.getBoundingClientRect().right>innerWidth+1 && getComputedStyle(e).position!=='absolute').map(e=>({tag:e.tagName,cls:e.className,right:e.getBoundingClientRect().right})).slice(0,15))");
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth"), overflow);
        await page.ReloadAsync();await Ready(page);await Assertions.Expect(page.Locator("#place-heading")).ToHaveTextAsync("Nearby pharmacy");
        await using var failedMapContext = await browser.NewContextAsync(new(){ViewportSize=new(){Width=width,Height=900},ServiceWorkers=ServiceWorkerPolicy.Block});
        await failedMapContext.RouteAsync("https://tile.openstreetmap.org/**", route => route.FulfillAsync(new(){Status=503,Body="Unavailable"}));
        if (webkit) await failedMapContext.RouteAsync("**/lib/leaflet/leaflet.js", route => route.AbortAsync());
        var failedMapPage = await failedMapContext.NewPageAsync();
        await failedMapPage.GotoAsync(page.Url);await Ready(failedMapPage);
        await Assertions.Expect(failedMapPage.GetByRole(AriaRole.Status).Filter(new(){HasText="map background is unavailable"})).ToBeVisibleAsync();
        await Assertions.Expect(failedMapPage.Locator("#place-heading")).ToHaveTextAsync("Nearby pharmacy");
        var evidence=Environment.GetEnvironmentVariable("DIAPERSCOUT_BROWSER_EVIDENCE");if(!string.IsNullOrEmpty(evidence)){Directory.CreateDirectory(evidence);await page.ScreenshotAsync(new(){Path=Path.Combine(evidence,$"atlas-{width}.png"),FullPage=true});}
        await context.CloseAsync();
    }
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [Trait("Category", "Browser")]
    public async Task Unknown_scan_guided_draft_survives_signin_and_enters_real_queue_without_publication(bool webkit, bool lostResponse)
    {
        using var api=new ObservationApiFactory(fixture);using var webBase=new PasskeyWebFactory(api);using var web=webBase.WithWebHostBuilder(builder=>builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?>{["Authentication:Development:Subject"]=PostgreSqlFixture.ExplorerSubject})).ConfigureServices(services=>{ if(lostResponse) services.AddHttpClient<DiaperScoutWeb::DiaperScout.Web.Services.PlaceObservationClient>().ConfigurePrimaryHttpMessageHandler(()=>new LoseFirstProposalResponse(api.Server.CreateHandler())); }));
        web.UseKestrel(0);using var client=web.CreateClient();var origin=client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        using var playwright=await Playwright.CreateAsync();await using var browser=await (webkit?playwright.Webkit:playwright.Chromium).LaunchAsync(new(){Headless=true});var page=await browser.NewPageAsync(new(){ViewportSize=new(){Width=390,Height=844}});
        await page.GotoAsync(origin+"/scan");await Ready(page);await EnterManual(page);await page.Locator("#gtin").FillAsync("96385074");await page.GetByRole(AriaRole.Button,new(){Name="Look up",Exact=true}).ClickAsync();
        Assert.Equal("042100005264", await page.EvaluateAsync<string>("async () => (await import('/Components/Pages/Scan.razor.js')).expandUpcE('04252614')"));
        await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="We don’t have this one yet.",Exact=true})).ToBeVisibleAsync();await page.GetByRole(AriaRole.Link,new(){Name="Add product",Exact=true}).ClickAsync();await Ready(page);
        await Assertions.Expect(page.Locator(".contribution-barcode")).ToContainTextAsync("96385074");
        await page.Locator("#proposal-brand").FillAsync("Browser Brand");await page.Locator("#proposal-name").FillAsync("Browser Proposed Product " + webkit + lostResponse);await page.GetByRole(AriaRole.Button,new(){Name="Continue to pack",Exact=true}).ClickAsync();
        await page.Locator("#proposal-size").FillAsync("Large");await page.Locator("#proposal-quantity").FillAsync("12");await page.GetByRole(AriaRole.Button,new(){Name="Review proposal",Exact=true}).ClickAsync();
        await page.GetByRole(AriaRole.Button,new(){Name="Sign in to submit",Exact=true}).ClickAsync();await page.GetByRole(AriaRole.Link,new(){Name="Sign in as development moderator",Exact=true}).ClickAsync();await Ready(page);
        await Assertions.Expect(page.Locator(".contribution-review")).ToContainTextAsync("Browser Proposed Product " + webkit + lostResponse);await Assertions.Expect(page.Locator(".contribution-review")).ToContainTextAsync("Large");
        await page.GetByRole(AriaRole.Button,new(){Name="Submit for review",Exact=true}).ClickAsync();
        if(lostResponse) {
            await Assertions.Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("couldn’t confirm");
            await page.ReloadAsync();await Ready(page);
            await Assertions.Expect(page.Locator(".contribution-review")).ToContainTextAsync("Browser Proposed Product " + webkit + lostResponse);
            await page.GetByRole(AriaRole.Button,new(){Name="Submit for review",Exact=true}).ClickAsync();
        }
        await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Sent for review",Exact=true})).ToBeVisibleAsync();
        using var scope=api.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();var submission=await db.CatalogueSubmissions.SingleAsync(s=>s.ProposedProductName=="Browser Proposed Product " + webkit + lostResponse);Assert.Equal(CatalogueSubmissionSource.Explorer,submission.Source);Assert.Equal("96385074",submission.ProposedGtin);Assert.Null(submission.PublishedProductId);Assert.False(await db.Products.AnyAsync(p=>p.Name=="Browser Proposed Product " + webkit + lostResponse));
        Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('diaperscout-product-draft:96385074')"));
        Assert.Null(await page.EvaluateAsync<string?>("sessionStorage.getItem('diaperscout-product-draft:96385074')"));
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth"));
    }
    private sealed class LoseFirstProposalResponse(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        private int dropped;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct) {
            var response=await base.SendAsync(request,ct);
            if(request.RequestUri!.AbsolutePath=="/api/v1/public-product-proposals" && response.IsSuccessStatusCode && Interlocked.Exchange(ref dropped,1)==0) {
                response.Dispose();throw new HttpRequestException("Test-only lost response after durable commit");
            }
            return response;
        }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category","Browser")]
    public async Task Draft_storage_preserves_identity_step_expiry_legacy_and_account_isolation(bool webkit) {
        using var api=new ObservationApiFactory(fixture);using var web=new PasskeyWebFactory(api);
        web.UseKestrel(0);using var client=web.CreateClient();
        using var playwright=await Playwright.CreateAsync();await using var browser=await (webkit?playwright.Webkit:playwright.Chromium).LaunchAsync(new(){Headless=true});
        var page=await browser.NewPageAsync();await page.GotoAsync(client.BaseAddress!.ToString());
        Assert.True(await page.EvaluateAsync<bool>("""
            async () => {
                const m=await import('/js/contribution-draft.js');const gtin='96385074';
                const draft={brandName:'Exact brand',productName:'Exact name',version:'Overnight',manufacturerName:'Maker',size:'S',quantity:12,contributionId:'stable-retry-id'};
                if(!m.saveDraft(gtin,draft,'owner-a',2))return false;
                if(JSON.stringify(m.loadDraft(gtin,'owner-a'))!==JSON.stringify(draft)||m.draftStep(gtin)!==2)return false;
                if(m.loadDraft(gtin,'owner-b')!==null||m.loadDraft(gtin,null)!==null)return false;
                m.clearDraft(gtin,'different-id');if(!m.loadDraft(gtin,'owner-a'))return false;
                m.clearDraft(gtin,'stable-retry-id');if(m.loadDraft(gtin,'owner-a'))return false;
                sessionStorage.setItem('diaperscout-product-draft:'+gtin,JSON.stringify({expires:Date.now()+10000,draft}));
                if(JSON.stringify(m.loadDraft(gtin,'owner-a'))!==JSON.stringify(draft)||m.draftStep(gtin)!==3)return false;
                m.saveDraft(gtin,draft,'owner-a',3);sessionStorage.clear();if(!m.loadDraft(gtin,'owner-a'))return false;
                localStorage.setItem('diaperscout-product-draft:'+gtin,JSON.stringify({expires:Date.now()-1,draft}));
                if(m.loadDraft(gtin)!==null)return false;
                const set=Storage.prototype.setItem;Storage.prototype.setItem=()=>{throw new Error('Storage blocked')};
                try {return m.saveDraft(gtin,draft)===false;}finally{Storage.prototype.setItem=set;}
            }
            """));
    }
    private static Task EnterManual(IPage page)=>page.GetByRole(AriaRole.Button,new(){Name="Enter barcode number",Exact=true}).ClickAsync();
    private static Task Ready(IPage page)=>page.WaitForFunctionAsync("() => document.documentElement.dataset.pwaState === 'ready'");
}
