extern alias DiaperScoutWeb;

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class PasskeyWebTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MagicLinkThenPasskey_IssuesWorkingCookieAndKeepsAccountRoles(bool administrator)
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api);
        using var browser = Browser(web);
        var (user, token) = await SeedUser(administrator);
        var signedIn = await browser.GetAsync("/signin/magic-link?token=" + token);
        Assert.Equal(HttpStatusCode.Redirect, signedIn.StatusCode);
        Assert.Contains(signedIn.Headers.GetValues("Set-Cookie"), value => value.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        var csrf = await Csrf(browser, "/account/passkeys");
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsJsonAsync("/account/passkeys/options", new { })).StatusCode);
        using var key = new TestPasskey();
        var started = await Post(browser, "/account/passkeys/options", new { }, csrf);
        started.EnsureSuccessStatusCode();
        var options = (await started.Content.ReadFromJsonAsync<PasskeyOptions>())!;
        var registration = await Post(browser, "/account/passkeys/verify", new { requestId = options.RequestId, credential = key.Register(options), name = "My test phone" }, csrf);
        registration.EnsureSuccessStatusCode();
        var item = Assert.Single((await browser.GetFromJsonAsync<PasskeyItem[]>("/account/passkeys/list"))!);
        await browser.GetAsync("/signout");
        var signinHtml = await browser.GetStringAsync("/signin");
        Assert.Contains("/signin/request", signinHtml);
        Assert.Contains("data-passkey-action=\"signin\"", signinHtml);
        csrf = Token(signinHtml);
        var loginOptionsResponse = await Post(browser, "/signin/passkey/options", new { }, csrf);
        loginOptionsResponse.EnsureSuccessStatusCode();
        var loginOptions = (await loginOptionsResponse.Content.ReadFromJsonAsync<PasskeyOptions>())!;
        var assertion = new { requestId = loginOptions.RequestId, credential = key.Assert(loginOptions) };
        using var otherBrowser = Browser(web);
        var otherCsrf = await Csrf(otherBrowser, "/signin");
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(otherBrowser, "/signin/passkey/verify", assertion, otherCsrf)).StatusCode);
        var login = await Post(browser, "/signin/passkey/verify", assertion, csrf);
        login.EnsureSuccessStatusCode();
        Assert.Contains(login.Headers.GetValues("Set-Cookie"), value => value.StartsWith("DiaperScout.DevelopmentAuth="));
        Assert.Single((await browser.GetFromJsonAsync<PasskeyItem[]>("/account/passkeys/list"))!);
        var adminPage = await browser.GetAsync("/admin/users");
        if (administrator)
        {
            adminPage.EnsureSuccessStatusCode();
            Assert.Contains(user.Subject + "@example.test", await adminPage.Content.ReadAsStringAsync());
        }
        else Assert.NotEqual(HttpStatusCode.OK, adminPage.StatusCode);
        csrf = await Csrf(browser, "/account/passkeys");
        using var remove = new HttpRequestMessage(HttpMethod.Delete, "/account/passkeys/" + item.Id);
        remove.Headers.Add("X-CSRF-TOKEN", csrf);
        Assert.Equal(HttpStatusCode.NoContent, (await browser.SendAsync(remove)).StatusCode);
    }

    [Fact]
    public async Task Management_RequiresRecentSignIn_AndAnonymousOptionsRequireCsrf()
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api);
        using var browser = Browser(web);
        Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/account/passkeys")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsJsonAsync("/signin/passkey/options", new { })).StatusCode);
        var (user, _) = await SeedUser(false);
        var cookieOptions = web.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get("DevelopmentCookie");
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim("sub", user.Subject),
            new Claim("auth_time", DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds().ToString())
        }, "DevelopmentCookie"));
        var ticket = new AuthenticationTicket(principal, new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) }, "DevelopmentCookie");
        browser.DefaultRequestHeaders.Add("Cookie", cookieOptions.Cookie.Name + "=" + cookieOptions.TicketDataFormat.Protect(ticket));
        var csrf = await Csrf(browser, "/account/passkeys");
        var response = await Post(browser, "/account/passkeys/options", new { }, csrf);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("sign in again", await response.Content.ReadAsStringAsync());
        using var remove = new HttpRequestMessage(HttpMethod.Delete, "/account/passkeys/" + Guid.NewGuid());
        remove.Headers.Add("X-CSRF-TOKEN", csrf);
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.SendAsync(remove)).StatusCode);
    }

    private async Task<(User User, string Token)> SeedUser(bool administrator)
    {
        var user = new User("passkey-web-" + Guid.NewGuid().ToString("N"));
        var email = new UserEmail(user.Id, user.Subject + "@example.test");
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await using var db = fixture.CreateDbContext();
        db.AddRange(user, email, new MagicLinkToken(email.Id,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant(), DateTimeOffset.UtcNow.AddMinutes(5)));
        if (administrator) db.Add(new PrivilegedRoleAssignment(user.Id, PrivilegedRole.Administrator, fixture.AdministratorUserId, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
        return (user, token);
    }

    private static HttpClient Browser(PasskeyWebFactory web) => web.CreateClient(new WebApplicationFactoryClientOptions
    { BaseAddress = new Uri("https://localhost:7167"), AllowAutoRedirect = false });
    private static async Task<string> Csrf(HttpClient browser, string page) => Token(await browser.GetStringAsync(page));
    private static string Token(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "The page must render an antiforgery token.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    private static Task<HttpResponseMessage> Post(HttpClient browser, string url, object body, string csrf)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return browser.SendAsync(request);
    }
}
