extern alias DiaperScoutWeb;

using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using AdminUsers = DiaperScoutWeb::DiaperScout.Web.Components.Pages.AdminUsers;
using UserManagementClient = DiaperScoutWeb::DiaperScout.Web.Services.UserManagementClient;

namespace DiaperScout.Api.IntegrationTests;

public sealed class UserManagementApiTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Theory]
    [InlineData(false, false, true, "Explorer")]
    [InlineData(true, false, true, "Administrator")]
    [InlineData(true, true, true, "Moderator · Administrator")]
    [InlineData(true, false, false, "Administrator")]
    [InlineData(true, true, false, "Moderator · Administrator")]
    [InlineData(false, false, false, "Explorer")]
    public async Task Users_PreserveActiveRoles_FromDatabaseThroughClientAndPage(
        bool administrator, bool moderator, bool hasProfile, string expectedRoles)
    {
        var user = new User($"role-display-{Guid.NewGuid():N}");
        var email = new UserEmail(user.Id, $"{user.Subject}@example.test");
        await using (var db = fixture.CreateDbContext())
        {
            db.AddRange(user, email);
            if (hasProfile) db.Add(new ExplorerProfile(user.Id, user.Subject));
            // Revoked roles must never appear, including when the same role is re-granted.
            var revoked = new PrivilegedRoleAssignment(user.Id, PrivilegedRole.Moderator,
                fixture.AdministratorUserId, DateTimeOffset.UtcNow.AddDays(-1));
            revoked.Revoke(fixture.AdministratorUserId, DateTimeOffset.UtcNow);
            db.Add(revoked);
            if (administrator)
                db.Add(new PrivilegedRoleAssignment(user.Id, PrivilegedRole.Administrator,
                    fixture.AdministratorUserId, DateTimeOffset.UtcNow));
            if (moderator)
                db.Add(new PrivilegedRoleAssignment(user.Id, PrivilegedRole.Moderator,
                    fixture.AdministratorUserId, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }

        using var factory = new ObservationApiFactory(fixture);
        using var http = factory.CreateClient();
        http.DefaultRequestHeaders.Add("X-Development-Subject",
            administrator ? user.Subject : PostgreSqlFixture.AdministratorSubject);
        http.DefaultRequestHeaders.Add("X-Development-Role", "Administrator");

        var response = await http.GetAsync("/api/v1/user-management/users");
        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<UserManagementItem[]>();
        var item = Assert.Single(items!, value => value.Id == user.Id);
        Assert.Equal(hasProfile ? user.Subject : string.Empty, item.DisplayName);
        var expected = new List<PrivilegedRole>();
        if (moderator) expected.Add(PrivilegedRole.Moderator);
        if (administrator) expected.Add(PrivilegedRole.Administrator);
        Assert.Equal(expected, item.Roles);

        var client = new UserManagementClient(http);
        var clientItem = Assert.Single(await client.GetUsersAsync(), value => value.Id == user.Id);
        Assert.Equal(expected, clientItem.Roles);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(client);
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new PageRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = renderer.BeginRenderingComponent(typeof(AdminUsers), ParameterView.Empty);
            await component.QuiescenceTask;
            return component.ToHtmlString();
        });
        var identityIndex = html.IndexOf(email.Email, StringComparison.Ordinal);
        Assert.True(identityIndex >= 0, "The account email should be rendered in the user table.");
        var rowStart = html.LastIndexOf("<div class=\"admin-users-row\"", identityIndex, StringComparison.Ordinal);
        Assert.True(rowStart >= 0, "The account should be rendered in the user table.");
        var identityEnd = html.IndexOf("</div>", rowStart, StringComparison.Ordinal);
        var rowEnd = html.IndexOf("</div>", identityEnd + 6, StringComparison.Ordinal);
        var row = html[rowStart..rowEnd];
        var spans = System.Text.RegularExpressions.Regex.Matches(row, "<span[^>]*>(.*?)</span>",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        var displayedRoles = System.Net.WebUtility.HtmlDecode(spans[^1].Groups[1].Value).Trim();
        Assert.Equal(expectedRoles, displayedRoles);
        var name = System.Text.RegularExpressions.Regex.Match(row, "<strong[^>]*>(.*?)</strong>",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.Equal(hasProfile ? user.Subject : email.Email,
            System.Net.WebUtility.HtmlDecode(name.Groups[1].Value).Trim());
    }

    // Exercise the page's normal lifecycle and markup without starting a Blazor circuit.
    private sealed class PageRenderer(IServiceProvider services, ILoggerFactory loggerFactory)
        : StaticHtmlRenderer(services, loggerFactory)
    {
        protected override IComponent ResolveComponentForRenderMode(Type componentType,
            int? parentComponentId, IComponentActivator componentActivator, IComponentRenderMode renderMode)
            => componentActivator.CreateInstance(componentType);
    }
}
