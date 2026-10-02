using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace DiaperScout.Web.Services;

// ACA adds replica/revision metadata to these logs. Hash the random circuit ID
// so routing can be correlated without logging a reconnect identifier or user data.
public sealed class ReliabilityCircuitHandler(ILogger<ReliabilityCircuitHandler> logger) : CircuitHandler
{
    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation("Blazor circuit connection up {CircuitHash}", Fingerprint(circuit));
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation("Blazor circuit connection down {CircuitHash}", Fingerprint(circuit));
        return Task.CompletedTask;
    }

    private static string Fingerprint(Circuit circuit) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(circuit.Id)))[..12];
}
