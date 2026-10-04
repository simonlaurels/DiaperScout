using DiaperScout.Domain;
using DiaperScout.Application;
using DiaperScout.Web.Components;
using DiaperScout.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebDataProtection(builder.Configuration, builder.Environment);

if (builder.Environment.IsDevelopment())
{
    builder.Services
        .AddAuthentication("DevelopmentCookie")
        .AddCookie("DevelopmentCookie", options =>
        {
            options.Cookie.Name = "DiaperScout.DevelopmentAuth";
            options.LoginPath = "/signin";
            options.LogoutPath = "/signout";
        });
}
else
{
    builder.Services
        .AddAuthentication("ProductionCookie")
        .AddCookie("ProductionCookie", options =>
        {
            options.Cookie.Name = "DiaperScout.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.LoginPath = "/signin";
            options.LogoutPath = "/signout";
        });
}

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler, ReliabilityCircuitHandler>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddServiceDiscovery();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<DevelopmentSubjectForwardingHandler>();
builder.Services.AddTransient<ProductionIdentityForwardingHandler>();
builder.Services.AddPasskeyWebServices(builder.Configuration);

builder.Services.AddHttpClient("DiaperScoutApi", client =>
        client.BaseAddress = new Uri(
            builder.Configuration["Api:BaseUrl"]
            ?? "https+http://api"))
    .AddServiceDiscovery();

builder.Services.AddHttpClient<ProductLookupClient>(client =>
        client.BaseAddress = new Uri(
            builder.Configuration["Api:BaseUrl"]
            ?? "https+http://api"))
    .AddServiceDiscovery();

builder.Services.AddHttpClient<PlaceObservationClient>(client =>
        client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https+http://api"))
    .AddHttpMessageHandler<DevelopmentSubjectForwardingHandler>()
    .AddHttpMessageHandler<ProductionIdentityForwardingHandler>()
    .AddServiceDiscovery();

builder.Services.AddHttpClient<UserManagementClient>(client =>
        client.BaseAddress = new Uri(
            builder.Configuration["Api:BaseUrl"]
            ?? "https+http://api"))
    .AddHttpMessageHandler<DevelopmentSubjectForwardingHandler>()
    .AddHttpMessageHandler<ProductionIdentityForwardingHandler>()
    .AddServiceDiscovery();

builder.Services.AddHttpClient<ProductCatalogueClient>(client =>
        client.BaseAddress = new Uri(
            builder.Configuration["Api:BaseUrl"]
            ?? "https+http://api"))
    .AddHttpMessageHandler<DevelopmentSubjectForwardingHandler>()
    .AddHttpMessageHandler<ProductionIdentityForwardingHandler>()
    .AddServiceDiscovery();

builder.Services.AddHttpClient<CommercePluginClient>(client =>
        client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https+http://api"))
    .AddHttpMessageHandler<DevelopmentSubjectForwardingHandler>()
    .AddHttpMessageHandler<ProductionIdentityForwardingHandler>()
    .AddServiceDiscovery();

builder.Services.AddHttpClient<CatalogueAffiliateClient>(client =>
        client.BaseAddress = new Uri(
            builder.Configuration["Api:BaseUrl"]
            ?? "https+http://api"))
    .AddHttpMessageHandler<DevelopmentSubjectForwardingHandler>()
    .AddHttpMessageHandler<ProductionIdentityForwardingHandler>()
    .AddServiceDiscovery();

builder.Services.AddHttpClient<RetailerManagementClient>(client =>
        client.BaseAddress = new Uri(
            builder.Configuration["Api:BaseUrl"]
            ?? "https+http://api"))
    .AddHttpMessageHandler<DevelopmentSubjectForwardingHandler>()
    .AddHttpMessageHandler<ProductionIdentityForwardingHandler>()
    .AddServiceDiscovery();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // Do not advertise a healthy replica until it can read/write the protected shared ring.
    var protector = app.Services.GetRequiredService<IDataProtectionProvider>()
        .CreateProtector("DiaperScout.Web.KeyRingReadiness.v1");
    if (protector.Unprotect(protector.Protect("ready")) != "ready")
        throw new InvalidOperationException("The shared Data Protection key ring failed its startup check.");
    app.Logger.LogInformation("Shared Data Protection key ring readiness check passed.");
}

if (app.Environment.IsDevelopment() &&
    app.Configuration.GetValue<bool>("LanTesting:Enabled") &&
    !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_LAUNCH_PROFILE")))
{
    var lanTestingUrl = app.Configuration["LanTesting:HttpsUrl"]
        ?? throw new InvalidOperationException("LanTesting:HttpsUrl is required when LAN testing is enabled.");

    if (!Uri.TryCreate(lanTestingUrl, UriKind.Absolute, out var lanTestingUri) || lanTestingUri.Scheme != Uri.UriSchemeHttps)
        throw new InvalidOperationException("LanTesting:HttpsUrl must be an absolute HTTPS URL.");

    app.Urls.Add(lanTestingUri.ToString());
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// Worker updates must be checked against network truth; this does not change TLS or ingress configuration.
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/service-worker.js" || context.Request.Path == "/manifest.webmanifest")
        context.Response.Headers.CacheControl = "no-cache";
    await next();
});
app.UseHttpsRedirection();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/signin") ContributionReturn.Remember(context);
    await next();
});
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();
app.MapPasskeyWebEndpoints();
app.MapBackpackAccountWebEndpoints();
app.MapProposalEvidenceWebEndpoints();

