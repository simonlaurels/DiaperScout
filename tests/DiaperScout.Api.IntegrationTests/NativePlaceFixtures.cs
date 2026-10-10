using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DiaperScout.Application;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace DiaperScout.Api.IntegrationTests;
// Native historical places are seeded through the domain service, never an Explorer HTTP creation route.
internal static class NativePlaceFixtures
{
    public static async Task<HttpResponseMessage> CreateAsync(ObservationApiFactory api, object request)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();
        var user = await db.Users.SingleAsync(u => u.Subject == PostgreSqlFixture.ExplorerSubject);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var shop = JsonSerializer.Deserialize<CreatePublicShopRequest>(JsonSerializer.Serialize(request, options), options)!;
        try {
            var place = await scope.ServiceProvider.GetRequiredService<IPlaceObservations>().CreateShopAsync(new(user.Id, Guid.Empty, user.Subject, "Test Explorer"), shop);
            return new(HttpStatusCode.OK) {Content = JsonContent.Create(place)};
        } catch(CatalogueValidationException e) {
            return new(HttpStatusCode.BadRequest) {Content = JsonContent.Create(new { errors = new Dictionary<string,string[]> {[e.Field] = [e.Message]} })};
        }
    }
}
