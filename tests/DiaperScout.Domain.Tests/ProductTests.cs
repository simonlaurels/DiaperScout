using DiaperScout.Domain;
using Xunit;

namespace DiaperScout.Domain.Tests;

public sealed class ProductTests
{
    [Fact]
    public void Can_change_between_current_and_discontinued()
    {
        var product = new Product(Guid.NewGuid(), null, "Test Product", "test-product", ProductType.Tape);

        product.SetStatus(ProductStatus.Discontinued);
        Assert.Equal(ProductStatus.Discontinued, product.Status);

        product.SetStatus(ProductStatus.Current);
        Assert.Equal(ProductStatus.Current, product.Status);
    }

    [Fact]
    public void Rejects_an_invalid_status()
    {
        var product = new Product(Guid.NewGuid(), null, "Test Product", "test-product", ProductType.Tape);

        Assert.Throws<ArgumentException>(() => product.SetStatus((ProductStatus)999));
    }
}
