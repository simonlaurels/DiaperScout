namespace DiaperScout.Domain;

public abstract class Entity
{
    protected Entity() => Id = Guid.NewGuid();
    public Guid Id { get; private set; }
}

public enum ProductType { Tape, PullUp, Pad, Booster, AllInOne, Other }
public enum ProductStatus { Current, Discontinued, Prototype }
public enum RetailerStatus { Discovered, Verified, NeedsReview, Inactive }
public enum RetailerIdentityVerificationOutcome { Verified, NeedsReview }
public enum BackingType { Unknown, Plastic, Cloth, Hybrid, Other }
public enum FastenerType { Unknown, AdhesiveTape, HookAndLoop, Other }
public enum CatalogueVariantAppearance { Unknown, Plain, Printed }
public enum CatalogueVariantColour { Unknown, White, Black, Grey, Silver, Beige, Brown, Red, Orange, Yellow, Green, Blue, Purple, Pink, Clear, Multicolour, Other }
public enum CatalogueVariantDesignedFor { Unknown, Baby, Child, Youth, Adult, Unisex, Other }
public enum WaistbandStyle { Unknown, NoElasticWaistband, FrontElastic, RearElastic, FrontAndRearElastic, AllAroundElastic, Other }
public enum FragranceType { Unknown, None, Fragranced }
public enum PackagingType { Bag, Box, Case }
public enum IdentifierType { Gtin, Other }
public enum UserAccountStatus { Active, Suspended, Anonymised }
public enum PrivilegedRole { Moderator, Administrator }
public enum PrivilegedRoleAssignmentAction { Granted, Revoked }
public enum CatalogueAuditAction { ProductCreated, ProductChanged }
public enum ObservationType { FieldResearch, DeskResearch, ProductExperience, RetailAvailability, CorrectionRequest, ManufacturerSubmission }
public enum ObservationState { Draft, Submitted, UnderReview, Accepted, Rejected, Deferred }
public enum EvidenceType { Photograph, BarcodeImage, Document, Measurement, Note, Packaging }
public enum EditorialOutcome { Accepted, Rejected, RequestAdditionalEvidence, Deferred }
public enum KnowledgeGapType { Availability, NewProduct, ConflictingEvidence, Correction, RegionalVariation, Specification }
public enum DiscoveryTaskState { Open, Accepted, Resolved, PartiallyResolved, Unresolved, Invalid, Closed }
public enum CatalogueVariantOverrideAttribute { BackingType, FastenerType, Appearance, PrimaryColour, WetnessIndicator, StandingLeakGuards, WaistbandStyle, Fragrance, LatexFree, DesignedFor, FastenerCount, ConstructionNotes }

public enum CatalogueSubmissionStatus
{
    Draft,
    InVerification,
    ReadyForReview,
    Approved,
    Published,
    Rejected,
    NeedsChanges
}

public enum CatalogueSubmissionSource
{
    Moderator,
    Explorer,
    Manufacturer,
    BulkImport,
    Other
}

public enum CatalogueContentVisibility
{
    Public,
    ModeratorOnly
}

public enum CatalogueVerificationArea
{
    ProductIdentity,
    Specifications,
    ContentAndRights,
    Retail,
    Affiliate
}

public enum CatalogueVerificationStatus
{
    Verified,
    Inherited,
    Exception
}

public enum CatalogueSubmissionImageRole
{
    PackFront,
    PackBack,
    ProductFront,
    ProductRear,
    ProductInterior,
    ProductDetail,
    SizeMeasurement,
    Other
}

public enum CatalogueImageSourceType
{
    Unknown,
    Manufacturer,
    Retailer,
    OfficialProductWebsite,
    UserCommunity,
    Other
}

public enum CatalogueImagePermissionStatus
{
    Unknown,
    PermissionGranted,
    PermissionNotRequired,
    PermissionRequested,
    PermissionDenied
}

public enum AffiliateProgrammeStatus
{
    NotInvestigated,
    NoAffiliateProgramme,
    ProgrammeAvailable,
    ApplicationRequired,
    ApplicationSubmitted,
    Approved,
    Configured,
    NotApplicable
}

public sealed class Manufacturer : Entity
{
    private Manufacturer() { Name = null!; Slug = null!; }
    public Manufacturer(string name, string slug, string? websiteUrl = null) { Name = name; Slug = slug; WebsiteUrl = websiteUrl; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public ICollection<Brand> Brands { get; } = new List<Brand>();
}

public sealed class Brand : Entity
{
    private Brand() { Name = null!; Slug = null!; }
    public Brand(Guid manufacturerId, string name, string slug) { ManufacturerId = manufacturerId; Name = name; Slug = slug; }
    public Guid ManufacturerId { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
}

public sealed class Product : Entity
{
    private Product() { Name = null!; Slug = null!; }
    public Product(
        Guid manufacturerId,
        Guid? brandId,
        string name,
        string slug,
        ProductType type,
        ProductStatus status = ProductStatus.Current,
        string? family = null,
        string? description = null,
        string? officialWebsiteUrl = null,
        CatalogueContentVisibility descriptionVisibility = CatalogueContentVisibility.Public)
    {
        ManufacturerId = manufacturerId;
        BrandId = brandId;
        Name = name;
        Slug = slug;
        ProductType = type;
        Status = status;
        Family = string.IsNullOrWhiteSpace(family) ? null : family.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        OfficialWebsiteUrl = string.IsNullOrWhiteSpace(officialWebsiteUrl) ? null : officialWebsiteUrl.Trim();
        DescriptionVisibility = descriptionVisibility;
    }
    public Guid ManufacturerId { get; private set; }
    public Guid? BrandId { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? Family { get; private set; }
    public ProductType ProductType { get; private set; }
    public ProductStatus Status { get; private set; }
    public string? Description { get; private set; }
    public CatalogueContentVisibility DescriptionVisibility { get; private set; }
    public string? OfficialWebsiteUrl { get; private set; }
    public ICollection<ProductVariant> Variants { get; } = new List<ProductVariant>();

    public void SetDescriptionVisibility(CatalogueContentVisibility visibility) =>
        DescriptionVisibility = visibility;

    public void SetStatus(ProductStatus status)
    {
        if (!Enum.IsDefined(status))
            throw new ArgumentException("The product status is invalid.", nameof(status));

        Status = status;
    }

    public void UpdateIdentity(
        Guid manufacturerId,
        Guid? brandId,
        string name,
        ProductType productType,
        string? family,
        string? description,
        CatalogueContentVisibility descriptionVisibility,
        string? officialWebsiteUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A product name is required.", nameof(name));
        if (!Enum.IsDefined(productType))
            throw new ArgumentException("The product type is invalid.", nameof(productType));
        if (!Enum.IsDefined(descriptionVisibility))
            throw new ArgumentException("The description visibility is invalid.", nameof(descriptionVisibility));

        ManufacturerId = manufacturerId;
        BrandId = brandId;
        Name = name.Trim();
        ProductType = productType;
        Family = string.IsNullOrWhiteSpace(family) ? null : family.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        DescriptionVisibility = descriptionVisibility;
        OfficialWebsiteUrl = string.IsNullOrWhiteSpace(officialWebsiteUrl) ? null : officialWebsiteUrl.Trim();
    }
}

public sealed class ProductVariant : Entity
{
    private ProductVariant() { Name = null!; }
    public ProductVariant(
        Guid productId,
        string name,
        BackingType backingType = BackingType.Unknown,
        FastenerType fastenerType = FastenerType.Unknown,
        CatalogueVariantAppearance? appearance = null,
        string? primaryColour = null,
        bool? hasWetnessIndicator = null,
        bool? hasStandingLeakGuards = null,
        WaistbandStyle waistbandStyle = WaistbandStyle.Unknown,
        FragranceType fragrance = FragranceType.Unknown,
        bool? isLatexFree = null,
        string? designedFor = null,
        int? fastenerCount = null,
        string? constructionNotes = null)
    {
        ProductId = productId;
        Name = name;
        BackingType = backingType;
        FastenerType = fastenerType;
        PrintDesign = appearance?.ToString();
        PrimaryColour = string.IsNullOrWhiteSpace(primaryColour) ? null : primaryColour.Trim();
        HasWetnessIndicator = hasWetnessIndicator;
        HasStandingLeakGuards = hasStandingLeakGuards;
        WaistbandStyle = waistbandStyle;
        Fragrance = fragrance;
        IsLatexFree = isLatexFree;
        DesignedFor = string.IsNullOrWhiteSpace(designedFor) ? null : designedFor.Trim();
        FastenerCount = fastenerCount;
        ConstructionNotes = string.IsNullOrWhiteSpace(constructionNotes) ? null : constructionNotes.Trim();
    }
    public Guid ProductId { get; private set; }
    public string Name { get; private set; }
    public BackingType BackingType { get; private set; }
    public FastenerType FastenerType { get; private set; }
    public string? PrintDesign { get; private set; }
    public string? PrimaryColour { get; private set; }
    public bool? HasWetnessIndicator { get; private set; }
    public bool? HasStandingLeakGuards { get; private set; }
    public WaistbandStyle WaistbandStyle { get; private set; }
    public FragranceType Fragrance { get; private set; }
    public bool? IsLatexFree { get; private set; }
    public string? DesignedFor { get; private set; }
    public int? FastenerCount { get; private set; }
    public string? ConstructionNotes { get; private set; }
    public ICollection<SizeVariant> Sizes { get; } = new List<SizeVariant>();

