using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class BackpackPersonalBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    [Trait("Category","Browser")]
    public async Task Every_menu_uses_real_owned_data_and_settings_preserve_security_and_draft_recovery()
    {
        var user=new User("personal-browser-"+Guid.NewGuid());var name="Personal Explorer "+Guid.NewGuid().ToString()[..6];
        await using(var db=fixture.CreateDbContext()){
            var country=await db.Countries.SingleAsync(c=>c.IsoCode=="ZZ");
            var shop=DiaperScout.Domain.Location.PublicShop(user.Id,country.Id,"Personal test shop","1 Street","Town","ZZ1",51,-2,Guid.NewGuid().ToString());
            db.AddRange(user,new ExplorerProfile(user.Id,name),new UserEmail(user.Id,"personal-browser@example.test"),shop);await db.SaveChangesAsync();
        }
        using var api=new ObservationApiFactory(fixture);using var webBase=new PasskeyWebFactory(api);
        using var web=webBase.WithWebHostBuilder(b=>b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{["Authentication:Development:Subject"]=user.Subject})));
        web.UseKestrel(0);using var client=web.CreateClient();using var playwright=await Playwright.CreateAsync();
        await using var browser=await playwright.Chromium.LaunchAsync(new(){Headless=true});
        var context=await browser.NewContextAsync(new(){ViewportSize=new(){Width=390,Height=844},ServiceWorkers=ServiceWorkerPolicy.Block});
        await context.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});localStorage.setItem('ds-welcome-complete-v1','yes');");
        var page=await context.NewPageAsync();await page.GotoAsync(client.BaseAddress+"signin/development");await page.GotoAsync(client.BaseAddress+"backpack");
        await page.EvaluateAsync("owner=>{for(const [gtin,name,who,expiry] of [['5012345678900','Owned recovery draft',owner,Date.now()+120000],['4006381333931','Foreign recovery draft','another-owner',Date.now()+120000],['96385074','Expired recovery draft',owner,Date.now()-1]])localStorage.setItem('diaperscout-product-draft:'+gtin,JSON.stringify({owner:who,expires:expiry,step:1,draft:{productName:name,brandName:'Test brand',contributionId:crypto.randomUUID()}}));}",user.Subject);
        await page.ReloadAsync();await Assertions.Expect(page.Locator("[data-backpack-draft-summary]")).ToHaveTextAsync("You have 1 draft waiting to be finished.");
        await page.EvaluateAsync("document.activeElement?.blur()");
        await AtlasBrowserTests.Evidence(page,"backpack-all-menus-390");
        var evidence=Environment.GetEnvironmentVariable("DIAPERSCOUT_BROWSER_EVIDENCE");
        if(!string.IsNullOrEmpty(evidence)){
            await page.EvaluateAsync("window.scrollTo(0,document.documentElement.scrollHeight)");
            await page.ScreenshotAsync(new(){Path=Path.Combine(evidence,"backpack-menus-lower-390.png")});
        }
        await page.Locator(".backpack-menu[href='/backpack/journey']").ClickAsync();
        await Assertions.Expect(page.Locator("[data-personal-content]")).ToContainTextAsync("Owned recovery draft");
        await Assertions.Expect(page.Locator("[data-personal-content]")).Not.ToContainTextAsync("Foreign recovery");
        await Assertions.Expect(page.Locator("[data-personal-content]")).Not.ToContainTextAsync("Expired recovery");
        await page.Locator(".backpack-personal-item a").ClickAsync();
        await Assertions.Expect(page.Locator("#proposal-name")).ToHaveValueAsync("Owned recovery draft");
        await page.GotoAsync(client.BaseAddress+"backpack/discoveries");
        await Assertions.Expect(page.Locator("[data-personal-content]")).ToContainTextAsync("Personal test shop");
        await page.GotoAsync(client.BaseAddress+"backpack/settings");
        await Assertions.Expect(page.Locator("#explorer-name")).ToHaveValueAsync(name);
        await Assertions.Expect(page.Locator("[data-account-email]")).ToHaveTextAsync("personal-browser@example.test");
        Assert.Equal(400,await page.EvaluateAsync<int>("async()=> (await fetch('/backpack/data/name',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({displayName:'CSRF attempt'})})).status"));
        await page.GetByLabel("Explorer name",new(){Exact=true}).FillAsync("Updated "+name);
        await page.GetByRole(AriaRole.Button,new(){Name="Save name",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator("[data-profile-message]")).ToHaveTextAsync("Explorer name saved.");
        await page.Locator("[data-keep-product-drafts]").UncheckAsync();
        await Assertions.Expect(page.Locator("[data-preference-message]")).ToHaveTextAsync("Preference saved on this device.");
        Assert.False(await page.EvaluateAsync<bool>("async()=> (await import('/js/contribution-draft.js')).saveDraft('5012345678900',{productName:'Overwrite attempt'},null,1)"));
        Assert.Contains("Owned recovery draft",await page.EvaluateAsync<string>("localStorage.getItem('diaperscout-product-draft:5012345678900')"));
        await page.Locator("[data-keep-product-drafts]").CheckAsync();
        await page.GotoAsync(client.BaseAddress+"backpack");await Assertions.Expect(page.Locator("[data-backpack-name]")).ToHaveTextAsync("Updated "+name);
        await page.Locator(".backpack-signout").ClickAsync();await page.GotoAsync(client.BaseAddress+"backpack/settings");
        await Assertions.Expect(page.Locator("[data-backpack-settings]")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();await context.CloseAsync();
    }
}
