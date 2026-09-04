## Faza 4 — Dockerfile za aplikaciju, Hadolint, docker-compose integracija

### Šta sam radila

- Napravila `.dockerignore` da isključi nepotrebne/osjetljive fajlove iz Docker build konteksta
- Napravila multi-stage `Dockerfile` sa non-root korisnikom (sigurnosne best practice za kontejnerizaciju)
- Testirala samostalno izgrađen image (`docker build`, `docker run`) — potvrdila i objasnila zašto samostalan kontejner ne može pristupiti bazi (izolacija kontejnera, `host.docker.internal`)
- Pokrenula Hadolint (linter za Dockerfile) i ispravila jedan nalaz (DL3066)
- Integrisala aplikacijski kontejner sa postojećim PostgreSQL kontejnerom kroz zajednički `docker-compose.yml`, sa svim tajnama izmještenim u `.env`
- Potvrdila da oba kontejnera (app + db) rade zajedno, sa automatskom komunikacijom preko Docker interne mreže

### Komande koje sam koristila

```bash
# Build i samostalno testiranje
docker build -t bankdemo-app .
docker images
docker run -p 8080:8080 --rm bankdemo-app
# (očekivana greška konekcije - kontejner izolovan od Postgres kontejnera)

# Testiranje sa environment varijablama (privremeno, ručno)
docker run -p 8080:8080 --rm -e ConnectionStrings__Default="Host=host.docker.internal;Port=5432;Database=bankdemo;Username=bankuser;Password=***" -e Jwt__Key="***" -e Jwt__Issuer="BankDemo" -e Jwt__Audience="BankDemoUsers" bankdemo-app

# Hadolint (PowerShell ne podržava '<' redirekciju, korišten Get-Content pipe)
docker pull hadolint/hadolint
Get-Content Dockerfile | docker run --rm -i hadolint/hadolint

# Finalno pokretanje (oba servisa zajedno)
docker compose down
docker compose up -d --build
docker ps
```

### Dockerfile — finalna verzija

```dockerfile
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

RUN groupadd -r -g 1001 appgroup && useradd -r -u 1001 -g appgroup appuser

COPY --from=build /app/publish .

RUN chown -R appuser:appgroup /app

USER 1001

EXPOSE 8080
ENTRYPOINT ["dotnet", "BankDemo.dll"]
```

### .dockerignore

```
bin/
obj/
.vs/
Logs/
*.user
.git/
.gitignore
Dockerfile
.dockerignore
```

### Ključne sigurnosne odluke

1. **Multi-stage build** — build faza (`sdk:10.0`, sadrži pun SDK i alate za kompajliranje) potpuno odvojena od runtime faze (`aspnet:10.0`, samo ono što je potrebno za pokretanje). `COPY --from=build` prenosi isključivo gotov, izgrađen rezultat — finalni image nikad ne sadrži SDK alate. Smanjuje veličinu image-a i napadnu površinu.

2. **Non-root korisnik sa numeričkim UID-om.** Prvobitno kreiran imenovan korisnik (`appuser`) bez eksplicitnog UID-a; Hadolint nalaz **DL3066** ("Non-numeric user-id may not be resolvable by host system") ukazao da je preporučeno koristiti numerički UID radi bolje kompatibilnosti sa orkestracionim sistemima (npr. Kubernetes politike koje ograničavaju izvršavanje po numeričkom UID-u). Ispravljeno eksplicitnim dodjeljivanjem UID/GID 1001. Nakon ispravke, Hadolint provjera vraća prazan izlaz (nema nalaza).

3. **`.dockerignore`** isključuje build artefakte, IDE fajlove, `.git` istoriju, i sam `Dockerfile`/`.dockerignore` iz build konteksta — smanjuje veličinu konteksta i izbjegava kopiranje irelevantnih/potencijalno osjetljivih podataka u image.

4. **Environment varijable umjesto `user-secrets` u kontejnerizovanom okruženju.** Otkriveno testiranjem: `dotnet user-secrets` čuva podatke lokalno na razvojnoj mašini (izvan projekta), nedostupno unutar izolovanog Docker kontejnera. Rješenje — .NET automatski mapira environment varijable oblika `Sekcija__Kljuc` (dvostruka donja crta) na konfiguracijske vrijednosti (npr. `ConnectionStrings__Default`), bez izmjene bilo koje linije koda. Ovim je praktično demonstriran princip odvajanja konfiguracije od koda kroz različite faze životnog ciklusa (razvoj → kontejner), najavljen ranije u Fazi 1.

