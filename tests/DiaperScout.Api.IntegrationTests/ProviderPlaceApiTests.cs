using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Infrastructure.Persistence;
using DiaperScout.Infrastructure;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace DiaperScout.Api.IntegrationTests;
public sealed class ProviderPlaceApiTests(PostgreSqlFixture fixture):IClassFixture<PostgreSqlFixture>
{
    [Fact] public async Task Search_select_retry_observe_exports_only_public_place_snapshot() {
        using var api=new ObservationApiFactory(fixture);using var actor=api.CreateClient();actor.DefaultRequestHeaders.Add("X-Development-Subject",PostgreSqlFixture.ExplorerSubject);
        using var scope=api.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();
        var before=await db.Locations.CountAsync();
        var search=await actor.PostAsJsonAsync("/api/v1/places/nearby",new NearbyPlaceRequest(51.9001m,-2.1001m));search.EnsureSuccessStatusCode();
        var candidate=Assert.Single((await search.Content.ReadFromJsonAsync<NearbyPlace[]>())!);
        Assert.Equal(before,await db.Locations.CountAsync());
        var select=new SelectProviderPlaceRequest(candidate.Place.SelectionToken!);
        var first=await actor.PostAsJsonAsync("/api/v1/places/select",select);first.EnsureSuccessStatusCode();var place=(await first.Content.ReadFromJsonAsync<PlaceItem>())!;
        var retry=await actor.PostAsJsonAsync("/api/v1/places/select",select);retry.EnsureSuccessStatusCode();Assert.Equal(place,(await retry.Content.ReadFromJsonAsync<PlaceItem>()));
        var row=await db.Locations.SingleAsync(l=>l.Id==place.Id);Assert.NotNull(row.ProviderSnapshotJson);Assert.Equal(51.9m,row.Latitude);Assert.Equal(-2.1m,row.Longitude);
        Assert.Contains("n:",row.ProviderSnapshotJson!);Assert.DoesNotContain("51.9001",row.ProviderSnapshotJson!);
        Assert.Throws<InvalidOperationException>(()=>row.RecordProviderSnapshot("{}"));
        var historical=JsonSerializer.Deserialize<ProviderPlaceSnapshot>(row.ProviderSnapshotJson!)!;
        using var publicReader=api.CreateClient();
        Assert.DoesNotContain(historical.ExternalId,await publicReader.GetStringAsync("/api/v1/places/open-data"));
        var moved=historical with {Latitude=52.0m,AddressLine1="2 New Street"};
        var payload=Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new {Place=moved,Expires=DateTimeOffset.UtcNow.AddMinutes(5)}));
        var key=SHA256.HashData(Encoding.UTF8.GetBytes("DiaperScout.PlaceSelection.v1|synthetic-test-key"));
        var movedToken=payload+"."+Convert.ToBase64String(HMACSHA256.HashData(key,Encoding.UTF8.GetBytes(payload)));
        var movedResponse=await actor.PostAsJsonAsync("/api/v1/places/select",new SelectProviderPlaceRequest(movedToken));movedResponse.EnsureSuccessStatusCode();
        var movedPlace=(await movedResponse.Content.ReadFromJsonAsync<PlaceItem>())!;
        Assert.NotEqual(place.Id,movedPlace.Id);Assert.Equal(51.9m,(await db.Locations.AsNoTracking().SingleAsync(l=>l.Id==place.Id)).Latitude);

        var contribution=Guid.NewGuid();var request=new CreatePhysicalObservationRequest(fixture.PackTypeId,place.Id,DateTimeOffset.UtcNow.AddMinutes(-1),12.50m,"GBP",contribution);
        var observation=await actor.PostAsJsonAsync("/api/v1/physical-observations",request);observation.EnsureSuccessStatusCode();
        var receipt=(await observation.Content.ReadFromJsonAsync<PhysicalObservationReceipt>())!;
        (await actor.PostAsJsonAsync("/api/v1/physical-observations",request)).EnsureSuccessStatusCode();
        Assert.Equal(1,await db.Observations.CountAsync(o=>o.ContributionId==contribution));
        using var anonymous=api.CreateClient();var atlas=(await anonymous.GetFromJsonAsync<AtlasPlace[]>("/api/v1/places/atlas"))!;
        Assert.Contains(atlas,p=>p.Place.Id==place.Id && p.Observations.Any(o=>o.ObservationId==receipt.Id));
        var export=await anonymous.GetStringAsync("/api/v1/places/open-data");Assert.Contains("ODbL-1.0",export);Assert.DoesNotContain("AuthorUserId",export);Assert.DoesNotContain("PriceAmount",export);Assert.DoesNotContain("PackTypeId",export);
        var bad=await actor.PostAsJsonAsync("/api/v1/places/select",new SelectProviderPlaceRequest(candidate.Place.SelectionToken+"bad"));Assert.Equal(HttpStatusCode.BadRequest,bad.StatusCode);
    }
    [Fact] public async Task Explorer_cannot_create_manual_business_and_admin_management_is_preserved() {
        using var api=new ObservationApiFactory(fixture);using var actor=api.CreateClient();actor.DefaultRequestHeaders.Add("X-Development-Subject",PostgreSqlFixture.ExplorerSubject);
        var shop=new CreatePublicShopRequest("Admin maintained "+Guid.NewGuid(),"1 Public Street","Testville","ZZ1 2ZZ","ZZ",51.9m,-2.1m,true,true);
        Assert.Equal(HttpStatusCode.MethodNotAllowed,(await actor.PostAsJsonAsync("/api/v1/places/",shop)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await actor.PostAsJsonAsync("/api/v1/admin/places",shop)).StatusCode);
        using var admin=api.CreateClient();admin.DefaultRequestHeaders.Add("X-Development-Subject",PostgreSqlFixture.AdministratorSubject);
        (await admin.PostAsJsonAsync("/api/v1/admin/places",shop)).EnsureSuccessStatusCode();
        using var anonymous=api.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.PostAsJsonAsync("/api/v1/places/select",new SelectProviderPlaceRequest("invalid"))).StatusCode);
    }
}
