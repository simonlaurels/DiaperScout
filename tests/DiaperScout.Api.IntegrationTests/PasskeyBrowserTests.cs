using System.Security.Cryptography;
using System.Text;
using DiaperScout.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class PasskeyBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    [Trait("Category", "Browser")]
    public async Task NativeWebAuthn_AddSignInAndRemovePasskey()
    {
        string origin = "http://localhost";
        using var apiBase = new ObservationApiFactory(fixture);
        using var api = apiBase.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Authentication:Passkeys:Origins:0"] = origin })));
        using var web = new PasskeyWebFactory(api);
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
        await cdp.SendAsync("WebAuthn.addVirtualAuthenticator", new Dictionary<string, object>
        {
            ["options"] = new { protocol = "ctap2", transport = "internal", hasResidentKey = true,
                hasUserVerification = true, isUserVerified = true, automaticPresenceSimulation = true }
        });

        var initial = await page.GotoAsync(origin + "/signin/magic-link?token=" + token);
        Assert.True(initial?.Ok, $"Sign-in landing page {page.Url}: {initial?.Status}. {await page.Locator("body").InnerTextAsync()}");
        await page.GetByRole(AriaRole.Link, new() { Name = "Passkeys", Exact = true }).ClickAsync();
        await page.GetByLabel("Passkey name", new() { Exact = true }).FillAsync("Browser test passkey");
        await page.GetByRole(AriaRole.Button, new() { Name = "Add a passkey" }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-passkey-message]")).ToHaveTextAsync("Passkey added. You can now use it to sign in.");
        await Assertions.Expect(page.Locator("[data-passkey-list]")).ToContainTextAsync("Browser test passkey");
        await page.GetByRole(AriaRole.Link, new() { Name = "Sign out", Exact = true }).ClickAsync();
        await page.GotoAsync(origin + "/signin");
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Email me a sign-in link" })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign in with a passkey" }).ClickAsync();
        await page.WaitForURLAsync(origin + "/products");
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
