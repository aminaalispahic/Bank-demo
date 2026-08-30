# Dnevnik rada — Završni rad: DevOps i sigurnost: osnovni princip DevSecOps-a

## Faza 1 — Skeleton aplikacije, PostgreSQL, autentifikacija i RBAC

### Šta sam radila

- Kreirala ASP.NET Core Web API projekat (.NET 10) sa controller-based strukturom
- Postavila PostgreSQL 16 bazu kroz Docker kontejner (docker-compose), umjesto originalno planirane SQLite baze
- Napravila modele podataka (User, Account, Transaction) i enum UserRole (Client, Referent, Admin)
- Implementirala clean/slojevitu arhitekturu: Controller → Service → Repository, sa odvojenim DTO i Interface slojevima
- Implementirala JWT autentifikaciju (generisanje i validacija tokena) i BCrypt hashing lozinki
- Implementirala register/login, upravljanje računima (kreiranje, pregled, transfer), i historiju transakcija
- Implementirala RBAC (role-based access control) kroz tri role, uključujući seed mehanizam za prvi Admin nalog
- Dodala rate limiting na login endpoint i osnovne sigurnosne HTTP headere
- Testirala kompletan tok kroz Swagger i Postman

### Komande koje sam koristila

```bash
dotnet new webapi -n BankDemo -controllers
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package BCrypt.Net-Next
dotnet add package Swashbuckle.AspNetCore

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=bankdemo;Username=bankuser;Password=***"
dotnet user-secrets set "Jwt:Key" "***"
dotnet user-secrets set "Jwt:Issuer" "BankDemo"
dotnet user-secrets set "Jwt:Audience" "BankDemoUsers"
dotnet user-secrets set "AdminSeed:Username" "admin"
dotnet user-secrets set "AdminSeed:Password" "***"

docker compose up -d

dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update

dotnet dev-certs https --trust
```

### Arhitektura i tehnologije

- **Backend:** ASP.NET Core Web API, .NET 10
- **Baza:** PostgreSQL 16, pokrenuta u Docker kontejneru (docker-compose)
- **ORM:** Entity Framework Core, sa Npgsql provajderom
- **Autentifikacija:** JWT (HMAC-SHA256, simetrično potpisivanje), BCrypt za hash lozinki
- **API dokumentacija:** Swagger UI (Swashbuckle.AspNetCore), sa konfigurisanom Bearer autorizacijom
- **Arhitekturni obrazac:** Controller → Service → Repository, sa odvojenim slojevima za DTO, Interfaces, Enums, Models

Struktura projekta:
```
BankDemo/
├── Models/          (User, Account, Transaction)
├── Enums/           (UserRole)
├── DTOs/            (RegisterRequestDto, LoginRequestDto, LoginResponseDto,
│                     AccountDto, TransferRequestDto, TransferResult,
│                     CreateAccountRequestDto, CreateStaffRequestDto,
│                     TransactionDto, RegisterResult)
├── Interfaces/       (ITokenService, IUserRepository, IAuthService,
│                     IAccountRepository, ITransactionRepository,
│                     IAccountService, ITransactionService)
├── Repositories/    (UserRepository, AccountRepository, TransactionRepository)
├── Services/        (TokenService, AuthService, AccountService, TransactionService)
├── Data/            (AppDbContext)
└── Controllers/     (AuthController, AccountsController, TransactionsController)
```

### Ključne sigurnosne odluke (security-by-design)

1. **Zamjena SQLite → PostgreSQL kroz Docker.** Odluka donesena radi realnije demonstracije principa upravljanja tajnama — PostgreSQL zahtijeva stvarne pristupne podatke (korisničko ime, lozinka), za razliku od SQLite fajla koji nema ekvivalentnu potrebu za autentifikacijom. Napomena: originalni prijedlog poslan mentorici je naveo SQLite; ova izmjena je manja i biće joj naknadno saopštena.

2. **Upravljanje tajnama.** Sve osjetljive vrijednosti (connection string, JWT ključ, seed admin kredencijali) čuvaju se lokalno kroz `dotnet user-secrets`, van izvornog koda i van Git repozitorija. U kasnijim fazama (kontejnerizacija, CI/CD), iste vrijednosti će biti ubačene kroz environment varijable — princip odvajanja konfiguracije od koda (tzv. "twelve-factor app" pristup).

3. **Hashing lozinki.** BCrypt korišten umjesto čuvanja lozinki u čistom tekstu ili slabijih hash funkcija (npr. MD5/SHA1 bez soljenja).

4. **JWT sa HMAC-SHA256 (HS256).** Odabran simetrični algoritam jer aplikacija sama i izdaje i provjerava tokene — nema potrebe za asimetričnim RSA pristupom koji bi bio opravdan tek u sistemu sa više nezavisnih servisa.

5. **Ista poruka greške za nepostojeći korisnik i pogrešnu lozinku** pri loginu — sprječava tzv. user enumeration napad (napadač ne može zaključiti koja korisnička imena postoje u sistemu).

6. **IDOR (Insecure Direct Object Reference) zaštita kod transfera.** `TransferRequestDto` namjerno ne sadrži `FromAccountId` polje — izvorni račun se izvodi isključivo iz autentifikovanog korisnika (JWT token), ne iz korisničkog unosa. Servisni sloj eksplicitno provjerava vlasništvo nad računom (`fromAccount.UserId != userId`) prije izvršavanja transfera.

