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

public sealed record CatalogueProductManagementFilters(
    IReadOnlyList<Guid> ManufacturerIds,
    IReadOnlyList<ProductType> ProductTypes,
    IReadOnlyList<ProductStatus> Statuses);

public sealed record CatalogueProductListItem(
    Guid Id,
    string Name,
    string Slug,
    ProductType ProductType,
    ProductStatus Status,
    string ManufacturerName,
    string? BrandName,
    int VariantCount,
    string? ImageUrl,
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
    FastenerType FastenerType,
    CatalogueVariantAppearance Appearance,
    CatalogueVariantColour PrimaryColour,
    bool? HasWetnessIndicator,
    bool? HasStandingLeakGuards,
    WaistbandStyle WaistbandStyle,
    FragranceType Fragrance,
    bool? IsLatexFree,
    CatalogueVariantDesignedFor DesignedFor,
    int? FastenerCount,
    string? ConstructionNotes,
    IReadOnlyList<CatalogueProductSize> Sizes);

public sealed record CatalogueProductSize(
    Guid Id,
    string ManufacturerSize,
    int? WaistMinimumCm,
    int? WaistMaximumCm,
    int? HipMinimumCm,
    int? HipMaximumCm,
    int? ManufacturerStatedAbsorbencyMl,
    string? FitMeasurementBasis,
    string? AbsorbencyBasisMethod,
    string? AbsorbencySource,
    int? LengthMm,
    int? WidthMm,
    int? WeightGrams,
    IReadOnlyList<CatalogueProductPack> Packs);

public sealed record CatalogueProductPack(
    Guid Id,
    int QuantityPerPack,
    PackagingType PackagingType,
    IReadOnlyList<string> Gtins);

public sealed record CatalogueProductImage(
    Guid Id,
    CatalogueSubmissionImageRole Role,
    bool IsPrimary,
    string ContentUrl);

public sealed record CatalogueModeratorProductImage(
    Guid Id,
    CatalogueSubmissionImageRole Role,
    bool IsPrimary,
    CatalogueContentVisibility Visibility,
    CatalogueImageSourceType SourceType,
    string? SourceUrl,
    string? SourceNotes,
    CatalogueImagePermissionStatus PermissionStatus,
    string? PermissionEvidence,
    string OriginalFileName,
    long FileSizeBytes,
    string ContentUrl);

public sealed record CatalogueModeratorProductDetails(
    Guid Id,
    string Name,
    string Slug,
    ProductType ProductType,
    ProductStatus Status,
    string ManufacturerName,
    string? BrandName,
    string? Description,
    CatalogueContentVisibility DescriptionVisibility,
    string? OfficialWebsiteUrl,
    IReadOnlyList<CatalogueProductVariant> Variants,
    IReadOnlyList<CatalogueModeratorProductImage> Images);

public enum CatalogueDataQualitySeverity
{
    Blocking,
    Warning
}

public sealed record CatalogueDataQualityProduct(
    Guid ProductId,
    string ProductName,
    string ManufacturerName,
    string Detail);

public sealed record CatalogueDataQualityRule(
    string Code,
    string Label,
    string Impact,
    CatalogueDataQualitySeverity Severity,
    int ProductCount,
    IReadOnlyList<CatalogueDataQualityProduct> Products);

public sealed record CatalogueDataQualitySummary(
    int CurrentProductCount,
    int AutomationReadyProductCount,
    int BlockingProductCount,
    int WarningProductCount,
    IReadOnlyList<CatalogueDataQualityRule> Rules);

public sealed record CatalogueProductManagementDetails(
    Guid Id,
    string Name,
    string Slug,
    Guid ManufacturerId,
    Guid? BrandId,
    string ManufacturerName,
    string? BrandName,
    string? ProductFamily,
    ProductType ProductType,
    ProductStatus Status,
    string? Description,
    CatalogueContentVisibility DescriptionVisibility,
    string? OfficialWebsiteUrl,
    IReadOnlyList<CatalogueProductVariant> Variants,
    IReadOnlyList<CatalogueModeratorProductImage> Images);

public sealed record UpdateCanonicalProductIdentity(
    Guid ManufacturerId,
    Guid? BrandId,
    string ProductName,
    ProductType ProductType,
    string? ProductFamily,
    string? Description,
    CatalogueContentVisibility DescriptionVisibility,
    string? OfficialWebsiteUrl,
    string SourceSummary,
    IReadOnlyList<string> SourceReferences,
    string EditorialRationale,
    string? CorrelationId);

public sealed record SetCanonicalProductStatus(
    ProductStatus Status,
    string SourceSummary,
    IReadOnlyList<string> SourceReferences,
    string EditorialRationale,
    string? CorrelationId);

