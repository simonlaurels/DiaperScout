namespace DiaperScout.Application;

public sealed record PublicProductProposal(string Gtin, string BrandName, string ProductName, string? Version,
    string? ManufacturerName, string? Size, int? Quantity, Guid ContributionId);
public sealed record PublicProductProposalReceipt(Guid SubmissionId);
public sealed record ResolveBarcodeProposal(Guid PackTypeId, string Rationale);
public interface IPublicProductContributions
{
    Task<PublicProductProposalReceipt> SubmitAsync(ExplorerIdentity actor, PublicProductProposal request, CancellationToken ct = default);
    Task ResolveAsync(AuthenticatedUser moderator, Guid submissionId, ResolveBarcodeProposal request, CancellationToken ct = default);
}
