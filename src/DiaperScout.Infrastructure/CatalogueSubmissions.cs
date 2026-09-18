using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiaperScout.Infrastructure;

internal sealed class CatalogueSubmissions(
    DiaperScoutDbContext db,
    IEditorialAuthorisation editorialAuthorisation,
    ICanonicalCatalogue canonicalCatalogue) : ICatalogueSubmissions
{
    public async Task<IReadOnlyList<CatalogueSubmissionQueueItem>> GetSubmissionsAsync(
        AuthenticatedUser actor,
        CancellationToken cancellationToken = default)
    {
        await RequireModeratorAsync(actor, cancellationToken);

        return await db.CatalogueSubmissions
            .AsNoTracking()
            .OrderByDescending(value => value.UpdatedAtUtc)
            .Select(value => new CatalogueSubmissionQueueItem(
                value.Id,
                value.Status,
                value.ProposedManufacturerName,
                value.ProposedBrandName,
                value.ProposedProductName,
                value.CreatedAtUtc,
                value.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
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
        db.CatalogueSubmissionVariants.RemoveRange(variants);
        db.CatalogueSubmissions.Remove(submission);

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
                value.CapacityMl,
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
                command.CapacityMl,
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
                command.CapacityMl,
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
                command.ProposedManufacturerSize,
                command.ProposedWaistMinimumCm,
                command.ProposedWaistMaximumCm,
                command.ProposedBackingType,
                command.ProposedFastenerType,
                command.ProposedWaistbandStyle,
                command.ProposedFragranceType,
                command.ProposedQuantityPerPack,
                command.ProposedPackagingType,
                command.ProposedProductFamily,
                command.ProposedDescription,
                command.ProposedProductStatus,
                command.ProposedOfficialWebsiteUrl,
                command.SharedPrintDesign,
                command.SharedPrimaryColour,
                command.SharedSecondaryColours,
                command.SharedWetnessIndicator,
                command.SharedStandingLeakGuards,
                command.SharedInnerLeakGuards,
                command.SharedElasticWaistbandFront,
                command.SharedElasticWaistbandRear,
                command.SharedLatexFree,
                command.SharedChlorineFree,
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
            CatalogueVerificationArea.Specifications,
            CatalogueVerificationArea.ContentAndRights,
            CatalogueVerificationArea.Retail
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

        if (!await db.CatalogueSubmissionRetailDestinations
            .AnyAsync(
                value => value.SubmissionId == submission.Id,
                cancellationToken))
        {
            throw new CatalogueValidationException(
                "retail",
                "At least one retail destination is required before publication.");
        }

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
                        size.CapacityMl,
                        size.LengthMm,
                        size.WidthMm,
                        size.WeightGrams,
                        size.ManufacturerPackQuantity!.Value,
                        submission.ProposedPackagingType.Value,
                        size.Gtin))
                    .ToList();

                return new CreateCanonicalProductVariant(
                    variant.Name ?? submission.ProposedProductName,
                    overrideValue?.BackingType ?? submission.ProposedBackingType ?? BackingType.Unknown,
                    overrideValue?.FastenerType ?? submission.ProposedFastenerType ?? FastenerType.Unknown,
                    overrideValue?.PrintDesign ?? submission.SharedPrintDesign,
                    overrideValue?.PrimaryColour ?? submission.SharedPrimaryColour,
                    overrideValue?.SecondaryColours ?? submission.SharedSecondaryColours,
                    overrideValue?.HasWetnessIndicator ?? submission.SharedWetnessIndicator,
                    overrideValue?.HasStandingLeakGuards ?? submission.SharedStandingLeakGuards,
                    overrideValue?.HasInnerLeakGuards ?? submission.SharedInnerLeakGuards,
                    overrideValue?.HasElasticWaistbandFront ?? submission.SharedElasticWaistbandFront,
                    overrideValue?.HasElasticWaistbandRear ?? submission.SharedElasticWaistbandRear,
                    overrideValue?.WaistbandStyle ?? submission.ProposedWaistbandStyle ?? WaistbandStyle.Unknown,
                    overrideValue?.Fragrance ?? submission.ProposedFragranceType ?? FragranceType.Unknown,
                    overrideValue?.IsLatexFree ?? submission.SharedLatexFree,
                    overrideValue?.IsChlorineFree ?? submission.SharedChlorineFree,
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
                submission.ProposedOfficialWebsiteUrl),
            cancellationToken);

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
            size.CapacityMl,
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
            "capacityMl" => "capacity",
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
            value.PrintDesign,
            value.PrimaryColour,
            value.SecondaryColours,
            value.HasWetnessIndicator,
            value.HasStandingLeakGuards,
            value.HasInnerLeakGuards,
            value.HasElasticWaistbandFront,
            value.HasElasticWaistbandRear,
            value.WaistbandStyle,
            value.Fragrance,
            value.IsLatexFree,
            value.IsChlorineFree,
            value.FastenerCount,
            value.ConstructionNotes);

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
            submission.ProposedProductStatus,
            submission.ProposedOfficialWebsiteUrl,
            submission.ProposedManufacturerSize,
            submission.ProposedWaistMinimumCm,
            submission.ProposedWaistMaximumCm,
            submission.ProposedBackingType,
            submission.ProposedFastenerType,
            submission.ProposedWaistbandStyle,
            submission.ProposedFragranceType,
            submission.ProposedQuantityPerPack,
            submission.ProposedPackagingType,
            submission.SharedPrintDesign,
            submission.SharedPrimaryColour,
            submission.SharedSecondaryColours,
            submission.SharedWetnessIndicator,
            submission.SharedStandingLeakGuards,
            submission.SharedInnerLeakGuards,
            submission.SharedElasticWaistbandFront,
            submission.SharedElasticWaistbandRear,
            submission.SharedLatexFree,
            submission.SharedChlorineFree,
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