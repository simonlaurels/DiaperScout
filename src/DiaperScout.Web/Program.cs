using DiaperScout.Web.Components;
using DiaperScout.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCascadingAuthenticationState();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddServiceDiscovery();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<DevelopmentSubjectForwardingHandler>();
builder.Services.AddHttpClient<ProductLookupClient>(client => client.BaseAddress = new Uri("https+http://api"))
    .AddServiceDiscovery();
builder.Services.AddHttpClient<ProductCatalogueClient>(client => client.BaseAddress = new Uri("https+http://api"))
    .AddHttpMessageHandler<DevelopmentSubjectForwardingHandler>()
    .AddServiceDiscovery();
builder.Services.AddHttpClient<CatalogueAffiliateClient>(client => client.BaseAddress = new Uri("https+http://api"))
    .AddHttpMessageHandler<DevelopmentSubjectForwardingHandler>()
    .AddServiceDiscovery();

var app = builder.Build();

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
