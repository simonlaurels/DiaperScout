# Catalogue submission CSV import

The Product Management queue can import CSV research batches. Imports create normal moderator submissions; they never publish automatically.

## Safety model

- Imported descriptions are **ModeratorOnly** by default.
- A moderator can explicitly mark a description public after reviewing its rights/provenance.
- Imported rows do not download or publish remote images. Image URLs are deliberately not part of the import format so a bulk research import cannot accidentally reproduce third-party photography.
- Existing canonical GTINs are skipped with a warning.
- A malformed or invalid row is skipped without rolling back successful earlier rows.

## One row = one candidate submission

The importer supports product identity, product details, one variant, and one size variant per row. Multiple rows can therefore be used when a research batch needs separate candidate submissions for variants/sizes.

## Columns

`Manufacturer, Brand, ProductName, ProductType, PackagingType, ProductFamily, Description, ProductStatus, OfficialWebsite, VariantName, BackingType, FastenerType, FastenerCount, Appearance, PrimaryColour, WetnessIndicator, StandingLeakGuards, WaistbandStyle, Fragrance, LatexFree, DesignedFor, ConstructionNotes, ManufacturerSize, WaistMinCm, WaistMaxCm, HipMinCm, HipMaxCm, FitMeasurementBasis, AbsorbencyMl, AbsorbencyBasisMethod, AbsorbencySource, LengthMm, WidthMm, WeightGrams, ManufacturerPackQuantity, GTIN, IdentitySourceUrl, Notes, DescriptionVisibility`

`ProductType=Diaper` is accepted and maps to the current domain's `Tape` value, which is displayed as **Diaper** in the UI.

`DescriptionVisibility` may be `Public` or `ModeratorOnly`. If omitted and a description is supplied, the importer defaults it to `ModeratorOnly`.
