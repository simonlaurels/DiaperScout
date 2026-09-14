namespace DiaperScout.Domain;

public abstract class Entity
{
    protected Entity() => Id = Guid.NewGuid();
    public Guid Id { get; private set; }
}

public enum ProductType { Tape, PullUp, Pad, Booster, AllInOne, Other }
public enum ProductStatus { Current, Discontinued, Prototype }
public enum BackingType { Unknown, Plastic, Cloth, Hybrid }
public enum FastenerType { Unknown, Tape, HookAndLoop, PullUp }
public enum WaistbandStyle { Unknown, None, Front, Rear, FrontAndRear }
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
    Other
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
    public Product(Guid manufacturerId, Guid? brandId, string name, string slug, ProductType type, ProductStatus status = ProductStatus.Current)
    { ManufacturerId = manufacturerId; BrandId = brandId; Name = name; Slug = slug; ProductType = type; Status = status; }
    public Guid ManufacturerId { get; private set; }
    public Guid? BrandId { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? Family { get; private set; }
    public ProductType ProductType { get; private set; }
    public ProductStatus Status { get; private set; }
    public string? Description { get; private set; }
    public string? OfficialWebsiteUrl { get; private set; }
    public ICollection<ProductVariant> Variants { get; } = new List<ProductVariant>();
}

public sealed class ProductVariant : Entity
{
    private ProductVariant() { Name = null!; }
    public ProductVariant(Guid productId, string name, BackingType backingType = BackingType.Unknown) { ProductId = productId; Name = name; BackingType = backingType; }
    public Guid ProductId { get; private set; }
    public string Name { get; private set; }
    public BackingType BackingType { get; private set; }
    public FastenerType FastenerType { get; private set; }
    public string? PrintDesign { get; private set; }
    public string? PrimaryColour { get; private set; }
    public bool? HasWetnessIndicator { get; private set; }
    public bool? HasStandingLeakGuards { get; private set; }
    public bool? HasInnerLeakGuards { get; private set; }
    public bool? HasElasticWaistbandFront { get; private set; }
    public bool? HasElasticWaistbandRear { get; private set; }
    public WaistbandStyle WaistbandStyle { get; private set; }
    public FragranceType Fragrance { get; private set; }
    public bool? IsLatexFree { get; private set; }
    public bool? IsChlorineFree { get; private set; }
    public int? FastenerCount { get; private set; }
    public string? ConstructionNotes { get; private set; }
    public ICollection<SizeVariant> Sizes { get; } = new List<SizeVariant>();
}

public sealed class SizeVariant : Entity
{
    private SizeVariant() { ManufacturerSize = null!; }
    public SizeVariant(Guid productVariantId, string manufacturerSize, int? waistMinimumCm = null, int? waistMaximumCm = null)
    {
        if (waistMinimumCm is not null && waistMaximumCm is not null && waistMinimumCm > waistMaximumCm)
            throw new ArgumentException("Waist minimum cannot exceed waist maximum.", nameof(waistMinimumCm));

        ProductVariantId = productVariantId;
        ManufacturerSize = manufacturerSize;
        WaistMinimumCm = waistMinimumCm;
        WaistMaximumCm = waistMaximumCm;
    }
    public Guid ProductVariantId { get; private set; }
    public string ManufacturerSize { get; private set; }
    public int? WaistMinimumCm { get; private set; }
    public int? WaistMaximumCm { get; private set; }
    public int? HipMinimumCm { get; private set; }
    public int? HipMaximumCm { get; private set; }
    public int? CapacityMl { get; private set; }
    public int? LengthMm { get; private set; }
    public int? WidthMm { get; private set; }
    public int? WeightGrams { get; private set; }
    public ICollection<PackType> PackTypes { get; } = new List<PackType>();
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
}

