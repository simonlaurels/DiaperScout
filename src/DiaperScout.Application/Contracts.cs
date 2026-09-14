using DiaperScout.Domain;

namespace DiaperScout.Application;

public sealed record ProductSummary(
    Guid Id,
    string Name,
    string Slug,
    ProductType ProductType,
    ProductStatus Status);

public sealed record ProductIdentification(
    ProductSummary Product,
    Guid ProductVariantId,
    string VariantName,
    Guid SizeVariantId,
    string ManufacturerSize,
    Guid PackTypeId,
    int QuantityPerPack,
    PackagingType PackagingType,
    string Gtin);

public sealed record CatalogueRetailDestination(
    Guid Id,
    string RetailerName,
    string ListingUrl);

public sealed record CatalogueFacetOption(
    string Value,
    string Label,
    int Count);

public sealed record CatalogueFacet(
    string Key,
    string Label,
    IReadOnlyList<CatalogueFacetOption> Options);

public sealed record CatalogueProductFilters(
    IReadOnlyList<Guid> ManufacturerIds,
    IReadOnlyList<Guid> BrandIds,
    IReadOnlyList<ProductType> ProductTypes,
    IReadOnlyList<string> Sizes,
    IReadOnlyList<BackingType> Backings,
    IReadOnlyList<PackagingType> PackagingTypes);

public sealed record CatalogueProductListItem(
    Guid Id,
    string Name,
    string Slug,
    ProductType ProductType,
    ProductStatus Status,
    string ManufacturerName,
    string? BrandName,
    IReadOnlyList<string> Sizes,
    IReadOnlyList<BackingType> Backings,
    IReadOnlyList<PackagingType> PackagingTypes,
    IReadOnlyList<CatalogueRetailDestination> RetailDestinations);

public sealed record CatalogueProductSearch(
    IReadOnlyList<CatalogueProductListItem> Products,
    int TotalCount,
    IReadOnlyList<CatalogueFacet> Facets);

public sealed record CatalogueProductVariant(
    Guid Id,
    string Name,
    BackingType BackingType,
    IReadOnlyList<CatalogueProductSize> Sizes);

public sealed record CatalogueProductSize(
    Guid Id,
    string ManufacturerSize,
    int? WaistMinimumCm,
    int? WaistMaximumCm,
    IReadOnlyList<CatalogueProductPack> Packs);

public sealed record CatalogueProductPack(
    Guid Id,
    int QuantityPerPack,
    PackagingType PackagingType,
    IReadOnlyList<string> Gtins);

public sealed record CatalogueProductDetails(
    Guid Id,
    string Name,
    string Slug,
    ProductType ProductType,
    ProductStatus Status,
    string ManufacturerName,
    string? BrandName,
    string? Description,
    string? OfficialWebsiteUrl,
    IReadOnlyList<CatalogueProductVariant> Variants);

public sealed record ExplorerIdentity(
    Guid UserId,
    Guid ExplorerProfileId,
    string Subject,
    string DisplayName);

public sealed record AuthenticatedUser(
    Guid UserId,
    string Subject);

public interface ICurrentExplorer
{
    Task<ExplorerIdentity?> GetAsync(
        CancellationToken cancellationToken = default);
}

public interface ICurrentUser
{
    Task<AuthenticatedUser?> GetAsync(
        CancellationToken cancellationToken = default);
}

public interface IEditorialAuthorisation
{
    Task<bool> CanPublishAtlasAsync(
        AuthenticatedUser user,
        CancellationToken cancellationToken = default);
}

public sealed record ObservationSubmission(
    ObservationType Type,
    DateTimeOffset ObservedAtUtc,
    Guid? ProductId,
    string? CandidateProductName,
    Guid? LocationId,
    string? Narrative);

public sealed record ObservationReceipt(
    Guid Id,
    ObservationState State,
    DateTimeOffset SubmittedAtUtc);

public interface IAtlasQueries
{
    Task<ProductSummary?> GetProductBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<ProductIdentification?> GetProductByGtinAsync(
        string gtin,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CatalogueProductListItem>> SearchProductsAsync(
        string? query,
        int limit,
        CancellationToken cancellationToken = default);

    Task<CatalogueProductSearch> SearchCatalogueAsync(
        string? query,
        CatalogueProductFilters filters,
        string sort,
        int limit,
        CancellationToken cancellationToken = default);

    Task<CatalogueProductDetails?> GetProductDetailsBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);
}

public interface IObservationSubmissions
{
    Task<ObservationReceipt> SubmitAsync(
        ExplorerIdentity explorer,
        ObservationSubmission submission,
        CancellationToken cancellationToken = default);
}

