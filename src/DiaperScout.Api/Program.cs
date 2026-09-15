using DiaperScout.Infrastructure;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
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
                cancellationToken);

        return Results.Ok(catalogue);
    })
    .WithName("SearchCatalogueProducts")
    .WithTags("Products")
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

if (builder.Configuration.GetValue<bool>(
        "Editorial:CatalogueWritesEnabled"))
{
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
                        [exception.Field] = [exception.Message]
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
                        [exception.Field] = [exception.Message]
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
                        [exception.Field] = [exception.Message]
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
                        [exception.Field] = [exception.Message]
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
                        [exception.Field] = [exception.Message]
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
                            request.ProposedManufacturerSize,
                            request.ProposedWaistMinimumCm,
                            request.ProposedWaistMaximumCm,
                            request.ProposedBackingType,
                            request.ProposedFastenerType,
                            request.ProposedWaistbandStyle,
                            request.ProposedFragranceType,
                            request.ProposedQuantityPerPack,
                            request.ProposedPackagingType,
                            request.ProposedProductFamily,
                            request.ProposedDescription,
                            request.ProposedProductStatus,
                            request.ProposedOfficialWebsiteUrl,
                            request.SharedPrintDesign,
                            request.SharedPrimaryColour,
                            request.SharedSecondaryColours,
                            request.SharedWetnessIndicator,
                            request.SharedStandingLeakGuards,
                            request.SharedInnerLeakGuards,
                            request.SharedElasticWaistbandFront,
                            request.SharedElasticWaistbandRear,
                            request.SharedLatexFree,
                            request.SharedChlorineFree,
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
                        [exception.Field] = [exception.Message]
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
                        [exception.Field] = [exception.Message]
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
                        [exception.Field] = [exception.Message]
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
                        [exception.Field] = [exception.Message]
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
                        [exception.Field] = [exception.Message]
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
                        [exception.Field] = [exception.Message]
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
                        [exception.Field] = [exception.Message]
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
                        [exception.Field] = [exception.Message]
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
                        [exception.Field] = [exception.Message]
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
                        [exception.Field] = [exception.Message]
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
                            request.VariantName,
                            request.BackingType,
                            request.ManufacturerSize,
                            request.WaistMinimumCm,
                            request.WaistMaximumCm,
                            request.QuantityPerPack,
                            request.PackagingType,
                            request.Gtin,
                            request.SourceSummary,
                            request.SourceReferences,
                            request.EditorialRationale,
                            httpContext.Request.Headers["X-Correlation-ID"]
                                .FirstOrDefault()
                                ?? httpContext.TraceIdentifier),
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
                        [exception.Field] = [exception.Message]
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

public sealed record CreateObservationRequest(
    ObservationType Type,
    DateTimeOffset ObservedAtUtc,
    Guid? ProductId,
    string? CandidateProductName,
    Guid? LocationId,
    string? Narrative);

public sealed record CreateCanonicalProductRequest(
    Guid ManufacturerId,
    Guid? BrandId,
    string ProductName,
    string ProductSlug,
    ProductType ProductType,
    ProductStatus Status,
    string VariantName,
    BackingType BackingType,
    string ManufacturerSize,
    int? WaistMinimumCm,
    int? WaistMaximumCm,
    int QuantityPerPack,
    PackagingType PackagingType,
    string Gtin,
    string SourceSummary,
    IReadOnlyList<string> SourceReferences,
    string EditorialRationale);

public sealed record CreateCatalogueSubmissionRequest(
    CatalogueSubmissionSource Source,
    string ProposedManufacturerName,
    string? ProposedBrandName,
    string ProposedProductName,
    string ProposedVariantName,
    string? Notes);

public sealed record AddCatalogueSubmissionVariantRequest(
    string Name);

public sealed record UpdateCatalogueSubmissionVariantRequest(
    string Name);

public sealed record AddCatalogueSubmissionRetailDestinationRequest(
    Guid RetailerId,
    string ListingUrl,
    string? Notes);

public sealed record AddCatalogueSubmissionRetailAffiliateRequest(
    AffiliateProgrammeStatus Status,
    string? Network,
    string? TrackingConfiguration,
    string? DeepLinkMechanism,
    string? TermsUrl,
    string? ApplicationReference,
    string? Notes);

public sealed record ReviewCatalogueSubmissionRequest(
    EditorialOutcome Outcome,
    string? Rationale);

public sealed record AddCatalogueSubmissionVerificationRequest(
    CatalogueVerificationArea Area,
    CatalogueVerificationStatus Status,
    string Scope,
    string Source,
    string? SourceUrl,
    string? Notes,
    string? PermissionTerms);

public sealed record UpdateCatalogueSubmissionSpecificationsRequest(
    ProductType? ProposedProductType,
    string? ProposedManufacturerSize,
    int? ProposedWaistMinimumCm,
    int? ProposedWaistMaximumCm,
    BackingType? ProposedBackingType,
    FastenerType? ProposedFastenerType,
    WaistbandStyle? ProposedWaistbandStyle,
    FragranceType? ProposedFragranceType,
    int? ProposedQuantityPerPack,
    PackagingType? ProposedPackagingType,
    string? ProposedProductFamily,
    string? ProposedDescription,
    ProductStatus? ProposedProductStatus,
    string? ProposedOfficialWebsiteUrl,
    string? SharedPrintDesign,
    string? SharedPrimaryColour,
    string? SharedSecondaryColours,
    bool? SharedWetnessIndicator,
    bool? SharedStandingLeakGuards,
    bool? SharedInnerLeakGuards,
    bool? SharedElasticWaistbandFront,
    bool? SharedElasticWaistbandRear,
    bool? SharedLatexFree,
    bool? SharedChlorineFree,
    int? SharedFastenerCount,
    string? SharedConstructionNotes);

public sealed record UpdateCatalogueSubmissionVariantOverrideRequest(
    CatalogueVariantOverrideAttribute Attribute,
    string? Value);

public sealed record UpdateCatalogueSubmissionIdentityRequest(
    string? ProposedGtin,
    string? ProposedSku,
    string? IdentitySourceUrl);

public partial class Program;