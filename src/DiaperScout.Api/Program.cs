using DiaperScout.Infrastructure;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDiaperScoutInfrastructure(builder.Configuration);

if (builder.Environment.IsDevelopment() &&
    builder.Configuration.GetValue<bool>("Authentication:Development:Enabled"))
{
    builder.Services.AddAuthentication("DevelopmentHeader")
        .AddScheme<
            AuthenticationSchemeOptions,
            DevelopmentHeaderAuthenticationHandler>(
            "DevelopmentHeader",
            _ => { });
}
else
{
    builder.Services.AddAuthentication();
}

builder.Services
    .AddAuthorizationBuilder()
    .AddPolicy(
        "Explorer",
        policy => policy.RequireAuthenticatedUser());

builder.Services.AddScoped<IAuthorizationHandler, PublishAtlasAuthorizationHandler>();

builder.Services
    .AddAuthorizationBuilder()
    .AddPolicy(
        "PublishAtlas",
        policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new PublishAtlasRequirement()));

var app = builder.Build();

if (app.Environment.IsDevelopment() &&
    builder.Configuration.GetValue<bool>("DevelopmentCatalogue:Enabled"))
{
    await app.Services.PopulateDevelopmentCatalogueAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet(
    "/health",
    () => Results.Ok(new { status = "healthy" }))
    .WithName("Health");

app.MapPost(
    "/api/v1/retailer-management/{retailerId:guid}/identity-verification",
    async (
        Guid retailerId,
        RetailerIdentityVerificationRequest request,
        ICurrentUser currentUser,
        IRetailerManagement retailerManagement,
        CancellationToken cancellationToken) =>
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
            return Results.Forbid();

        try
        {
            return Results.Ok(await retailerManagement.VerifyIdentityAsync(actor, retailerId, request, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (CatalogueValidationException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { [exception.Field] = [exception.Message] });
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { [exception.ParamName ?? "identity"] = [exception.Message] });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    })
    .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
    .WithName("VerifyRetailerIdentity")
    .WithTags("Retailer Management")
    .Produces<RetailerIdentityVerificationItem>()
    .ProducesValidationProblem();

app.MapGet(
    "/api/v1/retailer-management/{retailerId:guid}/identity-verifications",
    async (
        Guid retailerId,
        ICurrentUser currentUser,
        IRetailerManagement retailerManagement,
        CancellationToken cancellationToken) =>
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
            return Results.Forbid();

        try
        {
            return Results.Ok(await retailerManagement.GetIdentityVerificationsAsync(actor, retailerId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    })
    .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
    .WithName("GetRetailerIdentityVerifications")
    .WithTags("Retailer Management")
    .Produces<IReadOnlyList<RetailerIdentityVerificationItem>>();

app.MapGet(
    "/api/v1/retailer-management/{retailerId:guid}/affiliate-programmes",
    async (
        Guid retailerId,
        ICurrentUser currentUser,
        IRetailerManagement retailerManagement,
        CancellationToken cancellationToken) =>
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
            return Results.Forbid();

        try
        {
            return Results.Ok(
                await retailerManagement.GetAffiliateProgrammesAsync(
                    actor,
                    retailerId,
                    cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    })
    .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
    .WithName("GetRetailerAffiliateProgrammes")
    .WithTags("Retailer Management")
    .Produces<IReadOnlyList<RetailerAffiliateProgrammeItem>>();

app.MapPost(
    "/api/v1/retailer-management/{retailerId:guid}/affiliate-programmes",
    async (
        Guid retailerId,
        RetailerAffiliateProgrammeDiscoveryRequest request,
        ICurrentUser currentUser,
        IRetailerManagement retailerManagement,
        CancellationToken cancellationToken) =>
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
            return Results.Forbid();

        try
        {
            return Results.Ok(
                await retailerManagement.RecordAffiliateProgrammeDiscoveryAsync(
                    actor,
                    retailerId,
                    request,
                    cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (CatalogueValidationException exception)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [exception.Field] = [exception.Message]
                });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    })
    .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
    .WithName("RecordRetailerAffiliateProgramme")
    .WithTags("Retailer Management")
    .Produces<RetailerAffiliateProgrammeItem>()
    .ProducesValidationProblem();

app.MapPut(
    "/api/v1/retailer-management/{retailerId:guid}/affiliate-programmes/{programmeId:guid}/status",
    async (
        Guid retailerId,
        Guid programmeId,
        RetailerAffiliateProgrammeStatusUpdateRequest request,
        ICurrentUser currentUser,
        IRetailerManagement retailerManagement,
        CancellationToken cancellationToken) =>
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
            return Results.Forbid();

        try
        {
            return Results.Ok(
                await retailerManagement.UpdateAffiliateProgrammeStatusAsync(
                    actor,
                    retailerId,
                    programmeId,
                    request,
                    cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (CatalogueValidationException exception)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [exception.Field] = [exception.Message]
                });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    })
    .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
    .WithName("UpdateRetailerAffiliateProgrammeStatus")
    .WithTags("Retailer Management")
    .Produces<RetailerAffiliateProgrammeItem>()
    .ProducesValidationProblem();

app.MapPost(
    "/api/v1/retailer-management/{retailerId:guid}/affiliate-programmes/{programmeId:guid}/select",
    async (
        Guid retailerId,
        Guid programmeId,
        ICurrentUser currentUser,
        IRetailerManagement retailerManagement,
        CancellationToken cancellationToken) =>
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
            return Results.Forbid();

        try
        {
            return Results.Ok(
                await retailerManagement.SelectAffiliateProgrammeAsync(
                    actor,
                    retailerId,
                    programmeId,
                    cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (CatalogueValidationException exception)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [exception.Field] = [exception.Message]
                });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    })
    .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
    .WithName("SelectRetailerAffiliateProgramme")
    .WithTags("Retailer Management")
    .Produces<RetailerAffiliateProgrammeItem>()
    .ProducesValidationProblem();

app.MapGet(
    "/api/v1/retailer-discovery/{gtin}",
    async (
        string gtin,
        ICurrentUser currentUser,
        IRetailerDiscovery retailerDiscovery,
        CancellationToken cancellationToken) =>
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
            return Results.Forbid();

        try
        {
            return Results.Ok(await retailerDiscovery.GetForGtinAsync(gtin, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (CatalogueValidationException exception)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [exception.Field] = [exception.Message]
                });
        }
    })
    .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
    .WithName("GetRetailerDiscoveryByGtin")
    .WithTags("Retailer Discovery")
    .Produces<IReadOnlyList<RetailerProductListingItem>>()
    .ProducesValidationProblem();

app.MapPost(
    "/api/v1/retailer-discovery",
    async (
        RetailerDiscoveryResult request,
        ICurrentUser currentUser,
        IRetailerDiscovery retailerDiscovery,
        CancellationToken cancellationToken) =>
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
            return Results.Forbid();

        try
        {
            return Results.Ok(await retailerDiscovery.RecordAsync(request, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (CatalogueValidationException exception)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [exception.Field] = [exception.Message]
                });
        }
    })
    .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
    .WithName("RecordRetailerDiscovery")
    .WithTags("Retailer Discovery")
    .Produces<RetailerProductListingItem>()
    .ProducesValidationProblem();

app.MapGet(
    "/api/v1/retailer-management",
    async (
        string? q,
        string? status,
        ICurrentUser currentUser,
        IRetailerManagement retailerManagement,
        CancellationToken cancellationToken) =>
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
            return Results.Forbid();

        RetailerStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<RetailerStatus>(status, true, out var value) ||
                !Enum.IsDefined(value))
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["status"] = ["The retailer status is invalid."]
                    });
            }

            parsedStatus = value;
        }

        try
        {
            return Results.Ok(
                await retailerManagement.GetAsync(
                    actor,
                    q,
                    parsedStatus,
                    cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    })
    .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
    .WithName("GetRetailerManagement")
    .WithTags("Retailer Management")
    .Produces<IReadOnlyList<RetailerManagementItem>>();

app.MapPost(
    "/api/v1/retailer-management",
    async (
        CreateRetailerManagement request,
        ICurrentUser currentUser,
        IRetailerManagement retailerManagement,
        CancellationToken cancellationToken) =>
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
            return Results.Forbid();

        try
        {
            var retailer = await retailerManagement.CreateAsync(
                actor,
                request,
                cancellationToken);

            return Results.Created(
                $"/api/v1/retailer-management/{retailer.Id}",
                retailer);
        }
        catch (CatalogueValidationException exception)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [exception.Field] = [exception.Message]
                });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    })
    .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
    .WithName("CreateRetailerManagement")
    .WithTags("Retailer Management")
    .Produces<RetailerManagementItem>(StatusCodes.Status201Created)
    .ProducesValidationProblem();

