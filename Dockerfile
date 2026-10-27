
# Compilar o backend C#
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/DailyXPCore.Api.csproj backend/
RUN dotnet restore backend/DailyXPCore.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/DailyXPCore.Api.csproj -c Release -o /app/publish --no-restore

# Executar a API
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "DailyXPCore.Api.dll"]
