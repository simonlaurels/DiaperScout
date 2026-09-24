using System.Linq.Expressions;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace DiaperScout.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiaperScoutInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("diaperscout")
            ?? throw new InvalidOperationException("Connection string 'diaperscout' is required.");

        services.AddDbContext<DiaperScoutDbContext>(options => options.UseNpgsql(connectionString));
        services.AddHttpContextAccessor();
        services.AddDataProtection();
        services.Configure<AwinAffiliateProgrammeDiscoveryOptions>(configuration.GetSection(AwinAffiliateProgrammeDiscoveryOptions.SectionName));
        services.Configure<RetailerDiscoveryJobOptions>(configuration.GetSection(RetailerDiscoveryJobOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IRetailerDiscoveryScheduler, RetailerDiscoveryScheduler>();
        services.AddHostedService<RetailerDiscoveryBackgroundService>();

        services.AddScoped<IAtlasQueries, AtlasQueries>();
        services.AddScoped<IRetailerManagement, RetailerManagement>();
        services.AddScoped<IRetailerDiscovery, RetailerDiscovery>();
        services.AddScoped<IObservationSubmissions, ObservationSubmissions>();
        services.AddScoped<ICatalogueSubmissions, CatalogueSubmissions>();
        services.AddSingleton<ICatalogueSubmissionImageStorage, CatalogueSubmissionImageStorage>();
        services.AddScoped<ICatalogueRetailQueries, CatalogueRetailQueries>();
        services.AddSingleton<IAffiliateLinkResolver, AwinAffiliateLinkResolver>();
        services.AddScoped<ICurrentExplorer, CurrentExplorer>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IEditorialAuthorisation, EditorialAuthorisation>();
        services.AddScoped<IPrivilegedRoleAssignments, PrivilegedRoleAssignments>();
        services.AddScoped<ICanonicalCatalogue, CanonicalCatalogue>();
        services.AddScoped<ICanonicalCatalogueQueries, CanonicalCatalogueQueries>();

        return services;
    }
}

public sealed class CurrentExplorer(DiaperScoutDbContext db, IHttpContextAccessor httpContextAccessor) : ICurrentExplorer
{
    public async Task<ExplorerIdentity?> GetAsync(CancellationToken cancellationToken = default)
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true) return null;
        var subject = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject)) return null;
        return await (from user in db.Users.AsNoTracking()
                      join explorer in db.ExplorerProfiles.AsNoTracking() on user.Id equals explorer.UserId
                      where user.Subject == subject && user.Status == UserAccountStatus.Active
                      select new ExplorerIdentity(user.Id, explorer.Id, user.Subject, explorer.DisplayName))
            .SingleOrDefaultAsync(cancellationToken);
    }
}


internal sealed class RetailerDiscovery(DiaperScoutDbContext db, IRetailerDiscoveryProvider provider) : IRetailerDiscovery
{
    public async Task<IReadOnlyList<RetailerProductListingItem>> DiscoverAndRecordAsync(
        string gtin,
        CancellationToken cancellationToken = default)
    {
        var candidates = await provider.DiscoverAsync(gtin, cancellationToken);
        var results = new List<RetailerProductListingItem>(candidates.Count);

        foreach (var candidate in candidates)
        {
            var slug = CreateRetailerSlug(candidate.RetailerName);
            results.Add(await RecordAsync(
                new RetailerDiscoveryResult(
                    candidate.Gtin,
                    candidate.RetailerName,
                    slug,
                    candidate.RetailerWebsiteUrl,
                    candidate.ListingUrl,
                    "DataForSEO.GoogleShopping",
                    candidate.SourceUrl,
                    candidate.ExternalListingId),
                cancellationToken));
        }

        return results;
    }

    public async Task<RetailerProductListingItem> RecordAsync(
        RetailerDiscoveryResult result,
        CancellationToken cancellationToken = default)
    {
        var gtin = result.Gtin?.Trim();
        if (string.IsNullOrWhiteSpace(gtin))
            throw new CatalogueValidationException("gtin", "A GTIN is required.");

        var pack = await (from identifier in db.ProductIdentifiers
                          join packType in db.PackTypes on identifier.PackTypeId equals packType.Id
                          join size in db.SizeVariants on packType.SizeVariantId equals size.Id
                          join variant in db.ProductVariants on size.ProductVariantId equals variant.Id
                          join product in db.Products on variant.ProductId equals product.Id
                          where identifier.Type == IdentifierType.Gtin && identifier.Value == gtin
                          select new { PackType = packType, ProductStatus = product.Status })
            .SingleOrDefaultAsync(cancellationToken);

        if (pack is null)
            throw new KeyNotFoundException();

        if (pack.ProductStatus != ProductStatus.Current)
            throw new CatalogueValidationException("gtin", "Retailer discovery is only available for current catalogue products.");

        var retailer = await FindRetailerAsync(result, cancellationToken);
        if (retailer is null)
        {
            var name = result.RetailerName?.Trim();
            var slug = result.RetailerSlug?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(name))
                throw new CatalogueValidationException("retailerName", "A retailer name is required.");
            if (string.IsNullOrWhiteSpace(slug))
                throw new CatalogueValidationException("retailerSlug", "A retailer slug is required.");

            try
            {
                retailer = new Retailer(name, slug, result.RetailerWebsiteUrl);
            }
            catch (ArgumentException exception)
            {
                throw new CatalogueValidationException(exception.ParamName ?? "retailer", exception.Message);
            }

            db.Retailers.Add(retailer);
        }

        RetailerProductListing listing;
        var listingUrl = NormalizeUrlForComparison(result.ListingUrl);
        if (listingUrl is null)
            throw new CatalogueValidationException("listingUrl", "A valid HTTP or HTTPS listing URL is required.");

        listing = await db.RetailerProductListings
            .SingleOrDefaultAsync(
                value => value.PackTypeId == pack.PackType.Id &&
                         value.RetailerId == retailer.Id &&
                         value.ListingUrl == listingUrl,
                cancellationToken);

        try
        {
            if (listing is null)
            {
                listing = new RetailerProductListing(
                    pack.PackType.Id,
                    retailer.Id,
                    listingUrl,
                    result.DiscoveryProvider,
                    result.SourceUrl,
                    result.ExternalListingId);
                db.RetailerProductListings.Add(listing);
            }
            else
            {
                listing.UpdateDiscovery(
                    listingUrl,
                    result.DiscoveryProvider,
                    result.SourceUrl,
                    result.ExternalListingId);
            }
        }
        catch (ArgumentException exception)
        {
            throw new CatalogueValidationException(exception.ParamName ?? "discovery", exception.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToListingItem(listing, retailer);
    }

    public async Task<IReadOnlyList<RetailerProductListingItem>> GetForGtinAsync(
        string gtin,
        CancellationToken cancellationToken = default)
    {
        var normalizedGtin = gtin?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedGtin))
            throw new CatalogueValidationException("gtin", "A GTIN is required.");

        var packId = await db.ProductIdentifiers
            .Where(identifier => identifier.Type == IdentifierType.Gtin && identifier.Value == normalizedGtin)
            .Select(identifier => (Guid?)identifier.PackTypeId)
            .SingleOrDefaultAsync(cancellationToken);

        if (!packId.HasValue)
            throw new KeyNotFoundException();

