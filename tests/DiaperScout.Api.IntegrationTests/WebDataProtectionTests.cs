extern alias DiaperScoutWeb;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using WebKeys = DiaperScoutWeb::DiaperScout.Web.Services.WebDataProtection;

public class WebDataProtectionTests
{
    private static Dictionary<string, string?> Settings => new()
    {
        ["DataProtection:ApplicationName"] = "DiaperScout.Web.Production",
        ["DataProtection:BlobUri"] = "https://example.blob.core.windows.net/web-production/keys.xml",
        ["DataProtection:KeyVaultKeyUri"] = "https://example.vault.azure.net/keys/web-production"
    };

    [Fact]
    public void Production_requires_explicit_shared_protected_storage()
    {
        Assert.Throws<InvalidOperationException>(() => WebKeys.AddWebDataProtection(new ServiceCollection(),
            new ConfigurationBuilder().Build(), new EnvironmentStub("Production")));
    }

    [Theory]
    [InlineData("ApplicationName", "DiaperScout.Web.Development")]
    [InlineData("BlobUri", "http://example.blob.core.windows.net/container/keys.xml")]
    [InlineData("BlobUri", "https://example.blob.core.windows.net/container/keys.xml?sig=secret")]
    [InlineData("BlobUri", "https://example.blob.core.windows.net/container")]
    [InlineData("KeyVaultKeyUri", "https://example.vault.azure.net/keys/web/version")]
    [InlineData("KeyVaultKeyUri", "https://example.vault.azure.net/secrets/web")]
    public void Invalid_or_cross_environment_configuration_is_rejected(string setting, string value)
    {
        var settings = Settings;
        settings["DataProtection:" + setting] = value;
        Assert.Throws<InvalidOperationException>(() => WebKeys.AddWebDataProtection(new ServiceCollection(),
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), new EnvironmentStub("Production")));
    }

    [Fact]
    public void Production_binds_both_Azure_storage_and_encryption_providers()
    {
        var services = new ServiceCollection().AddLogging();
        WebKeys.AddWebDataProtection(services, new ConfigurationBuilder().AddInMemoryCollection(Settings).Build(), new EnvironmentStub("Production"));
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value;
        Assert.Contains("Azure", options.XmlRepository!.GetType().FullName);
        Assert.Contains("KeyVault", options.XmlEncryptor!.GetType().FullName);
        Assert.Equal("DiaperScout.Web.Production", provider.GetRequiredService<IOptions<DataProtectionOptions>>().Value.ApplicationDiscriminator);
    }

    [Fact]
    public void Protected_shared_ring_survives_restart_and_enforces_application_and_purpose_isolation()
    {
        var directory = Directory.CreateTempSubdirectory("ds-shared-keys-");
        using var rsa = RSA.Create(2048);
        using var certificate = new CertificateRequest("CN=DataProtection regression", rsa, HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1).CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        try
        {
            IDataProtectionProvider Instance(string app) => DataProtectionProvider.Create(directory,
                builder => builder.SetApplicationName(app).ProtectKeysWithCertificate(certificate));
            var payload = Instance("DiaperScout.Web.Production").CreateProtector("circuit-test").Protect("cross-replica payload");
            Assert.Equal("cross-replica payload", Instance("DiaperScout.Web.Production").CreateProtector("circuit-test").Unprotect(payload));
            Assert.Throws<CryptographicException>(() => Instance("DiaperScout.Web.Staging").CreateProtector("circuit-test").Unprotect(payload));
            Assert.Throws<CryptographicException>(() => Instance("DiaperScout.Web.Production").CreateProtector("other-purpose").Unprotect(payload));
            var xml = File.ReadAllText(Directory.GetFiles(directory.FullName, "key-*.xml").Single());
            Assert.Contains("encryptedSecret", xml);
            Assert.DoesNotContain("<masterKey", xml);
        }
        finally { directory.Delete(true); }
    }

    private sealed class EnvironmentStub(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "DiaperScout.Web";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
