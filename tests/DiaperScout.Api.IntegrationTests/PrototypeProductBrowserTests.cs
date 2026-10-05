extern alias DiaperScoutWeb;
using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using CatalogueClient=DiaperScoutWeb::DiaperScout.Web.Services.ProductCatalogueClient;

namespace DiaperScout.Api.IntegrationTests;

public sealed class PrototypeProductBrowserTests(PostgreSqlFixture fixture, ITestOutputHelper output):IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData(390,false)]
    [InlineData(430,true)]
    [InlineData(768,false)]
    [Trait("Category","Browser")]
    public async Task Approved_product_to_availability_preserves_exact_pack_and_real_sources(int width,bool webkit)
    {
        using var api=new ObservationApiFactory(fixture);
        using var moderator=RetailListingApiTests.Moderator(api);
        var receipt=await PublicVariantCatalogueApiTests.CreateProductAsync(moderator,fixture);
        var managed=await PublicVariantCatalogueApiTests.ManagedAsync(moderator,receipt.ProductId);
        var otherPack=new PackType(receipt.SizeVariantId,99,PackagingType.Case);
        await using(var db=fixture.CreateDbContext()){db.PackTypes.Add(otherPack);await db.SaveChangesAsync();}
        using var explorer=api.CreateClient();explorer.DefaultRequestHeaders.Add("X-Development-Subject",PostgreSqlFixture.ExplorerSubject);
        async Task<PlaceItem> Shop(string name){var response=await NativePlaceFixtures.CreateAsync(api,new CreatePublicShopRequest(name+Guid.NewGuid(),"1 High Street","Testville","ZZ1 1ZZ","ZZ",51.501m,-0.1m,true,true));response.EnsureSuccessStatusCode();return(await response.Content.ReadFromJsonAsync<PlaceItem>())!;}
        var observed=await Shop("Boots ");var unrelated=await Shop("Wrong pack ");
        foreach(var entry in new[]{(observed.Id,receipt.PackTypeId),(unrelated.Id,otherPack.Id)}) {
            var saved=await explorer.PostAsJsonAsync("/api/v1/physical-observations",new CreatePhysicalObservationRequest(entry.Item2,entry.Item1,DateTimeOffset.UtcNow.AddDays(-1),null,null,Guid.NewGuid()));saved.EnsureSuccessStatusCode();
        }
        var retailer=await RetailListingApiTests.CreateRetailerAsync(moderator,true);
        var destination=retailer.WebsiteUrl+"prototype-real-destination";
        var created=await moderator.PostAsJsonAsync("/api/v1/retail-listings/",new ManualRetailerListingRequest(receipt.ProductId,receipt.ProductVariantId,receipt.SizeVariantId,receipt.PackTypeId,retailer.Id,destination));created.EnsureSuccessStatusCode();
        await RetailListingApiTests.StatusAsync(moderator,(await created.Content.ReadFromJsonAsync<RetailerProductListingItem>())!.Id,RetailerProductDiscoveryStatus.Verified);
        using var webBase=new PasskeyWebFactory(api);
        // Local visual fixture only: catalogue, variants, packs, listings and observations remain real.
        // Images are the approved pack asset; no production catalogue data is created by this fixture.
        var imageFixture=new ImageFixture();
        using var web=webBase.WithWebHostBuilder(b=>b.ConfigureServices(s=>{s.AddSingleton<IStartupFilter,FixtureImageFilter>();s.Configure<Microsoft.AspNetCore.Components.Server.CircuitOptions>(o=>o.DetailedErrors=true);s.AddHttpClient<CatalogueClient>().ConfigurePrimaryHttpMessageHandler(()=>new GalleryFixtureHandler(api.Server.CreateHandler(),managed.Slug,imageFixture));}));
        web.UseKestrel(0);using var client=web.CreateClient();var origin=client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        using var playwright=await Playwright.CreateAsync();await using var browser=await(webkit?playwright.Webkit:playwright.Chromium).LaunchAsync(new(){Headless=true});
        await using var context=await browser.NewContextAsync(new(){ViewportSize=new(){Width=width,Height=844},Geolocation=new(){Latitude=51.501f,Longitude=-0.1f},Permissions=["geolocation"],HasTouch=true});
        await context.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});");
        var page=await context.NewPageAsync();var errors=new List<string>();page.PageError+=(_,error)=>errors.Add(error);page.Console+=(_,message)=>{if(message.Type=="error")output.WriteLine(message.Text);};
        var productUrl=PublicProductIdentity.ProductUrl(managed.Slug,receipt.ProductVariantId)+"&packTypeId="+receipt.PackTypeId;
        await page.GotoAsync(origin+productUrl);await page.WaitForFunctionAsync("() => document.documentElement.dataset.pwaState === 'ready'");
        await Assertions.Expect(page.Locator(".ds-app-header")).ToBeHiddenAsync();
        await Assertions.Expect(page.Locator(".product-intro h1")).ToHaveTextAsync("TENA Slip Maxi");
        await Assertions.Expect(page.Locator(".observation-card").First).ToContainTextAsync(observed.Name);
        await Assertions.Expect(page.Locator(".observation-card").First).ToContainTextAsync("Yesterday");
        await page.WaitForFunctionAsync("() => [...document.querySelectorAll('.gallery-slide img')].every(image => image.complete && image.naturalWidth > 0)");
        await page.EvaluateAsync("async () => { await document.fonts.ready; }");
        // Values measured directly from the approved HTML/CSS, not the old responsive site.
        await Assertions.Expect(page.Locator(".product-intro h1")).ToHaveCSSAsync("color", "rgb(23, 56, 65)");
        await Assertions.Expect(page.Locator(".product-intro h1")).ToHaveCSSAsync("font-weight", "600");
        await Assertions.Expect(page.Locator(".observation-card").First).ToHaveCSSAsync("border-radius", "16px");
        await Assertions.Expect(page.Locator(".gallery-slide").First).ToHaveCSSAsync("height", $"{Math.Min(width,530)}px");
        Assert.Equal(14, (await page.Locator(".product-nav").BoundingBoxAsync())!.Y);
        Assert.True(await page.EvaluateAsync<bool>("document.fonts.check('600 16px Inter') && getComputedStyle(document.querySelector('.product-shell')).fontFamily.startsWith('Inter')"));
        await page.WaitForFunctionAsync("() => document.querySelector('.gallery-viewport')?.dataset.galleryReady === 'true'");
        // A completed import may receive a removed enhanced-navigation element.
        Assert.True(await page.EvaluateAsync<bool>("async () => { const module = await import('/js/product-prototype.js'); return module.gallery(null,0) === false && module.gallery(document.createElement('div'),0) === false; }"));
        await page.Locator(".gallery-viewport").FocusAsync();await page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(page.Locator(".gallery-counter")).ToHaveTextAsync("2 / 2");
        var bounds=(await page.Locator(".gallery-viewport").BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(bounds.X+bounds.Width*.3f,bounds.Y+80);await page.Mouse.DownAsync();await page.Mouse.MoveAsync(bounds.X+bounds.Width*.7f,bounds.Y+80);await page.Mouse.UpAsync();
        await Assertions.Expect(page.Locator(".gallery-counter")).ToHaveTextAsync("1 / 2");
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth"));
        await page.Locator(".product-intro h1").ClickAsync();
        var artifacts=Environment.GetEnvironmentVariable("DIAPERSCOUT_TEST_ARTIFACTS");
        if(!string.IsNullOrEmpty(artifacts)){Directory.CreateDirectory(artifacts);await page.ScreenshotAsync(new(){Path=Path.Combine(artifacts,$"prototype-product-{width}.png"),FullPage=true});}
        await page.Locator(".availability-card .view-all").ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(origin+$"/products/{managed.Slug}/retailers?variantId={receipt.ProductVariantId}&packTypeId={receipt.PackTypeId}");
        await Assertions.Expect(page.Locator(".retailer-product-meta")).ToContainTextAsync("Pack of 10");
        await page.GetByRole(AriaRole.Button,new(){Name="All Stores",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".retailer-entry")).ToHaveCountAsync(2);
        await Assertions.Expect(page.Locator(".retailer-logo").First).ToHaveCSSAsync("width", width <= 600 ? "64px" : "92px");
        await Assertions.Expect(page.Locator(".retailers-header h1")).ToHaveCSSAsync("font-weight", "600");
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth"));
        await Assertions.Expect(page.Locator(".retailer-list")).Not.ToContainTextAsync(unrelated.Name);
        await Assertions.Expect(page.Locator(".retailer-entry").Filter(new(){HasText=retailer.Name})).ToContainTextAsync("Catalogue listing");
        await Assertions.Expect(page.Locator($"a[href='{destination}']")).ToHaveAttributeAsync("rel","noopener noreferrer");
        if(!string.IsNullOrEmpty(artifacts))await page.ScreenshotAsync(new(){Path=Path.Combine(artifacts,$"prototype-retailers-{width}.png"),FullPage=true});
        await page.GetByRole(AriaRole.Button,new(){Name="Filter",Exact=true}).ClickAsync();await page.Locator("#availability-filter").FillAsync(observed.Name);
        await Assertions.Expect(page.Locator(".retailer-entry")).ToHaveCountAsync(1);
        await page.Locator("#availability-filter").FillAsync("");await page.GetByRole(AriaRole.Button,new(){Name="Nearby",Exact=true}).ClickAsync();await page.GetByRole(AriaRole.Button,new(){Name="Choose location",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".retailer-entry")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator(".retailer-entry")).ToContainTextAsync(observed.Name);
        await Assertions.Expect(page.Locator(".retailer-chevron")).ToHaveAttributeAsync("href","/atlas?locationId="+observed.Id);
        await page.GetByRole(AriaRole.Link,new(){Name="Back to TENA Slip Maxi",Exact=true}).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Combobox,new(){Name="Pack size",Exact=true})).ToHaveValueAsync(receipt.PackTypeId.ToString());
        // The restored page's SSR select is visible before its interactive handlers attach.
        await page.WaitForFunctionAsync("() => document.querySelector('.gallery-viewport')?.dataset.galleryReady === 'true'");
        await page.GetByRole(AriaRole.Combobox,new(){Name="Pack size",Exact=true}).SelectOptionAsync(otherPack.Id.ToString());
        await Assertions.Expect(page.Locator(".observation-card").First).ToContainTextAsync(unrelated.Name);
        await Assertions.Expect(page.Locator($"a[href='{destination}']")).ToHaveCountAsync(0);
        await page.Locator(".availability-card .view-all").ClickAsync();await Assertions.Expect(page.Locator(".retailer-product-meta")).ToContainTextAsync("Pack of 99");
        // Production also has verified products without images. Their fallback must
        // keep the approved 92px summary slot rather than stretching the whole row.
        imageFixture.ShowImages=false;
        await page.GotoAsync(origin+$"/products/{managed.Slug}/retailers?variantId={receipt.ProductVariantId}&packTypeId={otherPack.Id}");
        await Assertions.Expect(page.Locator(".retailer-product-image.gallery-empty")).ToHaveCSSAsync("width", "92px");
        await Assertions.Expect(page.Locator(".retailer-product-meta")).ToBeVisibleAsync();
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth"));
        await page.GetByRole(AriaRole.Link,new(){Name="Add Observation",Exact=true}).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(origin+"/observations/new?packTypeId="+otherPack.Id);
        await Assertions.Expect(page.GetByRole(AriaRole.Link,new(){Name="Sign in and continue",Exact=true})).ToBeVisibleAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
    // Serve the test image over real HTTP: WebKit's service-worker-controlled requests
    // can bypass Playwright route interception after the first enhanced navigation.
    private sealed class FixtureImageFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, continuation) =>
            {
                if (context.Request.Path != "/reference-pack.png") { await continuation(); return; }
                context.Response.ContentType = "image/png";
                await context.Response.Body.WriteAsync(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","prototype-tena1.png")));
            });
            next(app);
        };
    }
    private sealed class ImageFixture { public bool ShowImages {get;set;} = true; }
    private sealed class GalleryFixtureHandler(HttpMessageHandler inner,string slug,ImageFixture fixture):DelegatingHandler(inner) {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct) {
            var response=await base.SendAsync(request,ct);
            if(response.IsSuccessStatusCode && request.RequestUri!.AbsolutePath=="/api/v1/products/"+slug) {
                var product=(await response.Content.ReadFromJsonAsync<CatalogueProductDetails>(ct))!;
                response.Content.Dispose();response.Content=JsonContent.Create(product with{Name="TENA Slip Maxi",Images=fixture.ShowImages ? [new(Guid.NewGuid(),CatalogueSubmissionImageRole.PackFront,true,"/reference-pack.png"),new(Guid.NewGuid(),CatalogueSubmissionImageRole.PackBack,false,"/reference-pack.png")] : []});
            }
            return response;
        }
    }
}
