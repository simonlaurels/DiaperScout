using System.Net;
using System.Net.Http.Json;
using System.Text;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class AwinAffiliateProgrammeDiscoveryProviderTests
{
    [Fact]
    public async Task DiscoverAsync_ReturnsMatchingProgrammeAndMembershipState()
    {
        var handler = new RecordingHandler(request =>
        {
            var query = request.RequestUri!.Query;

            if (query.Contains("advertiserId=12345", StringComparison.Ordinal))
            {
                return JsonResponse("""
                {
                  "programmeInfo": {
                    "id": 12345,
                    "name": "Example Retailer",
                    "displayUrl": "https://retailer.example.test",
                    "membershipStatus": "Notjoined",
                    "deeplinkEnabled": true
                  }
                }
                """);
            }

            if (query.Contains("relationship=notjoined", StringComparison.Ordinal))
            {
                return JsonResponse("""
                [
                  {
                    "id": 12345,
                    "name": "Example Retailer",
                    "displayUrl": "https://retailer.example.test",
                    "validDomains": [{ "domain": "retailer.example.test" }]
                  }
                ]
                """);
            }

            return JsonResponse("[]");
        });

        using var httpClient = new HttpClient(handler);
        var provider = new AwinAffiliateProgrammeDiscoveryProvider(
            httpClient,
            Options.Create(new AwinAffiliateProgrammeDiscoveryOptions
            {
                Enabled = true,
                BaseUrl = "https://api.awin.test/",
                PublisherId = "999",
                AccessToken = "test-token",
                CountryCodes = "GB"
            }));

        var result = await provider.DiscoverAsync(
            new RetailerAffiliateProgrammeDiscoveryTarget(
                Guid.NewGuid(),
                "Example Retailer",
                "https://retailer.example.test"));

        var programme = Assert.Single(result);
        Assert.Equal("Awin", programme.Network);
        Assert.Equal("12345", programme.ProgrammeId);
        Assert.Equal("Example Retailer", programme.ProgrammeName);
        Assert.Equal(AffiliateProgrammeStatus.ApplicationRequired, programme.Status);
        Assert.True(programme.ApplicationRequired);
        Assert.True(programme.DeepLinksAllowed);
        Assert.Equal("https://retailer.example.test", programme.ProgrammeUrl);
        Assert.Equal("Bearer", handler.LastAuthorizationScheme);
        Assert.Equal("test-token", handler.LastAuthorizationParameter);
    }

    [Fact]
    public async Task DiscoverAsync_ReturnsEmptyWhenProviderIsDisabled()
    {
        var handler = new RecordingHandler(_ => JsonResponse("[]"));
        using var httpClient = new HttpClient(handler);

        var provider = new AwinAffiliateProgrammeDiscoveryProvider(
            httpClient,
            Options.Create(new AwinAffiliateProgrammeDiscoveryOptions
            {
                Enabled = false
            }));

        var result = await provider.DiscoverAsync(
            new RetailerAffiliateProgrammeDiscoveryTarget(
                Guid.NewGuid(),
                "Example Retailer",
                "https://retailer.example.test"));

        Assert.Empty(result);
        Assert.Equal(0, handler.RequestCount);
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public string? LastAuthorizationScheme { get; private set; }
        public string? LastAuthorizationParameter { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            LastAuthorizationScheme = request.Headers.Authorization?.Scheme;
            LastAuthorizationParameter = request.Headers.Authorization?.Parameter;
            return Task.FromResult(responder(request));
        }
    }
}
