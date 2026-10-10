using DiaperScout.Domain;
namespace DiaperScout.Web.Services;
public static class PlaceCategoryPresentation
{
    public static string Label(PlaceCategory? category) => category switch {
        PlaceCategory.Pharmacy => "Pharmacy", PlaceCategory.Supermarket => "Supermarket",
        PlaceCategory.SpecialistRetailer => "Specialist retailer", PlaceCategory.GeneralRetailer => "General retailer",
        PlaceCategory.ConvenienceStore => "Convenience store", PlaceCategory.Other => "Other", _ => "Unspecified"
    };
}
