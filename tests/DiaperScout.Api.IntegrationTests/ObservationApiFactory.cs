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
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:diaperscout"] = fixture.ConnectionString,
                ["Authentication:Development:Enabled"] = "true",
                ["DevelopmentCatalogue:Enabled"] = "false",
                ["Editorial:CatalogueWritesEnabled"] = "true"
            }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<DiaperScoutDbContext>>();
            services.RemoveAll<DiaperScoutDbContext>();
            services.AddDbContext<DiaperScoutDbContext>(options => options.UseNpgsql(fixture.ConnectionString));
        });
    }
}
