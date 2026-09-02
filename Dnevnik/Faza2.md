## Faza 2 — Git repo, GitLeaks, namjerne ranjivosti, sigurnosni testovi

### Šta sam radila

- Postavila `.gitignore` za projekat (build artefakti, IDE fajlovi, lokalne tajne)
- Instalirala i konfigurisala GitLeaks za detekciju tajni u kodu, uključujući prilagođeno (custom) pravilo
- Postavila pre-commit hook koji automatski pokreće GitLeaks prije svakog commit-a
- Izmjestila lozinku baze podataka iz `docker-compose.yml` u `.env` fajl
- Napravila prvi zvaničan commit kompletne aplikacije (Faza 1) na `main` granu
- Kreirala posebnu granu (`vulnerable-baseline`) sa četiri namjerno ubačene, dokumentovane ranjivosti — priprema za kasnije SAST/DAST/SCA demonstracije i metrike
- Postavila xUnit test projekat i napisala jedinične testove za sigurnosnu/poslovnu logiku transfera sredstava

### Komande koje sam koristila

```bash
# GitLeaks instalacija
winget install --id Gitleaks.Gitleaks -e
gitleaks version

# Testiranje GitLeaks-a
gitleaks detect --source . --verbose --no-git
gitleaks protect --staged --verbose
gitleaks protect --staged --verbose --config .gitleaks.toml

# Git operacije
git status
git add docker-compose.yml
git add .
git commit -m "Dodaj BankDemo aplikaciju: autentifikacija, RBAC, transferi, rate limiting, security headeri"
git push --set-upstream origin main   # (ili ekvivalent, ako je repo već povezan)

# Nova grana za namjerne ranjivosti
git checkout -b vulnerable-baseline
git add .
git commit -m "Namjerno ubačene ranjivosti za SAST/DAST demonstraciju (baseline)" --no-verify
git push --set-upstream origin vulnerable-baseline
git checkout main

# Test projekat
dotnet new xunit -n BankDemo.Tests
dotnet add reference ../BankDemo/BankDemo.csproj
dotnet add package Moq
dotnet test
```

### Ključne odluke i obrazloženja

1. **`.gitignore` sadržaj.** Isključeni: `bin/`, `obj/`, `.vs/`, `*.user`, `*.suo`, `appsettings.Development.json`, `.env`, `*.db`, OS-specifični fajlovi (`.DS_Store`, `Thumbs.db`). Obrazloženje: build artefakti se uvijek mogu ponovo generisati, IDE-specifični fajlovi nisu relevantni za druge saradnike/komisiju, a `.env`/`appsettings.Development.json` mogu potencijalno sadržavati tajne pa se isključuju kao dodatna mjera opreza uz `user-secrets`.

2. **GitLeaks — testiranje default pravila na stvarnom, kontrolisanom secret-u.** Prvi test (`gitleaks protect --staged`) na lozinci `sifraZavrsni` u `docker-compose.yml` **nije** pronašao ništa (false negative) — default "generic-password" pravilo zahtijeva visoku entropiju (nasumične karaktere), a obična, čitljiva lozinka je entropijski preniska da bi bila prepoznata. Ovo je dokumentovan, autentičan nalaz o ograničenju alata, ne greška u konfiguraciji.

3. **Prilagođeno (custom) GitLeaks pravilo.** Napravljen `.gitleaks.toml` sa pravilom `docker-compose-hardcoded-password`, koje eksplicitno traži ključeve `POSTGRES_PASSWORD`/`DB_PASSWORD`/`MYSQL_ROOT_PASSWORD` u YAML fajlovima, bez obzira na entropiju vrijednosti. Uz to, definisan allowlist regex (`\$\{.*\}`) koji izuzima već-bezbjedne reference na environment varijable (Docker Compose `${VAR}` sintaksa) od prijavljivanja kao nalaz. Nakon dodavanja pravila, ista lozinka je uspješno detektovana (true positive) — potvrđen "before/after" par nalaza.

4. **Izmještanje lozinke u `.env`.** `docker-compose.yml` sad referencira `${POSTGRES_PASSWORD}` umjesto hardkodirane vrijednosti; stvarna vrijednost živi u `.env` fajlu, koji je u `.gitignore`. GitLeaks (uz custom pravilo i allowlist) potvrđuje da fajl spreman za commit više ne sadrži nalaz.

5. **GitLeaks je, suprotno prethodnom nalazu, uspješno prepoznao Stripe-format API ključa** (`sk_live_...`, dodat kao namjerna ranjivost br. 2 — vidi niže) kroz ugrađeno pravilo `stripe-access-token`. Ovo pokazuje da alat dobro pokriva **poznate, strukturirane formate** tajni (API ključevi sa prepoznatljivim prefiksima), dok generičke, ljudski-čitljive lozinke ostaju slabija tačka — koristan kontrast za kritičku analizu alata u radu.

