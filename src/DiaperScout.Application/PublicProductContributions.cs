using DiaperScout.Domain;
namespace DiaperScout.Application;

public sealed record PublicProductProposal(string Gtin, string BrandName, string ProductName, string? Version,
    string? ManufacturerName, string? Size, int? Quantity, Guid ContributionId,
    string? Notes = null, ProductType? ProductType = null, Guid? SuggestedExistingProductId = null,
    PendingPhysicalDiscovery? Discovery = null);
public sealed record PendingPhysicalDiscovery(Guid LocationId, DateTimeOffset ObservedAtUtc, decimal? PriceAmount, string? CurrencyCode);
public sealed record PublicProductProposalReceipt(Guid SubmissionId);
public sealed record PublicProposalIdentity(Guid Id, string ProductName, string? BrandName, CatalogueSubmissionStatus Status);
public sealed record ResolveBarcodeProposal(Guid PackTypeId, string Rationale);
public interface IPublicProductContributions
{
    Task<PublicProductProposalReceipt> BeginAsync(AuthenticatedUser actor, string gtin, Guid contributionId, CancellationToken ct = default);
    Task<CatalogueSubmissionImageReceipt> AddEvidenceAsync(AuthenticatedUser actor, Guid submissionId, Guid uploadId, AddCatalogueSubmissionImage image, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogueSubmissionImageReceipt>> ImagesAsync(AuthenticatedUser actor, Guid submissionId, CancellationToken ct = default);
    Task<CatalogueSubmissionImageContent?> EvidenceAsync(AuthenticatedUser actor, Guid submissionId, Guid imageId, CancellationToken ct = default);
    Task ReconcileAsync(Guid submissionId, Guid packTypeId, CancellationToken ct = default);
    Task<PublicProposalIdentity?> IdentityAsync(AuthenticatedUser actor, Guid id, CancellationToken ct = default);
    Task AttachDiscoveryAsync(AuthenticatedUser actor, Guid id, PendingPhysicalDiscovery discovery, CancellationToken ct = default);
    Task<PublicProductProposalReceipt> SubmitAsync(AuthenticatedUser actor, PublicProductProposal request, CancellationToken ct = default);
    Task ResolveAsync(AuthenticatedUser moderator, Guid submissionId, ResolveBarcodeProposal request, CancellationToken ct = default);
}
