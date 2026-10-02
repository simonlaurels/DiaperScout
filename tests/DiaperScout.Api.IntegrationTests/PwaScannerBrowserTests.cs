using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class PwaScannerBrowserTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    // Deterministic permission/lifecycle harness, not proof of an installed iPhone camera.
    private const string Camera = """
        Object.defineProperty(navigator,'standalone',{value:true});
        const camera = window.camera = {requests:0,stops:0,active:0,maxActive:0,error:null,code:null,hidden:false,hold:false,pending:[],torch:true,lights:[]};
        Object.defineProperty(document,'hidden',{get:()=>camera.hidden,configurable:true});
        Object.defineProperty(HTMLMediaElement.prototype,'readyState',{get:()=>4,configurable:true});
        HTMLMediaElement.prototype.play=async function(){};
        HTMLMediaElement.prototype.pause=function(){};
        const sources=new WeakMap();for(const prototype of [HTMLMediaElement.prototype,HTMLVideoElement.prototype])Object.defineProperty(prototype,'srcObject',{get(){return sources.get(this)||null;},set(value){sources.set(this,value);},configurable:true});
        Object.defineProperty(navigator,'mediaDevices',{value:{getUserMedia:async()=>{
          camera.requests++;
          if(camera.hold)await new Promise(resolve=>camera.pending.push(resolve));
          if(camera.error)throw new DOMException('Camera fixture',camera.error);
          let stopped=false;camera.active++;camera.maxActive=Math.max(camera.active,camera.maxActive);
          const track={stop(){if(!stopped){stopped=true;camera.stops++;camera.active--;}},getCapabilities:()=>({torch:camera.torch}),applyConstraints:async c=>camera.lights.push(c.advanced[0].torch)};
          return {getTracks:()=>[track],getVideoTracks:()=>[track]};
        }},configurable:true});
        window.BarcodeDetector=class {static async getSupportedFormats(){return ['ean_13','upc_e'];}async detect(){if(camera.detectError)throw new Error('Detector fixture');return camera.code?[{rawValue:camera.code,format:camera.format||'ean_13'},{rawValue:camera.code,format:camera.format||'ean_13'}]:[];}};
        """;

    [Theory]
    [InlineData(320, 844, false)]
    [InlineData(390, 844, false)]
    [InlineData(430, 932, true)]
    [InlineData(844, 390, true)]
    [Trait("Category", "Browser")]
    public async Task Camera_owns_viewport_above_unchanged_navigation_and_unknown_capture_leaves_camera(int width, int height, bool webkit)
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
        using var client = web.CreateClient();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await (webkit ? playwright.Webkit : playwright.Chromium).LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = width, Height = height }, HasTouch = true });
        await context.AddInitScriptAsync(Camera + "camera.hold=true;");
        var page = await context.NewPageAsync();
        var errors = new List<string>(); page.PageError += (_, message) => errors.Add(message);
        await page.GotoAsync(client.BaseAddress!.GetLeftPart(UriPartial.Authority) + "/scan");
        await page.WaitForFunctionAsync("() => camera.requests===1");
        await Assertions.Expect(page.Locator(".scanner-state h2")).ToHaveTextAsync("Opening your camera…");
        await Assertions.Expect(page.Locator(".ds-app-header")).ToBeHiddenAsync();
        var nav = page.Locator(".pwa-mobile-nav");
        await Assertions.Expect(nav).ToBeVisibleAsync();
        await Assertions.Expect(nav.Locator("a[href='/scan']")).ToHaveAttributeAsync("aria-current", "page");
        var navigation = await nav.BoundingBoxAsync();
        var video = await page.Locator("video").BoundingBoxAsync();
        Assert.NotNull(navigation); Assert.NotNull(video);
        Assert.InRange(video.X, -1, 1); Assert.InRange(video.Y, -1, 1);
        Assert.InRange(video.Width, width - 1, width + 1);
        Assert.InRange(video.Height, navigation.Y - 1, navigation.Y + 1);
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth<=innerWidth"));
        await CaptureAsync(page, $"scanner-startup-{width}.png");
        await page.EvaluateAsync("camera.hold=false;camera.pending.splice(0).forEach(resolve=>resolve());");
        await CameraReadyAsync(page);
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Toggle flashlight" })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Toggle flashlight" }).ClickAsync();
        await page.WaitForFunctionAsync("() => camera.lights.includes(true)");
        await CaptureAsync(page, $"scanner-ready-{width}.png");
        await page.EvaluateAsync("camera.code='96385074';");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "We don’t have this one yet." })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".scan-page")).ToHaveAttributeAsync("data-capture", "false");
        await page.WaitForFunctionAsync("() => camera.active===0 && camera.stops===1");
        await Assertions.Expect(nav).ToHaveCSSAsync("height", $"{navigation.Height}px");
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Add product", Exact = true })).ToHaveAttributeAsync("href", "/contribute/product?gtin=96385074");
        await page.GetByRole(AriaRole.Link, new() { Name = "Add product", Exact = true }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Add a Missing Product" })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".scanner-overlay")).ToHaveCountAsync(0);
        Assert.Equal(1, await page.EvaluateAsync<int>("camera.maxActive"));
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("NotAllowedError", "Enable camera access")]
    [InlineData("NotFoundError", "No camera is available")]
    [InlineData("NotReadableError", "couldn’t start the camera")]
    [Trait("Category", "Browser")]
    public async Task Permission_and_initialisation_errors_allow_retry_and_accessible_manual_entry(string error, string message)
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
        using var client = web.CreateClient(); using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Webkit.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 390, Height = 844 } });
        await context.AddInitScriptAsync(Camera + $"camera.error='{error}';camera.torch=false;");
        var page = await context.NewPageAsync();
        await page.GotoAsync(client.BaseAddress!.GetLeftPart(UriPartial.Authority) + "/scan");
        await Assertions.Expect(page.Locator(".scanner-state")).ToContainTextAsync(message);
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Toggle flashlight" })).ToHaveCountAsync(0);
        await page.EvaluateAsync("camera.error=null;");
        await page.GetByRole(AriaRole.Button, new() { Name = "Try camera again" }).ClickAsync();
        await CameraReadyAsync(page);
        await page.GetByRole(AriaRole.Button, new() { Name = "Enter barcode number", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("#gtin")).ToBeFocusedAsync();
        Assert.Equal(0, await page.EvaluateAsync<int>("camera.active"));
        await page.Locator("#gtin").FillAsync("96385074");
        await page.GetByRole(AriaRole.Button, new() { Name = "Look up", Exact = true }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "We don’t have this one yet." })).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("Category", "Browser")]
    public async Task Known_capture_keeps_exact_pack_and_variant_and_lifecycle_cancels_late_camera()
    {
        using var api = new ObservationApiFactory(fixture);
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DiaperScoutDbContext>();
            var pack = await (from p in db.PackTypes join s in db.SizeVariants on p.SizeVariantId equals s.Id join v in db.ProductVariants on s.ProductVariantId equals v.Id where v.ProductId == fixture.ProductId select p).SingleAsync();
            if (!await db.ProductIdentifiers.AnyAsync(i => i.Value == "036000291452")) { db.ProductIdentifiers.Add(new ProductIdentifier(pack.Id, IdentifierType.Gtin, "036000291452")); await db.SaveChangesAsync(); }
        }
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
        using var client = web.CreateClient(); using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 390, Height = 844 } });
        await context.AddInitScriptAsync(Camera);
        var page = await context.NewPageAsync(); var origin = client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        await page.GotoAsync(origin + "/scan");
        await page.WaitForFunctionAsync("() => camera.active===1");
        await page.EvaluateAsync("camera.hidden=true;document.dispatchEvent(new Event('visibilitychange'));camera.hidden=false;document.dispatchEvent(new Event('visibilitychange'));");
        await Assertions.Expect(page.Locator(".scanner-state")).ToContainTextAsync("app was away");
        Assert.Equal(0, await page.EvaluateAsync<int>("camera.active"));
        await page.EvaluateAsync("camera.hold=true;");
        await page.GetByRole(AriaRole.Button, new() { Name = "Try camera again" }).ClickAsync();
        await page.WaitForFunctionAsync("() => camera.pending.length===1");
        await page.Locator(".pwa-mobile-nav a[href='/products']").ClickAsync();
        await Assertions.Expect(page.Locator("#pwa-search-query")).ToBeVisibleAsync();
        await page.EvaluateAsync("camera.hold=false;camera.pending.splice(0).forEach(resolve=>resolve());");
        await page.WaitForFunctionAsync("() => camera.stops===2 && camera.active===0");
        await page.Locator(".pwa-mobile-nav a[href='/scan']").ClickAsync();
        await page.WaitForFunctionAsync("() => camera.requests===3 && camera.active===1");
        await page.EvaluateAsync("camera.code='036000291452';");
        await Assertions.Expect(page.Locator(".scan-result-found")).ToContainTextAsync("Exact pack found");
        var href = await page.GetByRole(AriaRole.Link, new() { Name = "View product", Exact = true }).GetAttributeAsync("href");
        Assert.Contains("variantId=", href); Assert.Contains("packTypeId=", href);
        await page.WaitForFunctionAsync("() => camera.active===0");
        Assert.Equal(1, await page.EvaluateAsync<int>("camera.maxActive"));
        await page.EvaluateAsync("camera.code=null;");
        await page.GetByRole(AriaRole.Button, new() { Name = "Scan another item" }).ClickAsync();
        await page.WaitForFunctionAsync("() => camera.requests===4 && camera.active===1");
        await page.Locator(".pwa-mobile-nav a[href='/']").ClickAsync();
        await page.WaitForFunctionAsync("() => camera.active===0");
    }

    [Fact]
    [Trait("Category", "Browser")]
    public async Task Bundled_Zxing_fallback_uses_real_browser_camera_and_releases_tracks_on_navigation()
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
        using var client = web.CreateClient(); using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, Args = ["--use-fake-ui-for-media-stream", "--use-fake-device-for-media-stream"] });
        await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 390, Height = 844 } });
        await context.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});delete window.BarcodeDetector;window.scannerTracks=[];const get=navigator.mediaDevices.getUserMedia.bind(navigator.mediaDevices);navigator.mediaDevices.getUserMedia=async c=>{const stream=await get(c);window.scannerTracks.push(...stream.getTracks());return stream;};");
        var page = await context.NewPageAsync();
        await page.GotoAsync(client.BaseAddress!.GetLeftPart(UriPartial.Authority) + "/scan");
        await Assertions.Expect(page.Locator(".scan-page")).ToHaveAttributeAsync("data-camera-ready", "true");
        Assert.True(await page.EvaluateAsync<bool>("!!window.ZXingBrowser?.BrowserMultiFormatOneDReader"));
        Assert.True(await page.EvaluateAsync<bool>("scannerTracks.length===1 && scannerTracks[0].readyState==='live'"));
        await page.Locator(".pwa-mobile-nav a[href='/products']").ClickAsync();
        await page.WaitForFunctionAsync("() => scannerTracks.every(t=>t.readyState==='ended')");
    }

    private static async Task CaptureAsync(IPage page, string name)
    {
        var directory = Environment.GetEnvironmentVariable("DIAPERSCOUT_TEST_ARTIFACTS");
        if (directory is null) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new() { Path = Path.Combine(directory, name) });
    }
    [Fact]
    [Trait("Category", "Browser")]
    public async Task Rapid_manual_camera_switch_serialises_permission_and_detector_failure_is_recoverable()
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
        using var client = web.CreateClient(); using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 390, Height = 844 } });
        await context.AddInitScriptAsync(Camera + "camera.hold=true;");
        var page = await context.NewPageAsync();
        await page.GotoAsync(client.BaseAddress!.GetLeftPart(UriPartial.Authority) + "/scan");
        await page.WaitForFunctionAsync("() => camera.pending.length===1");
        await page.GetByRole(AriaRole.Button, new() { Name = "Enter barcode number", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Use camera instead", Exact = true }).ClickAsync();
        Assert.Equal(1, await page.EvaluateAsync<int>("camera.requests"));
        await page.EvaluateAsync("camera.hold=false;camera.pending.splice(0).forEach(resolve=>resolve());");
        await CameraReadyAsync(page);
        await page.WaitForFunctionAsync("() => camera.requests===2 && camera.active===1 && camera.stops===1");
        await page.EvaluateAsync("camera.detectError=true;");
        await Assertions.Expect(page.Locator(".scanner-state")).ToContainTextAsync("couldn’t read that barcode");
        Assert.Equal(0, await page.EvaluateAsync<int>("camera.active"));
        await page.EvaluateAsync("camera.detectError=false;");
        await page.GetByRole(AriaRole.Button, new() { Name = "Try camera again", Exact = true }).ClickAsync();
        await CameraReadyAsync(page);
        await page.EvaluateAsync("camera.code='12345678';");
        await Assertions.Expect(page.Locator(".scan-panel > .scan-message")).ToContainTextAsync("including its check digit");
        Assert.Equal(0, await page.EvaluateAsync<int>("camera.active"));
        Assert.Equal(1, await page.EvaluateAsync<int>("camera.maxActive"));
    }

    [Fact]
    [Trait("Category", "Browser")]
    public async Task Desktop_retains_explicit_camera_start_and_manual_lookup()
    {
        using var api = new ObservationApiFactory(fixture);
        using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
        using var client = web.CreateClient(); using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1280, Height = 900 } });
        await context.AddInitScriptAsync(Camera.Replace("value:true", "value:false"));
        var page = await context.NewPageAsync();
        await page.GotoAsync(client.BaseAddress!.GetLeftPart(UriPartial.Authority) + "/scan");
        await Assertions.Expect(page.Locator("#gtin")).ToBeEnabledAsync();
        await Assertions.Expect(page.Locator("#scan-heading")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".scan-camera-preview")).ToBeHiddenAsync();
        Assert.Equal(0, await page.EvaluateAsync<int>("camera.requests"));
        await page.Locator("#gtin").FillAsync("96385074");
        await page.GetByRole(AriaRole.Button, new() { Name = "Look up", Exact = true }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "We don’t have this one yet." })).ToBeVisibleAsync();
        Assert.Equal(0, await page.EvaluateAsync<int>("camera.requests"));
        await page.GetByRole(AriaRole.Button, new() { Name = "Scan another item", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Scan barcode", Exact = true }).ClickAsync();
        await page.WaitForFunctionAsync("() => camera.active===1");
        await page.EvaluateAsync("camera.hidden=true;document.dispatchEvent(new Event('visibilitychange'));camera.hidden=false;");
        await Assertions.Expect(page.Locator(".scan-camera > .scan-message")).ToContainTextAsync("Start the camera again");
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Scan barcode", Exact = true })).ToBeEnabledAsync();
        Assert.Equal(0, await page.EvaluateAsync<int>("camera.active"));
    }
    [Fact]
    [Trait("Category", "Browser")]
    public async Task Bundled_Zxing_recognises_real_Ean8_video_frames_and_finishes_once()
    {
        var path = Path.Combine(Path.GetTempPath(), $"diaperscout-ean8-{Guid.NewGuid():N}.y4m");
        try
        {
            WriteEan8Video(path, "96385074");
            using var api = new ObservationApiFactory(fixture);
            using var web = new PasskeyWebFactory(api); web.UseKestrel(0);
            using var client = web.CreateClient(); using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, Args = ["--use-fake-ui-for-media-stream", "--use-fake-device-for-media-stream", $"--use-file-for-fake-video-capture={path}"] });
            await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 390, Height = 844 } });
            await context.AddInitScriptAsync("Object.defineProperty(navigator,'standalone',{value:true});delete window.BarcodeDetector;window.scannerTracks=[];window.cameraRequests=0;const get=navigator.mediaDevices.getUserMedia.bind(navigator.mediaDevices);navigator.mediaDevices.getUserMedia=async c=>{cameraRequests++;const stream=await get(c);scannerTracks.push(...stream.getTracks());return stream;};");
            var page = await context.NewPageAsync();
            await page.GotoAsync(client.BaseAddress!.GetLeftPart(UriPartial.Authority) + "/scan");
            await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "We don’t have this one yet." })).ToBeVisibleAsync(new() { Timeout = 15000 });
            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Add product", Exact = true })).ToHaveAttributeAsync("href", "/contribute/product?gtin=96385074");
            await page.WaitForFunctionAsync("() => scannerTracks.length===1 && scannerTracks.every(t=>t.readyState==='ended')");
            Assert.Equal(1, await page.EvaluateAsync<int>("cameraRequests"));
            await Assertions.Expect(page.Locator(".scan-page")).ToHaveAttributeAsync("data-capture", "false");
        }
        finally { File.Delete(path); }
    }

    private static void WriteEan8Video(string path, string code)
    {
        string[] left = ["0001101", "0011001", "0010011", "0111101", "0100011", "0110001", "0101111", "0111011", "0110111", "0001011"];
        var bars = "101" + string.Concat(code.Take(4).Select(c => left[c - '0'])) + "01010" + string.Concat(code.Skip(4).Select(c => new string(left[c - '0'].Select(bit => bit == '0' ? '1' : '0').ToArray()))) + "101";
        var frame = Enumerable.Repeat((byte)235, 640 * 480).Concat(Enumerable.Repeat((byte)128, 640 * 480 / 2)).ToArray();
        for (var y = 180; y < 300; y++)
            for (var module = 0; module < bars.Length; module++)
                if (bars[module] == '1') for (var x = 0; x < 5; x++) frame[y * 640 + 152 + module * 5 + x] = 16;
        using var file = File.Create(path);
        file.Write(System.Text.Encoding.ASCII.GetBytes("YUV4MPEG2 W640 H480 F30:1 Ip A1:1 C420jpeg\n"));
        for (var n = 0; n < 30; n++) { file.Write(System.Text.Encoding.ASCII.GetBytes("FRAME\n")); file.Write(frame); }
    }
    private static async Task CameraReadyAsync(IPage page)
    {
        try { await Assertions.Expect(page.Locator(".scan-page")).ToHaveAttributeAsync("data-camera-ready", "true"); }
        catch (PlaywrightException exception)
        {
            var diagnostic = await page.EvaluateAsync<string>("JSON.stringify({debug:window.scannerDebug,requests:camera.requests,active:camera.active,stops:camera.stops,hidden:document.hidden,state:document.querySelector('.scanner-state')?.textContent,ready:document.querySelector('.scan-page').dataset.cameraReady,videoConnected:document.querySelector('video').isConnected,videoReady:document.querySelector('video').readyState,overlay:getComputedStyle(document.querySelector('.scanner-overlay')).display})");
            await CaptureAsync(page, "scanner-ready-failure.png");
            throw new InvalidOperationException(diagnostic, exception);
        }
    }
}
