# ===== FAZA GRADNJE (build stage) =====
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

# ===== FAZA POKRETANJA (runtime stage) =====
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Instaliraj curl da bi HEALTHCHECK mogao provjeriti dostupnost aplikacije
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Kreiraj ne-root korisnika i grupu
RUN groupadd -r -g 1001 appgroup && useradd -r -u 1001 -g appgroup appuser

COPY --from=build /app/publish .

# Postavi vlasništvo fajlova na novog korisnika
RUN chown -R appuser:appgroup /app

# Promijeni aktivnog korisnika sa root na appuser
USER 1001

EXPOSE 8080

# Provjerava svakih 30s da li aplikacija odgovara; ako ne odgovori 3 puta zaredom, kontejner se označava kao "unhealthy"
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/swagger/index.html || exit 1

ENTRYPOINT ["dotnet", "BankDemo.dll"]