using System.Net;
using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class BackpackAccountTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Personal_reads_and_name_edits_are_owned_validated_and_private()
    {
        var owner=new User("backpack-owner-"+Guid.NewGuid());var other=new User("backpack-other-"+Guid.NewGuid());
        var ownerName="Owner "+Guid.NewGuid();var otherName="Other "+Guid.NewGuid();
        await using(var db=fixture.CreateDbContext()) {
            var country=await db.Countries.SingleAsync(c=>c.IsoCode=="ZZ");
            var shop=Location.PublicShop(owner.Id,country.Id,"Owner's shop","1 Street","Town","ZZ1",51,-2,Guid.NewGuid().ToString());
            var foreignShop=Location.PublicShop(other.Id,country.Id,"Other's private contribution","2 Street","Town","ZZ2",52,-2,Guid.NewGuid().ToString());
            var seen=new Observation(owner.Id,ObservationType.RetailAvailability,DateTimeOffset.UtcNow,fixture.ProductId,locationId:shop.Id);
            seen.RecordExactPack(fixture.PackTypeId,Guid.NewGuid(),null,null);seen.Submit();
            var proposal=new CatalogueSubmission(CatalogueSubmissionSource.Explorer,owner.Id,"Test manufacturer","Owner proposal",null,"Test brand");proposal.SetPublicContribution(Guid.NewGuid());proposal.BeginVerification();
            var draft=new CatalogueSubmission(CatalogueSubmissionSource.Moderator,owner.Id,"Test manufacturer","Owned catalogue draft",null,"Test brand");
            var otherDraft=new CatalogueSubmission(CatalogueSubmissionSource.Moderator,other.Id,"Test manufacturer","Other catalogue draft",null,"Test brand");
            db.AddRange(owner,other,new ExplorerProfile(owner.Id,ownerName),new ExplorerProfile(other.Id,otherName),new UserEmail(owner.Id,"owner-backpack@example.test"),shop,foreignShop,seen,proposal,draft,otherDraft,new PrivilegedRoleAssignment(owner.Id,PrivilegedRole.Moderator,fixture.AdministratorUserId,DateTimeOffset.UtcNow));await db.SaveChangesAsync();
        }
        using var api=new ObservationApiFactory(fixture);using var client=api.CreateClient();client.DefaultRequestHeaders.Add("X-Development-Subject",owner.Subject);
        var account=await client.GetAsync("/api/v1/me/backpack/account");account.EnsureSuccessStatusCode();Assert.True(account.Headers.CacheControl?.NoStore);
        var info=(await account.Content.ReadFromJsonAsync<BackpackAccountInfo>())!;Assert.Equal(ownerName,info.DisplayName);Assert.Equal("owner-backpack@example.test",info.Email);
        var discoveries=(await client.GetFromJsonAsync<PersonalDiscovery[]>("/api/v1/me/backpack/discoveries"))!;
        Assert.Equal(3,discoveries.Length);Assert.DoesNotContain(discoveries,d=>d.Name.Contains("Other's"));
        Assert.Contains(discoveries,d=>d.Kind=="Product proposal" && d.Name=="Owner proposal" && d.Url==null);
        Assert.Equal(discoveries.Single(d=>d.Kind=="Place added").Url,discoveries.Single(d=>d.Kind=="Product discovery").Url);
        var drafts=(await client.GetFromJsonAsync<PersonalCatalogueDrafts>("/api/v1/me/backpack/drafts"))!;
        Assert.Equal(1,drafts.TotalCount);Assert.Equal("Owned catalogue draft",Assert.Single(drafts.Items).Name);Assert.EndsWith("/2",drafts.Items[0].Url);
        using var otherClient=api.CreateClient();otherClient.DefaultRequestHeaders.Add("X-Development-Subject",other.Subject);
        Assert.Equal(0,(await otherClient.GetFromJsonAsync<PersonalCatalogueDrafts>("/api/v1/me/backpack/drafts"))!.TotalCount);
        foreach(var invalid in new[]{"",new string('x',101),otherName})Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/v1/me/backpack/name",new UpdateExplorerName(invalid))).StatusCode);
        var renamed="Renamed "+Guid.NewGuid();Assert.Equal(HttpStatusCode.NoContent,(await client.PostAsJsonAsync("/api/v1/me/backpack/name",new UpdateExplorerName(" "+renamed+" "))).StatusCode);
        await using(var db=fixture.CreateDbContext()){Assert.Equal(renamed,(await db.ExplorerProfiles.SingleAsync(p=>p.UserId==owner.Id)).DisplayName);Assert.Equal(otherName,(await db.ExplorerProfiles.SingleAsync(p=>p.UserId==other.Id)).DisplayName);}
        using var anonymous=api.CreateClient();Assert.Contains((await anonymous.GetAsync("/api/v1/me/backpack/account")).StatusCode,new[]{HttpStatusCode.Unauthorized,HttpStatusCode.Forbidden});
    }
}