public sealed class ProductIdentifier : Entity
{
    private ProductIdentifier() { Value = null!; }
    public ProductIdentifier(Guid packTypeId, IdentifierType type, string value) { PackTypeId = packTypeId; Type = type; Value = value; }
    public Guid PackTypeId { get; private set; }
    public IdentifierType Type { get; private set; }
    public string Value { get; private set; }
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
    private Retailer() { Name = null!; Slug = null!; }
    public Retailer(string name, string slug, string? websiteUrl = null) { Name = name; Slug = slug; WebsiteUrl = websiteUrl; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? WebsiteUrl { get; private set; }
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

public sealed class CatalogueSubmissionVariant : Entity
{
    private CatalogueSubmissionVariant()
    {
        Name = null!;
    }

    public CatalogueSubmissionVariant(
        Guid submissionId,
        string name,
        bool isStructuralFallback = false)
    {
        if (submissionId == Guid.Empty)
            throw new ArgumentException("A catalogue submission is required.", nameof(submissionId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A variant name is required.", nameof(name));

        SubmissionId = submissionId;
        Name = name.Trim();
        IsStructuralFallback = isStructuralFallback;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid SubmissionId { get; private set; }
    public string Name { get; private set; }
    public bool IsStructuralFallback { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A variant name is required.", nameof(name));

        if (IsStructuralFallback)
            throw new InvalidOperationException("The structural Single version cannot be renamed.");

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
        ProposedVariantName = null!;
    }

    public CatalogueSubmission(
        CatalogueSubmissionSource source,
        Guid? submittedByUserId,
        string proposedManufacturerName,
        string proposedProductName,
        string proposedVariantName,
        string? proposedBrandName = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(proposedManufacturerName))
            throw new ArgumentException("A proposed manufacturer name is required.", nameof(proposedManufacturerName));

        if (string.IsNullOrWhiteSpace(proposedProductName))
            throw new ArgumentException("A proposed product name is required.", nameof(proposedProductName));

        if (string.IsNullOrWhiteSpace(proposedVariantName))
            throw new ArgumentException("A proposed variant name is required.", nameof(proposedVariantName));

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

    public string ProposedVariantName { get; private set; }

    public string? ProposedGtin { get; private set; }

    public string? ProposedSku { get; private set; }

    public string? IdentitySourceUrl { get; private set; }

    public ProductType? ProposedProductType { get; private set; }

    public string? ProposedManufacturerSize { get; private set; }

    public int? ProposedWaistMinimumCm { get; private set; }

    public int? ProposedWaistMaximumCm { get; private set; }

    public BackingType? ProposedBackingType { get; private set; }

    public FastenerType? ProposedFastenerType { get; private set; }

    public WaistbandStyle? ProposedWaistbandStyle { get; private set; }

    public FragranceType? ProposedFragranceType { get; private set; }

    public int? ProposedQuantityPerPack { get; private set; }

    public PackagingType? ProposedPackagingType { get; private set; }

    public string? Notes { get; private set; }

    public Guid? PublishedProductId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void UpdateProposal(
        string proposedManufacturerName,
        string proposedProductName,
        string proposedVariantName,
        string? proposedBrandName,
        string? notes)
    {
        if (Status is not CatalogueSubmissionStatus.Draft and not CatalogueSubmissionStatus.NeedsChanges)
            throw new InvalidOperationException("Only draft submissions or submissions needing changes can be edited.");

        if (string.IsNullOrWhiteSpace(proposedManufacturerName))
            throw new ArgumentException("A proposed manufacturer name is required.", nameof(proposedManufacturerName));

        if (string.IsNullOrWhiteSpace(proposedProductName))
            throw new ArgumentException("A proposed product name is required.", nameof(proposedProductName));

        if (string.IsNullOrWhiteSpace(proposedVariantName))
            throw new ArgumentException("A proposed variant name is required.", nameof(proposedVariantName));

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

    public void UpdateSpecifications(
        ProductType? proposedProductType,
        string? proposedManufacturerSize,
        int? proposedWaistMinimumCm,
        int? proposedWaistMaximumCm,
        BackingType? proposedBackingType,
        FastenerType? proposedFastenerType,
        WaistbandStyle? proposedWaistbandStyle,
        FragranceType? proposedFragranceType,
        int? proposedQuantityPerPack,
        PackagingType? proposedPackagingType)
    {
        if (Status is not CatalogueSubmissionStatus.Draft and not CatalogueSubmissionStatus.NeedsChanges)
            throw new InvalidOperationException("Only draft submissions or submissions needing changes can be edited.");

        if (proposedWaistMinimumCm is < 0)
            throw new ArgumentException("Minimum waist measurement cannot be negative.", nameof(proposedWaistMinimumCm));

        if (proposedWaistMaximumCm is < 0)
            throw new ArgumentException("Maximum waist measurement cannot be negative.", nameof(proposedWaistMaximumCm));

        if (proposedWaistMinimumCm.HasValue &&
            proposedWaistMaximumCm.HasValue &&
            proposedWaistMinimumCm.Value > proposedWaistMaximumCm.Value)
            throw new ArgumentException("Minimum waist measurement cannot exceed maximum waist measurement.", nameof(proposedWaistMinimumCm));

        if (proposedQuantityPerPack is <= 0)
            throw new ArgumentException("Quantity per pack must be greater than zero.", nameof(proposedQuantityPerPack));

        ProposedProductType = proposedProductType;
        ProposedManufacturerSize = string.IsNullOrWhiteSpace(proposedManufacturerSize)
            ? null
            : proposedManufacturerSize.Trim();
        ProposedWaistMinimumCm = proposedWaistMinimumCm;
        ProposedWaistMaximumCm = proposedWaistMaximumCm;
        ProposedBackingType = proposedBackingType;
        ProposedFastenerType = proposedFastenerType;
        ProposedWaistbandStyle = proposedWaistbandStyle;
        ProposedFragranceType = proposedFragranceType;
        ProposedQuantityPerPack = proposedQuantityPerPack;
        ProposedPackagingType = proposedPackagingType;
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