app.MapGet(
    "/catalogue/import-template",
    () => Results.File(
        System.Text.Encoding.UTF8.GetBytes(
            "ImportProductKey,Manufacturer,Brand,ProductName,ProductType,PackagingType,ProductFamily,Description,ProductStatus,OfficialWebsite,VariantName,BackingType,FastenerType,FastenerCount,Appearance,PrimaryColour,WetnessIndicator,StandingLeakGuards,WaistbandStyle,Fragrance,LatexFree,DesignedFor,ConstructionNotes,ManufacturerSize,WaistMinCm,WaistMaxCm,HipMinCm,HipMaxCm,FitMeasurementBasis,AbsorbencyMl,AbsorbencyBasisMethod,AbsorbencySource,LengthMm,WidthMm,WeightGrams,ManufacturerPackQuantity,GTIN,IdentitySourceUrl,Notes,DescriptionVisibility\n" +
            "EXAMPLE-PRODUCT-001,Example Manufacturer,Example Brand,Example Product,Diaper,Bag,Example Family,,Current,https://example.com,Original,Plastic,AdhesiveTape,2,Plain,White,Yes,Yes,AllAroundElastic,None,No,Adult,,Medium,80,110,,,,Waist,7500,Manufacturer stated,Manufacturer,900,700,1800,10,1234567890123,https://example.com/product,,ModeratorOnly\n"),
        "text/csv",
        "DiaperScout-Catalogue-Import-Template.csv"))
    .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
    .WithName("GetCatalogueImportTemplate")
    .WithTags("Catalogue");

app.MapGet(
    "/api/v1/products/{productId:guid}/images/{imageId:guid}",
    async (
        Guid productId,
        Guid imageId,
        ProductCatalogueClient catalogueClient,
        CancellationToken cancellationToken) =>
    {
        var content = await catalogueClient.GetProductImageContentAsync(
            productId, imageId, false, cancellationToken);

        return content is null
            ? Results.NotFound()
            : Results.File(
                content.Content,
                content.ContentType,
                enableRangeProcessing: true);
    })
    .WithName("GetWebCatalogueProductImage")
    .WithTags("Products");

app.MapGet(
    "/api/v1/products/{productId:guid}/moderator-images/{imageId:guid}",
    async (
        Guid productId,
        Guid imageId,
        ProductCatalogueClient catalogueClient,
        CancellationToken cancellationToken) =>
    {
        var content = await catalogueClient.GetProductImageContentAsync(
            productId, imageId, true, cancellationToken);

        return content is null
            ? Results.NotFound()
            : Results.File(
                content.Content,
                content.ContentType,
                enableRangeProcessing: true);
    })
    .RequireAuthorization(policy => policy.RequireRole("Moderator", "Administrator"))
    .WithName("GetWebModeratorCatalogueProductImage")
    .WithTags("Products");

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/signin/request", async (
    IHttpClientFactory httpClientFactory,
    [FromForm] string email,
    CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient("DiaperScoutApi");

    using var response = await client.PostAsJsonAsync(
        "/api/v1/auth/sign-in-link",
        new { Email = email },
        cancellationToken);

    return Results.LocalRedirect(
        response.IsSuccessStatusCode
            ? "/signin?sent=true"
            : "/signin?error=true");
})
    .AllowAnonymous()
    .DisableAntiforgery();

app.MapPost("/join/request", async (
    IHttpClientFactory httpClientFactory,
    [FromForm] string email,
    [FromForm] string displayName,
    CancellationToken cancellationToken) =>
{
    var client = httpClientFactory.CreateClient("DiaperScoutApi");

    using var response = await client.PostAsJsonAsync(
        "/api/v1/auth/registration-link",
        new
        {
            Email = email,
            DisplayName = displayName
        },
        cancellationToken);

    return Results.LocalRedirect(
        response.IsSuccessStatusCode
            ? "/join?sent=true"
            : "/join?error=true");
})
    .AllowAnonymous()
    .DisableAntiforgery();

app.MapGet("/signin/magic-link", async (
    HttpContext httpContext,
    IHttpClientFactory httpClientFactory,
    string token,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(token))
        return Results.LocalRedirect("/signin?error=true");

    var client = httpClientFactory.CreateClient("DiaperScoutApi");

    using var response = await client.PostAsync(
        $"/api/v1/auth/magic-link/consume?token={Uri.EscapeDataString(token)}",
        content: null,
        cancellationToken);

    if (!response.IsSuccessStatusCode)
        return Results.LocalRedirect("/signin?error=true");

    var authentication =
        await response.Content.ReadFromJsonAsync<PasswordlessAuthenticationResult>(
            cancellationToken);

    if (authentication is null)
        return Results.LocalRedirect("/signin?error=true");

    await AuthenticationSession.SignInAsync(httpContext, authentication, app.Environment);

    return Results.LocalRedirect(ContributionReturn.Consume(httpContext));
})
    .AllowAnonymous();

app.MapGet("/signout", async (HttpContext httpContext) =>
{
    var scheme = app.Environment.IsDevelopment()
        ? "DevelopmentCookie"
        : "ProductionCookie";

    await httpContext.SignOutAsync(scheme);
    return Results.LocalRedirect("/");
});

if (app.Environment.IsDevelopment())
{
    app.MapGet("/signin/development", async (
        HttpContext httpContext,
        IConfiguration configuration) =>
    {
        var subject = configuration["Authentication:Development:Subject"]
            ?? "development-moderator";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, subject),
            new Claim(ClaimTypes.Name, subject),
            new Claim(ClaimTypes.Role, "Administrator"),
            new Claim("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture))
        };

        var identity = new ClaimsIdentity(claims, "DevelopmentCookie");
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            "DevelopmentCookie",
            principal,
            new AuthenticationProperties { IsPersistent = false });

        return Results.LocalRedirect(ContributionReturn.Consume(httpContext, "/catalogue/products"));
    });

}

app.Run();

public partial class Program { }
