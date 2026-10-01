using System.Collections.Concurrent;
using DiaperScout.Application;
using DiaperScout.Domain;
using DiaperScout.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiaperScout.Infrastructure;

public sealed class CommercePluginOptions
{
    public const string SectionName = "CommercePlugins";
    public Dictionary<string, CommercePluginOperationalOptions> Plugins { get; set; } = [];
}
public sealed class CommercePluginOperationalOptions
{
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; } = 100;
    public double TimeoutSeconds { get; set; } = 10;
}

public static class CommercePluginRegistration
{
    public static IServiceCollection AddCommercePlugin<T>(this IServiceCollection services) where T : class, ICommercePlugin
    {
        // Each implementation is registered once under the common registry contract, with its own scoped lifecycle.
        services.TryAddScoped<T>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ICommercePlugin, T>());
        return services;
    }
    public static IServiceCollection AddCommercePlugins(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CommercePluginOptions>(configuration.GetSection(CommercePluginOptions.SectionName));
        services.AddScoped<ICommercePluginRegistry, CommercePluginRegistry>();
        services.AddScoped<ICommercePluginRuntime, CommercePluginRuntime>();
        services.AddScoped<ICommercePluginOrchestrator, CommercePluginOrchestrator>();
        services.AddScoped<ICommercePluginManagement, CommercePluginManagement>();
        services.AddSingleton<CommercePluginHealth>();
        services.AddScoped<ICanonicalCommerce, CanonicalCommerce>();
        services.AddScoped<IRetailerAffiliateProgrammeDiscovery, RetailerAffiliateProgrammeDiscovery>();
        return services;
    }
}

internal sealed class CommercePluginHealth(ILogger<CommercePluginHealth> logger)
{
    internal sealed record History(DateTimeOffset? Success, DateTimeOffset? Failure, string? FailureCode);
    private readonly ConcurrentDictionary<string, History> history = new(StringComparer.Ordinal);
    public History Get(string id) => history.GetValueOrDefault(id) ?? new(null, null, null);
    public void Record(CommercePluginExecution execution)
    {
        if (execution.Status == CommercePluginExecutionStatus.Succeeded)
            history.AddOrUpdate(execution.PluginId, new History(execution.CompletedAtUtc, null, null),
                (_, previous) => previous with { Success = Max(previous.Success, execution.CompletedAtUtc) });
        else if (execution.Status is CommercePluginExecutionStatus.Failed or CommercePluginExecutionStatus.TimedOut or CommercePluginExecutionStatus.InvalidResult)
        {
            history.AddOrUpdate(execution.PluginId, new History(null, execution.CompletedAtUtc, execution.Status.ToString()),
                (_, previous) => previous.Failure > execution.CompletedAtUtc ? previous
                    : previous with { Failure = execution.CompletedAtUtc, FailureCode = execution.Status.ToString() });
            logger.LogWarning("Commerce plugin {PluginId} completed with {Status}.", execution.PluginId, execution.Status);
        }
    }
    private static DateTimeOffset Max(DateTimeOffset? current, DateTimeOffset incoming) => current > incoming ? current.Value : incoming;
}

internal sealed class CommercePluginRuntime(DiaperScoutDbContext db, IOptions<CommercePluginOptions> options,
    CommercePluginHealth health) : ICommercePluginRuntime
{
    internal static string EnabledKey(string id) => $"commerce.plugin.{id}.enabled";
    private readonly Dictionary<string, CommercePluginPolicy> policies = new(StringComparer.Ordinal);
    public async Task<CommercePluginPolicy> GetPolicyAsync(string pluginId, CancellationToken cancellationToken)
    {
        if (policies.TryGetValue(pluginId, out var cached)) return cached;
        var settings = options.Value.Plugins.GetValueOrDefault(pluginId) ?? new();
        var persisted = await db.PlatformSettings.AsNoTracking().Where(s => s.Key == EnabledKey(pluginId))
            .Select(s => s.Value).SingleOrDefaultAsync(cancellationToken);
        // Deployment configuration can disable a plugin globally; an operator toggle cannot bypass that restriction.
        var enabled = settings.Enabled && (persisted is null || (bool.TryParse(persisted, out var flag) && flag));
        var timeout = double.IsFinite(settings.TimeoutSeconds) ? Math.Clamp(settings.TimeoutSeconds, 0.01, 120) : 10;
        var policy = new CommercePluginPolicy(enabled, settings.Priority, TimeSpan.FromSeconds(timeout));
        policies[pluginId] = policy;
        return policy;
    }
    public void Record(CommercePluginExecution execution) => health.Record(execution);
}

internal sealed class CommercePluginManagement(DiaperScoutDbContext db, ICommercePluginRegistry registry,
    ICommercePluginRuntime runtime, CommercePluginHealth health, IEditorialAuthorisation authorisation,
    IOptions<CommercePluginOptions> options, TimeProvider clock) : ICommercePluginManagement
{
    public async Task<IReadOnlyList<CommercePluginStatus>> GetAsync(AuthenticatedUser actor, CancellationToken cancellationToken = default)
    {
        await RequireAsync(actor, cancellationToken);
        var result = new List<CommercePluginStatus>();
        foreach (var plugin in registry.Plugins)
            result.Add(Status(plugin, (await runtime.GetPolicyAsync(plugin.Descriptor.Id, cancellationToken)).Enabled));
        return result;
    }
    public async Task<CommercePluginStatus> SetEnabledAsync(AuthenticatedUser actor, string pluginId, bool enabled, CancellationToken cancellationToken = default)
    {
        await RequireAsync(actor, cancellationToken);
        var plugin = registry.Plugins.SingleOrDefault(p => p.Descriptor.Id == pluginId) ?? throw new KeyNotFoundException();
        if (enabled && options.Value.Plugins.TryGetValue(pluginId, out var policy) && !policy.Enabled)
            throw new CatalogueValidationException("enabled", "This integration is disabled by deployment configuration.");
        var key = CommercePluginRuntime.EnabledKey(pluginId);
        var setting = await db.PlatformSettings.SingleOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (setting is null) db.PlatformSettings.Add(new PlatformSetting(key, enabled.ToString(), clock.GetUtcNow()));
        else setting.UpdateValue(enabled.ToString(), clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return Status(plugin, enabled);
    }
    private CommercePluginStatus Status(ICommercePlugin plugin, bool enabled)
    {
        var h = health.Get(plugin.Descriptor.Id);
        CommercePluginConfiguration configuration;
        try { configuration = plugin.Configuration; }
        catch { configuration = CommercePluginConfiguration.MissingConfiguration; }
        var status = !enabled ? "Disabled" : configuration != CommercePluginConfiguration.Ready ? "Unavailable"
            : h.Failure.HasValue && (!h.Success.HasValue || h.Failure > h.Success) ? "Degraded"
            : h.Success.HasValue ? "Healthy" : "Not run";
        return new(plugin.Descriptor, enabled, configuration, status, h.Success, h.Failure, h.FailureCode);
    }
    private async Task RequireAsync(AuthenticatedUser actor, CancellationToken ct)
    {
        if (!await authorisation.CanManageCatalogueAsync(actor, ct)) throw new UnauthorizedAccessException();
    }
}
