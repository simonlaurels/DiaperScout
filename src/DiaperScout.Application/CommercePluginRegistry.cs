using System.Text.RegularExpressions;

namespace DiaperScout.Application;

public sealed class CommercePluginRegistry : ICommercePluginRegistry
{
    public CommercePluginRegistry(IEnumerable<ICommercePlugin> plugins)
    {
        var installed = plugins.ToArray();
        foreach (var plugin in installed)
        {
            var d = plugin.Descriptor;
            if (!Regex.IsMatch(d.Id, "^[a-z][a-z0-9.-]{1,59}$") || string.IsNullOrWhiteSpace(d.Name)
                || d.Name.Length > 100 || string.IsNullOrWhiteSpace(d.Version) || d.Version.Length > 40)
                throw new InvalidOperationException("Commerce plugin metadata is invalid.");
            var families = new List<CommercePluginFamily>();
            if (plugin is IRetailDiscoveryPlugin) families.Add(CommercePluginFamily.RetailDiscovery);
            if (plugin is IPricingPlugin) families.Add(CommercePluginFamily.Pricing);
            if (plugin is IAvailabilityPlugin) families.Add(CommercePluginFamily.Availability);
            if (plugin is IAffiliatePlugin) families.Add(CommercePluginFamily.Affiliate);
            if (families.Count != 1 || families[0] != d.Family)
                throw new InvalidOperationException("Each commerce plugin must implement exactly its declared family.");
        }
        if (installed.GroupBy(p => p.Descriptor.Id, StringComparer.Ordinal).Any(g => g.Count() > 1))
            throw new InvalidOperationException("Commerce plugin IDs must be unique.");
        Plugins = installed.OrderBy(p => p.Descriptor.Id, StringComparer.Ordinal).ToArray();
    }
    public IReadOnlyList<ICommercePlugin> Plugins { get; }
}
