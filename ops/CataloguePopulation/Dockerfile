FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish ops/CataloguePopulation/CataloguePopulation.csproj -c Release -o /app/population
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/population .
COPY --from=build /src/ops/CataloguePopulation/pilot-publication.json /app/pilot-publication.json
ENTRYPOINT ["dotnet", "CataloguePopulation.dll"]
