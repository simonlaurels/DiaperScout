using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using DiaperScout.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class PasskeyBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Category", "Browser")]
    public async Task NativeWebAuthn_profileless_account_contributes_and_auth_continuation_preserves_draft(bool authenticatedFirst)
    {
        string origin = "http://localhost";
        using var apiBase = new ObservationApiFactory(fixture);
        using var api = apiBase.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Authentication:Passkeys:Origins:0"] = origin, ["Authentication:Development:Enabled"] = "false", ["Authentication:Production:InternalSecret"] = "contribution-browser-test-only-key" })));
        using var webBase = new PasskeyWebFactory(api);
        using var web = webBase.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string,string?> { ["Authentication:Production:InternalSecret"] = "contribution-browser-test-only-key" })));
        web.UseKestrel(0);
        using var client = web.CreateClient();
        var uri = new UriBuilder(client.BaseAddress!) { Host = "localhost" };
        origin = uri.Uri.GetLeftPart(UriPartial.Authority);

        var user = new User("browser-passkey-" + Guid.NewGuid().ToString("N"));
        var email = new UserEmail(user.Id, user.Subject + "@example.test");
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await using (var db = fixture.CreateDbContext())
        {
            db.AddRange(user, email, new MagicLinkToken(email.Id,
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant(), DateTimeOffset.UtcNow.AddMinutes(5)));
            await db.SaveChangesAsync();
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1280, Height = 900 } });
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        var cdp = await context.NewCDPSessionAsync(page);
        await cdp.SendAsync("WebAuthn.enable", new Dictionary<string, object> { ["enableUI"] = false });
        var authenticator = await cdp.SendAsync("WebAuthn.addVirtualAuthenticator", new Dictionary<string, object>
        {
            ["options"] = new { protocol = "ctap2", transport = "internal", hasResidentKey = true,
                hasUserVerification = true, isUserVerified = true, automaticPresenceSimulation = true }
        });

        var authenticatorId = authenticator!.Value.GetProperty("authenticatorId").GetString()!;
        var initial = await page.GotoAsync(origin + "/signin/magic-link?token=" + token);
        Assert.True(initial?.Ok, $"Sign-in landing page {page.Url}: {initial?.Status}. {await page.Locator("body").InnerTextAsync()}");
        await page.GetByRole(AriaRole.Link, new() { Name = "Passkeys", Exact = true }).ClickAsync();
        await page.GetByLabel("Passkey name", new() { Exact = true }).FillAsync("Browser test passkey");
        await page.GetByRole(AriaRole.Button, new() { Name = "Add a passkey" }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-passkey-message]")).ToHaveTextAsync("Passkey added. You can now use it to sign in.");
        await Assertions.Expect(page.Locator("[data-passkey-list]")).ToContainTextAsync("Browser test passkey");
        await page.GetByRole(AriaRole.Link, new() { Name = "Sign out", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(origin + "/");
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Sign in", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Sign out", Exact = true })).ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Passkeys", Exact = true })).ToHaveCountAsync(0);
        var contributionTarget = "/contribute/product?gtin=96385074";
        if (authenticatedFirst) {
            await context.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});");
            await page.GotoAsync(origin);
            await page.Locator("#pwa-welcome [data-passkey-action='signin']").ClickAsync();
            await Assertions.Expect(page.Locator("a[href='/signout']")).ToHaveCountAsync(1);
            await Assertions.Expect(page.Locator("#pwa-welcome")).ToHaveCountAsync(0);
            await page.GotoAsync(origin + contributionTarget);
        } else {
            await page.GotoAsync(origin + "/scan");
            await page.WaitForFunctionAsync("() => document.documentElement.dataset.pwaState === 'ready'");
            await page.Locator("#gtin").FillAsync("96385074");
            await page.GetByRole(AriaRole.Button,new(){Name="Look up",Exact=true}).ClickAsync();
            await page.GetByRole(AriaRole.Link,new(){Name="Add product",Exact=true}).ClickAsync();
        }
        await page.Locator("#proposal-brand").FillAsync("Passkey regression brand");
        await page.Locator("#proposal-name").FillAsync("Passkey regression " + user.Id);
        await page.Locator("#proposal-manufacturer").FillAsync("Regression manufacturer");
        await page.GetByRole(AriaRole.Button,new(){Name="Continue to pack",Exact=true}).ClickAsync();
        await page.Locator("#proposal-size").FillAsync("S");
        await page.Locator("#proposal-quantity").FillAsync("12");
        await page.GetByRole(AriaRole.Button,new(){Name="Review proposal",Exact=true}).ClickAsync();
        var saved = await page.EvaluateAsync<string>("localStorage.getItem('diaperscout-product-draft:96385074')");
        if (!authenticatedFirst) {
            await page.GetByRole(AriaRole.Button,new(){Name="Sign in to submit",Exact=true}).ClickAsync();
            // New tab shares the protected continuation cookie and local draft, but no sessionStorage.
            await page.WaitForURLAsync("**/signin?returnUrl=**");
            var signInUrl = page.Url;
            var credentials = await cdp.SendAsync("WebAuthn.getCredentials", new Dictionary<string,object>{["authenticatorId"] = authenticatorId});
            var newPage = await context.NewPageAsync();
            var newCdp = await context.NewCDPSessionAsync(newPage);
            await newCdp.SendAsync("WebAuthn.enable", new Dictionary<string,object>{["enableUI"]=false});
            var newAuthenticator = await newCdp.SendAsync("WebAuthn.addVirtualAuthenticator", new Dictionary<string,object>{["options"] = new { protocol="ctap2",transport="internal",hasResidentKey=true,hasUserVerification=true,isUserVerified=true,automaticPresenceSimulation=true }});
            foreach(var credential in credentials!.Value.GetProperty("credentials").EnumerateArray())
                await newCdp.SendAsync("WebAuthn.addCredential",new Dictionary<string,object>{["authenticatorId"]=newAuthenticator!.Value.GetProperty("authenticatorId").GetString()!,["credential"]=credential});
            await page.CloseAsync();
            page = newPage;
            await page.GotoAsync(signInUrl);
            await page.GetByRole(AriaRole.Button,new(){Name="Sign in", Exact=true}).ClickAsync();
            await page.WaitForURLAsync(origin + contributionTarget);
        }
        await Assertions.Expect(page.Locator(".contribution-review")).ToContainTextAsync("Passkey regression " + user.Id);
        await Assertions.Expect(page.Locator(".contribution-review")).ToContainTextAsync("Regression manufacturer");
        await Assertions.Expect(page.Locator(".contribution-review")).ToContainTextAsync("12");
        await Assertions.Expect(page.GetByRole(AriaRole.Button,new(){Name="Sign in to submit",Exact=true})).ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByRole(AriaRole.Link,new(){Name="Sign out",Exact=true})).ToBeVisibleAsync();
        var restored = await page.EvaluateAsync<string>("localStorage.getItem('diaperscout-product-draft:96385074')");
        Assert.Equal(System.Text.Json.JsonDocument.Parse(saved).RootElement.GetProperty("draft").GetProperty("contributionId").GetString(), System.Text.Json.JsonDocument.Parse(restored).RootElement.GetProperty("draft").GetProperty("contributionId").GetString());
        await page.GetByRole(AriaRole.Button,new(){Name="Submit for review",Exact=true}).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Sent for review",Exact=true})).ToBeVisibleAsync();
        await using (var db = fixture.CreateDbContext()) {
            var submission = await db.CatalogueSubmissions.SingleAsync(s=>s.SubmittedByUserId==user.Id);
            Assert.Equal("96385074",submission.ProposedGtin);
            Assert.Equal("S", await (from v in db.CatalogueSubmissionVariants join size in db.CatalogueSubmissionSizeVariants on v.Id equals size.VariantId where v.SubmissionId==submission.Id select size.ManufacturerSize).SingleAsync());
            Assert.Equal(12,submission.ProposedPackQuantity);
            Assert.Null(submission.PublishedProductId);
            Assert.False(await db.ExplorerProfiles.AnyAsync(p=>p.UserId==user.Id));
            Assert.Equal(1,await db.CatalogueSubmissions.CountAsync(s=>s.SubmittedByUserId==user.Id));
        }
        Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('diaperscout-product-draft:96385074')"));
        await page.GetByRole(AriaRole.Link, new() { Name = "Passkeys", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-passkey-list]")).ToContainTextAsync("Last used");
        await page.SetViewportSizeAsync(390, 844);
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Passkeys", Exact = true })).ToBeVisibleAsync();
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth"), "Passkey management should fit a mobile viewport.");
        var artifactDirectory = Environment.GetEnvironmentVariable("DIAPERSCOUT_TEST_ARTIFACTS");
        if (!string.IsNullOrWhiteSpace(artifactDirectory))
        {
            Directory.CreateDirectory(artifactDirectory);
            await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDirectory, "passkeys-mobile.png"), FullPage = true });
        }
        page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Remove Browser test passkey" }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-passkey-list]")).ToHaveTextAsync("You haven’t added a passkey yet.");
        Assert.Empty(errors);
    }
}
