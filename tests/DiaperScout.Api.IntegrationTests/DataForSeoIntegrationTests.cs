using DiaperScout.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;

public sealed class DataForSeoIntegrationTests
{
    [Fact]
    public void RuntimeSettings_UseConfiguredDefaults()
    {
        var store = new DataForSeoRuntimeSettingsStore(
            Options.Create(new DataForSeoOptions
            {
                Enabled = true,
                Login = "login",
                Password = "password",
                LocationName = "United Kingdom",
                LanguageCode = "en",
                SearchDomain = "google.co.uk"
            }));

        Assert.True(store.Current.Enabled);
        Assert.Equal("login", store.Current.Login);
        Assert.Equal("password", store.Current.Password);
        Assert.Equal("google.co.uk", store.Current.SearchDomain);
    }

    [Fact]
    public void RuntimeSettings_UpdateImmediately()
    {
        var store = new DataForSeoRuntimeSettingsStore(
            Options.Create(new DataForSeoOptions()));

        store.Update(new(
            true,
            "admin@diaperscout.app",
            "secret",
            "United Kingdom",
            "en",
            "google.co.uk"));

        Assert.True(store.Current.Enabled);
        Assert.Equal("admin@diaperscout.app", store.Current.Login);
        Assert.Equal("secret", store.Current.Password);
    }

    [Fact]
    public async Task ConnectionTester_ReturnsNotConfiguredWithoutCredentials()
    {
        var store = new DataForSeoRuntimeSettingsStore(
            Options.Create(new DataForSeoOptions()));

        var tester = new DataForSeoConnectionTester(store);

        var result = await tester.TestAsync();

        Assert.False(result.Success);
        Assert.Equal("DataForSEO credentials are not configured.", result.Message);
    }

    [Fact]
    public void RuntimeSettings_KeepMarketDefaults()
    {
        var store = new DataForSeoRuntimeSettingsStore(
            Options.Create(new DataForSeoOptions()));

        Assert.Equal("United Kingdom", store.Current.LocationName);
        Assert.Equal("en", store.Current.LanguageCode);
        Assert.Equal("google.co.uk", store.Current.SearchDomain);
    }
}