    public void UpdateDetails(
        string name,
        BackingType backingType,
        FastenerType fastenerType,
        CatalogueVariantAppearance appearance,
        CatalogueVariantColour primaryColour,
        bool? hasWetnessIndicator,
        bool? hasStandingLeakGuards,
        WaistbandStyle waistbandStyle,
        FragranceType fragrance,
        bool? isLatexFree,
        CatalogueVariantDesignedFor designedFor,
        int? fastenerCount,
        string? constructionNotes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A variant name is required.", nameof(name));
        if (name.Trim().Length > 200)
            throw new ArgumentException("Variant name must be 200 characters or fewer.", nameof(name));
        if (!Enum.IsDefined(backingType) || !Enum.IsDefined(fastenerType) || !Enum.IsDefined(appearance) ||
            !Enum.IsDefined(primaryColour) || !Enum.IsDefined(waistbandStyle) || !Enum.IsDefined(fragrance) ||
            !Enum.IsDefined(designedFor))
            throw new ArgumentException("One or more variant specification values are invalid.");
        if (fastenerCount is < 0)
            throw new ArgumentException("Fastener count cannot be negative.", nameof(fastenerCount));

        Name = name.Trim();
        BackingType = backingType;
        FastenerType = fastenerType;
        PrintDesign = appearance.ToString();
        PrimaryColour = primaryColour.ToString();
        HasWetnessIndicator = hasWetnessIndicator;
        HasStandingLeakGuards = hasStandingLeakGuards;
        WaistbandStyle = waistbandStyle;
        Fragrance = fragrance;
        IsLatexFree = isLatexFree;
        DesignedFor = designedFor.ToString();
        FastenerCount = fastenerCount;
        ConstructionNotes = string.IsNullOrWhiteSpace(constructionNotes) ? null : constructionNotes.Trim();
    }
}

public sealed class SizeVariant : Entity
{
    private SizeVariant() { ManufacturerSize = null!; }

    public SizeVariant(
        Guid productVariantId,
        string manufacturerSize,
        int? waistMinimumCm = null,
        int? waistMaximumCm = null,
        int? hipMinimumCm = null,
        int? hipMaximumCm = null,
        int? manufacturerStatedAbsorbencyMl = null,
        string? fitMeasurementBasis = null,
        string? absorbencyBasisMethod = null,
        string? absorbencySource = null,
        int? lengthMm = null,
        int? widthMm = null,
        int? weightGrams = null)
    {
        if (productVariantId == Guid.Empty)
            throw new ArgumentException("A product variant is required.", nameof(productVariantId));

        if (string.IsNullOrWhiteSpace(manufacturerSize))
            throw new ArgumentException("A manufacturer size is required.", nameof(manufacturerSize));

        if (manufacturerSize.Trim().Length > 100)
            throw new ArgumentException("Manufacturer size must be 100 characters or fewer.", nameof(manufacturerSize));

        ValidateRange(waistMinimumCm, waistMaximumCm, "waist");
        ValidateRange(hipMinimumCm, hipMaximumCm, "hip");
        ValidateNonNegative(manufacturerStatedAbsorbencyMl, nameof(manufacturerStatedAbsorbencyMl));
        ValidateNonNegative(lengthMm, nameof(lengthMm));
        ValidateNonNegative(widthMm, nameof(widthMm));
        ValidateNonNegative(weightGrams, nameof(weightGrams));

        ProductVariantId = productVariantId;
        ManufacturerSize = manufacturerSize.Trim();
        WaistMinimumCm = waistMinimumCm;
        WaistMaximumCm = waistMaximumCm;
        HipMinimumCm = hipMinimumCm;
        HipMaximumCm = hipMaximumCm;
        FitMeasurementBasis = NormaliseText(fitMeasurementBasis);
        AbsorbencyBasisMethod = NormaliseText(absorbencyBasisMethod);
        AbsorbencySource = NormaliseText(absorbencySource);
        ManufacturerStatedAbsorbencyMl = manufacturerStatedAbsorbencyMl;
        LengthMm = lengthMm;
        WidthMm = widthMm;
        WeightGrams = weightGrams;
    }

    public Guid ProductVariantId { get; private set; }
    public string ManufacturerSize { get; private set; }
    public int? WaistMinimumCm { get; private set; }
    public int? WaistMaximumCm { get; private set; }
    public int? HipMinimumCm { get; private set; }
    public int? HipMaximumCm { get; private set; }
    public int? ManufacturerStatedAbsorbencyMl { get; private set; }
    public string? FitMeasurementBasis { get; private set; }
    public string? AbsorbencyBasisMethod { get; private set; }
    public string? AbsorbencySource { get; private set; }
    public int? LengthMm { get; private set; }
    public int? WidthMm { get; private set; }
    public int? WeightGrams { get; private set; }
    public ICollection<PackType> PackTypes { get; } = new List<PackType>();

    public void UpdateMeasurements(
        string manufacturerSize,
        int? waistMinimumCm,
        int? waistMaximumCm,
        int? hipMinimumCm,
        int? hipMaximumCm,
        int? manufacturerStatedAbsorbencyMl,
        string? fitMeasurementBasis,
        string? absorbencyBasisMethod,
        string? absorbencySource,
        int? lengthMm,
        int? widthMm,
        int? weightGrams)
    {
        if (string.IsNullOrWhiteSpace(manufacturerSize))
            throw new ArgumentException("A manufacturer size is required.", nameof(manufacturerSize));

        if (manufacturerSize.Trim().Length > 100)
            throw new ArgumentException("Manufacturer size must be 100 characters or fewer.", nameof(manufacturerSize));

        ValidateRange(waistMinimumCm, waistMaximumCm, "waist");
        ValidateRange(hipMinimumCm, hipMaximumCm, "hip");
        ValidateNonNegative(manufacturerStatedAbsorbencyMl, nameof(manufacturerStatedAbsorbencyMl));
        ValidateNonNegative(lengthMm, nameof(lengthMm));
        ValidateNonNegative(widthMm, nameof(widthMm));
        ValidateNonNegative(weightGrams, nameof(weightGrams));

        ManufacturerSize = manufacturerSize.Trim();
        WaistMinimumCm = waistMinimumCm;
        WaistMaximumCm = waistMaximumCm;
        HipMinimumCm = hipMinimumCm;
        HipMaximumCm = hipMaximumCm;
        FitMeasurementBasis = NormaliseText(fitMeasurementBasis);
        AbsorbencyBasisMethod = NormaliseText(absorbencyBasisMethod);
        AbsorbencySource = NormaliseText(absorbencySource);
        ManufacturerStatedAbsorbencyMl = manufacturerStatedAbsorbencyMl;
        LengthMm = lengthMm;
        WidthMm = widthMm;
        WeightGrams = weightGrams;
    }

    private static string? NormaliseText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateRange(int? minimum, int? maximum, string name)
    {
        if (minimum is < 0 || maximum is < 0)
            throw new ArgumentException($"{name} measurements cannot be negative.", name);

        if (minimum.HasValue && maximum.HasValue && minimum > maximum)
            throw new ArgumentException($"{name} minimum cannot be greater than maximum.", name);
    }

