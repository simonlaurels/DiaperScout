#:sdk Aspire.AppHost.Sdk@13.4.6

#pragma warning disable ASPIRECSHARPAPPS001

var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddCSharpApp(
    "api",
    "../src/DiaperScout.Api/DiaperScout.Api.csproj");

var web = builder.AddCSharpApp(
    "web",
    "../src/DiaperScout.Web/DiaperScout.Web.csproj")
    .WithReference(api);

builder.Build().Run();