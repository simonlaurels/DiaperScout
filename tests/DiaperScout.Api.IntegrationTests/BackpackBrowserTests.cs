using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class BackpackBrowserTests(PostgreSqlFixture fixture):IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData(390,0,false)]
    [InlineData(430,1,true)]
    [InlineData(430,1,false)]
    [InlineData(390,3,false)]
    [Trait("Category","Browser")]
    public async Task Actual_identity_and_zero_one_multiple_registered_keys_use_existing_security_flows(int width,int count,bool webkit)
    {
        using var api=new ObservationApiFactory(fixture);
        using var owner=api.CreateClient();owner.DefaultRequestHeaders.Add("X-Development-Subject",PostgreSqlFixture.ExplorerSubject);
        await using(var db=fixture.CreateDbContext()) {
            await db.PasskeyCredentials.Where(k=>k.UserId==fixture.ExplorerUserId).ExecuteDeleteAsync();
            if(!await db.UserEmails.AnyAsync(e=>e.UserId==fixture.ExplorerUserId)) {
                db.Add(new UserEmail(fixture.ExplorerUserId,"backpack-test@example.test"));
                await db.SaveChangesAsync();
            }
        }
        for(var i=0;i<count;i++){
            using var key=new TestPasskey();var binding=Guid.NewGuid().ToString();
            var begin=await owner.PostAsJsonAsync("/api/v1/account/passkeys/options",new BeginPasskeyRequest(binding));begin.EnsureSuccessStatusCode();
            var options=(await begin.Content.ReadFromJsonAsync<PasskeyOptions>())!;
            (await owner.PostAsJsonAsync("/api/v1/account/passkeys/verify",new CompletePasskeyRequest(options.RequestId,binding,key.Register(options),"Test-only key "+i))).EnsureSuccessStatusCode();
        }
        var identity=(await owner.GetFromJsonAsync<ExplorerIdentity>("/api/v1/me/explorer"))!;
        using var webBase=new PasskeyWebFactory(api);
        using var web=webBase.WithWebHostBuilder(builder=>builder.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{["Authentication:Development:Subject"]=PostgreSqlFixture.ExplorerSubject})));
        web.UseKestrel(0);using var client=web.CreateClient();using var playwright=await Playwright.CreateAsync();
        await using var browser=await (webkit?playwright.Webkit:playwright.Chromium).LaunchAsync(new(){Headless=true});
        var context=await browser.NewContextAsync(new(){ViewportSize=new(){Width=width,Height=844},ServiceWorkers=ServiceWorkerPolicy.Block});
        await context.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});localStorage.setItem('ds-welcome-complete-v1','yes');");
        var page=await context.NewPageAsync();await page.GotoAsync(client.BaseAddress+"backpack");
        await Assertions.Expect(page.Locator(".backpack-guest")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("[data-backpack-account]")).ToHaveCountAsync(0);
        await page.GotoAsync(client.BaseAddress+"signin/development");await page.GotoAsync(client.BaseAddress+"backpack");
        await Assertions.Expect(page.Locator("[data-backpack-name]")).ToHaveTextAsync(identity.DisplayName);
        await Assertions.Expect(page.Locator(".backpack-key")).ToHaveCountAsync(count);
        await page.WaitForFunctionAsync("document.querySelector('.backpack-scene')?.naturalWidth > 0");
        Assert.True(await page.Locator("[data-backpack-name]").EvaluateAsync<bool>("e=>{const a=e.getBoundingClientRect(),b=e.closest('.backpack-id').getBoundingClientRect();return a.top>=b.top && a.bottom<=b.bottom;}"));
        var evidence=Environment.GetEnvironmentVariable("DIAPERSCOUT_BROWSER_EVIDENCE");
        if(!string.IsNullOrEmpty(evidence)){
            Directory.CreateDirectory(evidence);
            await page.EvaluateAsync("document.activeElement?.blur()");
            await page.ScreenshotAsync(new(){Path=Path.Combine(evidence,$"backpack-preview-{count}-{width}-{(webkit?"webkit":"chromium")}.png")});
        }
        if(count==0)await Assertions.Expect(page.Locator("[data-backpack-passkey-copy]")).ToContainTextAsync("There’s room for a key");
        else {
            await Assertions.Expect(page.Locator(".backpack-key").First).ToContainTextAsync("Test-only key 0");
            await Assertions.Expect(page.Locator(".backpack-key").First).ToHaveAttributeAsync("href","/backpack/passkeys");
        }
        await Assertions.Expect(page.Locator(".backpack-menu-grid a")).ToHaveCountAsync(4);
        var info=await page.EvaluateAsync<string>("async()=>JSON.stringify(await (await fetch('/backpack/identity')).json())");
        Assert.DoesNotContain("userId",info);Assert.DoesNotContain("subject",info);Assert.DoesNotContain("email",info);
        var text=await page.Locator("[data-backpack-account]").InnerTextAsync();Assert.DoesNotContain(PostgreSqlFixture.ExplorerSubject,text);Assert.DoesNotContain("Member since",text);
        await Assertions.Expect(page.Locator(".ds-app-header")).ToBeHiddenAsync();
        await Assertions.Expect(page.Locator(".pwa-mobile-nav")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator(".pwa-mobile-nav a[aria-current]")).ToHaveAttributeAsync("href","/backpack");
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth<=innerWidth"));
        await page.EvaluateAsync("window.scrollTo(0,document.documentElement.scrollHeight)");
        Assert.True(await page.Locator(".backpack-encouragement a").EvaluateAsync<bool>("e=>e.getBoundingClientRect().bottom<=document.querySelector('.pwa-mobile-nav').getBoundingClientRect().top"));
        await page.EvaluateAsync("window.scrollTo(0,0)");await AtlasBrowserTests.Evidence(page,$"backpack-{count}-{width}");
        await page.Locator(".backpack-menu-grid a[href='/backpack/passkeys']").ClickAsync();
        await Assertions.Expect(page.Locator(".ds-passkey-item")).ToHaveCountAsync(count);
        // The Passkeys menu reaches the unchanged registration/security flow.
        await page.EvaluateAsync("Object.defineProperty(window,'PublicKeyCredential',{value:function(){},configurable:true});Object.defineProperty(navigator,'credentials',{value:{create:async()=>{window.ceremonyCalled=true;throw new DOMException('Cancelled','NotAllowedError');}},configurable:true});");
        await page.GetByLabel("Passkey name",new(){Exact=true}).FillAsync("Cancelled test key");
        await page.Locator("[data-passkey-action='register']").ClickAsync();
        await Assertions.Expect(page.Locator("[data-passkey-message]")).ToContainTextAsync("cancelled");Assert.True(await page.EvaluateAsync<bool>("window.ceremonyCalled===true"));
        if(count>0){
            page.Dialog += async (_,dialog)=>{Assert.Contains("email link",dialog.Message);await dialog.AcceptAsync();};
            await page.Locator(".ds-passkey-item button").First.ClickAsync();
            await Assertions.Expect(page.Locator("[data-passkey-message]")).ToHaveTextAsync("Passkey removed.");
            await Assertions.Expect(page.Locator(".ds-passkey-item")).ToHaveCountAsync(count-1);
        }
        await page.Locator(".pwa-mobile-nav a[href='/backpack']").ClickAsync();
        await Assertions.Expect(page.Locator("[data-backpack-name]")).ToHaveTextAsync(identity.DisplayName);
        await Assertions.Expect(page.Locator(".backpack-key")).ToHaveCountAsync(Math.Max(0,count-1));
        await page.Locator(".backpack-signout").ClickAsync();await page.WaitForURLAsync(client.BaseAddress!.ToString());
        await page.GotoAsync(client.BaseAddress+"backpack");await Assertions.Expect(page.Locator(".backpack-guest")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();await context.CloseAsync();
    }
}
