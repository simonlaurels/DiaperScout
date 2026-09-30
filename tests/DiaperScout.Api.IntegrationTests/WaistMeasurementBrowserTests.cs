using System.Text.RegularExpressions;
using DiaperScout.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class WaistMeasurementBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    [Trait("Category", "Browser")]
    public async Task PublishedProductEditor_InchEntryConvertsOnAddAndEdit()
    {
        using var api = new ObservationApiFactory(fixture);
        using var webBase = new PasskeyWebFactory(api);
        using var web = webBase.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            { ["Authentication:Development:Subject"] = PostgreSqlFixture.ModeratorSubject })));
        web.UseKestrel(0);
        using var client = web.CreateClient();
        var origin = client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 390, Height = 900 } });
        await page.GotoAsync(origin + "/signin/development");
        await page.GotoAsync(origin + $"/catalogue/products/manage/{fixture.ProductId}");
        await page.GetByRole(AriaRole.Button, new() { Name = "+ Add size", Exact = true }).ClickAsync();
        var editor = page.Locator(".product-editor-size-editor").Filter(new() { Has = page.Locator(".waist-measurements") });
        var waist = page.Locator(".waist-measurements");
        await waist.Locator("select").SelectOptionAsync("Inches");
        await waist.Locator("input").Nth(0).FillAsync("30");
        await waist.Locator("input").Nth(1).FillAsync("40");
        await waist.Locator("input").Nth(1).BlurAsync();
        var first = (await waist.Locator("input").Nth(0).BoundingBoxAsync())!;
        var selector = (await waist.Locator("select").BoundingBoxAsync())!;
        Assert.True(Math.Abs(first.Y - selector.Y) < 2);
        Assert.True(selector.X + selector.Width <= 390);
        await editor.Locator(".submission-field").Filter(new() { HasText = "Manufacturer size" }).Locator("input").FillAsync("Inch test");
        await page.GetByRole(AriaRole.Button, new() { Name = "Add size", Exact = true }).ClickAsync();
        await Assertions.Expect(waist).ToHaveCountAsync(0);
        await using (var db = fixture.CreateDbContext())
        {
            var size = await db.SizeVariants.SingleAsync(x => x.ManufacturerSize == "Inch test");
            Assert.Equal(76, size.WaistMinimumCm);
            Assert.Equal(102, size.WaistMaximumCm);
        }
        var row = page.Locator(".product-editor-size-row").Filter(new() { HasText = "Inch test" });
        await row.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        await waist.Locator("select").SelectOptionAsync("Inches");
        await waist.Locator("input").Nth(0).FillAsync("32.5");
        await waist.Locator("input").Nth(0).BlurAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Save size", Exact = true }).ClickAsync();
        await Assertions.Expect(waist).ToHaveCountAsync(0);
        await using (var db = fixture.CreateDbContext())
            Assert.Equal(83, (await db.SizeVariants.SingleAsync(x => x.ManufacturerSize == "Inch test")).WaistMinimumCm);
    }

    [Fact]
    [Trait("Category", "Browser")]
    public async Task SubmissionEditor_InchEntryUnitSwitchAndSave_WithInlineResponsiveSelector()
    {
        var submission = new CatalogueSubmission(CatalogueSubmissionSource.Moderator,
            fixture.ModeratorUserId, "Browser manufacturer", "Browser unit product");
        var variant = new CatalogueSubmissionVariant(submission.Id, null);
        await using (var db = fixture.CreateDbContext())
        {
            db.AddRange(submission, variant);
            await db.SaveChangesAsync();
        }
        using var api = new ObservationApiFactory(fixture);
        using var webBase = new PasskeyWebFactory(api);
        using var web = webBase.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            { ["Authentication:Development:Subject"] = PostgreSqlFixture.ModeratorSubject })));
        web.UseKestrel(0);
        using var client = web.CreateClient();
        var origin = client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 900 } });
        await page.GotoAsync(origin + "/signin/development");
        await page.GotoAsync(origin + $"/catalogue/add/{submission.Id}/3?maxStep=3");
        await page.GetByRole(AriaRole.Button, new() { Name = "＋ Add size", Exact = true }).ClickAsync();
        var waist = page.Locator(".waist-measurements");
        var minimum = waist.Locator("input").Nth(0);
        var maximum = waist.Locator("input").Nth(1);
        var units = waist.Locator("select");
        await units.SelectOptionAsync("Inches");
        await minimum.FillAsync("32.5");
        await maximum.FillAsync("40");
        await maximum.BlurAsync();
        await units.SelectOptionAsync("Centimetres");
        await Assertions.Expect(minimum).ToHaveValueAsync("82.55");
        await Assertions.Expect(maximum).ToHaveValueAsync(new Regex("^101[.]60?$"));
        await units.SelectOptionAsync("Inches");
        await Assertions.Expect(minimum).ToHaveValueAsync(new Regex("^32[.]50?$"));
        await Assertions.Expect(maximum).ToHaveValueAsync(new Regex("^40([.]0+)?$"));
        foreach (var width in new[] { 1280, 390 })
        {
            await page.SetViewportSizeAsync(width, 900);
            var first = (await minimum.BoundingBoxAsync())!;
            var selector = (await units.BoundingBoxAsync())!;
            Assert.True(Math.Abs(first.Y - selector.Y) < 2, "Unit selector must stay inline with waist fields.");
            Assert.True(selector.X + selector.Width <= width, "Waist controls must fit the viewport.");
            var artifacts = Environment.GetEnvironmentVariable("DIAPERSCOUT_TEST_ARTIFACTS");
            if (!string.IsNullOrWhiteSpace(artifacts))
            {
                Directory.CreateDirectory(artifacts);
                await waist.ScreenshotAsync(new() { Path = Path.Combine(artifacts, $"waist-units-{width}.png") });
            }
        }
        await page.Locator("input[placeholder='e.g. Medium']").FillAsync("Medium");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save size", Exact = true }).ClickAsync();
        await Assertions.Expect(waist).ToHaveCountAsync(0);
        await using (var db = fixture.CreateDbContext())
        {
            var size = await db.CatalogueSubmissionSizeVariants.SingleAsync(x => x.VariantId == variant.Id);
            Assert.Equal(83, size.WaistMinimumCm);
            Assert.Equal(102, size.WaistMaximumCm);
        }
        await page.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        await Assertions.Expect(minimum).ToHaveValueAsync("83");
        await units.SelectOptionAsync("Inches");
        await Assertions.Expect(minimum).ToHaveValueAsync("32.68");
    }
}