public sealed record CreateCatalogueSubmission(
    CatalogueSubmissionSource Source,
    string ProposedManufacturerName,
    string? ProposedBrandName,
    string ProposedProductName,
    string ProposedVariantName,
    string? Notes);

public sealed record CatalogueSubmissionVariantReceipt(
    Guid Id,
    Guid SubmissionId,
    string Name,
    bool IsStructuralFallback,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record AddCatalogueSubmissionVariant(
    string Name);

public sealed record UpdateCatalogueSubmissionVariant(
    string Name);

public sealed record CatalogueSubmissionVariantsResult(
    CatalogueSubmissionVariantsStatus Status,
    IReadOnlyList<CatalogueSubmissionVariantReceipt>? Variants = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? Message = null)
{
    public static CatalogueSubmissionVariantsResult Found(
        IReadOnlyList<CatalogueSubmissionVariantReceipt> variants) =>
        new(CatalogueSubmissionVariantsStatus.Found, variants);

    public static CatalogueSubmissionVariantsResult Invalid(
        IReadOnlyDictionary<string, string[]> errors) =>
        new(CatalogueSubmissionVariantsStatus.Invalid, Errors: errors);

    public static CatalogueSubmissionVariantsResult AccessDenied() =>
        new(
            CatalogueSubmissionVariantsStatus.AccessDenied,
            Message: "You need Moderator editorial authority to manage the catalogue.");

    public static CatalogueSubmissionVariantsResult Failed() =>
        new(
            CatalogueSubmissionVariantsStatus.Failed,
            Message: "The product variants could not be loaded just now.");
}

public enum CatalogueSubmissionVariantsStatus
{
    Found,
    Invalid,
    AccessDenied,
    Failed
}

public sealed record CatalogueSubmissionReceipt(
    Guid Id,
    CatalogueSubmissionStatus Status,
    CatalogueSubmissionSource Source,
    string ProposedManufacturerName,
    string? ProposedBrandName,
    string ProposedProductName,
    string ProposedVariantName,
    string? ProposedGtin,
    string? ProposedSku,
    string? IdentitySourceUrl,
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
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpdateCatalogueSubmissionIdentity(
    string? ProposedGtin,
    string? ProposedSku,
    string? IdentitySourceUrl);

public sealed record AddCatalogueSubmissionVerification(
    CatalogueVerificationArea Area,
    CatalogueVerificationStatus Status,
    string Scope,
    string Source,
    string? SourceUrl,
    string? Notes,
    string? PermissionTerms);

public sealed record AddCatalogueSubmissionRetailDestination(
    Guid RetailerId,
    string ListingUrl,
    string? Notes);

public sealed record CatalogueSubmissionRetailDestinationReceipt(
    Guid Id,
    Guid SubmissionId,
    Guid RetailerId,
    string ListingUrl,
    string? Notes,
    DateTimeOffset AddedAtUtc);

public sealed record CatalogueRetailerOption(
    Guid Id,
    string Name);

public interface ICatalogueRetailQueries
{
    Task<IReadOnlyList<CatalogueRetailerOption>> GetRetailersAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CatalogueSubmissionRetailDestinationReceipt>> GetDestinationsAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default);
}

public sealed record AddCatalogueSubmissionRetailAffiliate(
    Guid RetailDestinationId,
    AffiliateProgrammeStatus Status,
    string? Network,
    string? TrackingConfiguration,
    string? DeepLinkMechanism,
    string? TermsUrl,
    string? ApplicationReference,
    string? Notes);

public sealed record CatalogueSubmissionRetailWorkspace(
    Guid SubmissionId,
    CatalogueSubmissionStatus Status,
    string ProductName,
    string VariantName,
    IReadOnlyList<CatalogueSubmissionRetailDestinationDetails> Destinations);

public sealed record CatalogueSubmissionRetailDestinationDetails(
    Guid Id,
    Guid RetailerId,
    string RetailerName,
    string ListingUrl,
    string? Notes,
    DateTimeOffset AddedAtUtc,
    CatalogueSubmissionRetailAffiliateReceipt? Affiliate);

public sealed record CatalogueSubmissionRetailAffiliateReceipt(
    Guid Id,
    Guid SubmissionId,
    Guid RetailDestinationId,
    AffiliateProgrammeStatus Status,
    string? Network,
    string? TrackingConfiguration,
    string? DeepLinkMechanism,
    string? TermsUrl,
    string? ApplicationReference,
    string? Notes,
    DateTimeOffset LastVerifiedAtUtc);

public sealed record CatalogueSubmissionVerificationWorkspace(
    Guid SubmissionId,
    CatalogueSubmissionStatus Status,
    string ProductName,
    string VariantName,
    IReadOnlyList<CatalogueSubmissionVerificationReceipt> Verifications);

public sealed record ReviewCatalogueSubmission(
    EditorialOutcome Outcome,
    string? Rationale);

public sealed record CatalogueSubmissionEditorialDecisionReceipt(
    Guid Id,
    Guid SubmissionId,
    Guid ModeratorUserId,
    EditorialOutcome Outcome,
    string? Rationale,
    DateTimeOffset DecidedAtUtc);

public sealed record CatalogueSubmissionVerificationReceipt(
    Guid Id,
    Guid SubmissionId,
    Guid VerifiedByUserId,
    CatalogueVerificationArea Area,
    CatalogueVerificationStatus Status,
    string Scope,
    string Source,
    string? SourceUrl,
    string? Notes,
    string? PermissionTerms,
    DateTimeOffset VerifiedAtUtc);

public sealed record UpdateCatalogueSubmissionSpecifications(
    ProductType? ProposedProductType,
    string? ProposedManufacturerSize,
    int? ProposedWaistMinimumCm,
    int? ProposedWaistMaximumCm,
    BackingType? ProposedBackingType,
    FastenerType? ProposedFastenerType,
    WaistbandStyle? ProposedWaistbandStyle,
    FragranceType? ProposedFragranceType,
    int? ProposedQuantityPerPack,
    PackagingType? ProposedPackagingType);

public interface ICatalogueSubmissions
{
    Task<CatalogueSubmissionReceipt> CreateAsync(
        AuthenticatedUser actor,
        CreateCatalogueSubmission command,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionVariantsResult> GetVariantsAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionVariantReceipt> AddVariantAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        AddCatalogueSubmissionVariant command,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionVariantReceipt> UpdateVariantAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid variantId,
        UpdateCatalogueSubmissionVariant command,
        CancellationToken cancellationToken = default);

    Task RemoveVariantAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid variantId,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionReceipt> UpdateIdentityAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        UpdateCatalogueSubmissionIdentity command,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionReceipt> UpdateSpecificationsAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        UpdateCatalogueSubmissionSpecifications command,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionRetailDestinationReceipt> AddRetailDestinationAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        AddCatalogueSubmissionRetailDestination command,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionRetailAffiliateReceipt> AddAffiliateAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        AddCatalogueSubmissionRetailAffiliate command,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionRetailWorkspace> GetRetailWorkspaceAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionVerificationWorkspace> GetVerificationWorkspaceAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionVerificationReceipt> AddVerificationAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        AddCatalogueSubmissionVerification command,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionEditorialDecisionReceipt> ReviewAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        ReviewCatalogueSubmission command,
        CancellationToken cancellationToken = default);

    Task<CataloguePublicationReceipt> PublishAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionReceipt> BeginVerificationAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionReceipt> MarkReadyForReviewAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default);
}

