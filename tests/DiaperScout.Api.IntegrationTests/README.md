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