public sealed record CreateCanonicalProductVariantManagement(
    string Name,
    BackingType BackingType,
    FastenerType FastenerType,
    CatalogueVariantAppearance Appearance,
    CatalogueVariantColour PrimaryColour,
    bool? HasWetnessIndicator,
    bool? HasStandingLeakGuards,
    WaistbandStyle WaistbandStyle,
    FragranceType Fragrance,
    bool? IsLatexFree,
    CatalogueVariantDesignedFor DesignedFor,
    int? FastenerCount,
    string? ConstructionNotes,
    string SourceSummary,
    IReadOnlyList<string> SourceReferences,
    string EditorialRationale,
    string? CorrelationId);

public sealed record UpdateCanonicalProductVariantManagement(
    string Name,
    BackingType BackingType,
    FastenerType FastenerType,
    CatalogueVariantAppearance Appearance,
    CatalogueVariantColour PrimaryColour,
    bool? HasWetnessIndicator,
    bool? HasStandingLeakGuards,
    WaistbandStyle WaistbandStyle,
    FragranceType Fragrance,
    bool? IsLatexFree,
    CatalogueVariantDesignedFor DesignedFor,
    int? FastenerCount,
    string? ConstructionNotes,
    string SourceSummary,
    IReadOnlyList<string> SourceReferences,
    string EditorialRationale,
    string? CorrelationId);

public sealed record CreateCanonicalProductSizeManagement(
    string ManufacturerSize,
    int? WaistMinimumCm,
    int? WaistMaximumCm,
    int? HipMinimumCm,
    int? HipMaximumCm,
    int? ManufacturerStatedAbsorbencyMl,
    string? FitMeasurementBasis,
    string? AbsorbencyBasisMethod,
    string? AbsorbencySource,
    int? LengthMm,
    int? WidthMm,
    int? WeightGrams,
    int ManufacturerPackQuantity,
    PackagingType PackagingType,
    string? Gtin,
    string SourceSummary,
    IReadOnlyList<string> SourceReferences,
    string EditorialRationale,
    string? CorrelationId);

public sealed record UpdateCanonicalProductSizeManagement(
    string ManufacturerSize,
    int? WaistMinimumCm,
    int? WaistMaximumCm,
    int? HipMinimumCm,
    int? HipMaximumCm,
    int? ManufacturerStatedAbsorbencyMl,
    string? FitMeasurementBasis,
    string? AbsorbencyBasisMethod,
    string? AbsorbencySource,
    int? LengthMm,
    int? WidthMm,
    int? WeightGrams,
    int? ManufacturerPackQuantity,
    PackagingType? PackagingType,
    string? Gtin,
    string SourceSummary,
    IReadOnlyList<string> SourceReferences,
    string EditorialRationale,
    string? CorrelationId);

public sealed record UpdateCanonicalProductImageMetadata(
    CatalogueSubmissionImageRole Role,
    CatalogueImageSourceType SourceType,
    string? SourceUrl,
    string? SourceNotes,
    CatalogueImagePermissionStatus PermissionStatus,
    string? PermissionEvidence,
    string SourceSummary,
    IReadOnlyList<string> SourceReferences,
    string EditorialRationale,
    string? CorrelationId);

public sealed record AddCanonicalProductImageMetadata(
    CatalogueSubmissionImageRole Role,
    CatalogueImageSourceType SourceType,
    string? SourceUrl,
    string? SourceNotes,
    CatalogueImagePermissionStatus PermissionStatus,
    string? PermissionEvidence,
    bool IsPrimary,
    string SourceSummary,
    IReadOnlyList<string> SourceReferences,
    string EditorialRationale,
    string? CorrelationId);

public sealed record RemoveCanonicalProductElement(
    string SourceSummary,
    IReadOnlyList<string> SourceReferences,
    string EditorialRationale,
    string? CorrelationId);

public sealed record CatalogueRetailOffer(
    Guid Id,
    Guid PackTypeId,
    string ManufacturerSize,
    int QuantityPerPack,
    PackagingType PackagingType,
    string? Gtin,
    string RetailerName,
    string ListingUrl,
    string DestinationUrl,
    string? AffiliateNetwork,
    bool IsAffiliateBacked);