        return await (from listing in db.RetailerProductListings.AsNoTracking()
                      join retailer in db.Retailers.AsNoTracking() on listing.RetailerId equals retailer.Id
                      where listing.PackTypeId == packId.Value
                      orderby retailer.Name, listing.ListingUrl
                      select new RetailerProductListingItem(
                          listing.Id,
                          listing.PackTypeId,
                          listing.RetailerId,
                          retailer.Name,
                          retailer.Status,
                          listing.ListingUrl,
                          listing.DiscoveryProvider,
                          listing.SourceUrl,
                          listing.ExternalListingId,
                          listing.Status,
                          listing.DiscoveredAtUtc,
                          listing.LastCheckedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private static string CreateRetailerSlug(string retailerName)
    {
        var slug = new string(retailerName.Trim().ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());

        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);

        return slug.Trim('-');
    }

    private async Task<Retailer?> FindRetailerAsync(
        RetailerDiscoveryResult result,
        CancellationToken cancellationToken)
    {
        var websiteUrl = NormalizeUrlForComparison(result.RetailerWebsiteUrl);
        if (websiteUrl is not null)
        {
            var retailer = await db.Retailers
                .SingleOrDefaultAsync(value => value.WebsiteUrl == websiteUrl, cancellationToken);
            if (retailer is not null)
                return retailer;
        }

        if (!string.IsNullOrWhiteSpace(result.RetailerSlug))
            return await db.Retailers.SingleOrDefaultAsync(
                value => value.Slug == result.RetailerSlug.Trim().ToLowerInvariant(),
                cancellationToken);

        return null;
    }

    private static string? NormalizeUrlForComparison(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return null;

        return uri.ToString();
    }

    private static RetailerProductListingItem ToListingItem(
        RetailerProductListing listing,
        Retailer retailer) =>
        new(
            listing.Id,
            listing.PackTypeId,
            listing.RetailerId,
            retailer.Name,
            retailer.Status,
            listing.ListingUrl,
            listing.DiscoveryProvider,
            listing.SourceUrl,
            listing.ExternalListingId,
            listing.Status,
            listing.DiscoveredAtUtc,
            listing.LastCheckedAtUtc);

}

internal sealed class RetailerManagement(DiaperScoutDbContext db, IEditorialAuthorisation editorialAuthorisation) : IRetailerManagement
{
    public async Task<IReadOnlyList<RetailerManagementItem>> GetAsync(
        AuthenticatedUser actor,
        string? query,
        RetailerStatus? status,
        CancellationToken cancellationToken = default)
    {
        await RequireManagementAsync(actor, cancellationToken);

        var retailers = db.Retailers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim()}%";
            retailers = retailers.Where(retailer =>
                EF.Functions.ILike(retailer.Name, pattern) ||
                EF.Functions.ILike(retailer.Slug, pattern) ||
                (retailer.WebsiteUrl != null && EF.Functions.ILike(retailer.WebsiteUrl, pattern)));
        }

        if (status.HasValue)
            retailers = retailers.Where(retailer => retailer.Status == status.Value);

