using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class ObservationApiTests : IClassFixture<PostgreSqlFixture>, IDisposable
{
    private readonly PostgreSqlFixture _fixture;
    private readonly ObservationApiFactory _factory;

    public ObservationApiTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
        _factory = new ObservationApiFactory(fixture);
    }

    [Fact]
    public async Task CreateObservation_WithoutCredentials_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/observations", ValidRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateObservation_ForSubjectWithoutExplorer_ReturnsForbidden()
    {
        using var client = AuthenticatedClient("not-an-explorer");
        var response = await client.PostAsJsonAsync("/api/v1/observations", ValidRequest());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateObservation_WithUnknownProduct_ReturnsValidationProblem()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ExplorerSubject);
        var response = await client.PostAsJsonAsync("/api/v1/observations", ValidRequest(productId: Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateObservation_WithUnknownLocation_ReturnsValidationProblem()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ExplorerSubject);
        var response = await client.PostAsJsonAsync("/api/v1/observations", ValidRequest(locationId: Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateObservation_ForExplorer_PersistsSubmittedObservationWithServerResolvedAuthor()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ExplorerSubject);
        var observedAt = DateTimeOffset.UtcNow;
        var request = new
        {
            type = ObservationType.RetailAvailability,
            observedAtUtc = observedAt,
            productId = _fixture.ProductId,
            locationId = _fixture.LocationId,
            narrative = "Synthetic integration-test observation.",
            authorUserId = Guid.NewGuid()
        };

        var response = await client.PostAsJsonAsync("/api/v1/observations", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var receipt = await response.Content.ReadFromJsonAsync<ObservationReceipt>();
        Assert.NotNull(receipt);
        Assert.Equal(ObservationState.Submitted, receipt.State);

        await using var db = _fixture.CreateDbContext();
        var persisted = await db.Observations.SingleAsync(observation => observation.Id == receipt.Id);
        Assert.Equal(_fixture.ExplorerUserId, persisted.AuthorUserId);
        Assert.Equal(_fixture.ProductId, persisted.ProductId);
        Assert.Equal(_fixture.LocationId, persisted.LocationId);
        Assert.Equal(ObservationState.Submitted, persisted.State);
    }

    [Fact]
    public async Task Development_catalogue_population_is_idempotent_and_preserves_the_curated_pack_hierarchy()
    {
        await _factory.Services.PopulateDevelopmentCatalogueAsync();
        await _factory.Services.PopulateDevelopmentCatalogueAsync();

        await using var db = _fixture.CreateDbContext();
        var record = await (from identifier in db.ProductIdentifiers
                            join pack in db.PackTypes on identifier.PackTypeId equals pack.Id
                            join size in db.SizeVariants on pack.SizeVariantId equals size.Id
                            join variant in db.ProductVariants on size.ProductVariantId equals variant.Id
                            join product in db.Products on variant.ProductId equals product.Id
                            where identifier.Type == IdentifierType.Gtin && identifier.Value == "5060572900820"
                            select new { pack.Id, ProductName = product.Name, VariantName = variant.Name, variant.BackingType, size.ManufacturerSize, size.WaistMinimumCm, size.WaistMaximumCm, pack.QuantityPerPack, pack.PackagingType })
            .SingleAsync();

        Assert.Equal("Little Rascals", record.ProductName);
        Assert.Equal("Little Rascals V2", record.VariantName);
        Assert.Equal(BackingType.Plastic, record.BackingType);
        Assert.Equal("Medium", record.ManufacturerSize);
        Assert.Equal(81, record.WaistMinimumCm);
        Assert.Equal(102, record.WaistMaximumCm);
        Assert.Equal(10, record.QuantityPerPack);
        Assert.Equal(PackagingType.Bag, record.PackagingType);
        Assert.True(await db.ProductIdentifiers.AnyAsync(value => value.PackTypeId == record.Id && value.Type == IdentifierType.Other && value.Value == "NLRM"));
    }

    private HttpClient AuthenticatedClient(string subject)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Development-Subject", subject);
        return client;
    }

    private object ValidRequest(Guid? productId = null, Guid? locationId = null) => new
    {
        type = ObservationType.RetailAvailability,
        observedAtUtc = DateTimeOffset.UtcNow,
        productId = productId ?? _fixture.ProductId,
        locationId = locationId ?? _fixture.LocationId,
        narrative = "Synthetic integration-test observation."
    };

    public void Dispose() => _factory.Dispose();
}