    private static void ValidateNonNegative(int? value, string name)
    {
        if (value is < 0)
            throw new ArgumentException($"{name} cannot be negative.", name);
    }
}

public sealed class PackType : Entity
{
    private PackType() { }
    public PackType(Guid sizeVariantId, int quantityPerPack, PackagingType packagingType) { SizeVariantId = sizeVariantId; QuantityPerPack = quantityPerPack; PackagingType = packagingType; }
    public Guid SizeVariantId { get; private set; }
    public int QuantityPerPack { get; private set; }
    public PackagingType PackagingType { get; private set; }
    public int? CaseQuantity { get; private set; }
    public string? PackagingNotes { get; private set; }
    public ICollection<ProductIdentifier> Identifiers { get; } = new List<ProductIdentifier>();

    public void UpdateDetails(int quantityPerPack, PackagingType packagingType)
    {
        if (quantityPerPack <= 0)
            throw new ArgumentException("Pack quantity must be greater than zero.", nameof(quantityPerPack));
        if (!Enum.IsDefined(packagingType))
            throw new ArgumentException("The packaging type is invalid.", nameof(packagingType));
        QuantityPerPack = quantityPerPack;
        PackagingType = packagingType;
    }
}

public sealed class ProductIdentifier : Entity
{
    private ProductIdentifier() { Value = null!; }
    public ProductIdentifier(Guid packTypeId, IdentifierType type, string value) { PackTypeId = packTypeId; Type = type; Value = value; }
    public Guid PackTypeId { get; private set; }
    public IdentifierType Type { get; private set; }
    public string Value { get; private set; }

    public void UpdateValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Identifier value is required.", nameof(value));
        Value = value.Trim();
    }
}

public sealed class Country : Entity
{
    private Country() { IsoCode = null!; Name = null!; }
    public Country(string isoCode, string name) { IsoCode = isoCode; Name = name; }
    public string IsoCode { get; private set; }
    public string Name { get; private set; }
}

public sealed class Retailer : Entity
{
    private Retailer()
    {
        Name = null!;
        Slug = null!;
    }

