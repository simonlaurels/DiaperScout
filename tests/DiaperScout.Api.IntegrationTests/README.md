# API integration tests

These tests use a disposable `postgres:18.3` container through Testcontainers.
Each test run starts with a clean database, applies the EF Core migrations, and
inserts only the synthetic manufacturer, catalogue, retail location, user,
Explorer profile, and Backpack fixture data needed for the observation API.

Run them with Docker Desktop running:

```powershell
dotnet test tests/DiaperScout.Api.IntegrationTests/DiaperScout.Api.IntegrationTests.csproj
```

The fixture deliberately does not connect to, alter, or require the Aspire
development database. It uses the same PostgreSQL 18.3 engine and the same
Infrastructure migrations and Npgsql provider, while preventing test state from
leaking into development data.

## Passkeys

The passkey browser test uses Playwright's Chromium and a virtual WebAuthn authenticator.
Install the browser once after building:

```powershell
dotnet build tests/DiaperScout.Api.IntegrationTests/DiaperScout.Api.IntegrationTests.csproj
pwsh tests/DiaperScout.Api.IntegrationTests/bin/Debug/net10.0/playwright.ps1 install chromium
dotnet test tests/DiaperScout.Api.IntegrationTests/DiaperScout.Api.IntegrationTests.csproj --filter FullyQualifiedName~Passkey
```

Set `DIAPERSCOUT_TEST_ARTIFACTS` to a local output directory to save the mobile passkey-page screenshot.
To run API tests without browser tests, use `--filter "Category!=Browser"`.
All accounts and credentials are synthetic and isolated from production.
