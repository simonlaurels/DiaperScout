using DiaperScout.Domain;
using Xunit;

namespace DiaperScout.Domain.Tests;

public sealed class ExplorerProposalTests
{
    private static CatalogueSubmission Proposal()
    {
        var proposal = new CatalogueSubmission(CatalogueSubmissionSource.Explorer, Guid.NewGuid(), "Printed brand", "Printed product");
        proposal.SetPublicContribution(Guid.NewGuid());
        return proposal;
    }

    [Fact]
    public void Suggestion_and_pending_discovery_do_not_establish_canonical_identity()
    {
        var proposal = Proposal();
        var suggested = Guid.NewGuid(); var place = Guid.NewGuid();
        proposal.SetExplorerEvidence(suggested, null, null, null, null);
        proposal.BeginVerification();
        proposal.AttachPendingDiscovery(place, DateTimeOffset.UtcNow, null, "GBP");
        Assert.Equal(suggested, proposal.SuggestedExistingProductId);
        Assert.Equal(place, proposal.PendingLocationId);
        Assert.Null(proposal.PendingCurrencyCode);
        Assert.Null(proposal.PublishedProductId);
        Assert.Null(proposal.ResolvedPackTypeId);
        Assert.Null(proposal.ResultingObservationId);
        Assert.Throws<InvalidOperationException>(() => proposal.SetExplorerEvidence(null, null, null, null, null));
        Assert.Throws<InvalidOperationException>(() => proposal.LinkResolvedDiscovery(Guid.NewGuid(), Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(() => proposal.AttachPendingDiscovery(place, DateTimeOffset.UtcNow, null, null));
    }

    [Fact]
    public void Resolved_discovery_keeps_one_observation_and_exact_pack()
    {
        var proposal = Proposal();
        proposal.BeginVerification(); proposal.MarkReadyForReview(); proposal.Approve(); proposal.Publish(Guid.NewGuid());
        var pack = Guid.NewGuid(); var observation = Guid.NewGuid();
        proposal.LinkResolvedDiscovery(pack, observation);
        proposal.LinkResolvedDiscovery(pack, observation);
        Assert.Equal(observation, proposal.ResultingObservationId);
        Assert.Throws<InvalidOperationException>(() => proposal.LinkResolvedDiscovery(pack, Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(() => proposal.LinkResolvedDiscovery(Guid.NewGuid(), observation));
        Assert.Throws<InvalidOperationException>(() => proposal.AttachPendingDiscovery(Guid.NewGuid(), DateTimeOffset.UtcNow, null, null));
    }

    [Fact]
    public void Rejected_evidence_cannot_be_reconciled_or_extended()
    {
        var proposal = Proposal(); proposal.Reject();
        Assert.Throws<InvalidOperationException>(() => proposal.AttachPendingDiscovery(Guid.NewGuid(), DateTimeOffset.UtcNow, null, null));
        Assert.Throws<InvalidOperationException>(() => proposal.LinkResolvedDiscovery(Guid.NewGuid(), Guid.NewGuid()));
        Assert.Null(proposal.ResultingObservationId);
    }
}
