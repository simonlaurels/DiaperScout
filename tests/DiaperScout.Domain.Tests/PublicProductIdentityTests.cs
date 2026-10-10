using DiaperScout.Application;
using Xunit;

namespace DiaperScout.Domain.Tests;

public class PublicProductIdentityTests
{
    [Theory]
    [InlineData("Crinklz", "Original", null, "Crinklz Original")]
    [InlineData("Crinklz", "Original", "Current", "Crinklz Original")]
    [InlineData("NorthShore", "MEGAMAX", "Black", "NorthShore MEGAMAX Black")]
    [InlineData("NorthShore", "MEGAMAX", "", "NorthShore MEGAMAX")]
    [InlineData("NorthShore", "MEGAMAX", " Default ", "NorthShore MEGAMAX")]
    [InlineData("NorthShore", "MEGAMAX", "Single version", "NorthShore MEGAMAX")]
    [InlineData("NorthShore", "MEGAMAX", "Unnamed", "NorthShore MEGAMAX")]
    [InlineData("NorthShore", "NorthShore MEGAMAX", "MEGAMAX Black", "NorthShore MEGAMAX Black")]
    [InlineData("Brand Care", "Care Original", "Original", "Brand Care Original")]
    [InlineData(null, "Original", null, "Original")]
    [InlineData("ABENA", "Slip Premium Special", null, "ABENA Slip Premium Special")]
    [InlineData("TENA", "TENA Slip", "Plus", "TENA Slip Plus")]
    [InlineData("Seni", "Active Classic", "Default", "Seni Active Classic")]
    public void ComposesCanonicalPublicNames(string? brand, string product, string? variant, string expected)
        => Assert.Equal(expected, PublicProductIdentity.DisplayName(brand, product, variant));
    [Fact]
    public void CurrentRemainsMeaningfulWhenItDistinguishesMultipleVersions()
        => Assert.Equal("Brand Product Current", PublicProductIdentity.DisplayName("Brand", "Product", "Current", 2));

}
