namespace DiaperScout.Infrastructure.Persistence;

public sealed class DataForSeoIntegrationSetting
{
    public Guid Id { get; set; }
    public string ProviderKey { get; set; } = "DataForSEO";
    public bool Enabled { get; set; }
    public string Login { get; set; } = string.Empty;
    public string? ProtectedPassword { get; set; }
    public string LocationName { get; set; } = "United Kingdom";
    public string LanguageCode { get; set; } = "en";
    public string SearchDomain { get; set; } = "google.co.uk";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