public sealed record CatalogueProductDetails(
    Guid Id,
    string Name,
    string Slug,
    ProductType ProductType,
    ProductStatus Status,
    string ManufacturerName,
    string? BrandName,
    string? Description,
    CatalogueContentVisibility DescriptionVisibility,
    string? OfficialWebsiteUrl,
    IReadOnlyList<CatalogueProductVariant> Variants,
    IReadOnlyList<CatalogueProductImage> Images,
    IReadOnlyList<CatalogueRetailOffer> RetailOffers);

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

    Task<bool> CanManageCatalogueAsync(
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

public sealed record CatalogueProductImageContent(
    Stream Content,
    string ContentType,
    string FileName);

public sealed record RetailerManagementItem(
    Guid Id,
    string Name,
    string Slug,
    string? WebsiteUrl,
    RetailerStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? IdentityVerifiedAtUtc);

public sealed record CreateRetailerManagement(
    string Name,
    string Slug,
    string? WebsiteUrl);

public sealed record UpdateRetailerIdentity(
    string Name,
    string Slug,
    string? WebsiteUrl);

public sealed record RetailerIdentityCheckResults(
    bool WebsiteUrlValid,
    bool SourceUrlValid,
    bool ListingUrlValid,
    bool NameMatches,
    bool DomainMatches)
{
    public bool ReadyToVerify => WebsiteUrlValid && SourceUrlValid && ListingUrlValid && NameMatches && DomainMatches;
}

public sealed record RetailerIdentityVerificationRequest(
    string ObservedRetailerName,
    string SourceUrl,
    string? ListingUrl,
    RetailerIdentityVerificationOutcome Outcome,
    string? Notes);

public sealed record RetailerIdentityVerificationItem(
    Guid Id,
    Guid RetailerId,
    string ObservedRetailerName,
    string SourceUrl,
    string? ListingUrl,
    RetailerIdentityVerificationOutcome Outcome,
    RetailerIdentityCheckResults Checks,
    string? Notes,
    Guid VerifiedByUserId,
    DateTimeOffset VerifiedAtUtc);


public sealed record RetailerAffiliateProgrammeDiscoveryRequest(
    string Network,
    string ProgrammeId,
    string ProgrammeName,
    AffiliateProgrammeStatus Status,
    string? ProgrammeUrl,
    string? TermsUrl,
    string? ReferralTerms,
    int? CookieDurationDays,
    bool? DeepLinksAllowed,
    bool ApplicationRequired,
    string? SourceUrl);

public sealed record RetailerAffiliateProgrammeItem(
    Guid Id,
    Guid RetailerId,
    string Network,
    string ProgrammeId,
    string ProgrammeName,
    AffiliateProgrammeStatus Status,
    string? ProgrammeUrl,
    string? TermsUrl,
    string? ReferralTerms,
    int? CookieDurationDays,
    bool? DeepLinksAllowed,
    bool ApplicationRequired,
    string? SourceUrl,
    DateTimeOffset DiscoveredAtUtc,
    DateTimeOffset LastCheckedAtUtc,
    bool IsPreferred,
    DateTimeOffset? PreferredAtUtc);

public sealed record RetailerAffiliateProgrammeStatusUpdateRequest(
    AffiliateProgrammeStatus Status);

public sealed record RetailerDiscoveryResult(
    string Gtin,
    string RetailerName,
    string RetailerSlug,
    string? RetailerWebsiteUrl,
    string ListingUrl,
    string DiscoveryProvider,
    string? SourceUrl,
    string? ExternalListingId);

public sealed record RetailerProductListingItem(
    Guid Id,
    Guid PackTypeId,
    Guid RetailerId,
    string RetailerName,
    RetailerStatus RetailerStatus,
    string ListingUrl,
    string DiscoveryProvider,
    string? SourceUrl,
    string? ExternalListingId,
    RetailerProductDiscoveryStatus Status,
    DateTimeOffset DiscoveredAtUtc,
    DateTimeOffset LastCheckedAtUtc);

public sealed record RetailerDiscoveryCandidate(
    string Gtin,
    string RetailerName,
    string? RetailerWebsiteUrl,
    string ListingUrl,
    string? ExternalListingId,
    string? SourceUrl);

public interface IRetailerDiscoveryProvider
{
    Task<IReadOnlyList<RetailerDiscoveryCandidate>> DiscoverAsync(
        string gtin,
        CancellationToken cancellationToken = default);
}

public sealed record RetailerDiscoveryRunResult(
    int EligibleGtins,
    int SucceededGtins,
    int DiscoveredListings,
    int FailedGtins);

public interface IRetailerDiscoveryScheduler
{
    Task<RetailerDiscoveryRunResult> RunOnceAsync(
        CancellationToken cancellationToken = default);
}

public interface IRetailerDiscovery
{
    Task<IReadOnlyList<RetailerProductListingItem>> DiscoverAndRecordAsync(
        string gtin,
        CancellationToken cancellationToken = default);

    Task<RetailerProductListingItem> RecordAsync(
        RetailerDiscoveryResult result,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RetailerProductListingItem>> GetForGtinAsync(
        string gtin,
        CancellationToken cancellationToken = default);
}

public interface IRetailerManagement
{
    Task<IReadOnlyList<RetailerManagementItem>> GetAsync(
        AuthenticatedUser actor,
        string? query,
        RetailerStatus? status,
        CancellationToken cancellationToken = default);

    Task<RetailerManagementItem> CreateAsync(
        AuthenticatedUser actor,
        CreateRetailerManagement command,
        CancellationToken cancellationToken = default);

    Task<RetailerManagementItem> UpdateIdentityAsync(
        AuthenticatedUser actor,
        Guid retailerId,
        UpdateRetailerIdentity command,
        CancellationToken cancellationToken = default);

    Task<RetailerIdentityVerificationItem> VerifyIdentityAsync(
        AuthenticatedUser actor,
        Guid retailerId,
        RetailerIdentityVerificationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RetailerIdentityVerificationItem>> GetIdentityVerificationsAsync(
        AuthenticatedUser actor,
        Guid retailerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RetailerAffiliateProgrammeItem>> GetAffiliateProgrammesAsync(
        AuthenticatedUser actor,
        Guid retailerId,
        CancellationToken cancellationToken = default);

    Task<RetailerAffiliateProgrammeItem> RecordAffiliateProgrammeDiscoveryAsync(
        AuthenticatedUser actor,
        Guid retailerId,
        RetailerAffiliateProgrammeDiscoveryRequest request,
        CancellationToken cancellationToken = default);

    Task<RetailerAffiliateProgrammeItem> SelectAffiliateProgrammeAsync(
        AuthenticatedUser actor,
        Guid retailerId,
        Guid programmeId,
        CancellationToken cancellationToken = default);

    Task<RetailerAffiliateProgrammeItem> UpdateAffiliateProgrammeStatusAsync(
        AuthenticatedUser actor,
        Guid retailerId,
        Guid programmeId,
        RetailerAffiliateProgrammeStatusUpdateRequest request,
        CancellationToken cancellationToken = default);
}

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
        int offset = 0,
        CancellationToken cancellationToken = default);

    Task<CatalogueProductSearch> SearchCatalogueManagementAsync(
        string? query,
        CatalogueProductManagementFilters filters,
        string sort,
        int limit,
        int offset = 0,
        CancellationToken cancellationToken = default);

    Task<CatalogueProductDetails?> GetProductDetailsBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<CatalogueModeratorProductDetails?> GetProductDetailsForModeratorAsync(
        AuthenticatedUser actor,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<CatalogueProductManagementDetails?> GetProductManagementDetailsAsync(
        AuthenticatedUser actor,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<CatalogueDataQualitySummary> GetProductDataQualityAsync(
        AuthenticatedUser actor,
        CancellationToken cancellationToken = default);

    Task<CatalogueProductImageContent?> GetProductImageContentAsync(
        Guid productId,
        Guid imageId,
        bool moderatorOnly,
        CancellationToken cancellationToken = default);
}

public interface IObservationSubmissions
{
    Task<ObservationReceipt> SubmitAsync(
        ExplorerIdentity explorer,
        ObservationSubmission submission,
        CancellationToken cancellationToken = default);
}

public sealed record CatalogueSubmissionQueueItem(
    Guid Id,
    CatalogueSubmissionStatus Status,
    string ProposedManufacturerName,
    string? ProposedBrandName,
    string ProposedProductName,
    Guid? PublishedProductId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateCatalogueSubmission(
    CatalogueSubmissionSource Source,
    string ProposedManufacturerName,
    string? ProposedBrandName,
    string ProposedProductName,
    string? ProposedVariantName,
    string? Notes);

public sealed record CatalogueSubmissionVariantReceipt(
    Guid Id,
    Guid SubmissionId,
    string? Name,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CatalogueSubmissionSizeVariantReceipt(
    Guid Id,
    Guid VariantId,
    string ManufacturerSize,
    int? WaistMinimumCm,
    int? WaistMaximumCm,
    int? HipMinimumCm,
    int? HipMaximumCm,
    int? ManufacturerStatedAbsorbencyMl,
    string? FitMeasurementBasis,
    string? AbsorbencyBasisMethod,
    string? AbsorbencySource,
    int? LengthMm,
    int? WidthMm,
    int? WeightGrams,
    int? ManufacturerPackQuantity,
    string? Gtin,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record AddCatalogueSubmissionSizeVariantRequest(
    string ManufacturerSize,
    int? WaistMinimumCm,
    int? WaistMaximumCm,
    int? HipMinimumCm,
    int? HipMaximumCm,
    int? ManufacturerStatedAbsorbencyMl,
    string? FitMeasurementBasis,
    string? AbsorbencyBasisMethod,
    string? AbsorbencySource,
    int? LengthMm,
    int? WidthMm,
    int? WeightGrams,
    int? ManufacturerPackQuantity,
    string? Gtin);

public sealed record UpdateCatalogueSubmissionSizeVariantRequest(
    string ManufacturerSize,
    int? WaistMinimumCm,
    int? WaistMaximumCm,
    int? HipMinimumCm,
    int? HipMaximumCm,
    int? ManufacturerStatedAbsorbencyMl,
    string? FitMeasurementBasis,
    string? AbsorbencyBasisMethod,
    string? AbsorbencySource,
    int? LengthMm,
    int? WidthMm,
    int? WeightGrams,
    int? ManufacturerPackQuantity,
    string? Gtin);

public sealed record AddCatalogueSubmissionSizeVariant(
    string ManufacturerSize,
    int? WaistMinimumCm,
    int? WaistMaximumCm,
    int? HipMinimumCm,
    int? HipMaximumCm,
    int? ManufacturerStatedAbsorbencyMl,
    string? FitMeasurementBasis,
    string? AbsorbencyBasisMethod,
    string? AbsorbencySource,
    int? LengthMm,
    int? WidthMm,
    int? WeightGrams,
    int? ManufacturerPackQuantity,
    string? Gtin);

public sealed record UpdateCatalogueSubmissionSizeVariant(
    string ManufacturerSize,
    int? WaistMinimumCm,
    int? WaistMaximumCm,
    int? HipMinimumCm,
    int? HipMaximumCm,
    int? ManufacturerStatedAbsorbencyMl,
    string? FitMeasurementBasis,
    string? AbsorbencyBasisMethod,
    string? AbsorbencySource,
    int? LengthMm,
    int? WidthMm,
    int? WeightGrams,
    int? ManufacturerPackQuantity,
    string? Gtin);

public sealed record CatalogueSubmissionSizeVariantsResult(
    CatalogueSubmissionSizeVariantsStatus Status,
    IReadOnlyList<CatalogueSubmissionSizeVariantReceipt>? Sizes = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? Message = null)
{
    public static CatalogueSubmissionSizeVariantsResult Found(
        IReadOnlyList<CatalogueSubmissionSizeVariantReceipt> sizes) =>
        new(CatalogueSubmissionSizeVariantsStatus.Found, sizes);

    public static CatalogueSubmissionSizeVariantsResult Invalid(
        IReadOnlyDictionary<string, string[]> errors) =>
        new(CatalogueSubmissionSizeVariantsStatus.Invalid, Errors: errors);

    public static CatalogueSubmissionSizeVariantsResult AccessDenied() =>
        new(CatalogueSubmissionSizeVariantsStatus.AccessDenied,
            Message: "You need Moderator editorial authority to manage the catalogue.");

    public static CatalogueSubmissionSizeVariantsResult Failed() =>
        new(CatalogueSubmissionSizeVariantsStatus.Failed,
            Message: "The size variants could not be loaded just now.");
}

public enum CatalogueSubmissionSizeVariantsStatus
{
    Found,
    Invalid,
    AccessDenied,
    Failed
}

public sealed record CatalogueSubmissionSizeVariantResult(
    CatalogueSubmissionSizeVariantResultStatus Status,
    CatalogueSubmissionSizeVariantReceipt? Size = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    string? Message = null)
{
    public static CatalogueSubmissionSizeVariantResult Saved(CatalogueSubmissionSizeVariantReceipt size) =>
        new(CatalogueSubmissionSizeVariantResultStatus.Saved, size);

    public static CatalogueSubmissionSizeVariantResult Removed() =>
        new(CatalogueSubmissionSizeVariantResultStatus.Removed);

    public static CatalogueSubmissionSizeVariantResult Invalid(
        IReadOnlyDictionary<string, string[]> errors) =>
        new(CatalogueSubmissionSizeVariantResultStatus.Invalid, Errors: errors);

    public static CatalogueSubmissionSizeVariantResult AccessDenied() =>
        new(CatalogueSubmissionSizeVariantResultStatus.AccessDenied,
            Message: "You need Moderator editorial authority to manage the catalogue.");

    public static CatalogueSubmissionSizeVariantResult Failed() =>
        new(CatalogueSubmissionSizeVariantResultStatus.Failed,
            Message: "The size variant could not be saved.");
}

public enum CatalogueSubmissionSizeVariantResultStatus
{
    Saved,
    Removed,
    Invalid,
    AccessDenied,
    Failed
}

public sealed record CatalogueSubmissionVariantOverrideReceipt(
    Guid VariantId,
    BackingType? BackingType,
    FastenerType? FastenerType,
    CatalogueVariantAppearance? Appearance,
    CatalogueVariantColour? PrimaryColour,
    bool? HasWetnessIndicator,
    bool? HasStandingLeakGuards,
    WaistbandStyle? WaistbandStyle,
    FragranceType? Fragrance,
    bool? IsLatexFree,
    CatalogueVariantDesignedFor? DesignedFor,
    int? FastenerCount,
    string? ConstructionNotes);

public sealed record UpdateCatalogueSubmissionVariantOverride(
    CatalogueVariantOverrideAttribute Attribute,
    string? Value);

public sealed record AddCatalogueSubmissionVariant(
    string? Name);

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
    string? ProposedVariantName,
    string? ProposedGtin,
    string? ProposedSku,
    string? IdentitySourceUrl,
    ProductType? ProposedProductType,
    string? ProposedProductFamily,
    string? ProposedDescription,
    CatalogueContentVisibility ProposedDescriptionVisibility,
    ProductStatus? ProposedProductStatus,
    string? ProposedOfficialWebsiteUrl,
    PackagingType? ProposedPackagingType,
    CatalogueVariantAppearance? SharedAppearance,
    string? SharedPrimaryColour,
    bool? SharedWetnessIndicator,
    bool? SharedStandingLeakGuards,
    WaistbandStyle? SharedWaistbandStyle,
    FragranceType? SharedFragrance,
    bool? SharedLatexFree,
    string? SharedDesignedFor,
    int? SharedFastenerCount,
    string? SharedConstructionNotes,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpdateCatalogueSubmissionIdentity(
    string? ProposedGtin,
    string? ProposedSku,
    string? IdentitySourceUrl);

public sealed record ResolveCatalogueSubmissionEntities(
    Guid? ManufacturerId,
    string? NewManufacturerName,
    Guid? BrandId,
    string? NewBrandName);

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

public sealed record AddCatalogueSubmissionImage(
    CatalogueSubmissionImageRole Role,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    Stream Content,
    CatalogueImageSourceType SourceType,
    string? SourceUrl,
    string? SourceNotes,
    CatalogueImagePermissionStatus PermissionStatus,
    string? PermissionEvidence);

public sealed record UpdateCatalogueSubmissionImageMetadata(
    CatalogueImageSourceType SourceType,
    string? SourceUrl,
    string? SourceNotes,
    CatalogueImagePermissionStatus PermissionStatus,
    string? PermissionEvidence);

public sealed record CatalogueSubmissionImageReceipt(
    Guid Id,
    Guid SubmissionId,
    CatalogueSubmissionImageRole Role,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    CatalogueImageSourceType SourceType,
    string? SourceUrl,
    string? SourceNotes,
    CatalogueImagePermissionStatus PermissionStatus,
    string? PermissionEvidence,
    CatalogueContentVisibility Visibility,
    string ContentUrl,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CatalogueSubmissionImagesWorkspace(
    Guid SubmissionId,
    IReadOnlyList<CatalogueSubmissionImageReceipt> Images);

public sealed record CatalogueSubmissionImageContent(
    Stream Content,
    string ContentType,
    string FileName);

public enum CatalogueSubmissionImagesResultStatus
{
    Found,
    AccessDenied,
    NotFound,
    Failed
}

public sealed record CatalogueSubmissionImagesResult(
    CatalogueSubmissionImagesResultStatus Status,
    IReadOnlyList<CatalogueSubmissionImageReceipt>? Images = null)
{
    public static CatalogueSubmissionImagesResult Found(IReadOnlyList<CatalogueSubmissionImageReceipt> images) =>
        new(CatalogueSubmissionImagesResultStatus.Found, images);

    public static CatalogueSubmissionImagesResult AccessDenied() =>
        new(CatalogueSubmissionImagesResultStatus.AccessDenied);

    public static CatalogueSubmissionImagesResult NotFound() =>
        new(CatalogueSubmissionImagesResultStatus.NotFound);

    public static CatalogueSubmissionImagesResult Failed() =>
        new(CatalogueSubmissionImagesResultStatus.Failed);
}

public enum CatalogueSubmissionImageResultStatus
{
    Saved,
    Invalid,
    AccessDenied,
    NotFound,
    Failed
}

public sealed record CatalogueSubmissionImageResult(
    CatalogueSubmissionImageResultStatus Status,
    CatalogueSubmissionImageReceipt? Image = null,
    IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public static CatalogueSubmissionImageResult Saved(CatalogueSubmissionImageReceipt image) =>
        new(CatalogueSubmissionImageResultStatus.Saved, image);

    public static CatalogueSubmissionImageResult Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(CatalogueSubmissionImageResultStatus.Invalid, Errors: errors);

    public static CatalogueSubmissionImageResult AccessDenied() =>
        new(CatalogueSubmissionImageResultStatus.AccessDenied);

    public static CatalogueSubmissionImageResult NotFound() =>
        new(CatalogueSubmissionImageResultStatus.NotFound);

    public static CatalogueSubmissionImageResult Failed() =>
        new(CatalogueSubmissionImageResultStatus.Failed);
}

public interface ICatalogueSubmissionImageStorage
{
    Task SaveAsync(
        string storageKey,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}

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
    PackagingType? ProposedPackagingType,
    string? ProposedProductFamily,
    string? ProposedDescription,
    CatalogueContentVisibility ProposedDescriptionVisibility,
    ProductStatus? ProposedProductStatus,
    string? ProposedOfficialWebsiteUrl,
    CatalogueVariantAppearance? SharedAppearance,
    string? SharedPrimaryColour,
    bool? SharedWetnessIndicator,
    bool? SharedStandingLeakGuards,
    WaistbandStyle? SharedWaistbandStyle,
    FragranceType? SharedFragrance,
    bool? SharedLatexFree,
    string? SharedDesignedFor,
    int? SharedFastenerCount,
    string? SharedConstructionNotes);

public sealed record CatalogueSubmissionImportResult(
    int RowsRead,
    int SubmissionsCreated,
    int RowsImported,
    int RowsSkipped,
    IReadOnlyList<string> Warnings);

public sealed record CatalogueSubmissionImportOptions(
    bool TreatImportedDescriptionsAsModeratorOnly = true);

public interface ICatalogueSubmissions
{
    Task<IReadOnlyList<CatalogueSubmissionQueueItem>> GetSubmissionsAsync(
        AuthenticatedUser actor,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionImportResult> ImportCsvAsync(
        AuthenticatedUser actor,
        Stream csvContent,
        CatalogueSubmissionImportOptions options,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionReceipt> CreateAsync(
        AuthenticatedUser actor,
        CreateCatalogueSubmission command,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionReceipt> GetAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionVariantsResult> GetVariantsAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionSizeVariantsResult> GetSizeVariantsAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid variantId,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionSizeVariantReceipt> AddSizeVariantAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid variantId,
        AddCatalogueSubmissionSizeVariant command,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionSizeVariantReceipt> UpdateSizeVariantAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid variantId,
        Guid sizeVariantId,
        UpdateCatalogueSubmissionSizeVariant command,
        CancellationToken cancellationToken = default);

    Task RemoveSizeVariantAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid variantId,
        Guid sizeVariantId,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionVariantOverrideReceipt?> GetVariantOverrideAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid variantId,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionVariantOverrideReceipt> UpdateVariantOverrideAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid variantId,
        UpdateCatalogueSubmissionVariantOverride command,
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

    Task<CatalogueSubmissionReceipt> ResolveEntitiesAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        ResolveCatalogueSubmissionEntities command,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionReceipt> UpdateSpecificationsAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        UpdateCatalogueSubmissionSpecifications command,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionReceipt> UpdateDescriptionVisibilityAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CatalogueContentVisibility visibility,
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

    Task<CatalogueSubmissionImagesWorkspace> GetImagesAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionImageReceipt> AddImageAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        AddCatalogueSubmissionImage command,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionImageReceipt> UpdateImageMetadataAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid imageId,
        UpdateCatalogueSubmissionImageMetadata command,
        CancellationToken cancellationToken = default);

    Task RemoveImageAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid imageId,
        CancellationToken cancellationToken = default);

    Task<CatalogueSubmissionImageContent?> GetImageContentAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid imageId,
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

    Task<CatalogueSubmissionReceipt> ReturnToVerificationAsync(
        AuthenticatedUser actor,
        Guid submissionId,
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

public sealed record CreateCanonicalProductSizeVariant(
    string ManufacturerSize,
    int? WaistMinimumCm,
    int? WaistMaximumCm,
    int? HipMinimumCm,
    int? HipMaximumCm,
    int? ManufacturerStatedAbsorbencyMl,
    string? FitMeasurementBasis,
    string? AbsorbencyBasisMethod,
    string? AbsorbencySource,
    int? LengthMm,
    int? WidthMm,
    int? WeightGrams,
    int ManufacturerPackQuantity,
    PackagingType PackagingType,
    string? Gtin = null);

public sealed record CreateCanonicalProductVariant(
    string Name,
    BackingType BackingType,
    FastenerType FastenerType = FastenerType.Unknown,
    CatalogueVariantAppearance? Appearance = null,
    string? PrimaryColour = null,
    bool? HasWetnessIndicator = null,
    bool? HasStandingLeakGuards = null,
    WaistbandStyle WaistbandStyle = WaistbandStyle.Unknown,
    FragranceType Fragrance = FragranceType.Unknown,
    bool? IsLatexFree = null,
    string? DesignedFor = null,
    int? FastenerCount = null,
    string? ConstructionNotes = null,
    IReadOnlyList<CreateCanonicalProductSizeVariant>? Sizes = null);

public sealed record CreateCanonicalProduct(
    Guid ManufacturerId,
    Guid? BrandId,
    string ProductName,
    string ProductSlug,
    ProductType ProductType,
    ProductStatus Status,
    IReadOnlyList<CreateCanonicalProductVariant> Variants,
    string SourceSummary,
    IReadOnlyList<string> SourceReferences,
    string EditorialRationale,
    string? CorrelationId,
    string? ProductFamily = null,
    string? Description = null,
    string? OfficialWebsiteUrl = null,
    CatalogueContentVisibility DescriptionVisibility = CatalogueContentVisibility.Public);

public sealed record CanonicalProductReceipt(
    Guid ProductId,
    Guid ProductVariantId,
    Guid SizeVariantId,
    Guid PackTypeId,
    Guid AuditRecordId,
    string? Gtin);

public interface ICanonicalCatalogue
{
    Task SetDescriptionVisibilityAsync(
        AuthenticatedUser actor,
        Guid productId,
        CatalogueContentVisibility visibility,
        CancellationToken cancellationToken = default);

    Task UpdateProductIdentityAsync(
        AuthenticatedUser actor,
        Guid productId,
        UpdateCanonicalProductIdentity command,
        CancellationToken cancellationToken = default);

    Task SetProductStatusAsync(
        AuthenticatedUser actor,
        Guid productId,
        SetCanonicalProductStatus command,
        CancellationToken cancellationToken = default);

    Task AddProductVariantAsync(
        AuthenticatedUser actor,
        Guid productId,
        CreateCanonicalProductVariantManagement command,
        CancellationToken cancellationToken = default);

    Task UpdateProductVariantAsync(
        AuthenticatedUser actor,
        Guid productId,
        Guid variantId,
        UpdateCanonicalProductVariantManagement command,
        CancellationToken cancellationToken = default);

    Task RemoveProductVariantAsync(
        AuthenticatedUser actor,
        Guid productId,
        Guid variantId,
        string sourceSummary,
        IReadOnlyList<string> sourceReferences,
        string editorialRationale,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task AddProductSizeAsync(
        AuthenticatedUser actor,
        Guid productId,
        Guid variantId,
        CreateCanonicalProductSizeManagement command,
        CancellationToken cancellationToken = default);

    Task UpdateProductSizeAsync(
        AuthenticatedUser actor,
        Guid productId,
        Guid variantId,
        Guid sizeId,
        UpdateCanonicalProductSizeManagement command,
        CancellationToken cancellationToken = default);

    Task RemoveProductSizeAsync(
        AuthenticatedUser actor,
        Guid productId,
        Guid variantId,
        Guid sizeId,
        string sourceSummary,
        IReadOnlyList<string> sourceReferences,
        string editorialRationale,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task<CatalogueModeratorProductImage> AddProductImageAsync(
        AuthenticatedUser actor,
        Guid productId,
        string storageKey,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        AddCanonicalProductImageMetadata command,
        CancellationToken cancellationToken = default);

    Task<CatalogueModeratorProductImage> UpdateProductImageAsync(
        AuthenticatedUser actor,
        Guid productId,
        Guid imageId,
        UpdateCanonicalProductImageMetadata command,
        CancellationToken cancellationToken = default);

    Task RemoveProductImageAsync(
        AuthenticatedUser actor,
        Guid productId,
        Guid imageId,
        string sourceSummary,
        IReadOnlyList<string> sourceReferences,
        string editorialRationale,
        string? correlationId,
        CancellationToken cancellationToken = default);

    Task SetProductImagePrimaryAsync(
        AuthenticatedUser actor,
        Guid productId,
        Guid imageId,
        string sourceSummary,
        IReadOnlyList<string> sourceReferences,
        string editorialRationale,
        string? correlationId,
        CancellationToken cancellationToken = default);

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