    public Retailer(string name, string slug, string? websiteUrl = null)
    {
        Name = RequireValue(name, nameof(name), 200);
        Slug = RequireValue(slug, nameof(slug), 200);
        WebsiteUrl = NormalizeUrl(websiteUrl, nameof(websiteUrl));
        Status = RetailerStatus.Discovered;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public RetailerStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? IdentityVerifiedAtUtc { get; private set; }
    public string? IdentitySourceUrl { get; private set; }

    public void UpdateIdentity(string name, string slug, string? websiteUrl)
    {
        Name = RequireValue(name, nameof(name), 200);
        Slug = RequireValue(slug, nameof(slug), 200);
        WebsiteUrl = NormalizeUrl(websiteUrl, nameof(websiteUrl));

        if (Status == RetailerStatus.Verified)
        {
            Status = RetailerStatus.NeedsReview;
            IdentityVerifiedAtUtc = null;
            IdentitySourceUrl = null;
        }

        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void VerifyIdentity(string? sourceUrl = null)
    {
        Status = RetailerStatus.Verified;
        IdentityVerifiedAtUtc = DateTimeOffset.UtcNow;
        IdentitySourceUrl = NormalizeUrl(sourceUrl, nameof(sourceUrl));
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkNeedsReview()
    {
        Status = RetailerStatus.NeedsReview;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkInactive()
    {
        Status = RetailerStatus.Inactive;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string RequireValue(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A value is required.", parameterName);

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"The value must be {maxLength} characters or fewer.", parameterName);

        return trimmed;
    }

    private static string? NormalizeUrl(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("The URL must be an absolute HTTP or HTTPS URL.", parameterName);

        return uri.ToString();
    }
}


public sealed class RetailerIdentityVerification : Entity
{
    private RetailerIdentityVerification()
    {
        SourceUrl = null!;
        ObservedRetailerName = null!;
    }

    public RetailerIdentityVerification(
        Guid retailerId,
        Guid verifiedByUserId,
        RetailerIdentityVerificationOutcome outcome,
        string observedRetailerName,
        string sourceUrl,
        string? listingUrl,
        bool websiteUrlValid,
        bool sourceUrlValid,
        bool listingUrlValid,
        bool nameMatches,
        bool domainMatches,
        string? notes = null)
    {
        if (retailerId == Guid.Empty)
            throw new ArgumentException("A retailer is required.", nameof(retailerId));
        if (verifiedByUserId == Guid.Empty)
            throw new ArgumentException("A verifying user is required.", nameof(verifiedByUserId));
        if (!Enum.IsDefined(outcome))
            throw new ArgumentException("The verification outcome is invalid.", nameof(outcome));
        ObservedRetailerName = RequireValue(observedRetailerName, nameof(observedRetailerName), 200);
        SourceUrl = RequireUrl(sourceUrl, nameof(sourceUrl));
        ListingUrl = NormalizeUrl(listingUrl, nameof(listingUrl));
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        RetailerId = retailerId;
        VerifiedByUserId = verifiedByUserId;
        Outcome = outcome;
        WebsiteUrlValid = websiteUrlValid;
        SourceUrlValid = sourceUrlValid;
        ListingUrlValid = listingUrlValid;
        NameMatches = nameMatches;
        DomainMatches = domainMatches;
        VerifiedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid RetailerId { get; private set; }
    public Guid VerifiedByUserId { get; private set; }
    public RetailerIdentityVerificationOutcome Outcome { get; private set; }
    public string ObservedRetailerName { get; private set; }
    public string SourceUrl { get; private set; }
    public string? ListingUrl { get; private set; }
    public bool WebsiteUrlValid { get; private set; }
    public bool SourceUrlValid { get; private set; }
    public bool ListingUrlValid { get; private set; }
    public bool NameMatches { get; private set; }
    public bool DomainMatches { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset VerifiedAtUtc { get; private set; }

    private static string RequireValue(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A value is required.", parameterName);
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"The value must be {maxLength} characters or fewer.", parameterName);
        return trimmed;
    }

    private static string RequireUrl(string value, string parameterName) =>
        NormalizeUrl(value, parameterName) ?? throw new ArgumentException("A valid HTTP or HTTPS URL is required.", parameterName);

    private static string? NormalizeUrl(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("The URL must be an absolute HTTP or HTTPS URL.", parameterName);
        return uri.ToString();
    }
}

public sealed class CatalogueSubmissionRetailAffiliate : Entity
{
    private CatalogueSubmissionRetailAffiliate()
    {
    }

    public CatalogueSubmissionRetailAffiliate(
        Guid submissionId,
        Guid retailDestinationId,
        AffiliateProgrammeStatus status,
        string? network = null,
        string? trackingConfiguration = null,
        string? deepLinkMechanism = null,
        string? termsUrl = null,
        string? applicationReference = null,
        string? notes = null)
    {
        if (submissionId == Guid.Empty)
            throw new ArgumentException("A catalogue submission is required.", nameof(submissionId));

        if (retailDestinationId == Guid.Empty)
            throw new ArgumentException("A retail destination is required.", nameof(retailDestinationId));

        if (!Enum.IsDefined(status))
            throw new ArgumentException("The affiliate programme status is invalid.", nameof(status));

        SubmissionId = submissionId;
        RetailDestinationId = retailDestinationId;
        Status = status;
        Network = string.IsNullOrWhiteSpace(network) ? null : network.Trim();
        TrackingConfiguration = string.IsNullOrWhiteSpace(trackingConfiguration) ? null : trackingConfiguration.Trim();
        DeepLinkMechanism = string.IsNullOrWhiteSpace(deepLinkMechanism) ? null : deepLinkMechanism.Trim();
        TermsUrl = string.IsNullOrWhiteSpace(termsUrl) ? null : termsUrl.Trim();
        ApplicationReference = string.IsNullOrWhiteSpace(applicationReference) ? null : applicationReference.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        LastVerifiedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid SubmissionId { get; private set; }
    public Guid RetailDestinationId { get; private set; }
    public AffiliateProgrammeStatus Status { get; private set; }
    public string? Network { get; private set; }
    public string? TrackingConfiguration { get; private set; }
    public string? DeepLinkMechanism { get; private set; }
    public string? TermsUrl { get; private set; }
    public string? ApplicationReference { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset LastVerifiedAtUtc { get; private set; }
}

public sealed class CatalogueSubmissionRetailDestination : Entity
{
    private CatalogueSubmissionRetailDestination()
    {
        ListingUrl = null!;
    }

    public CatalogueSubmissionRetailDestination(
        Guid submissionId,
        Guid retailerId,
        string listingUrl,
        string? notes = null)
    {
        if (submissionId == Guid.Empty)
            throw new ArgumentException("A catalogue submission is required.", nameof(submissionId));

        if (retailerId == Guid.Empty)
            throw new ArgumentException("A retailer is required.", nameof(retailerId));

        if (string.IsNullOrWhiteSpace(listingUrl))
            throw new ArgumentException("A product listing URL is required.", nameof(listingUrl));

        if (!Uri.TryCreate(listingUrl.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("The product listing URL must be an absolute HTTP or HTTPS URL.", nameof(listingUrl));

        SubmissionId = submissionId;
        RetailerId = retailerId;
        ListingUrl = uri.ToString();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        AddedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid SubmissionId { get; private set; }
    public Guid RetailerId { get; private set; }
    public string ListingUrl { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset AddedAtUtc { get; private set; }
}

public sealed class Location : Entity
{
    private Location() { Name = null!; AddressLine1 = null!; Locality = null!; Postcode = null!; }
    public Location(Guid retailerId, Guid countryId, string name, string addressLine1, string locality, string postcode)
    { RetailerId = retailerId; CountryId = countryId; Name = name; AddressLine1 = addressLine1; Locality = locality; Postcode = postcode; }
    public Guid RetailerId { get; private set; }
    public Guid CountryId { get; private set; }
    public string Name { get; private set; }
    public string AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string Locality { get; private set; }
    public string Postcode { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
}

public sealed class User : Entity
{
    private User() { Subject = null!; }
    public User(string subject) { Subject = subject; }
    public string Subject { get; private set; }
    public UserAccountStatus Status { get; private set; } = UserAccountStatus.Active;
    public ExplorerProfile? ExplorerProfile { get; private set; }
}

/// <summary>Security authority assigned to an existing User; it is not a second identity.</summary>
public sealed class PrivilegedRoleAssignment : Entity
{
    private PrivilegedRoleAssignment() { }
    public PrivilegedRoleAssignment(Guid userId, PrivilegedRole role, Guid grantedByUserId, DateTimeOffset grantedAtUtc)
    {
        UserId = userId; Role = role; GrantedByUserId = grantedByUserId; GrantedAtUtc = grantedAtUtc;
    }
    public Guid UserId { get; private set; }
    public PrivilegedRole Role { get; private set; }
    public Guid GrantedByUserId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; }
    public Guid? RevokedByUserId { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public bool IsActive => RevokedAtUtc is null;
    public void Revoke(Guid revokedByUserId, DateTimeOffset revokedAtUtc)
    {
        if (!IsActive) throw new InvalidOperationException("This role assignment has already been revoked.");
        RevokedByUserId = revokedByUserId; RevokedAtUtc = revokedAtUtc;
    }
}

/// <summary>Immutable record of a privileged-role grant or revocation.</summary>
public sealed class PrivilegedRoleAssignmentAudit : Entity
{
    private PrivilegedRoleAssignmentAudit() { }
    public PrivilegedRoleAssignmentAudit(Guid actingUserId, Guid subjectUserId, PrivilegedRole role, PrivilegedRoleAssignmentAction action, DateTimeOffset occurredAtUtc)
    { ActingUserId = actingUserId; SubjectUserId = subjectUserId; Role = role; Action = action; OccurredAtUtc = occurredAtUtc; }
    public Guid ActingUserId { get; private set; }
    public Guid SubjectUserId { get; private set; }
    public PrivilegedRole Role { get; private set; }
    public PrivilegedRoleAssignmentAction Action { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
}

/// <summary>Immutable provenance record for a direct canonical catalogue mutation.</summary>
public sealed class CatalogueAuditRecord : Entity
{
    private CatalogueAuditRecord() { SubmittedPayloadJson = null!; AffectedCanonicalIdsJson = null!; SourceSummary = null!; SourceReferencesJson = null!; EditorialRationale = null!; }
    public CatalogueAuditRecord(CatalogueAuditAction action, Guid productId, Guid actingUserId, DateTimeOffset occurredAtUtc, string submittedPayloadJson, string affectedCanonicalIdsJson, string sourceSummary, string sourceReferencesJson, string editorialRationale, string? correlationId)
    {
        Action = action; ProductId = productId; ActingUserId = actingUserId; OccurredAtUtc = occurredAtUtc;
        SubmittedPayloadJson = submittedPayloadJson; AffectedCanonicalIdsJson = affectedCanonicalIdsJson;
        SourceSummary = sourceSummary; SourceReferencesJson = sourceReferencesJson; EditorialRationale = editorialRationale; CorrelationId = correlationId;
    }
    public CatalogueAuditAction Action { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ActingUserId { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string SubmittedPayloadJson { get; private set; }
    public string AffectedCanonicalIdsJson { get; private set; }
    public string SourceSummary { get; private set; }
    public string SourceReferencesJson { get; private set; }
    public string EditorialRationale { get; private set; }
    public string? CorrelationId { get; private set; }
}

public sealed class CatalogueSubmissionImage : Entity
{
    private CatalogueSubmissionImage()
    {
        StorageKey = null!;
        OriginalFileName = null!;
        ContentType = null!;
        SourceType = CatalogueImageSourceType.Other;
        PermissionStatus = CatalogueImagePermissionStatus.Unknown;
    }

    public CatalogueSubmissionImage(
        Guid submissionId,
        CatalogueSubmissionImageRole role,
        string storageKey,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        CatalogueImageSourceType sourceType,
        string? sourceUrl = null,
        string? sourceNotes = null,
        CatalogueImagePermissionStatus permissionStatus = CatalogueImagePermissionStatus.Unknown,
        string? permissionEvidence = null)
    {
        if (submissionId == Guid.Empty)
            throw new ArgumentException("A catalogue submission is required.", nameof(submissionId));

        if (!Enum.IsDefined(role))
            throw new ArgumentException("The image role is invalid.", nameof(role));

        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("An image storage key is required.", nameof(storageKey));

        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("An original file name is required.", nameof(originalFileName));

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("An image content type is required.", nameof(contentType));

        if (fileSizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "Image file size must be greater than zero.");

        if (!Enum.IsDefined(sourceType))
            throw new ArgumentException("The image source type is invalid.", nameof(sourceType));

        if (!Enum.IsDefined(permissionStatus))
            throw new ArgumentException("The image permission status is invalid.", nameof(permissionStatus));

        SubmissionId = submissionId;
        Role = role;
        StorageKey = storageKey.Trim();
        OriginalFileName = originalFileName.Trim();
        ContentType = contentType.Trim();
        FileSizeBytes = fileSizeBytes;
        SourceType = sourceType;
        SourceUrl = NormaliseUrl(sourceUrl);
        SourceNotes = NormaliseText(sourceNotes);
        PermissionStatus = permissionStatus;
        PermissionEvidence = NormaliseText(permissionEvidence);
        IsPrimary = false;
        Visibility = CalculateVisibility(sourceType, permissionStatus);
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid SubmissionId { get; private set; }
    public Guid? ProductId { get; private set; }
    public CatalogueSubmissionImageRole Role { get; private set; }
    public string StorageKey { get; private set; }
    public string OriginalFileName { get; private set; }
    public string ContentType { get; private set; }
    public long FileSizeBytes { get; private set; }
    public CatalogueImageSourceType SourceType { get; private set; }
    public string? SourceUrl { get; private set; }
    public string? SourceNotes { get; private set; }
    public CatalogueImagePermissionStatus PermissionStatus { get; private set; }
    public string? PermissionEvidence { get; private set; }
    public CatalogueContentVisibility Visibility { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void UpdateRole(CatalogueSubmissionImageRole role)
    {
        if (!Enum.IsDefined(role))
            throw new ArgumentException("The image role is invalid.", nameof(role));

        Role = role;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void UpdateMetadata(
        CatalogueImageSourceType sourceType,
        string? sourceUrl,
        string? sourceNotes,
        CatalogueImagePermissionStatus permissionStatus,
        string? permissionEvidence)
    {
        if (!Enum.IsDefined(sourceType))
            throw new ArgumentException("The image source type is invalid.", nameof(sourceType));

        if (!Enum.IsDefined(permissionStatus))
            throw new ArgumentException("The image permission status is invalid.", nameof(permissionStatus));

        SourceType = sourceType;
        SourceUrl = NormaliseUrl(sourceUrl);
        SourceNotes = NormaliseText(sourceNotes);
        PermissionStatus = permissionStatus;
        PermissionEvidence = NormaliseText(permissionEvidence);
        Visibility = CalculateVisibility(sourceType, permissionStatus);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void PublishToProduct(Guid productId)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("A canonical product is required.", nameof(productId));

        ProductId = productId;
        Visibility = CalculateVisibility(SourceType, PermissionStatus);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static CatalogueContentVisibility CalculateVisibility(
        CatalogueImageSourceType sourceType,
        CatalogueImagePermissionStatus permissionStatus) =>
        sourceType != CatalogueImageSourceType.Unknown &&
        permissionStatus is CatalogueImagePermissionStatus.PermissionGranted or CatalogueImagePermissionStatus.PermissionNotRequired
            ? CatalogueContentVisibility.Public
            : CatalogueContentVisibility.ModeratorOnly;

    private static string? NormaliseText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormaliseUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("Image source URL must be an absolute HTTP or HTTPS URL.", nameof(value));

        return uri.AbsoluteUri;
    }
}

/// <summary>
/// A proposed catalogue entry being researched and verified before it can become
/// a canonical Product. Submission data is intentionally separate from canonical
/// catalogue entities so unverified information cannot mutate the trusted catalogue.
/// </summary>
public sealed class CatalogueSubmissionVerification : Entity
{
    private CatalogueSubmissionVerification()
    {
        Scope = null!;
        Source = null!;
    }

    public CatalogueSubmissionVerification(
        Guid submissionId,
        Guid verifiedByUserId,
        CatalogueVerificationArea area,
        CatalogueVerificationStatus status,
        string scope,
        string source,
        string? sourceUrl = null,
        string? notes = null,
        string? permissionTerms = null)
    {
        if (submissionId == Guid.Empty)
            throw new ArgumentException("A catalogue submission is required.", nameof(submissionId));

        if (verifiedByUserId == Guid.Empty)
            throw new ArgumentException("A verifying user is required.", nameof(verifiedByUserId));

        if (!Enum.IsDefined(area))
            throw new ArgumentException("The verification area is invalid.", nameof(area));

        if (!Enum.IsDefined(status))
            throw new ArgumentException("The verification status is invalid.", nameof(status));

        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("A verification scope is required.", nameof(scope));

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("A verification source is required.", nameof(source));

        SubmissionId = submissionId;
        VerifiedByUserId = verifiedByUserId;
        Area = area;
        Status = status;
        Scope = scope;
        Source = source;
        SourceUrl = sourceUrl;
        Notes = notes;
        PermissionTerms = permissionTerms;
        VerifiedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid SubmissionId { get; private set; }
    public Guid VerifiedByUserId { get; private set; }
    public CatalogueVerificationArea Area { get; private set; }
    public CatalogueVerificationStatus Status { get; private set; }
    public string Scope { get; private set; }
    public string Source { get; private set; }
    public string? SourceUrl { get; private set; }
    public string? Notes { get; private set; }
    public string? PermissionTerms { get; private set; }
    public DateTimeOffset VerifiedAtUtc { get; private set; }
}

/// <summary>
/// Immutable editorial decision recorded against a catalogue submission.
/// The decision is separate from the submission lifecycle so the review history
/// remains auditable.
/// </summary>
public sealed class CatalogueSubmissionEditorialDecision : Entity
{
    private CatalogueSubmissionEditorialDecision()
    {
    }

    public CatalogueSubmissionEditorialDecision(
        Guid submissionId,
        Guid moderatorUserId,
        EditorialOutcome outcome,
        string? rationale = null)
    {
        if (submissionId == Guid.Empty)
            throw new ArgumentException("A catalogue submission is required.", nameof(submissionId));

        if (moderatorUserId == Guid.Empty)
            throw new ArgumentException("A reviewing moderator is required.", nameof(moderatorUserId));

        if (!Enum.IsDefined(outcome))
            throw new ArgumentException("The editorial outcome is invalid.", nameof(outcome));

        SubmissionId = submissionId;
        ModeratorUserId = moderatorUserId;
        Outcome = outcome;
        Rationale = string.IsNullOrWhiteSpace(rationale) ? null : rationale.Trim();
        DecidedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid SubmissionId { get; private set; }
    public Guid ModeratorUserId { get; private set; }
    public EditorialOutcome Outcome { get; private set; }
    public string? Rationale { get; private set; }
    public DateTimeOffset DecidedAtUtc { get; private set; }
}

public sealed class CatalogueSubmissionVariantOverride : Entity
{
    private CatalogueSubmissionVariantOverride() { }

    public CatalogueSubmissionVariantOverride(Guid variantId)
    {
        if (variantId == Guid.Empty)
            throw new ArgumentException("A product variant is required.", nameof(variantId));

        VariantId = variantId;
    }

    public Guid VariantId { get; private set; }
    public BackingType? BackingType { get; private set; }
    public FastenerType? FastenerType { get; private set; }
    public string? PrintDesign { get; private set; }
    public string? PrimaryColour { get; private set; }
    public bool? HasWetnessIndicator { get; private set; }
    public bool? HasStandingLeakGuards { get; private set; }
    public WaistbandStyle? WaistbandStyle { get; private set; }
    public FragranceType? Fragrance { get; private set; }
    public bool? IsLatexFree { get; private set; }
    public string? DesignedFor { get; private set; }
    public int? FastenerCount { get; private set; }
    public string? ConstructionNotes { get; private set; }

    public bool HasAnyOverride =>
        BackingType.HasValue || FastenerType.HasValue || PrintDesign is not null || PrimaryColour is not null ||
        HasWetnessIndicator.HasValue || HasStandingLeakGuards.HasValue || WaistbandStyle.HasValue ||
        Fragrance.HasValue || IsLatexFree.HasValue || DesignedFor is not null || FastenerCount.HasValue ||
        ConstructionNotes is not null;

    public void Set(CatalogueVariantOverrideAttribute attribute, string? value)
    {
        switch (attribute)
        {
            case CatalogueVariantOverrideAttribute.BackingType:
                BackingType = ParseEnum<BackingType>(value, attribute); break;
            case CatalogueVariantOverrideAttribute.FastenerType:
                FastenerType = ParseEnum<FastenerType>(value, attribute); break;
            case CatalogueVariantOverrideAttribute.Appearance:
                PrintDesign = ParseAppearance(value, attribute); break;
            case CatalogueVariantOverrideAttribute.PrimaryColour:
                PrimaryColour = ParseColour(value, attribute); break;
            case CatalogueVariantOverrideAttribute.WetnessIndicator:
                HasWetnessIndicator = ParseBool(value, attribute); break;
            case CatalogueVariantOverrideAttribute.StandingLeakGuards:
                HasStandingLeakGuards = ParseBool(value, attribute); break;
            case CatalogueVariantOverrideAttribute.WaistbandStyle:
                WaistbandStyle = ParseEnum<WaistbandStyle>(value, attribute); break;
            case CatalogueVariantOverrideAttribute.Fragrance:
                Fragrance = ParseEnum<FragranceType>(value, attribute); break;
            case CatalogueVariantOverrideAttribute.LatexFree:
                IsLatexFree = ParseBool(value, attribute); break;
            case CatalogueVariantOverrideAttribute.DesignedFor:
                DesignedFor = ParseDesignedFor(value, attribute); break;
            case CatalogueVariantOverrideAttribute.FastenerCount:
                FastenerCount = ParseInt(value, attribute); break;
            case CatalogueVariantOverrideAttribute.ConstructionNotes:
                ConstructionNotes = NormaliseText(value); break;
            default:
                throw new ArgumentException("The variant override attribute is invalid.", nameof(attribute));
        }
    }

    private static string? NormaliseText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? ParseAppearance(string? value, CatalogueVariantOverrideAttribute attribute)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Enum.TryParse<CatalogueVariantAppearance>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed.ToString()
            : throw new ArgumentException($"The value for {attribute} is invalid.", nameof(value));
    }

    private static string? ParseColour(string? value, CatalogueVariantOverrideAttribute attribute)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Enum.TryParse<CatalogueVariantColour>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed.ToString()
            : throw new ArgumentException($"The value for {attribute} is invalid.", nameof(value));
    }

    private static string? ParseDesignedFor(string? value, CatalogueVariantOverrideAttribute attribute)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Enum.TryParse<CatalogueVariantDesignedFor>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed.ToString()
            : throw new ArgumentException($"The value for {attribute} is invalid.", nameof(value));
    }

    private static T? ParseEnum<T>(string? value, CatalogueVariantOverrideAttribute attribute) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Enum.TryParse<T>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new ArgumentException($"The value for {attribute} is invalid.", nameof(value));
    }

    private static bool? ParseBool(string? value, CatalogueVariantOverrideAttribute attribute)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return bool.TryParse(value, out var parsed)
            ? parsed
            : throw new ArgumentException($"The value for {attribute} must be true or false.", nameof(value));
    }

    private static int? ParseInt(string? value, CatalogueVariantOverrideAttribute attribute)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return int.TryParse(value, out var parsed) && parsed >= 0
            ? parsed
            : throw new ArgumentException($"The value for {attribute} must be a non-negative whole number.", nameof(value));
    }
}

public sealed class CatalogueSubmissionSizeVariant : Entity
{
    private CatalogueSubmissionSizeVariant()
    {
        ManufacturerSize = null!;
    }

    public CatalogueSubmissionSizeVariant(
        Guid variantId,
        string manufacturerSize,
        int? waistMinimumCm = null,
        int? waistMaximumCm = null,
        int? hipMinimumCm = null,
        int? hipMaximumCm = null,
        int? manufacturerStatedAbsorbencyMl = null,
        string? fitMeasurementBasis = null,
        string? absorbencyBasisMethod = null,
        string? absorbencySource = null,
        int? lengthMm = null,
        int? widthMm = null,
        int? weightGrams = null,
        int? manufacturerPackQuantity = null,
        string? gtin = null)
    {
        if (variantId == Guid.Empty)
            throw new ArgumentException("A product variant is required.", nameof(variantId));

        ValidateManufacturerSize(manufacturerSize);
        ValidateRange(waistMinimumCm, waistMaximumCm, "waist");
        ValidateRange(hipMinimumCm, hipMaximumCm, "hip");
        ValidateNonNegative(manufacturerStatedAbsorbencyMl, nameof(manufacturerStatedAbsorbencyMl));
        ValidateNonNegative(lengthMm, nameof(lengthMm));
        ValidateNonNegative(widthMm, nameof(widthMm));
        ValidateNonNegative(weightGrams, nameof(weightGrams));
        ValidatePackQuantity(manufacturerPackQuantity);
        ValidateGtin(gtin);

        VariantId = variantId;
        ManufacturerSize = manufacturerSize.Trim();
        WaistMinimumCm = waistMinimumCm;
        WaistMaximumCm = waistMaximumCm;
        HipMinimumCm = hipMinimumCm;
        HipMaximumCm = hipMaximumCm;
        FitMeasurementBasis = NormaliseText(fitMeasurementBasis);
        AbsorbencyBasisMethod = NormaliseText(absorbencyBasisMethod);
        AbsorbencySource = NormaliseText(absorbencySource);
        ManufacturerStatedAbsorbencyMl = manufacturerStatedAbsorbencyMl;
        LengthMm = lengthMm;
        WidthMm = widthMm;
        WeightGrams = weightGrams;
        ManufacturerPackQuantity = manufacturerPackQuantity;
        Gtin = NormaliseGtin(gtin);
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid VariantId { get; private set; }
    public string ManufacturerSize { get; private set; }
    public int? WaistMinimumCm { get; private set; }
    public int? WaistMaximumCm { get; private set; }
    public int? HipMinimumCm { get; private set; }
    public int? HipMaximumCm { get; private set; }
    public int? ManufacturerStatedAbsorbencyMl { get; private set; }
    public string? FitMeasurementBasis { get; private set; }
    public string? AbsorbencyBasisMethod { get; private set; }
    public string? AbsorbencySource { get; private set; }
    public int? LengthMm { get; private set; }
    public int? WidthMm { get; private set; }
    public int? WeightGrams { get; private set; }
    public int? ManufacturerPackQuantity { get; private set; }
    public string? Gtin { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(
        string manufacturerSize,
        int? waistMinimumCm,
        int? waistMaximumCm,
        int? hipMinimumCm,
        int? hipMaximumCm,
        int? manufacturerStatedAbsorbencyMl,
        string? fitMeasurementBasis,
        string? absorbencyBasisMethod,
        string? absorbencySource,
        int? lengthMm,
        int? widthMm,
        int? weightGrams,
        int? manufacturerPackQuantity,
        string? gtin)
    {
        ValidateManufacturerSize(manufacturerSize);
        ValidateRange(waistMinimumCm, waistMaximumCm, "waist");
        ValidateRange(hipMinimumCm, hipMaximumCm, "hip");
        ValidateNonNegative(manufacturerStatedAbsorbencyMl, nameof(manufacturerStatedAbsorbencyMl));
        ValidateNonNegative(lengthMm, nameof(lengthMm));
        ValidateNonNegative(widthMm, nameof(widthMm));
        ValidateNonNegative(weightGrams, nameof(weightGrams));
        ValidatePackQuantity(manufacturerPackQuantity);
        ValidateGtin(gtin);

        ManufacturerSize = manufacturerSize.Trim();
        WaistMinimumCm = waistMinimumCm;
        WaistMaximumCm = waistMaximumCm;
        HipMinimumCm = hipMinimumCm;
        HipMaximumCm = hipMaximumCm;
        FitMeasurementBasis = NormaliseText(fitMeasurementBasis);
        AbsorbencyBasisMethod = NormaliseText(absorbencyBasisMethod);
        AbsorbencySource = NormaliseText(absorbencySource);
        ManufacturerStatedAbsorbencyMl = manufacturerStatedAbsorbencyMl;
        LengthMm = lengthMm;
        WidthMm = widthMm;
        WeightGrams = weightGrams;
        ManufacturerPackQuantity = manufacturerPackQuantity;
        Gtin = NormaliseGtin(gtin);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string? NormaliseText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateManufacturerSize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A manufacturer size is required.", nameof(value));

        if (value.Trim().Length > 100)
            throw new ArgumentException("Manufacturer size must be 100 characters or fewer.", nameof(value));
    }

    private static void ValidateRange(int? minimum, int? maximum, string name)
    {
        if (minimum is < 0 || maximum is < 0)
            throw new ArgumentException($"{name} measurements cannot be negative.", name);

        if (minimum.HasValue && maximum.HasValue && minimum > maximum)
            throw new ArgumentException($"{name} minimum cannot be greater than maximum.", name);
    }

    private static void ValidateNonNegative(int? value, string name)
    {
        if (value is < 0)
            throw new ArgumentException($"{name} cannot be negative.", name);
    }

    private static void ValidatePackQuantity(int? value)
    {
        if (value is <= 0)
            throw new ArgumentException(
                "Manufacturer pack quantity must be greater than zero when supplied.",
                nameof(ManufacturerPackQuantity));
    }

    private static void ValidateGtin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var normalised = NormaliseGtin(value);
        if (normalised is null || normalised.Length is < 8 or > 14 || normalised.Any(character => !char.IsDigit(character)))
            throw new ArgumentException("GTIN must contain 8 to 14 digits.", nameof(Gtin));
    }

    private static string? NormaliseGtin(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Replace(" ", string.Empty).Replace("-", string.Empty).Trim();
}

public sealed class CatalogueSubmissionVariant : Entity
{
    private CatalogueSubmissionVariant()
    {
    }

    public CatalogueSubmissionVariant(
        Guid submissionId,
        string? name)
    {
        if (submissionId == Guid.Empty)
            throw new ArgumentException("A catalogue submission is required.", nameof(submissionId));

        if (name is not null && string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "A manufacturer-defined variant name must contain text when supplied.",
                nameof(name));

        SubmissionId = submissionId;
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid SubmissionId { get; private set; }
    public string? Name { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ICollection<CatalogueSubmissionSizeVariant> Sizes { get; } = new List<CatalogueSubmissionSizeVariant>();

    public bool IsBaseVariant => Name is null;

    public void Rename(string name)
    {
        if (IsBaseVariant)
            throw new InvalidOperationException("The base product variant cannot be renamed.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "A manufacturer-defined variant name is required.",
                nameof(name));

        Name = name.Trim();
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}

public sealed class CatalogueSubmission : Entity
{
    private CatalogueSubmission()
    {
        ProposedManufacturerName = null!;
        ProposedBrandName = null!;
        ProposedProductName = null!;
        ProposedVariantName = null;
    }

    public CatalogueSubmission(
        CatalogueSubmissionSource source,
        Guid? submittedByUserId,
        string proposedManufacturerName,
        string proposedProductName,
        string? proposedVariantName = null,
        string? proposedBrandName = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(proposedManufacturerName))
            throw new ArgumentException("A proposed manufacturer name is required.", nameof(proposedManufacturerName));

        if (string.IsNullOrWhiteSpace(proposedProductName))
            throw new ArgumentException("A proposed product name is required.", nameof(proposedProductName));

        Source = source;
        SubmittedByUserId = submittedByUserId;
        ProposedManufacturerName = proposedManufacturerName;
        ProposedBrandName = proposedBrandName;
        ProposedProductName = proposedProductName;
        ProposedVariantName = proposedVariantName;
        Notes = notes;
        Status = CatalogueSubmissionStatus.Draft;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public CatalogueSubmissionStatus Status { get; private set; }

    public CatalogueSubmissionSource Source { get; private set; }

    public Guid? SubmittedByUserId { get; private set; }

    public string ProposedManufacturerName { get; private set; }

    public string? ProposedBrandName { get; private set; }

    public string ProposedProductName { get; private set; }

    public string? ProposedVariantName { get; private set; }

    public string? ProposedGtin { get; private set; }

    public string? ProposedSku { get; private set; }

    public string? IdentitySourceUrl { get; private set; }

    public ProductType? ProposedProductType { get; private set; }
    public string? ProposedProductFamily { get; private set; }
    public string? ProposedDescription { get; private set; }
    public CatalogueContentVisibility ProposedDescriptionVisibility { get; private set; } = CatalogueContentVisibility.Public;
    public ProductStatus? ProposedProductStatus { get; private set; }
    public string? ProposedOfficialWebsiteUrl { get; private set; }

    public PackagingType? ProposedPackagingType { get; private set; }

    public string? SharedPrintDesign { get; private set; }
    public string? SharedPrimaryColour { get; private set; }
    public bool? SharedWetnessIndicator { get; private set; }
    public bool? SharedStandingLeakGuards { get; private set; }
    public WaistbandStyle? SharedWaistbandStyle { get; private set; }
    public FragranceType? SharedFragrance { get; private set; }
    public bool? SharedLatexFree { get; private set; }
    public string? SharedDesignedFor { get; private set; }
    public int? SharedFastenerCount { get; private set; }
    public string? SharedConstructionNotes { get; private set; }

    public string? Notes { get; private set; }

    public Guid? PublishedProductId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ICollection<CatalogueSubmissionImage> Images { get; } = new List<CatalogueSubmissionImage>();

    public void UpdateProposal(
        string proposedManufacturerName,
        string proposedProductName,
        string? proposedVariantName,
        string? proposedBrandName,
        string? notes)
    {
        if (Status is not CatalogueSubmissionStatus.Draft and not CatalogueSubmissionStatus.NeedsChanges)
            throw new InvalidOperationException("Only draft submissions or submissions needing changes can be edited.");

        if (string.IsNullOrWhiteSpace(proposedManufacturerName))
            throw new ArgumentException("A proposed manufacturer name is required.", nameof(proposedManufacturerName));

        if (string.IsNullOrWhiteSpace(proposedProductName))
            throw new ArgumentException("A proposed product name is required.", nameof(proposedProductName));

        ProposedManufacturerName = proposedManufacturerName;
        ProposedBrandName = proposedBrandName;
        ProposedProductName = proposedProductName;
        ProposedVariantName = proposedVariantName;
        Notes = notes;
        Touch();
    }

    public void UpdateIdentity(
        string? proposedGtin,
        string? proposedSku,
        string? identitySourceUrl)
    {
        if (Status is not CatalogueSubmissionStatus.Draft and not CatalogueSubmissionStatus.NeedsChanges)
            throw new InvalidOperationException("Only draft submissions or submissions needing changes can be edited.");

        if (!string.IsNullOrWhiteSpace(proposedGtin))
        {
            proposedGtin = proposedGtin.Replace(" ", string.Empty).Replace("-", string.Empty);

            if (proposedGtin.Length is < 8 or > 14 || proposedGtin.Any(character => character is < '0' or > '9'))
                throw new ArgumentException("GTIN must contain 8 to 14 digits.", nameof(proposedGtin));
        }
        else
        {
            proposedGtin = null;
        }

        proposedSku = string.IsNullOrWhiteSpace(proposedSku) ? null : proposedSku.Trim();

        if (!string.IsNullOrWhiteSpace(identitySourceUrl))
        {
            if (!Uri.TryCreate(identitySourceUrl.Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new ArgumentException("Identity source URL must be an absolute HTTP or HTTPS URL.", nameof(identitySourceUrl));

            identitySourceUrl = uri.AbsoluteUri;
        }
        else
        {
            identitySourceUrl = null;
        }

        ProposedGtin = proposedGtin;
        ProposedSku = proposedSku;
        IdentitySourceUrl = identitySourceUrl;
        Touch();
    }

    public void ResolveCanonicalEntities(
        string manufacturerName,
        string? brandName)
    {
        if (Status is CatalogueSubmissionStatus.Published or CatalogueSubmissionStatus.Rejected)
            throw new InvalidOperationException("Published or rejected submissions cannot have catalogue entities resolved.");

        if (string.IsNullOrWhiteSpace(manufacturerName))
            throw new ArgumentException("A manufacturer name is required.", nameof(manufacturerName));

        ProposedManufacturerName = manufacturerName.Trim();
        ProposedBrandName = string.IsNullOrWhiteSpace(brandName)
            ? null
            : brandName.Trim();

        Touch();
    }

    public void UpdateSpecifications(
        ProductType? proposedProductType,
        PackagingType? proposedPackagingType,
        string? proposedProductFamily = null,
        string? proposedDescription = null,
        CatalogueContentVisibility proposedDescriptionVisibility = CatalogueContentVisibility.Public,
        ProductStatus? proposedProductStatus = null,
        string? proposedOfficialWebsiteUrl = null,
        CatalogueVariantAppearance? sharedAppearance = null,
        string? sharedPrimaryColour = null,
        bool? sharedWetnessIndicator = null,
        bool? sharedStandingLeakGuards = null,
        WaistbandStyle? sharedWaistbandStyle = null,
        FragranceType? sharedFragrance = null,
        bool? sharedLatexFree = null,
        string? sharedDesignedFor = null,
        int? sharedFastenerCount = null,
        string? sharedConstructionNotes = null)
    {
        if (Status is not CatalogueSubmissionStatus.Draft and not CatalogueSubmissionStatus.NeedsChanges)
            throw new InvalidOperationException("Only draft submissions or submissions needing changes can be edited.");

        if (proposedProductStatus.HasValue && !Enum.IsDefined(proposedProductStatus.Value))
            throw new ArgumentException("The product status is invalid.", nameof(proposedProductStatus));

        if (!string.IsNullOrWhiteSpace(proposedOfficialWebsiteUrl))
        {
            if (!Uri.TryCreate(proposedOfficialWebsiteUrl.Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new ArgumentException("Official website must be an absolute HTTP or HTTPS URL.", nameof(proposedOfficialWebsiteUrl));

            proposedOfficialWebsiteUrl = uri.AbsoluteUri;
        }

        ProposedProductType = proposedProductType;
        ProposedPackagingType = proposedPackagingType;
        ProposedProductFamily = string.IsNullOrWhiteSpace(proposedProductFamily) ? null : proposedProductFamily.Trim();
        ProposedDescription = string.IsNullOrWhiteSpace(proposedDescription) ? null : proposedDescription.Trim();
        ProposedDescriptionVisibility = proposedDescriptionVisibility;
        ProposedProductStatus = proposedProductStatus;
        ProposedOfficialWebsiteUrl = string.IsNullOrWhiteSpace(proposedOfficialWebsiteUrl) ? null : proposedOfficialWebsiteUrl.Trim();
        SharedPrintDesign = sharedAppearance?.ToString();
        SharedPrimaryColour = string.IsNullOrWhiteSpace(sharedPrimaryColour) ? null : sharedPrimaryColour.Trim();
        SharedWetnessIndicator = sharedWetnessIndicator;
        SharedStandingLeakGuards = sharedStandingLeakGuards;
        SharedWaistbandStyle = sharedWaistbandStyle;
        SharedFragrance = sharedFragrance;
        SharedLatexFree = sharedLatexFree;
        SharedDesignedFor = string.IsNullOrWhiteSpace(sharedDesignedFor) ? null : sharedDesignedFor.Trim();
        SharedFastenerCount = sharedFastenerCount;
        SharedConstructionNotes = string.IsNullOrWhiteSpace(sharedConstructionNotes) ? null : sharedConstructionNotes.Trim();
        Touch();
    }

    public void UpdateDescriptionVisibility(CatalogueContentVisibility visibility)
    {
        if (Status is not CatalogueSubmissionStatus.Draft
            and not CatalogueSubmissionStatus.NeedsChanges
            and not CatalogueSubmissionStatus.InVerification)
            throw new InvalidOperationException(
                "Description visibility can only be changed while a submission is being prepared or verified.");

        if (!Enum.IsDefined(visibility))
            throw new ArgumentException("The description visibility is invalid.", nameof(visibility));

        ProposedDescriptionVisibility = visibility;
        Touch();
    }

    public void BeginVerification()
    {
        if (Status is not CatalogueSubmissionStatus.Draft and not CatalogueSubmissionStatus.NeedsChanges)
            throw new InvalidOperationException("Only draft submissions or submissions needing changes can enter verification.");

        Status = CatalogueSubmissionStatus.InVerification;
        Touch();
    }

    public void MarkReadyForReview()
    {
        if (Status != CatalogueSubmissionStatus.InVerification)
            throw new InvalidOperationException("Only submissions in verification can be marked ready for review.");

        Status = CatalogueSubmissionStatus.ReadyForReview;
        Touch();
    }

    public void Approve()
    {
        if (Status != CatalogueSubmissionStatus.ReadyForReview)
            throw new InvalidOperationException("Only submissions ready for review can be approved.");

        Status = CatalogueSubmissionStatus.Approved;
        Touch();
    }

    public void MarkNeedsChanges()
    {
        if (Status != CatalogueSubmissionStatus.ReadyForReview)
            throw new InvalidOperationException("Only submissions ready for review can be returned for changes.");

        Status = CatalogueSubmissionStatus.NeedsChanges;
        Touch();
    }

    public void Reject()
    {
        if (Status is CatalogueSubmissionStatus.Published or CatalogueSubmissionStatus.Rejected)
            throw new InvalidOperationException("This submission cannot be rejected in its current state.");

        Status = CatalogueSubmissionStatus.Rejected;
        Touch();
    }

    public void ReturnToVerification()
    {
        if (Status != CatalogueSubmissionStatus.Approved)
            throw new InvalidOperationException("Only approved submissions can be returned to verification.");

        Status = CatalogueSubmissionStatus.InVerification;
        Touch();
    }

    public void Publish(Guid productId)
    {
        if (Status != CatalogueSubmissionStatus.Approved)
            throw new InvalidOperationException("Only approved submissions can be published.");

        if (productId == Guid.Empty)
            throw new ArgumentException("A published product ID is required.", nameof(productId));

        PublishedProductId = productId;
        Status = CatalogueSubmissionStatus.Published;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}

public sealed class ExplorerProfile : Entity
{
    private ExplorerProfile() { DisplayName = null!; }
    public ExplorerProfile(Guid userId, string displayName) { UserId = userId; DisplayName = displayName; }
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; }
    public Backpack? Backpack { get; private set; }
}

public sealed class Backpack : Entity
{
    private Backpack() { }
    public Backpack(Guid userId) { UserId = userId; }
    public Guid UserId { get; private set; }
    public ICollection<SavedProduct> SavedProducts { get; } = new List<SavedProduct>();
    public ICollection<SavedLocation> SavedLocations { get; } = new List<SavedLocation>();
}

public sealed class SavedProduct : Entity
{
    private SavedProduct() { }
    public SavedProduct(Guid backpackId, Guid productId) { BackpackId = backpackId; ProductId = productId; }
    public Guid BackpackId { get; private set; }
    public Guid ProductId { get; private set; }
    public DateTimeOffset SavedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
}

public sealed class SavedLocation : Entity
{
    private SavedLocation() { }
    public SavedLocation(Guid backpackId, Guid locationId) { BackpackId = backpackId; LocationId = locationId; }
    public Guid BackpackId { get; private set; }
    public Guid LocationId { get; private set; }
    public DateTimeOffset SavedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
}

public sealed class Observation : Entity
{
    private Observation() { }
    public Observation(Guid authorUserId, ObservationType type, DateTimeOffset observedAtUtc, Guid? productId = null, string? candidateProductName = null, Guid? locationId = null, string? narrative = null)
    {
        if (productId is null && string.IsNullOrWhiteSpace(candidateProductName)) throw new ArgumentException("An observation must identify a product or candidate product.", nameof(candidateProductName));
        AuthorUserId = authorUserId; Type = type; ObservedAtUtc = observedAtUtc; ProductId = productId; CandidateProductName = candidateProductName; LocationId = locationId; Narrative = narrative;
    }
    public Guid AuthorUserId { get; private set; }
    public Guid? ProductId { get; private set; }
    public string? CandidateProductName { get; private set; }
    public Guid? LocationId { get; private set; }
    public ObservationType Type { get; private set; }
    public DateTimeOffset ObservedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public ObservationState State { get; private set; } = ObservationState.Draft;
    public string? Narrative { get; private set; }
    public decimal? PriceAmount { get; private set; }
    public string? PriceCurrencyCode { get; private set; }
    public void Submit() { if (State != ObservationState.Draft) throw new InvalidOperationException("Only drafts can be submitted."); State = ObservationState.Submitted; }
    public ICollection<EvidenceItem> Evidence { get; } = new List<EvidenceItem>();
}

public sealed class EvidenceItem : Entity
{
    private EvidenceItem() { StorageKey = null!; }
    public EvidenceItem(Guid observationId, Guid submittedByUserId, EvidenceType type, string storageKey) { ObservationId = observationId; SubmittedByUserId = submittedByUserId; Type = type; StorageKey = storageKey; }
    public Guid ObservationId { get; private set; }
    public Guid SubmittedByUserId { get; private set; }
    public EvidenceType Type { get; private set; }
    public string StorageKey { get; private set; }
    public string? ContentType { get; private set; }
    public DateTimeOffset SubmittedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
}

public sealed class EditorialDecision : Entity
{
    private EditorialDecision() { }
    public EditorialDecision(Guid observationId, Guid moderatorUserId, EditorialOutcome outcome) { ObservationId = observationId; ModeratorUserId = moderatorUserId; Outcome = outcome; }
    public Guid ObservationId { get; private set; }
    public Guid ModeratorUserId { get; private set; }
    public EditorialOutcome Outcome { get; private set; }
    public string? Rationale { get; private set; }
    public DateTimeOffset DecidedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
}

public sealed class KnowledgeGap : Entity
{
    private KnowledgeGap() { Title = null!; }
    public KnowledgeGap(KnowledgeGapType type, string title) { Type = type; Title = title; }
    public KnowledgeGapType Type { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public Guid? ProductId { get; private set; }
    public Guid? LocationId { get; private set; }
}

public sealed class DiscoveryTask : Entity
{
    private DiscoveryTask() { Title = null!; }
    public DiscoveryTask(Guid knowledgeGapId, string title) { KnowledgeGapId = knowledgeGapId; Title = title; }
    public Guid KnowledgeGapId { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public DiscoveryTaskState State { get; private set; } = DiscoveryTaskState.Open;
    public Guid? AcceptedByUserId { get; private set; }
    public Guid? ResultingObservationId { get; private set; }
}