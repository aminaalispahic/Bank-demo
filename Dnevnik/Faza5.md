## Faza 5 — Lokalno testiranje SAST/SCA alata (vulnerable-baseline grana)

### Šta sam radila

- Instalirala i pokrenula Semgrep (SAST, besplatna/OSS verzija) na `vulnerable-baseline` grani
- Instalirala i pokrenula Security Code Scan (C#-specifičan Roslyn analyzer, SAST) kao dopunu Semgrep-u
- Pripremila i pokrenula OWASP Dependency-Check (SCA) kroz Docker, uz besplatan NVD API ključ

### Komande koje sam koristila

```bash
# Priprema grane
git checkout vulnerable-baseline

# Semgrep instalacija
pip install semgrep
python -m semgrep --version   # "python -m" pristup jer semgrep nije bio direktno u PATH-u

# Privremeno rješenje PATH problema (samo za trenutnu sesiju terminala)
$env:PATH += ";C:\Users\korisnik\AppData\Roaming\Python\Python314\Scripts"
semgrep --version
semgrep --config=auto BankDemo
semgrep --config=auto BankDemo --json --output semgrep-results.json

# Security Code Scan
cd BankDemo
dotnet add package SecurityCodeScan.VS2019
dotnet clean
dotnet build

# OWASP Dependency-Check (SCA) - Docker pristup
docker pull owasp/dependency-check
docker run --rm -v ${PWD}:/src -v dependency-check-data:/usr/share/dependency-check/data owasp/dependency-check --scan /src/BankDemo --format "HTML" --format "JSON" --out /src/dependency-check-report --project "BankDemo" --nvdApiKey <API_KLJUC>
```

### Rezultati — Semgrep (SAST, OSS/besplatna verzija)

**Nalaz:** 1 od 4 namjerne ranjivosti pronađena.

| Polje | Vrijednost |
|---|---|
| Pravilo | `generic.secrets.security.detected-stripe-api-key` |
| Lokacija | `Services/AuthService.cs`, linija 13 |
| CWE | CWE-798 (Use of Hard-coded Credentials) |
| OWASP | A07:2021 / A07:2025 — Identification and Authentication Failures |
| Severity / Confidence | ERROR / LOW |

**Nepronađeno:** SQL Injection (raw SQL upit u `AccountsController.cs`), nedostatak autorizacije (`GetAllAccounts`), logovanje lozinke (`AuthService.LoginAsync`).

**Analiza:** Semgrep je skenirao 42 fajla sa 84 pravila (besplatan, anoniman режим — alat je eksplicitno naveo poruku "Missed out on 1856 pro rules since you aren't logged in", ukazujući da komercijalna verzija ima znatno širi obuhvat pravila). Zaključak: besplatna (OSS) verzija Semgrep-a dobro pokriva prepoznatljive formate tajni (API ključevi sa specifičnim prefiksima), ali ima ograničen obuhvat C#/.NET-specifičnih obrazaca poput SQL Injection preko ADO.NET/Npgsql poziva, kao i problema vezanih za autorizacijsku logiku i logovanje — potonje dvije kategorije su generalno teško uočljive statičkom analizom bez dubljeg razumijevanja poslovne logike.

### Rezultati — Security Code Scan (SAST, C#-specifičan Roslyn analyzer)

**Nalaz:**
```
warning SCS0002: Potential SQL injection vulnerability was found where 'cmdText' in
'NpgsqlCommand.NpgsqlCommand(string? cmdText, NpgsqlConnection? connection)' may be tainted
by user-controlled data from 'string accountNumber' in method
'IActionResult AccountsController.SearchByAccountNumber(string accountNumber)'.
```

**Analiza:** Security Code Scan je uspješno detektovao SQL Injection ranjivost koju Semgrep OSS nije prepoznao — potvrđuje vrijednost korištenja **jezik/tehnologija-specifičnog** analizatora uz generički SAST alat. Roslyn analyzer pristup (analiza na nivou kompajlera, sa punim razumijevanjem C# tipova) omogućio je preciznije praćenje toka podataka (`accountNumber` parametar → `NpgsqlCommand` konstruktor) nego generička regex-slična pravila.

**Bitna napomena (lekcija):** prvi pokušaj build-a nakon instalacije paketa je prikazao "Build succeeded" **bez** ikakvog upozorenja — što se ispostavilo da je **keširan rezultat** od prije instalacije analizatora. Tek nakon `dotnet clean` pa `dotnet build`, upozorenje SCS0002 se pojavilo. Zaključak za buduću praksu: build keš može sakriti nalaze novo-dodatih analyzer alata; potreban je čist (`clean`) build nakon svake izmjene skupa analizatora.

### Uporedna tabela nalaza (baseline, prije ispravki)

| Ranjivost | GitLeaks | Semgrep (OSS) | Security Code Scan |
|---|:---:|:---:|:---:|
| Hardkodirana tajna (Stripe format) | ✅ | ✅ | — (van fokusa alata) |
| SQL Injection (raw upit) | — | ❌ | ✅ |
| Nedostatak autorizacije | — | ❌ | ❌ |
| Logovanje lozinke | — | ❌ | ❌ |

**Zaključak za poglavlje 6:** Preklapanje nalaza GitLeaks-a i Semgrep-a na istu hardkodiranu tajnu (dva nezavisna alata, isti nalaz) povećava pouzdanost da je riječ o stvarnom (true positive) problemu. Istovremeno, nijedan pojedinačan alat ne pokriva sve četiri kategorije ranjivosti — svaki ima svoje "slijepe tačke" (Semgrep OSS: ograničen obuhvat C#-specifičnih SQL injection obrazaca; Security Code Scan: fokusiran na tok podataka, ne na autorizacijsku logiku ili logovanje). Ovo potvrđuje potrebu za **kombinovanjem više komplementarnih alata**, umjesto oslanjanja na jedan kao univerzalno rješenje. Nijedan od dostupnih (besplatnih) SAST alata nije detektovao nedostatak autorizacije niti logovanje osjetljivih podataka — ove kategorije zahtijevaju ili napredniju (Pro/komercijalnu) analizu toka podataka i poslovne logike, ili ručnu/DAST provjeru.

### OWASP Dependency-Check (SCA)

- Pokrenut kroz Docker (`owasp/dependency-check` image), izbjegnuta lokalna Java instalacija
- Zatražen i korišten besplatan NVD API ključ (nvd.nist.gov) radi bržeg preuzimanja baze ranjivosti — bez ključa, prvo preuzimanje NVD podataka može trajati preko 30 minuta zbog ograničenja brzine
- Korišten imenovani Docker volume (`dependency-check-data`) za trajno čuvanje NVD baze između pokretanja — izbjegava ponovno preuzimanje cijele baze pri svakom sljedećem skeniranju
- Izvještaj generisan u dva formata (HTML za čitanje, JSON za kasniju obradu/metrike)

**[DOPUNITI nakon pregleda izvještaja: broj i severity pronađenih CVE nalaza u zavisnostima, konkretni paketi/verzije na koje se odnose, snimak ekrana HTML izvještaja]**

### Problemi na koje sam naišla i kako sam ih riješila

- **`semgrep` komanda nije prepoznata nakon instalacije preko `pip`** — uzrok: izvršni fajl instaliran u folder koji nije bio u PATH promjenljivoj. Privremeno riješeno dodavanjem `Scripts` foldera u `$env:PATH` za trenutnu sesiju terminala (`python -m semgrep` je alternativa, ali zvanično označena kao zastarjela od verzije 1.38.0).
- **`dotnet build` nije prikazao SCS upozorenje odmah nakon instalacije Security Code Scan-a** — riješeno sa `dotnet clean` prije ponovnog build-a (vidi lekciju gore).
- **Docker volume mount sintaksa na Windows/PowerShell** — `${PWD}` korišten umjesto `$(pwd)` (Bash sintaksa) za referencu na trenutni radni direktorijum.

## Faza 5 — Lokalno testiranje SAST/SCA alata (vulnerable-baseline grana)

### Šta sam radila

- Instalirala i pokrenula Semgrep (SAST, besplatna/OSS verzija) na `vulnerable-baseline` grani
- Instalirala i pokrenula Security Code Scan (C#-specifičan Roslyn analyzer, SAST) kao dopunu Semgrep-u
- Pripremila i pokrenula OWASP Dependency-Check (SCA) kroz Docker, uz besplatan NVD API ključ

### Komande koje sam koristila

```bash
# Priprema grane
git checkout vulnerable-baseline

# Semgrep instalacija
pip install semgrep
python -m semgrep --version   # "python -m" pristup jer semgrep nije bio direktno u PATH-u

# Privremeno rješenje PATH problema (samo za trenutnu sesiju terminala)
$env:PATH += ";C:\Users\korisnik\AppData\Roaming\Python\Python314\Scripts"
semgrep --version
semgrep --config=auto BankDemo
semgrep --config=auto BankDemo --json --output semgrep-results.json

# Security Code Scan
cd BankDemo
dotnet add package SecurityCodeScan.VS2019
dotnet clean
dotnet build

# OWASP Dependency-Check (SCA) - Docker pristup
docker pull owasp/dependency-check
docker run --rm -v ${PWD}:/src -v dependency-check-data:/usr/share/dependency-check/data owasp/dependency-check --scan /src/BankDemo --format "HTML" --format "JSON" --out /src/dependency-check-report --project "BankDemo" --nvdApiKey <API_KLJUC>
```

### Rezultati — Semgrep (SAST, OSS/besplatna verzija)

**Nalaz:** 1 od 4 namjerne ranjivosti pronađena.

| Polje | Vrijednost |
|---|---|
| Pravilo | `generic.secrets.security.detected-stripe-api-key` |
| Lokacija | `Services/AuthService.cs`, linija 13 |
| CWE | CWE-798 (Use of Hard-coded Credentials) |
| OWASP | A07:2021 / A07:2025 — Identification and Authentication Failures |
| Severity / Confidence | ERROR / LOW |

**Nepronađeno:** SQL Injection (raw SQL upit u `AccountsController.cs`), nedostatak autorizacije (`GetAllAccounts`), logovanje lozinke (`AuthService.LoginAsync`).

**Analiza:** Semgrep je skenirao 42 fajla sa 84 pravila (besplatan, anoniman режим — alat je eksplicitno naveo poruku "Missed out on 1856 pro rules since you aren't logged in", ukazujući da komercijalna verzija ima znatno širi obuhvat pravila). Zaključak: besplatna (OSS) verzija Semgrep-a dobro pokriva prepoznatljive formate tajni (API ključevi sa specifičnim prefiksima), ali ima ograničen obuhvat C#/.NET-specifičnih obrazaca poput SQL Injection preko ADO.NET/Npgsql poziva, kao i problema vezanih za autorizacijsku logiku i logovanje — potonje dvije kategorije su generalno teško uočljive statičkom analizom bez dubljeg razumijevanja poslovne logike.

### Rezultati — Security Code Scan (SAST, C#-specifičan Roslyn analyzer)

**Nalaz:**
```
warning SCS0002: Potential SQL injection vulnerability was found where 'cmdText' in
'NpgsqlCommand.NpgsqlCommand(string? cmdText, NpgsqlConnection? connection)' may be tainted
by user-controlled data from 'string accountNumber' in method
'IActionResult AccountsController.SearchByAccountNumber(string accountNumber)'.
```

**Analiza:** Security Code Scan je uspješno detektovao SQL Injection ranjivost koju Semgrep OSS nije prepoznao — potvrđuje vrijednost korištenja **jezik/tehnologija-specifičnog** analizatora uz generički SAST alat. Roslyn analyzer pristup (analiza na nivou kompajlera, sa punim razumijevanjem C# tipova) omogućio je preciznije praćenje toka podataka (`accountNumber` parametar → `NpgsqlCommand` konstruktor) nego generička regex-slična pravila.

**Bitna napomena (lekcija):** prvi pokušaj build-a nakon instalacije paketa je prikazao "Build succeeded" **bez** ikakvog upozorenja — što se ispostavilo da je **keširan rezultat** od prije instalacije analizatora. Tek nakon `dotnet clean` pa `dotnet build`, upozorenje SCS0002 se pojavilo. Zaključak za buduću praksu: build keš može sakriti nalaze novo-dodatih analyzer alata; potreban je čist (`clean`) build nakon svake izmjene skupa analizatora.

### Uporedna tabela nalaza (baseline, prije ispravki)

| Ranjivost | GitLeaks | Semgrep (OSS) | Security Code Scan |
|---|:---:|:---:|:---:|
| Hardkodirana tajna (Stripe format) | ✅ | ✅ | — (van fokusa alata) |
| SQL Injection (raw upit) | — | ❌ | ✅ |
| Nedostatak autorizacije | — | ❌ | ❌ |
| Logovanje lozinke | — | ❌ | ❌ |

**Zaključak za poglavlje 6:** Preklapanje nalaza GitLeaks-a i Semgrep-a na istu hardkodiranu tajnu (dva nezavisna alata, isti nalaz) povećava pouzdanost da je riječ o stvarnom (true positive) problemu. Istovremeno, nijedan pojedinačan alat ne pokriva sve četiri kategorije ranjivosti — svaki ima svoje "slijepe tačke" (Semgrep OSS: ograničen obuhvat C#-specifičnih SQL injection obrazaca; Security Code Scan: fokusiran na tok podataka, ne na autorizacijsku logiku ili logovanje). Ovo potvrđuje potrebu za **kombinovanjem više komplementarnih alata**, umjesto oslanjanja na jedan kao univerzalno rješenje. Nijedan od dostupnih (besplatnih) SAST alata nije detektovao nedostatak autorizacije niti logovanje osjetljivih podataka — ove kategorije zahtijevaju ili napredniju (Pro/komercijalnu) analizu toka podataka i poslovne logike, ili ručnu/DAST provjeru.

### OWASP Dependency-Check (SCA)

- Pokrenut kroz Docker (`owasp/dependency-check` image), izbjegnuta lokalna Java instalacija
- Zatražen i korišten besplatan NVD API ključ (nvd.nist.gov) radi bržeg preuzimanja baze ranjivosti — bez ključa, prvo preuzimanje NVD podataka može trajati preko 30 minuta zbog ograničenja brzine
- Korišten imenovani Docker volume (`dependency-check-data`) za trajno čuvanje NVD baze između pokretanja — izbjegava ponovno preuzimanje cijele baze pri svakom sljedećem skeniranju
- Izvještaj generisan u dva formata (HTML za čitanje, JSON za kasniju obradu/metrike)

**Rezultati skena:**

| Metrika | Vrijednost |
|---|---|
| Zavisnosti skenirano | 118 (106 jedinstvenih) |
| Ranjivih zavisnosti | 2 |
| Ukupno CVE nalaza | 46 |
| dependency-check verzija | 13.0.0 |

**Ranjiva zavisnost #1 — `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 (CRITICAL, 36 CVE)**

Analizom utvrđeno da je riječ o **lažno pozitivnom nalazu** uzrokovanom netačnim CPE (Common Platform Enumeration) mapiranjem — alat je .NET paket (Npgsql EF Core provider) pogrešno identifikovao kao sam PostgreSQL server, zbog poklapanja imena i broja verzije (10.0.3). Prijavljeni CVE-ovi (npr. CVE-2018-16850, CVE-2019-10211) odnose se na ranjivosti PostgreSQL **servera** (SQL injection u pg_upgrade, Windows installer propusti), ne na .NET biblioteku koja se na njega povezuje.

**Ranjiva zavisnost #2 — `System.CodeDom` 6.0.0 (HIGH, 10 CVE, uključujući CISA Known Exploited Vulnerability)**

Isti obrazac — CPE mapiranje pogrešno povezalo .NET `System.CodeDom` paket sa istorijskim **Visual Basic 6.0 / Visual FoxPro** ranjivostima (2001–2012), zbog dijeljenog pojma "CodeDom" u opisu i nazivu proizvoda.

**Zaključak za poglavlje 6:** oba nalaza predstavljaju dokumentovano, poznato ograničenje CPE-baziranih SCA alata — netačno mapiranje paketa na nepovezan softver sličnog imena/verzije. Ovo je vrijedniji nalaz za kritičku analizu nego stvarna ranjivost, jer direktno demonstrira da automatizovani SCA rezultati **zahtijevaju stručnu verifikaciju** prije donošenja zaključaka, i da se broj/severity CVE nalaza ne smije uzimati "zdravo za gotovo" bez provjere relevantnosti. Preporučena mjera: kreiranje suppression fajla (ugrađena funkcija alata) kojim se ova dva netačna CPE mapiranja eksplicitno isključuju iz budućih skenova — standardna praksa poznata kao "tuning" u realnim DevSecOps timovima.

### Problemi na koje sam naišla i kako sam ih riješila

- **`semgrep` komanda nije prepoznata nakon instalacije preko `pip`** — uzrok: izvršni fajl instaliran u folder koji nije bio u PATH promjenljivoj. Privremeno riješeno dodavanjem `Scripts` foldera u `$env:PATH` za trenutnu sesiju terminala (`python -m semgrep` je alternativa, ali zvanično označena kao zastarjela od verzije 1.38.0).
- **`dotnet build` nije prikazao SCS upozorenje odmah nakon instalacije Security Code Scan-a** — riješeno sa `dotnet clean` prije ponovnog build-a (vidi lekciju gore).
- **Docker volume mount sintaksa na Windows/PowerShell** — `${PWD}` korišten umjesto `$(pwd)` (Bash sintaksa) za referencu na trenutni radni direktorijum.

### Sljedeći korak

Dovršiti pregled Dependency-Check izvještaja i dopuniti ovaj dnevnik rezultatima. Zatim Faza 6 — puni CI/CD pipeline koji objedinjuje sve alate testirane u ovoj fazi (GitLeaks, Semgrep, Security Code Scan, Dependency-Check) u automatizovane pipeline stage-ove, uz Trivy skeniranje Docker image-a i DAST/SQLMap testiranje pokrenute aplikacije.
