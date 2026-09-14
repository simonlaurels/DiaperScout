using DiaperScout.Domain;
using DiaperScout.Infrastructure;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace DiaperScout.Domain.Tests;

public sealed class CurrentExplorerTests
{
    [Fact]
    public async Task Resolves_active_user_to_its_explorer_profile()
    {
        await using var db = CreateDb();
        var user = new User("development-user");
        db.Users.Add(user);
        db.ExplorerProfiles.Add(new ExplorerProfile(user.Id, "Harbour Explorer"));
        await db.SaveChangesAsync();
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "development-user")], "test")) };

        var result = await new CurrentExplorer(db, new HttpContextAccessor { HttpContext = context }).GetAsync();

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("Harbour Explorer", result.DisplayName);
    }

    [Fact]
    public async Task Does_not_resolve_anonymous_request()
    {
        await using var db = CreateDb();
        var result = await new CurrentExplorer(db, new HttpContextAccessor { HttpContext = new DefaultHttpContext() }).GetAsync();
        Assert.Null(result);
    }

    private static DiaperScoutDbContext CreateDb() => new(new DbContextOptionsBuilder<DiaperScoutDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
