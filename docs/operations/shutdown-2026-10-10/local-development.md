# Local development without production Azure

The standard entry point is `apphost/AppHost.cs` (.NET10, Aspire13.4.6). It creates local PostgreSQL on5432 with a persistent local volume, references local API/web and uses localhost service discovery. It does not reference the production PostgreSQL server. The password `postgres` in the launcher is local-only.

1. Install .NET10/Aspire prerequisites, run Docker Desktop, and trust the local ASP.NET development HTTPS certificate (`dotnet dev-certs https --trust`). Do not overwrite an existing local PostgreSQL service/volume on5432; use a dedicated local port override if it is occupied.
2. Start `dotnet run apphost/AppHost.cs`. Supply `resend-api-token` via the Aspire secret prompt/configuration if exercising real mail. Never paste the token into Git. The launcher requests this parameter even though catalogue/development-header browsing does not need production Azure. A dummy token is adequate only for a local non-mail path; real mail delivery will fail.
3. API Development settings enable development-header authentication, catalogue writes and `DevelopmentCatalogue`. Startup invokes local migrations/seeding. Web Development uses local Data Protection when the three `DataProtection` production settings are absent. Do not copy Azure Blob/Key Vault settings, production connection strings, internal secrets or production URLs into local configuration.
4. Confirm development passkey RpId/origin match the actual localhost browser URL; defaults include `https://localhost:7167` and `http://localhost:5230`. The Aspire magic-link base URL is `https://web-diaperscout.dev.localhost:7167/signin/magic-link`; check hostname/certificate/origin before testing email/passkeys. A direct localhost launch may be simpler for passkey testing.
5. Development images have a local disk storage path (`CatalogueImages:RootPath` or output `catalogue-images`). R2 is an optional configured image provider; avoid production R2 credentials when testing writes. Geoapify live new-place search needs its retained external key/network; mocked place discovery is used in tests. Maps/CDN resources may still need internet. Resend mail, Geoapify and optional affiliate APIs are external dependencies, not Azure-production dependencies.

Repeatable isolated verification (Docker running):

```powershell
dotnet build DiaperScout.slnx -c Release
pwsh tests/DiaperScout.Api.IntegrationTests/bin/Release/net10.0/playwright.ps1 install chromium webkit
dotnet test tests/DiaperScout.Domain.Tests/DiaperScout.Domain.Tests.csproj -c Release --no-build
dotnet test tests/DiaperScout.Api.IntegrationTests/DiaperScout.Api.IntegrationTests.csproj -c Release --no-build
```

Integration fixtures create disposable `postgres:18.3`, migrate/seed synthetic data and replace provider HTTP calls. They deliberately do not use Aspire's persistent development database or production Azure. Two-worker xUnit configuration protects laptop memory. Do not prune images/volumes or stop unrelated test hosts.

10 October verification: 76/76 tests passed in1m59s using exact validated Release binaries; filter `FullyQualifiedName~WebDataProtectionTests|FullyQualifiedName~Passkey|FullyQualifiedName~CatalogueSubmission`. Receipt `local-validation.log`. Prior exact-head full suite passed361/361 twice and domain67/67. This verifies the selected local hosts/browser/database paths; it does not claim a fresh interactive Aspire walkthrough or real external email/provider delivery.
