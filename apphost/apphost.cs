#:sdk Aspire.AppHost.Sdk@13.4.6
#:package Aspire.Hosting.PostgreSQL@13.4.6

#pragma warning disable ASPIRECSHARPAPPS001

var builder = DistributedApplication.CreateBuilder(args);

// Local development only: keep PostgreSQL credentials and host port stable
// across machines. Production/deployment credentials are configured separately.
var postgresPassword = builder.AddParameter(
    "postgres-password",
    "postgres",
    secret: true);

var postgres = builder.AddPostgres(
        "postgres",
        password: postgresPassword,
        port: 5432)
    .WithDataVolume();

var database = postgres.AddDatabase("diaperscout");

var api = builder.AddCSharpApp(
    "api",
    "../src/DiaperScout.Api/DiaperScout.Api.csproj")
    .WithReference(database)
    .WaitFor(database);

var web = builder.AddCSharpApp(
    "web",
    "../src/DiaperScout.Web/DiaperScout.Web.csproj")
    .WithReference(api)
    .WithHttpsEndpoint()
    .WithEnvironment("LanTesting__Enabled", "false");

builder.Build().Run();
