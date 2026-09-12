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

# VULN: Uklonjeno kreiranje ne-root korisnika i USER instrukcija — kontejner se izvršava kao root
COPY --from=build /app/publish .
ENV DB_PASSWORD="test-password-DO-NOT-USE-IN-PRODUCTION"

EXPOSE 8080
ENTRYPOINT ["dotnet", "BankDemo.dll"]