        return await retailers
            .OrderBy(retailer => retailer.Name)
            .Select(retailer => new RetailerManagementItem(
                retailer.Id,
                retailer.Name,
                retailer.Slug,
                retailer.WebsiteUrl,
                retailer.Status,
                retailer.CreatedAtUtc,
                retailer.UpdatedAtUtc,
                retailer.IdentityVerifiedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<RetailerManagementItem> CreateAsync(
        AuthenticatedUser actor,
        CreateRetailerManagement command,
        CancellationToken cancellationToken = default)
    {
        await RequireManagementAsync(actor, cancellationToken);

        var name = command.Name?.Trim();
        var slug = command.Slug?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(name))
            throw new CatalogueValidationException("name", "A retailer name is required.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new CatalogueValidationException("slug", "A retailer slug is required.");

        if (await db.Retailers.AnyAsync(retailer => retailer.Slug == slug, cancellationToken))
            throw new CatalogueValidationException("slug", "A retailer with this slug already exists.");

        Retailer retailer;
        try
        {
            retailer = new Retailer(name, slug, command.WebsiteUrl);
        }
        catch (ArgumentException exception)
        {
            throw new CatalogueValidationException(
                exception.ParamName ?? "retailer",
                exception.Message);
        }

        db.Retailers.Add(retailer);
        await db.SaveChangesAsync(cancellationToken);

        return ToItem(retailer);
    }

    public async Task<RetailerManagementItem> UpdateIdentityAsync(
        AuthenticatedUser actor,
        Guid retailerId,
        UpdateRetailerIdentity command,
        CancellationToken cancellationToken = default)
    {
        await RequireManagementAsync(actor, cancellationToken);

        var retailer = await db.Retailers.SingleOrDefaultAsync(value => value.Id == retailerId, cancellationToken)
            ?? throw new KeyNotFoundException();

        var name = command.Name?.Trim();
        var slug = command.Slug?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(name))
            throw new CatalogueValidationException("name", "A retailer name is required.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new CatalogueValidationException("slug", "A retailer slug is required.");

        if (await db.Retailers.AnyAsync(value => value.Id != retailerId && value.Slug == slug, cancellationToken))
            throw new CatalogueValidationException("slug", "A retailer with this slug already exists.");

        try
        {
            retailer.UpdateIdentity(name, slug, command.WebsiteUrl);
        }
        catch (ArgumentException exception)
        {
            throw new CatalogueValidationException(
                exception.ParamName ?? "retailer",
                exception.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToItem(retailer);
    }

    public async Task<RetailerIdentityVerificationItem> VerifyIdentityAsync(
        AuthenticatedUser actor,
        Guid retailerId,
        RetailerIdentityVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        await RequireManagementAsync(actor, cancellationToken);

        var retailer = await db.Retailers.SingleOrDefaultAsync(value => value.Id == retailerId, cancellationToken)
            ?? throw new KeyNotFoundException();

        var checks = BuildChecks(retailer, request);
        if (request.Outcome == RetailerIdentityVerificationOutcome.Verified && !checks.ReadyToVerify)
            throw new CatalogueValidationException("identity", "The automated identity checks are not all satisfied. Review the evidence or correct the retailer identity first.");

        if (request.Outcome == RetailerIdentityVerificationOutcome.Verified)
            retailer.VerifyIdentity(request.SourceUrl);
        else
            retailer.MarkNeedsReview();

        var verification = new RetailerIdentityVerification(
            retailer.Id,
            actor.UserId,
            request.Outcome,
            request.ObservedRetailerName,
            request.SourceUrl,
            request.ListingUrl,
            checks.WebsiteUrlValid,
            checks.SourceUrlValid,
            checks.ListingUrlValid,
            checks.NameMatches,
            checks.DomainMatches,
            request.Notes);

        db.RetailerIdentityVerifications.Add(verification);
        await db.SaveChangesAsync(cancellationToken);

        return ToVerificationItem(verification);
    }

    public async Task<IReadOnlyList<RetailerIdentityVerificationItem>> GetIdentityVerificationsAsync(
        AuthenticatedUser actor,
        Guid retailerId,
        CancellationToken cancellationToken = default)
    {
        await RequireManagementAsync(actor, cancellationToken);

        var exists = await db.Retailers.AnyAsync(value => value.Id == retailerId, cancellationToken);
        if (!exists)
            throw new KeyNotFoundException();

        return await db.RetailerIdentityVerifications
            .AsNoTracking()
            .Where(value => value.RetailerId == retailerId)
            .OrderByDescending(value => value.VerifiedAtUtc)
            .Select(value => new RetailerIdentityVerificationItem(
                value.Id,
                value.RetailerId,
                value.ObservedRetailerName,
                value.SourceUrl,
                value.ListingUrl,
                value.Outcome,
                new RetailerIdentityCheckResults(
                    value.WebsiteUrlValid,
                    value.SourceUrlValid,
                    value.ListingUrlValid,
                    value.NameMatches,
                    value.DomainMatches),
                value.Notes,
                value.VerifiedByUserId,
                value.VerifiedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RetailerAffiliateProgrammeItem>> GetAffiliateProgrammesAsync(
        AuthenticatedUser actor,
        Guid retailerId,
        CancellationToken cancellationToken = default)
    {
        await RequireManagementAsync(actor, cancellationToken);

        var exists = await db.Retailers.AnyAsync(value => value.Id == retailerId, cancellationToken);
        if (!exists)
            throw new KeyNotFoundException();

        return await db.RetailerAffiliateProgrammes
            .AsNoTracking()
            .Where(value => value.RetailerId == retailerId)
            .OrderByDescending(value => value.IsPreferred)
            .ThenByDescending(value => value.LastCheckedAtUtc)
            .Select(ToAffiliateProgrammeItemExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<RetailerAffiliateProgrammeItem> RecordAffiliateProgrammeDiscoveryAsync(
        AuthenticatedUser actor,
        Guid retailerId,
        RetailerAffiliateProgrammeDiscoveryRequest request,
        CancellationToken cancellationToken = default)
    {
        await RequireManagementAsync(actor, cancellationToken);

        var retailer = await db.Retailers.SingleOrDefaultAsync(value => value.Id == retailerId, cancellationToken)
            ?? throw new KeyNotFoundException();

        if (retailer.Status != RetailerStatus.Verified)
            throw new CatalogueValidationException("retailer", "The retailer must be verified before affiliate programme discovery can be recorded.");

        RetailerAffiliateProgramme programme;
        try
        {
            programme = await db.RetailerAffiliateProgrammes
                .SingleOrDefaultAsync(
                    value => value.RetailerId == retailerId
                        && value.Network == request.Network.Trim()
                        && value.ProgrammeId == request.ProgrammeId.Trim(),
                    cancellationToken);

            if (programme is null)
            {
                programme = new RetailerAffiliateProgramme(
                    retailerId,
                    request.Network,
                    request.ProgrammeId,
                    request.ProgrammeName,
                    request.Status,
                    request.ProgrammeUrl,
                    request.TermsUrl,
                    request.ReferralTerms,
                    request.CookieDurationDays,
                    request.DeepLinksAllowed,
                    request.ApplicationRequired,
                    request.SourceUrl);
                db.RetailerAffiliateProgrammes.Add(programme);
            }
            else
            {
                programme.UpdateDiscovery(
                    request.ProgrammeName,
                    request.Status,
                    request.ProgrammeUrl,
                    request.TermsUrl,
                    request.ReferralTerms,
                    request.CookieDurationDays,
                    request.DeepLinksAllowed,
                    request.ApplicationRequired,
                    request.SourceUrl);
            }
        }
        catch (ArgumentException exception)
        {
            throw new CatalogueValidationException(
                exception.ParamName ?? "affiliate",
                exception.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToAffiliateProgrammeItem(programme);
    }

    public async Task<RetailerAffiliateProgrammeItem> UpdateAffiliateProgrammeStatusAsync(
        AuthenticatedUser actor,
        Guid retailerId,
        Guid programmeId,
        RetailerAffiliateProgrammeStatusUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        await RequireManagementAsync(actor, cancellationToken);

        var retailer = await db.Retailers.SingleOrDefaultAsync(value => value.Id == retailerId, cancellationToken)
            ?? throw new KeyNotFoundException();

        if (retailer.Status != RetailerStatus.Verified)
            throw new CatalogueValidationException("retailer", "The retailer must be verified before an affiliate programme can be managed.");

        var programme = await db.RetailerAffiliateProgrammes
            .SingleOrDefaultAsync(
                value => value.Id == programmeId && value.RetailerId == retailerId,
                cancellationToken)
            ?? throw new KeyNotFoundException();

        try
        {
            programme.SetManagementStatus(request.Status);
        }
        catch (ArgumentException exception)
        {
            throw new CatalogueValidationException(
                exception.ParamName ?? "status",
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            throw new CatalogueValidationException("status", exception.Message);
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToAffiliateProgrammeItem(programme);
    }

    public async Task<RetailerAffiliateProgrammeItem> SelectAffiliateProgrammeAsync(
        AuthenticatedUser actor,
        Guid retailerId,
        Guid programmeId,
        CancellationToken cancellationToken = default)
    {
        await RequireManagementAsync(actor, cancellationToken);

        var retailer = await db.Retailers.SingleOrDefaultAsync(value => value.Id == retailerId, cancellationToken)
            ?? throw new KeyNotFoundException();

        if (retailer.Status != RetailerStatus.Verified)
            throw new CatalogueValidationException("retailer", "The retailer must be verified before an affiliate programme can be selected.");

        var programmes = await db.RetailerAffiliateProgrammes
            .Where(value => value.RetailerId == retailerId)
            .ToListAsync(cancellationToken);

        var selected = programmes.SingleOrDefault(value => value.Id == programmeId)
            ?? throw new KeyNotFoundException();

        foreach (var programme in programmes)
        {
            if (programme.Id == selected.Id)
                programme.MarkPreferred();
            else
                programme.ClearPreferred();
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToAffiliateProgrammeItem(selected);
    }

    private static Expression<Func<RetailerAffiliateProgramme, RetailerAffiliateProgrammeItem>> ToAffiliateProgrammeItemExpression() =>
        value => new RetailerAffiliateProgrammeItem(
            value.Id,
            value.RetailerId,
            value.Network,
            value.ProgrammeId,
            value.ProgrammeName,
            value.Status,
            value.ProgrammeUrl,
            value.TermsUrl,
            value.ReferralTerms,
            value.CookieDurationDays,
            value.DeepLinksAllowed,
            value.ApplicationRequired,
            value.SourceUrl,
            value.DiscoveredAtUtc,
            value.LastCheckedAtUtc,
            value.IsPreferred,
            value.PreferredAtUtc);

    private static RetailerAffiliateProgrammeItem ToAffiliateProgrammeItem(RetailerAffiliateProgramme value) =>
        new(
            value.Id,
            value.RetailerId,
            value.Network,
            value.ProgrammeId,
            value.ProgrammeName,
            value.Status,
            value.ProgrammeUrl,
            value.TermsUrl,
            value.ReferralTerms,
            value.CookieDurationDays,
            value.DeepLinksAllowed,
            value.ApplicationRequired,
            value.SourceUrl,
            value.DiscoveredAtUtc,
            value.LastCheckedAtUtc,
            value.IsPreferred,
            value.PreferredAtUtc);

    private static RetailerIdentityCheckResults BuildChecks(
        Retailer retailer,
        RetailerIdentityVerificationRequest request)
    {
        var websiteUrlValid = IsHttpUrl(retailer.WebsiteUrl, out var website);
        var sourceUrlValid = IsHttpUrl(request.SourceUrl, out _);
        var listingUrlValid = IsHttpUrl(request.ListingUrl, out var listing);
        var nameMatches = NormaliseName(retailer.Name) == NormaliseName(request.ObservedRetailerName);
        var domainMatches = websiteUrlValid && listingUrlValid && SameDomain(website!, listing!);

        return new RetailerIdentityCheckResults(
            websiteUrlValid,
            sourceUrlValid,
            listingUrlValid,
            nameMatches,
            domainMatches);
    }

    private static bool IsHttpUrl(string? value, out Uri? uri)
    {
        uri = null;
        return !string.IsNullOrWhiteSpace(value)
            && Uri.TryCreate(value.Trim(), UriKind.Absolute, out uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool SameDomain(Uri first, Uri second)
    {
        var firstHost = first.Host.Trim().TrimEnd('.').ToLowerInvariant();
        var secondHost = second.Host.Trim().TrimEnd('.').ToLowerInvariant();
        firstHost = firstHost.StartsWith("www.", StringComparison.Ordinal) ? firstHost[4..] : firstHost;
        secondHost = secondHost.StartsWith("www.", StringComparison.Ordinal) ? secondHost[4..] : secondHost;
        return firstHost == secondHost || secondHost.EndsWith("." + firstHost, StringComparison.Ordinal);
    }

    private static string NormaliseName(string value) =>
        new string(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static RetailerIdentityVerificationItem ToVerificationItem(RetailerIdentityVerification verification) =>
        new(
            verification.Id,
            verification.RetailerId,
            verification.ObservedRetailerName,
            verification.SourceUrl,
            verification.ListingUrl,
            verification.Outcome,
            new RetailerIdentityCheckResults(
                verification.WebsiteUrlValid,
                verification.SourceUrlValid,
                verification.ListingUrlValid,
                verification.NameMatches,
                verification.DomainMatches),
            verification.Notes,
            verification.VerifiedByUserId,
            verification.VerifiedAtUtc);

    private async Task RequireManagementAsync(
        AuthenticatedUser actor,
        CancellationToken cancellationToken)
    {
        if (!await editorialAuthorisation.CanManageCatalogueAsync(actor, cancellationToken))
            throw new UnauthorizedAccessException();
    }

    private static RetailerManagementItem ToItem(Retailer retailer) =>
        new(
            retailer.Id,
            retailer.Name,
            retailer.Slug,
            retailer.WebsiteUrl,
            retailer.Status,
            retailer.CreatedAtUtc,
            retailer.UpdatedAtUtc,
            retailer.IdentityVerifiedAtUtc);
}

internal sealed class AtlasQueries(DiaperScoutDbContext db, ICatalogueSubmissionImageStorage imageStorage, IEditorialAuthorisation editorialAuthorisation, IAffiliateLinkResolver affiliateLinkResolver) : IAtlasQueries
{
    public async Task<ProductSummary?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await db.Products.AsNoTracking()
            .Where(p => p.Slug == slug)
            .Select(p => new ProductSummary(p.Id, p.Name, p.Slug, p.ProductType, p.Status))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ProductIdentification?> GetProductByGtinAsync(string gtin, CancellationToken cancellationToken = default) =>
        await (from identifier in db.ProductIdentifiers.AsNoTracking()
               join pack in db.PackTypes on identifier.PackTypeId equals pack.Id
               join size in db.SizeVariants on pack.SizeVariantId equals size.Id
               join variant in db.ProductVariants on size.ProductVariantId equals variant.Id
               join product in db.Products on variant.ProductId equals product.Id
               where identifier.Type == IdentifierType.Gtin && identifier.Value == gtin
               select new ProductIdentification(new ProductSummary(product.Id, product.Name, product.Slug, product.ProductType, product.Status), variant.Id, variant.Name, size.Id, size.ManufacturerSize, pack.Id, pack.QuantityPerPack, pack.PackagingType, identifier.Value))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<CatalogueProductListItem>> SearchProductsAsync(string? query, int limit, CancellationToken cancellationToken = default) =>
        (await SearchCatalogueAsync(query, new CatalogueProductFilters([], [], [], [], [], []), "relevance", limit, 0, cancellationToken)).Products;

    public Task<CatalogueProductSearch> SearchCatalogueAsync(
        string? query,
        CatalogueProductFilters filters,
        string sort,
        int limit,
        int offset = 0,
        CancellationToken cancellationToken = default) =>
        SearchCatalogueCoreAsync(query, filters, [ProductStatus.Current], sort, limit, offset, cancellationToken);

    public Task<CatalogueProductSearch> SearchCatalogueManagementAsync(
        string? query,
        CatalogueProductManagementFilters filters,
        string sort,
        int limit,
        int offset = 0,
        CancellationToken cancellationToken = default) =>
        SearchCatalogueCoreAsync(
            query,
            new CatalogueProductFilters(filters.ManufacturerIds, [], filters.ProductTypes, [], [], []),
            filters.Statuses.Count == 0 ? Enum.GetValues<ProductStatus>() : filters.Statuses,
            sort,
            limit,
            offset,
            cancellationToken);

    private async Task<CatalogueProductSearch> SearchCatalogueCoreAsync(
        string? query,
        CatalogueProductFilters filters,
        IReadOnlyList<ProductStatus> statuses,
        string sort,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        limit = Math.Clamp(limit, 1, 50);
        offset = Math.Max(offset, 0);
        var baseQuery = db.Products.AsNoTracking().Where(p => statuses.Contains(p.Status));

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim()}%";
            baseQuery = baseQuery.Where(product =>
                EF.Functions.ILike(product.Name, pattern) ||
                EF.Functions.ILike(product.Slug, pattern) ||
                db.Manufacturers.Any(manufacturer => manufacturer.Id == product.ManufacturerId && EF.Functions.ILike(manufacturer.Name, pattern)) ||
                (product.BrandId.HasValue && db.Brands.Any(brand => brand.Id == product.BrandId.Value && EF.Functions.ILike(brand.Name, pattern))));
        }

        var filteredQuery = ApplyFilters(baseQuery, filters);
        var totalCount = await filteredQuery.CountAsync(cancellationToken);

        var orderedQuery = sort.Trim().ToLowerInvariant() switch
        {
            "manufacturer" => filteredQuery.OrderBy(p => p.ManufacturerId).ThenBy(p => p.Name),
            "newest" => filteredQuery.OrderByDescending(p => p.Id),
            _ => filteredQuery.OrderBy(p => p.Name)
        };

        var products = await (from product in orderedQuery.Skip(offset).Take(limit)
                              join manufacturer in db.Manufacturers.AsNoTracking() on product.ManufacturerId equals manufacturer.Id
                              join brand in db.Brands.AsNoTracking() on product.BrandId equals brand.Id into brandJoin
                              from brand in brandJoin.DefaultIfEmpty()
                              select new { product.Id, product.Name, product.Slug, product.ProductType, product.Status, ManufacturerName = manufacturer.Name, BrandName = brand == null ? null : brand.Name })
            .ToListAsync(cancellationToken);

        var productIds = products.Select(p => p.Id).ToArray();
        var variants = await db.ProductVariants.AsNoTracking()
            .Where(v => productIds.Contains(v.ProductId))
            .Select(v => new { v.ProductId, v.BackingType, Sizes = v.Sizes.Select(s => new { s.ManufacturerSize, PackagingTypes = s.PackTypes.Select(p => p.PackagingType) }) })
            .ToListAsync(cancellationToken);

        var images = await db.CatalogueSubmissionImages.AsNoTracking()
            .Where(image => image.ProductId.HasValue && productIds.Contains(image.ProductId.Value) && image.Visibility == CatalogueContentVisibility.Public)
            .OrderBy(image => image.Role)
            .ThenBy(image => image.CreatedAtUtc)
            .Select(image => new { ProductId = image.ProductId!.Value, ImageId = image.Id })
            .ToListAsync(cancellationToken);

        var retailDestinations = await (from submission in db.CatalogueSubmissions.AsNoTracking()
                                        join destination in db.CatalogueSubmissionRetailDestinations.AsNoTracking() on submission.Id equals destination.SubmissionId
                                        join retailer in db.Retailers.AsNoTracking() on destination.RetailerId equals retailer.Id
                                        where submission.Status == CatalogueSubmissionStatus.Published
                                              && submission.PublishedProductId != null
                                              && productIds.Contains(submission.PublishedProductId.Value)
                                        select new { ProductId = submission.PublishedProductId!.Value, destination.Id, RetailerName = retailer.Name, destination.ListingUrl })
            .ToListAsync(cancellationToken);

        var productResults = products.Select(product =>
        {
            var productVariants = variants.Where(v => v.ProductId == product.Id).ToList();
            var image = images.FirstOrDefault(value => value.ProductId == product.Id);
            return new CatalogueProductListItem(
                product.Id,
                product.Name,
                product.Slug,
                product.ProductType,
                product.Status,
                product.ManufacturerName,
                product.BrandName,
                productVariants.Count,
                image is null ? null : $"/api/v1/products/{product.Id}/images/{image.ImageId}",
                productVariants.SelectMany(v => v.Sizes).Select(s => s.ManufacturerSize).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToArray(),
                productVariants.Select(v => v.BackingType).Where(v => v != BackingType.Unknown).Distinct().OrderBy(v => v).ToArray(),
                productVariants.SelectMany(v => v.Sizes).SelectMany(s => s.PackagingTypes).Distinct().OrderBy(v => v).ToArray(),
                retailDestinations.Where(d => d.ProductId == product.Id).Select(d => new CatalogueRetailDestination(d.Id, d.RetailerName, d.ListingUrl)).ToArray());
        }).ToArray();

        var facets = new List<CatalogueFacet>
        {
            await BuildManufacturerFacetAsync(baseQuery, filters, cancellationToken),
            await BuildBrandFacetAsync(baseQuery, filters, cancellationToken),
            await BuildProductTypeFacetAsync(baseQuery, filters, cancellationToken),
            await BuildSizeFacetAsync(baseQuery, filters, cancellationToken),
            await BuildBackingFacetAsync(baseQuery, filters, cancellationToken),
            await BuildPackagingFacetAsync(baseQuery, filters, cancellationToken)
        };

        return new CatalogueProductSearch(productResults, totalCount, facets.Where(f => f.Options.Count > 0).ToArray());
    }

    private static IQueryable<Product> ApplyFilters(IQueryable<Product> query, CatalogueProductFilters filters, string? excludedFacet = null)
    {
        if (excludedFacet != "manufacturer" && filters.ManufacturerIds.Count > 0)
            query = query.Where(p => filters.ManufacturerIds.Contains(p.ManufacturerId));

        if (excludedFacet != "brand" && filters.BrandIds.Count > 0)
            query = query.Where(p => p.BrandId.HasValue && filters.BrandIds.Contains(p.BrandId.Value));

        if (excludedFacet != "productType" && filters.ProductTypes.Count > 0)
            query = query.Where(p => filters.ProductTypes.Contains(p.ProductType));

        if (excludedFacet != "size" && filters.Sizes.Count > 0)
            query = query.Where(p => p.Variants.Any(v => v.Sizes.Any(s => filters.Sizes.Contains(s.ManufacturerSize))));

        if (excludedFacet != "backing" && filters.Backings.Count > 0)
            query = query.Where(p => p.Variants.Any(v => filters.Backings.Contains(v.BackingType)));

        if (excludedFacet != "packaging" && filters.PackagingTypes.Count > 0)
            query = query.Where(p => p.Variants.Any(v => v.Sizes.Any(s => s.PackTypes.Any(pack => filters.PackagingTypes.Contains(pack.PackagingType)))));

        return query;
    }

    private async Task<CatalogueFacet> BuildManufacturerFacetAsync(IQueryable<Product> baseQuery, CatalogueProductFilters filters, CancellationToken cancellationToken)
    {
        var rows = await (from product in ApplyFilters(baseQuery, filters, "manufacturer")
                          join manufacturer in db.Manufacturers.AsNoTracking() on product.ManufacturerId equals manufacturer.Id
                          group product by new { manufacturer.Id, manufacturer.Name } into groupValue
                          orderby groupValue.Key.Name
                          select new CatalogueFacetOption(groupValue.Key.Id.ToString(), groupValue.Key.Name, groupValue.Count()))
            .ToListAsync(cancellationToken);
        return new CatalogueFacet("manufacturer", "Manufacturer", rows);
    }

    private async Task<CatalogueFacet> BuildBrandFacetAsync(IQueryable<Product> baseQuery, CatalogueProductFilters filters, CancellationToken cancellationToken)
    {
        var rows = await (from product in ApplyFilters(baseQuery, filters, "brand")
                          where product.BrandId.HasValue
                          join brand in db.Brands.AsNoTracking() on product.BrandId equals brand.Id
                          group product by new { brand.Id, brand.Name } into groupValue
                          orderby groupValue.Key.Name
                          select new CatalogueFacetOption(groupValue.Key.Id.ToString(), groupValue.Key.Name, groupValue.Count()))
            .ToListAsync(cancellationToken);
        return new CatalogueFacet("brand", "Brand", rows);
    }

    private async Task<CatalogueFacet> BuildProductTypeFacetAsync(
    IQueryable<Product> baseQuery,
    CatalogueProductFilters filters,
    CancellationToken cancellationToken)
{
    var rows = await ApplyFilters(baseQuery, filters, "productType")
        .GroupBy(p => p.ProductType)
        .Select(groupValue => new
        {
            Value = groupValue.Key,
            Count = groupValue.Count()
        })
        .OrderBy(value => value.Value)
        .ToListAsync(cancellationToken);

    var options = rows
        .Select(value => new CatalogueFacetOption(
            value.Value.ToString(),
            value.Value.ToString(),
            value.Count))
        .ToList();

    return new CatalogueFacet("productType", "Product type", options);
}

    private async Task<CatalogueFacet> BuildSizeFacetAsync(IQueryable<Product> baseQuery, CatalogueProductFilters filters, CancellationToken cancellationToken)
    {
        var rows = await (from product in ApplyFilters(baseQuery, filters, "size")
                          from variant in product.Variants
                          from size in variant.Sizes
                          group product by size.ManufacturerSize into groupValue
                          orderby groupValue.Key
                          select new CatalogueFacetOption(groupValue.Key, groupValue.Key, groupValue.Select(p => p.Id).Distinct().Count()))
            .ToListAsync(cancellationToken);
        return new CatalogueFacet("size", "Size", rows);
    }

    private async Task<CatalogueFacet> BuildBackingFacetAsync(IQueryable<Product> baseQuery, CatalogueProductFilters filters, CancellationToken cancellationToken)
    {
        var rows = await (from product in ApplyFilters(baseQuery, filters, "backing")
                          from variant in product.Variants
                          where variant.BackingType != BackingType.Unknown
                          group product by variant.BackingType into groupValue
                          orderby groupValue.Key
                          select new CatalogueFacetOption(groupValue.Key.ToString(), groupValue.Key.ToString(), groupValue.Select(p => p.Id).Distinct().Count()))
            .ToListAsync(cancellationToken);
        return new CatalogueFacet("backing", "Backing", rows);
    }

    private async Task<CatalogueFacet> BuildPackagingFacetAsync(IQueryable<Product> baseQuery, CatalogueProductFilters filters, CancellationToken cancellationToken)
    {
        var rows = await (from product in ApplyFilters(baseQuery, filters, "packaging")
                          from variant in product.Variants
                          from size in variant.Sizes
                          from pack in size.PackTypes
                          group product by pack.PackagingType into groupValue
                          orderby groupValue.Key
                          select new CatalogueFacetOption(groupValue.Key.ToString(), groupValue.Key.ToString(), groupValue.Select(p => p.Id).Distinct().Count()))
            .ToListAsync(cancellationToken);
        return new CatalogueFacet("packaging", "Packaging", rows);
    }

    public async Task<CatalogueProductDetails?> GetProductDetailsBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => (await GetProductDetailsCoreAsync(slug, includeModeratorOnly: false, cancellationToken))?.PublicDetails;

    public async Task<CatalogueModeratorProductDetails?> GetProductDetailsForModeratorAsync(
        AuthenticatedUser actor,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (!await editorialAuthorisation.CanPublishAtlasAsync(actor, cancellationToken))
            throw new UnauthorizedAccessException();

        return (await GetProductDetailsCoreByIdAsync(productId, includeModeratorOnly: true, cancellationToken))?.ModeratorDetails;
    }

    public async Task<CatalogueDataQualitySummary> GetProductDataQualityAsync(
        AuthenticatedUser actor,
        CancellationToken cancellationToken = default)
    {
        if (!await editorialAuthorisation.CanManageCatalogueAsync(actor, cancellationToken))
            throw new UnauthorizedAccessException();

        var products = await (from product in db.Products.AsNoTracking()
                              join manufacturer in db.Manufacturers.AsNoTracking() on product.ManufacturerId equals manufacturer.Id
                              where product.Status == ProductStatus.Current
                              orderby product.Name
                              select new
                              {
                                  product.Id,
                                  product.Name,
                                  ManufacturerName = manufacturer.Name
                              })
            .ToListAsync(cancellationToken);

        var productIds = products.Select(value => value.Id).ToArray();
        if (productIds.Length == 0)
            return new CatalogueDataQualitySummary(0, 0, 0, 0, []);

        var variants = await db.ProductVariants.AsNoTracking()
            .Where(value => productIds.Contains(value.ProductId))
            .Select(value => new { value.Id, value.ProductId, value.Name })
            .ToListAsync(cancellationToken);

        var variantIds = variants.Select(value => value.Id).ToArray();
        var sizes = variantIds.Length == 0
            ? []
            : await db.SizeVariants.AsNoTracking()
                .Where(value => variantIds.Contains(value.ProductVariantId))
                .Select(value => new { value.Id, value.ProductVariantId, value.ManufacturerSize })
                .ToListAsync(cancellationToken);

        var sizeIds = sizes.Select(value => value.Id).ToArray();
        var packs = sizeIds.Length == 0
            ? []
            : await db.PackTypes.AsNoTracking()
                .Where(value => sizeIds.Contains(value.SizeVariantId))
                .Select(value => new { value.Id, value.SizeVariantId, value.QuantityPerPack, value.PackagingType })
                .ToListAsync(cancellationToken);

        var packIds = packs.Select(value => value.Id).ToArray();
        var gtins = packIds.Length == 0
            ? []
            : await db.ProductIdentifiers.AsNoTracking()
                .Where(value => packIds.Contains(value.PackTypeId) && value.Type == IdentifierType.Gtin)
                .Select(value => new { value.PackTypeId, value.Value })
                .ToListAsync(cancellationToken);

        var primaryImageProductIds = await db.CatalogueSubmissionImages.AsNoTracking()
            .Where(value => value.ProductId.HasValue && productIds.Contains(value.ProductId.Value) && value.IsPrimary && value.Visibility == CatalogueContentVisibility.Public)
            .Select(value => value.ProductId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var variantsByProduct = variants.GroupBy(value => value.ProductId).ToDictionary(group => group.Key, group => group.ToList());
        var sizesByVariant = sizes.GroupBy(value => value.ProductVariantId).ToDictionary(group => group.Key, group => group.ToList());
        var packsBySize = packs.GroupBy(value => value.SizeVariantId).ToDictionary(group => group.Key, group => group.ToList());
        var gtinsByPack = gtins.GroupBy(value => value.PackTypeId).ToDictionary(group => group.Key, group => group.Select(value => value.Value).ToList());
        var productLookup = products.ToDictionary(value => value.Id);

        var ruleProducts = new Dictionary<string, (string Label, string Impact, CatalogueDataQualitySeverity Severity, List<CatalogueDataQualityProduct> Products)>(StringComparer.Ordinal);

        void AddIssue(string code, string label, string impact, CatalogueDataQualitySeverity severity, Guid productId, string detail)
        {
            if (!ruleProducts.TryGetValue(code, out var rule))
            {
                rule = (label, impact, severity, []);
                ruleProducts[code] = rule;
            }

            rule.Products.Add(new CatalogueDataQualityProduct(
                productId,
                productLookup[productId].Name,
                productLookup[productId].ManufacturerName,
                detail));
        }

        var blockingProductIds = new HashSet<Guid>();
        var warningProductIds = new HashSet<Guid>();

        foreach (var product in products)
        {
            var productVariants = variantsByProduct.GetValueOrDefault(product.Id) ?? [];

            if (productVariants.Count == 0)
            {
                AddIssue("RetailMatching.NoVariant", "No product variant", "Retail matching cannot identify a size or offer without a variant.", CatalogueDataQualitySeverity.Blocking, product.Id, "Add at least one product variant.");
                blockingProductIds.Add(product.Id);
            }

            var productSizes = productVariants
                .SelectMany(variant => sizesByVariant.GetValueOrDefault(variant.Id) ?? [])
                .ToList();

            if (productSizes.Count == 0)
            {
                AddIssue("RetailMatching.NoSize", "No manufacturer sizes", "Retail matching needs a size-level identity to match offers reliably.", CatalogueDataQualitySeverity.Blocking, product.Id, "Add at least one manufacturer size.");
                blockingProductIds.Add(product.Id);
            }

            var productPacks = productSizes
                .SelectMany(size => packsBySize.GetValueOrDefault(size.Id) ?? [])
                .ToList();

            if (productPacks.Count == 0)
            {
                AddIssue("RetailMatching.NoPack", "No manufacturer pack", "Retail matching needs a manufacturer pack configuration before an offer can be matched.", CatalogueDataQualitySeverity.Blocking, product.Id, "Add at least one pack configuration.");
                blockingProductIds.Add(product.Id);
            }

            var packsWithoutGtins = productPacks
                .Where(pack => !gtinsByPack.TryGetValue(pack.Id, out var values) || values.Count == 0)
                .ToList();

            if (packsWithoutGtins.Count > 0)
            {
                var detail = string.Join(", ", packsWithoutGtins.Select(pack =>
                {
                    var size = productSizes.First(value => packsBySize[value.Id].Any(candidate => candidate.Id == pack.Id));
                    return $"{size.ManufacturerSize} / {pack.QuantityPerPack} {pack.PackagingType.ToString().ToLowerInvariant()}";
                }));
                AddIssue("RetailMatching.MissingGtin", "Missing GTIN / barcode", "GTINs are the primary matching key for automated retailer offer matching.", CatalogueDataQualitySeverity.Blocking, product.Id, $"Missing on: {detail}.");
                blockingProductIds.Add(product.Id);
            }

            var invalidGtins = productPacks
                .SelectMany(pack => gtinsByPack.GetValueOrDefault(pack.Id) ?? [], (pack, gtin) => new { pack, gtin })
                .Where(value => !IsValidGtin(value.gtin))
                .ToList();

            if (invalidGtins.Count > 0)
            {
                AddIssue("RetailMatching.InvalidGtin", "Invalid GTIN / barcode", "An invalid GTIN cannot be used safely as an automated product match key.", CatalogueDataQualitySeverity.Blocking, product.Id, $"Invalid value: {invalidGtins[0].gtin}.");
                blockingProductIds.Add(product.Id);
            }

            if (!primaryImageProductIds.Contains(product.Id))
            {
                AddIssue("Catalogue.MissingPrimaryImage", "Missing primary image", "The public catalogue has no primary product image for this product.", CatalogueDataQualitySeverity.Warning, product.Id, "Add a public primary image with appropriate rights/provenance.");
                warningProductIds.Add(product.Id);
            }
        }

        var rules = ruleProducts
            .OrderBy(value => value.Value.Severity)
            .ThenBy(value => value.Value.Label)
            .Select(value => new CatalogueDataQualityRule(
                value.Key,
                value.Value.Label,
                value.Value.Impact,
                value.Value.Severity,
                value.Value.Products.Count,
                value.Value.Products.OrderBy(product => product.ProductName).ToList()))
            .ToList();

        return new CatalogueDataQualitySummary(
            products.Count,
            products.Count - blockingProductIds.Count,
            blockingProductIds.Count,
            warningProductIds.Count,
            rules);
    }

    private static bool IsValidGtin(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length is not (8 or 12 or 13 or 14) || value.Any(character => !char.IsDigit(character)))
            return false;

        var checksum = 0;
        var multiplier = 3;
        for (var index = value.Length - 2; index >= 0; index--)
        {
            checksum += (value[index] - '0') * multiplier;
            multiplier = multiplier == 3 ? 1 : 3;
        }

        var expected = (10 - checksum % 10) % 10;
        return expected == value[^1] - '0';
    }

    public async Task<CatalogueProductManagementDetails?> GetProductManagementDetailsAsync(
        AuthenticatedUser actor,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (!await editorialAuthorisation.CanManageCatalogueAsync(actor, cancellationToken))
            throw new UnauthorizedAccessException();

        var product = await (from value in db.Products.AsNoTracking()
                             join manufacturer in db.Manufacturers.AsNoTracking() on value.ManufacturerId equals manufacturer.Id
                             join brand in db.Brands.AsNoTracking() on value.BrandId equals brand.Id into brandJoin
                             from brand in brandJoin.DefaultIfEmpty()
                             where value.Id == productId
                             select new
                             {
                                 value.Id, value.Name, value.Slug, value.ManufacturerId, value.BrandId,
                                 ManufacturerName = manufacturer.Name,
                                 BrandName = brand == null ? null : brand.Name,
                                 ProductFamily = value.Family, ProductType = value.ProductType, ProductStatus = value.Status, Description = value.Description, DescriptionVisibility = value.DescriptionVisibility, OfficialWebsiteUrl = value.OfficialWebsiteUrl
                             })
            .SingleOrDefaultAsync(cancellationToken);

        if (product is null)
            return null;

        var variants = await db.ProductVariants.AsNoTracking()
            .Where(value => value.ProductId == product.Id)
            .OrderBy(value => value.Name)
            .ToListAsync(cancellationToken);

        var variantResults = new List<CatalogueProductVariant>();
        foreach (var variant in variants)
        {
            var sizes = await db.SizeVariants.AsNoTracking()
                .Where(value => value.ProductVariantId == variant.Id)
                .OrderBy(value => value.ManufacturerSize)
                .ToListAsync(cancellationToken);

            var sizeResults = new List<CatalogueProductSize>();
            foreach (var size in sizes)
            {
                var packs = await db.PackTypes.AsNoTracking()
                    .Where(value => value.SizeVariantId == size.Id)
                    .OrderBy(value => value.QuantityPerPack)
                    .ToListAsync(cancellationToken);

                var packResults = new List<CatalogueProductPack>();
                foreach (var pack in packs)
                {
                    var gtins = await db.ProductIdentifiers.AsNoTracking()
                        .Where(value => value.PackTypeId == pack.Id && value.Type == IdentifierType.Gtin)
                        .OrderBy(value => value.Value)
                        .Select(value => value.Value)
                        .ToListAsync(cancellationToken);
                    packResults.Add(new CatalogueProductPack(pack.Id, pack.QuantityPerPack, pack.PackagingType, gtins));
                }

                sizeResults.Add(new CatalogueProductSize(size.Id, size.ManufacturerSize, size.WaistMinimumCm, size.WaistMaximumCm, size.HipMinimumCm, size.HipMaximumCm, size.ManufacturerStatedAbsorbencyMl, size.FitMeasurementBasis, size.AbsorbencyBasisMethod, size.AbsorbencySource, size.LengthMm, size.WidthMm, size.WeightGrams, packResults));
            }

            var appearance = Enum.TryParse<CatalogueVariantAppearance>(variant.PrintDesign, true, out var parsedAppearance) && Enum.IsDefined(parsedAppearance) ? parsedAppearance : CatalogueVariantAppearance.Unknown;
            var colour = Enum.TryParse<CatalogueVariantColour>(variant.PrimaryColour, true, out var parsedColour) && Enum.IsDefined(parsedColour) ? parsedColour : CatalogueVariantColour.Unknown;
            var designedFor = Enum.TryParse<CatalogueVariantDesignedFor>(variant.DesignedFor, true, out var parsedDesignedFor) && Enum.IsDefined(parsedDesignedFor) ? parsedDesignedFor : CatalogueVariantDesignedFor.Unknown;
            variantResults.Add(new CatalogueProductVariant(variant.Id, variant.Name, variant.BackingType, variant.FastenerType, appearance, colour, variant.HasWetnessIndicator, variant.HasStandingLeakGuards, variant.WaistbandStyle, variant.Fragrance, variant.IsLatexFree, designedFor, variant.FastenerCount, variant.ConstructionNotes, sizeResults));
        }

        var managementImages = await db.CatalogueSubmissionImages
            .AsNoTracking()
            .Where(value => value.ProductId == product.Id)
            .OrderByDescending(value => value.IsPrimary)
            .ThenBy(value => value.Role)
            .ThenBy(value => value.CreatedAtUtc)
            .Select(value => new CatalogueModeratorProductImage(
                value.Id,
                value.Role,
                value.IsPrimary,
                value.Visibility,
                value.SourceType,
                value.SourceUrl,
                value.SourceNotes,
                value.PermissionStatus,
                value.PermissionEvidence,
                value.OriginalFileName,
                value.FileSizeBytes,
                $"/api/v1/products/{product.Id}/moderator-images/{value.Id}"))
            .ToListAsync(cancellationToken);

        return new CatalogueProductManagementDetails(
            product.Id, product.Name, product.Slug, product.ManufacturerId, product.BrandId,
            product.ManufacturerName, product.BrandName, product.ProductFamily, product.ProductType,
            product.ProductStatus, product.Description, product.DescriptionVisibility, product.OfficialWebsiteUrl, variantResults, managementImages);
    }

    public async Task<CatalogueProductImageContent?> GetProductImageContentAsync(
        Guid productId,
        Guid imageId,
        bool moderatorOnly,
        CancellationToken cancellationToken = default)
    {
        var image = await db.CatalogueSubmissionImages
            .AsNoTracking()
            .SingleOrDefaultAsync(
                value => value.ProductId == productId &&
                         value.Id == imageId &&
                         (moderatorOnly || value.Visibility == CatalogueContentVisibility.Public),
                cancellationToken);

        if (image is null)
            return null;

        var stream = await imageStorage.OpenReadAsync(image.StorageKey, cancellationToken);
        return stream is null
            ? null
            : new CatalogueProductImageContent(stream, image.ContentType, image.OriginalFileName);
    }

    private async Task<(CatalogueProductDetails PublicDetails, CatalogueModeratorProductDetails ModeratorDetails)?> GetProductDetailsCoreByIdAsync(
        Guid productId,
        bool includeModeratorOnly,
        CancellationToken cancellationToken)
    {
        var slug = await db.Products.AsNoTracking()
            .Where(value => value.Id == productId)
            .Select(value => value.Slug)
            .SingleOrDefaultAsync(cancellationToken);

        return slug is null ? null : await GetProductDetailsCoreAsync(slug, includeModeratorOnly, cancellationToken);
    }

    private async Task<(CatalogueProductDetails PublicDetails, CatalogueModeratorProductDetails ModeratorDetails)?> GetProductDetailsCoreAsync(
        string slug,
        bool includeModeratorOnly,
        CancellationToken cancellationToken)
    {
        var product = await (from value in db.Products.AsNoTracking()
                             join manufacturer in db.Manufacturers.AsNoTracking() on value.ManufacturerId equals manufacturer.Id
                             join brand in db.Brands.AsNoTracking() on value.BrandId equals brand.Id into brandJoin
                             from brand in brandJoin.DefaultIfEmpty()
                             where value.Slug == slug
                             select new { value.Id, value.Name, value.Slug, value.ProductType, value.Status, ManufacturerName = manufacturer.Name, BrandName = brand == null ? null : brand.Name, value.Description, value.DescriptionVisibility, value.OfficialWebsiteUrl })
            .SingleOrDefaultAsync(cancellationToken);

        if (product is null)
            return null;

        var variants = await db.ProductVariants.AsNoTracking()
            .Where(v => v.ProductId == product.Id)
            .OrderBy(v => v.Name)
            .ToListAsync(cancellationToken);

        var variantResults = new List<CatalogueProductVariant>();
        foreach (var variant in variants)
        {
            var sizes = await db.SizeVariants.AsNoTracking()
                .Where(s => s.ProductVariantId == variant.Id)
                .OrderBy(s => s.ManufacturerSize)
                .ToListAsync(cancellationToken);

            var sizeResults = new List<CatalogueProductSize>();
            foreach (var size in sizes)
            {
                var packs = await db.PackTypes.AsNoTracking()
                    .Where(p => p.SizeVariantId == size.Id)
                    .OrderBy(p => p.QuantityPerPack)
                    .ToListAsync(cancellationToken);

                var packResults = new List<CatalogueProductPack>();
                foreach (var pack in packs)
                {
                    var gtins = await db.ProductIdentifiers.AsNoTracking()
                        .Where(i => i.PackTypeId == pack.Id && i.Type == IdentifierType.Gtin)
                        .OrderBy(i => i.Value)
                        .Select(i => i.Value)
                        .ToListAsync(cancellationToken);

                    packResults.Add(new CatalogueProductPack(pack.Id, pack.QuantityPerPack, pack.PackagingType, gtins));
                }

                sizeResults.Add(new CatalogueProductSize(size.Id, size.ManufacturerSize, size.WaistMinimumCm, size.WaistMaximumCm, size.HipMinimumCm, size.HipMaximumCm, size.ManufacturerStatedAbsorbencyMl, size.FitMeasurementBasis, size.AbsorbencyBasisMethod, size.AbsorbencySource, size.LengthMm, size.WidthMm, size.WeightGrams, packResults));
            }

            var appearance = Enum.TryParse<CatalogueVariantAppearance>(variant.PrintDesign, true, out var parsedAppearance) && Enum.IsDefined(parsedAppearance) ? parsedAppearance : CatalogueVariantAppearance.Unknown;
            var colour = Enum.TryParse<CatalogueVariantColour>(variant.PrimaryColour, true, out var parsedColour) && Enum.IsDefined(parsedColour) ? parsedColour : CatalogueVariantColour.Unknown;
            var designedFor = Enum.TryParse<CatalogueVariantDesignedFor>(variant.DesignedFor, true, out var parsedDesignedFor) && Enum.IsDefined(parsedDesignedFor) ? parsedDesignedFor : CatalogueVariantDesignedFor.Unknown;
            variantResults.Add(new CatalogueProductVariant(variant.Id, variant.Name, variant.BackingType, variant.FastenerType, appearance, colour, variant.HasWetnessIndicator, variant.HasStandingLeakGuards, variant.WaistbandStyle, variant.Fragrance, variant.IsLatexFree, designedFor, variant.FastenerCount, variant.ConstructionNotes, sizeResults));
        }

        var images = await db.CatalogueSubmissionImages
            .AsNoTracking()
            .Where(value => value.ProductId == product.Id && (includeModeratorOnly || value.Visibility == CatalogueContentVisibility.Public))
            .OrderBy(value => value.Role)
            .ThenBy(value => value.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var publicImages = images
            .Where(value => value.Visibility == CatalogueContentVisibility.Public)
            .Select(value => new CatalogueProductImage(
                value.Id,
                value.Role,
                value.IsPrimary,
                $"/api/v1/products/{product.Id}/images/{value.Id}"))
            .ToList();

        var moderatorImages = images
            .Select(value => new CatalogueModeratorProductImage(
                value.Id,
                value.Role,
                value.IsPrimary,
                value.Visibility,
                value.SourceType,
                value.SourceUrl,
                value.SourceNotes,
                value.PermissionStatus,
                value.PermissionEvidence,
                value.OriginalFileName,
                value.FileSizeBytes,
                $"/api/v1/products/{product.Id}/moderator-images/{value.Id}"))
            .ToList();

        var publicDescription = product.DescriptionVisibility == CatalogueContentVisibility.Public
            ? product.Description
            : null;

        var retailOfferRows = await (from listing in db.RetailerProductListings.AsNoTracking()
                                     join retailer in db.Retailers.AsNoTracking() on listing.RetailerId equals retailer.Id
                                     join pack in db.PackTypes.AsNoTracking() on listing.PackTypeId equals pack.Id
                                     join size in db.SizeVariants.AsNoTracking() on pack.SizeVariantId equals size.Id
                                     join variant in db.ProductVariants.AsNoTracking() on size.ProductVariantId equals variant.Id
                                     join identifier in db.ProductIdentifiers.AsNoTracking().Where(value => value.Type == IdentifierType.Gtin) on pack.Id equals identifier.PackTypeId into identifierJoin
                                     from identifier in identifierJoin.DefaultIfEmpty()
                                     where variant.ProductId == product.Id
                                           && product.Status == ProductStatus.Current
                                           && retailer.Status == RetailerStatus.Verified
                                           && listing.Status == RetailerProductDiscoveryStatus.Verified
                                     orderby retailer.Name, size.ManufacturerSize, pack.QuantityPerPack, listing.ListingUrl
                                     select new
                                     {
                                         ListingId = listing.Id,
                                         PackTypeId = pack.Id,
                                         ManufacturerSize = size.ManufacturerSize,
                                         QuantityPerPack = pack.QuantityPerPack,
                                         PackagingType = pack.PackagingType,
                                         Gtin = identifier == null ? null : identifier.Value,
                                         RetailerId = retailer.Id,
                                         RetailerName = retailer.Name,
                                         ListingUrl = listing.ListingUrl
                                     })
            .ToListAsync(cancellationToken);

        var retailerIds = retailOfferRows.Select(value => value.RetailerId).Distinct().ToArray();
        var preferredProgrammes = await db.RetailerAffiliateProgrammes.AsNoTracking()
            .Where(value => retailerIds.Contains(value.RetailerId)
                            && value.IsPreferred
                            && value.Status == AffiliateProgrammeStatus.Configured)
            .ToDictionaryAsync(value => value.RetailerId, cancellationToken);

        var retailOffers = retailOfferRows
            .Select(value =>
            {
                preferredProgrammes.TryGetValue(value.RetailerId, out var programme);
                var destination = affiliateLinkResolver.Resolve(value.ListingUrl, programme);
                return new CatalogueRetailOffer(
                    value.ListingId,
                    value.PackTypeId,
                    value.ManufacturerSize,
                    value.QuantityPerPack,
                    value.PackagingType,
                    value.Gtin,
                    value.RetailerName,
                    value.ListingUrl,
                    destination.Url,
                    destination.Network,
                    destination.IsAffiliateBacked);
            })
            .ToList();

        return (
            new CatalogueProductDetails(product.Id, product.Name, product.Slug, product.ProductType, product.Status, product.ManufacturerName, product.BrandName, publicDescription, product.DescriptionVisibility, product.OfficialWebsiteUrl, variantResults, publicImages, retailOffers),
            new CatalogueModeratorProductDetails(product.Id, product.Name, product.Slug, product.ProductType, product.Status, product.ManufacturerName, product.BrandName, product.Description, product.DescriptionVisibility, product.OfficialWebsiteUrl, variantResults, moderatorImages));
    }


}

internal sealed class ObservationSubmissions(DiaperScoutDbContext db) : IObservationSubmissions
{
    public async Task<ObservationReceipt> SubmitAsync(ExplorerIdentity explorer, ObservationSubmission submission, CancellationToken cancellationToken = default)
    {
        if (submission.ProductId is { } productId && !await db.Products.AnyAsync(product => product.Id == productId, cancellationToken)) throw new InvalidOperationException("Referenced product was not found.");
        if (submission.LocationId is { } locationId && !await db.Locations.AnyAsync(location => location.Id == locationId, cancellationToken)) throw new InvalidOperationException("Referenced location was not found.");
        var observation = new Observation(explorer.UserId, submission.Type, submission.ObservedAtUtc, submission.ProductId, submission.CandidateProductName, submission.LocationId, submission.Narrative);
        observation.Submit(); db.Observations.Add(observation); await db.SaveChangesAsync(cancellationToken);
        return new ObservationReceipt(observation.Id, observation.State, observation.CreatedAtUtc);
    }
}


internal sealed class CatalogueRetailQueries(DiaperScoutDbContext db) : ICatalogueRetailQueries
{
    public async Task<IReadOnlyList<CatalogueRetailerOption>> GetRetailersAsync(CancellationToken cancellationToken = default) =>
        await db.Retailers.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new CatalogueRetailerOption(r.Id, r.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CatalogueSubmissionRetailDestinationReceipt>> GetDestinationsAsync(
        Guid submissionId, CancellationToken cancellationToken = default) =>
        await (from destination in db.CatalogueSubmissionRetailDestinations.AsNoTracking()
               join retailer in db.Retailers.AsNoTracking() on destination.RetailerId equals retailer.Id
               where destination.SubmissionId == submissionId
               orderby retailer.Name, destination.AddedAtUtc
               select new CatalogueSubmissionRetailDestinationReceipt(
                   destination.Id, destination.SubmissionId, destination.RetailerId,
                   destination.ListingUrl, destination.Notes, destination.AddedAtUtc))
            .ToListAsync(cancellationToken);
}
