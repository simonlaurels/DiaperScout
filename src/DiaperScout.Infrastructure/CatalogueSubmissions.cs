using DiaperScout.Application;
using System.Globalization;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiaperScout.Infrastructure;

internal sealed class CatalogueSubmissions(
    DiaperScoutDbContext db,
    IEditorialAuthorisation editorialAuthorisation,
    ICanonicalCatalogue canonicalCatalogue,
    ICatalogueSubmissionImageStorage imageStorage) : ICatalogueSubmissions
{
    public async Task<IReadOnlyList<CatalogueSubmissionQueueItem>> GetSubmissionsAsync(
        AuthenticatedUser actor,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        return await db.CatalogueSubmissions
            .AsNoTracking()
            .Where(value => value.Status != CatalogueSubmissionStatus.Published)
            .OrderByDescending(value => value.UpdatedAtUtc)
            .Select(value => new CatalogueSubmissionQueueItem(
                value.Id,
                value.Status,
                value.ProposedManufacturerName,
                value.ProposedBrandName,
                value.ProposedProductName,
                value.PublishedProductId,
                value.CreatedAtUtc,
                value.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<CatalogueSubmissionImportResult> ImportCsvAsync(
        AuthenticatedUser actor,
        Stream csvContent,
        CatalogueSubmissionImportOptions options,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var rows = CatalogueSubmissionCsvParser.Parse(
            csvContent,
            options.TreatImportedDescriptionsAsModeratorOnly);

        var warnings = new List<string>();
        var created = 0;
        var imported = 0;
        var skipped = 0;

        foreach (var productGroup in rows.GroupBy(
                     row => row.ImportProductKey.Trim(),
                     StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var groupRows = productGroup.ToList();
                var groupRowsSkipped = 0;
                ValidateProductGroup(groupRows);

                await using var transaction =
                    await db.Database.BeginTransactionAsync(cancellationToken);

                var eligibleRows = new List<CatalogueSubmissionCsvRow>();

                foreach (var row in groupRows)
                {
                    if (!string.IsNullOrWhiteSpace(row.Gtin) &&
                        await db.ProductIdentifiers.AnyAsync(
                            value =>
                                value.Type == IdentifierType.Gtin &&
                                value.Value == row.Gtin,
                            cancellationToken))
                    {
                        groupRowsSkipped++;
                        warnings.Add(
                            $"Line {row.LineNumber}: GTIN {row.Gtin} already exists in the canonical catalogue; the row was skipped.");
                        continue;
                    }

                    eligibleRows.Add(row);
                }

                if (eligibleRows.Count == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    skipped += groupRowsSkipped;
                    continue;
                }

                ValidateProductGroup(eligibleRows);

                var submissionTemplate = eligibleRows[0];

                var submission = new CatalogueSubmission(
                    CatalogueSubmissionSource.BulkImport,
                    actor.UserId,
                    submissionTemplate.Manufacturer,
                    submissionTemplate.ProductName,
                    null,
                    submissionTemplate.Brand,
                    submissionTemplate.Notes);

                // GTINs belong to size variants. Keep the submission-level identity
                // GTIN empty for grouped imports so one size cannot masquerade as
                // the product's sole identifier.
                submission.UpdateIdentity(
                    null,
                    null,
                    submissionTemplate.IdentitySourceUrl);

                submission.UpdateSpecifications(
                    submissionTemplate.ProductType,
                    submissionTemplate.PackagingType,
                    submissionTemplate.ProductFamily,
                    submissionTemplate.Description,
                    submissionTemplate.DescriptionVisibility,
                    submissionTemplate.ProductStatus,
                    submissionTemplate.OfficialWebsite,
                    null,
                    submissionTemplate.PrimaryColour,
                    submissionTemplate.WetnessIndicator,
                    submissionTemplate.StandingLeakGuards,
                    submissionTemplate.WaistbandStyle,
                    submissionTemplate.Fragrance,
                    submissionTemplate.LatexFree,
                    submissionTemplate.DesignedFor,
                    submissionTemplate.FastenerCount,
                    submissionTemplate.ConstructionNotes);

                db.CatalogueSubmissions.Add(submission);

                var variantsCreated = 0;
                foreach (var variantGroup in eligibleRows.GroupBy(
                             row => NormaliseVariantName(row.VariantName),
                             StringComparer.OrdinalIgnoreCase))
                {
                    var variantRows = variantGroup.ToList();
                    ValidateVariantGroup(variantRows);

                    var variant = new CatalogueSubmissionVariant(
                        submission.Id,
                        variantGroup.Key);

                    db.CatalogueSubmissionVariants.Add(variant);

                    var overrideValue =
                        new CatalogueSubmissionVariantOverride(variant.Id);

                    SetImportOverride(
                        overrideValue,
                        CatalogueVariantOverrideAttribute.BackingType,
                        variantRows[0].BackingType?.ToString());
                    SetImportOverride(
                        overrideValue,
                        CatalogueVariantOverrideAttribute.FastenerType,
                        variantRows[0].FastenerType?.ToString());
                    SetImportOverride(
                        overrideValue,
                        CatalogueVariantOverrideAttribute.Appearance,
                        variantRows[0].Appearance?.ToString());
                    SetImportOverride(
                        overrideValue,
                        CatalogueVariantOverrideAttribute.PrimaryColour,
                        variantRows[0].PrimaryColour);
                    SetImportOverride(
                        overrideValue,
                        CatalogueVariantOverrideAttribute.WetnessIndicator,
                        variantRows[0].WetnessIndicator?.ToString());
                    SetImportOverride(
                        overrideValue,
                        CatalogueVariantOverrideAttribute.StandingLeakGuards,
                        variantRows[0].StandingLeakGuards?.ToString());
                    SetImportOverride(
                        overrideValue,
                        CatalogueVariantOverrideAttribute.WaistbandStyle,
                        variantRows[0].WaistbandStyle?.ToString());
                    SetImportOverride(
                        overrideValue,
                        CatalogueVariantOverrideAttribute.Fragrance,
                        variantRows[0].Fragrance?.ToString());
                    SetImportOverride(
                        overrideValue,
                        CatalogueVariantOverrideAttribute.LatexFree,
                        variantRows[0].LatexFree?.ToString());
                    SetImportOverride(
                        overrideValue,
                        CatalogueVariantOverrideAttribute.DesignedFor,
                        variantRows[0].DesignedFor);
                    SetImportOverride(
                        overrideValue,
                        CatalogueVariantOverrideAttribute.FastenerCount,
                        variantRows[0].FastenerCount?.ToString(
                            CultureInfo.InvariantCulture));
                    SetImportOverride(
                        overrideValue,
                        CatalogueVariantOverrideAttribute.ConstructionNotes,
                        variantRows[0].ConstructionNotes);

                    if (overrideValue.HasAnyOverride)
                        db.CatalogueSubmissionVariantOverrides.Add(overrideValue);

                    foreach (var row in variantRows)
                    {
                        if (string.IsNullOrWhiteSpace(row.ManufacturerSize))
                        {
                            warnings.Add(
                                $"Line {row.LineNumber}: no manufacturer size was supplied for product '{row.ProductName}' variant '{variantGroup.Key ?? "Original"}'; the row was imported without a size.");
                            continue;
                        }

                        var size = new CatalogueSubmissionSizeVariant(
                            variant.Id,
                            row.ManufacturerSize,
                            row.WaistMinCm,
                            row.WaistMaxCm,
                            row.HipMinCm,
                            row.HipMaxCm,
                            row.AbsorbencyMl,
                            row.FitMeasurementBasis,
                            row.AbsorbencyBasisMethod,
                            row.AbsorbencySource,
                            row.LengthMm,
                            row.WidthMm,
                            row.WeightGrams,
                            row.ManufacturerPackQuantity,
                            row.Gtin);

                        db.CatalogueSubmissionSizeVariants.Add(size);
                    }

                    variantsCreated++;
                }

                if (variantsCreated == 0)
                    throw new CatalogueValidationException(
                        "variants",
                        "The import group did not contain any usable product variants.");

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                created++;
                imported += eligibleRows.Count;
                skipped += groupRowsSkipped;
            }
            catch (Exception exception) when (
                exception is ArgumentException or
                CatalogueValidationException or
                FormatException)
            {
                skipped += productGroup.Count();
                warnings.Add(
                    $"Import product key '{productGroup.Key}' was skipped: {exception.Message}");
            }
        }

        return new CatalogueSubmissionImportResult(
            rows.Count,
            created,
            imported,
            skipped,
            warnings);
    }

    private static void ValidateProductGroup(
        IReadOnlyList<CatalogueSubmissionCsvRow> rows)
    {
        if (rows.Count == 0)
            throw new CatalogueValidationException(
                "importProductKey",
                "An import product group cannot be empty.");

        EnsureConsistent(rows, "Manufacturer", row => row.Manufacturer);
        EnsureConsistent(rows, "Brand", row => row.Brand);
        EnsureConsistent(rows, "ProductName", row => row.ProductName);
        EnsureConsistent(rows, "ProductType", row => row.ProductType);
        EnsureConsistent(rows, "PackagingType", row => row.PackagingType);
        EnsureConsistent(rows, "ProductFamily", row => row.ProductFamily);
        EnsureConsistent(rows, "Description", row => row.Description);
        EnsureConsistent(rows, "ProductStatus", row => row.ProductStatus);
        EnsureConsistent(rows, "OfficialWebsite", row => row.OfficialWebsite);
        // IdentitySourceUrl is row-level provenance. A grouped product may legitimately
        // use different manufacturer/retailer source pages for different variants or sizes.
        // The submission keeps the first eligible source URL as its identity provenance.
        // Notes are row/source metadata and may legitimately differ between sizes.
        // Do not make them a product-level grouping constraint.
        EnsureConsistent(
            rows,
            "DescriptionVisibility",
            row => row.DescriptionVisibility);
    }

    private static void ValidateVariantGroup(
        IReadOnlyList<CatalogueSubmissionCsvRow> rows)
    {
        EnsureConsistent(rows, "BackingType", row => row.BackingType);
        EnsureConsistent(rows, "FastenerType", row => row.FastenerType);
        EnsureConsistent(rows, "FastenerCount", row => row.FastenerCount);
        EnsureConsistent(rows, "Appearance", row => row.Appearance);
        EnsureConsistent(rows, "PrimaryColour", row => row.PrimaryColour);
        EnsureConsistent(
            rows,
            "WetnessIndicator",
            row => row.WetnessIndicator);
        EnsureConsistent(
            rows,
            "StandingLeakGuards",
            row => row.StandingLeakGuards);
        EnsureConsistent(rows, "WaistbandStyle", row => row.WaistbandStyle);
        EnsureConsistent(rows, "Fragrance", row => row.Fragrance);
        EnsureConsistent(rows, "LatexFree", row => row.LatexFree);
        EnsureConsistent(rows, "DesignedFor", row => row.DesignedFor);
        EnsureConsistent(
            rows,
            "ConstructionNotes",
            row => row.ConstructionNotes);

        var duplicateSizes = rows
            .Where(row => !string.IsNullOrWhiteSpace(row.ManufacturerSize))
            .GroupBy(
                row => row.ManufacturerSize!.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateSizes.Count > 0)
        {
            throw new CatalogueValidationException(
                "ManufacturerSize",
                $"The variant contains duplicate size rows: {string.Join(", ", duplicateSizes)}.");
        }

        var rowsWithoutSize = rows.Count(
            row => string.IsNullOrWhiteSpace(row.ManufacturerSize));

        if (rowsWithoutSize > 1)
        {
            throw new CatalogueValidationException(
                "ManufacturerSize",
                "A variant can contain at most one row without a manufacturer size.");
        }
    }

    private static void EnsureConsistent(
        IReadOnlyList<CatalogueSubmissionCsvRow> rows,
        string fieldName,
        Func<CatalogueSubmissionCsvRow, object?> selector)
    {
        var values = rows
            .Select(selector)
            .Select(value => value is string text ? text.Trim() : value)
            .ToList();

        var first = values[0];

        if (values.Any(value => !Equals(value, first)))
        {
            throw new CatalogueValidationException(
                fieldName,
                $"Rows sharing the same import product key must have the same {fieldName} value.");
        }
    }

    private static string? NormaliseVariantName(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    private static void SetImportOverride(CatalogueSubmissionVariantOverride value, CatalogueVariantOverrideAttribute attribute, string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
            value.Set(attribute, text);
    }

    public async Task<CatalogueSubmissionReceipt> CreateAsync(
        AuthenticatedUser actor,
        CreateCatalogueSubmission command,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        if (!Enum.IsDefined(command.Source))
            throw new CatalogueValidationException("source", "The submission source is invalid.");

        try
        {
            var submission = new CatalogueSubmission(
                command.Source,
                actor.UserId,
                command.ProposedManufacturerName.Trim(),
                command.ProposedProductName.Trim(),
                command.ProposedVariantName?.Trim(),
                command.ProposedBrandName?.Trim(),
                command.Notes?.Trim());

            db.CatalogueSubmissions.Add(submission);

            // The optional proposed variant is retained for API compatibility
            // with older callers. New catalogue creation leaves variants empty
            // and the moderator chooses the base/named variants explicitly in
            // Step 2.
            if (!string.IsNullOrWhiteSpace(command.ProposedVariantName))
            {
                var initialName = command.ProposedVariantName.Trim();
                db.CatalogueSubmissionVariants.Add(
                    new CatalogueSubmissionVariant(
                        submission.Id,
                        string.Equals(initialName, "Single version", StringComparison.OrdinalIgnoreCase)
                            ? null
                            : initialName));
            }

            await db.SaveChangesAsync(cancellationToken);

            return ToReceipt(submission);
        }
        catch (ArgumentException exception)
        {
            throw new CatalogueValidationException(
                GetFieldName(exception.ParamName),
                exception.Message);
        }
    }

    public async Task<CatalogueSubmissionReceipt> GetAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(
            submissionId,
            cancellationToken);

        return ToReceipt(submission);
    }

    public async Task DeleteAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(
            submissionId,
            cancellationToken);

        if (submission.Status is not CatalogueSubmissionStatus.Draft and
            not CatalogueSubmissionStatus.NeedsChanges)
        {
            throw new CatalogueValidationException(
                "status",
                "Only draft submissions or submissions needing changes can be deleted.");
        }

        var variants = await db.CatalogueSubmissionVariants
            .Where(value => value.SubmissionId == submissionId)
            .ToListAsync(cancellationToken);

        var variantIds = variants
            .Select(value => value.Id)
            .ToArray();

        if (variantIds.Length > 0)
        {
            var overrides = await db.CatalogueSubmissionVariantOverrides
                .Where(value => variantIds.Contains(value.VariantId))
                .ToListAsync(cancellationToken);

            db.CatalogueSubmissionVariantOverrides.RemoveRange(overrides);
        }

        var retailDestinations = await db.CatalogueSubmissionRetailDestinations
            .Where(value => value.SubmissionId == submissionId)
            .ToListAsync(cancellationToken);

        var destinationIds = retailDestinations
            .Select(value => value.Id)
            .ToArray();

        if (destinationIds.Length > 0)
        {
            var affiliates = await db.CatalogueSubmissionRetailAffiliates
                .Where(value => destinationIds.Contains(value.RetailDestinationId))
                .ToListAsync(cancellationToken);

            db.CatalogueSubmissionRetailAffiliates.RemoveRange(affiliates);
        }

        var editorialDecisions = await db.CatalogueSubmissionEditorialDecisions
            .Where(value => value.SubmissionId == submissionId)
            .ToListAsync(cancellationToken);

        var verifications = await db.CatalogueSubmissionVerifications
            .Where(value => value.SubmissionId == submissionId)
            .ToListAsync(cancellationToken);

        db.CatalogueSubmissionRetailDestinations.RemoveRange(retailDestinations);
        db.CatalogueSubmissionEditorialDecisions.RemoveRange(editorialDecisions);
        db.CatalogueSubmissionVerifications.RemoveRange(verifications);
        var images = await db.CatalogueSubmissionImages
            .Where(value => value.SubmissionId == submissionId)
            .ToListAsync(cancellationToken);

        db.CatalogueSubmissionImages.RemoveRange(images);
        db.CatalogueSubmissionVariants.RemoveRange(variants);
        db.CatalogueSubmissions.Remove(submission);

        foreach (var image in images)
            await imageStorage.DeleteAsync(image.StorageKey, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CatalogueSubmissionVariantsResult> GetVariantsAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var exists = await db.CatalogueSubmissions
            .AsNoTracking()
            .AnyAsync(value => value.Id == submissionId, cancellationToken);

        if (!exists)
            return CatalogueSubmissionVariantsResult.Invalid(
                new Dictionary<string, string[]>
                {
                    ["submissionId"] = ["The catalogue submission was not found."]
                });

        var variants = await db.CatalogueSubmissionVariants
            .AsNoTracking()
            .Where(value => value.SubmissionId == submissionId)
            .OrderBy(value => value.CreatedAtUtc)
            .Select(value => new CatalogueSubmissionVariantReceipt(
                value.Id,
                value.SubmissionId,
                value.Name,
                value.CreatedAtUtc,
                value.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return CatalogueSubmissionVariantsResult.Found(variants);
    }

    public async Task<CatalogueSubmissionSizeVariantsResult> GetSizeVariantsAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid variantId,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var variantExists = await db.CatalogueSubmissionVariants
            .AsNoTracking()
            .AnyAsync(
                value => value.Id == variantId && value.SubmissionId == submissionId,
                cancellationToken);

        if (!variantExists)
            return CatalogueSubmissionSizeVariantsResult.Invalid(
                new Dictionary<string, string[]>
                {
                    ["variantId"] = ["The product variant was not found."]
                });

        var sizes = await db.CatalogueSubmissionSizeVariants
            .AsNoTracking()
            .Where(value => value.VariantId == variantId)
            .OrderBy(value => value.CreatedAtUtc)
            .Select(value => new CatalogueSubmissionSizeVariantReceipt(
                value.Id,
                value.VariantId,
                value.ManufacturerSize,
                value.WaistMinimumCm,
                value.WaistMaximumCm,
                value.HipMinimumCm,
                value.HipMaximumCm,
                value.ManufacturerStatedAbsorbencyMl,
                value.FitMeasurementBasis,
                value.AbsorbencyBasisMethod,
                value.AbsorbencySource,
                value.LengthMm,
                value.WidthMm,
                value.WeightGrams,
                value.ManufacturerPackQuantity,
                value.Gtin,
                value.CreatedAtUtc,
                value.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return CatalogueSubmissionSizeVariantsResult.Found(sizes);
    }

    public async Task<CatalogueSubmissionSizeVariantReceipt> AddSizeVariantAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid variantId,
        AddCatalogueSubmissionSizeVariant command,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(submissionId, cancellationToken);
        EnsureDraftEditable(submission);

        var variantExists = await db.CatalogueSubmissionVariants.AnyAsync(
            value => value.Id == variantId && value.SubmissionId == submissionId,
            cancellationToken);

        if (!variantExists)
            throw new CatalogueValidationException("variantId", "The product variant was not found.");

        try
        {
            var size = new CatalogueSubmissionSizeVariant(
                variantId,
                command.ManufacturerSize,
                command.WaistMinimumCm,
                command.WaistMaximumCm,
                command.HipMinimumCm,
                command.HipMaximumCm,
                command.ManufacturerStatedAbsorbencyMl,
                command.FitMeasurementBasis,
                command.AbsorbencyBasisMethod,
                command.AbsorbencySource,
                command.LengthMm,
                command.WidthMm,
                command.WeightGrams,
                command.ManufacturerPackQuantity,
                command.Gtin);

            var duplicate = await db.CatalogueSubmissionSizeVariants.AnyAsync(
                value => value.VariantId == variantId &&
                         value.ManufacturerSize.ToLower() == size.ManufacturerSize.ToLower(),
                cancellationToken);

            if (duplicate)
                throw new ArgumentException(
                    "A size with this manufacturer size already exists for this product variant.",
                    nameof(command.ManufacturerSize));

            var gtin = size.Gtin;
            if (gtin is not null)
            {
                var alreadyInSubmission = await db.CatalogueSubmissionSizeVariants.AnyAsync(
                    value => value.Gtin == gtin,
                    cancellationToken);

                if (alreadyInSubmission)
                    throw new ArgumentException(
                        $"GTIN {gtin} is already used by another size in this submission.",
                        nameof(command.Gtin));

                var alreadyInCatalogue = await db.ProductIdentifiers.AnyAsync(
                    identifier =>
                        identifier.Type == IdentifierType.Gtin &&
                        identifier.Value == gtin,
                    cancellationToken);

                if (alreadyInCatalogue)
                    throw new ArgumentException(
                        $"GTIN {gtin} is already assigned to a published catalogue product.",
                        nameof(command.Gtin));
            }

            db.CatalogueSubmissionSizeVariants.Add(size);
            await db.SaveChangesAsync(cancellationToken);
            return ToSizeVariantReceipt(size);
        }
        catch (ArgumentException exception)
        {
            throw new CatalogueValidationException(
                GetSizeVariantFieldName(exception.ParamName),
                exception.Message);
        }
    }

    public async Task<CatalogueSubmissionSizeVariantReceipt> UpdateSizeVariantAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid variantId,
        Guid sizeVariantId,
        UpdateCatalogueSubmissionSizeVariant command,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(submissionId, cancellationToken);
        EnsureDraftEditable(submission);

        var size = await db.CatalogueSubmissionSizeVariants
            .SingleOrDefaultAsync(
                value => value.Id == sizeVariantId && value.VariantId == variantId,
                cancellationToken)
            ?? throw new CatalogueValidationException(
                "sizeVariantId",
                "The size variant was not found.");

        var variantExists = await db.CatalogueSubmissionVariants.AnyAsync(
            value => value.Id == variantId && value.SubmissionId == submissionId,
            cancellationToken);

        if (!variantExists)
            throw new CatalogueValidationException("variantId", "The product variant was not found.");

        try
        {
            var duplicate = await db.CatalogueSubmissionSizeVariants.AnyAsync(
                value => value.VariantId == variantId &&
                         value.Id != sizeVariantId &&
                         value.ManufacturerSize.ToLower() == command.ManufacturerSize.Trim().ToLower(),
                cancellationToken);

            if (duplicate)
                throw new ArgumentException(
                    "A size with this manufacturer size already exists for this product variant.",
                    nameof(command.ManufacturerSize));

            var gtin = string.IsNullOrWhiteSpace(command.Gtin)
                ? null
                : command.Gtin.Trim();

            if (gtin is not null)
            {
                var alreadyInSubmission = await db.CatalogueSubmissionSizeVariants.AnyAsync(
                    value => value.Id != sizeVariantId && value.Gtin == gtin,
                    cancellationToken);

                if (alreadyInSubmission)
                    throw new ArgumentException(
                        $"GTIN {gtin} is already used by another size in this submission.",
                        nameof(command.Gtin));

                var alreadyInCatalogue = await db.ProductIdentifiers.AnyAsync(
                    identifier =>
                        identifier.Type == IdentifierType.Gtin &&
                        identifier.Value == gtin,
                    cancellationToken);

                if (alreadyInCatalogue)
                    throw new ArgumentException(
                        $"GTIN {gtin} is already assigned to a published catalogue product.",
                        nameof(command.Gtin));
            }

            size.Update(
                command.ManufacturerSize,
                command.WaistMinimumCm,
                command.WaistMaximumCm,
                command.HipMinimumCm,
                command.HipMaximumCm,
                command.ManufacturerStatedAbsorbencyMl,
                command.FitMeasurementBasis,
                command.AbsorbencyBasisMethod,
                command.AbsorbencySource,
                command.LengthMm,
                command.WidthMm,
                command.WeightGrams,
                command.ManufacturerPackQuantity,
                command.Gtin);

            await db.SaveChangesAsync(cancellationToken);
            return ToSizeVariantReceipt(size);
        }
        catch (ArgumentException exception)
        {
            throw new CatalogueValidationException(
                GetSizeVariantFieldName(exception.ParamName),
                exception.Message);
        }
    }

    public async Task RemoveSizeVariantAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid variantId,
        Guid sizeVariantId,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(submissionId, cancellationToken);
        EnsureDraftEditable(submission);

        var size = await db.CatalogueSubmissionSizeVariants
            .SingleOrDefaultAsync(
                value => value.Id == sizeVariantId && value.VariantId == variantId,
                cancellationToken)
            ?? throw new CatalogueValidationException(
                "sizeVariantId",
                "The size variant was not found.");

        var variantExists = await db.CatalogueSubmissionVariants.AnyAsync(
            value => value.Id == variantId && value.SubmissionId == submissionId,
            cancellationToken);

        if (!variantExists)
            throw new CatalogueValidationException("variantId", "The product variant was not found.");

        db.CatalogueSubmissionSizeVariants.Remove(size);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CatalogueSubmissionVariantOverrideReceipt?> GetVariantOverrideAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid variantId,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var variantExists = await db.CatalogueSubmissionVariants.AnyAsync(
            value => value.Id == variantId && value.SubmissionId == submissionId,
            cancellationToken);

        if (!variantExists)
            throw new CatalogueValidationException("variantId", "The product variant was not found.");

        var overrideValue = await db.CatalogueSubmissionVariantOverrides
            .AsNoTracking()
            .SingleOrDefaultAsync(value => value.VariantId == variantId, cancellationToken);

        return overrideValue is null ? null : ToVariantOverrideReceipt(overrideValue);
    }

    public async Task<CatalogueSubmissionVariantOverrideReceipt> UpdateVariantOverrideAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid variantId,
        UpdateCatalogueSubmissionVariantOverride command,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(submissionId, cancellationToken);
        EnsureDraftEditable(submission);

        var variant = await db.CatalogueSubmissionVariants
            .SingleOrDefaultAsync(
                value => value.Id == variantId && value.SubmissionId == submissionId,
                cancellationToken)
            ?? throw new CatalogueValidationException("variantId", "The product variant was not found.");

        try
        {
            var overrideValue = await db.CatalogueSubmissionVariantOverrides
                .SingleOrDefaultAsync(value => value.VariantId == variantId, cancellationToken);

            if (overrideValue is null)
            {
                overrideValue = new CatalogueSubmissionVariantOverride(variantId);
                db.CatalogueSubmissionVariantOverrides.Add(overrideValue);
            }

            overrideValue.Set(command.Attribute, command.Value);

            if (!overrideValue.HasAnyOverride)
                db.CatalogueSubmissionVariantOverrides.Remove(overrideValue);

            await db.SaveChangesAsync(cancellationToken);
            return ToVariantOverrideReceipt(overrideValue);
        }
        catch (ArgumentException exception)
        {
            throw new CatalogueValidationException("value", exception.Message);
        }
    }

    public async Task<CatalogueSubmissionVariantReceipt> AddVariantAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        AddCatalogueSubmissionVariant command,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(submissionId, cancellationToken);
        EnsureDraftEditable(submission);

        try
        {
            var name = string.IsNullOrWhiteSpace(command.Name)
                ? null
                : command.Name.Trim();

            if (name is null)
            {
                var baseExists = await db.CatalogueSubmissionVariants.AnyAsync(
                    value => value.SubmissionId == submissionId && value.Name == null,
                    cancellationToken);

                if (baseExists)
                    throw new ArgumentException(
                        "This product already has a base variant.",
                        nameof(command.Name));
            }
            else
            {
                var duplicate = await db.CatalogueSubmissionVariants.AnyAsync(
                    value => value.SubmissionId == submissionId &&
                             value.Name != null &&
                             value.Name.ToLower() == name.ToLower(),
                    cancellationToken);

                if (duplicate)
                    throw new ArgumentException(
                        "A product variant with this name already exists.",
                        nameof(command.Name));
            }

            var variant = new CatalogueSubmissionVariant(
                submissionId,
                name);

            db.CatalogueSubmissionVariants.Add(variant);
            await db.SaveChangesAsync(cancellationToken);

            return ToVariantReceipt(variant);
        }
        catch (ArgumentException exception)
        {
            throw new CatalogueValidationException("name", exception.Message);
        }
    }

    public async Task<CatalogueSubmissionVariantReceipt> UpdateVariantAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid variantId,
        UpdateCatalogueSubmissionVariant command,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(submissionId, cancellationToken);
        EnsureDraftEditable(submission);

        var variant = await db.CatalogueSubmissionVariants
            .SingleOrDefaultAsync(
                value => value.Id == variantId && value.SubmissionId == submissionId,
                cancellationToken)
            ?? throw new CatalogueValidationException(
                "variantId",
                "The product variant was not found.");

        try
        {
            var name = command.Name.Trim();

            var duplicate = await db.CatalogueSubmissionVariants.AnyAsync(
                value => value.SubmissionId == submissionId &&
                         value.Id != variantId &&
                         value.Name != null &&
                         value.Name.ToLower() == name.ToLower(),
                cancellationToken);

            if (duplicate)
                throw new ArgumentException(
                    "A product variant with this name already exists.",
                    nameof(command.Name));

            variant.Rename(name);
            await db.SaveChangesAsync(cancellationToken);
            return ToVariantReceipt(variant);
        }
        catch (ArgumentException exception)
        {
            throw new CatalogueValidationException("name", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            throw new CatalogueValidationException("variantId", exception.Message);
        }
    }

    public async Task RemoveVariantAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid variantId,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(submissionId, cancellationToken);
        EnsureDraftEditable(submission);

        var variants = await db.CatalogueSubmissionVariants
            .Where(value => value.SubmissionId == submissionId)
            .OrderBy(value => value.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var variant = variants.SingleOrDefault(value => value.Id == variantId)
            ?? throw new CatalogueValidationException(
                "variantId",
                "The product variant was not found.");

        if (variants.Count <= 1)
            throw new CatalogueValidationException(
                "variantId",
                "A product must have at least one variant.");

        db.CatalogueSubmissionVariants.Remove(variant);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CatalogueSubmissionReceipt> UpdateIdentityAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        UpdateCatalogueSubmissionIdentity command,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(submissionId, cancellationToken);

        try
        {
            submission.UpdateIdentity(
                command.ProposedGtin,
                command.ProposedSku,
                command.IdentitySourceUrl);
        }
        catch (ArgumentException exception)
        {
            throw new CatalogueValidationException(
                GetIdentityFieldName(exception.ParamName),
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            throw new CatalogueValidationException("status", exception.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToReceipt(submission);
    }

    public async Task<CatalogueSubmissionReceipt> ResolveEntitiesAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        ResolveCatalogueSubmissionEntities command,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(
            submissionId,
            cancellationToken);

        if (submission.Status is CatalogueSubmissionStatus.Published or CatalogueSubmissionStatus.Rejected)
            throw new CatalogueValidationException(
                "status",
                "Published or rejected submissions cannot have catalogue entities resolved.");

        if (command.ManufacturerId is null && string.IsNullOrWhiteSpace(command.NewManufacturerName))
            throw new CatalogueValidationException(
                "manufacturer",
                "Select an existing business or enter a new business name.");

        if (command.ManufacturerId is not null && !string.IsNullOrWhiteSpace(command.NewManufacturerName))
            throw new CatalogueValidationException(
                "manufacturer",
                "Choose either an existing business or a new business name, not both.");

        Manufacturer manufacturer;

        if (command.ManufacturerId is Guid manufacturerId)
        {
            manufacturer = await db.Manufacturers
                .SingleOrDefaultAsync(
                    value => value.Id == manufacturerId,
                    cancellationToken)
                ?? throw new CatalogueValidationException(
                    "manufacturer",
                    "The selected business could not be found.");
        }
        else
        {
            var manufacturerName = command.NewManufacturerName!.Trim();

            var existingManufacturer = await db.Manufacturers
                .SingleOrDefaultAsync(
                    value => value.Name.ToLower() == manufacturerName.ToLower(),
                    cancellationToken);

            if (existingManufacturer is not null)
                throw new CatalogueValidationException(
                    "manufacturer",
                    $"A canonical business named \"{existingManufacturer.Name}\" already exists. Select it instead of creating a duplicate.");

            manufacturer = new Manufacturer(
                manufacturerName,
                Slugify(manufacturerName));

            db.Manufacturers.Add(manufacturer);
        }

        Brand? brand = null;

        if (!string.IsNullOrWhiteSpace(submission.ProposedBrandName))
        {
            if (command.BrandId is null && string.IsNullOrWhiteSpace(command.NewBrandName))
                throw new CatalogueValidationException(
                    "brand",
                    "Select an existing brand or enter a new brand name.");

            if (command.BrandId is not null && !string.IsNullOrWhiteSpace(command.NewBrandName))
                throw new CatalogueValidationException(
                    "brand",
                    "Choose either an existing brand or a new brand name, not both.");

            if (command.BrandId is Guid brandId)
            {
                brand = await db.Brands
                    .SingleOrDefaultAsync(
                        value => value.Id == brandId,
                        cancellationToken)
                    ?? throw new CatalogueValidationException(
                        "brand",
                        "The selected brand could not be found.");

                if (brand.ManufacturerId != manufacturer.Id)
                    throw new CatalogueValidationException(
                        "brand",
                        "The selected brand does not belong to the selected business.");
            }
            else
            {
                var brandName = command.NewBrandName!.Trim();

                var existingBrand = await db.Brands
                    .SingleOrDefaultAsync(
                        value =>
                            value.ManufacturerId == manufacturer.Id &&
                            value.Name.ToLower() == brandName.ToLower(),
                        cancellationToken);

                if (existingBrand is not null)
                    throw new CatalogueValidationException(
                        "brand",
                        $"A brand named \"{existingBrand.Name}\" already exists for this business. Select it instead of creating a duplicate.");

                brand = new Brand(
                    manufacturer.Id,
                    brandName,
                    Slugify(brandName));

                db.Brands.Add(brand);
            }
        }

        submission.ResolveCanonicalEntities(
            manufacturer.Name,
            brand?.Name);

        await db.SaveChangesAsync(cancellationToken);

        return ToReceipt(submission);
    }

    public async Task<CatalogueSubmissionReceipt> UpdateSpecificationsAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        UpdateCatalogueSubmissionSpecifications command,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(submissionId, cancellationToken);

        try
        {
            submission.UpdateSpecifications(
                command.ProposedProductType,
                command.ProposedPackagingType,
                command.ProposedProductFamily,
                command.ProposedDescription,
                command.ProposedDescriptionVisibility,
                command.ProposedProductStatus,
                command.ProposedOfficialWebsiteUrl,
                command.SharedAppearance,
                command.SharedPrimaryColour,
                command.SharedWetnessIndicator,
                command.SharedStandingLeakGuards,
                command.SharedWaistbandStyle,
                command.SharedFragrance,
                command.SharedLatexFree,
                command.SharedDesignedFor,
                command.SharedFastenerCount,
                command.SharedConstructionNotes);
        }
        catch (ArgumentException exception)
        {
            throw new CatalogueValidationException("specifications", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            throw new CatalogueValidationException("status", exception.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToReceipt(submission);
    }

    public async Task<CatalogueSubmissionReceipt> UpdateDescriptionVisibilityAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CatalogueContentVisibility visibility,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(submissionId, cancellationToken);

        try
        {
            submission.UpdateDescriptionVisibility(visibility);
        }
        catch (ArgumentException exception)
        {
            throw new CatalogueValidationException("descriptionVisibility", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            throw new CatalogueValidationException("status", exception.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToReceipt(submission);
    }

    public async Task<CatalogueSubmissionRetailDestinationReceipt> AddRetailDestinationAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        AddCatalogueSubmissionRetailDestination command,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(submissionId, cancellationToken);

        if (submission.Status != CatalogueSubmissionStatus.InVerification)
            throw new CatalogueValidationException(
                "status",
                "Only submissions in verification can receive retail destinations.");

        var retailerExists = await db.Retailers
            .AnyAsync(
                retailer => retailer.Id == command.RetailerId,
                cancellationToken);

        if (!retailerExists)
            throw new CatalogueValidationException(
                "retailerId",
                "The specified retailer does not exist.");

        try
        {
            var destination = new CatalogueSubmissionRetailDestination(
                submission.Id,
                command.RetailerId,
                command.ListingUrl,
                command.Notes);

            db.CatalogueSubmissionRetailDestinations.Add(destination);
            await db.SaveChangesAsync(cancellationToken);

            return ToRetailDestinationReceipt(destination);
        }
        catch (ArgumentException exception)
        {
            throw new CatalogueValidationException(
                GetRetailDestinationFieldName(exception.ParamName),
                exception.Message);
        }
    }

    public async Task<CatalogueSubmissionRetailWorkspace> GetRetailWorkspaceAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await db.CatalogueSubmissions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                value => value.Id == submissionId,
                cancellationToken)
            ?? throw new CatalogueValidationException(
                "submissionId",
                "The catalogue submission was not found.");

        var destinations = await (
            from destination in db.CatalogueSubmissionRetailDestinations.AsNoTracking()
            join retailer in db.Retailers.AsNoTracking()
                on destination.RetailerId equals retailer.Id
            join affiliate in db.CatalogueSubmissionRetailAffiliates.AsNoTracking()
                on destination.Id equals affiliate.RetailDestinationId into affiliateGroup
            from affiliate in affiliateGroup.DefaultIfEmpty()
            where destination.SubmissionId == submissionId
            orderby retailer.Name
            select new CatalogueSubmissionRetailDestinationDetails(
                destination.Id,
                destination.RetailerId,
                retailer.Name,
                destination.ListingUrl,
                destination.Notes,
                destination.AddedAtUtc,
                affiliate == null
                    ? null
                    : new CatalogueSubmissionRetailAffiliateReceipt(
                        affiliate.Id,
                        affiliate.SubmissionId,
                        affiliate.RetailDestinationId,
                        affiliate.Status,
                        affiliate.Network,
                        affiliate.TrackingConfiguration,
                        affiliate.DeepLinkMechanism,
                        affiliate.TermsUrl,
                        affiliate.ApplicationReference,
                        affiliate.Notes,
                        affiliate.LastVerifiedAtUtc)))
            .ToListAsync(cancellationToken);

        return new CatalogueSubmissionRetailWorkspace(
            submission.Id,
            submission.Status,
            submission.ProposedProductName,
            submission.ProposedVariantName ?? "Multiple variants",
            destinations);
    }

    public async Task<CatalogueSubmissionImagesWorkspace> GetImagesAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        _ = await GetSubmissionAsync(submissionId, cancellationToken);

        var images = (await db.CatalogueSubmissionImages
            .AsNoTracking()
            .Where(value => value.SubmissionId == submissionId)
            .OrderBy(value => value.Role)
            .ThenBy(value => value.CreatedAtUtc)
            .ToListAsync(cancellationToken))
            .Select(ToImageReceipt)
            .ToList();

        return new CatalogueSubmissionImagesWorkspace(submissionId, images);
    }

    public async Task<CatalogueSubmissionImageReceipt> AddImageAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        AddCatalogueSubmissionImage command,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(submissionId, cancellationToken);
        EnsureDraftEditable(submission);
        ValidateImageUpload(command);

        var existing = command.Role == CatalogueSubmissionImageRole.Other
            ? null
            : await db.CatalogueSubmissionImages
                .SingleOrDefaultAsync(
                    value => value.SubmissionId == submissionId && value.Role == command.Role,
                    cancellationToken);

        var extension = command.ContentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => throw new ArgumentException("Only JPEG, PNG and WebP images are supported.", nameof(command))
        };

        var storageKey = $"{submissionId:N}/{Guid.NewGuid():N}{extension}";
        var image = new CatalogueSubmissionImage(
            submissionId,
            command.Role,
            storageKey,
            command.OriginalFileName,
            command.ContentType,
            command.FileSizeBytes,
            command.SourceType,
            command.SourceUrl,
            command.SourceNotes,
            command.PermissionStatus,
            command.PermissionEvidence);

        try
        {
            await imageStorage.SaveAsync(storageKey, command.Content, cancellationToken);

            if (existing is not null)
                db.CatalogueSubmissionImages.Remove(existing);

            db.CatalogueSubmissionImages.Add(image);
            await db.SaveChangesAsync(cancellationToken);

            if (existing is not null)
                await imageStorage.DeleteAsync(existing.StorageKey, cancellationToken);

            return ToImageReceipt(image);
        }
        catch
        {
            await imageStorage.DeleteAsync(storageKey, cancellationToken);
            throw;
        }
    }

    public async Task<CatalogueSubmissionImageReceipt> UpdateImageMetadataAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid imageId,
        UpdateCatalogueSubmissionImageMetadata command,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(submissionId, cancellationToken);
        EnsureImageMetadataEditable(submission);

        var image = await db.CatalogueSubmissionImages
            .SingleOrDefaultAsync(
                value => value.Id == imageId && value.SubmissionId == submissionId,
                cancellationToken)
            ?? throw new KeyNotFoundException("The catalogue submission image was not found.");

        image.UpdateMetadata(
            command.SourceType,
            command.SourceUrl,
            command.SourceNotes,
            command.PermissionStatus,
            command.PermissionEvidence);

        await db.SaveChangesAsync(cancellationToken);
        return ToImageReceipt(image);
    }

    public async Task RemoveImageAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(submissionId, cancellationToken);
        EnsureDraftEditable(submission);

        var image = await db.CatalogueSubmissionImages
            .SingleOrDefaultAsync(
                value => value.Id == imageId && value.SubmissionId == submissionId,
                cancellationToken)
            ?? throw new KeyNotFoundException("The catalogue submission image was not found.");

        db.CatalogueSubmissionImages.Remove(image);
        await db.SaveChangesAsync(cancellationToken);
        await imageStorage.DeleteAsync(image.StorageKey, cancellationToken);
    }

    public async Task<CatalogueSubmissionImageContent?> GetImageContentAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var image = await db.CatalogueSubmissionImages
            .AsNoTracking()
            .SingleOrDefaultAsync(
                value => value.Id == imageId && value.SubmissionId == submissionId,
                cancellationToken);

        if (image is null)
            return null;

        var stream = await imageStorage.OpenReadAsync(image.StorageKey, cancellationToken);
        return stream is null
            ? null
            : new CatalogueSubmissionImageContent(
                stream,
                image.ContentType,
                image.OriginalFileName);
    }

    public async Task<CatalogueSubmissionVerificationWorkspace> GetVerificationWorkspaceAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await db.CatalogueSubmissions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                value => value.Id == submissionId,
                cancellationToken)
            ?? throw new CatalogueValidationException(
                "submissionId",
                "The catalogue submission was not found.");

        var verifications = await db.CatalogueSubmissionVerifications
            .AsNoTracking()
            .Where(value => value.SubmissionId == submissionId)
            .OrderBy(value => value.Area)
            .ThenBy(value => value.VerifiedAtUtc)
            .Select(value => new CatalogueSubmissionVerificationReceipt(
                value.Id,
                value.SubmissionId,
                value.VerifiedByUserId,
                value.Area,
                value.Status,
                value.Scope,
                value.Source,
                value.SourceUrl,
                value.Notes,
                value.PermissionTerms,
                value.VerifiedAtUtc))
            .ToListAsync(cancellationToken);

        return new CatalogueSubmissionVerificationWorkspace(
            submission.Id,
            submission.Status,
            submission.ProposedProductName,
            submission.ProposedVariantName ?? "Multiple variants",
            verifications);
    }

    public async Task<CatalogueSubmissionRetailAffiliateReceipt> AddAffiliateAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        AddCatalogueSubmissionRetailAffiliate command,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(
            submissionId,
            cancellationToken);

        if (submission.Status != CatalogueSubmissionStatus.InVerification)
            throw new CatalogueValidationException(
                "status",
                "Only submissions in verification can receive affiliate records.");

        var destination = await db.CatalogueSubmissionRetailDestinations
            .SingleOrDefaultAsync(
                value =>
                    value.Id == command.RetailDestinationId &&
                    value.SubmissionId == submissionId,
                cancellationToken);

        if (destination is null)
            throw new CatalogueValidationException(
                "retailDestinationId",
                "The specified retail destination does not belong to this submission.");

        var affiliateExists = await db.CatalogueSubmissionRetailAffiliates
            .AnyAsync(
                value => value.RetailDestinationId == command.RetailDestinationId,
                cancellationToken);

        if (affiliateExists)
            throw new CatalogueValidationException(
                "retailDestinationId",
                "The retail destination already has an affiliate record.");

        try
        {
            var affiliate = new CatalogueSubmissionRetailAffiliate(
                submission.Id,
                destination.Id,
                command.Status,
                command.Network,
                command.TrackingConfiguration,
                command.DeepLinkMechanism,
                command.TermsUrl,
                command.ApplicationReference,
                command.Notes);

            db.CatalogueSubmissionRetailAffiliates.Add(affiliate);
            await db.SaveChangesAsync(cancellationToken);

            return ToAffiliateReceipt(affiliate);
        }
        catch (ArgumentException exception)
        {
            throw new CatalogueValidationException(
                "affiliate",
                exception.Message);
        }
    }

    public async Task<CatalogueSubmissionEditorialDecisionReceipt> ReviewAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        ReviewCatalogueSubmission command,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(
            submissionId,
            cancellationToken);

        if (submission.Status != CatalogueSubmissionStatus.ReadyForReview)
            throw new CatalogueValidationException(
                "status",
                "Only submissions ready for review can receive an editorial decision.");

        try
        {
            var decision = new CatalogueSubmissionEditorialDecision(
                submission.Id,
                actor.UserId,
                command.Outcome,
                command.Rationale);

            switch (command.Outcome)
            {
                case EditorialOutcome.Accepted:
                    submission.Approve();
                    break;

                case EditorialOutcome.Rejected:
                    submission.Reject();
                    break;

                case EditorialOutcome.RequestAdditionalEvidence:
                    submission.MarkNeedsChanges();
                    break;

                case EditorialOutcome.Deferred:
                    break;

                default:
                    throw new ArgumentException(
                        "The editorial outcome is invalid.",
                        nameof(command.Outcome));
            }

            db.CatalogueSubmissionEditorialDecisions.Add(decision);
            await db.SaveChangesAsync(cancellationToken);

            return ToEditorialDecisionReceipt(decision);
        }
        catch (ArgumentException exception)
        {
            throw new CatalogueValidationException(
                GetEditorialOutcomeFieldName(exception.ParamName),
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            throw new CatalogueValidationException(
                "status",
                exception.Message);
        }
    }

    public async Task<CatalogueSubmissionReceipt> ReturnToVerificationAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(
            submissionId,
            cancellationToken);

        try
        {
            submission.ReturnToVerification();
        }
        catch (InvalidOperationException exception)
        {
            throw new CatalogueValidationException(
                "status",
                exception.Message);
        }

        await db.SaveChangesAsync(cancellationToken);

        return ToReceipt(submission);
    }

    public async Task<CataloguePublicationReceipt> PublishAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(
            submissionId,
            cancellationToken);

        if (submission.Status != CatalogueSubmissionStatus.Approved)
            throw new CatalogueValidationException(
                "status",
                "Only approved submissions can be published.");

        if (submission.PublishedProductId is not null)
            throw new CatalogueValidationException(
                "status",
                "This submission has already been published.");

        if (submission.ProposedProductType is null)
            throw new CatalogueValidationException(
                "proposedProductType",
                "A product type is required before publication.");

        if (submission.ProposedPackagingType is null)
            throw new CatalogueValidationException(
                "proposedPackagingType",
                "A packaging type is required before publication.");

        var manufacturer = await db.Manufacturers
            .SingleOrDefaultAsync(
                value => value.Name == submission.ProposedManufacturerName,
                cancellationToken);

        if (manufacturer is null)
            throw new CatalogueValidationException(
                "proposedManufacturerName",
                "The proposed manufacturer must resolve to an existing canonical manufacturer before publication.");

        Guid? brandId = null;

        if (!string.IsNullOrWhiteSpace(submission.ProposedBrandName))
        {
            var brand = await db.Brands
                .SingleOrDefaultAsync(
                    value =>
                        value.ManufacturerId == manufacturer.Id &&
                        value.Name == submission.ProposedBrandName,
                    cancellationToken);

            if (brand is null)
                throw new CatalogueValidationException(
                    "proposedBrandName",
                    "The proposed brand must resolve to an existing brand for the selected manufacturer before publication.");

            brandId = brand.Id;
        }

        var requiredAreas = new[]
        {
            CatalogueVerificationArea.ProductIdentity,
            CatalogueVerificationArea.Specifications
        };

        var verifiedAreas = await db.CatalogueSubmissionVerifications
            .Where(value =>
                value.SubmissionId == submission.Id &&
                requiredAreas.Contains(value.Area) &&
                (value.Status == CatalogueVerificationStatus.Verified ||
                 value.Status == CatalogueVerificationStatus.Inherited))
            .Select(value => value.Area)
            .Distinct()
            .ToListAsync(cancellationToken);

        var missingArea = requiredAreas
            .FirstOrDefault(area => !verifiedAreas.Contains(area));

        if (!verifiedAreas.Contains(missingArea))
            throw new CatalogueValidationException(
                "verification",
                $"The {missingArea} verification area must be verified or inherited before publication.");

        var submissionVariants = await db.CatalogueSubmissionVariants
            .AsNoTracking()
            .Where(value => value.SubmissionId == submission.Id)
            .OrderBy(value => value.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (submissionVariants.Count == 0)
            throw new CatalogueValidationException(
                "variants",
                "At least one product variant is required before publication.");

        var variantIds = submissionVariants.Select(value => value.Id).ToArray();

        var submissionSizes = await db.CatalogueSubmissionSizeVariants
            .AsNoTracking()
            .Where(value => variantIds.Contains(value.VariantId))
            .OrderBy(value => value.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var sizesByVariant = submissionSizes
            .GroupBy(value => value.VariantId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

        if (submissionVariants.Any(variant =>
                !sizesByVariant.TryGetValue(variant.Id, out var sizes) ||
                sizes.Count == 0))
        {
            throw new CatalogueValidationException(
                "sizes",
                "Every product variant must have at least one size variant before publication.");
        }

        if (submissionSizes.Any(size => size.ManufacturerPackQuantity is null))
            throw new CatalogueValidationException(
                "manufacturerPackQuantity",
                "Manufacturer pack quantity is required for every size before publication.");

        var overrides = await db.CatalogueSubmissionVariantOverrides
            .AsNoTracking()
            .Where(value => variantIds.Contains(value.VariantId))
            .ToDictionaryAsync(value => value.VariantId, cancellationToken);

        var canonicalVariants = submissionVariants
            .Select(variant =>
            {
                overrides.TryGetValue(variant.Id, out var overrideValue);

                var sizes = sizesByVariant[variant.Id]
                    .Select(size => new CreateCanonicalProductSizeVariant(
                        size.ManufacturerSize,
                        size.WaistMinimumCm,
                        size.WaistMaximumCm,
                        size.HipMinimumCm,
                        size.HipMaximumCm,
                        size.ManufacturerStatedAbsorbencyMl,
                        size.FitMeasurementBasis,
                        size.AbsorbencyBasisMethod,
                        size.AbsorbencySource,
                        size.LengthMm,
                        size.WidthMm,
                        size.WeightGrams,
                        size.ManufacturerPackQuantity!.Value,
                        submission.ProposedPackagingType.Value,
                        size.Gtin))
                    .ToList();

                return new CreateCanonicalProductVariant(
                    variant.Name ?? submission.ProposedProductName,
                    overrideValue?.BackingType ?? BackingType.Unknown,
                    overrideValue?.FastenerType ?? FastenerType.Unknown,
                    ParseAppearance(overrideValue?.PrintDesign ?? submission.SharedPrintDesign),
                    overrideValue?.PrimaryColour ?? submission.SharedPrimaryColour,
                    overrideValue?.HasWetnessIndicator ?? submission.SharedWetnessIndicator,
                    overrideValue?.HasStandingLeakGuards ?? submission.SharedStandingLeakGuards,
                    overrideValue?.WaistbandStyle ?? submission.SharedWaistbandStyle ?? WaistbandStyle.Unknown,
                    overrideValue?.Fragrance ?? submission.SharedFragrance ?? FragranceType.Unknown,
                    overrideValue?.IsLatexFree ?? submission.SharedLatexFree,
                    overrideValue?.DesignedFor ?? submission.SharedDesignedFor,
                    overrideValue?.FastenerCount ?? submission.SharedFastenerCount,
                    overrideValue?.ConstructionNotes ?? submission.SharedConstructionNotes,
                    sizes);
            })
            .ToList();

        var productSlug = Slugify(
            submission.ProposedProductName);

        var canonicalReceipt = await canonicalCatalogue.CreateProductAsync(
            actor,
            new CreateCanonicalProduct(
                manufacturer.Id,
                brandId,
                submission.ProposedProductName,
                productSlug,
                submission.ProposedProductType.Value,
                submission.ProposedProductStatus ?? ProductStatus.Current,
                canonicalVariants,
                "Published from an approved catalogue submission.",
                new[]
                {
                    $"/api/v1/catalogue-submissions/{submission.Id}"
                },
                "Editorially approved catalogue submission.",
                submission.Id.ToString(),
                submission.ProposedProductFamily,
                submission.ProposedDescription,
                submission.ProposedOfficialWebsiteUrl,
                submission.ProposedDescriptionVisibility),
            cancellationToken);

        var submissionImages = await db.CatalogueSubmissionImages
            .Where(value => value.SubmissionId == submission.Id && value.ProductId == null)
            .ToListAsync(cancellationToken);

        foreach (var image in submissionImages)
            image.PublishToProduct(canonicalReceipt.ProductId);

        submission.Publish(
            canonicalReceipt.ProductId);

        await db.SaveChangesAsync(
            cancellationToken);

        return new CataloguePublicationReceipt(
            submission.Id,
            canonicalReceipt.ProductId,
            canonicalReceipt.ProductVariantId,
            canonicalReceipt.SizeVariantId,
            canonicalReceipt.PackTypeId,
            canonicalReceipt.AuditRecordId,
            canonicalReceipt.Gtin);
    }

    private static string Slugify(string value)
    {
        var slug = new string(
            value
                .Trim()
                .ToLowerInvariant()
                .Select(character =>
                    char.IsLetterOrDigit(character)
                        ? character
                        : '-')
                .ToArray());

        while (slug.Contains(
            "--",
            StringComparison.Ordinal))
        {
            slug = slug.Replace(
                "--",
                "-",
                StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }

    public async Task<CatalogueSubmissionVerificationReceipt> AddVerificationAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        AddCatalogueSubmissionVerification command,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(
            submissionId,
            cancellationToken);

        if (submission.Status != CatalogueSubmissionStatus.InVerification)
            throw new CatalogueValidationException(
                "status",
                "Only submissions in verification can receive verification records.");

        try
        {
            var verification = new CatalogueSubmissionVerification(
                submission.Id,
                actor.UserId,
                command.Area,
                command.Status,
                command.Scope.Trim(),
                command.Source.Trim(),
                command.SourceUrl?.Trim(),
                command.Notes?.Trim(),
                command.PermissionTerms?.Trim());

            db.CatalogueSubmissionVerifications.Add(
                verification);

            await db.SaveChangesAsync(
                cancellationToken);

            return ToVerificationReceipt(
                verification);
        }
        catch (ArgumentException exception)
        {
            throw new CatalogueValidationException(
                "verification",
                exception.Message);
        }
    }

    public async Task<CatalogueSubmissionReceipt> BeginVerificationAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(
            submissionId,
            cancellationToken);

        try
        {
            submission.BeginVerification();
        }
        catch (InvalidOperationException exception)
        {
            throw new CatalogueValidationException(
                "status",
                exception.Message);
        }

        await db.SaveChangesAsync(
            cancellationToken);

        return ToReceipt(submission);
    }

    public async Task<CatalogueSubmissionReceipt> MarkReadyForReviewAsync(
        AuthenticatedUser actor,
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        var submission = await GetSubmissionAsync(
            submissionId,
            cancellationToken);

        try
        {
            submission.MarkReadyForReview();
        }
        catch (InvalidOperationException exception)
        {
            throw new CatalogueValidationException(
                "status",
                exception.Message);
        }

        await db.SaveChangesAsync(
            cancellationToken);

        return ToReceipt(submission);
    }

    private async Task RequireModeratorAsync(
        AuthenticatedUser actor,
        CancellationToken cancellationToken)
    {
        if (!await editorialAuthorisation.CanPublishAtlasAsync(
                actor,
                cancellationToken))
        {
            throw new UnauthorizedAccessException(
                "Only an assigned Moderator may manage catalogue submissions.");
        }
    }

    private async Task<CatalogueSubmission> GetSubmissionAsync(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        return await db.CatalogueSubmissions
            .SingleOrDefaultAsync(
                submission => submission.Id == submissionId,
                cancellationToken)
            ?? throw new CatalogueValidationException(
                "submissionId",
                "The catalogue submission was not found.");
    }

    private static CatalogueSubmissionRetailDestinationReceipt ToRetailDestinationReceipt(
        CatalogueSubmissionRetailDestination destination) =>
        new(
            destination.Id,
            destination.SubmissionId,
            destination.RetailerId,
            destination.ListingUrl,
            destination.Notes,
            destination.AddedAtUtc);

    private static CatalogueSubmissionEditorialDecisionReceipt ToEditorialDecisionReceipt(
        CatalogueSubmissionEditorialDecision decision)
        => new(
            decision.Id,
            decision.SubmissionId,
            decision.ModeratorUserId,
            decision.Outcome,
            decision.Rationale,
            decision.DecidedAtUtc);

    private static CatalogueSubmissionRetailAffiliateReceipt ToAffiliateReceipt(
        CatalogueSubmissionRetailAffiliate affiliate) =>
        new(
            affiliate.Id,
            affiliate.SubmissionId,
            affiliate.RetailDestinationId,
            affiliate.Status,
            affiliate.Network,
            affiliate.TrackingConfiguration,
            affiliate.DeepLinkMechanism,
            affiliate.TermsUrl,
            affiliate.ApplicationReference,
            affiliate.Notes,
            affiliate.LastVerifiedAtUtc);

    private static CatalogueSubmissionVerificationReceipt ToVerificationReceipt(
        CatalogueSubmissionVerification verification) =>
        new(
            verification.Id,
            verification.SubmissionId,
            verification.VerifiedByUserId,
            verification.Area,
            verification.Status,
            verification.Scope,
            verification.Source,
            verification.SourceUrl,
            verification.Notes,
            verification.PermissionTerms,
            verification.VerifiedAtUtc);

    private static void ValidateImageUpload(AddCatalogueSubmissionImage command)
    {
        if (!Enum.IsDefined(command.Role))
            throw new ArgumentException("The image role is invalid.", nameof(command));

        if (!Enum.IsDefined(command.SourceType))
            throw new ArgumentException("The image source type is invalid.", nameof(command));

        if (!Enum.IsDefined(command.PermissionStatus))
            throw new ArgumentException("The image permission status is invalid.", nameof(command));

        if (command.FileSizeBytes is <= 0 or > 15 * 1024 * 1024)
            throw new ArgumentException("Images must be greater than zero and no larger than 15 MB.", nameof(command));

        if (string.IsNullOrWhiteSpace(command.OriginalFileName))
            throw new ArgumentException("An image file name is required.", nameof(command));

        if (command.OriginalFileName.Trim().Length > 255)
            throw new ArgumentException("Image file names must be 255 characters or fewer.", nameof(command));

        if (command.ContentType is not "image/jpeg" and not "image/png" and not "image/webp")
            throw new ArgumentException("Only JPEG, PNG and WebP images are supported.", nameof(command));
    }

    private static void EnsureImageMetadataEditable(CatalogueSubmission submission)
    {
        if (submission.Status is not CatalogueSubmissionStatus.Draft and
            not CatalogueSubmissionStatus.NeedsChanges and
            not CatalogueSubmissionStatus.Published)
            throw new CatalogueValidationException(
                "status",
                "Image rights metadata can be edited while a submission is being prepared or after publication.");
    }

    private static void EnsureDraftEditable(CatalogueSubmission submission)
    {
        if (submission.Status is not CatalogueSubmissionStatus.Draft and not CatalogueSubmissionStatus.NeedsChanges)
            throw new CatalogueValidationException(
                "status",
                "Only draft submissions or submissions needing changes can be edited.");
    }

    private static CatalogueSubmissionSizeVariantReceipt ToSizeVariantReceipt(
        CatalogueSubmissionSizeVariant size) =>
        new(
            size.Id,
            size.VariantId,
            size.ManufacturerSize,
            size.WaistMinimumCm,
            size.WaistMaximumCm,
            size.HipMinimumCm,
            size.HipMaximumCm,
            size.ManufacturerStatedAbsorbencyMl,
            size.FitMeasurementBasis,
            size.AbsorbencyBasisMethod,
            size.AbsorbencySource,
            size.LengthMm,
            size.WidthMm,
            size.WeightGrams,
            size.ManufacturerPackQuantity,
            size.Gtin,
            size.CreatedAtUtc,
            size.UpdatedAtUtc);

    private static string GetSizeVariantFieldName(string? parameterName) =>
        parameterName switch
        {
            "manufacturerSize" => "manufacturerSize",
            "waistMinimumCm" or "waistMaximumCm" => "waist",
            "hipMinimumCm" or "hipMaximumCm" => "hip",
            "manufacturerStatedAbsorbencyMl" => "absorbency",
            "lengthMm" => "length",
            "widthMm" => "width",
            "weightGrams" => "weight",
            "manufacturerPackQuantity" => "manufacturerPackQuantity",
            "gtin" => "gtin",
            _ => "size"
        };

    private static CatalogueSubmissionVariantReceipt ToVariantReceipt(
        CatalogueSubmissionVariant variant) =>
        new(
            variant.Id,
            variant.SubmissionId,
            variant.Name,
            variant.CreatedAtUtc,
            variant.UpdatedAtUtc);

    private static CatalogueSubmissionVariantOverrideReceipt ToVariantOverrideReceipt(
        CatalogueSubmissionVariantOverride value) =>
        new(
            value.VariantId,
            value.BackingType,
            value.FastenerType,
            ParseAppearance(value.PrintDesign),
            ParseColour(value.PrimaryColour),
            value.HasWetnessIndicator,
            value.HasStandingLeakGuards,
            value.WaistbandStyle,
            value.Fragrance,
            value.IsLatexFree,
            ParseDesignedFor(value.DesignedFor),
            value.FastenerCount,
            value.ConstructionNotes);

    private static CatalogueVariantColour? ParseColour(string? value) =>
        Enum.TryParse<CatalogueVariantColour>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : null;

    private static CatalogueVariantDesignedFor? ParseDesignedFor(string? value) =>
        Enum.TryParse<CatalogueVariantDesignedFor>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : null;

    private static CatalogueVariantAppearance? ParseAppearance(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Enum.TryParse<CatalogueVariantAppearance>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : CatalogueVariantAppearance.Printed;
    }

    private static CatalogueSubmissionImageReceipt ToImageReceipt(CatalogueSubmissionImage image) =>
        new(
            image.Id,
            image.SubmissionId,
            image.Role,
            image.OriginalFileName,
            image.ContentType,
            image.FileSizeBytes,
            image.SourceType,
            image.SourceUrl,
            image.SourceNotes,
            image.PermissionStatus,
            image.PermissionEvidence,
            image.Visibility,
            $"/api/v1/catalogue-submissions/{image.SubmissionId}/images/{image.Id}/content",
            image.CreatedAtUtc,
            image.UpdatedAtUtc);

    private static CatalogueSubmissionReceipt ToReceipt(
        CatalogueSubmission submission) =>
        new(
            submission.Id,
            submission.Status,
            submission.Source,
            submission.ProposedManufacturerName,
            submission.ProposedBrandName,
            submission.ProposedProductName,
            submission.ProposedVariantName,
            submission.ProposedGtin,
            submission.ProposedSku,
            submission.IdentitySourceUrl,
            submission.ProposedProductType,
            submission.ProposedProductFamily,
            submission.ProposedDescription,
            submission.ProposedDescriptionVisibility,
            submission.ProposedProductStatus,
            submission.ProposedOfficialWebsiteUrl,
            submission.ProposedPackagingType,
            ParseAppearance(submission.SharedPrintDesign),
            submission.SharedPrimaryColour,
            submission.SharedWetnessIndicator,
            submission.SharedStandingLeakGuards,
            submission.SharedWaistbandStyle,
            submission.SharedFragrance,
            submission.SharedLatexFree,
            submission.SharedDesignedFor,
            submission.SharedFastenerCount,
            submission.SharedConstructionNotes,
            submission.Notes,
            submission.CreatedAtUtc,
            submission.UpdatedAtUtc);

    private static string GetEditorialOutcomeFieldName(
        string? parameterName)
        => parameterName switch
        {
            "outcome" => "outcome",
            "rationale" => "rationale",
            _ => "editorial"
        };

    private static string GetRetailDestinationFieldName(
        string? parameterName) =>
        parameterName switch
        {
            "retailerId" => "retailerId",
            "listingUrl" => "listingUrl",
            "notes" => "notes",
            _ => "destination"
        };

    private static string GetIdentityFieldName(
        string? parameterName) =>
        parameterName switch
        {
            nameof(UpdateCatalogueSubmissionIdentity.ProposedGtin)
                => "proposedGtin",
            nameof(UpdateCatalogueSubmissionIdentity.ProposedSku)
                => "proposedSku",
            nameof(UpdateCatalogueSubmissionIdentity.IdentitySourceUrl)
                => "identitySourceUrl",
            _ => "identity"
        };

    private static string GetFieldName(
        string? parameterName) =>
        parameterName switch
        {
            nameof(CreateCatalogueSubmission.ProposedManufacturerName)
                => "proposedManufacturerName",
            nameof(CreateCatalogueSubmission.ProposedProductName)
                => "proposedProductName",
            nameof(CreateCatalogueSubmission.ProposedVariantName)
                => "proposedVariantName",
            _ => "submission"
        };
}