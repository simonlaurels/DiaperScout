using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:18.3")
        .WithDatabase("diaperscout_integration")
        .WithUsername("postgres")
        .WithPassword("diaperscout-integration-tests")
        .Build();

    public const string ExplorerSubject = "integration-explorer";
    public const string ModeratorSubject = "integration-moderator";
    public const string AdministratorSubject = "integration-administrator";
    public string ConnectionString => _container.GetConnectionString();
    public Guid ExplorerUserId { get; private set; }
    public Guid ModeratorUserId { get; private set; }
    public Guid AdministratorUserId { get; private set; }
    public Guid ManufacturerId { get; private set; }
    public Guid BrandId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid LocationId { get; private set; }
    public Guid RetailerId { get; private set; }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();

        var manufacturer = new Manufacturer("Integration Test Manufacturer", "integration-test-manufacturer");
        var brand = new Brand(manufacturer.Id, "Integration Test Brand", "integration-test-brand");
        var product = new Product(manufacturer.Id, brand.Id, "Integration Test Product", "integration-test-product", ProductType.Tape);
        var productVariant = new ProductVariant(product.Id, "Integration Test Variant");
        var sizeVariant = new SizeVariant(productVariant.Id, "Integration Test Size");
        var packType = new PackType(sizeVariant.Id, 12, PackagingType.Bag);
        var identifier = new ProductIdentifier(packType.Id, IdentifierType.Gtin, "12345678");
        var country = new Country("ZZ", "Integration Test Country");
        var retailer = new Retailer("Integration Test Retailer", "integration-test-retailer", "https://retailer.example.test");
        var location = new Location(retailer.Id, country.Id, "Integration Test Location", "1 Test Street", "Testville", "ZZ1 1ZZ");
        var user = new User(ExplorerSubject);
        var explorer = new ExplorerProfile(user.Id, "Integration Explorer");
        var moderator = new User(ModeratorSubject);
        var administrator = new User(AdministratorSubject);
        var moderatorAssignment = new PrivilegedRoleAssignment(moderator.Id, PrivilegedRole.Moderator, moderator.Id, DateTimeOffset.UtcNow);
        var administratorAssignment = new PrivilegedRoleAssignment(administrator.Id, PrivilegedRole.Administrator, administrator.Id, DateTimeOffset.UtcNow);

        var publishedSubmission = new CatalogueSubmission(
            CatalogueSubmissionSource.Moderator,
            moderator.Id,
            "Integration Test Manufacturer",
            "Integration Test Product",
            "Integration Test Variant",
            "Integration Test Brand",
            "Published fixture product.");
        publishedSubmission.BeginVerification();
        publishedSubmission.MarkReadyForReview();
        publishedSubmission.Approve();
        publishedSubmission.Publish(product.Id);

        var retailDestination = new CatalogueSubmissionRetailDestination(
            publishedSubmission.Id,
            retailer.Id,
            "https://shop.example.test/published-product",
            "Published fixture retailer destination.");

        db.AddRange(manufacturer, brand, product, productVariant, sizeVariant, packType, identifier,
            country, retailer, location, user, explorer, moderator, administrator, moderatorAssignment, administratorAssignment,
            publishedSubmission, retailDestination,
            new PrivilegedRoleAssignmentAudit(moderator.Id, moderator.Id, PrivilegedRole.Moderator, PrivilegedRoleAssignmentAction.Granted, DateTimeOffset.UtcNow),
            new PrivilegedRoleAssignmentAudit(administrator.Id, administrator.Id, PrivilegedRole.Administrator, PrivilegedRoleAssignmentAction.Granted, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var savedSubmission = await db.CatalogueSubmissions
            .AsNoTracking()
            .SingleAsync(x => x.Id == publishedSubmission.Id);

        var savedDestination = await db.CatalogueSubmissionRetailDestinations
            .AsNoTracking()
            .SingleAsync(x => x.Id == retailDestination.Id);

        Console.WriteLine(
            $"FIXTURE CHECK: Submission={savedSubmission.Status}, PublishedProductId={savedSubmission.PublishedProductId}, " +
            $"DestinationSubmissionId={savedDestination.SubmissionId}, RetailerId={savedDestination.RetailerId}");

        // The existing EF relationship uses Backpack.UserId as the dependent key
        // for ExplorerProfile's primary key. Persist the active profile first,
        // then use its key without changing the established production mapping.
        db.Backpacks.Add(new Backpack(explorer.Id));
        await db.SaveChangesAsync();

        ExplorerUserId = user.Id;
        ModeratorUserId = moderator.Id;
        AdministratorUserId = administrator.Id;
        ManufacturerId = manufacturer.Id;
        BrandId = brand.Id;
        ProductId = product.Id;
        LocationId = location.Id;
        RetailerId = retailer.Id;
    }

    public DiaperScoutDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DiaperScoutDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new DiaperScoutDbContext(options);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
