using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DiaperScout.Infrastructure;

// Used only after authentication / successful email ownership verification.
internal static class ExplorerProfileLifecycle
{
    public static async Task SetNameAsync(DiaperScoutDbContext db, Guid userId, string? displayName,
        bool overwrite, CancellationToken ct)
    {
        var name = displayName?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 100)
            throw new CatalogueValidationException("displayName", "Enter a name or nickname of 1 to 100 characters.");
        await using var ownedTransaction = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(ct) : null;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({"profile:" + userId}, 0))", ct);
        var profile = await db.ExplorerProfiles.SingleOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is null || overwrite)
        {
            if (await db.ExplorerProfiles.AnyAsync(p => p.UserId != userId && p.DisplayName == name, ct))
                throw new CatalogueValidationException("displayName", "That name or nickname is already in use. Please choose another.");
            if (profile is null)
            {
                profile = new ExplorerProfile(userId, name);
                db.ExplorerProfiles.Add(profile);
            }
            else profile.Rename(name);
        }
        if (!await db.Backpacks.AnyAsync(b => b.UserId == profile.Id, ct))
            db.Backpacks.Add(new Backpack(profile.Id));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505" })
        {
            // EF rolls this save back to its transaction savepoint. Do not retain failed inserts.
            foreach (var entry in db.ChangeTracker.Entries().Where(e => e.Entity is ExplorerProfile or Backpack).ToArray())
                entry.State = EntityState.Detached;
            throw new CatalogueValidationException("displayName", "That name or nickname is already in use. Please choose another.");
        }
        if (ownedTransaction is not null) await ownedTransaction.CommitAsync(ct);
    }
}
