#:sdk Aspire.AppHost.Sdk@13.4.6
#:package Aspire.Hosting.PostgreSQL@13.4.6

#pragma warning disable ASPIRECSHARPAPPS001

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var database = postgres.AddDatabase("diaperscout");

var api = builder.AddCSharpApp(
    "api",
    "../src/DiaperScout.Api/DiaperScout.Api.csproj")
    .WithReference(database);

var web = builder.AddCSharpApp(
    "web",
    "../src/DiaperScout.Web/DiaperScout.Web.csproj")
    .WithReference(api)
    .WithHttpsEndpoint()
    .WithEnvironment("LanTesting__Enabled", "false");

builder.Build().Run();
