using Xunit;
using System.Net;
using System.Text;
using DiaperScout.Application;
using DiaperScout.Infrastructure;
using Microsoft.Extensions.Options;

namespace DiaperScout.Api.IntegrationTests;

public sealed class DataForSeoGoogleShoppingDiscoveryProviderTests
{
    [Fact]
    public async Task DiscoverAsync_MapsGoogleShoppingSellersToRetailerCandidates()
    {
        var handler = new FakeDataForSeoHandler();
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.dataforseo.com/")
        };
        var options = Options.Create(new DataForSeoOptions
        {
            Enabled = true,
            Login = "test-login",
            Password = "test-password",
            LocationName = "London,England,United Kingdom",
            LanguageCode = "en",
            SearchDomain = "google.co.uk",
            MaxProductsPerGtin = 1,
            PollAttempts = 2,
            PollDelaySeconds = 1
        });

        var provider = new DataForSeoGoogleShoppingProvider(client, options);
        var candidates = await provider.DiscoverAsync("05012345678903");

        var candidate = Assert.Single(candidates);
        Assert.Equal("05012345678903", candidate.Gtin);
        Assert.Equal("Example Retailer", candidate.RetailerName);
        Assert.Equal("https://example-retailer.test", candidate.RetailerWebsiteUrl);
        Assert.Equal("https://example-retailer.test/product/merries-l", candidate.ListingUrl);
        Assert.Equal("12345", candidate.ExternalListingId);
        Assert.Equal("https://www.google.com/shopping/product/example", candidate.SourceUrl);
    }

    private sealed class FakeDataForSeoHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var response = path switch
            {
                "/v3/merchant/google/products/task_post" => Json("""
                    {"status_code":20000,"tasks":[{"id":"search-task","status_code":20100,"status_message":"Task Created.","result":null}]}
                    """),
                "/v3/merchant/google/products/task_get/advanced/search-task" => Json("""
                    {"status_code":20000,"tasks":[{"id":"search-task","status_code":20000,"status_message":"Ok.","result":[{"items":[{"type":"google_shopping_serp","product_id":"12345"}]}]}]}
                    """),
                "/v3/merchant/google/product_info/task_post" => Json("""
                    {"status_code":20000,"tasks":[{"id":"product-info-task","status_code":20100,"status_message":"Task Created.","result":null}]}
                    """),
                "/v3/merchant/google/product_info/task_get/advanced/product-info-task" => Json("""
                    {"status_code":20000,"tasks":[{"id":"product-info-task","status_code":20000,"status_message":"Ok.","result":[{"check_url":"https://www.google.com/shopping/product/example","items":[{"sellers":[{"title":"Example Retailer","url":"https://example-retailer.test/product/merries-l"}]}]}]}]}
                    """),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };

            return Task.FromResult(response);
        }

        private static HttpResponseMessage Json(string content) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };
    }
}
