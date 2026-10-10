using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class CatalogueSizeSeedingApiTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NewSize_FromAnyVariantSeedsIndependentCopiesWithoutIdentifiers(bool fromNamed)
    {
        await using var db = fixture.CreateDbContext();
        var submission = NewSubmission();
        var variants = new[] { new CatalogueSubmissionVariant(submission.Id, null),
            new CatalogueSubmissionVariant(submission.Id, "Plain"), new CatalogueSubmissionVariant(submission.Id, "Printed") };
        db.Add(submission);
        db.AddRange(variants);
        await db.SaveChangesAsync();
        var source = variants[fromNamed ? 1 : 0];
        var gtin = Random.Shared.NextInt64(10000000000000, 99999999999999).ToString();
        using var factory = new ObservationApiFactory(fixture);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Development-Subject", PostgreSqlFixture.ModeratorSubject);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{submission.Id}/variants/{source.Id}/sizes/seed",
            new { manufacturerSize = "Medium", waistMinimumCm = 80, waistMaximumCm = 110,
                hipMinimumCm = 85, manufacturerPackQuantity = 10, fitMeasurementBasis = "Waist", gtin });
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var receipt = await response.Content.ReadFromJsonAsync<CatalogueSubmissionSizeVariantReceipt>();
        Assert.NotNull(receipt);
        db.ChangeTracker.Clear();
        var ids = variants.Select(x => x.Id).ToArray();
        var sizes = await db.CatalogueSubmissionSizeVariants.Where(x => ids.Contains(x.VariantId)).ToListAsync();
        Assert.Equal(3, sizes.Count);
        Assert.Equal(3, sizes.Select(x => x.Id).Distinct().Count());
        foreach (var size in sizes)
        {
            Assert.Equal("Medium", size.ManufacturerSize);
            Assert.Equal(80, size.WaistMinimumCm);
            Assert.Equal(110, size.WaistMaximumCm);
            Assert.Equal(85, size.HipMinimumCm);
            Assert.Equal(10, size.ManufacturerPackQuantity);
            Assert.Equal("Waist", size.FitMeasurementBasis);
            Assert.Equal(size.VariantId == source.Id ? gtin : null, size.Gtin);
        }
        var edit = await client.PutAsJsonAsync(
            $"/api/v1/catalogue-submissions/{submission.Id}/variants/{source.Id}/sizes/{receipt.Id}",
            new { manufacturerSize = "Medium", waistMinimumCm = 90, manufacturerPackQuantity = 12, gtin });
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        db.ChangeTracker.Clear();
        Assert.Equal(90, (await db.CatalogueSubmissionSizeVariants.SingleAsync(x => x.Id == receipt.Id)).WaistMinimumCm);
        var siblings = await db.CatalogueSubmissionSizeVariants.Where(x => ids.Contains(x.VariantId) && x.Id != receipt.Id).ToListAsync();
        Assert.All(siblings, x => { Assert.Equal(80, x.WaistMinimumCm); Assert.Equal(10, x.ManufacturerPackQuantity); Assert.Null(x.Gtin); });
        var remove = await client.DeleteAsync(
            $"/api/v1/catalogue-submissions/{submission.Id}/variants/{source.Id}/sizes/{receipt.Id}");
        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);
        db.ChangeTracker.Clear();
        Assert.Equal(2, await db.CatalogueSubmissionSizeVariants.CountAsync(x => ids.Contains(x.VariantId)));
        Assert.False(await db.CatalogueSubmissionSizeVariants.AnyAsync(x => x.Id == receipt.Id));
    }

    [Fact]
    public async Task ExistingSiblingSize_IsNotOverwrittenOrRecreated()
    {
        await using var db = fixture.CreateDbContext();
        var submission = NewSubmission();
        var source = new CatalogueSubmissionVariant(submission.Id, "Printed");
        var sibling = new CatalogueSubmissionVariant(submission.Id, null);
        var existing = new CatalogueSubmissionSizeVariant(sibling.Id, "medium", waistMinimumCm: 95,
            manufacturerPackQuantity: 8, gtin: "98765432101234");
        db.AddRange(submission, source, sibling, existing);
        await db.SaveChangesAsync();
        using var factory = new ObservationApiFactory(fixture);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Development-Subject", PostgreSqlFixture.ModeratorSubject);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{submission.Id}/variants/{source.Id}/sizes/seed",
            new { manufacturerSize = " Medium ", waistMinimumCm = 80, manufacturerPackQuantity = 10 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        db.ChangeTracker.Clear();
        var retained = await db.CatalogueSubmissionSizeVariants.SingleAsync(x => x.VariantId == sibling.Id);
        Assert.Equal(existing.Id, retained.Id);
        Assert.Equal(95, retained.WaistMinimumCm);
        Assert.Equal(8, retained.ManufacturerPackQuantity);
        Assert.Equal("98765432101234", retained.Gtin);
    }

    [Fact]
    public async Task DuplicateSourceSize_FailsWithoutCreatingSiblingCopies()
    {
        await using var db = fixture.CreateDbContext();
        var submission = NewSubmission();
        var source = new CatalogueSubmissionVariant(submission.Id, "Printed");
        var sibling = new CatalogueSubmissionVariant(submission.Id, null);
        var existing = new CatalogueSubmissionSizeVariant(source.Id, "Medium", manufacturerPackQuantity: 8);
        db.AddRange(submission, source, sibling, existing);
        await db.SaveChangesAsync();
        using var factory = new ObservationApiFactory(fixture);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Development-Subject", PostgreSqlFixture.ModeratorSubject);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{submission.Id}/variants/{source.Id}/sizes/seed",
            new { manufacturerSize = "Medium", manufacturerPackQuantity = 10 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(await db.CatalogueSubmissionSizeVariants.AnyAsync(x => x.VariantId == sibling.Id));
        Assert.Equal(1, await db.CatalogueSubmissionSizeVariants.CountAsync(x => x.VariantId == source.Id));
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData(PostgreSqlFixture.ExplorerSubject, HttpStatusCode.Forbidden)]
    public async Task Seeding_RequiresModerator(string? subject, HttpStatusCode expected)
    {
        using var factory = new ObservationApiFactory(fixture);
        using var client = factory.CreateClient();
        if (subject is not null) client.DefaultRequestHeaders.Add("X-Development-Subject", subject);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{Guid.NewGuid()}/variants/{Guid.NewGuid()}/sizes/seed",
            new { manufacturerSize = "Medium" });
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task FailedSiblingInsert_RollsBackSourceAndAllCopies()
    {
        await using var db = fixture.CreateDbContext();
        var submission = NewSubmission();
        var source = new CatalogueSubmissionVariant(submission.Id, "Printed");
        var sibling = new CatalogueSubmissionVariant(submission.Id, null);
        db.AddRange(submission, source, sibling);
        await db.SaveChangesAsync();
        var constraint = "seed_fault_" + Guid.NewGuid().ToString("N");
        // Force a failure after the source insert, in this disposable test database only.
        await db.Database.OpenConnectionAsync();
        await using var ddl = db.Database.GetDbConnection().CreateCommand();
        ddl.CommandText = "ALTER TABLE diaperscout.catalogue_submission_size_variants ADD CONSTRAINT " +
            constraint + " CHECK (\"VariantId\" <> '" + sibling.Id + "')";
        await ddl.ExecuteNonQueryAsync();
        try
        {
            using var factory = new ObservationApiFactory(fixture);
            using var scope = factory.Services.CreateScope();
            var submissions = scope.ServiceProvider.GetRequiredService<ICatalogueSubmissions>();
            await Assert.ThrowsAsync<DbUpdateException>(() => submissions.AddSizeVariantToAllVariantsAsync(
                new AuthenticatedUser(fixture.ModeratorUserId, PostgreSqlFixture.ModeratorSubject),
                submission.Id, source.Id,
                new AddCatalogueSubmissionSizeVariant("Medium", 80, 110, null, null, null,
                    null, null, null, null, null, null, 10, null)));
            Assert.False(await db.CatalogueSubmissionSizeVariants.AnyAsync(x =>
                x.VariantId == source.Id || x.VariantId == sibling.Id));
        }
        finally
        {
            ddl.CommandText = "ALTER TABLE diaperscout.catalogue_submission_size_variants DROP CONSTRAINT " + constraint;
            await ddl.ExecuteNonQueryAsync();
        }
    }

    private CatalogueSubmission NewSubmission() => new(CatalogueSubmissionSource.Moderator,
        fixture.ModeratorUserId, "Seeding manufacturer", "Seeding product");
}