app.MapPut(
    "/api/v1/retailer-management/{retailerId:guid}/identity",
    async (
        Guid retailerId,
        UpdateRetailerIdentity request,
        ICurrentUser currentUser,
        IRetailerManagement retailerManagement,
        CancellationToken cancellationToken) =>
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
            return Results.Forbid();

        try
        {
            return Results.Ok(
                await retailerManagement.UpdateIdentityAsync(
                    actor,
                    retailerId,
                    request,
                    cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (CatalogueValidationException exception)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [exception.Field] = [exception.Message]
                });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    })
    .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
    .WithName("UpdateRetailerIdentity")
    .WithTags("Retailer Management")
    .Produces<RetailerManagementItem>()
    .ProducesValidationProblem();

app.MapGet(
    "/api/v1/me/explorer",
    async (
        ICurrentExplorer currentExplorer,
        CancellationToken cancellationToken) =>
    {
        var explorer =
            await currentExplorer.GetAsync(cancellationToken);

        return explorer is null
            ? Results.Forbid()
            : Results.Ok(explorer);
    })
    .RequireAuthorization("Explorer")
    .WithName("GetCurrentExplorer")
    .WithTags("Explorer");

app.MapPost(
    "/api/v1/observations",
    async (
        CreateObservationRequest request,
        ICurrentExplorer currentExplorer,
        IObservationSubmissions submissions,
        CancellationToken cancellationToken) =>
    {
        var explorer =
            await currentExplorer.GetAsync(cancellationToken);

        if (explorer is null)
            return Results.Forbid();

        if (!Enum.IsDefined(request.Type) ||
            request.ObservedAtUtc == default ||
            (request.ProductId is null &&
             string.IsNullOrWhiteSpace(request.CandidateProductName)))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["observation"] =
                    [
                        "Type, observation time, and a known or candidate product are required."
                    ]
                });
        }

        try
        {
            var receipt =
                await submissions.SubmitAsync(
                    explorer,
                    new ObservationSubmission(
                        request.Type,
                        request.ObservedAtUtc,
                        request.ProductId,
                        request.CandidateProductName,
                        request.LocationId,
                        request.Narrative),
                    cancellationToken);

            return Results.Created(
                $"/api/v1/observations/{receipt.Id}",
                receipt);
        }
        catch (InvalidOperationException exception)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["reference"] = [exception.Message]
                });
        }
    })
    .RequireAuthorization("Explorer")
    .WithName("CreateObservation")
    .WithTags("Observations")
    .Produces<ObservationReceipt>(StatusCodes.Status201Created)
    .ProducesValidationProblem();

app.MapGet(
    "/api/v1/products/lookup/{gtin}",
    async (
        string gtin,
        IAtlasQueries atlasQueries,
        CancellationToken cancellationToken) =>
    {
        var normalizedGtin =
            gtin
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty);

        if (!Regex.IsMatch(normalizedGtin, "^[0-9]{8,14}$"))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["gtin"] =
                    [
                        "GTIN must contain 8 to 14 digits."
                    ]
                });
        }

        var identification =
            await atlasQueries.GetProductByGtinAsync(
                normalizedGtin,
                cancellationToken);

        return identification is null
            ? Results.NotFound(
                new
                {
                    code = "product_identifier_not_found",
                    message = "No product pack is known for this GTIN."
                })
            : Results.Ok(identification);
    })
    .WithName("LookupProductByGtin")
    .WithTags("Products")
    .Produces<ProductIdentification>()
    .Produces(StatusCodes.Status404NotFound)
    .ProducesValidationProblem();

app.MapGet(
    "/api/v1/products",
    async (
        string? q,
        string? manufacturer,
        string? brand,
        string? productType,
        string? size,
        string? backing,
        string? packaging,
        string? sort,
        int? limit,
        int? offset,
        IAtlasQueries atlasQueries,
        CancellationToken cancellationToken) =>
    {
        if (!TryParseGuidList(
                manufacturer,
                out var manufacturerIds) ||
            !TryParseGuidList(
                brand,
                out var brandIds) ||
            !TryParseEnumList(
                productType,
                out IReadOnlyList<ProductType> productTypes) ||
            !TryParseEnumList(
                backing,
                out IReadOnlyList<BackingType> backings) ||
            !TryParseEnumList(
                packaging,
                out IReadOnlyList<PackagingType> packagingTypes))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["filters"] =
                    [
                        "One or more catalogue filter values are invalid."
                    ]
                });
        }

        var filters =
            new CatalogueProductFilters(
                manufacturerIds,
                brandIds,
                productTypes,
                ParseStringList(size),
                backings,
                packagingTypes);

        var catalogue =
            await atlasQueries.SearchCatalogueAsync(
                q,
                filters,
                string.IsNullOrWhiteSpace(sort)
                    ? "relevance"
                    : sort,
                limit ?? 24,
                offset ?? 0,
                cancellationToken);

        return Results.Ok(catalogue);
    })
    .WithName("SearchCatalogueProducts")
    .WithTags("Products")
    .Produces<CatalogueProductSearch>()
    .ProducesValidationProblem();

app.MapGet(
    "/api/v1/catalogue-management/products",
    async (
        string? q,
        string? manufacturer,
        string? productType,
        string? status,
        string? sort,
        int? limit,
        int? offset,
        IAtlasQueries atlasQueries,
        CancellationToken cancellationToken) =>
    {
        if (!TryParseGuidList(manufacturer, out var manufacturerIds) ||
            !TryParseEnumList(productType, out IReadOnlyList<ProductType> productTypes) ||
            !TryParseEnumList(status, out IReadOnlyList<ProductStatus> statuses))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["filters"] = ["One or more product management filter values are invalid."]
                });
        }

        var catalogue = await atlasQueries.SearchCatalogueManagementAsync(
            q,
            new CatalogueProductManagementFilters(manufacturerIds, productTypes, statuses),
            string.IsNullOrWhiteSpace(sort) ? "name" : sort,
            limit ?? 25,
            offset ?? 0,
            cancellationToken);

        return Results.Ok(catalogue);
    })
    .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
    .WithName("SearchCatalogueManagementProducts")
    .WithTags("Catalogue Management")
    .Produces<CatalogueProductSearch>()
    .ProducesValidationProblem();

static IReadOnlyList<string> ParseStringList(string? value) =>
    string.IsNullOrWhiteSpace(value)
        ? []
        : value
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

static bool TryParseEnum<TEnum>(string? value, out TEnum result)
    where TEnum : struct, Enum
{
    return Enum.TryParse(value, true, out result) && Enum.IsDefined(result);
}

