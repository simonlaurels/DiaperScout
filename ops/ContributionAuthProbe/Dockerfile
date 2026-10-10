FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish ops/ContributionAuthProbe/Probe.csproj -c Release -o /app/probe
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/probe .
ENTRYPOINT ["dotnet", "Probe.dll"]
