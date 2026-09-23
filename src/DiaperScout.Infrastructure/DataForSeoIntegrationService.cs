using DiaperScout.Application;

namespace DiaperScout.Infrastructure;

public sealed class DataForSeoIntegrationService(
    DataForSeoIntegrationStore store,
    DataForSeoConnectionTester tester) : IDataForSeoIntegration
{
    public Task<DataForSeoIntegrationSettings> GetAsync(CancellationToken cancellationToken = default) =>
        store.GetAsync(cancellationToken);

    public Task<DataForSeoIntegrationSettings> SaveAsync(
        UpdateDataForSeoIntegrationSettings request,
        CancellationToken cancellationToken = default) =>
        store.SaveAsync(request, cancellationToken);

    public Task<DataForSeoConnectionTestResult> TestConnectionAsync(
        CancellationToken cancellationToken = default) =>
        tester.TestAsync(cancellationToken);
}