static string? NullIfEmpty(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.ToString();

static bool TryParseGuidList(
    string? value,
    out IReadOnlyList<Guid> result)
{
    var values = ParseStringList(value);
    var parsed = new List<Guid>(values.Count);

    foreach (var item in values)
    {
        if (!Guid.TryParse(item, out var id))
        {
            result = [];
            return false;
        }

        parsed.Add(id);
    }

    result = parsed;
    return true;
}

static bool TryParseEnumList<TEnum>(
    string? value,
    out IReadOnlyList<TEnum> result)
    where TEnum : struct, Enum
{
    var values = ParseStringList(value);
    var parsed = new List<TEnum>(values.Count);

    foreach (var item in values)
    {
        if (!Enum.TryParse<TEnum>(
                item,
                true,
                out var parsedValue) ||
            !Enum.IsDefined(parsedValue))
        {
            result = [];
            return false;
        }

        parsed.Add(parsedValue);
    }

    result = parsed;
    return true;
}

app.MapGet(
    "/api/v1/products/{slug}",
    async (
        string slug,
        IAtlasQueries atlasQueries,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Results.NotFound();

        var product =
            await atlasQueries.GetProductDetailsBySlugAsync(
                slug.Trim(),
                cancellationToken);

        return product is null
            ? Results.NotFound()
            : Results.Ok(product);
    })
    .WithName("GetCatalogueProduct")
    .WithTags("Products")
    .Produces<CatalogueProductDetails>()
    .Produces(StatusCodes.Status404NotFound);

app.MapGet(
    "/api/v1/products/{productId:guid}/images/{imageId:guid}",
    async (
        Guid productId,
        Guid imageId,
        IAtlasQueries atlasQueries,
        CancellationToken cancellationToken) =>
    {
        var content = await atlasQueries.GetProductImageContentAsync(productId, imageId, false, cancellationToken);
        return content is null
            ? Results.NotFound()
            : Results.File(content.Content, content.ContentType, content.FileName, enableRangeProcessing: true);
    })
    .WithName("GetPublicCatalogueProductImage")
    .WithTags("Products")
    .Produces(StatusCodes.Status404NotFound);

app.MapGet(
    "/api/v1/products/{productId:guid}/moderator-preview",
    async (
        Guid productId,
        ICurrentUser currentUser,
        IAtlasQueries atlasQueries,
        CancellationToken cancellationToken) =>
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
            return Results.Forbid();

        try
        {
            var product = await atlasQueries.GetProductDetailsForModeratorAsync(actor, productId, cancellationToken);
            return product is null ? Results.NotFound() : Results.Ok(product);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    })
    .RequireAuthorization("PublishAtlas")
    .WithName("GetModeratorCatalogueProductPreview")
    .WithTags("Products")
    .Produces<CatalogueModeratorProductDetails>()
    .Produces(StatusCodes.Status404NotFound);

app.MapGet(
    "/api/v1/products/{productId:guid}/moderator-images/{imageId:guid}",
    async (
        Guid productId,
        Guid imageId,
        ICurrentUser currentUser,
        IAtlasQueries atlasQueries,
        CancellationToken cancellationToken) =>
    {
        var actor = await currentUser.GetAsync(cancellationToken);
        if (actor is null)
            return Results.Forbid();

        var content = await atlasQueries.GetProductImageContentAsync(productId, imageId, true, cancellationToken);
        return content is null
            ? Results.NotFound()
            : Results.File(content.Content, content.ContentType, content.FileName, enableRangeProcessing: true);
    })
    .RequireAuthorization("PublishAtlas")
    .WithName("GetModeratorCatalogueProductImage")
    .WithTags("Products")
    .Produces(StatusCodes.Status404NotFound);

if (builder.Configuration.GetValue<bool>(
        "Editorial:CatalogueWritesEnabled"))
{
    app.MapPut(
        "/api/v1/products/{productId:guid}/description-visibility",
        async (
            Guid productId,
            UpdateCatalogueProductDescriptionVisibilityRequest request,
            ICurrentUser currentUser,
            ICanonicalCatalogue canonicalCatalogue,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null)
                return Results.Forbid();

            try
            {
                await canonicalCatalogue.SetDescriptionVisibilityAsync(
                    actor,
                    productId,
                    request.Visibility,
                    cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (ArgumentException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["visibility"] = [exception.Message] });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("UpdateCatalogueProductDescriptionVisibility")
        .WithTags("Products")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem();

    app.MapGet(
        "/api/v1/catalogue-submissions",
        async (
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var queue =
                    await submissions.GetSubmissionsAsync(
                        actor,
                        cancellationToken);

                return Results.Ok(queue);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("GetCatalogueSubmissionQueue")
        .WithTags("Catalogue Submissions")
        .Produces<IReadOnlyList<CatalogueSubmissionQueueItem>>();

    app.MapGet(
        "/api/v1/catalogue-submissions/import-template",
        () => Results.File(
            System.Text.Encoding.UTF8.GetBytes(
                "ImportProductKey,Manufacturer,Brand,ProductName,ProductType,PackagingType,ProductFamily,Description,ProductStatus,OfficialWebsite,VariantName,BackingType,FastenerType,FastenerCount,Appearance,PrimaryColour,WetnessIndicator,StandingLeakGuards,WaistbandStyle,Fragrance,LatexFree,DesignedFor,ConstructionNotes,ManufacturerSize,WaistMinCm,WaistMaxCm,HipMinCm,HipMaxCm,FitMeasurementBasis,AbsorbencyMl,AbsorbencyBasisMethod,AbsorbencySource,LengthMm,WidthMm,WeightGrams,ManufacturerPackQuantity,GTIN,IdentitySourceUrl,Notes,DescriptionVisibility\n" +
            "EXAMPLE-PRODUCT-001,Example Manufacturer,Example Brand,Example Product,Diaper,Bag,Example Family,,Current,https://example.com,Original,Plastic,AdhesiveTape,2,Plain,White,Yes,Yes,AllAroundElastic,None,No,Adult,,Medium,80,110,,,,Waist,7500,Manufacturer stated,Manufacturer,900,700,1800,10,1234567890123,https://example.com/product,,ModeratorOnly\n"),
            "text/csv",
            "DiaperScout-Catalogue-Import-Template.csv"))
        .WithName("GetCatalogueSubmissionImportTemplate")
        .WithTags("Catalogue Submissions");

    app.MapPost(
        "/api/v1/catalogue-submissions/import-csv",
        async (
            HttpRequest httpRequest,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null)
                return Results.Forbid();

            if (!httpRequest.HasFormContentType)
                return Results.BadRequest(new { message = "A multipart/form-data CSV upload is required." });

            var form = await httpRequest.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { message = "Choose a CSV file to import." });

            if (file.Length > 10 * 1024 * 1024)
                return Results.BadRequest(new { message = "CSV imports are limited to 10 MB." });

            try
            {
                await using var stream = file.OpenReadStream();
                var result = await submissions.ImportCsvAsync(
                    actor,
                    stream,
                    new CatalogueSubmissionImportOptions(
                        TreatImportedDescriptionsAsModeratorOnly: true),
                    cancellationToken);

                return Results.Ok(result);
            }
            catch (FormatException exception)
            {
                return Results.BadRequest(new { message = exception.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("ImportCatalogueSubmissionsCsv")
        .WithTags("Catalogue Submissions")
        .DisableAntiforgery()
        .Produces<CatalogueSubmissionImportResult>()
        .Produces(StatusCodes.Status400BadRequest);

    app.MapPost(
        "/api/v1/catalogue-submissions",
        async (
            CreateCatalogueSubmissionRequest request,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var receipt =
                    await submissions.CreateAsync(
                        actor,
                        new CreateCatalogueSubmission(
                            request.Source,
                            request.ProposedManufacturerName,
                            request.ProposedBrandName,
                            request.ProposedProductName,
                            request.ProposedVariantName,
                            request.Notes),
                        cancellationToken);

                return Results.Created(
                    $"/api/v1/catalogue-submissions/{receipt.Id}",
                    receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("CreateCatalogueSubmission")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionReceipt>(
            StatusCodes.Status201Created)
        .ProducesValidationProblem();

    app.MapGet(
        "/api/v1/catalogue-submissions/{id:guid}",
        async (
            Guid id,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var receipt =
                    await submissions.GetAsync(
                        actor,
                        id,
                        cancellationToken);

                return Results.Ok(receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("GetCatalogueSubmission")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionReceipt>()
        .ProducesValidationProblem();

    app.MapDelete(
        "/api/v1/catalogue-submissions/{id:guid}",
        async (
            Guid id,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                await submissions.DeleteAsync(
                    actor,
                    id,
                    cancellationToken);

                return Results.NoContent();
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("DeleteCatalogueSubmission")
        .WithTags("Catalogue Submissions")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem();

    app.MapGet(
        "/api/v1/catalogue-submissions/{id:guid}/variants",
        async (
            Guid id,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            var result = await submissions.GetVariantsAsync(
                actor,
                id,
                cancellationToken);

            return result.Status switch
            {
                CatalogueSubmissionVariantsStatus.Found =>
                    Results.Ok(result.Variants),
                CatalogueSubmissionVariantsStatus.Invalid =>
                    Results.ValidationProblem(result.Errors!),
                CatalogueSubmissionVariantsStatus.AccessDenied =>
                    Results.Forbid(),
                _ => Results.Problem(result.Message)
            };
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("GetCatalogueSubmissionVariants")
        .WithTags("Catalogue Submissions")
        .Produces<IReadOnlyList<CatalogueSubmissionVariantReceipt>>();

    app.MapGet(
        "/api/v1/catalogue-submissions/{id:guid}/variants/{variantId:guid}/sizes",
        async (
            Guid id,
            Guid variantId,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null) return Results.Forbid();

            var result = await submissions.GetSizeVariantsAsync(
                actor,
                id,
                variantId,
                cancellationToken);

            return result.Status switch
            {
                CatalogueSubmissionSizeVariantsStatus.Found =>
                    Results.Ok(result.Sizes),
                CatalogueSubmissionSizeVariantsStatus.Invalid =>
                    Results.ValidationProblem(result.Errors!),
                CatalogueSubmissionSizeVariantsStatus.AccessDenied =>
                    Results.Forbid(),
                _ => Results.Problem(result.Message)
            };
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("GetCatalogueSubmissionSizeVariants")
        .WithTags("Catalogue Submissions")
        .Produces<IReadOnlyList<CatalogueSubmissionSizeVariantReceipt>>();

    app.MapPost(
        "/api/v1/catalogue-submissions/{id:guid}/variants/{variantId:guid}/sizes",
        async (
            Guid id,
            Guid variantId,
            AddCatalogueSubmissionSizeVariantRequest request,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null) return Results.Forbid();

            try
            {
                var receipt = await submissions.AddSizeVariantAsync(
                    actor,
                    id,
                    variantId,
                    new AddCatalogueSubmissionSizeVariant(
                        request.ManufacturerSize,
                        request.WaistMinimumCm,
                        request.WaistMaximumCm,
                        request.HipMinimumCm,
                        request.HipMaximumCm,
                        request.ManufacturerStatedAbsorbencyMl,
                        request.FitMeasurementBasis,
                        request.AbsorbencyBasisMethod,
                        request.AbsorbencySource,
                        request.LengthMm,
                        request.WidthMm,
                        request.WeightGrams,
                        request.ManufacturerPackQuantity,
                        request.Gtin),
                    cancellationToken);

                return Results.Created(
                    $"/api/v1/catalogue-submissions/{id}/variants/{variantId}/sizes/{receipt.Id}",
                    receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { [exception.Field] = [exception.Message] });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("AddCatalogueSubmissionSizeVariant")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionSizeVariantReceipt>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

    app.MapPut(
        "/api/v1/catalogue-submissions/{id:guid}/variants/{variantId:guid}/sizes/{sizeVariantId:guid}",
        async (
            Guid id,
            Guid variantId,
            Guid sizeVariantId,
            UpdateCatalogueSubmissionSizeVariantRequest request,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null) return Results.Forbid();

            try
            {
                var receipt = await submissions.UpdateSizeVariantAsync(
                    actor,
                    id,
                    variantId,
                    sizeVariantId,
                    new UpdateCatalogueSubmissionSizeVariant(
                        request.ManufacturerSize,
                        request.WaistMinimumCm,
                        request.WaistMaximumCm,
                        request.HipMinimumCm,
                        request.HipMaximumCm,
                        request.ManufacturerStatedAbsorbencyMl,
                        request.FitMeasurementBasis,
                        request.AbsorbencyBasisMethod,
                        request.AbsorbencySource,
                        request.LengthMm,
                        request.WidthMm,
                        request.WeightGrams,
                        request.ManufacturerPackQuantity,
                        request.Gtin),
                    cancellationToken);

                return Results.Ok(receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { [exception.Field] = [exception.Message] });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("UpdateCatalogueSubmissionSizeVariant")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionSizeVariantReceipt>()
        .ProducesValidationProblem();

    app.MapDelete(
        "/api/v1/catalogue-submissions/{id:guid}/variants/{variantId:guid}/sizes/{sizeVariantId:guid}",
        async (
            Guid id,
            Guid variantId,
            Guid sizeVariantId,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null) return Results.Forbid();

            try
            {
                await submissions.RemoveSizeVariantAsync(
                    actor,
                    id,
                    variantId,
                    sizeVariantId,
                    cancellationToken);

                return Results.NoContent();
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { [exception.Field] = [exception.Message] });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("RemoveCatalogueSubmissionSizeVariant")
        .WithTags("Catalogue Submissions")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem();

    app.MapGet(
        "/api/v1/catalogue-submissions/{id:guid}/variants/{variantId:guid}/override",
        async (
            Guid id,
            Guid variantId,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null) return Results.Forbid();

            try
            {
                var result = await submissions.GetVariantOverrideAsync(actor, id, variantId, cancellationToken);
                return result is null
                    ? Results.NoContent()
                    : Results.Ok(result);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [exception.Field] = [exception.Message] });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("GetCatalogueSubmissionVariantOverride")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionVariantOverrideReceipt>();

    app.MapPut(
        "/api/v1/catalogue-submissions/{id:guid}/variants/{variantId:guid}/override",
        async (
            Guid id,
            Guid variantId,
            UpdateCatalogueSubmissionVariantOverrideRequest request,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null) return Results.Forbid();

            try
            {
                if (!Enum.IsDefined(request.Attribute))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["attribute"] = ["The variant override attribute is invalid."] });

                var result = await submissions.UpdateVariantOverrideAsync(
                    actor,
                    id,
                    variantId,
                    new UpdateCatalogueSubmissionVariantOverride(request.Attribute, request.Value),
                    cancellationToken);

                return Results.Ok(result);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [exception.Field] = [exception.Message] });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("UpdateCatalogueSubmissionVariantOverride")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionVariantOverrideReceipt>()
        .ProducesValidationProblem();

    app.MapPost(
        "/api/v1/catalogue-submissions/{id:guid}/variants",
        async (
            Guid id,
            AddCatalogueSubmissionVariantRequest request,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var receipt = await submissions.AddVariantAsync(
                    actor,
                    id,
                    new AddCatalogueSubmissionVariant(request.Name),
                    cancellationToken);

                return Results.Created(
                    $"/api/v1/catalogue-submissions/{id}/variants/{receipt.Id}",
                    receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("AddCatalogueSubmissionVariant")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionVariantReceipt>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

    app.MapPut(
        "/api/v1/catalogue-submissions/{id:guid}/variants/{variantId:guid}",
        async (
            Guid id,
            Guid variantId,
            UpdateCatalogueSubmissionVariantRequest request,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var receipt = await submissions.UpdateVariantAsync(
                    actor,
                    id,
                    variantId,
                    new UpdateCatalogueSubmissionVariant(request.Name),
                    cancellationToken);

                return Results.Ok(receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("UpdateCatalogueSubmissionVariant")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionVariantReceipt>()
        .ProducesValidationProblem();

    app.MapDelete(
        "/api/v1/catalogue-submissions/{id:guid}/variants/{variantId:guid}",
        async (
            Guid id,
            Guid variantId,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                await submissions.RemoveVariantAsync(
                    actor,
                    id,
                    variantId,
                    cancellationToken);

                return Results.NoContent();
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("RemoveCatalogueSubmissionVariant")
        .WithTags("Catalogue Submissions")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem();

    app.MapPut(
        "/api/v1/catalogue-submissions/{id:guid}/identity",
        async (
            Guid id,
            UpdateCatalogueSubmissionIdentityRequest request,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var receipt =
                    await submissions.UpdateIdentityAsync(
                        actor,
                        id,
                        new UpdateCatalogueSubmissionIdentity(
                            request.ProposedGtin,
                            request.ProposedSku,
                            request.IdentitySourceUrl),
                        cancellationToken);

                return Results.Ok(receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("UpdateCatalogueSubmissionIdentity")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionReceipt>()
        .ProducesValidationProblem();

    app.MapPut(
        "/api/v1/catalogue-submissions/{id:guid}/entity-resolution",
        async (
            Guid id,
            ResolveCatalogueSubmissionEntitiesRequest request,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var receipt =
                    await submissions.ResolveEntitiesAsync(
                        actor,
                        id,
                        new ResolveCatalogueSubmissionEntities(
                            request.ManufacturerId,
                            request.NewManufacturerName,
                            request.BrandId,
                            request.NewBrandName),
                        cancellationToken);

                return Results.Ok(receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("ResolveCatalogueSubmissionEntities")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionReceipt>()
        .ProducesValidationProblem();

    app.MapPut(
        "/api/v1/catalogue-submissions/{id:guid}/specifications",
        async (
            Guid id,
            UpdateCatalogueSubmissionSpecificationsRequest request,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var receipt =
                    await submissions.UpdateSpecificationsAsync(
                        actor,
                        id,
                        new UpdateCatalogueSubmissionSpecifications(
                            request.ProposedProductType,
                            request.ProposedPackagingType,
                            request.ProposedProductFamily,
                            request.ProposedDescription,
                            request.ProposedDescriptionVisibility,
                            request.ProposedProductStatus,
                            request.ProposedOfficialWebsiteUrl,
                            request.SharedAppearance,
                            request.SharedPrimaryColour,
                            request.SharedWetnessIndicator,
                            request.SharedStandingLeakGuards,
                            request.SharedWaistbandStyle,
                            request.SharedFragrance,
                            request.SharedLatexFree,
                            request.SharedDesignedFor,
                            request.SharedFastenerCount,
                            request.SharedConstructionNotes),
                        cancellationToken);

                return Results.Ok(receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("UpdateCatalogueSubmissionSpecifications")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionReceipt>()
        .ProducesValidationProblem();

    app.MapPut(
        "/api/v1/catalogue-submissions/{id:guid}/description-visibility",
        async (
            Guid id,
            UpdateCatalogueSubmissionDescriptionVisibilityRequest request,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var receipt =
                    await submissions.UpdateDescriptionVisibilityAsync(
                        actor,
                        id,
                        request.Visibility,
                        cancellationToken);

                return Results.Ok(receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("UpdateCatalogueSubmissionDescriptionVisibility")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionReceipt>()
        .ProducesValidationProblem();

    app.MapPost(
        "/api/v1/catalogue-submissions/{id:guid}/retail-destinations",
        async (
            Guid id,
            AddCatalogueSubmissionRetailDestinationRequest request,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var receipt =
                    await submissions.AddRetailDestinationAsync(
                        actor,
                        id,
                        new AddCatalogueSubmissionRetailDestination(
                            request.RetailerId,
                            request.ListingUrl,
                            request.Notes),
                        cancellationToken);

                return Results.Created(
                    $"/api/v1/catalogue-submissions/{id}/retail-destinations/{receipt.Id}",
                    receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("AddCatalogueSubmissionRetailDestination")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionRetailDestinationReceipt>(
            StatusCodes.Status201Created)
        .ProducesValidationProblem();

    // Catalogue submission images.
    app.MapGet(
        "/api/v1/catalogue-submissions/{id:guid}/images",
        async (
            Guid id,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null)
                return Results.Forbid();

            try
            {
                var result = await submissions.GetImagesAsync(actor, id, cancellationToken);
                return Results.Ok(result.Images);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("GetCatalogueSubmissionImages")
        .WithTags("Catalogue Submissions")
        .Produces<IReadOnlyList<CatalogueSubmissionImageReceipt>>();

    app.MapPost(
        "/api/v1/catalogue-submissions/{id:guid}/images",
        async (
            Guid id,
            HttpRequest request,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null)
                return Results.Forbid();

            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");

            if (file is null)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = ["An image file is required."]
                });

            if (!Enum.TryParse<CatalogueSubmissionImageRole>(form["role"].ToString(), true, out var role) ||
                !Enum.IsDefined(role))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["role"] = ["A valid image role is required."]
                });

            if (!Enum.TryParse<CatalogueImageSourceType>(form["sourceType"].ToString(), true, out var sourceType) ||
                !Enum.IsDefined(sourceType))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["sourceType"] = ["A valid image source type is required."]
                });

            if (!Enum.TryParse<CatalogueImagePermissionStatus>(form["permissionStatus"].ToString(), true, out var permissionStatus) ||
                !Enum.IsDefined(permissionStatus))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["permissionStatus"] = ["A valid image permission status is required."]
                });

            if (file.Length is <= 0 or > 15 * 1024 * 1024)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = ["Images must be greater than zero and no larger than 15 MB."]
                });

            if (file.ContentType is not "image/jpeg" and not "image/png" and not "image/webp")
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = ["Only JPEG, PNG and WebP images are supported."]
                });

            try
            {
                await using var stream = file.OpenReadStream();

                var receipt = await submissions.AddImageAsync(
                    actor,
                    id,
                    new AddCatalogueSubmissionImage(
                        role,
                        Path.GetFileName(file.FileName),
                        file.ContentType,
                        file.Length,
                        stream,
                        sourceType,
                        form["sourceUrl"].ToString(),
                        form["sourceNotes"].ToString(),
                        permissionStatus,
                        form["permissionEvidence"].ToString()),
                    cancellationToken);

                return Results.Created(receipt.ContentUrl, receipt);
            }
            catch (ArgumentException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["image"] = [exception.Message]
                });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { message = exception.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("AddCatalogueSubmissionImage")
        .WithTags("Catalogue Submissions")
        .DisableAntiforgery()
        .Produces<CatalogueSubmissionImageReceipt>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

    app.MapPut(
        "/api/v1/catalogue-submissions/{id:guid}/images/{imageId:guid}",
        async (
            Guid id,
            Guid imageId,
            UpdateCatalogueSubmissionImageMetadataRequest request,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null)
                return Results.Forbid();

            try
            {
                var receipt = await submissions.UpdateImageMetadataAsync(
                    actor,
                    id,
                    imageId,
                    new UpdateCatalogueSubmissionImageMetadata(
                        request.SourceType,
                        request.SourceUrl,
                        request.SourceNotes,
                        request.PermissionStatus,
                        request.PermissionEvidence),
                    cancellationToken);

                return Results.Ok(receipt);
            }
            catch (ArgumentException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["image"] = [exception.Message]
                });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { message = exception.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("UpdateCatalogueSubmissionImageMetadata")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionImageReceipt>()
        .ProducesValidationProblem();

    app.MapDelete(
        "/api/v1/catalogue-submissions/{id:guid}/images/{imageId:guid}",
        async (
            Guid id,
            Guid imageId,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null)
                return Results.Forbid();

            try
            {
                await submissions.RemoveImageAsync(actor, id, imageId, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { message = exception.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("RemoveCatalogueSubmissionImage")
        .WithTags("Catalogue Submissions");

    app.MapGet(
        "/api/v1/catalogue-submissions/{id:guid}/images/{imageId:guid}/content",
        async (
            Guid id,
            Guid imageId,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null)
                return Results.Forbid();

            try
            {
                var content = await submissions.GetImageContentAsync(actor, id, imageId, cancellationToken);

                return content is null
                    ? Results.NotFound()
                    : Results.File(
                        content.Content,
                        content.ContentType,
                        content.FileName,
                        enableRangeProcessing: true);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("GetCatalogueSubmissionImageContent")
        .WithTags("Catalogue Submissions");

    // Retail workspace
    app.MapGet(
        "/api/v1/catalogue-submissions/{id:guid}/retail-workspace",
        async (
            Guid id,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var workspace =
                    await submissions.GetRetailWorkspaceAsync(
                        actor,
                        id,
                        cancellationToken);

                return Results.Ok(workspace);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("GetCatalogueSubmissionRetailWorkspace")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionRetailWorkspace>();

    // Retailer list used by the retail destination editor.
    app.MapGet(
        "/api/v1/catalogue-submissions/retailers",
        async (
            ICurrentUser currentUser,
            ICatalogueRetailQueries retailQueries,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            var retailers =
                await retailQueries.GetRetailersAsync(
                    cancellationToken);

            return Results.Ok(retailers);
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("GetCatalogueRetailers")
        .WithTags("Catalogue Submissions")
        .Produces<IReadOnlyList<CatalogueRetailerOption>>();

    // Existing retail destinations for a submission.
    app.MapGet(
        "/api/v1/catalogue-submissions/{id:guid}/retail-destinations",
        async (
            Guid id,
            ICurrentUser currentUser,
            ICatalogueRetailQueries retailQueries,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            var destinations =
                await retailQueries.GetDestinationsAsync(
                    id,
                    cancellationToken);

            return Results.Ok(destinations);
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("GetCatalogueSubmissionRetailDestinations")
        .WithTags("Catalogue Submissions")
        .Produces<IReadOnlyList<CatalogueSubmissionRetailDestinationReceipt>>();

    app.MapPost(
        "/api/v1/catalogue-submissions/{id:guid}/retail-destinations/{destinationId:guid}/affiliate",
        async (
            Guid id,
            Guid destinationId,
            AddCatalogueSubmissionRetailAffiliateRequest request,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var receipt =
                    await submissions.AddAffiliateAsync(
                        actor,
                        id,
                        new AddCatalogueSubmissionRetailAffiliate(
                            destinationId,
                            request.Status,
                            request.Network,
                            request.TrackingConfiguration,
                            request.DeepLinkMechanism,
                            request.TermsUrl,
                            request.ApplicationReference,
                            request.Notes),
                        cancellationToken);

                return Results.Created(
                    $"/api/v1/catalogue-submissions/{id}/retail-destinations/{destinationId}/affiliate",
                    receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("AddCatalogueSubmissionRetailAffiliate")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionRetailAffiliateReceipt>(
            StatusCodes.Status201Created)
        .ProducesValidationProblem();

    // Verification workspace.
    app.MapGet(
        "/api/v1/catalogue-submissions/{id:guid}/verification-workspace",
        async (
            Guid id,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var workspace =
                    await submissions.GetVerificationWorkspaceAsync(
                        actor,
                        id,
                        cancellationToken);

                return Results.Ok(workspace);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("GetCatalogueSubmissionVerificationWorkspace")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionVerificationWorkspace>();

    app.MapPost(
        "/api/v1/catalogue-submissions/{id:guid}/review",
        async (
            Guid id,
            ReviewCatalogueSubmissionRequest request,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var receipt =
                    await submissions.ReviewAsync(
                        actor,
                        id,
                        new ReviewCatalogueSubmission(
                            request.Outcome,
                            request.Rationale),
                        cancellationToken);

                return Results.Ok(receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("ReviewCatalogueSubmission")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionEditorialDecisionReceipt>()
        .ProducesValidationProblem();

    app.MapPost(
        "/api/v1/catalogue-submissions/{id:guid}/return-to-verification",
        async (
            Guid id,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var receipt =
                    await submissions.ReturnToVerificationAsync(
                        actor,
                        id,
                        cancellationToken);

                return Results.Ok(receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("ReturnCatalogueSubmissionToVerification")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionReceipt>()
        .ProducesValidationProblem();

    app.MapPost(
        "/api/v1/catalogue-submissions/{id:guid}/publish",
        async (
            Guid id,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var receipt =
                    await submissions.PublishAsync(
                        actor,
                        id,
                        cancellationToken);

                return Results.Ok(receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("PublishCatalogueSubmission")
        .WithTags("Catalogue Submissions")
        .Produces<CataloguePublicationReceipt>()
        .ProducesValidationProblem();

    app.MapPost(
        "/api/v1/catalogue-submissions/{id:guid}/verifications",
        async (
            Guid id,
            AddCatalogueSubmissionVerificationRequest request,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var receipt =
                    await submissions.AddVerificationAsync(
                        actor,
                        id,
                        new AddCatalogueSubmissionVerification(
                            request.Area,
                            request.Status,
                            request.Scope,
                            request.Source,
                            request.SourceUrl,
                            request.Notes,
                            request.PermissionTerms),
                        cancellationToken);

                return Results.Created(
                    $"/api/v1/catalogue-submissions/{id}/verifications/{receipt.Id}",
                    receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("AddCatalogueSubmissionVerification")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionVerificationReceipt>(
            StatusCodes.Status201Created)
        .ProducesValidationProblem();

    app.MapPost(
        "/api/v1/catalogue-submissions/{id:guid}/begin-verification",
        async (
            Guid id,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var receipt =
                    await submissions.BeginVerificationAsync(
                        actor,
                        id,
                        cancellationToken);

                return Results.Ok(receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("BeginCatalogueSubmissionVerification")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionReceipt>()
        .ProducesValidationProblem();

    app.MapPost(
        "/api/v1/catalogue-submissions/{id:guid}/ready-for-review",
        async (
            Guid id,
            ICurrentUser currentUser,
            ICatalogueSubmissions submissions,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var receipt =
                    await submissions.MarkReadyForReviewAsync(
                        actor,
                        id,
                        cancellationToken);

                return Results.Ok(receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("MarkCatalogueSubmissionReadyForReview")
        .WithTags("Catalogue Submissions")
        .Produces<CatalogueSubmissionReceipt>()
        .ProducesValidationProblem();

    app.MapGet(
        "/api/v1/products/catalogue-entry-options",
        async (
            ICanonicalCatalogueQueries catalogueQueries,
            CancellationToken cancellationToken) =>
            Results.Ok(
                await catalogueQueries.GetEntryOptionsAsync(
                    cancellationToken)))
        .RequireAuthorization("PublishAtlas")
        .WithName("GetCanonicalProductEntryOptions")
        .WithTags("Products")
        .Produces<CatalogueEntryOptions>();

    app.MapGet(
        "/api/v1/catalogue-management/entry-options",
        async (
            ICanonicalCatalogueQueries catalogueQueries,
            CancellationToken cancellationToken) =>
            Results.Ok(await catalogueQueries.GetEntryOptionsAsync(cancellationToken)))
        .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
        .WithName("GetCatalogueManagementEntryOptions")
        .WithTags("Catalogue Management")
        .Produces<CatalogueEntryOptions>();

    app.MapGet(
        "/api/v1/catalogue-management/data-quality",
        async (
            ICurrentUser currentUser,
            IAtlasQueries atlasQueries,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null)
                return Results.Forbid();

            try
            {
                return Results.Ok(await atlasQueries.GetProductDataQualityAsync(actor, cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
        .WithName("GetCatalogueProductDataQuality")
        .WithTags("Catalogue Management")
        .Produces<CatalogueDataQualitySummary>();

    app.MapGet(
        "/api/v1/catalogue-management/products/{productId:guid}",
        async (
            Guid productId,
            ICurrentUser currentUser,
            IAtlasQueries atlasQueries,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null)
                return Results.Forbid();

            try
            {
                var product = await atlasQueries.GetProductManagementDetailsAsync(actor, productId, cancellationToken);
                return product is null ? Results.NotFound() : Results.Ok(product);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
        .WithName("GetCatalogueManagementProduct")
        .WithTags("Catalogue Management")
        .Produces<CatalogueProductManagementDetails>()
        .Produces(StatusCodes.Status404NotFound);

    app.MapPost(
        "/api/v1/catalogue-management/products/{productId:guid}/images",
        async (
            Guid productId,
            HttpRequest httpRequest,
            HttpContext httpContext,
            ICurrentUser currentUser,
            ICanonicalCatalogue catalogue,
            ICatalogueSubmissionImageStorage imageStorage,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null)
                return Results.Forbid();

            var form = await httpRequest.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");
            if (file is null || file.Length <= 0 || file.Length > 15 * 1024 * 1024)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Images must be greater than zero and no larger than 15 MB."] });

            if (!TryParseEnum(form["role"].ToString(), out CatalogueSubmissionImageRole role) ||
                !TryParseEnum(form["sourceType"].ToString(), out CatalogueImageSourceType sourceType) ||
                !TryParseEnum(form["permissionStatus"].ToString(), out CatalogueImagePermissionStatus permissionStatus))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["image"] = ["One or more image metadata values are invalid."] });

            var allowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["image/jpeg"] = ".jpg",
                ["image/png"] = ".png",
                ["image/webp"] = ".webp"
            };
            if (!allowedTypes.TryGetValue(file.ContentType, out var extension))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Only JPEG, PNG and WebP images are supported."] });

            var storageKey = $"products/{productId:N}/{Guid.NewGuid():N}{extension}";
            try
            {
                await using (var stream = file.OpenReadStream())
                    await imageStorage.SaveAsync(storageKey, stream, cancellationToken);

                var result = await catalogue.AddProductImageAsync(
                    actor,
                    productId,
                    storageKey,
                    Path.GetFileName(file.FileName),
                    file.ContentType,
                    file.Length,
                    new AddCanonicalProductImageMetadata(
                        role,
                        sourceType,
                        NullIfEmpty(form["sourceUrl"].ToString()),
                        NullIfEmpty(form["sourceNotes"].ToString()),
                        permissionStatus,
                        NullIfEmpty(form["permissionEvidence"].ToString()),
                        bool.TryParse(form["isPrimary"], out var isPrimary) && isPrimary,
                        form["sourceSummary"].ToString(),
                        ParseStringList(form["sourceReferences"]),
                        form["editorialRationale"].ToString(),
                        httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? httpContext.TraceIdentifier),
                    cancellationToken);

                return Results.Created($"/api/v1/catalogue-management/products/{productId}/images/{result.Id}", result);
            }
            catch (CatalogueValidationException exception)
            {
                await imageStorage.DeleteAsync(storageKey, cancellationToken);
                return Results.ValidationProblem(new Dictionary<string, string[]> { [exception.Field] = [exception.Message] });
            }
            catch (KeyNotFoundException exception)
            {
                await imageStorage.DeleteAsync(storageKey, cancellationToken);
                return Results.NotFound(exception.Message);
            }
            catch (UnauthorizedAccessException)
            {
                await imageStorage.DeleteAsync(storageKey, cancellationToken);
                return Results.Forbid();
            }
            catch
            {
                await imageStorage.DeleteAsync(storageKey, cancellationToken);
                throw;
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
        .WithName("AddCatalogueManagementProductImage")
        .WithTags("Catalogue Management")
        .Produces<CatalogueModeratorProductImage>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

    app.MapPut(
        "/api/v1/catalogue-management/products/{productId:guid}/images/{imageId:guid}",
        async (
            Guid productId,
            Guid imageId,
            UpdateCanonicalProductImageMetadata request,
            ICurrentUser currentUser,
            ICanonicalCatalogue catalogue,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null) return Results.Forbid();
            try
            {
                var result = await catalogue.UpdateProductImageAsync(actor, productId, imageId, request, cancellationToken);
                return Results.Ok(result);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [exception.Field] = [exception.Message] });
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
        .WithName("UpdateCatalogueManagementProductImage")
        .WithTags("Catalogue Management")
        .Produces<CatalogueModeratorProductImage>()
        .ProducesValidationProblem();

    app.MapDelete(
        "/api/v1/catalogue-management/products/{productId:guid}/images/{imageId:guid}",
        async (
            Guid productId,
            Guid imageId,
            [FromBody] RemoveCanonicalProductElement request,
            ICurrentUser currentUser,
            ICanonicalCatalogue catalogue,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null) return Results.Forbid();
            try
            {
                await catalogue.RemoveProductImageAsync(actor, productId, imageId, request.SourceSummary, request.SourceReferences, request.EditorialRationale, request.CorrelationId, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
        .WithName("RemoveCatalogueManagementProductImage")
        .WithTags("Catalogue Management")
        .Produces(StatusCodes.Status204NoContent);

    app.MapPost(
        "/api/v1/catalogue-management/products/{productId:guid}/images/{imageId:guid}/primary",
        async (
            Guid productId,
            Guid imageId,
            [FromBody] RemoveCanonicalProductElement request,
            ICurrentUser currentUser,
            ICanonicalCatalogue catalogue,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null) return Results.Forbid();
            try
            {
                await catalogue.SetProductImagePrimaryAsync(actor, productId, imageId, request.SourceSummary, request.SourceReferences, request.EditorialRationale, request.CorrelationId, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
        .WithName("SetCatalogueManagementProductImagePrimary")
        .WithTags("Catalogue Management")
        .Produces(StatusCodes.Status204NoContent);

    app.MapPut(
        "/api/v1/catalogue-management/products/{productId:guid}/identity",
        async (
            Guid productId,
            UpdateCanonicalProductIdentity request,
            HttpContext httpContext,
            ICurrentUser currentUser,
            ICanonicalCatalogue catalogue,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null)
                return Results.Forbid();

            try
            {
                await catalogue.UpdateProductIdentityAsync(
                    actor,
                    productId,
                    request with
                    {
                        CorrelationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                            ?? httpContext.TraceIdentifier
                    },
                    cancellationToken);

                return Results.NoContent();
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [exception.Field] = [exception.Message]
                });
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
        .WithName("UpdateCatalogueManagementProductIdentity")
        .WithTags("Catalogue Management")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem();

    app.MapPut(
        "/api/v1/catalogue-management/products/{productId:guid}/status",
        async (
            Guid productId,
            SetCanonicalProductStatus request,
            HttpContext httpContext,
            ICurrentUser currentUser,
            ICanonicalCatalogue catalogue,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null)
                return Results.Forbid();

            try
            {
                await catalogue.SetProductStatusAsync(
                    actor,
                    productId,
                    request with
                    {
                        CorrelationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                            ?? httpContext.TraceIdentifier
                    },
                    cancellationToken);

                return Results.NoContent();
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [exception.Field] = [exception.Message]
                });
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
        .WithName("SetCatalogueManagementProductStatus")
        .WithTags("Catalogue Management")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem();

    app.MapPost(
        "/api/v1/catalogue-management/products/{productId:guid}/variants",
        async (
            Guid productId,
            CreateCanonicalProductVariantManagement request,
            HttpContext httpContext,
            ICurrentUser currentUser,
            ICanonicalCatalogue catalogue,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null) return Results.Forbid();
            try
            {
                await catalogue.AddProductVariantAsync(
                    actor,
                    productId,
                    request with { CorrelationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? httpContext.TraceIdentifier },
                    cancellationToken);
                return Results.NoContent();
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [exception.Field] = [exception.Message] });
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
        .WithName("AddCatalogueManagementProductVariant")
        .WithTags("Catalogue Management")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem();

    app.MapPut(
        "/api/v1/catalogue-management/products/{productId:guid}/variants/{variantId:guid}",
        async (
            Guid productId,
            Guid variantId,
            UpdateCanonicalProductVariantManagement request,
            HttpContext httpContext,
            ICurrentUser currentUser,
            ICanonicalCatalogue catalogue,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null) return Results.Forbid();
            try
            {
                await catalogue.UpdateProductVariantAsync(
                    actor,
                    productId,
                    variantId,
                    request with { CorrelationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? httpContext.TraceIdentifier },
                    cancellationToken);
                return Results.NoContent();
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [exception.Field] = [exception.Message] });
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
        .WithName("UpdateCatalogueManagementProductVariant")
        .WithTags("Catalogue Management")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem();

    app.MapDelete(
        "/api/v1/catalogue-management/products/{productId:guid}/variants/{variantId:guid}",
        async (
            Guid productId,
            Guid variantId,
            [FromBody] RemoveCanonicalProductElement request,
            ICurrentUser currentUser,
            ICanonicalCatalogue catalogue,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null) return Results.Forbid();
            try
            {
                await catalogue.RemoveProductVariantAsync(actor, productId, variantId, request.SourceSummary, request.SourceReferences, request.EditorialRationale, request.CorrelationId, cancellationToken);
                return Results.NoContent();
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [exception.Field] = [exception.Message] });
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
        .WithName("RemoveCatalogueManagementProductVariant")
        .WithTags("Catalogue Management")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem();

    app.MapPost(
        "/api/v1/catalogue-management/products/{productId:guid}/variants/{variantId:guid}/sizes",
        async (
            Guid productId,
            Guid variantId,
            CreateCanonicalProductSizeManagement request,
            HttpContext httpContext,
            ICurrentUser currentUser,
            ICanonicalCatalogue catalogue,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null) return Results.Forbid();
            try
            {
                await catalogue.AddProductSizeAsync(
                    actor,
                    productId,
                    variantId,
                    request with { CorrelationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? httpContext.TraceIdentifier },
                    cancellationToken);
                return Results.NoContent();
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [exception.Field] = [exception.Message] });
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (ArgumentException exception) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["size"] = [exception.Message] }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
        .WithName("AddCatalogueManagementProductSize")
        .WithTags("Catalogue Management")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem();

    app.MapPut(
        "/api/v1/catalogue-management/products/{productId:guid}/variants/{variantId:guid}/sizes/{sizeId:guid}",
        async (
            Guid productId,
            Guid variantId,
            Guid sizeId,
            UpdateCanonicalProductSizeManagement request,
            HttpContext httpContext,
            ICurrentUser currentUser,
            ICanonicalCatalogue catalogue,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null) return Results.Forbid();
            try
            {
                await catalogue.UpdateProductSizeAsync(
                    actor,
                    productId,
                    variantId,
                    sizeId,
                    request with { CorrelationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? httpContext.TraceIdentifier },
                    cancellationToken);
                return Results.NoContent();
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [exception.Field] = [exception.Message] });
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (ArgumentException exception) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["size"] = [exception.Message] }); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
        .WithName("UpdateCatalogueManagementProductSize")
        .WithTags("Catalogue Management")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem();

    app.MapDelete(
        "/api/v1/catalogue-management/products/{productId:guid}/variants/{variantId:guid}/sizes/{sizeId:guid}",
        async (
            Guid productId,
            Guid variantId,
            Guid sizeId,
            [FromBody] RemoveCanonicalProductElement request,
            ICurrentUser currentUser,
            ICanonicalCatalogue catalogue,
            CancellationToken cancellationToken) =>
        {
            var actor = await currentUser.GetAsync(cancellationToken);
            if (actor is null) return Results.Forbid();
            try
            {
                await catalogue.RemoveProductSizeAsync(actor, productId, variantId, sizeId, request.SourceSummary, request.SourceReferences, request.EditorialRationale, request.CorrelationId, cancellationToken);
                return Results.NoContent();
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [exception.Field] = [exception.Message] });
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
        .WithName("RemoveCatalogueManagementProductSize")
        .WithTags("Catalogue Management")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem();

    app.MapPost(
        "/api/v1/products",
        async (
            CreateCanonicalProductRequest request,
            HttpContext httpContext,
            ICurrentUser currentUser,
            ICanonicalCatalogue catalogue,
            CancellationToken cancellationToken) =>
        {
            var actor =
                await currentUser.GetAsync(cancellationToken);

            if (actor is null)
                return Results.Forbid();

            try
            {
                var receipt =
                    await catalogue.CreateProductAsync(
                        actor,
                        new CreateCanonicalProduct(
                            request.ManufacturerId,
                            request.BrandId,
                            request.ProductName,
                            request.ProductSlug,
                            request.ProductType,
                            request.Status,
                            new[]
                            {
                                new CreateCanonicalProductVariant(
                                    request.VariantName,
                                    request.BackingType,
                                    Sizes:
                                    new[]
                                    {
                                        new CreateCanonicalProductSizeVariant(
                                            request.ManufacturerSize,
                                            request.WaistMinimumCm,
                                            request.WaistMaximumCm,
                                            null,
                                            null,
                                            null,
                                            null,
                                            null,
                                            null,
                                            null,
                                            null,
                                            null,
                                            request.QuantityPerPack,
                                            request.PackagingType,
                                            request.Gtin)
                                    })
                            },
                            request.SourceSummary,
                            request.SourceReferences,
                            request.EditorialRationale,
                            httpContext.Request.Headers["X-Correlation-ID"]
                                .FirstOrDefault()
                                ?? httpContext.TraceIdentifier,
                            request.ProductFamily,
                            request.Description,
                            request.OfficialWebsiteUrl),
                        cancellationToken);

                return Results.Created(
                    $"/api/v1/products/{receipt.ProductId}",
                    receipt);
            }
            catch (CatalogueValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.Field] = new[] { exception.Message }
                    });
            }
        })
        .RequireAuthorization("PublishAtlas")
        .WithName("CreateCanonicalProduct")
        .WithTags("Products")
        .Produces<CanonicalProductReceipt>(
            StatusCodes.Status201Created)
        .ProducesValidationProblem();
}

app.Run();

public partial class Program
{
}
