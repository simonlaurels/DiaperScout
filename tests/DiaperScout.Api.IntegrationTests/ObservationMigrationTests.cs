using DiaperScout.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;
public sealed class ObservationMigrationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Additive_migration_preserves_legacy_location_observation_catalogue_and_users()
    {
        // This fixture owns a disposable PostgreSQL container. Never run Down against production.
        await using var db = fixture.CreateDbContext();
        var observation = new Observation(fixture.ExplorerUserId, ObservationType.RetailAvailability,
            DateTimeOffset.UtcNow, fixture.ProductId, locationId:fixture.LocationId);
        observation.Submit(); db.Observations.Add(observation); await db.SaveChangesAsync();
        var storedTime = await db.Observations.AsNoTracking().Where(o=>o.Id==observation.Id).Select(o=>o.ObservedAtUtc).SingleAsync();
        var products = await db.Products.CountAsync(); var users = await db.Users.CountAsync();
        var listings = await db.RetailerProductListings.CountAsync(); var submissions = await db.CatalogueSubmissions.CountAsync();
        await db.GetService<IMigrator>().MigrateAsync("20260929203653_AddPasskeyAuthentication");
        await db.Database.MigrateAsync(); db.ChangeTracker.Clear();
        Assert.Equal(products, await db.Products.CountAsync()); Assert.Equal(users, await db.Users.CountAsync());
        Assert.Equal(listings, await db.RetailerProductListings.CountAsync()); Assert.Equal(submissions, await db.CatalogueSubmissions.CountAsync());
        var legacy = await db.Locations.SingleAsync(l=>l.Id==fixture.LocationId);
        Assert.Equal(fixture.RetailerId,legacy.RetailerId); Assert.False(legacy.IsPublicCommercialPlace);
        Assert.Null(legacy.PlaceIdentity); Assert.Null(legacy.CreatedByUserId);
        var preserved = await db.Observations.SingleAsync(o=>o.Id==observation.Id);
        Assert.Equal(fixture.ExplorerUserId,preserved.AuthorUserId); Assert.Equal(fixture.ProductId,preserved.ProductId);
        Assert.Equal(fixture.LocationId,preserved.LocationId); Assert.Equal(storedTime,preserved.ObservedAtUtc);
        Assert.Null(preserved.PackTypeId); Assert.Null(preserved.ContributionId);
    }
}
