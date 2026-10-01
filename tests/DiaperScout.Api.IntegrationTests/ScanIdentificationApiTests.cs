using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace DiaperScout.Api.IntegrationTests;
public sealed class ScanIdentificationApiTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Anonymous_GTIN_lookup_normalises_formats_and_rejects_ambiguous_canonical_mapping()
    {
        using var api=new ObservationApiFactory(fixture);using var client=api.CreateClient();
        using var scope=api.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();
        var pack=await(from p in db.PackTypes join s in db.SizeVariants on p.SizeVariantId equals s.Id join v in db.ProductVariants on s.ProductVariantId equals v.Id where v.ProductId==fixture.ProductId select p).SingleAsync();
        db.ProductIdentifiers.Add(new ProductIdentifier(pack.Id,IdentifierType.Gtin,"036000291452"));await db.SaveChangesAsync();
        var first=await client.GetFromJsonAsync<ProductIdentification>("/api/v1/products/lookup/00036000291452");Assert.Equal(pack.Id,first!.PackTypeId);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/v1/products/lookup/12345678")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync("/api/v1/products/lookup/96385074")).StatusCode);
        var other=new PackType(pack.SizeVariantId,24,PackagingType.Box);db.PackTypes.Add(other);db.ProductIdentifiers.Add(new ProductIdentifier(other.Id,IdentifierType.Gtin,"00036000291452"));await db.SaveChangesAsync();
        var response=await client.GetAsync("/api/v1/products/lookup/036000291452");Assert.Equal(HttpStatusCode.Conflict,response.StatusCode);
        var body=await response.Content.ReadAsStringAsync();Assert.Contains("ambiguous_barcode",body);Assert.DoesNotContain(pack.Id.ToString(),body);Assert.DoesNotContain(other.Id.ToString(),body);
    }
}
