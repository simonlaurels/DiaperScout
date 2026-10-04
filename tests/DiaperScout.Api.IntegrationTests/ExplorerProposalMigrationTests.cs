using DiaperScout.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class ExplorerProposalMigrationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Additive_evidence_migration_preserves_legacy_proposal_image_and_exact_observation()
    {
        // Down/Up is restricted to this fixture's disposable PostgreSQL container.
        await using var db = fixture.CreateDbContext();
        var proposal = new CatalogueSubmission(CatalogueSubmissionSource.Explorer, fixture.ExplorerUserId, "Legacy manufacturer", "Legacy proposed product", notes: "Preserved packaging notes");
        var key = Guid.NewGuid(); proposal.SetPublicContribution(key); proposal.UpdateIdentity("96385074", null, null); proposal.BeginVerification();
        var image = new CatalogueSubmissionImage(proposal.Id, CatalogueSubmissionImageRole.PackFront, "legacy-test.png", "legacy-test.png", "image/png", 100, CatalogueImageSourceType.Other, permissionStatus: CatalogueImagePermissionStatus.PermissionGranted);
        image.PublishToProduct(fixture.ProductId); image.SetPrimary(true);
        var observation = new Observation(fixture.ExplorerUserId, ObservationType.RetailAvailability, DateTimeOffset.UtcNow, fixture.ProductId, locationId: fixture.LocationId);
        observation.RecordExactPack(fixture.PackTypeId, Guid.NewGuid(), 18.25m, "GBP"); observation.Submit();
        db.AddRange(proposal, image, observation); await db.SaveChangesAsync();
        var productCount = await db.Products.CountAsync(); var observationCount = await db.Observations.CountAsync();
        await db.GetService<IMigrator>().MigrateAsync("20261003102616_AddPlaceCategory");
        await db.Database.MigrateAsync(); db.ChangeTracker.Clear();
        var preserved = await db.CatalogueSubmissions.SingleAsync(s => s.Id == proposal.Id);
        Assert.Equal(key, preserved.PublicContributionId); Assert.Equal(fixture.ExplorerUserId, preserved.SubmittedByUserId);
        Assert.Equal("Preserved packaging notes", preserved.Notes); Assert.Equal(CatalogueSubmissionStatus.InVerification, preserved.Status);
        Assert.Null(preserved.SuggestedExistingProductId); Assert.Null(preserved.PendingLocationId); Assert.Null(preserved.PendingObservedAtUtc);
        Assert.Null(preserved.PendingPriceAmount); Assert.Null(preserved.PendingCurrencyCode); Assert.Null(preserved.ResultingObservationId);
        var legacyImage = await db.CatalogueSubmissionImages.SingleAsync(i => i.Id == image.Id);
        Assert.False(legacyImage.IsExplorerEvidence); Assert.Null(legacyImage.EvidenceUploadId); Assert.Null(legacyImage.EvidenceContentHash);
        Assert.Equal(fixture.ProductId, legacyImage.ProductId); Assert.True(legacyImage.IsPrimary); Assert.Equal(CatalogueContentVisibility.Public, legacyImage.Visibility);
        var legacyObservation = await db.Observations.SingleAsync(o => o.Id == observation.Id);
        Assert.Equal(fixture.PackTypeId, legacyObservation.PackTypeId); Assert.Equal(18.25m, legacyObservation.PriceAmount); Assert.Equal(ObservationState.Submitted, legacyObservation.State);
        Assert.Equal(productCount, await db.Products.CountAsync()); Assert.Equal(observationCount, await db.Observations.CountAsync());
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
