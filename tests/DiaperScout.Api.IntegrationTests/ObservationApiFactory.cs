using DiaperScout.Infrastructure;
using DiaperScout.Commerce.Plugins.Awin;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DiaperScout.Infrastructure.Persistence;

namespace DiaperScout.Api.IntegrationTests;

public sealed class ObservationApiFactory(PostgreSqlFixture fixture) : WebApplicationFactory<Program>
{
    private readonly int providerFixtureId = Random.Shared.Next(1, int.MaxValue);
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:diaperscout"] = fixture.ConnectionString,
                ["Authentication:Development:Enabled"] = "true",
                ["Authentication:MagicLink:BaseUrl"] = "https://localhost:7167/signin/magic-link",
                ["Resend:FromEmail"] = "DiaperScout <test@example.test>",
                ["DevelopmentCatalogue:Enabled"] = "false",
                ["Geoapify:ApiKey"] = "synthetic-test-key",
                ["Editorial:CatalogueWritesEnabled"] = "true",
                ["RetailerDiscoveryJob:Enabled"] = "false",
                ["RetailerIdentityVerificationJob:Enabled"] = "false",
                ["RetailerAffiliateProgrammeDiscoveryJob:Enabled"] = "false"
            }));
        builder.ConfigureTestServices(services =>
        {
            services.Configure<AwinAffiliateProgrammeDiscoveryOptions>(o => o.PublisherId = "999");

            services.RemoveAll<DbContextOptions<DiaperScoutDbContext>>();
            services.RemoveAll<DiaperScoutDbContext>();
            services.AddDbContext<DiaperScoutDbContext>(options => options.UseNpgsql(fixture.ConnectionString));
            services.AddHttpClient<GeoapifyPlaces>().ConfigurePrimaryHttpMessageHandler(() => new GeoapifyPlacesTests.Handler(_ => "{\"features\":[" + GeoapifyPlacesTests.Feature(providerFixtureId) + "]}"));
        });
    }
}
