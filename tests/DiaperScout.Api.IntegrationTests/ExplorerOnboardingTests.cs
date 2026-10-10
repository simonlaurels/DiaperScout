using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Resend;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class ExplorerOnboardingTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData("", "valid@example.test")]
    [InlineData("   ", "valid@example.test")]
    [InlineData("Name", "invalid")]
    [InlineData("Name", "a@@example.test")]
    public async Task Invalid_details_do_not_create_pending_identity(string name, string email)
    {
        var mail = new OnboardingMail(); using var api = OnboardingMail.Api(fixture, mail);
        using var client = api.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/registration-link", new { email, displayName = name })).StatusCode);
        Assert.Empty(mail.Messages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Verified_signup_resumes_with_one_profile_and_optional_real_passkey(bool createPasskey)
    {
        var mail = new OnboardingMail(); using var api = OnboardingMail.Api(fixture, mail);
        using var web = new PasskeyWebFactory(api); using var browser = Browser(web);
        var email = "onboarding-" + Guid.NewGuid().ToString("N") + "@example.test";
        var name = "Zoë 李 " + Guid.NewGuid().ToString("N")[..6];
        var csrf = await Csrf(browser, "/join/details");
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsJsonAsync("/join/data/request", new { email, displayName = name })).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/join/continue")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await browser.PostAsJsonAsync("/account/passkeys/options", new { })).StatusCode);
        (await Post(browser, "/join/data/request", new { email, displayName = "  " + name + "  " }, csrf)).EnsureSuccessStatusCode();
        (await Post(browser, "/join/data/request", new { email, displayName = name }, csrf)).EnsureSuccessStatusCode();
        Assert.Single(mail.Messages);
        var pending = await browser.GetFromJsonAsync<JsonElement>("/join/data/pending"); Assert.Equal(email, pending.GetProperty("email").GetString());
        await using (var db = fixture.CreateDbContext()) {
            Assert.False(await db.UserEmails.AnyAsync(e => e.Email == email));
            Assert.Equal(name, (await db.PendingRegistrations.SingleAsync(p => p.Email == email)).DisplayName);
        }
        // A fresh HTTP navigation, not a Blazor circuit, consumes the existing email token.
        var authenticated = await browser.GetAsync(mail.Link(email) + "&returnUrl=https://attacker.example");
        Assert.Equal("/join/continue", authenticated.Headers.Location?.OriginalString);
        csrf = await Csrf(browser, "/join/continue");
        var state = await browser.GetFromJsonAsync<JsonElement>("/join/data/state");
        Assert.Equal(name, state.GetProperty("displayName").GetString()); Assert.Equal("verified", state.GetProperty("stage").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(browser,"/join/data/complete",new { skip = false },csrf)).StatusCode);
        if (createPasskey) {
            using var key = new TestPasskey();
            var optionsResponse = await Post(browser, "/account/passkeys/options", new { }, csrf); optionsResponse.EnsureSuccessStatusCode();
            var options = (await optionsResponse.Content.ReadFromJsonAsync<PasskeyOptions>())!;
            (await Post(browser, "/account/passkeys/verify", new { requestId = options.RequestId, credential = key.Register(options), name = "Passkey" }, csrf)).EnsureSuccessStatusCode();
            Assert.Single((await browser.GetFromJsonAsync<PasskeyItem[]>("/account/passkeys/list"))!);
        }
        (await Post(browser, "/join/data/complete", new { skip = !createPasskey }, csrf)).EnsureSuccessStatusCode();
        state = await browser.GetFromJsonAsync<JsonElement>("/join/data/state");
        Assert.Equal(createPasskey ? "passkey" : "skipped", state.GetProperty("stage").GetString());
        Assert.Equal(createPasskey, state.GetProperty("hasPasskey").GetBoolean());
        Assert.Equal(name, (await browser.GetFromJsonAsync<BackpackAccountInfo>("/backpack/data/account"))!.DisplayName);
        Guid owner;
        await using (var db = fixture.CreateDbContext()) {
            owner = (await db.UserEmails.SingleAsync(e => e.Email == email)).UserId;
            var profile = Assert.Single(await db.ExplorerProfiles.Where(p => p.UserId == owner).ToArrayAsync());
            Assert.Single(await db.Backpacks.Where(b => b.UserId == profile.Id).ToArrayAsync());
        }
        var replay = await browser.GetAsync(mail.Link(email)); Assert.Equal("/join/details?error=true", replay.Headers.Location?.OriginalString);
        Assert.Equal(name, (await browser.GetFromJsonAsync<BackpackAccountInfo>("/backpack/data/account"))!.DisplayName);
        await browser.GetAsync("/signout");
        Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/join/data/state")).StatusCode);
        // Skipping a passkey must leave the real, supported passwordless route usable.
        var signin = await browser.PostAsync("/signin/request", new FormUrlEncodedContent(new Dictionary<string,string>{{"email",email}}));
        Assert.Equal("/signin?sent=true", signin.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.Redirect,(await browser.GetAsync(mail.Link(email))).StatusCode);
        Assert.Equal(email,(await browser.GetFromJsonAsync<BackpackAccountInfo>("/backpack/data/account"))!.Email);
    }

    [Fact]
    public async Task Existing_identity_preserves_profile_and_roles_before_and_after_verification()
    {
        var user = new User("existing-onboarding-" + Guid.NewGuid()); var email = user.Subject + "@example.test";
        var original = "Original " + Guid.NewGuid().ToString("N")[..6];
        await using(var db=fixture.CreateDbContext()){db.AddRange(user,new UserEmail(user.Id,email),new ExplorerProfile(user.Id,original),
            new PrivilegedRoleAssignment(user.Id,PrivilegedRole.Administrator,fixture.AdministratorUserId,DateTimeOffset.UtcNow));await db.SaveChangesAsync();}
        var mail=new OnboardingMail();using var api=OnboardingMail.Api(fixture,mail);using var client=api.CreateClient();
        (await client.PostAsJsonAsync("/api/v1/auth/registration-link",new{email,displayName="Unproven replacement"})).EnsureSuccessStatusCode();
        await using(var db=fixture.CreateDbContext()) Assert.Equal(original,(await db.ExplorerProfiles.SingleAsync(p=>p.UserId==user.Id)).DisplayName);
        var response=await client.PostAsync("/api/v1/auth/magic-link/consume?token="+mail.Token(email),null);response.EnsureSuccessStatusCode();
        var auth=(await response.Content.ReadFromJsonAsync<PasswordlessAuthenticationResult>())!;
        Assert.Equal(user.Id,auth.UserId);Assert.True(auth.ContinueOnboarding);Assert.Contains(PrivilegedRole.Administrator,auth.Roles);
        await using(var db=fixture.CreateDbContext()){Assert.Equal(original,(await db.ExplorerProfiles.SingleAsync(p=>p.UserId==user.Id)).DisplayName);Assert.Single(await db.UserEmails.Where(e=>e.Email==email).ToArrayAsync());}
    }

    [Fact]
    public async Task Resend_reuses_pending_record_supersedes_old_link_and_concurrent_consumption_is_single_use()
    {
        var email="resend-"+Guid.NewGuid()+"@example.test";var name="Resend "+Guid.NewGuid();
        var mail=new OnboardingMail();using var api=OnboardingMail.Api(fixture,mail);using var client=api.CreateClient();
        (await client.PostAsJsonAsync("/api/v1/auth/registration-link",new{email,displayName=name})).EnsureSuccessStatusCode();var old=mail.Token(email);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/v1/auth/registration-link",new{email,displayName="Changed too soon"})).StatusCode);
        await using(var db=fixture.CreateDbContext()){var p=await db.PendingRegistrations.SingleAsync(p=>p.Email==email);db.Entry(p).Property(x=>x.CreatedAtUtc).CurrentValue=DateTimeOffset.UtcNow.AddMinutes(-2);await db.SaveChangesAsync();}
        (await client.PostAsJsonAsync("/api/v1/auth/registration-link",new{email,displayName=name})).EnsureSuccessStatusCode();
        Assert.Equal(2,mail.Messages.Count);Assert.NotEqual(old,mail.Token(email));
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync("/api/v1/auth/magic-link/consume?token="+old,null)).StatusCode);
        var path="/api/v1/auth/magic-link/consume?token="+mail.Token(email);
        var results=await Task.WhenAll(client.PostAsync(path,null),client.PostAsync(path,null));
        Assert.Single(results,r=>r.StatusCode==HttpStatusCode.OK);Assert.Single(results,r=>r.StatusCode==HttpStatusCode.BadRequest);
        await using(var db=fixture.CreateDbContext()) {Assert.Single(await db.PendingRegistrations.Where(p=>p.Email==email).ToArrayAsync());var user=(await db.UserEmails.SingleAsync(e=>e.Email==email)).UserId;Assert.Single(await db.ExplorerProfiles.Where(p=>p.UserId==user).ToArrayAsync());}
    }

    [Fact]
    public async Task Profileless_account_can_establish_and_update_one_owned_profile_and_backpack()
    {
        var user=new User("profileless-onboarding-"+Guid.NewGuid());
        await using(var db=fixture.CreateDbContext()){db.Add(user);await db.SaveChangesAsync();}
        using var api=new ObservationApiFactory(fixture);using var client=api.CreateClient();client.DefaultRequestHeaders.Add("X-Development-Subject",user.Subject);
        var name="Profileless "+Guid.NewGuid();
        foreach(var next in new[]{name,"Renamed "+name})Assert.Equal(HttpStatusCode.NoContent,(await client.PostAsJsonAsync("/api/v1/me/backpack/name",new UpdateExplorerName(next))).StatusCode);
        await using(var db=fixture.CreateDbContext()){var profile=Assert.Single(await db.ExplorerProfiles.Where(p=>p.UserId==user.Id).ToArrayAsync());Assert.Equal("Renamed "+name,profile.DisplayName);Assert.Single(await db.Backpacks.Where(b=>b.UserId==profile.Id).ToArrayAsync());}
    }

    [Fact]
    public async Task Expired_verification_and_failed_delivery_have_recoverable_generic_results()
    {
        var email="expiry-"+Guid.NewGuid()+"@example.test";var mail=new OnboardingMail();using var api=OnboardingMail.Api(fixture,mail);using var web=new PasskeyWebFactory(api);using var browser=Browser(web);
        var csrf=await Csrf(browser,"/join/details");(await Post(browser,"/join/data/request",new{email,displayName="Expiry "+Guid.NewGuid()},csrf)).EnsureSuccessStatusCode();
        await using(var db=fixture.CreateDbContext()){var token=await db.PendingRegistrationTokens.SingleAsync(t=>t.TokenHash==Hash(mail.Token(email)));db.Entry(token).Property(t=>t.ExpiresAtUtc).CurrentValue=DateTimeOffset.UtcNow.AddMinutes(-1);await db.SaveChangesAsync();}
        Assert.Equal("/join/details?error=true",(await browser.GetAsync(mail.Link(email))).Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.Redirect,(await browser.GetAsync("/join/continue")).StatusCode);
        mail.Fail=true;var response=await Post(browser,"/join/data/request",new{email="failed-"+email,displayName="Test"},csrf);
        Assert.False(response.IsSuccessStatusCode);Assert.DoesNotContain("Exception",await response.Content.ReadAsStringAsync());
        mail.Fail=false;
        (await Post(browser,"/join/data/request",new{email="failed-"+email,displayName="Test"},csrf)).EnsureSuccessStatusCode();
        Assert.Equal("/join/continue",(await browser.GetAsync(mail.Link("failed-"+email))).Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Fresh_navigation_without_pending_cookie_resumes_and_name_collision_is_owned_recovery()
    {
        var email="fresh-"+Guid.NewGuid()+"@example.test";
        var mail=new OnboardingMail();using var api=OnboardingMail.Api(fixture,mail);using var client=api.CreateClient();
        (await client.PostAsJsonAsync("/api/v1/auth/registration-link",new{email,displayName="Integration Explorer"})).EnsureSuccessStatusCode();
        using var web=new PasskeyWebFactory(api);using var freshBrowser=Browser(web);
        Assert.Equal(HttpStatusCode.NotFound,(await freshBrowser.GetAsync("/join/data/pending")).StatusCode);
        Assert.Equal("/join/continue",(await freshBrowser.GetAsync(mail.Link(email))).Headers.Location?.OriginalString);
        var state=await freshBrowser.GetFromJsonAsync<JsonElement>("/join/data/state");Assert.Equal(JsonValueKind.Null,state.GetProperty("displayName").ValueKind);
        var csrf=await Csrf(freshBrowser,"/join/continue");
        Assert.Equal(HttpStatusCode.BadRequest,(await Post(freshBrowser,"/join/data/complete",new{skip=true},csrf)).StatusCode);
        var name="Unique fresh "+Guid.NewGuid();
        (await Post(freshBrowser,"/backpack/data/name",new{displayName=name},csrf)).EnsureSuccessStatusCode();
        (await Post(freshBrowser,"/join/data/complete",new{skip=true},csrf)).EnsureSuccessStatusCode();
        Assert.Equal(name,(await freshBrowser.GetFromJsonAsync<BackpackAccountInfo>("/backpack/data/account"))!.DisplayName);
    }

    [Fact]
    public async Task Blocked_accounts_and_long_names_do_not_create_or_mutate_profiles()
    {
        var user=new User("blocked-onboarding-"+Guid.NewGuid());var email=user.Subject+"@example.test";
        await using(var db=fixture.CreateDbContext()){db.AddRange(user,new UserEmail(user.Id,email));db.Entry(user).Property(u=>u.Status).CurrentValue=UserAccountStatus.Suspended;await db.SaveChangesAsync();}
        var mail=new OnboardingMail();using var api=OnboardingMail.Api(fixture,mail);using var client=api.CreateClient();
        (await client.PostAsJsonAsync("/api/v1/auth/registration-link",new{email,displayName="Blocked"})).EnsureSuccessStatusCode();Assert.Empty(mail.Messages);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/v1/auth/registration-link",new{email="long-"+email,displayName=new string('x',101)})).StatusCode);
        await using(var db=fixture.CreateDbContext()){Assert.False(await db.ExplorerProfiles.AnyAsync(p=>p.UserId==user.Id));Assert.False(db.Database.HasPendingModelChanges());}
    }

    private static string Hash(string token)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    internal static HttpClient Browser(PasskeyWebFactory web)=>web.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost:7167"),AllowAutoRedirect=false});
    internal static async Task<string> Csrf(HttpClient browser,string page){var html=await browser.GetStringAsync(page);var match=Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");Assert.True(match.Success);return WebUtility.HtmlDecode(match.Groups[1].Value);}
    internal static Task<HttpResponseMessage> Post(HttpClient client,string path,object body,string csrf){var request=new HttpRequestMessage(HttpMethod.Post,path){Content=JsonContent.Create(body)};request.Headers.Add("X-CSRF-TOKEN",csrf);return client.SendAsync(request);}
}

internal sealed class OnboardingMail : HttpMessageHandler
{
    public ConcurrentQueue<(string Email,string Html)> Messages {get;}=new();
    public bool Fail {get;set;}
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
    {
        if(Fail)return new(HttpStatusCode.ServiceUnavailable){Content=JsonContent.Create(new{message="test delivery failure"})};
        var message=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct)).RootElement;
        Messages.Enqueue((message.GetProperty("to")[0].GetString()!,message.GetProperty("html").GetString()!));
        return new(HttpStatusCode.OK){Content=JsonContent.Create(new{id=Guid.NewGuid()})};
    }
    public string Token(string email)=>Regex.Match(WebUtility.HtmlDecode(Messages.Last(m=>m.Email==email).Html),"token=([^\"&]+)").Groups[1].Value;
    public string Link(string email)=>"/signin/magic-link?token="+Token(email)+(Messages.Last(m=>m.Email==email).Html.Contains("flow=explorer")?"&flow=explorer":"");
    public static WebApplicationFactory<Program> Api(PostgreSqlFixture fixture,OnboardingMail mail)=>new ObservationApiFactory(fixture).WithWebHostBuilder(builder=>builder.ConfigureTestServices(services=>{
        services.Configure<ResendClientOptions>(options=>{options.ApiToken="isolated-test-token";options.ApiUrl="https://email.test";});
        services.RemoveAll<IResend>();services.AddScoped<IResend>(sp=>new ResendClient(sp.GetRequiredService<IOptionsSnapshot<ResendClientOptions>>(),new HttpClient(mail,false)));
    }));
}
