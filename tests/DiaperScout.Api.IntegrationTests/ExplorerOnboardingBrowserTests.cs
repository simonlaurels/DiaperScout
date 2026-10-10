using DiaperScout.Application;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class ExplorerOnboardingBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData(true,"chromium")]
    [InlineData(false,"chromium")]
    [InlineData(false,"webkit")]
    [Trait("Category","Browser")]
    public async Task Installed_onboarding_verifies_email_recovers_cancelled_passkey_and_matches_Backpack(bool createPasskey,string engine)
    {
        var mail=new OnboardingMail();using var apiBase=OnboardingMail.Api(fixture,mail);
        using var api=apiBase.WithWebHostBuilder(b=>b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?> {
            ["Authentication:Passkeys:Origins:0"]="http://localhost",["Authentication:Development:Enabled"]="false",["Authentication:Production:InternalSecret"]="onboarding-isolated-browser-key"})));
        using var webBase=new PasskeyWebFactory(api);
        using var web=webBase.WithWebHostBuilder(b=>b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{["Authentication:Production:InternalSecret"]="onboarding-isolated-browser-key"})));
        web.UseKestrel(0);using var client=web.CreateClient();var origin=new UriBuilder(client.BaseAddress!){Host="localhost"}.Uri.GetLeftPart(UriPartial.Authority);
        api.Services.GetRequiredService<IConfiguration>()["Authentication:Passkeys:Origins:0"]=origin;
        using var playwright=await Playwright.CreateAsync();await using var browser=await (engine=="webkit"?playwright.Webkit:playwright.Chromium).LaunchAsync(new(){Headless=true});
        await using var context=await browser.NewContextAsync(new(){ViewportSize=new(){Width=engine=="webkit"?375:390,Height=844},ServiceWorkers=ServiceWorkerPolicy.Block});
        await context.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});localStorage.setItem('ds-welcome-complete-v1','yes');");
        var responseStatuses=new List<string>();context.Response+=(_,r)=>{if(new Uri(r.Url).AbsolutePath.StartsWith("/account/passkeys"))responseStatuses.Add(new Uri(r.Url).AbsolutePath+" "+r.Status);};
        var page=await context.NewPageAsync();var errors=new List<string>();page.PageError+=(_,e)=>errors.Add(page.Url.Split('?')[0]+": "+e);
        await page.GotoAsync(origin+"/join");
        Assert.False(await page.Locator(".ds-app-header").IsVisibleAsync());Assert.Equal(0,await page.Locator(".pwa-mobile-nav").CountAsync());
        await AtlasBrowserTests.Evidence(page,"onboarding-intro-"+engine);
        await page.GetByRole(AriaRole.Link,new(){Name="Get started",Exact=true}).ClickAsync();
        await AtlasBrowserTests.Evidence(page,"onboarding-details-"+engine);
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth<=innerWidth"));
        var name="Browser Zoë "+Guid.NewGuid().ToString("N")[..6];var email="onboard-browser-"+Guid.NewGuid()+"@example.test";
        await page.GetByLabel("Name or nickname",new(){Exact=false}).FillAsync("   ");await page.GetByLabel("Email address",new(){Exact=false}).FillAsync(email);
        await page.GetByRole(AriaRole.Button,new(){Name="Continue",Exact=true}).ClickAsync();Assert.EndsWith("/join/details",page.Url);Assert.Empty(mail.Messages);
        await page.GetByLabel("Name or nickname",new(){Exact=false}).FillAsync("  "+name+"  ");
        var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await page.RouteAsync("**/join/data/request",async route=>{await gate.Task;await route.ContinueAsync();});
        await page.GetByRole(AriaRole.Button,new(){Name="Continue",Exact=true}).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button,new(){Name="Sending link…",Exact=true})).ToBeDisabledAsync();gate.SetResult();
        await page.WaitForURLAsync("**/join/check-email");await Assertions.Expect(page.Locator("[data-onboarding-email]")).ToHaveTextAsync(email);
        await AtlasBrowserTests.Evidence(page,"onboarding-email-"+engine+"-"+createPasskey);
        await page.CloseAsync(); // Lose all transient page/circuit state before following the email.
        page=await context.NewPageAsync();page.PageError+=(_,e)=>errors.Add(page.Url.Split('?')[0]+": "+e);
        if(engine=="chromium") {
        var cdp=await context.NewCDPSessionAsync(page);await cdp.SendAsync("WebAuthn.enable",new Dictionary<string,object>{["enableUI"]=false});
        await cdp.SendAsync("WebAuthn.addVirtualAuthenticator",new Dictionary<string,object>{["options"]=new{protocol="ctap2",transport="internal",hasResidentKey=true,hasUserVerification=true,isUserVerified=true,automaticPresenceSimulation=true}});
        } else await page.AddInitScriptAsync("window.PublicKeyCredential=undefined;");
        await page.GotoAsync(origin+mail.Link(email));await Assertions.Expect(page.Locator("[data-onboarding-verified]")).ToBeVisibleAsync();
        await page.ReloadAsync();await Assertions.Expect(page.Locator("[data-onboarding-verified]")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("[data-onboarding-setup]")).ToBeHiddenAsync();
        await AtlasBrowserTests.Evidence(page,"onboarding-verified-"+engine+"-"+createPasskey);
        await page.GetByRole(AriaRole.Link,new(){Name="Continue",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator("[data-onboarding-setup]")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("[data-onboarding-name]")).ToHaveTextAsync(name);
        await Assertions.Expect(page.Locator("[data-onboarding-key]")).ToBeHiddenAsync();
        await page.ReloadAsync();await Assertions.Expect(page.Locator("[data-onboarding-setup]")).ToBeVisibleAsync();
        if(engine=="chromium") {
        await page.EvaluateAsync("() => {window.originalCredentialCreate=navigator.credentials.create.bind(navigator.credentials);navigator.credentials.create=async()=>{throw new DOMException('cancelled','NotAllowedError')}}");
        await page.GetByRole(AriaRole.Button,new(){Name="Create passkey",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator("[data-passkey-message]")).ToContainTextAsync("cancelled");
        await Assertions.Expect(page.GetByRole(AriaRole.Button,new(){Name="Maybe later",Exact=true})).ToBeEnabledAsync();
        await page.EvaluateAsync("() => {navigator.credentials.create=window.originalCredentialCreate}");
        await page.RouteAsync("**/account/passkeys/options",r=>r.FulfillAsync(new(){Status=503,ContentType="application/json",Body="{}"}));
        await page.GetByRole(AriaRole.Button,new(){Name="Create passkey",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator("[data-passkey-message]")).ToContainTextAsync("try again");
        await Assertions.Expect(page.GetByRole(AriaRole.Button,new(){Name="Maybe later",Exact=true})).ToBeEnabledAsync();
        await page.UnrouteAsync("**/account/passkeys/options");
        } else await Assertions.Expect(page.Locator("[data-passkey-message]")).ToContainTextAsync("supported");
        await AtlasBrowserTests.Evidence(page,"onboarding-passkey-"+engine+"-"+createPasskey);
        await page.GetByRole(AriaRole.Button,new(){Name=createPasskey?"Create passkey":"Maybe later",Exact=true}).ClickAsync();
        try { await page.WaitForURLAsync("**/join/ready",new(){Timeout=15000}); } catch { throw new Xunit.Sdk.XunitException(await page.Locator("[data-passkey-message]").InnerTextAsync()+"; "+string.Join("; ",responseStatuses)); } await Assertions.Expect(page.Locator("[data-onboarding-success]")).ToBeVisibleAsync();
        Assert.Equal(createPasskey,await page.Locator("[data-onboarding-key]").IsVisibleAsync());
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth<=innerWidth"));
        await Assertions.Expect(page.Locator("[data-onboarding-success-name]")).ToHaveTextAsync(name);
        Assert.Equal(!createPasskey,await page.Locator("[data-onboarding-skipped-id]").IsVisibleAsync());
        Assert.Equal(createPasskey,await page.Locator("[data-onboarding-key-confirmed]").IsVisibleAsync());
        await page.WaitForFunctionAsync("Array.from(document.querySelectorAll('img[src^=\"/images/onboarding/\"]')).filter(img => img.offsetParent !== null).every(img => img.complete && img.naturalWidth > 0)");
        foreach(var image in await page.Locator("img[src^='/images/onboarding/']:visible").AllAsync())
            Assert.True(await image.EvaluateAsync<bool>("img => img.complete && img.naturalWidth > 0"));
        await AtlasBrowserTests.Evidence(page,"onboarding-ready-"+engine+"-"+createPasskey);
        await page.GetByRole(AriaRole.Link,new(){Name="Go to my Backpack",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator("[data-backpack-name]")).ToHaveTextAsync(name);
        await Assertions.Expect(page.Locator(".backpack-hanging-key")).ToHaveCountAsync(createPasskey?1:0);
        await page.Locator(".backpack-signout").ClickAsync();await page.WaitForURLAsync(origin+"/");await page.WaitForFunctionAsync("() => document.documentElement.dataset.pwaState === 'ready'");await page.GotoAsync(origin+"/signin");
        await page.GetByLabel("Email address",new(){Exact=true}).FillAsync(email);
        await page.GetByRole(AriaRole.Button,new(){Name="Email me a sign-in link",Exact=true}).ClickAsync();await page.WaitForURLAsync("**/signin?sent=true");
        await page.GotoAsync(origin+mail.Link(email));await page.WaitForFunctionAsync("() => document.documentElement.dataset.pwaState === 'ready'");await page.GotoAsync(origin+"/backpack");await Assertions.Expect(page.Locator("[data-backpack-name]")).ToHaveTextAsync(name);
        Assert.True(errors.Count==0,string.Join("\n",errors));
    }
}
