using System.Net.Http.Json;
using DiaperScout.Application;
using DiaperScout.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class ExploreApiTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    public async Task Recent_products_use_creation_audit_not_random_ids_or_later_changes_and_preserve_public_eligibility()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var moderator = RetailListingApiTests.Moderator(factory);
        var older = await PublicVariantCatalogueApiTests.CreateProductAsync(moderator, fixture);
        var newer = await PublicVariantCatalogueApiTests.CreateProductAsync(moderator, fixture);
        await PublicVariantCatalogueApiTests.AddVariantAsync(moderator, newer.ProductId, "Second variant");
        var discontinued = await PublicVariantCatalogueApiTests.CreateProductAsync(moderator, fixture);
        var prototype = await PublicVariantCatalogueApiTests.CreateProductAsync(moderator, fixture);
        await using var db = fixture.CreateDbContext();
        // Deterministic chronology, deliberately opposite an updated/changed timestamp.
        var audits = await db.CatalogueAuditRecords.Where(a => a.Action == CatalogueAuditAction.ProductCreated).ToListAsync();
        foreach(var audit in audits) db.Entry(audit).Property(a => a.OccurredAtUtc).CurrentValue = audit.ProductId == newer.ProductId
            ? new DateTimeOffset(2026,10,2,0,0,0,TimeSpan.Zero) : new DateTimeOffset(2026,9,1,0,0,0,TimeSpan.Zero);
        db.CatalogueAuditRecords.Add(new(CatalogueAuditAction.ProductChanged, older.ProductId, fixture.ModeratorUserId,
            DateTimeOffset.UtcNow,"{}","[]","Test","[]","Test",null));
        var retired = await db.Products.SingleAsync(p => p.Id == discontinued.ProductId);
        db.Entry(retired).Property(p => p.Status).CurrentValue = ProductStatus.Discontinued;
        var draft = await db.Products.SingleAsync(p => p.Id == prototype.ProductId);
        db.Entry(draft).Property(p => p.Status).CurrentValue = ProductStatus.Prototype;
        await db.SaveChangesAsync();
        using var anonymous = factory.CreateClient();
        var recent = (await anonymous.GetFromJsonAsync<RecentCatalogueProduct[]>("/api/v1/explore/recent-products"))!;
        Assert.Equal(newer.ProductId, recent[0].Id);
        Assert.Contains(recent,p=>p.Id==older.ProductId);
        Assert.Equal(recent.Length,recent.Select(p=>p.Id).Distinct().Count());
        Assert.DoesNotContain(recent,p=>p.Id==fixture.ProductId); // Existing seed has no trustworthy audit date.
        Assert.DoesNotContain(recent,p=>p.Id==retired.Id || p.Id==draft.Id);
        Assert.Null(recent.Single(p=>p.Id==older.ProductId).ImageUrl);
        Assert.Null(recent.Single(p=>p.Id==newer.ProductId).ImageUrl);
        foreach(var item in recent) {
            var details = await anonymous.GetFromJsonAsync<CatalogueProductDetails>($"/api/v1/products/{item.Slug}?variantId={item.ProductVariantId}");
            Assert.Equal(item.Id,details!.Id);
        }
    }

    [Fact]
    public async Task Recent_products_only_reuse_public_published_image_projection()
    {
        using var factory = new ObservationApiFactory(fixture);
        using var moderator = RetailListingApiTests.Moderator(factory);
        var receipt = await PublicVariantCatalogueApiTests.CreateProductAsync(moderator,fixture);
        await using var db=fixture.CreateDbContext();
        var submission=new CatalogueSubmission(CatalogueSubmissionSource.Moderator,fixture.ModeratorUserId,"Test maker","Test product","Test variant",null,"Test image");
        var hidden=new CatalogueSubmissionImage(submission.Id,CatalogueSubmissionImageRole.ProductFront,"test/hidden.webp","hidden.webp","image/webp",100,
            CatalogueImageSourceType.Manufacturer,"https://example.test/hidden.webp","Test",CatalogueImagePermissionStatus.PermissionGranted,"Test");
        hidden.PublishToProduct(receipt.ProductId); hidden.SetPrimary(true);
        hidden.UpdateMetadata(CatalogueImageSourceType.Manufacturer,"https://example.test/hidden.webp","Test",CatalogueImagePermissionStatus.PermissionRequested,"Test");
        db.AddRange(submission,hidden); await db.SaveChangesAsync();
        using var anonymous=factory.CreateClient();
        var recent=(await anonymous.GetFromJsonAsync<RecentCatalogueProduct[]>("/api/v1/explore/recent-products"))!;
        Assert.Null(recent.Single(p=>p.Id==receipt.ProductId).ImageUrl);
        hidden.UpdateMetadata(CatalogueImageSourceType.Manufacturer,"https://example.test/hidden.webp","Test",CatalogueImagePermissionStatus.PermissionGranted,"Test"); await db.SaveChangesAsync();
        recent=(await anonymous.GetFromJsonAsync<RecentCatalogueProduct[]>("/api/v1/explore/recent-products"))!;
        Assert.Equal($"/api/v1/products/{receipt.ProductId}/images/{hidden.Id}",recent.Single(p=>p.Id==receipt.ProductId).ImageUrl);
    }
}
