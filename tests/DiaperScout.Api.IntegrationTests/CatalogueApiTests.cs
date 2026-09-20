using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Microsoft.AspNetCore.Mvc;

namespace DiaperScout.Api.IntegrationTests;

public sealed class CatalogueApiTests : IClassFixture<PostgreSqlFixture>, IDisposable
{
    private readonly PostgreSqlFixture _fixture;
    private readonly ObservationApiFactory _factory;

    public CatalogueApiTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
        _factory = new ObservationApiFactory(fixture);
    }

    [Fact]
    public async Task CreateCanonicalProduct_WithoutCredentials_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/products",
            Request("7000000000001", "unauthenticated-product"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCanonicalProduct_ForExplorerWithoutModeratorAuthority_ReturnsForbidden()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ExplorerSubject);
        var response = await client.PostAsJsonAsync(
            "/api/v1/products",
            Request("7000000000002", "explorer-product"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCanonicalProduct_ForAdministratorWithoutModeratorAuthority_ReturnsForbidden()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.AdministratorSubject);
        var response = await client.PostAsJsonAsync(
            "/api/v1/products",
            Request("7000000000003", "administrator-product"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCanonicalProduct_ForModerator_PersistsCompleteHierarchyAndAudit()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);
        client.DefaultRequestHeaders.Add("X-Correlation-ID", "catalogue-integration-test");

        var response = await client.PostAsJsonAsync(
            "/api/v1/products",
            Request("7000000000004", "moderator-created-product"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var receipt = await response.Content.ReadFromJsonAsync<CanonicalProductReceipt>();
        Assert.NotNull(receipt);

        await using var db = _fixture.CreateDbContext();

        var hierarchy = await (
            from product in db.Products
            join variant in db.ProductVariants on product.Id equals variant.ProductId
            join size in db.SizeVariants on variant.Id equals size.ProductVariantId
            join pack in db.PackTypes on size.Id equals pack.SizeVariantId
            join identifier in db.ProductIdentifiers on pack.Id equals identifier.PackTypeId
            where product.Id == receipt.ProductId
                  && identifier.Type == IdentifierType.Gtin
            select new
            {
                product.ManufacturerId,
                product.BrandId,
                ProductName = product.Name,
                VariantName = variant.Name,
                size.ManufacturerSize,
                pack.QuantityPerPack,
                pack.PackagingType,
                Gtin = identifier.Value
            })
            .SingleAsync();

        Assert.Equal(_fixture.ManufacturerId, hierarchy.ManufacturerId);
        Assert.Equal(_fixture.BrandId, hierarchy.BrandId);
        Assert.Equal("moderator created product", hierarchy.ProductName);
        Assert.Equal("Integration variant", hierarchy.VariantName);
        Assert.Equal("Medium", hierarchy.ManufacturerSize);
        Assert.Equal(10, hierarchy.QuantityPerPack);
        Assert.Equal(PackagingType.Bag, hierarchy.PackagingType);
        Assert.Equal("7000000000004", hierarchy.Gtin);

        var audit = await db.CatalogueAuditRecords
            .SingleAsync(record => record.Id == receipt.AuditRecordId);

        Assert.Equal(CatalogueAuditAction.ProductCreated, audit.Action);
        Assert.Equal(receipt.ProductId, audit.ProductId);
        Assert.Equal(_fixture.ModeratorUserId, audit.ActingUserId);
        Assert.Equal("Primary manufacturer product sheet.", audit.SourceSummary);
        Assert.Contains(
            "[https://example.test/source](https://example.test/source)",
            audit.SourceReferencesJson);
        Assert.Equal(
            "Verified against the cited product sheet.",
            audit.EditorialRationale);
        Assert.Equal(
            "catalogue-integration-test",
            audit.CorrelationId);
        Assert.Contains(
            receipt.PackTypeId.ToString(),
            audit.AffectedCanonicalIdsJson);
    }

    [Fact]
    public async Task CreateCanonicalProduct_RejectsExistingGtinWithoutPersistingAPartialHierarchy()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);
        var slug = "duplicate-gtin-product";

        var response = await client.PostAsJsonAsync(
            "/api/v1/products",
            Request("12345678", slug));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var db = _fixture.CreateDbContext();

        Assert.False(await db.Products.AnyAsync(
            product => product.Slug == slug));
    }

    [Fact]
    public async Task CreateCatalogueSubmission_WithoutCredentials_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCatalogueSubmission_ForExplorerWithoutModeratorAuthority_ReturnsForbidden()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ExplorerSubject);

        var response = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCatalogueSubmission_ForModerator_PersistsDraftWithoutCreatingCanonicalProduct()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var response = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var receipt = await response.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(receipt);
        Assert.Equal(CatalogueSubmissionStatus.Draft, receipt.Status);

        await using var db = _fixture.CreateDbContext();

        var submission = await db.CatalogueSubmissions
            .SingleAsync(value => value.Id == receipt.Id);

        Assert.Equal(
            CatalogueSubmissionSource.Moderator,
            submission.Source);
        Assert.Equal(
            _fixture.ModeratorUserId,
            submission.SubmittedByUserId);
        Assert.Equal(
            "Test Manufacturer",
            submission.ProposedManufacturerName);
        Assert.Equal(
            "Test Brand",
            submission.ProposedBrandName);
        Assert.Equal(
            "Test Product",
            submission.ProposedProductName);
        Assert.Equal(
            "Test Variant",
            submission.ProposedVariantName);
        Assert.Equal(
            "Integration test submission.",
            submission.Notes);
        Assert.Null(submission.PublishedProductId);

        Assert.False(await db.Products.AnyAsync(
            product => product.Name == "Test Product"));
    }

    [Fact]
    public async Task CatalogueSubmission_ManagesOptionalBaseAndNamedVariants()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            new
            {
                source = CatalogueSubmissionSource.Moderator,
                proposedManufacturerName = "Variant Test Manufacturer",
                proposedBrandName = "Variant Test Brand",
                proposedProductName = "Variant Test Product"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CatalogueSubmissionReceipt>();
        Assert.NotNull(created);

        var initialVariants = await client.GetFromJsonAsync<
            IReadOnlyList<CatalogueSubmissionVariantReceipt>>(
            $"/api/v1/catalogue-submissions/{created.Id}/variants");

        Assert.NotNull(initialVariants);
        Assert.Empty(initialVariants);

        var baseResponse = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/variants",
            new { name = (string?)null });

        Assert.Equal(HttpStatusCode.Created, baseResponse.StatusCode);

        var baseVariant = await baseResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionVariantReceipt>();

        Assert.NotNull(baseVariant);
        Assert.Null(baseVariant.Name);

        var namedResponse = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/variants",
            new { name = "Plastic Edition" });

        Assert.Equal(HttpStatusCode.Created, namedResponse.StatusCode);

        var namedVariant = await namedResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionVariantReceipt>();

        Assert.NotNull(namedVariant);
        Assert.Equal("Plastic Edition", namedVariant.Name);

        var duplicateBaseResponse = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/variants",
            new { name = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, duplicateBaseResponse.StatusCode);

        var renameBaseResponse = await client.PutAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/variants/{baseVariant.Id}",
            new { name = "Original" });

        Assert.Equal(HttpStatusCode.BadRequest, renameBaseResponse.StatusCode);

        var removeBaseResponse = await client.DeleteAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/variants/{baseVariant.Id}");

        Assert.Equal(HttpStatusCode.NoContent, removeBaseResponse.StatusCode);

        var remainingAfterBaseRemoval = await client.GetFromJsonAsync<
            IReadOnlyList<CatalogueSubmissionVariantReceipt>>(
            $"/api/v1/catalogue-submissions/{created.Id}/variants");

        Assert.NotNull(remainingAfterBaseRemoval);
        var remainingNamedVariant = Assert.Single(remainingAfterBaseRemoval);
        Assert.Equal(namedVariant.Id, remainingNamedVariant.Id);
        Assert.Equal("Plastic Edition", remainingNamedVariant.Name);

        var removeLastVariantResponse = await client.DeleteAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/variants/{namedVariant.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, removeLastVariantResponse.StatusCode);

        var remainingVariants = await client.GetFromJsonAsync<
            IReadOnlyList<CatalogueSubmissionVariantReceipt>>(
            $"/api/v1/catalogue-submissions/{created.Id}/variants");

        Assert.NotNull(remainingVariants);
        var remaining = Assert.Single(remainingVariants);
        Assert.Equal(namedVariant.Id, remaining.Id);
        Assert.Equal("Plastic Edition", remaining.Name);
    }

    [Fact]
    public async Task CatalogueSubmission_CanUpdateProductIdentityWhileDraft()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(created);

        var identityResponse = await client.PutAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/identity",
            new
            {
                proposedGtin = "12345678",
                proposedSku = "SKU-TEST-001",
                identitySourceUrl = "https://manufacturer.example/products/test"
            });

        Assert.Equal(HttpStatusCode.OK, identityResponse.StatusCode);

        var updated = await identityResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(updated);
        Assert.Equal("12345678", updated.ProposedGtin);
        Assert.Equal("SKU-TEST-001", updated.ProposedSku);
        Assert.Equal(
            "https://manufacturer.example/products/test",
            updated.IdentitySourceUrl);

        await using var db = _fixture.CreateDbContext();

        var submission = await db.CatalogueSubmissions
            .SingleAsync(value => value.Id == created.Id);

        Assert.Equal("12345678", submission.ProposedGtin);
        Assert.Equal("SKU-TEST-001", submission.ProposedSku);
        Assert.Equal(
            "https://manufacturer.example/products/test",
            submission.IdentitySourceUrl);
        Assert.False(await db.Products.AnyAsync(
            product => product.Name == "Test Product"));
    }

    [Fact]
    public async Task CatalogueSubmission_CannotUpdateProductIdentityAfterVerificationBegins()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(created);

        var verificationResponse = await client.PostAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/begin-verification",
            content: null);

        Assert.Equal(HttpStatusCode.OK, verificationResponse.StatusCode);

        var identityResponse = await client.PutAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/identity",
            new
            {
                proposedGtin = "12345678",
                proposedSku = "SKU-TEST-001",
                identitySourceUrl = "https://manufacturer.example/products/test"
            });

        Assert.Equal(HttpStatusCode.BadRequest, identityResponse.StatusCode);

        await using var db = _fixture.CreateDbContext();

        var submission = await db.CatalogueSubmissions
            .SingleAsync(value => value.Id == created.Id);

        Assert.Equal(
            CatalogueSubmissionStatus.InVerification,
            submission.Status);
        Assert.Null(submission.ProposedGtin);
        Assert.Null(submission.ProposedSku);
        Assert.Null(submission.IdentitySourceUrl);
    }

    [Fact]
    public async Task UpdateCatalogueSubmissionIdentity_ForExplorerWithoutModeratorAuthority_ReturnsForbidden()
    {
        using var moderatorClient = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await moderatorClient.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(created);

        using var explorerClient = AuthenticatedClient(PostgreSqlFixture.ExplorerSubject);

        var response = await explorerClient.PutAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/identity",
            new
            {
                proposedGtin = "12345678",
                proposedSku = "SKU-TEST-001",
                identitySourceUrl = "https://manufacturer.example/products/test"
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCatalogueSubmissionSpecifications_PersistsWithoutCreatingCanonicalProduct()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(created);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/specifications",
            new
            {
                proposedProductType = ProductType.Tape,
                proposedPackagingType = PackagingType.Bag
            });

        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK, responseBody);

        var receipt = await response.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(receipt);
        Assert.Equal(ProductType.Tape, receipt.ProposedProductType);
        Assert.Equal(PackagingType.Bag, receipt.ProposedPackagingType);

        await using var db = _fixture.CreateDbContext();
        var submission = await db.CatalogueSubmissions.SingleAsync(value => value.Id == created.Id);

        Assert.Equal(ProductType.Tape, submission.ProposedProductType);
        Assert.Equal(PackagingType.Bag, submission.ProposedPackagingType);
    }

    [Fact]
    public async Task AddCatalogueSubmissionVerification_ForModerator_PersistsVerificationRecord()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(created);

        var beginResponse = await client.PostAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/begin-verification",
            content: null);

        Assert.Equal(HttpStatusCode.OK, beginResponse.StatusCode);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/verifications",
            new
            {
                area = CatalogueVerificationArea.ProductIdentity,
                status = CatalogueVerificationStatus.Verified,
                scope = "Product name and variant identity.",
                source = "Manufacturer product page.",
                sourceUrl = "https://example.test/product",
                notes = "Identity matches the manufacturer's published information.",
                permissionTerms = (string?)null
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var receipt = await response.Content
            .ReadFromJsonAsync<CatalogueSubmissionVerificationReceipt>();

        Assert.NotNull(receipt);
        Assert.Equal(created.Id, receipt.SubmissionId);
        Assert.Equal(_fixture.ModeratorUserId, receipt.VerifiedByUserId);
        Assert.Equal(CatalogueVerificationArea.ProductIdentity, receipt.Area);
        Assert.Equal(CatalogueVerificationStatus.Verified, receipt.Status);
        Assert.Equal("Product name and variant identity.", receipt.Scope);
        Assert.Equal("Manufacturer product page.", receipt.Source);
        Assert.Equal("https://example.test/product", receipt.SourceUrl);

        await using var db = _fixture.CreateDbContext();
        var verification = await db.CatalogueSubmissionVerifications
            .SingleAsync(value => value.Id == receipt.Id);

        Assert.Equal(created.Id, verification.SubmissionId);
        Assert.Equal(_fixture.ModeratorUserId, verification.VerifiedByUserId);
        Assert.Equal(CatalogueVerificationArea.ProductIdentity, verification.Area);
        Assert.Equal(CatalogueVerificationStatus.Verified, verification.Status);
    }

    [Fact]
    public async Task AddCatalogueSubmissionRetailDestination_ForModerator_PersistsDestination()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(created);

        var beginResponse = await client.PostAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/begin-verification",
            content: null);

        Assert.Equal(HttpStatusCode.OK, beginResponse.StatusCode);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/retail-destinations",
            new
            {
                retailerId = _fixture.RetailerId,
                listingUrl = "https://shop.example.test/product",
                notes = "Current retailer listing."
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var receipt = await response.Content
            .ReadFromJsonAsync<CatalogueSubmissionRetailDestinationReceipt>();

        Assert.NotNull(receipt);
        Assert.Equal(created.Id, receipt.SubmissionId);
        Assert.Equal(_fixture.RetailerId, receipt.RetailerId);
        Assert.Equal("https://shop.example.test/product", receipt.ListingUrl);
        Assert.Equal("Current retailer listing.", receipt.Notes);

        await using var db = _fixture.CreateDbContext();
        var destination = await db.CatalogueSubmissionRetailDestinations
            .SingleAsync(value => value.Id == receipt.Id);

        Assert.Equal(created.Id, destination.SubmissionId);
        Assert.Equal(_fixture.RetailerId, destination.RetailerId);
        Assert.Equal("https://shop.example.test/product", destination.ListingUrl);
    }

    [Fact]
    public async Task GetCatalogueRetailers_ForModerator_ReturnsRetailers()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var response = await client.GetAsync("/api/v1/catalogue-submissions/retailers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var retailers = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<CatalogueRetailerOption>>();

        Assert.NotNull(retailers);
        Assert.Contains(retailers, retailer => retailer.Id == _fixture.RetailerId && retailer.Name == "Integration Test Retailer");
    }

    [Fact]
    public async Task GetCatalogueSubmissionRetailDestinations_ReturnsAllDestinations()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CatalogueSubmissionReceipt>();
        Assert.NotNull(created);

        var beginResponse = await client.PostAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/begin-verification", content: null);
        Assert.Equal(HttpStatusCode.OK, beginResponse.StatusCode);

        foreach (var url in new[] { "https://shop.example.test/one", "https://shop.example.test/two" })
        {
            var addResponse = await client.PostAsJsonAsync(
                $"/api/v1/catalogue-submissions/{created.Id}/retail-destinations",
                new { retailerId = _fixture.RetailerId, listingUrl = url });
            Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);
        }

        var response = await client.GetAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/retail-destinations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var destinations = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<CatalogueSubmissionRetailDestinationReceipt>>();

        Assert.NotNull(destinations);
        Assert.Equal(2, destinations.Count);
        Assert.Equal(
            new[] { "https://shop.example.test/one", "https://shop.example.test/two" },
            destinations.Select(destination => destination.ListingUrl).OrderBy(value => value));
    }

    [Fact]
    public async Task AddCatalogueSubmissionRetailDestination_BeforeVerification_ReturnsValidationProblem()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(created);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/retail-destinations",
            new
            {
                retailerId = _fixture.RetailerId,
                listingUrl = "https://shop.example.test/product"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var db = _fixture.CreateDbContext();
        Assert.False(await db.CatalogueSubmissionRetailDestinations
            .AnyAsync(value => value.SubmissionId == created.Id));
    }

    [Fact]
    public async Task AddCatalogueSubmissionRetailDestination_ForExplorer_ReturnsForbidden()
    {
        using var moderatorClient = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await moderatorClient.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(created);

        var beginResponse = await moderatorClient.PostAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/begin-verification",
            content: null);

        Assert.Equal(HttpStatusCode.OK, beginResponse.StatusCode);

        using var explorerClient = AuthenticatedClient(PostgreSqlFixture.ExplorerSubject);
        var response = await explorerClient.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/retail-destinations",
            new
            {
                retailerId = _fixture.RetailerId,
                listingUrl = "https://shop.example.test/product"
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        await using var db = _fixture.CreateDbContext();
        Assert.False(await db.CatalogueSubmissionRetailDestinations
            .AnyAsync(value => value.SubmissionId == created.Id));
    }

    [Fact]
    public async Task AddCatalogueSubmissionRetailAffiliate_ForModerator_PersistsAffiliateRecord()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CatalogueSubmissionReceipt>();
        Assert.NotNull(created);

        var beginResponse = await client.PostAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/begin-verification", content: null);
        Assert.Equal(HttpStatusCode.OK, beginResponse.StatusCode);

        var destinationResponse = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/retail-destinations",
            new { retailerId = _fixture.RetailerId, listingUrl = "https://shop.example.test/product" });
        Assert.Equal(HttpStatusCode.Created, destinationResponse.StatusCode);

        var destination = await destinationResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionRetailDestinationReceipt>();
        Assert.NotNull(destination);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/retail-destinations/{destination.Id}/affiliate",
            new
            {
                status = AffiliateProgrammeStatus.Configured,
                network = "Example Network",
                trackingConfiguration = "Publisher configured.",
                deepLinkMechanism = "Product deep link.",
                termsUrl = "https://shop.example.test/affiliate-terms",
                applicationReference = "AFF-123",
                notes = "Affiliate destination verified."
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var receipt = await response.Content
            .ReadFromJsonAsync<CatalogueSubmissionRetailAffiliateReceipt>();
        Assert.NotNull(receipt);
        Assert.Equal(created.Id, receipt.SubmissionId);
        Assert.Equal(destination.Id, receipt.RetailDestinationId);
        Assert.Equal(AffiliateProgrammeStatus.Configured, receipt.Status);
        Assert.Equal("Example Network", receipt.Network);

        await using var db = _fixture.CreateDbContext();
        var affiliate = await db.CatalogueSubmissionRetailAffiliates
            .SingleAsync(value => value.Id == receipt.Id);

        Assert.Equal(created.Id, affiliate.SubmissionId);
        Assert.Equal(destination.Id, affiliate.RetailDestinationId);
        Assert.Equal(AffiliateProgrammeStatus.Configured, affiliate.Status);
    }

    [Fact]
    public async Task AddCatalogueSubmissionRetailAffiliate_BeforeVerification_ReturnsValidationProblem()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CatalogueSubmissionReceipt>();
        Assert.NotNull(created);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/retail-destinations/00000000-0000-0000-0000-000000000001/affiliate",
            new { status = AffiliateProgrammeStatus.Configured });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var db = _fixture.CreateDbContext();
        Assert.False(await db.CatalogueSubmissionRetailAffiliates
            .AnyAsync(value => value.SubmissionId == created.Id));
    }

    [Fact]
    public async Task AddCatalogueSubmissionRetailAffiliate_ForExplorer_ReturnsForbidden()
    {
        using var moderatorClient = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await moderatorClient.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CatalogueSubmissionReceipt>();
        Assert.NotNull(created);

        var beginResponse = await moderatorClient.PostAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/begin-verification", content: null);
        Assert.Equal(HttpStatusCode.OK, beginResponse.StatusCode);

        var destinationResponse = await moderatorClient.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/retail-destinations",
            new { retailerId = _fixture.RetailerId, listingUrl = "https://shop.example.test/product" });
        Assert.Equal(HttpStatusCode.Created, destinationResponse.StatusCode);

        var destination = await destinationResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionRetailDestinationReceipt>();
        Assert.NotNull(destination);

        using var explorerClient = AuthenticatedClient(PostgreSqlFixture.ExplorerSubject);
        var response = await explorerClient.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/retail-destinations/{destination.Id}/affiliate",
            new { status = AffiliateProgrammeStatus.Configured });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        await using var db = _fixture.CreateDbContext();
        Assert.False(await db.CatalogueSubmissionRetailAffiliates
            .AnyAsync(value => value.SubmissionId == created.Id));
    }

    [Fact]
    public async Task AddCatalogueSubmissionVerification_BeforeVerification_ReturnsValidationProblem()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(created);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/verifications",
            new
            {
                area = CatalogueVerificationArea.ProductIdentity,
                status = CatalogueVerificationStatus.Verified,
                scope = "Product identity.",
                source = "Manufacturer product page."
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var db = _fixture.CreateDbContext();
        Assert.False(await db.CatalogueSubmissionVerifications
            .AnyAsync(value => value.SubmissionId == created.Id));
    }

    [Fact]
    public async Task AddCatalogueSubmissionVerification_ForExplorer_ReturnsForbidden()
    {
        using var moderatorClient = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await moderatorClient.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(created);

        var beginResponse = await moderatorClient.PostAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/begin-verification",
            content: null);

        Assert.Equal(HttpStatusCode.OK, beginResponse.StatusCode);

        using var explorerClient = AuthenticatedClient(PostgreSqlFixture.ExplorerSubject);

        var response = await explorerClient.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/verifications",
            new
            {
                area = CatalogueVerificationArea.ProductIdentity,
                status = CatalogueVerificationStatus.Verified,
                scope = "Product identity.",
                source = "Manufacturer product page."
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCatalogueSubmissionDescriptionVisibility_AllowsModeratorDuringVerification()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(created);

        var verificationResponse = await client.PostAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/begin-verification",
            content: null);

        Assert.Equal(HttpStatusCode.OK, verificationResponse.StatusCode);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/description-visibility",
            new { visibility = CatalogueContentVisibility.ModeratorOnly });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var receipt = await response.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(receipt);
        Assert.Equal(
            CatalogueContentVisibility.ModeratorOnly,
            receipt.ProposedDescriptionVisibility);

        await using var db = _fixture.CreateDbContext();

        var submission = await db.CatalogueSubmissions
            .SingleAsync(value => value.Id == created.Id);

        Assert.Equal(
            CatalogueContentVisibility.ModeratorOnly,
            submission.ProposedDescriptionVisibility);
        Assert.Equal(
            CatalogueSubmissionStatus.InVerification,
            submission.Status);
    }

    [Fact]
    public async Task UpdateCatalogueSubmissionSpecifications_AfterVerification_ReturnsValidationProblem()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(created);

        var verificationResponse = await client.PostAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/begin-verification",
            content: null);

        Assert.Equal(HttpStatusCode.OK, verificationResponse.StatusCode);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/specifications",
            new
            {
                proposedProductType = ProductType.PullUp,
                proposedQuantityPerPack = 10
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddCatalogueSubmissionSizeVariant_WithInvalidMeasurements_ReturnsValidationProblem()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(created);

        var variantResponse = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/variants",
            new { name = "Invalid Measurement Variant" });

        Assert.Equal(HttpStatusCode.Created, variantResponse.StatusCode);

        var variant = await variantResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionVariantReceipt>();

        Assert.NotNull(variant);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/variants/{variant.Id}/sizes",
            new
            {
                manufacturerSize = "Medium",
                waistMinimumCm = 120,
                waistMaximumCm = 100
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var db = _fixture.CreateDbContext();

        Assert.False(await db.CatalogueSubmissionSizeVariants
            .AnyAsync(value => value.VariantId == variant.Id));
    }

    [Fact]
    public async Task CatalogueSubmission_CanProgressFromDraftToReadyForReview()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(created);
        Assert.Equal(
            CatalogueSubmissionStatus.Draft,
            created.Status);

        var verificationResponse = await client.PostAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/begin-verification",
            content: null);

        Assert.Equal(
            HttpStatusCode.OK,
            verificationResponse.StatusCode);

        var verifying = await verificationResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(verifying);
        Assert.Equal(
            CatalogueSubmissionStatus.InVerification,
            verifying.Status);

        var reviewResponse = await client.PostAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/ready-for-review",
            content: null);

        Assert.Equal(
            HttpStatusCode.OK,
            reviewResponse.StatusCode);

        var ready = await reviewResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();

        Assert.NotNull(ready);
        Assert.Equal(
            CatalogueSubmissionStatus.ReadyForReview,
            ready.Status);

        await using var db = _fixture.CreateDbContext();

        var submission = await db.CatalogueSubmissions
            .SingleAsync(value => value.Id == created.Id);

        Assert.Equal(
            CatalogueSubmissionStatus.ReadyForReview,
            submission.Status);
    }

    [Fact]
    public async Task PrivilegedRoleAssignments_RecordImmutableGrantAndRevocationAudits()
    {
        using var scope = _factory.Services.CreateScope();
        var assignments = scope.ServiceProvider
            .GetRequiredService<IPrivilegedRoleAssignments>();

        var actor = new AuthenticatedUser(
            _fixture.AdministratorUserId,
            PostgreSqlFixture.AdministratorSubject);

        await assignments.GrantAsync(
            actor,
            _fixture.ExplorerUserId,
            PrivilegedRole.Moderator);

        await assignments.RevokeAsync(
            actor,
            _fixture.ExplorerUserId,
            PrivilegedRole.Moderator);

        await using var db = _fixture.CreateDbContext();

        var assignment = await db.PrivilegedRoleAssignments
            .SingleAsync(value =>
                value.UserId == _fixture.ExplorerUserId &&
                value.Role == PrivilegedRole.Moderator);

        Assert.NotNull(assignment.RevokedAtUtc);
        Assert.Equal(
            _fixture.AdministratorUserId,
            assignment.RevokedByUserId);

        var actions = await db.PrivilegedRoleAssignmentAudits
            .Where(value =>
                value.SubjectUserId == _fixture.ExplorerUserId &&
                value.Role == PrivilegedRole.Moderator)
            .Select(value => value.Action)
            .ToListAsync();

        Assert.Contains(
            PrivilegedRoleAssignmentAction.Granted,
            actions);
        Assert.Contains(
            PrivilegedRoleAssignmentAction.Revoked,
            actions);
    }

    private HttpClient AuthenticatedClient(string subject)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            "X-Development-Subject",
            subject);
        return client;
    }

    private object Request(string gtin, string slug) => new
    {
        manufacturerId = _fixture.ManufacturerId,
        brandId = _fixture.BrandId,
        productName = slug.Replace('-', ' '),
        productSlug = slug,
        productType = ProductType.Tape,
        status = ProductStatus.Current,
        variantName = "Integration variant",
        backingType = BackingType.Plastic,
        manufacturerSize = "Medium",
        waistMinimumCm = 80,
        waistMaximumCm = 100,
        quantityPerPack = 10,
        packagingType = PackagingType.Bag,
        gtin,
        sourceSummary = "Primary manufacturer product sheet.",
        sourceReferences = new[]
        {
            "[https://example.test/source](https://example.test/source)"
        },
        editorialRationale = "Verified against the cited product sheet."
    };

    private object SubmissionRequest() => new
    {
        source = CatalogueSubmissionSource.Moderator,
        proposedManufacturerName = "Test Manufacturer",
        proposedBrandName = "Test Brand",
        proposedProductName = "Test Product",
        proposedVariantName = "Test Variant",
        notes = "Integration test submission."
    };


    [Fact]
    public async Task ReviewCatalogueSubmission_AcceptsReadySubmissionAndPersistsDecision()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();
        Assert.NotNull(created);

        var beginResponse = await client.PostAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/begin-verification",
            content: null);
        Assert.Equal(HttpStatusCode.OK, beginResponse.StatusCode);

        var readyResponse = await client.PostAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/ready-for-review",
            content: null);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);

        var reviewResponse = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/review",
            new
            {
                outcome = EditorialOutcome.Accepted,
                rationale = "Identity, specifications, provenance, retail destination and affiliate review are complete."
            });

        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);

        var receipt = await reviewResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionEditorialDecisionReceipt>();

        Assert.NotNull(receipt);
        Assert.Equal(created.Id, receipt.SubmissionId);
        Assert.Equal(_fixture.ModeratorUserId, receipt.ModeratorUserId);
        Assert.Equal(EditorialOutcome.Accepted, receipt.Outcome);
        Assert.Equal(
            "Identity, specifications, provenance, retail destination and affiliate review are complete.",
            receipt.Rationale);

        await using var db = _fixture.CreateDbContext();

        var submission = await db.CatalogueSubmissions
            .SingleAsync(value => value.Id == created.Id);

        Assert.Equal(CatalogueSubmissionStatus.Approved, submission.Status);

        var decision = await db.CatalogueSubmissionEditorialDecisions
            .SingleAsync(value => value.Id == receipt.Id);

        Assert.Equal(created.Id, decision.SubmissionId);
        Assert.Equal(_fixture.ModeratorUserId, decision.ModeratorUserId);
        Assert.Equal(EditorialOutcome.Accepted, decision.Outcome);
    }

    [Fact]
    public async Task ReviewCatalogueSubmission_RequestAdditionalEvidenceReturnsSubmissionToNeedsChanges()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();
        Assert.NotNull(created);

        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsync(
                $"/api/v1/catalogue-submissions/{created.Id}/begin-verification",
                content: null)).StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsync(
                $"/api/v1/catalogue-submissions/{created.Id}/ready-for-review",
                content: null)).StatusCode);

        var reviewResponse = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/review",
            new
            {
                outcome = EditorialOutcome.RequestAdditionalEvidence,
                rationale = "Please provide a stronger source for the product photography rights."
            });

        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);

        await using var db = _fixture.CreateDbContext();

        var submission = await db.CatalogueSubmissions
            .SingleAsync(value => value.Id == created.Id);

        Assert.Equal(CatalogueSubmissionStatus.NeedsChanges, submission.Status);

        var decision = await db.CatalogueSubmissionEditorialDecisions
            .SingleAsync(value => value.SubmissionId == created.Id);

        Assert.Equal(
            EditorialOutcome.RequestAdditionalEvidence,
            decision.Outcome);
    }

    [Fact]
    public async Task ReviewCatalogueSubmission_ForExplorerWithoutModeratorAuthority_ReturnsForbidden()
    {
        using var moderatorClient = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await moderatorClient.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionReceipt>();
        Assert.NotNull(created);

        Assert.Equal(
            HttpStatusCode.OK,
            (await moderatorClient.PostAsync(
                $"/api/v1/catalogue-submissions/{created.Id}/begin-verification",
                content: null)).StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            (await moderatorClient.PostAsync(
                $"/api/v1/catalogue-submissions/{created.Id}/ready-for-review",
                content: null)).StatusCode);

        using var explorerClient = AuthenticatedClient(PostgreSqlFixture.ExplorerSubject);

        var response = await explorerClient.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/review",
            new
            {
                outcome = EditorialOutcome.Accepted,
                rationale = "Should not be allowed."
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }


    [Fact]
    public async Task ResolveCatalogueSubmissionEntities_CanMatchExistingOrCreateNewCanonicalEntities()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var existingResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            new
            {
                source = CatalogueSubmissionSource.Moderator,
                proposedManufacturerName = "Proposed Manufacturer",
                proposedBrandName = "Proposed Brand",
                proposedProductName = "Entity Resolution Existing Test",
                proposedVariantName = (string?)null,
                notes = "Entity resolution integration test."
            });

        Assert.Equal(HttpStatusCode.Created, existingResponse.StatusCode);

        var existingSubmission =
            await existingResponse.Content.ReadFromJsonAsync<CatalogueSubmissionReceipt>();
        Assert.NotNull(existingSubmission);

        var matchResponse = await client.PutAsJsonAsync(
            $"/api/v1/catalogue-submissions/{existingSubmission.Id}/entity-resolution",
            new
            {
                manufacturerId = _fixture.ManufacturerId,
                brandId = _fixture.BrandId,
                newManufacturerName = (string?)null,
                newBrandName = (string?)null
            });

        Assert.Equal(HttpStatusCode.OK, matchResponse.StatusCode);

        var matchedReceipt =
            await matchResponse.Content.ReadFromJsonAsync<CatalogueSubmissionReceipt>();
        Assert.NotNull(matchedReceipt);
        Assert.Equal("Integration Test Manufacturer", matchedReceipt.ProposedManufacturerName);
        Assert.Equal("Integration Test Brand", matchedReceipt.ProposedBrandName);

        var newManufacturerName = $"Resolution Manufacturer {Guid.NewGuid():N}";
        var newBrandName = $"Resolution Brand {Guid.NewGuid():N}";

        var newResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            new
            {
                source = CatalogueSubmissionSource.Moderator,
                proposedManufacturerName = "Another Proposed Manufacturer",
                proposedBrandName = "Another Proposed Brand",
                proposedProductName = "Entity Resolution New Test",
                proposedVariantName = (string?)null,
                notes = "Entity resolution new entity integration test."
            });

        Assert.Equal(HttpStatusCode.Created, newResponse.StatusCode);

        var newSubmission =
            await newResponse.Content.ReadFromJsonAsync<CatalogueSubmissionReceipt>();
        Assert.NotNull(newSubmission);

        var createResponse = await client.PutAsJsonAsync(
            $"/api/v1/catalogue-submissions/{newSubmission.Id}/entity-resolution",
            new
            {
                manufacturerId = (Guid?)null,
                brandId = (Guid?)null,
                newManufacturerName,
                newBrandName
            });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var createdReceipt =
            await createResponse.Content.ReadFromJsonAsync<CatalogueSubmissionReceipt>();
        Assert.NotNull(createdReceipt);
        Assert.Equal(newManufacturerName, createdReceipt.ProposedManufacturerName);
        Assert.Equal(newBrandName, createdReceipt.ProposedBrandName);

        await using var db = _fixture.CreateDbContext();

        var manufacturer = await db.Manufacturers
            .SingleAsync(value => value.Name == newManufacturerName);
        var brand = await db.Brands
            .SingleAsync(value => value.Name == newBrandName);

        Assert.Equal(manufacturer.Id, brand.ManufacturerId);
    }

    [Fact]
    public async Task PublishCatalogueSubmission_ForApprovedFullyVerifiedSubmission_CreatesCanonicalProduct()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            new
            {
                source = CatalogueSubmissionSource.Moderator,
                proposedManufacturerName = "Integration Test Manufacturer",
                proposedBrandName = "Integration Test Brand",
                proposedProductName = "Published Integration Test Product",
                proposedVariantName = "Integration Published Variant",
                notes = "Publication integration test."
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CatalogueSubmissionReceipt>();
        Assert.NotNull(created);

        var secondVariantResponse = await client.PostAsJsonAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/variants",
            new { name = "Second Variant" });
        Assert.Equal(HttpStatusCode.Created, secondVariantResponse.StatusCode);

        var secondVariant = await secondVariantResponse.Content
            .ReadFromJsonAsync<CatalogueSubmissionVariantReceipt>();
        Assert.NotNull(secondVariant);

        var variantsResponse = await client.GetAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/variants");
        Assert.Equal(HttpStatusCode.OK, variantsResponse.StatusCode);

        var submissionVariants = await variantsResponse.Content
            .ReadFromJsonAsync<List<CatalogueSubmissionVariantReceipt>>();
        Assert.NotNull(submissionVariants);

        var firstVariant = Assert.Single(
            submissionVariants,
            value => value.Name == "Integration Published Variant");

        Assert.Equal(
            HttpStatusCode.Created,
            (await client.PostAsJsonAsync(
                $"/api/v1/catalogue-submissions/{created.Id}/variants/{firstVariant.Id}/sizes",
                new
                {
                    manufacturerSize = "Medium",
                    waistMinimumCm = 80,
                    waistMaximumCm = 100,
                    hipMinimumCm = 90,
                    hipMaximumCm = 110,
                    manufacturerStatedAbsorbencyMl = 2500,
                    lengthMm = 850,
                    widthMm = 700,
                    weightGrams = 120,
                    manufacturerPackQuantity = 10,
                    gtin = "99000001"
                })).StatusCode);

        Assert.Equal(
            HttpStatusCode.Created,
            (await client.PostAsJsonAsync(
                $"/api/v1/catalogue-submissions/{created.Id}/variants/{secondVariant!.Id}/sizes",
                new
                {
                    manufacturerSize = "Large",
                    waistMinimumCm = 100,
                    waistMaximumCm = 120,
                    hipMinimumCm = 110,
                    hipMaximumCm = 130,
                    manufacturerStatedAbsorbencyMl = 3000,
                    lengthMm = 900,
                    widthMm = 750,
                    weightGrams = 135,
                    manufacturerPackQuantity = 8,
                    gtin = "99000002"
                })).StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PutAsJsonAsync(
                $"/api/v1/catalogue-submissions/{created.Id}/specifications",
                new
                {
                    proposedProductType = ProductType.Tape,
                    proposedManufacturerSize = "Medium",
                    proposedWaistMinimumCm = 80,
                    proposedWaistMaximumCm = 100,
                    proposedBackingType = BackingType.Plastic,
                    proposedFastenerType = FastenerType.AdhesiveTape,
                    proposedWaistbandStyle = WaistbandStyle.FrontAndRearElastic,
                    proposedFragranceType = FragranceType.None,
                    proposedQuantityPerPack = 10,
                    proposedPackagingType = PackagingType.Bag,
                    proposedProductFamily = "Integration Test Family",
                    proposedDescription = "Published integration test description.",
                    proposedOfficialWebsiteUrl = "https://example.test/product"
                })).StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsync(
                $"/api/v1/catalogue-submissions/{created.Id}/begin-verification",
                content: null)).StatusCode);

        foreach (var area in new[]
        {
            CatalogueVerificationArea.ProductIdentity,
            CatalogueVerificationArea.Specifications
        })
        {
            var verificationResponse = await client.PostAsJsonAsync(
                $"/api/v1/catalogue-submissions/{created.Id}/verifications",
                new
                {
                    area,
                    status = CatalogueVerificationStatus.Verified,
                    scope = "Publication integration test",
                    source = "Integration test source",
                    sourceUrl = "https://example.test/source"
                });
            Assert.Equal(HttpStatusCode.Created, verificationResponse.StatusCode);
        }

        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsync(
                $"/api/v1/catalogue-submissions/{created.Id}/ready-for-review",
                content: null)).StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsJsonAsync(
                $"/api/v1/catalogue-submissions/{created.Id}/review",
                new
                {
                    outcome = EditorialOutcome.Accepted,
                    rationale = "Publication integration test approval."
                })).StatusCode);

        var publishResponse = await client.PostAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/publish",
            content: null);

        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);

        var receipt = await publishResponse.Content.ReadFromJsonAsync<CataloguePublicationReceipt>();
        Assert.NotNull(receipt);
        Assert.Equal(created.Id, receipt.SubmissionId);
        Assert.NotEqual(Guid.Empty, receipt.ProductId);
        Assert.NotEqual(Guid.Empty, receipt.ProductVariantId);
        Assert.NotEqual(Guid.Empty, receipt.SizeVariantId);
        Assert.NotEqual(Guid.Empty, receipt.PackTypeId);
        Assert.NotEqual(Guid.Empty, receipt.AuditRecordId);
        Assert.Equal("99000001", receipt.Gtin);

        await using var db = _fixture.CreateDbContext();

        var submission = await db.CatalogueSubmissions.SingleAsync(value => value.Id == created.Id);
        Assert.Equal(CatalogueSubmissionStatus.Published, submission.Status);
        Assert.Equal(receipt.ProductId, submission.PublishedProductId);

        var product = await db.Products.SingleAsync(value => value.Id == receipt.ProductId);
        Assert.Equal("Published Integration Test Product", product.Name);

        var publishedVariants = await db.ProductVariants
            .Where(value => value.ProductId == product.Id)
            .OrderBy(value => value.Name)
            .ToListAsync();

        Assert.Equal(2, publishedVariants.Count);
        Assert.Contains(publishedVariants, value => value.Name == "Integration Published Variant");
        Assert.Contains(publishedVariants, value => value.Name == "Second Variant");
        Assert.Equal(_fixture.ManufacturerId, product.ManufacturerId);
        Assert.Equal(_fixture.BrandId, product.BrandId);
        Assert.Equal(ProductType.Tape, product.ProductType);
        Assert.Equal("Integration Test Family", product.Family);
        Assert.Equal("Published integration test description.", product.Description);
        Assert.Equal("https://example.test/product", product.OfficialWebsiteUrl);

        var publishedSizes = await db.SizeVariants
            .Where(value => db.ProductVariants.Any(variant =>
                variant.Id == value.ProductVariantId &&
                variant.ProductId == product.Id))
            .OrderBy(value => value.ManufacturerSize)
            .ToListAsync();

        Assert.Equal(2, publishedSizes.Count);

        var publishedFirstVariantSize = Assert.Single(
            publishedSizes,
            value => value.ProductVariantId == publishedVariants.Single(
                variant => variant.Name == "Integration Published Variant").Id);
        Assert.Equal("Medium", publishedFirstVariantSize.ManufacturerSize);
        Assert.Equal(80, publishedFirstVariantSize.WaistMinimumCm);
        Assert.Equal(100, publishedFirstVariantSize.WaistMaximumCm);
        Assert.Equal(90, publishedFirstVariantSize.HipMinimumCm);
        Assert.Equal(110, publishedFirstVariantSize.HipMaximumCm);
        Assert.Equal(2500, publishedFirstVariantSize.ManufacturerStatedAbsorbencyMl);
        Assert.Equal(850, publishedFirstVariantSize.LengthMm);
        Assert.Equal(700, publishedFirstVariantSize.WidthMm);
        Assert.Equal(120, publishedFirstVariantSize.WeightGrams);

        var publishedSecondVariantSize = Assert.Single(
            publishedSizes,
            value => value.ProductVariantId == publishedVariants.Single(
                variant => variant.Name == "Second Variant").Id);
        Assert.Equal("Large", publishedSecondVariantSize.ManufacturerSize);
        Assert.Equal(100, publishedSecondVariantSize.WaistMinimumCm);
        Assert.Equal(120, publishedSecondVariantSize.WaistMaximumCm);
        Assert.Equal(110, publishedSecondVariantSize.HipMinimumCm);
        Assert.Equal(130, publishedSecondVariantSize.HipMaximumCm);
        Assert.Equal(3000, publishedSecondVariantSize.ManufacturerStatedAbsorbencyMl);
        Assert.Equal(900, publishedSecondVariantSize.LengthMm);
        Assert.Equal(750, publishedSecondVariantSize.WidthMm);
        Assert.Equal(135, publishedSecondVariantSize.WeightGrams);

        var publishedPackQuantities = await db.PackTypes
            .Where(pack => publishedSizes.Select(size => size.Id).Contains(pack.SizeVariantId))
            .OrderBy(pack => pack.QuantityPerPack)
            .Select(pack => pack.QuantityPerPack)
            .ToListAsync();

        Assert.Equal([8, 10], publishedPackQuantities);

        var publishedGtins = await db.ProductIdentifiers
            .Where(identifier => db.PackTypes.Any(pack =>
                pack.Id == identifier.PackTypeId &&
                publishedSizes.Select(size => size.Id).Contains(pack.SizeVariantId)))
            .Select(identifier => identifier.Value)
            .OrderBy(value => value)
            .ToListAsync();

        Assert.Equal(["99000001", "99000002"], publishedGtins);

        Assert.True(await db.ProductVariants.AnyAsync(value => value.Id == receipt.ProductVariantId && value.ProductId == product.Id));
        Assert.True(await db.SizeVariants.AnyAsync(value => value.Id == receipt.SizeVariantId && value.ProductVariantId == receipt.ProductVariantId));
        Assert.True(await db.PackTypes.AnyAsync(value => value.Id == receipt.PackTypeId && value.SizeVariantId == receipt.SizeVariantId));
        Assert.True(await db.CatalogueAuditRecords.AnyAsync(value => value.Id == receipt.AuditRecordId && value.ProductId == product.Id));
    }

    [Fact]
    public async Task PublishCatalogueSubmission_BeforeApproval_ReturnsValidationProblem()
    {
        using var client = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            SubmissionRequest());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CatalogueSubmissionReceipt>();
        Assert.NotNull(created);

        var response = await client.PostAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/publish",
            content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PublishCatalogueSubmission_ForExplorerWithoutModeratorAuthority_ReturnsForbidden()
    {
        using var moderatorClient = AuthenticatedClient(PostgreSqlFixture.ModeratorSubject);

        var createResponse = await moderatorClient.PostAsJsonAsync(
            "/api/v1/catalogue-submissions",
            new
            {
                source = CatalogueSubmissionSource.Moderator,
                proposedManufacturerName = "Integration Test Manufacturer",
                proposedBrandName = "Integration Test Brand",
                proposedProductName = "Explorer Publication Test Product",
                proposedVariantName = "Integration Variant"
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CatalogueSubmissionReceipt>();
        Assert.NotNull(created);

        using var explorerClient = AuthenticatedClient(PostgreSqlFixture.ExplorerSubject);

        var response = await explorerClient.PostAsync(
            $"/api/v1/catalogue-submissions/{created.Id}/publish",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SearchCatalogueProducts_ReturnsPublishedProductsWithoutAuthentication()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var catalogue = await response.Content.ReadFromJsonAsync<CatalogueProductSearch>();
        Assert.NotNull(catalogue);
        Assert.Contains(catalogue.Products, product => product.Name == "Integration Test Product");
        Assert.Contains(catalogue.Facets, facet => facet.Key == "manufacturer");
        Assert.Contains(catalogue.Facets, facet => facet.Key == "productType");
    }


    [Fact]
    public async Task SearchCatalogueProducts_AppliesInterdependentFiltersAndReturnsRetailDestinations()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/v1/products?manufacturer={_fixture.ManufacturerId}&productType=Tape");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var catalogue = await response.Content.ReadFromJsonAsync<CatalogueProductSearch>();
        Assert.NotNull(catalogue);
        var product = Assert.Single(catalogue.Products, value => value.Name == "Integration Test Product");
        Assert.Contains("Integration Test Size", product.Sizes);
        var destination = Assert.Single(product.RetailDestinations);
        Assert.Equal("Integration Test Retailer", destination.RetailerName);
        Assert.Equal("https://shop.example.test/published-product", destination.ListingUrl);
    }

    [Fact]
    public async Task GetCatalogueProduct_ReturnsPublishedProductDetailsWithoutAuthentication()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/products/integration-test-product");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var product = await response.Content.ReadFromJsonAsync<CatalogueProductDetails>();
        Assert.NotNull(product);
        Assert.Equal("Integration Test Product", product.Name);
        Assert.NotEmpty(product.Variants);
    }

    [Fact]
    public async Task GetCatalogueProduct_ForUnknownSlug_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/products/does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();
}