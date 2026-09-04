## Faza 3 — Strukturisano logovanje (Serilog)

### Šta sam radila

- Instalirala i konfigurisala Serilog kao logging sistem aplikacije (zamjena/dopuna default .NET logging-a)
- Podesila dva "sink-a" (odredišta za logove): konzola i fajl, sa dnevnom rotacijom fajlova
- Dodala strukturisano logovanje sigurnosno-relevantnih događaja na četiri ključna mjesta u kodu: login (uspjeh/neuspjeh), transfer sredstava (uspjeh/IDOR pokušaj), i kreiranje staff naloga
- Testirala logovanje i lokalno (`dotnet run`) i kroz Docker kontejner, potvrdila identično ponašanje na oba okruženja

### Komande koje sam koristila

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File

# Testiranje lokalnih logova
dotnet run
type Logs\bankdemo-2026*.log

# Testiranje logova unutar Docker kontejnera
docker exec bank-demo-app-1 ls Logs
docker exec bank-demo-app-1 cat Logs/bankdemo-20260902.log
docker logs bank-demo-app-1
```

### Konfiguracija (Program.cs)

Na vrhu fajla, prije kreiranja `builder`-a:
```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/bankdemo-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

Odmah nakon `var builder = WebApplication.CreateBuilder(args);`:
```csharp
builder.Host.UseSerilog();
```

`Logs/` dodano u `.gitignore` (log fajlovi mogu sadržavati osjetljive podatke tokom razvoja i generalno se ne verzionišu).

### Mjesta gdje je dodano logovanje

| Metoda | Događaj | Nivo | Podaci u logu |
|---|---|---|---|
| `AuthService.LoginAsync` | Neuspješan pokušaj logina | Warning | Username (bez lozinke) |
| `AuthService.LoginAsync` | Uspješan login | Information | Username, UserId, Role |
| `AccountService.TransferAsync` | Pokušaj transfera sa tuđeg računa (IDOR) | Warning | UserId, pokušani AccountId |
| `AccountService.TransferAsync` | Uspješan transfer | Information | Amount, FromAccount, ToAccount |
| `AuthService.CreateStaffAsync` | Kreiranje novog staff naloga | Information | Username, Role |

Implementacija kroz standardnu .NET Dependency Injection — `ILogger<T>` ubrizgan u konstruktore `AuthService` i `AccountService`, korištenjem strukturisanih placeholder-a (`{Username}`, `{UserId}`, itd.) umjesto ručne konkatenacije stringova.

### Ključna odluka — kontrast sa namjernom ranjivošću

Eksplicitno poređenje sa Ranjivosti #4 iz `vulnerable-baseline` grane (`Console.WriteLine($"Login pokusaj: {request.Username} / {request.Password}")`):

| Loše (namjerna ranjivost, `vulnerable-baseline`) | Ispravno (`main`, ova implementacija) |
|---|---|
| Lozinka eksplicitno ispisana u logu | Lozinka se nikad ne pominje |
| Nestrukturisan tekst (`Console.WriteLine`) | Strukturisana polja (`{Username}`, `{UserId}`, `{Role}`) — kasnije pretraživa/filtrabilna |
| Samo konzola, nestaje pri zatvaranju terminala | Konzola + fajl (trajno, s dnevnom rotacijom) |
| Bez razlike u ozbiljnosti | `LogWarning` za sumnjive/neuspješne događaje, `LogInformation` za normalan tok |

Ovaj par (loše/ispravno) direktno demonstrira razliku između sigurnog i nesigurnog pristupa logovanju za isti tip događaja (pokušaj logina) — koristan materijal za poglavlje 4 ili 8.

### Testiranje i verifikacija

- [x] Log fajl (`Logs/bankdemo-YYYYMMDD.log`) uspješno kreiran pri lokalnom pokretanju (`dotnet run`)
- [x] Zapisi o neuspješnom i uspješnom loginu prisutni, bez lozinke u tekstu
- [x] Zapis o izvršenom transferu prisutan sa tačnim iznosom i brojevima računa
- [x] Identičan Serilog izlaz potvrđen i unutar Docker kontejnera (`docker logs`, `docker exec ... cat Logs/...`) — potvrđuje prenosivost konfiguracije između okruženja bez izmjene koda

### Problemi na koje sam naišla i kako sam ih riješila

- **Log fajl se prvo nije mogao naći** — uzrok: relativna putanja `Logs/bankdemo-.log` pravi folder u trenutnom radnom direktorijumu aplikacije (`BankDemo` folder, gdje je `.csproj`), ne u roditeljskom `Bank-demo` root folderu gdje se očekivalo.
- **Zabuna između lokalnog i Docker logovanja** — testiranje je prvo urađeno kroz Docker (port 8080), dok se provjera radila na lokalnom fajlu (koji ostaje prazan jer Docker kontejner ima izolovan fajl sistem). Riješeno razjašnjavanjem da svaki način pokretanja (lokalno vs. kontejner) ima svoj odvojen log fajl, i testiranjem oba posebno — lokalno na `https://localhost:7278/swagger`, Docker na `http://localhost:8080/swagger`.

### Sljedeći korak

Faza 4 — Dockerfile za aplikaciju (multi-stage build, non-root korisnik, Hadolint provjera), i integracija sa postojećim PostgreSQL kontejnerom kroz zajednički `docker-compose.yml`.
