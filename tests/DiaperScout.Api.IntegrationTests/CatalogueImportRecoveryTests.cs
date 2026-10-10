using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class CatalogueImportRecoveryTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private async Task<CatalogueSubmissionImportResult> Import(string rows)
    {
        using var factory = new ObservationApiFactory(fixture);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Development-Subject", PostgreSqlFixture.ModeratorSubject);
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("ImportProductKey,Manufacturer,ProductName,ManufacturerSize,ManufacturerPackQuantity,GTIN\n" + rows), "file", "research.csv");
        var response = await client.PostAsync("/api/v1/catalogue-submissions/import-csv", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CatalogueSubmissionImportResult>())!;
    }

    [Fact]
    public async Task RetryWithPaddedGtinDoesNotDuplicateDraft()
    {
        var name = "Retry " + Guid.NewGuid();
        Assert.Equal(1, (await Import($"retry,Research Manufacturer,{name},Medium,14,4052199303413\n")).RowsImported);
        var retry = await Import($"retry,Research Manufacturer,{name},Medium,14,04052199303413\n");
        Assert.Equal(0, retry.SubmissionsCreated);
        Assert.Equal(1, retry.RowsSkipped);
        await using var db = fixture.CreateDbContext();
        Assert.Equal(1, await db.CatalogueSubmissions.CountAsync(s => s.ProposedProductName == name));
    }

    [Fact]
    public async Task PaddedCanonicalGtinIsSkippedRegardlessOfProductIdentity()
    {
        await using (var db = fixture.CreateDbContext())
        {
            db.ProductIdentifiers.Add(new ProductIdentifier(fixture.PackTypeId, IdentifierType.Gtin, "5900516695750"));
            await db.SaveChangesAsync();
        }
        var result = await Import("canonical,Other Manufacturer,Other Product,Medium,10,05900516695750\n");
        Assert.Equal(0, result.SubmissionsCreated);
        Assert.Equal(1, result.RowsSkipped);
    }

    [Fact]
    public async Task GtinlessRetryRequiresEnrichmentInsteadOfAnotherDraft()
    {
        var name = "No barcode " + Guid.NewGuid();
        var row = $"no-code,Research Manufacturer,{name},Medium,10,\n";
        Assert.Equal(1, (await Import(row)).SubmissionsCreated);
        var retry = await Import(row);
        Assert.Equal(0, retry.SubmissionsCreated);
        Assert.Contains(retry.Warnings, w => w.Contains("enrich"));
    }

    [Fact]
    public async Task EquivalentIdentifiersWithinGroupRejectWholeGroupAndNextGroupSucceeds()
    {
        var name = "Conflicting rows " + Guid.NewGuid();
        var next = "Safe next " + Guid.NewGuid();
        var result = await Import($"bad,Research Manufacturer,{name},Medium,30,4052199297002\n" +
            $"bad,Research Manufacturer,{name},Large,30,04052199297002\n" +
            $"good,Research Manufacturer,{next},Medium,16,7332152207383\n");
        Assert.Equal(1, result.SubmissionsCreated);
        Assert.Equal(1, result.RowsImported);
        Assert.Equal(2, result.RowsSkipped);
        await using var db = fixture.CreateDbContext();
        Assert.False(await db.CatalogueSubmissions.AnyAsync(s => s.ProposedProductName == name));
        Assert.True(await db.CatalogueSubmissions.AnyAsync(s => s.ProposedProductName == next));
    }
}
