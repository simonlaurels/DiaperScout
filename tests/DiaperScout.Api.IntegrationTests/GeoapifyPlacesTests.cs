using System.Net;
using System.Text;
using System.Text.Json;
using DiaperScout.Application;
using DiaperScout.Infrastructure;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace DiaperScout.Api.IntegrationTests;
public sealed class GeoapifyPlacesTests
{
    public static string Feature(int id = 1, string name = "Nearby pharmacy", decimal lat = 51.9m) => JsonSerializer.Serialize(new {type="Feature", properties=new {
        name, place_id="provider-"+id, country_code="zz", lat, lon=-2.1m, street="Public Street", housenumber="1", city="Testville", postcode="ZZ1 2ZZ",
        categories=new[]{"healthcare.pharmacy"}, datasource=new {sourcename="openstreetmap", license="Open Database License", raw=new {osm_type="n",osm_id=id}}
    }});
    public sealed class Handler(Func<HttpRequestMessage,string> response) : HttpMessageHandler {
        public System.Collections.Concurrent.ConcurrentBag<HttpRequestMessage> Requests {get;}=[];
        public System.Collections.Concurrent.ConcurrentBag<string> Bodies {get;}=[];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct) {
            Requests.Add(request); Bodies.Add(await request.Content!.ReadAsStringAsync(ct));
            return new(HttpStatusCode.OK){Content=new StringContent(response(request),Encoding.UTF8,"application/json")};
        }
    }
    private static IConfiguration Configuration(string key="synthetic-test-key") => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Geoapify:ApiKey"]=key}).Build();
    [Fact] public async Task Individual_queries_merge_by_source_identity_without_merging_neighbouring_businesses() {
        var handler=new Handler(_=>"{\"features\":["+Feature()+","+Feature(2)+","+Feature()+"]}");
        var provider=new GeoapifyPlaces(new HttpClient(handler),Configuration());
        var found=await provider.NearbyAsync(new(51.9m,-2.1m),default);
        Assert.Equal(2,found.Count);Assert.Equal(5,handler.Requests.Count);
        Assert.All(handler.Requests,r=>{Assert.Equal(HttpMethod.Post,r.Method);Assert.Equal("",r.RequestUri!.Query);Assert.True(r.Headers.Contains("x-api-key"));});
        var categories=handler.Bodies.Select(s=>JsonDocument.Parse(s).RootElement.GetProperty("categories")[0].GetString()).ToArray();
        Assert.Contains("commercial.supermarket",categories);Assert.Contains("commercial.health_and_beauty.medical_supply",categories);
        Assert.Equal("n:1",provider.Verify(found[0].Place.SelectionToken!).SourceIdentity);
        Assert.DoesNotContain("synthetic-test-key",JsonSerializer.Serialize(found));
    }
    [Fact] public async Task Signed_selection_cannot_be_forged_or_used_after_key_rotation() {
        var provider=new GeoapifyPlaces(new HttpClient(new Handler(_=>"{\"features\":["+Feature()+"]}")),Configuration());
        var token=(await provider.NearbyAsync(new(51.9m,-2.1m),default))[0].Place.SelectionToken!;
        Assert.Throws<CatalogueValidationException>(()=>provider.Verify(token+"tampered"));
        Assert.Throws<CatalogueValidationException>(()=>new GeoapifyPlaces(new HttpClient(),Configuration("rotated")).Verify(token));
    }
    [Fact] public void Incomplete_or_unlicensed_records_are_not_selectable() {
        using var malformed=JsonDocument.Parse("{\"properties\":{}}");Assert.Null(GeoapifyPlaces.Map(malformed.RootElement));
        using var unsupported=JsonDocument.Parse(Feature().Replace("openstreetmap","other-provider"));Assert.Null(GeoapifyPlaces.Map(unsupported.RootElement));
    }
    [Fact] public async Task Empty_results_do_not_create_place_candidates() {
        var provider=new GeoapifyPlaces(new HttpClient(new Handler(_=>"{\"features\":[]}")),Configuration());
        Assert.Empty(await provider.NearbyAsync(new(51.9m,-2.1m),default));
        await Assert.ThrowsAsync<CatalogueValidationException>(()=>provider.NearbyAsync(new(91,0),default));
    }
    [Fact] public async Task Invalid_provider_response_fails_with_safe_message() {
        var provider=new GeoapifyPlaces(new HttpClient(new Handler(_=>"not-json")),Configuration());
        var e=await Assert.ThrowsAsync<CatalogueValidationException>(()=>provider.NearbyAsync(new(51.9m,-2.1m),default));
        Assert.Contains("temporarily unavailable",e.Message);Assert.DoesNotContain("synthetic-test-key",e.Message);Assert.DoesNotContain("51.9",e.Message);
    }
    private sealed class UnavailableHandler : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct) => throw new HttpRequestException("provider unavailable");
    }
    [Fact] public async Task Provider_failure_is_safe_and_expired_selection_requires_reselection() {
        var unavailable=new GeoapifyPlaces(new HttpClient(new UnavailableHandler()),Configuration());
        var failure=await Assert.ThrowsAsync<CatalogueValidationException>(()=>unavailable.NearbyAsync(new(51.9m,-2.1m),default));
        Assert.Contains("temporarily unavailable",failure.Message);Assert.Null(failure.InnerException);
        using var feature=JsonDocument.Parse(Feature());
        var payload=Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new {Place=GeoapifyPlaces.Map(feature.RootElement),Expires=DateTimeOffset.UtcNow.AddMinutes(-1)}));
        var key=System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("DiaperScout.PlaceSelection.v1|synthetic-test-key"));
        var token=payload+"."+Convert.ToBase64String(System.Security.Cryptography.HMACSHA256.HashData(key,Encoding.UTF8.GetBytes(payload)));
        Assert.Throws<CatalogueValidationException>(()=>unavailable.Verify(token));
    }
    private sealed class StalledHandler : HttpMessageHandler {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct) {await Task.Delay(Timeout.InfiniteTimeSpan,ct);return new(HttpStatusCode.OK);}
    }
    [Fact] public async Task Stalled_provider_is_cancelled_without_exposing_request_data() {
        var provider=new GeoapifyPlaces(new HttpClient(new StalledHandler()),Configuration());
        using var cancellation=new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var error=await Assert.ThrowsAsync<CatalogueValidationException>(()=>provider.NearbyAsync(new(51.9m,-2.1m),cancellation.Token));
        Assert.Contains("temporarily unavailable",error.Message);Assert.Null(error.InnerException);
    }
}
