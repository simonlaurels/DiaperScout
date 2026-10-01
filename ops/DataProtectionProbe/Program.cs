using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text.Json;
var credential = new AzureCliCredential();
var blob = new Uri("https://dsprodwebkeys21a5de71.blob.core.windows.net/web-production/keys.xml");
var key = new Uri("https://ds-prod-webkeys-21a5de71.vault.azure.net/keys/web-production");
ServiceProvider Instance(string name)
{
    var services = new ServiceCollection().AddLogging();
    services.AddDataProtection().SetApplicationName(name).SetDefaultKeyLifetime(TimeSpan.FromDays(90))
        .PersistKeysToAzureBlobStorage(blob, credential).ProtectKeysWithAzureKeyVault(key, credential);
    return services.BuildServiceProvider();
}
string protectedValue;
using (var first = Instance("DiaperScout.Web.Production"))
    protectedValue = first.GetRequiredService<IDataProtectionProvider>().CreateProtector("cloud-regression").Protect("cross-instance value");
using var second = Instance("DiaperScout.Web.Production");
var protector = second.GetRequiredService<IDataProtectionProvider>().CreateProtector("cloud-regression");
if (protector.Unprotect(protectedValue) != "cross-instance value") throw new Exception("Cross-instance decoding failed");
using var isolated = Instance("DiaperScout.Web.Staging");
var isolation = false;
try { isolated.GetRequiredService<IDataProtectionProvider>().CreateProtector("cloud-regression").Unprotect(protectedValue); }
catch (CryptographicException) { isolation = true; }
if (!isolation) throw new Exception("Application isolation failed");
var elements = second.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository.GetAllElements();
var keys = elements.Where(x => x.Name.LocalName == "key").ToArray();
var encrypted = keys.All(x => x.Descendants().Any(n => n.Name.LocalName == "encryptedSecret") &&
    !x.Descendants().Any(n => n.Name.LocalName == "masterKey"));
if (keys.Length == 0 || !encrypted) throw new Exception("Persisted key encryption failed");
Console.WriteLine(JsonSerializer.Serialize(new { crossInstanceDecryption = true, applicationIsolation = isolation,
    encryptedAtRest = encrypted, keyIds = keys.Select(x => (string)x.Attribute("id")).ToArray() }));
