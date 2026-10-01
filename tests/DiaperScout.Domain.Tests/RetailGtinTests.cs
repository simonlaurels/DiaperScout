using DiaperScout.Domain;
using Xunit;
namespace DiaperScout.Domain.Tests;
public class RetailGtinTests
{
    [Theory]
    [InlineData("4006381333931", "4006381333931")]
    [InlineData("5060572900820", "5060572900820")]
    [InlineData(" 5060-572900820 ", "5060572900820")]
    [InlineData("96385074", "96385074")]
    [InlineData("036000291452", "036000291452")]
    [InlineData("00036000291452", "00036000291452")]
    public void Retail_formats_validate_and_normalise(string input, string expected) => Assert.Equal(expected, RetailGtin.Normalise(input));
    [Theory]
    [InlineData("12345678")]
    [InlineData("123456789")]
    [InlineData("4006381333932")]
    [InlineData("not-a-barcode")]
    [InlineData("")]
    [InlineData("00000000")]
    public void Invalid_formats_or_check_digits_are_rejected(string input) => Assert.Null(RetailGtin.Normalise(input));
    [Fact]
    public void Exact_physical_observation_is_structured_and_cannot_be_assigned_after_submission() {
        var o=new Observation(Guid.NewGuid(),ObservationType.RetailAvailability,DateTimeOffset.UtcNow,Guid.NewGuid(),locationId:Guid.NewGuid());
        var pack=Guid.NewGuid();o.RecordExactPack(pack,Guid.NewGuid(),12.30m,"GBP");o.Submit();
        Assert.Equal(pack,o.PackTypeId);Assert.Null(o.Narrative);Assert.Equal(12.30m,o.PriceAmount);
        Assert.Throws<ArgumentException>(()=>o.RecordExactPack(pack,Guid.NewGuid(),1,"GBP"));
    }
}