public sealed record CataloguePublicationReceipt(
    Guid SubmissionId,
    Guid ProductId,
    Guid ProductVariantId,
    Guid SizeVariantId,
    Guid PackTypeId,
    Guid AuditRecordId,
    string? Gtin);

public sealed record CreateCanonicalProduct(
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
    string EditorialRationale,
    string? CorrelationId);

public sealed record CanonicalProductReceipt(
    Guid ProductId,
    Guid ProductVariantId,
    Guid SizeVariantId,
    Guid PackTypeId,
    Guid AuditRecordId,
    string? Gtin);

public interface ICanonicalCatalogue
{
    Task<CanonicalProductReceipt> CreateProductAsync(
        AuthenticatedUser actor,
        CreateCanonicalProduct command,
        CancellationToken cancellationToken = default);
}

public sealed record CatalogueManufacturerOption(
    Guid Id,
    string Name);

public sealed record CatalogueBrandOption(
    Guid Id,
    Guid ManufacturerId,
    string Name);

public sealed record CatalogueEntryOptions(
    IReadOnlyList<CatalogueManufacturerOption> Manufacturers,
    IReadOnlyList<CatalogueBrandOption> Brands);

public interface ICanonicalCatalogueQueries
{
    Task<CatalogueEntryOptions> GetEntryOptionsAsync(
        CancellationToken cancellationToken = default);
}

public sealed record PrivilegedRoleAssignmentReceipt(
    Guid AssignmentId,
    Guid SubjectUserId,
    PrivilegedRole Role,
    bool IsActive);

public interface IPrivilegedRoleAssignments
{
    Task<PrivilegedRoleAssignmentReceipt> GrantAsync(
        AuthenticatedUser actor,
        Guid subjectUserId,
        PrivilegedRole role,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        AuthenticatedUser actor,
        Guid subjectUserId,
        PrivilegedRole role,
        CancellationToken cancellationToken = default);
}

public sealed class CatalogueValidationException(
    string field,
    string message) : Exception(message)
{
    public string Field { get; } = field;
}