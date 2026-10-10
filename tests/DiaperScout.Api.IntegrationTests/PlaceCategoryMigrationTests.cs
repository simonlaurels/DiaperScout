using DiaperScout.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;
public sealed class PlaceCategoryMigrationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Category_migration_leaves_existing_places_unclassified_and_preserves_exact_observations() {
        await using var db = fixture.CreateDbContext();
        var country = (await db.Locations.SingleAsync(l => l.Id == fixture.LocationId)).CountryId;
        var place = Location.PublicShop(fixture.ExplorerUserId, country, "Migration shop", "1 Street", "Town", "ZZ1 1ZZ", 51m, -2m, "category-migration");
        var observation = new Observation(fixture.ExplorerUserId, ObservationType.RetailAvailability, DateTimeOffset.UtcNow, fixture.ProductId, locationId: place.Id);
        observation.RecordExactPack(fixture.PackTypeId, Guid.NewGuid(), 18.25m, "GBP"); observation.Submit();
        db.Locations.Add(place); db.Observations.Add(observation); await db.SaveChangesAsync();
        var places = await db.Locations.CountAsync(); var observations = await db.Observations.CountAsync();
        await db.GetService<IMigrator>().MigrateAsync("20261001141156_PhysicalObservationsAndPublicProposals");
        await db.Database.MigrateAsync(); db.ChangeTracker.Clear();
        Assert.Equal(places, await db.Locations.CountAsync()); Assert.Equal(observations, await db.Observations.CountAsync());
        var saved = await db.Locations.SingleAsync(l => l.Id == place.Id);
        Assert.Null(saved.Category); Assert.True(saved.IsPublicCommercialPlace); Assert.Equal("category-migration", saved.PlaceIdentity);
        Assert.Equal(fixture.ExplorerUserId, saved.CreatedByUserId); Assert.Equal(51m, saved.Latitude); Assert.Null(saved.RetailerId);
        var evidence = await db.Observations.SingleAsync(o => o.Id == observation.Id);
        Assert.Equal(place.Id, evidence.LocationId); Assert.Equal(fixture.PackTypeId, evidence.PackTypeId); Assert.Equal(18.25m, evidence.PriceAmount); Assert.Equal(ObservationState.Submitted, evidence.State);
        saved.SetCategory(PlaceCategory.Other); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal(PlaceCategory.Other, (await db.Locations.SingleAsync(l => l.Id == place.Id)).Category);
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
