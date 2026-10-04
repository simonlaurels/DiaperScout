using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DiaperScout.Infrastructure;

internal sealed class BackpackAccount(DiaperScoutDbContext db, IEditorialAuthorisation editorial) : IBackpackAccount
{
    public async Task<PersonalCatalogueDrafts> DraftsAsync(AuthenticatedUser actor, CancellationToken ct = default)
    {
        if (!await editorial.CanPublishAtlasAsync(actor, ct)) return new(0, []);
        var query = db.CatalogueSubmissions.AsNoTracking().Where(s => s.SubmittedByUserId == actor.UserId &&
            (s.Status == CatalogueSubmissionStatus.Draft || s.Status == CatalogueSubmissionStatus.NeedsChanges));
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(s => s.UpdatedAtUtc).ThenBy(s => s.Id).Take(50)
            .Select(s => new PersonalCatalogueDraft(s.ProposedProductName,
                "/catalogue/add/" + s.Id + (s.Status == CatalogueSubmissionStatus.Draft ? "/2" : "/1"), s.UpdatedAtUtc)).ToListAsync(ct);
        return new(total, items);
    }
    public async Task<BackpackAccountInfo> AccountAsync(AuthenticatedUser actor, CancellationToken ct = default) => new(
        await db.ExplorerProfiles.AsNoTracking().Where(p => p.UserId == actor.UserId).Select(p => p.DisplayName).SingleOrDefaultAsync(ct),
        await db.UserEmails.AsNoTracking().Where(e => e.UserId == actor.UserId).Select(e => e.Email).SingleOrDefaultAsync(ct));

    public async Task UpdateNameAsync(AuthenticatedUser actor, UpdateExplorerName request, CancellationToken ct = default)
    {
        var name = request.DisplayName?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 100)
            throw new CatalogueValidationException("displayName", "Enter an Explorer name of 1 to 100 characters.");
        var profile = await db.ExplorerProfiles.SingleOrDefaultAsync(p => p.UserId == actor.UserId, ct)
            ?? throw new CatalogueValidationException("displayName", "This account has no Explorer profile to edit.");
        if (await db.ExplorerProfiles.AnyAsync(p => p.UserId != actor.UserId && p.DisplayName == name, ct))
            throw new CatalogueValidationException("displayName", "That Explorer name is already in use.");
        profile.Rename(name);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505" })
        { throw new CatalogueValidationException("displayName", "That Explorer name is already in use."); }
    }

    public async Task<IReadOnlyList<PersonalDiscovery>> DiscoveriesAsync(AuthenticatedUser actor, CancellationToken ct = default)
    {
        // Filter by the authenticated actor before projecting any private contribution data.
        var sightings = await (from o in db.Observations.AsNoTracking()
            join p in db.Products on o.ProductId equals p.Id
            join l in db.Locations on o.LocationId equals l.Id
            where o.AuthorUserId == actor.UserId && o.Type == ObservationType.RetailAvailability
                && (o.State == ObservationState.Submitted || o.State == ObservationState.Accepted)
            orderby o.ObservedAtUtc descending, o.Id
            select new PersonalDiscovery("Product discovery", p.Name + " at " + l.Name, "Recorded",
                o.ObservedAtUtc, p.Status == ProductStatus.Current && l.IsPublicCommercialPlace ? "/atlas?locationId=" + l.Id : null)).Take(50).ToListAsync(ct);
        var shops = await db.Locations.AsNoTracking().Where(l => l.CreatedByUserId == actor.UserId && l.IsPublicCommercialPlace && l.CreatedAtUtc != null)
            .OrderByDescending(l => l.CreatedAtUtc).ThenBy(l => l.Id).Take(50)
            .Select(l => new PersonalDiscovery("Place added", l.Name, "Added", l.CreatedAtUtc!.Value, "/atlas?locationId=" + l.Id)).ToListAsync(ct);
        var proposals = await db.CatalogueSubmissions.AsNoTracking().Where(s => s.SubmittedByUserId == actor.UserId && s.PublicContributionId != null)
            .OrderByDescending(s => s.CreatedAtUtc).ThenBy(s => s.Id).Take(50)
            .Select(s => new PersonalDiscovery("Product proposal", s.ProposedProductName, s.Status.ToString(), s.CreatedAtUtc, null)).ToListAsync(ct);
        return sightings.Concat(shops).Concat(proposals).OrderByDescending(d => d.OccurredAtUtc).Take(50).ToArray();
    }
}
