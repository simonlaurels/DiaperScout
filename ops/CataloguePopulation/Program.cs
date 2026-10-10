using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure;
using DiaperScout.CataloguePopulation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

if (!((args.Length == 1 && args[0] == "inventory") || (args.Length == 3 && args[0] == "publish") ||
      (args.Length == 2 && args[0] == "inspect-draft" && Guid.TryParse(args[1], out _)) ||
      (args.Length == 3 && args[0] == "preview-manufacturer") ||
      (args.Length == 4 && args[0] == "reconcile-manufacturer") ||
      (args.Length == 3 && args[0] == "preview-grouping") ||
      (args.Length == 4 && args[0] == "reconcile-grouping")))
    throw new ArgumentException("Specify inventory, or publish with an explicit batch file and SHA-256.");
try
{
    var connection = Environment.GetEnvironmentVariable("ConnectionStrings__diaperscout")
        ?? throw new InvalidOperationException("Database configuration is required.");
    if (args[0] is "preview-grouping" or "reconcile-grouping")
    {
        var bytes = await File.ReadAllBytesAsync(args[1]);
        if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(args[2], StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Product grouping plan hash mismatch.");
        var plan = JsonSerializer.Deserialize<ProductGroupingPlan>(bytes, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Product grouping plan is empty.");
        await using var database = new DiaperScoutDbContext(new DbContextOptionsBuilder<DiaperScoutDbContext>().UseNpgsql(connection).Options);
        var operation = new ProductGroupingReconciliation(database);
        if (args[0] == "preview-grouping")
            Console.WriteLine("CATALOGUE_GROUPING_PREVIEW:" + JsonSerializer.Serialize(await operation.PreviewAsync(plan)));
        else
        {
            var administrators = await (from u in database.Users join r in database.PrivilegedRoleAssignments on u.Id equals r.UserId
                where u.Status == UserAccountStatus.Active && r.Role == PrivilegedRole.Administrator && r.RevokedAtUtc == null select u).Distinct().ToListAsync();
            if (administrators.Count != 1) throw new InvalidOperationException("A single active administrator is required.");
            Console.WriteLine("CATALOGUE_GROUPING_RECONCILIATION:" + JsonSerializer.Serialize(await operation.ApplyAsync(
                new(administrators[0].Id, administrators[0].Subject), plan, args[3])));
        }
        return;
    }
    if (args[0] is "preview-manufacturer" or "reconcile-manufacturer")
    {
        var bytes = await File.ReadAllBytesAsync(args[1]);
        if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(args[2], StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Manufacturer plan hash mismatch.");
        var plan = JsonSerializer.Deserialize<ManufacturerReconciliationPlan>(bytes,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Manufacturer plan is empty.");
        await using var database = new DiaperScoutDbContext(
            new DbContextOptionsBuilder<DiaperScoutDbContext>().UseNpgsql(connection).Options);
        var operation = new ManufacturerReconciliation(database);
        if (args[0] == "preview-manufacturer")
            Console.WriteLine("CATALOGUE_MANUFACTURER_PREVIEW:" + JsonSerializer.Serialize(await operation.PreviewAsync(plan)));
        else
        {
            var administrators = await (from user in database.Users join role in database.PrivilegedRoleAssignments
                on user.Id equals role.UserId where user.Status == UserAccountStatus.Active &&
                role.Role == PrivilegedRole.Administrator && role.RevokedAtUtc == null select user).Distinct().ToListAsync();
            if (administrators.Count != 1) throw new InvalidOperationException("A single active administrator is required.");
            Console.WriteLine("CATALOGUE_MANUFACTURER_RECONCILIATION:" + JsonSerializer.Serialize(await operation.ApplyAsync(
                new AuthenticatedUser(administrators[0].Id, administrators[0].Subject), plan, args[3])));
        }
        return;
    }
    if (args[0] == "publish")
    {
        var bytes = await File.ReadAllBytesAsync(args[1]);
        if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(args[2], StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Publication batch hash mismatch.");
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        var batch = JsonSerializer.Deserialize<PopulationPack[]>(bytes, jsonOptions)
            ?? throw new InvalidOperationException("Publication batch is empty.");
        if (batch.Length == 0 || batch.Select(p => p.ResearchId).Distinct().Count() != batch.Length)
            throw new InvalidOperationException("Publication batch IDs must be unique and nonempty.");
        foreach (var pack in batch) PopulationPublisher.Validate(pack);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:diaperscout"] = connection,
            ["Editorial:CatalogueWritesEnabled"] = "true"
        }).Build();
        await using var services = new ServiceCollection().AddLogging()
            .AddSingleton<IConfiguration>(configuration).AddDiaperScoutInfrastructure(configuration).BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();
        // Attribute the authorized batch to the existing administrator; never grant roles or create users.
        var administrators = await (from user in database.Users
            join role in database.PrivilegedRoleAssignments on user.Id equals role.UserId
            where user.Status == UserAccountStatus.Active && role.Role == PrivilegedRole.Administrator && role.RevokedAtUtc == null
            select user).Distinct().ToListAsync();
        Console.WriteLine("CATALOGUE_STAGE:ActiveAdministratorCount=" + administrators.Count);
        if (administrators.Count != 1) throw new InvalidOperationException("A single existing active administrator is required.");
        var actor = new AuthenticatedUser(administrators[0].Id, administrators[0].Subject);
        var publisher = new PopulationPublisher(database, scope.ServiceProvider.GetRequiredService<ICatalogueSubmissions>(),
            scope.ServiceProvider.GetRequiredService<IEditorialAuthorisation>(), scope.ServiceProvider.GetRequiredService<ICanonicalCatalogue>());
        foreach (var pack in batch)
        {
            Console.WriteLine("CATALOGUE_STAGE:Publishing=" + pack.ResearchId);
            try
            {
                Console.WriteLine("CATALOGUE_PUBLICATION:" + JsonSerializer.Serialize(await publisher.PublishAsync(actor, pack)));
            }
            catch (InvalidOperationException exception) when (exception.StackTrace?.Contains("DiaperScout.CataloguePopulation") == true)
            {
                Console.WriteLine("CATALOGUE_HELD:" + JsonSerializer.Serialize(new {pack.ResearchId, reason = exception.Message}));
                database.ChangeTracker.Clear();
            }
        }
        return;
    }
    await using var db = new DiaperScoutDbContext(
        new DbContextOptionsBuilder<DiaperScoutDbContext>().UseNpgsql(connection).Options);
    await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
    await db.Database.ExecuteSqlRawAsync("SET TRANSACTION READ ONLY");
    if (args[0] == "inspect-draft")
    {
        var id = Guid.Parse(args[1]);
        var proposal = await db.CatalogueSubmissions.AsNoTracking().Where(s => s.Id == id)
            .Select(s => new { s.ProposedManufacturerName, s.ProposedBrandName, s.ProposedProductName, s.Notes, s.IdentitySourceUrl, s.ProposedOfficialWebsiteUrl }).SingleAsync();
        var variants = await db.CatalogueSubmissionVariants.AsNoTracking().Where(v => v.SubmissionId == id).ToListAsync();
        var variantIds = variants.Select(v => v.Id).ToArray();
        var attributes = await db.CatalogueSubmissionVariantOverrides.AsNoTracking()
            .Where(v => variantIds.Contains(v.VariantId)).ToListAsync();
        Console.WriteLine("CATALOGUE_DRAFT_ATTRIBUTES:" + JsonSerializer.Serialize(new {submissionId=id,proposal,variants,attributes}));
        await transaction.RollbackAsync();
        return;
    }
    var result = new
    {
        schema = "catalogue-inventory-v1", readOnly = true, observedAtUtc = DateTimeOffset.UtcNow,
        appliedMigrations = await db.Database.GetAppliedMigrationsAsync(),
        pendingMigrations = await db.Database.GetPendingMigrationsAsync(),
        productRouteRedirects = await db.ProductRouteRedirects.AsNoTracking().Select(r => new {r.Id,r.SourceProductId,r.TargetProductId,r.DefaultVariantId}).ToListAsync(),
        manufacturers = await db.Manufacturers.AsNoTracking().Select(m => new {m.Id,m.Name,m.Slug}).ToListAsync(),
        brands = await db.Brands.AsNoTracking().Select(b => new {b.Id,b.ManufacturerId,b.Name,b.Slug}).ToListAsync(),
        products = await db.Products.AsNoTracking().Select(p => new {p.Id,p.ManufacturerId,p.BrandId,p.Name,p.Slug,p.Status,p.Family}).ToListAsync(),
        variants = await db.ProductVariants.AsNoTracking().Select(v => new {v.Id,v.ProductId,v.Name}).ToListAsync(),
        sizes = await db.SizeVariants.AsNoTracking().Select(s => new {s.Id,s.ProductVariantId,s.ManufacturerSize,s.WaistMinimumCm,s.WaistMaximumCm,s.HipMinimumCm,s.HipMaximumCm,s.FitMeasurementBasis}).ToListAsync(),
        packs = await db.PackTypes.AsNoTracking().Select(p => new {p.Id,p.SizeVariantId,p.QuantityPerPack,p.PackagingType,p.CaseQuantity}).ToListAsync(),
        identifiers = await db.ProductIdentifiers.AsNoTracking().Select(i => new {i.Id,i.PackTypeId,i.Type,i.Value}).ToListAsync(),
        submissions = await db.CatalogueSubmissions.AsNoTracking().Select(s => new {s.Id,s.Status,s.ProposedManufacturerName,s.ProposedBrandName,s.ProposedProductName,s.ProposedGtin,s.PublishedProductId}).ToListAsync(),
        submissionVariants = await db.CatalogueSubmissionVariants.AsNoTracking().Select(v => new {v.Id,v.SubmissionId,v.Name}).ToListAsync(),
        submissionSizes = await db.CatalogueSubmissionSizeVariants.AsNoTracking().Select(s => new {s.Id,s.VariantId,s.ManufacturerSize,s.ManufacturerPackQuantity,s.Gtin}).ToListAsync()
    };
    await transaction.RollbackAsync();
    Console.WriteLine("CATALOGUE_INVENTORY:" + JsonSerializer.Serialize(result));
}
catch (Exception exception)
{
    // Connection strings, provider diagnostics and user identities never enter research logs.
    Console.Error.WriteLine("Catalogue operation failed: " + exception.GetType().Name);
    if (exception is InvalidOperationException && exception.StackTrace?.Contains("DiaperScout.CataloguePopulation") == true)
        Console.Error.WriteLine("CATALOGUE_SAFETY_CHECK:" + exception.Message);
    Environment.ExitCode = 1;
}
