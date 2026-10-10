using DiaperScout.Domain;
using Xunit;
namespace DiaperScout.Domain.Tests;
public sealed class PlaceCategoryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(PlaceCategory.Pharmacy)]
    [InlineData(PlaceCategory.Supermarket)]
    [InlineData(PlaceCategory.SpecialistRetailer)]
    [InlineData(PlaceCategory.GeneralRetailer)]
    [InlineData(PlaceCategory.ConvenienceStore)]
    [InlineData(PlaceCategory.Other)]
    public void Category_is_optional_and_never_changes_shop_identity(PlaceCategory? category) {
        var shop = Location.PublicShop(Guid.NewGuid(), Guid.NewGuid(), "Shop", "Address", "Town", "Postcode", 51m, -2m, "identity", category);
        var id = shop.Id; var author = shop.CreatedByUserId;
        Assert.Equal(category, shop.Category);
        shop.SetCategory(PlaceCategory.Other); Assert.Equal(PlaceCategory.Other, shop.Category);
        shop.SetCategory(null); Assert.Null(shop.Category);
        Assert.Equal(id, shop.Id); Assert.Equal(author, shop.CreatedByUserId); Assert.Equal("identity", shop.PlaceIdentity);
        Assert.Null(shop.RetailerId); Assert.True(shop.IsPublicCommercialPlace); Assert.Equal(51m, shop.Latitude);
    }
    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(999)]
    public void Invalid_categories_cannot_replace_saved_classification(int value) {
        var shop = Location.PublicShop(Guid.NewGuid(), Guid.NewGuid(), "Shop", "Address", "Town", "Postcode", 51m, -2m, "identity", PlaceCategory.Pharmacy);
        Assert.Throws<ArgumentException>(() => shop.SetCategory((PlaceCategory)value));
        Assert.Equal(PlaceCategory.Pharmacy, shop.Category);
    }
}