7. **Zaštita od Mass Assignment ranjivosti.** `RegisterRequestDto` (javna registracija) nema polje za rolu — rola se hardkoduje na `Client` na serverskoj strani. Za razliku od toga, `CreateStaffRequestDto` (kreiranje Referent/Admin naloga) prihvata rolu kao ulazni parametar, ali je ta ranjivost eliminisana time što je endpoint zaštićen sa `[Authorize(Roles="Admin")]` — rola dolazi od korisnika, ali samo od već verifikovanog Admin korisnika.

8. **Database seeding za prvi Admin nalog.** Rješava "kokoška-jaje" problem (ko kreira prvog Admina, ako samo Admin smije kreirati Admine) — pri prvom pokretanju aplikacije, ako ne postoji nijedan Admin, kreira se jedan iz kredencijala definisanih u `user-secrets`/environment varijablama.

9. **RBAC (role-based access control) kroz tri role.** Endpoint za kreiranje računa ograničen na `Referent`/`Admin` role — obični klijent ne može sam sebi otvarati bankovne račune, što odgovara realnom bankarskom procesu.

10. **Validacija poslovnih pravila kod transfera.** Provjera vlasništva → provjera postojanja odredišnog računa → provjera pozitivnog iznosa → provjera dovoljnog stanja, u tom redoslijedu (svaka provjera "brani" sljedeću).

11. **Rate limiting na login endpoint.** Maksimalno 5 pokušaja prijave po minuti (fixed window algoritam) — konkretna odbrana od brute-force napada na lozinke. Vraća HTTP 429.

12. **Osnovni sigurnosni HTTP headeri** na svaki odgovor: `X-Content-Type-Options: nosniff` (sprječava MIME-sniffing), `X-Frame-Options: DENY` (sprječava clickjacking), `Content-Security-Policy: default-src 'self'` (ograničava izvore resursa).

13. **DTO-ovi umjesto direktnog izlaganja modela.** Nijedan endpoint ne vraća domain model direktno (npr. `User` sa `PasswordHash` poljem) — svi odgovori idu kroz namjenske DTO objekte, sprječavajući Excessive Data Exposure.

### Problemi na koje sam naišla i kako sam ih riješila

- **Konflikt verzija NuGet paketa** (`Microsoft.OpenApi` vs. Swashbuckle) pri implementaciji Swagger JWT autorizacije — riješeno čišćenjem NuGet keša i prilagođavanjem koda novijoj strukturi API-ja (`OpenApiSecuritySchemeReference` umjesto starijeg `Reference`/`OpenApiReference` pristupa).
- **Zaključani fajlovi pri build-u** ("file is locked by another process") — uzrokovano zaostalim `dotnet run` procesima; riješeno gašenjem procesa (Task Manager) i restartom sistema.
- **Connection string greška pri EF migraciji** — uzrokovana greškom pri ručnom upisu u `user-secrets` (nedostajao znak `=`); riješeno provjerom preko `dotnet user-secrets list` i ponovnim ispravnim unosom.
- **Rate limiter vraćao 503 umjesto 429** — riješeno eksplicitnim postavljanjem `RejectionStatusCode = StatusCodes.Status429TooManyRequests`.
- **Security headeri se nisu pojavljivali na odbijenim (429) zahtjevima** — uzrok: pogrešan redoslijed middleware-a (`UseRateLimiter()` postavljen prije headers middleware-a, pa se ovaj drugi nikad nije izvršio za odbijene zahtjeve). Riješeno premještanjem headers middleware-a na sam početak pipeline-a, prije bilo čega što može prekinuti tok zahtjeva.
- **Slučajan rad u pogrešnom (napuštenom) projektu** — imala sam dva paralelna projekta na disku (`C:\Bachelor-thesis` i `C:\Users\...\Bank-demo`); riješeno identifikacijom ispravne putanje i planiranim brisanjem napuštenog projekta.

### Testiranje (Faza 1 — svi scenariji potvrđeni uspješnim)

- [x] Uspješna registracija novog korisnika
- [x] Odbijena registracija sa već zauzetim korisničkim imenom
- [x] Uspješan login sa ispravnim kredencijalima (vraća JWT token)
- [x] Odbijen login sa pogrešnom lozinkom
- [x] Pristup zaštićenoj ruti sa validnim tokenom (200 OK)
- [x] Pristup zaštićenoj ruti bez tokena (401 Unauthorized)
- [x] Pokušaj kreiranja računa kao Client rola (403 Forbidden)
- [x] Uspješno kreiranje računa kao Referent/Admin rola (200 OK)
- [x] Seed Admin nalog kreiran automatski pri prvom pokretanju
- [x] Admin kreira Referent nalog kroz `create-staff` endpoint
- [x] Referent kreira bankovni račun za klijenta
- [x] Uspješan transfer sredstava između dva računa istog vlasnika
- [x] Historija transakcija ispravno prikazuje izvršeni transfer (kroz DTO, sa čitljivim brojevima računa)
- [x] Rate limiting — 6. uzastopni pokušaj pogrešnog logina vraća 429 Too Many Requests
- [x] Security headeri prisutni na svim odgovorima, uključujući odbijene (429) zahtjeve

### Napomene / stvari za spomenuti mentorici

- Promjena baze podataka sa SQLite (iz originalnog prijedloga) na PostgreSQL kroz Docker

### Sljedeći korak

Faza 2 — inicijalizacija Git repozitorija, `.gitignore`, pre-commit hook sa GitLeaks, i namjerno ubačene (kontrolisane) ranjivosti u posebnoj grani za kasnije "prije/poslije" demonstracije i metrike.