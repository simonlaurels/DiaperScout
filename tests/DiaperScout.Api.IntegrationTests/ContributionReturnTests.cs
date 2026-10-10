extern alias DiaperScoutWeb;
using ContributionReturn = DiaperScoutWeb::DiaperScout.Web.Services.ContributionReturn;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DiaperScout.Api.IntegrationTests;
public sealed class ContributionReturnTests
{
    [Theory]
    [InlineData("https://evil.example/observations/new")]
    [InlineData("//evil.example/observations/new")]
    [InlineData("/observations/new\\evil")]
    [InlineData("/admin")]
    [InlineData("/observations/new\n")]
    public void Return_context_rejects_external_or_unrelated_destinations(string target) => Assert.Null(ContributionReturn.Validate(target));

    [Fact]
    public void Return_context_is_time_limited_protected_and_tampering_fails_closed()
    {
        var provider = new EphemeralDataProtectionProvider();
        using var services = new ServiceCollection().AddSingleton<IDataProtectionProvider>(provider).BuildServiceProvider();
        var target = "/observations/new?packTypeId=" + Guid.NewGuid();
        var protector = provider.CreateProtector("ContributionReturn.v1").ToTimeLimitedDataProtector();
        string Consume(string value) { var context = new DefaultHttpContext { RequestServices = services }; context.Request.Headers.Cookie = "DiaperScout.ContributionReturn=" + value; return ContributionReturn.Consume(context); }
        var token = protector.Protect(target, TimeSpan.FromMinutes(30));
        Assert.Equal(target, Consume(token));
        Assert.Equal("/", Consume(token + "tampered"));
        Assert.Equal("/", Consume(protector.Protect(target, DateTimeOffset.UtcNow.AddSeconds(-1))));
        Assert.Equal("/", Consume(protector.Protect("https://evil.example", TimeSpan.FromMinutes(30))));
        Assert.Equal("/", Consume(provider.CreateProtector("AnotherApplication").Protect(target)));
    }
}
