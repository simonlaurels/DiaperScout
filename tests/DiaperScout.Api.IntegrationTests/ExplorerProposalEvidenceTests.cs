using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;
public sealed class ExplorerProposalEvidenceTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private static HttpClient Actor(ObservationApiFactory api, string subject = PostgreSqlFixture.ExplorerSubject) { var client = api.CreateClient(); client.DefaultRequestHeaders.Add("X-Development-Subject", subject); return client; }
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aN1sAAAAASUVORK5CYII=");
    private static MultipartFormDataContent Photo(Guid uploadId, CatalogueSubmissionImageRole role = CatalogueSubmissionImageRole.PackFront) {
        var body = new MultipartFormDataContent(); var file = new ByteArrayContent(Png); file.Headers.ContentType = new("image/png"); body.Add(file,"file","pack.png"); body.Add(new StringContent(role.ToString()),"role"); body.Add(new StringContent(uploadId.ToString()),"uploadId"); return body;
    }
    [Fact]
    public async Task Draft_requires_front_photo_and_rejects_foreign_invalid_or_conflicting_evidence()
    {
        using var api = new ObservationApiFactory(fixture); using var actor = Actor(api); using var other = Actor(api, PostgreSqlFixture.ModeratorSubject);
        var key = Guid.NewGuid();
        var begin = await actor.PostAsJsonAsync("/api/v1/public-product-proposals/draft", new { gtin = "96385074", contributionId = key }); begin.EnsureSuccessStatusCode();
        var draft = (await begin.Content.ReadFromJsonAsync<PublicProductProposalReceipt>())!;
        var proposal = new PublicProductProposal("96385074", "Printed brand", "Printed product", null, null, null, null, key);
        Assert.Equal(HttpStatusCode.BadRequest, (await actor.PostAsJsonAsync("/api/v1/public-product-proposals", proposal)).StatusCode);
        using var foreign = Photo(Guid.NewGuid());
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsync($"/api/v1/public-product-proposals/{draft.SubmissionId}/images", foreign)).StatusCode);
        using var invalid = new MultipartFormDataContent(); var text = new ByteArrayContent("Not a PNG"u8.ToArray()); text.Headers.ContentType = new("image/png");
        invalid.Add(text, "file", "fake.png"); invalid.Add(new StringContent("PackFront"), "role"); invalid.Add(new StringContent(Guid.NewGuid().ToString()), "uploadId");
        Assert.Equal(HttpStatusCode.BadRequest, (await actor.PostAsync($"/api/v1/public-product-proposals/{draft.SubmissionId}/images", invalid)).StatusCode);
        var upload = Guid.NewGuid(); using var front = Photo(upload);
        (await actor.PostAsync($"/api/v1/public-product-proposals/{draft.SubmissionId}/images", front)).EnsureSuccessStatusCode();
        using var conflict = Photo(upload, CatalogueSubmissionImageRole.PackBack);
        Assert.Equal(HttpStatusCode.BadRequest, (await actor.PostAsync($"/api/v1/public-product-proposals/{draft.SubmissionId}/images", conflict)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await actor.PostAsJsonAsync("/api/v1/public-product-proposals", proposal with { SuggestedExistingProductId = Guid.NewGuid() })).StatusCode);
        (await actor.PostAsJsonAsync("/api/v1/public-product-proposals", proposal)).EnsureSuccessStatusCode();
        using var late = Photo(Guid.NewGuid());
        Assert.Equal(HttpStatusCode.BadRequest, (await actor.PostAsync($"/api/v1/public-product-proposals/{draft.SubmissionId}/images", late)).StatusCode);
        var invalidDiscovery = new PendingPhysicalDiscovery(Guid.NewGuid(), DateTimeOffset.UtcNow, null, null);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync($"/api/v1/public-product-proposals/{draft.SubmissionId}/discovery", invalidDiscovery)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await actor.PostAsJsonAsync($"/api/v1/public-product-proposals/{draft.SubmissionId}/discovery", invalidDiscovery)).StatusCode);
        using var scope = api.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();
        var stored = await db.CatalogueSubmissions.SingleAsync(s => s.Id == draft.SubmissionId);
        Assert.Null(stored.PendingLocationId); Assert.Null(stored.SuggestedExistingProductId);
        Assert.Single(await db.CatalogueSubmissionImages.Where(i => i.SubmissionId == stored.Id).ToListAsync());
        Assert.False(await db.Observations.AnyAsync(o => o.ContributionId == key));
    }
    [Theory]
    [InlineData("merge")]
    [InlineData("publish")]
    [InlineData("reject")]
    public async Task Evidence_is_private_and_only_resolved_exact_identity_earns_one_observation(string outcome) {
        using var api = new ObservationApiFactory(fixture); using var actor = Actor(api); using var moderator = Actor(api, PostgreSqlFixture.ModeratorSubject); using var anonymous = api.CreateClient();
        var key = Guid.NewGuid(); var gtin = outcome == "publish" ? "96385074" : outcome == "merge" ? "4006381333931" : "5012345678900";
        var begin = await actor.PostAsJsonAsync("/api/v1/public-product-proposals/draft", new {gtin, contributionId=key}); begin.EnsureSuccessStatusCode();
        var draft = (await begin.Content.ReadFromJsonAsync<PublicProductProposalReceipt>())!;
        var uploadId = Guid.NewGuid(); using var imageBody = Photo(uploadId); var upload = await actor.PostAsync($"/api/v1/public-product-proposals/{draft.SubmissionId}/images", imageBody); upload.EnsureSuccessStatusCode();
        var image = (await upload.Content.ReadFromJsonAsync<CatalogueSubmissionImageReceipt>())!;
        using var retryBody = Photo(uploadId); var retryUpload = await actor.PostAsync($"/api/v1/public-product-proposals/{draft.SubmissionId}/images", retryBody); retryUpload.EnsureSuccessStatusCode();
        Assert.Equal(image.Id, (await retryUpload.Content.ReadFromJsonAsync<CatalogueSubmissionImageReceipt>())!.Id);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(image.ContentUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await moderator.GetAsync(image.ContentUrl)).StatusCode);
        var ownImage = await actor.GetAsync(image.ContentUrl); ownImage.EnsureSuccessStatusCode(); Assert.Equal("no-store", ownImage.Headers.CacheControl?.ToString()); Assert.Equal(Png, await ownImage.Content.ReadAsByteArrayAsync());
        var placeResponse = await actor.PostAsJsonAsync("/api/v1/places/", new CreatePublicShopRequest("Pending evidence " + key, "1 Public Street", "Testville", "ZZ1 2ZZ", "ZZ", 51.9m, -2.1m, true, true)); placeResponse.EnsureSuccessStatusCode();
        var place = (await placeResponse.Content.ReadFromJsonAsync<PlaceItem>())!;
        var discovery = new PendingPhysicalDiscovery(place.Id, DateTimeOffset.UtcNow.AddMinutes(-10), 14.99m, "GBP");
        var proposal = new PublicProductProposal(gtin, "Integration Test Brand", "Evidence product " + key, null, "Integration Test Manufacturer", "Medium", 12, key,
            "Explorer packaging notes", ProductType.Tape, fixture.ProductId);
        (await actor.PostAsJsonAsync("/api/v1/public-product-proposals", proposal)).EnsureSuccessStatusCode();
        (await actor.PostAsJsonAsync("/api/v1/public-product-proposals", proposal)).EnsureSuccessStatusCode();
        (await actor.PostAsJsonAsync($"/api/v1/public-product-proposals/{draft.SubmissionId}/discovery", discovery)).EnsureSuccessStatusCode();
        (await actor.PostAsJsonAsync($"/api/v1/public-product-proposals/{draft.SubmissionId}/discovery", discovery)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await actor.PostAsJsonAsync($"/api/v1/public-product-proposals/{draft.SubmissionId}/discovery", discovery with { PriceAmount = 15m })).StatusCode);
        Assert.DoesNotContain((await anonymous.GetFromJsonAsync<AtlasPlace[]>("/api/v1/places/atlas"))!, p => p.Place.Id == place.Id);
        using (var scope = api.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>(); var submission = await db.CatalogueSubmissions.SingleAsync(s => s.Id == draft.SubmissionId);
            Assert.Equal(fixture.ProductId, submission.SuggestedExistingProductId); Assert.Equal(place.Id, submission.PendingLocationId); Assert.Null(submission.ResultingObservationId);
            Assert.Single(await db.CatalogueSubmissionImages.Where(i => i.SubmissionId == submission.Id).ToListAsync());
            if (outcome == "reject") submission.Reject();
            else {
                db.Entry(submission).Property(s => s.ProposedPackagingType).CurrentValue = PackagingType.Bag;
                db.CatalogueSubmissionVerifications.AddRange(new CatalogueSubmissionVerification(submission.Id, fixture.ModeratorUserId, CatalogueVerificationArea.ProductIdentity, CatalogueVerificationStatus.Verified, "Test editorial identity", "Test verified packaging"), new CatalogueSubmissionVerification(submission.Id, fixture.ModeratorUserId, CatalogueVerificationArea.Specifications, CatalogueVerificationStatus.Verified, "Test editorial specification", "Test verified packaging"));
                submission.MarkReadyForReview(); submission.Approve();
            }
            await db.SaveChangesAsync();
        }
        if (outcome == "merge") {
            var resolve = new ResolveBarcodeProposal(fixture.PackTypeId, "Verified exact existing pack.");
            var concurrent = await Task.WhenAll(
                moderator.PostAsJsonAsync($"/api/v1/catalogue-submissions/{draft.SubmissionId}/resolve-existing-pack", resolve),
                moderator.PostAsJsonAsync($"/api/v1/catalogue-submissions/{draft.SubmissionId}/resolve-existing-pack", resolve));
            foreach (var response in concurrent) response.EnsureSuccessStatusCode();
            (await moderator.PostAsJsonAsync($"/api/v1/catalogue-submissions/{draft.SubmissionId}/resolve-existing-pack", resolve)).EnsureSuccessStatusCode();
        } else if (outcome == "publish") {
            var response = await moderator.PostAsync($"/api/v1/catalogue-submissions/{draft.SubmissionId}/publish", null);
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            var receipt = (await response.Content.ReadFromJsonAsync<CataloguePublicationReceipt>())!;
            using var scope = api.Services.CreateScope(); await scope.ServiceProvider.GetRequiredService<IPublicProductContributions>().ReconcileAsync(draft.SubmissionId, receipt.PackTypeId);
        }
        using (var scope = api.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>(); var submission = await db.CatalogueSubmissions.SingleAsync(s => s.Id == draft.SubmissionId);
            var observations = await db.Observations.Where(o => o.AuthorUserId == fixture.ExplorerUserId && o.ContributionId == key).ToListAsync();
            var photo = await db.CatalogueSubmissionImages.SingleAsync(i => i.Id == image.Id); Assert.True(photo.IsExplorerEvidence); Assert.Null(photo.ProductId);
            if (outcome == "reject") { Assert.Empty(observations); Assert.Null(submission.ResultingObservationId); }
            else { var observation = Assert.Single(observations); Assert.Equal(submission.ResultingObservationId, observation.Id); Assert.Equal(place.Id, observation.LocationId); Assert.Equal(submission.PublishedProductId, observation.ProductId); }
        }
        var atlas = (await anonymous.GetFromJsonAsync<AtlasPlace[]>("/api/v1/places/atlas"))!;
        if (outcome == "reject") Assert.DoesNotContain(atlas, p => p.Place.Id == place.Id); else Assert.Single(Assert.Single(atlas, p => p.Place.Id == place.Id).Observations);
    }
    [Fact]
    public async Task Nearby_known_shops_do_not_earn_pins_or_write_device_position() {
        using var api = new ObservationApiFactory(fixture); using var actor = Actor(api); using var anonymous = api.CreateClient();
        var response = await actor.PostAsJsonAsync("/api/v1/places/", new CreatePublicShopRequest("Nearby candidate " + Guid.NewGuid(), "1 Public Street", "Testville", "ZZ1 2ZZ", "ZZ", 51.9m, -2.1m, true, true)); response.EnsureSuccessStatusCode(); var place = (await response.Content.ReadFromJsonAsync<PlaceItem>())!;
        var found = await anonymous.PostAsJsonAsync("/api/v1/places/nearby", new NearbyPlaceRequest(51.9001m, -2.1001m)); found.EnsureSuccessStatusCode(); Assert.Equal("no-store", found.Headers.CacheControl?.ToString()); Assert.Contains((await found.Content.ReadFromJsonAsync<NearbyPlace[]>())!, p => p.Place.Id == place.Id && p.DistanceMetres < 100);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/v1/places/nearby", new NearbyPlaceRequest(100, 0))).StatusCode);
        Assert.DoesNotContain((await anonymous.GetFromJsonAsync<AtlasPlace[]>("/api/v1/places/atlas"))!, p => p.Place.Id == place.Id);
        using var scope = api.Services.CreateScope(); Assert.False(await scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>().Observations.AnyAsync(o => o.LocationId == place.Id));
    }
}