5. **Docker Compose interna mreža umjesto `host.docker.internal`.** Pri samostalnom testiranju jednog kontejnera, korištena je posebna adresa `host.docker.internal` (upućuje nazad na host mašinu, odakle se dalje pristupa mapiranom portu drugog kontejnera). U finalnoj, kombinovanoj `docker-compose.yml` konfiguraciji, oba servisa dijele zajedničku internu mrežu i komuniciraju direktno preko **imena servisa** (`Host=db`) — elegantnije i namjenski predviđeno rješenje za višekontejnerske sisteme.

6. **`ASPNETCORE_ENVIRONMENT=Development` eksplicitno postavljen u kontejneru** radi prikaza Swagger UI-ja tokom razvoja/testiranja — default vrijednost bez ove postavke je `Production`, gdje je Swagger namjerno onemogućen (sigurnosna praksa da se struktura API-ja ne izlaže javno u pravoj produkciji).

### Finalni docker-compose.yml (struktura)

- `db` servis: `postgres:16`, lozinka i ostali podaci iz `.env` preko `${POSTGRES_PASSWORD}`
- `app` servis: build iz lokalnog Dockerfile-a (`context: ./BankDemo`), sve konfiguracijske vrijednosti (connection string, JWT ključ, seed admin kredencijali) prosljeđene kao environment varijable iz `.env`
- `depends_on` osigurava redoslijed pokretanja (napomena: garantuje samo redoslijed pokretanja kontejnera, ne i da je Postgres već spreman za konekcije — potencijalno unaprjeđenje uz `healthcheck`, navedeno kao mogućnost za dalji rad)
- Zajednički named volume (`pgdata`) za perzistenciju baze

### Hadolint nalaz i ispravka

| Kod | Opis | Status |
|---|---|---|
| DL3066 | Non-numeric user-id may not be resolvable by host system | Ispravljeno — korišten eksplicitan numerički UID/GID (1001) |

### Testiranje i verifikacija

- [x] `docker build` uspješno izgrađuje image
- [x] Samostalan kontejner pokrenut, greška konekcije na bazu potvrđena i objašnjena (očekivano ponašanje usljed izolacije kontejnera)
- [x] Konekcija uspješno uspostavljena kroz `host.docker.internal` uz eksplicitne environment varijable (privremeni test)
- [x] Hadolint provjera bez nalaza nakon ispravke UID-a
- [x] Oba kontejnera (`bank-demo-app-1`, `bank-demo-db-1`) rade istovremeno, status "Up"
- [x] Aplikacija dostupna i funkcionalna na `http://localhost:8080/swagger`, uspješno povezana na bazu kroz `Host=db`

### Problemi na koje sam naišla i kako sam ih riješila

- **Samostalan kontejner nije mogao da se poveže na bazu** ("ConnectionString property has not been initialized", zatim greška konekcije) — očekivano i objašnjeno: `user-secrets` nisu dostupne unutar kontejnera; riješeno prosljeđivanjem konfiguracije kroz environment varijable.
- **PowerShell sintaksna razlika pri prosljeđivanju višelinijskih Docker komandi** — `^` (CMD nastavak linije) ne radi u PowerShell-u, koji koristi obrnutu kosu crtu (`` ` ``) ili se preporučuje pisanje u jednom redu radi izbjegavanja grešaka.
- **Hadolint `<` redirekcija ulaza nije podržana u PowerShell-u** ("The '<' operator is reserved for future use") — riješeno korištenjem `Get-Content Dockerfile | docker run --rm -i hadolint/hadolint` (pipe umjesto redirekcije).
- **Swagger UI nije bio dostupan nakon prvog pokretanja kroz Docker Compose** — uzrok: `Hosting environment: Production` po defaultu unutar kontejnera (Swagger blok u `Program.cs` uslovljen sa `IsDevelopment()`); riješeno eksplicitnim dodavanjem `ASPNETCORE_ENVIRONMENT: Development` u `docker-compose.yml`.

### Sljedeći korak

Faza 5 — lokalno testiranje SAST/SCA alata (Semgrep, Security Code Scan, OWASP Dependency-Check) na `vulnerable-baseline` grani (dokumentovano odvojeno).