6. **Pre-commit hook implementiran ručno** kroz `.git/hooks/pre-commit` (Bash skripta), umjesto gotovog frameworka poput `pre-commit` (Python) ili `husky` (Node.js). Skripta pokreće `gitleaks protect --staged` sa custom konfiguracijom i zaustavlja commit (`exit 1`) ako se pronađe nalaz. Ograničenje: `.git/hooks/` se ne verzioniše kroz Git, pa hook postoji samo lokalno na razvojnoj mašini — u timskom radu bi bio potreban dijeljeni mehanizam (napomenuto kao ograničenje implementacije).

7. **Namjerno zaobilaženje pre-commit hook-a (`--no-verify`) pri kreiranju `vulnerable-baseline` grane.** Opravdano isključivo kontrolisanom, dokumentovanom potrebom da se ranjiv kod svjesno unese radi kasnije demonstracije skeniranja — eksplicitno navedeno kao izuzetak, ne kao standardna praksa.

### Namjerno ubačene ranjivosti (grana `vulnerable-baseline`)

Sve označene komentarom `// VULN:` radi lakšeg pronalaska pri kasnijoj analizi/ispravci.

| # | Ranjivost | Lokacija | Kategorija / relevantan alat |
|---|---|---|---|
| 1 | SQL Injection — direktna konkatenacija korisničkog unosa u SQL upit (`SearchByAccountNumber` endpoint), bez parametrizacije | `Controllers/AccountsController.cs` | SAST (Semgrep/Security Code Scan), DAST/SQLMap |
| 2 | Hardkodirana tajna (lažni Stripe-format API ključ) direktno u kodu | `Services/AuthService.cs` | GitLeaks, SAST |
| 3 | Endpoint bez autorizacije (`[AllowAnonymous]` na `GetAllAccounts`, izlaže podatke svih korisnika) | `Controllers/AccountsController.cs` | DAST, ručna RBAC provjera, OWASP API Top 10 (Broken Function Level Authorization) |
| 4 | Logovanje osjetljivih podataka (lozinke) u čistom tekstu (`Console.WriteLine`) | `Services/AuthService.cs`, `LoginAsync` | SAST (detekcija logovanja osjetljivih podataka) |

### Sigurnosni jedinični testovi (xUnit + Moq)

Projekat `BankDemo.Tests`, sa mock-ovanim repozitorijima (bez zavisnosti od stvarne baze):

1. `TransferAsync_FromAccountNotOwnedByUser_ReturnsFailure` — potvrđuje IDOR zaštitu (korisnik ne može izvršiti transfer sa računa koji nije njegov)
2. `TransferAsync_InsufficientBalance_ReturnsFailure` — potvrđuje poslovno pravilo (nedovoljno sredstava)
3. `TransferAsync_NegativeAmount_ReturnsFailure` — potvrđuje validaciju inputa (negativan iznos odbijen)
4. `TransferAsync_ValidRequest_ReturnsSuccess` — kontrolni test, potvrđuje da ispravan zahtjev prolazi (nije prestrogo ograničeno)

Svi testovi prošli uspješno (`dotnet test` → succeeded: 4/4).

**Napomena o obimu:** ovi testovi pokrivaju poslovnu logiku unutar servisnog sloja. RBAC provjere na nivou HTTP endpoint-a (npr. Client dobija 403 na admin funkcijama) dokumentovane su ručno kroz Postman/Swagger (Faza 1) — puna automatizacija bi zahtijevala integracione testove sa `WebApplicationFactory`, što je van obima ove implementacije (navedeno kao ograničenje u poglavlju 11).

### Problemi na koje sam naišla i kako sam ih riješila

- **GitLeaks nije prepoznao jednostavnu lozinku** (`sifraZavrsni`) u `docker-compose.yml` — riješeno pisanjem prilagođenog pravila u `.gitleaks.toml`, sa pravilnim korištenjem `(?:...)` non-capturing grupe da bi allowlist provjeravao vrijednost, ne ime ključa.
- **PATH problem — `gitleaks: command not found` u Git Bash-u**, iako je alat radio u PowerShell/CMD — riješeno pronalaskom pune putanje (`where gitleaks`) i njenim eksplicitnim korištenjem u pre-commit hook skripti (konvertovano u Unix-style putanju za Git Bash: `/c/Users/...`).
- **TOML sintaksna greška** (`[[rules.allowlist]]` umjesto `[rules.allowlist]`) — allowlist je jedan objekat, ne lista; riješeno ispravkom broja uglastih zagrada.
- **Rad u pogrešnom (napuštenom) projektu** — otkriveno i riješeno ranije, relevantno i ovdje jer je uticalo na redoslijed provjera lokacija prije Git komandi.
- **Upstream grana nije bila povezana** (`git push` greška za novu granu) — riješeno sa `git push --set-upstream origin <grana>`.

### Sljedeći korak

Faza 3 — strukturisano logovanje (Serilog). Zgodan kontrast sa Ranjivosti br. 4 iz `vulnerable-baseline` grane: demonstracija razlike između nesigurnog logovanja (lozinka u čistom tekstu, `Console.WriteLine`) i ispravnog, strukturisanog logovanja bez osjetljivih podataka.
