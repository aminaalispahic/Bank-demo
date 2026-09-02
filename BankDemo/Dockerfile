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

# Kreiraj ne-root korisnika i grupu
RUN groupadd -r -g 1001 appgroup && useradd -r -u 1001 -g appgroup appuser

COPY --from=build /app/publish .

# Postavi vlasništvo fajlova na novog korisnika
RUN chown -R appuser:appgroup /app

# Promijeni aktivnog korisnika sa root na appuser
USER 1001

EXPOSE 8080
ENTRYPOINT ["dotnet", "BankDemo.dll"]