extern alias DiaperScoutWeb;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiaperScout.Api.IntegrationTests;

internal sealed class PasskeyWebFactory(WebApplicationFactory<Program> api) : WebApplicationFactory<DiaperScoutWeb::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Api:BaseUrl"] = "http://api.test",
            ["LanTesting:Enabled"] = "false"
        }));
        builder.ConfigureTestServices(services =>
        {
            foreach (var name in new[] { "PasskeyApi", "DiaperScoutApi" })
                services.AddHttpClient(name).ConfigurePrimaryHttpMessageHandler(() => api.Server.CreateHandler());
            services.AddHttpClient<DiaperScoutWeb::DiaperScout.Web.Services.UserManagementClient>()
                .ConfigurePrimaryHttpMessageHandler(() => api.Server.CreateHandler());
            services.AddHttpClient<DiaperScoutWeb::DiaperScout.Web.Services.ProductCatalogueClient>()
                .ConfigurePrimaryHttpMessageHandler(() => api.Server.CreateHandler());
        });
    }
}
