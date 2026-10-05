extern alias DiaperScoutWeb;
using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Xunit;
using ExplorePosition = DiaperScoutWeb::DiaperScout.Web.Services.ExplorePosition;
using ExploreDiscovery = DiaperScoutWeb::DiaperScout.Web.Services.ExploreDiscovery;

namespace DiaperScout.Api.IntegrationTests;

public sealed class ExploreBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public void Nearby_selection_filters_radius_empty_evidence_and_future_dates_then_ranks_by_observed_date()
    {
        var near=AtlasBrowserTests.Place("Near",51.5m,-2m,1);
        var further=AtlasBrowserTests.Place("Further",51.6m,-2m,1);
        further=further with { Observations=[further.Observations[0] with {ObservedAtUtc=near.Observations[0].ObservedAtUtc.AddHours(1)}] };
        var far=AtlasBrowserTests.Place("Far",55m,-2m,1);
        var candidate=AtlasBrowserTests.Place("Unobserved candidate",51.5m,-2m,0);
        var future=near with { Observations=[near.Observations[0] with {ObservedAtUtc=DateTimeOffset.UtcNow.AddDays(1)}] };
        var selected=ExploreDiscovery.Select([near,further,far,candidate,future],new(51.5,-2,30));
        Assert.Equal(new[]{"Further","Near"},selected.Select(s=>s.Place.Name));
        Assert.InRange(selected[0].Miles,6.8,7.0); Assert.Equal(0,selected[1].Miles);
        Assert.Empty(ExploreDiscovery.Select([near],new(51.5,-2,6000)));
        Assert.Empty(ExploreDiscovery.Select([near],new(double.NaN,-2,30)));
    }

    [Theory]
    [InlineData(390,844,false)]
    [InlineData(430,932,true)]
    [InlineData(375,667,true)]
    [InlineData(844,390,true)]
    [Trait("Category","Browser")]
    public async Task Installed_explore_real_navigation_product_links_images_and_nearby_states(int width,int height,bool webkit)
    {
        using var api=new ObservationApiFactory(fixture); using var webBase=new PasskeyWebFactory(api);
        var handler=new ExploreHandler();
        using var web=webBase.WithWebHostBuilder(builder=>builder.ConfigureServices(services=>{
            services.AddHttpClient<DiaperScoutWeb::DiaperScout.Web.Services.ProductCatalogueClient>().ConfigurePrimaryHttpMessageHandler(()=>handler);
            services.AddHttpClient<DiaperScoutWeb::DiaperScout.Web.Services.PlaceObservationClient>().ConfigurePrimaryHttpMessageHandler(()=>handler);
        }));
        web.UseKestrel(0); using var client=web.CreateClient();
        using var playwright=await Playwright.CreateAsync();
        await using var browser=await (webkit?playwright.Webkit:playwright.Chromium).LaunchAsync(new(){Headless=true});
        var context=await browser.NewContextAsync(new(){ViewportSize=new(){Width=width,Height=height},ServiceWorkers=ServiceWorkerPolicy.Block});
        await context.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});localStorage.setItem('ds-welcome-complete-v1','yes');window.locationRequests=0;Object.defineProperty(navigator,'geolocation',{configurable:true,value:{getCurrentPosition:(success)=>{window.locationRequests++;success({coords:{latitude:51.5,longitude:-2,accuracy:30}});}}});");
        await context.RouteAsync("**/test-only-pack.svg",r=>r.FulfillAsync(new(){ContentType="image/svg+xml",Body="<svg xmlns='http://www.w3.org/2000/svg' width='120' height='180'><rect width='120' height='180' fill='#fffdfa'/><text x='10' y='80'>TEST PACK</text></svg>"}));
        var page=await context.NewPageAsync(); await page.GotoAsync(client.BaseAddress!.ToString());
        await Assertions.Expect(page.Locator(".explore-product")).ToHaveCountAsync(5);
        await Assertions.Expect(page.Locator(".ds-app-header")).ToBeHiddenAsync();
        await Assertions.Expect(page.Locator(".pwa-mobile-nav")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator(".pwa-mobile-nav a[aria-current]")).ToHaveAttributeAsync("href","/");
        Assert.Equal(0,await page.EvaluateAsync<int>("window.locationRequests"));
        Assert.Equal(0,handler.AtlasRequests);
        await Assertions.Expect(page.Locator(".explore-product").First).ToHaveAttributeAsync("href",$"/products/test-product?variantId={handler.Variant}");
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth<=innerWidth && document.querySelector('.explore-product-scroll').scrollWidth>document.querySelector('.explore-product-scroll').clientWidth"));
        Assert.Equal("contain",await page.Locator(".explore-product-image img").First.EvaluateAsync<string>("e=>getComputedStyle(e).objectFit"));
        Assert.True(await page.Locator(".explore-intro").EvaluateAsync<bool>("e=>e.getBoundingClientRect().height<=210"));
        await Assertions.Expect(page.Locator(".explore-heading h1")).ToHaveTextAsync("Explore");
        await Assertions.Expect(page.Locator(".explore-intro img")).ToHaveAttributeAsync("src", "/pwa/explore-products-v1.webp");
        await Assertions.Expect(page.Locator(".explore-panel").First.GetByRole(AriaRole.Link,new(){Name="View all",Exact=false})).ToHaveAttributeAsync("href","/products?sort=newest");
        if(width<700) {
            Assert.Equal("0px",await page.Locator(".pwa-mobile-nav").EvaluateAsync<string>("e=>getComputedStyle(e).borderTopWidth"));
            Assert.True(await page.EvaluateAsync<bool>("document.querySelector('.explore-product-image').getBoundingClientRect().bottom < document.querySelector('.pwa-mobile-nav').getBoundingClientRect().top"));
        }
        await Evidence(page,$"explore-location-{width}");
        await page.GetByRole(AriaRole.Button,new(){Name="Use my location",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".explore-discovery")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator(".explore-discovery")).ToHaveAttributeAsync("href",$"/atlas?locationId={handler.Place.Place.Id}");
        await Assertions.Expect(page.Locator(".explore-discovery small")).ToContainTextAsync("About 0.0 miles away");
        await Evidence(page,$"explore-populated-{width}");
        // Location denial and absence are nonblocking and never trigger an automatic retry.
        await page.ReloadAsync();
        await page.EvaluateAsync("() => {navigator.geolocation.getCurrentPosition=(success,failure)=>{window.locationRequests++;failure({code:1});};}");
        await page.GetByRole(AriaRole.Button,new(){Name="Use my location",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".explore-location")).ToContainTextAsync("wasn’t allowed");
        await Assertions.Expect(page.Locator(".explore-product")).ToHaveCountAsync(5);
        Assert.Equal(1,await page.EvaluateAsync<int>("window.locationRequests"));
        await Evidence(page,$"explore-denied-{width}");
        await page.ReloadAsync();
        await Assertions.Expect(page.Locator(".explore-product")).ToHaveCountAsync(5);
        await page.EvaluateAsync("Object.defineProperty(navigator,'geolocation',{value:undefined,configurable:true})");
        await page.GetByRole(AriaRole.Button,new(){Name="Use my location",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".explore-location")).ToContainTextAsync("Location is unavailable");
        await page.ReloadAsync();
        await Assertions.Expect(page.Locator(".explore-product")).ToHaveCountAsync(5);
        await page.EvaluateAsync("Object.defineProperty(navigator,'geolocation',{value:{getCurrentPosition:success=>success({coords:{latitude:55,longitude:-2,accuracy:30}})},configurable:true})");
        await page.GetByRole(AriaRole.Button,new(){Name="Use my location",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".explore-quiet")).ToContainTextAsync("It’s quiet around here");
        await Assertions.Expect(page.Locator(".explore-quiet a")).ToHaveAttributeAsync("href","/scan");
        if(width<700) {
            await page.EvaluateAsync("window.scrollTo(0,document.documentElement.scrollHeight)");
            Assert.True(await page.EvaluateAsync<bool>("document.querySelector('.explore-quiet a').getBoundingClientRect().bottom <= document.querySelector('.pwa-mobile-nav').getBoundingClientRect().top"));
        }
        await Evidence(page,$"explore-empty-{width}");
        await Assertions.Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        await context.CloseAsync();
    }
    [Fact]
    [Trait("Category","Browser")]
    public async Task Independent_loading_errors_retry_and_few_products_preserve_existing_browser_home()
    {
        using var api=new ObservationApiFactory(fixture); using var webBase=new PasskeyWebFactory(api);
        var handler=new ExploreHandler {FailProducts=true, ProductCount=1, FailPlaces=true};
        using var web=webBase.WithWebHostBuilder(builder=>builder.ConfigureServices(services=>{
            services.AddHttpClient<DiaperScoutWeb::DiaperScout.Web.Services.ProductCatalogueClient>().ConfigurePrimaryHttpMessageHandler(()=>handler);
            services.AddHttpClient<DiaperScoutWeb::DiaperScout.Web.Services.PlaceObservationClient>().ConfigurePrimaryHttpMessageHandler(()=>handler);
        }));
        web.UseKestrel(0); using var client=web.CreateClient(); using var playwright=await Playwright.CreateAsync();
        await using var browser=await playwright.Chromium.LaunchAsync(new(){Headless=true});
        var context=await browser.NewContextAsync(new(){ViewportSize=new(){Width=390,Height=844},ServiceWorkers=ServiceWorkerPolicy.Block});
        await context.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});localStorage.setItem('ds-welcome-complete-v1','yes');Object.defineProperty(navigator,'geolocation',{value:{getCurrentPosition:success=>success({coords:{latitude:51.5,longitude:-2,accuracy:30}})}});");
        var page=await context.NewPageAsync(); await page.GotoAsync(client.BaseAddress!.ToString());
        await Assertions.Expect(page.Locator(".pwa-explore")).ToContainTextAsync("New arrivals are out of reach");
        handler.FailProducts=false;
        await page.Locator(".explore-panel").First.GetByRole(AriaRole.Button,new(){Name="Try again",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".explore-product")).ToHaveCountAsync(1);
        await page.GetByRole(AriaRole.Button,new(){Name="Use my location",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".explore-nearby")).ToContainTextAsync("Discoveries are out of reach");
        await Assertions.Expect(page.Locator(".explore-product")).ToHaveCountAsync(1);
        handler.FailPlaces=false;
        await page.Locator(".explore-nearby").GetByRole(AriaRole.Button,new(){Name="Try again",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".explore-discovery")).ToHaveCountAsync(1);
        handler.ProductCount=0; await page.ReloadAsync();
        await Assertions.Expect(page.Locator(".pwa-explore")).ToContainTextAsync("Fresh discoveries will appear here");
        await Evidence(page,"explore-no-recent-products");
        await context.CloseAsync();
        var standard=await browser.NewContextAsync(new(){ViewportSize=new(){Width=1280,Height=900}});
        var browserPage=await standard.NewPageAsync(); await browserPage.GotoAsync(client.BaseAddress.ToString());
        await Assertions.Expect(browserPage.Locator(".website-explore")).ToBeVisibleAsync();
        await Assertions.Expect(browserPage.Locator(".pwa-explore")).ToBeHiddenAsync();
        await Assertions.Expect(browserPage.Locator("#explore-query")).ToBeVisibleAsync();
        await standard.CloseAsync();
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category","Browser")]
    public async Task Explore_view_all_uses_existing_public_search_and_preserves_newest_sort_on_back(bool webkit)
    {
        using var api = new ObservationApiFactory(fixture);
        using var moderator = RetailListingApiTests.Moderator(api);
        var receipt = await PublicVariantCatalogueApiTests.CreateProductAsync(moderator,fixture);
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0); using var client=web.CreateClient();
        using var playwright=await Playwright.CreateAsync();
        await using var browser=await (webkit?playwright.Webkit:playwright.Chromium).LaunchAsync(new(){Headless=true});
        await using var context=await browser.NewContextAsync(new(){ViewportSize=new(){Width=390,Height=844},ServiceWorkers=ServiceWorkerPolicy.Block});
        await context.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});localStorage.setItem('ds-welcome-complete-v1','yes');");
        var page=await context.NewPageAsync(); await page.GotoAsync(client.BaseAddress!.ToString());
        await Assertions.Expect(page.Locator($".explore-product[href*='{receipt.ProductVariantId}'] strong")).ToHaveTextAsync("Integration Test Brand MEGAMAX");
        await page.Locator(".explore-panel").First.GetByRole(AriaRole.Link,new(){Name="View all",Exact=false}).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/products\\?sort=newest$"));
        await Assertions.Expect(page.Locator(".pwa-search .search-sort select")).ToHaveValueAsync("newest");
        var first=page.Locator(".pwa-search .search-result-card").First;
        await Assertions.Expect(first).ToContainTextAsync("Integration Test Brand MEGAMAX Black");
        await first.ClickAsync();
        await Assertions.Expect(page.Locator(".product-intro h1")).ToHaveTextAsync("Integration Test Brand MEGAMAX Black");
        Assert.Equal("1px",await page.Locator(".pwa-mobile-nav").EvaluateAsync<string>("e=>getComputedStyle(e).borderTopWidth"));
        await page.GoBackAsync();
        await Assertions.Expect(page.Locator(".pwa-search .search-sort select")).ToHaveValueAsync("newest");
        await Assertions.Expect(page.Locator(".pwa-search .search-result-card").First).ToContainTextAsync("Integration Test Brand MEGAMAX Black");
        await page.GoBackAsync();
        await Assertions.Expect(page.Locator(".explore-heading h1")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }

    private static async Task Evidence(IPage page,string name) {
        await page.EvaluateAsync("window.scrollTo(0,0)");
        await AtlasBrowserTests.Evidence(page,name);
    }
    private sealed class ExploreHandler:HttpMessageHandler {
        public Guid Variant {get;}=Guid.NewGuid();
        public AtlasPlace Place {get;}=AtlasBrowserTests.Place("Test-only pharmacy",51.5m,-2m,1);
        public int AtlasRequests;
        public bool FailProducts, FailPlaces;
        public int ProductCount=5;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct) {
            object body;
            if(request.RequestUri!.AbsolutePath.EndsWith("recent-products")) {
                if(FailProducts)return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                body=Enumerable.Range(0,ProductCount).Select(i=>new RecentCatalogueProduct(Guid.NewGuid(),i==1?"Test brand with a deliberately very long product discovery name":"Test product "+i,"test-product","Test brand","Test maker",Variant,i==0?"/test-only-pack.svg":null,DateTimeOffset.UtcNow.AddDays(-i))).ToArray();
            }
            else {Interlocked.Increment(ref AtlasRequests); if(FailPlaces)return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)); body=new[]{Place,AtlasBrowserTests.Place("Unobserved candidate",51.5m,-2m,0)};}
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(body)});
        }
    }
}
