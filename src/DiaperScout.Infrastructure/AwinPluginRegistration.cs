using DiaperScout.Application;
using DiaperScout.Commerce.Plugins.Awin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiaperScout.Infrastructure;

// Composition root only: provider implementations and configuration are registered here, not in generic commerce code.
internal static class AwinPluginRegistration
{
    public static IServiceCollection AddInstalledCommercePlugins(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCommercePlugins(configuration);
        services.Configure<AwinAffiliateProgrammeDiscoveryOptions>(configuration.GetSection(AwinAffiliateProgrammeDiscoveryOptions.SectionName));
        services.AddHttpClient<AwinAffiliateProgrammeDiscoveryProvider>();
        services.AddCommercePlugin<AwinAffiliatePlugin>();
        return services;
    }
}
