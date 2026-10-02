using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class PlaceObservationApiTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Active_authenticated_account_without_optional_explorer_profile_can_propose_exactly_once()
    {
        using var api=Factory();
        var subject="profileless-proposal-"+Guid.NewGuid().ToString("N");
        var user=new User(subject);
        using (var scope=api.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();
            db.Users.Add(user); await db.SaveChangesAsync();
            Assert.False(await db.ExplorerProfiles.AnyAsync(p=>p.UserId==user.Id));
        }
        using var client=Actor(api,subject);
        var request=new PublicProductProposal("96385074","Test Brand","Profileless proposal",null,"Test maker","S",12,Guid.NewGuid());
        var first=await client.PostAsJsonAsync("/api/v1/public-product-proposals",request);
        Assert.Equal(HttpStatusCode.OK,first.StatusCode);
        var receipt=(await first.Content.ReadFromJsonAsync<PublicProductProposalReceipt>())!;
        var retry=await client.PostAsJsonAsync("/api/v1/public-product-proposals",request);
        Assert.Equal(receipt,(await retry.Content.ReadFromJsonAsync<PublicProductProposalReceipt>()));
        using var verification=api.Services.CreateScope();
        var database=verification.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();
        Assert.Equal(1,await database.CatalogueSubmissions.CountAsync(s=>s.SubmittedByUserId==user.Id&&s.PublicContributionId==request.ContributionId));
        Assert.False(await database.ExplorerProfiles.AnyAsync(p=>p.UserId==user.Id));
    }

    [Theory]
    [InlineData(UserAccountStatus.Suspended)]
    [InlineData(UserAccountStatus.Anonymised)]
    public async Task Authenticated_nonactive_account_without_profile_is_still_forbidden(UserAccountStatus status) {
        using var api=Factory();var subject="blocked-proposal-"+Guid.NewGuid().ToString("N");var user=new User(subject);
        using(var scope=api.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();db.Users.Add(user);
            db.Entry(user).Property(u=>u.Status).CurrentValue=status;await db.SaveChangesAsync();
        }
        using var client=Actor(api,subject);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/v1/public-product-proposals",Proposal())).StatusCode);
        using var verification=api.Services.CreateScope();
        Assert.False(await verification.ServiceProvider.GetRequiredService<DiaperScoutDbContext>().CatalogueSubmissions.AnyAsync(s=>s.SubmittedByUserId==user.Id));
    }
    private ObservationApiFactory Factory() => new(fixture);
    private static HttpClient Actor(ObservationApiFactory api, string subject = PostgreSqlFixture.ExplorerSubject) {
        var client = api.CreateClient(); client.DefaultRequestHeaders.Add("X-Development-Subject", subject); return client;
    }
    private static CreatePublicShopRequest Shop(string name = "Test public shop") => new(name, "1 Public Street", "Testville", "ZZ1 2ZZ", "ZZ", 51.9m, -2.1m, true, true);
    private async Task<Guid> Pack(ObservationApiFactory api) {
        using var scope = api.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();
        return await (from p in db.PackTypes join s in db.SizeVariants on p.SizeVariantId equals s.Id join v in db.ProductVariants on s.ProductVariantId equals v.Id where v.ProductId == fixture.ProductId select p.Id).SingleAsync();
    }
    [Fact]
    public async Task Anonymous_discovery_works_and_all_contribution_writes_are_blocked() {
        using var api = Factory(); using var anonymous = api.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/api/v1/places/atlas")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/api/v1/places/countries")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/api/v1/places/packs/" + await Pack(api))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/places/", Shop())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/physical-observations", new CreatePhysicalObservationRequest(await Pack(api), fixture.LocationId, DateTimeOffset.UtcNow, null, null, Guid.NewGuid()))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/public-product-proposals", Proposal())).StatusCode);
    }
    [Fact]
    public async Task Exact_pack_shop_price_provenance_and_Atlas_persist_without_catalogue_or_commerce_mutation() {
        using var api = Factory(); using var actor = Actor(api);
        var pack = await Pack(api);
        using var scope = api.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();
        var before = await db.Products.SingleAsync(p => p.Id == fixture.ProductId); var name = before.Name;
        var listings = await db.RetailerProductListings.CountAsync();
        var placeResponse = await actor.PostAsJsonAsync("/api/v1/places/", Shop("Evidence shop")); placeResponse.EnsureSuccessStatusCode();
        var shop = (await placeResponse.Content.ReadFromJsonAsync<PlaceItem>())!;
        var when = DateTimeOffset.UtcNow.AddMinutes(-10).AddTicks(1);
        var request = new CreatePhysicalObservationRequest(pack, shop.Id, when, 17.25m, "GBP", Guid.NewGuid());
        var response = await actor.PostAsJsonAsync("/api/v1/physical-observations", request); response.EnsureSuccessStatusCode();
        var receipt = (await response.Content.ReadFromJsonAsync<PhysicalObservationReceipt>())!;
        var repeated = await actor.PostAsJsonAsync("/api/v1/physical-observations", request); repeated.EnsureSuccessStatusCode();
        Assert.Equal(receipt.Id, (await repeated.Content.ReadFromJsonAsync<PhysicalObservationReceipt>())!.Id);
        db.ChangeTracker.Clear();
        var record = await db.Observations.SingleAsync(o => o.Id == receipt.Id);
        Assert.Equal(fixture.ExplorerUserId, record.AuthorUserId); Assert.Equal(pack, record.PackTypeId); Assert.Equal(shop.Id, record.LocationId);
        Assert.Equal(fixture.ProductId, record.ProductId); Assert.Equal(17.25m, record.PriceAmount); Assert.Equal("GBP", record.PriceCurrencyCode);
        Assert.True((record.ObservedAtUtc - when).Duration() < TimeSpan.FromMilliseconds(1)); Assert.Equal(ObservationState.Submitted, record.State); Assert.Null(record.Narrative);
        var persistedShop = await db.Locations.SingleAsync(l => l.Id == shop.Id); Assert.Null(persistedShop.RetailerId); Assert.True(persistedShop.IsPublicCommercialPlace); Assert.Equal(fixture.ExplorerUserId, persistedShop.CreatedByUserId);
        Assert.Equal(listings, await db.RetailerProductListings.CountAsync()); Assert.Equal(name, (await db.Products.SingleAsync(p => p.Id == fixture.ProductId)).Name);
        var atlas = await actor.GetFromJsonAsync<AtlasPlace[]>("/api/v1/places/atlas");
        var entry = Assert.Single(atlas!, p => p.Place.Id == shop.Id); Assert.Equal(1, entry.ProductCount);
        var observation = Assert.Single(entry.Observations); Assert.Equal(receipt.Id, observation.ObservationId); Assert.Contains("packTypeId=" + pack, observation.ProductUrl);
        Assert.DoesNotContain(atlas!, p => p.Place.Id == fixture.LocationId);
        var publicJson = await actor.GetStringAsync("/api/v1/places/atlas"); Assert.DoesNotContain("authorUserId", publicJson); Assert.DoesNotContain("submittedByUserId", publicJson);
    }
    [Fact]
    public async Task Coordinates_public_confirmation_duplicate_shop_and_observation_validation_are_enforced() {
        using var api = Factory(); using var actor = Actor(api);
        foreach (var invalid in new[] {Shop() with {Latitude = 91}, Shop() with {Longitude = -181}, Shop() with {Latitude = null}, Shop() with {ConfirmPublicShop = false}, Shop() with {ConfirmShopPosition = false}, Shop() with {CountryCode = "XX"}})
            Assert.Equal(HttpStatusCode.BadRequest, (await actor.PostAsJsonAsync("/api/v1/places/", invalid)).StatusCode);
        var one = await actor.PostAsJsonAsync("/api/v1/places/", Shop("Duplicate Shop"));one.EnsureSuccessStatusCode();var shop = (await one.Content.ReadFromJsonAsync<PlaceItem>())!;
        var duplicate = await actor.PostAsJsonAsync("/api/v1/places/", Shop(" duplicate   shop "));duplicate.EnsureSuccessStatusCode();Assert.Equal(shop.Id,(await duplicate.Content.ReadFromJsonAsync<PlaceItem>())!.Id);
        var valid = new CreatePhysicalObservationRequest(await Pack(api),shop.Id,DateTimeOffset.UtcNow,null,null,Guid.NewGuid());
        foreach(var invalid in new[] {valid with {PackTypeId=Guid.NewGuid()},valid with {LocationId=fixture.LocationId},valid with {ObservedAtUtc=DateTimeOffset.UtcNow.AddDays(1)},valid with {PriceAmount=-1,CurrencyCode="GBP"},valid with {PriceAmount=1,CurrencyCode="ZZZ"},valid with {PriceAmount=1.234m,CurrencyCode="GBP"},valid with {ContributionId=Guid.Empty}})
            Assert.Equal(HttpStatusCode.BadRequest,(await actor.PostAsJsonAsync("/api/v1/physical-observations",invalid)).StatusCode);
        var json = new {valid.PackTypeId,valid.LocationId,valid.ObservedAtUtc,valid.ContributionId,authorUserId=fixture.ModeratorUserId};
        var response=await actor.PostAsJsonAsync("/api/v1/physical-observations",json);response.EnsureSuccessStatusCode();var receipt=(await response.Content.ReadFromJsonAsync<PhysicalObservationReceipt>())!;
        using var scope=api.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();
        Assert.Equal(fixture.ExplorerUserId,(await db.Observations.SingleAsync(o=>o.Id==receipt.Id)).AuthorUserId);
        var conflict=await actor.PostAsJsonAsync("/api/v1/physical-observations",valid with {PriceAmount=2,CurrencyCode="GBP"});Assert.Equal(HttpStatusCode.BadRequest,conflict.StatusCode);
    }
    [Fact]
    public async Task Unknown_barcode_enters_existing_queue_and_can_be_moderated_to_existing_pack_without_new_product() {
        using var api=Factory();using var actor=Actor(api);using var moderator=Actor(api,PostgreSqlFixture.ModeratorSubject);
        using var scope=api.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();
        var productCount=await db.Products.CountAsync();var proposal=Proposal();
        var response=await actor.PostAsJsonAsync("/api/v1/public-product-proposals",proposal);response.EnsureSuccessStatusCode();var receipt=(await response.Content.ReadFromJsonAsync<PublicProductProposalReceipt>())!;
        var repeated=await actor.PostAsJsonAsync("/api/v1/public-product-proposals",proposal);repeated.EnsureSuccessStatusCode();Assert.Equal(receipt.SubmissionId,(await repeated.Content.ReadFromJsonAsync<PublicProductProposalReceipt>())!.SubmissionId);
        Assert.Equal(productCount,await db.Products.CountAsync());
        var submission=await db.CatalogueSubmissions.SingleAsync(s=>s.Id==receipt.SubmissionId);Assert.Equal(CatalogueSubmissionSource.Explorer,submission.Source);Assert.Equal(fixture.ExplorerUserId,submission.SubmittedByUserId);Assert.Equal(CatalogueSubmissionStatus.InVerification,submission.Status);Assert.Equal(proposal.Gtin,submission.ProposedGtin);Assert.Equal(proposal.Quantity,submission.ProposedPackQuantity);
        Assert.Contains(receipt.SubmissionId.ToString(),await moderator.GetStringAsync("/api/v1/catalogue-submissions"));
        var resolution=new ResolveBarcodeProposal(await Pack(api),"Verified packaging identity against the existing exact pack.");
        Assert.Equal(HttpStatusCode.Forbidden,(await actor.PostAsJsonAsync($"/api/v1/catalogue-submissions/{receipt.SubmissionId}/resolve-existing-pack",resolution)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await moderator.PostAsJsonAsync($"/api/v1/catalogue-submissions/{receipt.SubmissionId}/resolve-existing-pack",resolution)).StatusCode);
        // Isolated fixture represents a completed editorial verification/review stage.
        submission.MarkReadyForReview();submission.Approve();await db.SaveChangesAsync();
        var resolved=await moderator.PostAsJsonAsync($"/api/v1/catalogue-submissions/{receipt.SubmissionId}/resolve-existing-pack",resolution);resolved.EnsureSuccessStatusCode();
        db.ChangeTracker.Clear();Assert.Equal(productCount,await db.Products.CountAsync());
        var published=await db.CatalogueSubmissions.SingleAsync(s=>s.Id==receipt.SubmissionId);Assert.Equal(fixture.ProductId,published.PublishedProductId);Assert.Equal(resolution.PackTypeId,published.ResolvedPackTypeId);Assert.Equal(CatalogueSubmissionStatus.Published,published.Status);
        Assert.True(await db.CatalogueAuditRecords.AnyAsync(a=>a.ProductId==fixture.ProductId&&a.CorrelationId==receipt.SubmissionId.ToString()));
        var identified=await actor.GetFromJsonAsync<ProductIdentification>("/api/v1/products/lookup/"+proposal.Gtin);Assert.Equal(resolution.PackTypeId,identified!.PackTypeId);
    }
    private static PublicProductProposal Proposal() => new("4006381333931","Observed Brand","Observed Product",null,null,null,12,Guid.NewGuid());
}
