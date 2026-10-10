extern alias DiaperScoutWeb;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DiaperScout.Application;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Forwarder = DiaperScoutWeb::DiaperScout.Web.Services.ProductionIdentityForwardingHandler;
using Places = DiaperScoutWeb::DiaperScout.Web.Services.PlaceObservationClient;

namespace DiaperScout.Api.IntegrationTests;

public sealed class ContributionIdentityTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, true, false)]
    public async Task Authenticated_contribution_forwards_current_user_from_request_or_circuit(bool httpRequest, bool staleAmbientUser, bool authenticatedCircuit = true)
    {
        const string secret = "test-only-production-assertion-key";
        var id = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier,id.ToString()), new Claim("sub","test-contributor") }, "PasskeyCookie"));
        var capture = new VerifyIdentityHandler(secret, id);
        var services = new ServiceCollection();
        services.AddLogging(); services.AddHttpContextAccessor();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["Authentication:Production:InternalSecret"] = secret }).Build());
        services.AddScoped<AuthenticationStateProvider,ServerAuthenticationStateProvider>();
        services.AddTransient<Forwarder>();
        services.AddHttpClient<Places>(client => client.BaseAddress = new Uri("https://api.test"))
            .AddHttpMessageHandler<Forwarder>().ConfigurePrimaryHttpMessageHandler(() => capture);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes=true });
        using var scope = provider.CreateScope();
        var state = (ServerAuthenticationStateProvider)scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>();
        state.SetAuthenticationState(Task.FromResult(new AuthenticationState(authenticatedCircuit ? user : new ClaimsPrincipal(new ClaimsIdentity()))));
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = httpRequest ? new DefaultHttpContext { User=staleAmbientUser ? new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,Guid.NewGuid().ToString())},"OldRequestCookie")) : user } : null;
        try {
            var client=scope.ServiceProvider.GetRequiredService<Places>();
            var proposal=new PublicProductProposal("96385074","Test brand","Test proposal",null,null,"S",12,Guid.NewGuid());
            if (!authenticatedCircuit) {
                var failure=await Assert.ThrowsAsync<DiaperScoutWeb::DiaperScout.Web.Services.ContributionException>(()=>client.ProposeAsync(proposal));
                Assert.True(failure.RequiresAuthentication);Assert.False(capture.Verified);return;
            }
            var receipt = await client.ProposeAsync(proposal);
            Assert.NotEqual(Guid.Empty,receipt.SubmissionId);
            Assert.True(capture.Verified);
        } finally { accessor.HttpContext=null; }
    }
    private sealed class VerifyIdentityHandler(string secret,Guid expectedUser) : HttpMessageHandler
    {
        public bool Verified { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            if (request.Headers.TryGetValues("X-DiaperScout-Identity",out var headers)) {
                var parts=headers.Single().Split('.');
                byte[] Decode(string s)=>Convert.FromBase64String(s.Replace('-','+').Replace('_','/').PadRight((s.Length+3)/4*4,'='));
                var payload=Decode(parts[0]);
                Verified=CryptographicOperations.FixedTimeEquals(Decode(parts[1]),HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret),payload))
                    && Encoding.UTF8.GetString(payload).Split('|')[0]==expectedUser.ToString();
            }
            return Task.FromResult(Verified
                ? new HttpResponseMessage(HttpStatusCode.OK) {Content=JsonContent.Create(new { SubmissionId=Guid.NewGuid() })}
                : new HttpResponseMessage(HttpStatusCode.Unauthorized));
        }
    }
}
