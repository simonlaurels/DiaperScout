using DiaperScout.Domain;
using Xunit;

namespace DiaperScout.Domain.Tests;

public sealed class ObservationTests
{
    [Fact]
    public void Requires_known_product_or_candidate_product() => Assert.Throws<ArgumentException>(() => new Observation(Guid.NewGuid(), ObservationType.FieldResearch, DateTimeOffset.UtcNow));

    [Fact]
    public void Submitted_observation_cannot_be_submitted_again()
    {
        var observation = new Observation(Guid.NewGuid(), ObservationType.FieldResearch, DateTimeOffset.UtcNow, candidateProductName: "Unknown pack");
        observation.Submit();
        Assert.Equal(ObservationState.Submitted, observation.State);
        Assert.Throws<InvalidOperationException>(() => observation.Submit());
    }

    [Fact]
    public void Size_variant_rejects_an_invalid_waist_range()
    {
        Assert.Throws<ArgumentException>(() => new SizeVariant(Guid.NewGuid(), "Medium", 102, 81));
    }
}
