# GameVault — Codex Roadmap & Project Brief

> **Projekt:** GameVault  
> **Koncept:** personal game library + desktop launcher + mini Steam-like sustav  
> **Stack:** C# / .NET 8 ili .NET 9, WPF, MVVM, SQLite, EF Core, ASP.NET Core Web API, REST, TCP/UDP mini servisi, crypto utilities, PDF reports  
> **Primarni cilj:** napraviti praktičan desktop launcher za organizaciju, pokretanje, download i praćenje igara, uz dovoljno demonstracija za projekt iz kolegija.

---

## 0. Kako koristiti ovaj dokument s Codexom

Ovaj dokument služi kao glavni roadmap. Ne tražiti od Codexa da napravi sve odjednom.

### Pravila za Codex

Koristi ove upute u promptovima:

```txt
Radi jedan mali milestone po zadatku.
Nemoj generirati cijeli projekt odjednom.
Prije koda objasni plan.
Nakon promjene objasni koje klase/datoteke si dodao i zašto.
Kod mora biti razumljiv za studentsku obranu.
Izbjegni over-engineering.
Ako predlažeš kompleksniji pristup, objasni jednostavniju alternativu.
```

### Idealni stil rada

1. Codex predloži mali korak.
2. Codex implementira taj korak.
3. Ti pokreneš aplikaciju.
4. Ako radi, commitaš.
5. Ideš na sljedeći korak.

### Ne raditi

```txt
Napravi cijeli GameVault projekt odjednom.
```

To vodi u kaos, puno bugova i kod koji nećeš znati objasniti.

---

## 1. Kratki opis aplikacije

GameVault je desktop aplikacija za upravljanje osobnom kolekcijom igara.

Korisnik može:

- prijaviti se u aplikaciju
- uređivati svoj profil
- dodavati lokalne igre s računala
- dodati putanju do `.exe` datoteke i pokrenuti igru iz launchera
- pregledavati biblioteku igara
- filtrirati i sortirati igre
- spremati cover slike igara
- skidati demo igre ili test ZIP pakete s vlastitog home lab servera
- raspakirati ZIP i označiti igru kao instaliranu
- pratiti vrijeme igranja
- otključavati achievemente
- imati friend listu
- generirati PDF izvještaje
- izvoziti/uvoziti podatke u JSON/XML
- šifrirati backupove
- provjeravati hash/integritet datoteka
- koristiti REST API za online katalog, prijatelje ili sinkronizaciju
- demonstrirati TCP/UDP komunikaciju kroz zasebne male servere
- koristiti DLL/class library projekte za crypto, report i plugin funkcionalnosti

---

## 2. Predložena struktura solutiona

Prvo krenuti s minimalnim projektima, a kasnije dodavati ostale.

### Finalna struktura

```txt
GameVault.sln
│
├── GameVault.Desktop          // WPF desktop aplikacija, glavni UI
├── GameVault.Core             // domain modeli, DTO-i, interfacei, business logika
├── GameVault.Data             // EF Core, SQLite, DbContext, repositories
├── GameVault.Crypto           // AES, RSA, SHA-256, digital signatures
├── GameVault.Reports          // PDF report generator
├── GameVault.API              // ASP.NET Core REST API
├── GameVault.MatchServer      // TCP server demo
├── GameVault.DiscoveryServer  // UDP server demo
├── GameVault.BackupTool       // Console app, drugi proces koji Desktop pokreće
└── GameVault.Plugins.*        // opcionalni plugin DLL-ovi
```

### Minimalni početak

Krenuti samo s:

```txt
GameVault.Desktop
GameVault.Core
GameVault.Data
```

Dodavati ostale tek kad radi osnovna aplikacija.

---

## 3. Tehnologije

### Desktop framework

Koristiti **WPF**.

Razlog:

- moderniji od WinForms
- dobro radi s MVVM patternom
- ima binding, commands, templates
- slično mentalnom modelu kao frontend frameworkovi
- odličan za dashboard, library grid, profile view, settings

### .NET verzija

Koristiti **.NET 8 LTS** ili najnoviju stabilnu verziju koju Visual Studio podržava bez problema.

### Baza

Koristiti **SQLite + EF Core**.

Razlog:

- lokalna baza, nema potrebe za SQL Server instalacijom
- super za desktop studentski projekt
- lako se demonstrira CRUD
- lako se backupira

### Arhitektura

Koristiti jednostavni MVVM:

```txt
View       = XAML ekran
ViewModel  = logika ekrana, properties, commands
Model      = podaci/domain klase
Service    = poslovna logika
Repository = rad s bazom
```

---

## 4. Glavni moduli aplikacije

## 4.1 Login modul

### Ideja

Korisnik se prijavi u GameVault.

### UI

- Username textbox
- Password textbox
- Login button
- Register link/button

### Implementacija

Model:

```csharp
User
- Id
- Username
- PasswordHash
- Role
- AvatarImage
- Bio
- Country
- FavoriteGameId
- CreatedAt
```

Servisi:

```csharp
AuthService
- Register(username, password)
- Login(username, password)
- Logout()
```

Crypto:

```csharp
HashService
- HashPassword(password)
- VerifyPassword(password, hash)
```

### Pokriva iz kolegija

- korisničke klase
- baze podataka
- hash funkcija SHA-256
- autorizacija korisnika ako se doda Role

---

## 4.2 User Profile modul

### Ideja

Steam-like profil korisnika.

### Podaci

- username
- avatar
- bio
- country
- favorite game
- total playtime
- number of installed games
- achievements count

### Implementacija

Avatar spremiti kao BLOB u SQLite ili kao datoteku na disku s pathom u bazi.

Za bolju demonstraciju profesorove BLOB stavke, barem jednu sliku spremiti direktno u bazu kao `byte[]`.

### Pokriva

- BLOB polje
- korisničke klase
- baza
- calculated fields: total playtime, achievement count

---

## 4.3 Game Library modul

### Ideja

Glavni dio aplikacije: popis svih igara.

### UI

- grid/card prikaz igara
- search box
- genre filter
- sort dropdown
- buttons: Add, Edit, Delete, Play, Install, Details

### Model

```csharp
Game
- Id
- Title
- Description
- Genre
- Developer
- Publisher
- ReleaseYear
- SizeMb
- CoverImageBlob
- LocalExePath
- InstallPath
- DownloadUrl
- Version
- IsInstalled
- CreatedAt
- UpdatedAt
```

### Servis

```csharp
GameService
- AddGame(game)
- UpdateGame(game)
- DeleteGame(id)
- GetAllGames()
- SearchGames(query)
- FilterByGenre(genre)
- SortGames(sortMode)
```

### Pokriva

- CRUD nad bazom
- sortiranje
- filtriranje
- lookup polje: GenreName iz Genre tablice
- calculated field: DisplaySize, InstalledStatusText
- BLOB cover image

---

## 4.4 Add Local Game

### Ideja

Korisnik može ručno dodati igru instaliranu na svom računalu.

### Flow

1. Klik na `Add Local Game`
2. Odabir `.exe` datoteke kroz file picker
3. Korisnik upiše title/genre
4. Spremi se u bazu
5. Gumb `PLAY` pokreće `.exe`

### Implementacija

Koristiti:

```csharp
OpenFileDialog
Process.Start(game.LocalExePath)
```

### Pokriva

- proces A/B: Desktop aplikacija pokreće vanjski proces/igru
- rad s datotekama
- korisna stvarna funkcionalnost

---

## 4.5 Play Game / Tracking Playtime

### Ideja

Launcher pokreće igru i mjeri koliko dugo je proces radio.

### Flow

1. User klikne `PLAY`
2. Aplikacija pozove `Process.Start`
3. Spremi se `StartedAt`
4. Kad proces završi, izračuna se trajanje
5. Playtime se upiše u bazu
6. Activity feed dobije log: `User played X for 42 minutes`

### Implementacija

```csharp
Process process = Process.Start(...);
await process.WaitForExitAsync();
```

### Pokriva

- pokretanje drugog procesa
- dretve/asinkroni rad
- logovi
- calculated fields

---

## 4.6 Download Manager

### Ideja

GameVault može skinuti ZIP paket igre/demo sadržaja s home lab servera.

### Home lab setup

Na home labu napraviti folder:

```txt
/gamevault-downloads
    demo-game-1.zip
    demo-game-2.zip
    demo-game-3.zip
```

Dignuti običan HTTP server:

```txt
http://192.168.1.50/games/demo-game-1.zip
```

Moguće opcije:

- nginx
- Apache
- IIS
- Python simple HTTP server
- Docker container s nginxom

### Flow

1. U bazi Game ima `DownloadUrl`
2. User klikne `Install`
3. Aplikacija skida ZIP preko `HttpClient`
4. Prikazuje progress bar
5. Ograničava brzinu downloada na par preset vrijednosti
6. Raspakira ZIP u lokalni install folder
7. Postavlja `IsInstalled = true`

### Speed limit ideja

Dropdown:

```txt
No limit
256 KB/s
1 MB/s
5 MB/s
```

Implementacija može biti jednostavna: nakon svakog chunka napraviti `Task.Delay` ovisno o odabranoj brzini.

### Pokriva

- HTTP klijent
- preuzimanje sadržaja s Interneta/LAN-a
- progress bar
- limit brzine
- dretve/asinkroni rad
- sigurno ažuriranje UI-ja iz background taska

---

## 4.7 Achievements

### Ideja

Steam-like achievement sustav.

### Primjeri achievementa

```txt
First Launch       - pokreni prvu igru
Collector          - dodaj 5 igara
Archivist          - napravi prvi backup
Online Player      - spoji se na REST API
Crypto Nerd        - šifriraj prvi backup
Night Owl          - igraj nakon ponoći
```

### Model

```csharp
Achievement
- Id
- Name
- Description
- IconBlob
- RuleKey
```

```csharp
UserAchievement
- UserId
- AchievementId
- UnlockedAt
```

### Servis

```csharp
AchievementService
- CheckAchievements(userId)
- UnlockAchievement(userId, achievementId)
```

### Pokriva

- više tablica
- master-detail
- BLOB ikone
- business klase i metode
- reporti

---

## 4.8 Friend List

### Ideja

Jednostavna friend lista, ne pravi chat.

### Funkcionalnosti

- dodaj prijatelja po usernameu
- prihvati/odbij zahtjev
- popis prijatelja
- online/offline status simuliran ili preko API-ja

### Model

```csharp
Friendship
- Id
- RequesterUserId
- ReceiverUserId
- Status
- CreatedAt
- AcceptedAt
```

Status:

```txt
Pending
Accepted
Rejected
Blocked
```

### REST opcija

Kasnije friend listu prebaciti u `GameVault.API`:

```txt
GET /api/friends
POST /api/friends/request
POST /api/friends/accept
DELETE /api/friends/{id}
```

### Pokriva

- REST API
- auth/autorizacija
- baza s više tablica
- korisnički tipovi

---

## 4.9 Activity Feed / Logs

### Ideja

Popis aktivnosti korisnika.

Primjeri:

```txt
Maksim installed Hollow Knight
Maksim played Terraria for 38 minutes
Maksim unlocked First Launch
Maksim exported encrypted backup
```

### Model

```csharp
ActivityLog
- Id
- UserId
- Type
- Message
- CreatedAt
```

### Servis

```csharp
ActivityLogService
- AddLog(userId, type, message)
- DeleteLog(id)
- GetLogsForPeriod(from, to)
```

### Pokriva

- klasa s atributima i metodama
- JSON export logova
- filtriranje po vremenskom periodu
- mutex/lock ako više dretvi zapisuje logove

---

## 4.10 Settings

### Ideja

Postavke aplikacije.

### Postavke

- jezik: HR/ENG
- theme: dark/light
- default install folder
- download speed limit
- remember last user

### Implementacija

Za profesorove zahtjeve koristi:

- INI datoteku za UI postavke
- appsettings.json za developer konfiguraciju

Primjer INI:

```ini
[Ui]
Language=hr
Theme=Dark
FontSize=14

[Downloads]
DefaultFolder=D:\GameVault\Games
SpeedLimit=1048576
```

### Pokriva

- spremanje i učitavanje minimalno 2 postavke iz INI datoteke
- višejezično sučelje

---

## 4.11 Localization HR/ENG

### Ideja

Sučelje na hrvatskom i engleskom.

### Implementacija

Opcije:

1. `.resx` resource files
2. vlastiti JSON prijevodi
3. vlastiti localization service

Za studentski projekt je jasno demonstrabilno:

```txt
Resources.hr.resx
Resources.en.resx
```

Prevesti barem 5 ekrana:

- Login
- Dashboard
- Game Library
- Game Details
- Settings

### Pokriva

- sučelje na 2 jezika
- 5+ prevedenih dijaloga/ekrana

---

## 4.12 Reports / PDF

### Ideja

Generirati PDF izvještaje.

### Biblioteka

Koristiti QuestPDF ili neku drugu stabilnu PDF biblioteku.

### Izvještaji

1. Library Report
   - popis igara
   - žanr
   - installed status
   - playtime

2. User Activity Report
   - aktivnosti u periodu
   - pokrenute igre
   - otključani achievementi

3. Master-detail report
   - User -> InstalledGames
   - Game -> Achievements

### Pokriva

- automatsko generiranje izvještaja
- spremanje u PDF
- podaci iz barem dvije tablice u master-detail odnosu

---

## 4.13 JSON/XML Import/Export

### Ideja

GameVault može izvesti i uvesti podatke.

### JSON

Export library:

```txt
game-library.json
```

Sadrži:

- igre
- playtime
- installed status

### XML

Export achievements ili settings:

```txt
achievements.xml
```

### Operacije

Za JSON/XML demonstrirati:

- čitanje
- pisanje
- uređivanje
- brisanje

### Pokriva

- XML/JSON obrada podataka
- CRUD nad datotekama

---

## 4.14 Custom Binary Format

### Ideja

Vlastiti `.gvault` backup format.

### Format

```txt
Header:
- Magic: GVAULT
- Version: 1
- CreatedAt
- RecordCount

Records:
- GameId
- TitleLength
- Title
- PlayTimeMinutes
- IsInstalled
```

### Klase

```csharp
GameVaultBinaryHeader
- Magic
- Version
- CreatedAt
- RecordCount
```

```csharp
GameVaultBinaryRecord
- GameId
- Title
- PlayTimeMinutes
- IsInstalled
```

```csharp
GameVaultBinaryService
- WriteBackup(path, games)
- ReadBackup(path)
```

### Pokriva

- vlastiti binarni format
- čitanje i pisanje
- dodatne korisničke klase

---

## 4.15 Crypto modul

### Projekt

```txt
GameVault.Crypto
```

Build output će biti DLL:

```txt
GameVault.Crypto.dll
```

### SHA-256

Koristiti za:

- hash lozinke
- provjeru integriteta ZIP datoteke

```csharp
HashService
- ComputeSha256(text)
- ComputeFileSha256(path)
- VerifyHash(...)
```

### AES

Koristiti za:

- šifriranje backup datoteke
- dešifriranje backup datoteke

```csharp
AesService
- EncryptBytes(data, password)
- DecryptBytes(data, password)
```

### RSA

Koristiti za:

- asimetrično šifriranje manjeg sadržaja
- digitalni potpis exporta

```csharp
RsaService
- GenerateKeyPair()
- EncryptWithPublicKey(...)
- DecryptWithPrivateKey(...)
- SignData(...)
- VerifySignature(...)
```

### Digital Signature

Flow:

1. Exportira se `library.json`
2. Generira se `library.sig`
3. Import radi samo ako je potpis valjan

### Pokriva

- AES
- RSA
- SHA-256
- digitalno potpisivanje
- DLL/class library

---

## 4.16 REST API

### Projekt

```txt
GameVault.API
```

### Ideja

REST API simulira online dio sustava.

### Resursi

```txt
/api/games
/api/users
/api/friends
/api/achievements
/api/status
```

### Minimalni endpointi

```txt
GET /api/games
GET /api/games/{id}
POST /api/games
PUT /api/games/{id}
DELETE /api/games/{id}
```

```txt
GET /api/friends
POST /api/friends/request
POST /api/friends/accept
```

```txt
GET /api/status/time
GET /api/status/health
```

### Auth

Dodati JWT ili jednostavniju token auth demonstraciju.

Role:

```txt
Admin
User
```

Primjer autorizacije:

- Admin može dodavati igre u online katalog
- User može samo čitati katalog i upravljati svojom friend listom

### Pokriva

- REST web servis u zasebnoj aplikaciji
- minimalno dva resursa
- auth/autorizacija
- klijent aplikacija koristi REST servis

---

## 4.17 External REST API

### Ideja

GameVault se može spojiti na neki vanjski REST API.

Moguće ideje:

- dohvat javne IP adrese
- dohvat države/geo info
- dohvat random joke kao “daily fun” widget
- dohvat exchange rate ako se dodaju cijene igara

### Primjer

```txt
GET https://api.ipify.org/?format=json
```

ili REST Countries za prikaz zemlje profila.

### Pokriva

- spajanje na udaljeni REST servis po izboru

---

## 4.18 SOAP

### Status

SOAP nije prioritet, ali može se dodati ako treba više bodova.

### Ideja

Koristiti javni SOAP servis za NumberConversion ili TextCasing.

Primjer korištenja:

- convert playtime number to words
- pretvori ime igre u uppercase/titlecase preko SOAP servisa

### Pokriva

- SOAP klijent prema udaljenom SOAP servisu

---

## 4.19 TCP Match Server

### Projekt

```txt
GameVault.MatchServer
```

### Ideja

Jednostavan TCP server koji simulira online gaming status.

### Komande

Klijent šalje:

```txt
PING
```

Server vraća:

```txt
PONG
```

Klijent šalje:

```txt
PLAYER_COUNT
```

Server vraća:

```txt
42
```

Klijent šalje:

```txt
GAME_STATUS Terraria
```

Server vraća:

```txt
ONLINE
```

### Naprednije

Stream prijenos:

- klijent pošalje JSON file
- server vrati obrađeni JSON/XML

### Pokriva

- TCP klijent-poslužitelj komunikacija
- zasebne aplikacije
- jednostavni tipovi podataka
- opcionalno streamovi

---

## 4.20 UDP Discovery Server

### Projekt

```txt
GameVault.DiscoveryServer
```

### Ideja

Desktop aplikacija može pronaći lokalni GameVault server na mreži.

### Flow

1. Desktop šalje UDP broadcast:

```txt
DISCOVER_GAMEVAULT
```

2. Server odgovara:

```txt
GAMEVAULT_SERVER_FOUND;192.168.1.50;9000
```

### Pokriva

- UDP klijent-poslužitelj komunikacija
- zasebne aplikacije

---

## 4.21 BackupTool kao drugi proces

### Projekt

```txt
GameVault.BackupTool
```

### Ideja

Desktop aplikacija pokreće poseban console program za backup.

### Flow

1. User klikne `Create Backup`
2. Desktop pokrene:

```txt
GameVault.BackupTool.exe --source db.sqlite --output backup.gvault
```

3. BackupTool vrati exit code:

```txt
0 = OK
1 = error
```

4. Desktop prikaže poruku korisniku

### Pokriva

- komunikacija između dva procesa
- proces A kreira proces B
- proces B vraća exit code

---

## 4.22 Threading / Parallel tasks

### Ideja

Demonstrirati barem tri paralelna zadatka.

### Primjeri

1. Download više ZIP paketa paralelno
2. Hashiranje više datoteka paralelno
3. Analiza playtime statistike paralelno
4. Generiranje thumbnailova cover slika paralelno

### Implementacija

Koristiti:

```csharp
Task.Run
Parallel.ForEachAsync
CancellationToken
IProgress<T>
```

### UI update

U WPF-u koristiti:

```csharp
Dispatcher.Invoke(...)
```

ili binding preko ViewModel propertyja ako se update radi pravilno na UI threadu.

### Pokriva

- dretve
- ThreadPool
- paralelno izvršavanje 3 zadatka
- sigurno ažuriranje UI komponenti

---

## 4.23 Lock / Mutex

### Ideja

Više taskova piše u isti log file.

### Implementacija

Jednostavno:

```csharp
private static readonly object _logLock = new();

lock (_logLock)
{
    File.AppendAllText(...);
}
```

Naprednije:

```csharp
SemaphoreSlim
Mutex
```

### Demonstracija

Pokrenuti 5 paralelnih taskova koji pišu activity log.
Bez locka pokazati potencijalni problem, s lockom pokazati sigurno pisanje.

### Pokriva

- međusobno zaključavanje
- kritična sekcija/mutex

---

## 4.24 DLL / Class Library / Plugins

### Obavezno lako

`GameVault.Crypto` i `GameVault.Reports` su class library projekti koji se buildaju u DLL.

### Plugin sustav

Folder:

```txt
Plugins
```

Plugin interface:

```csharp
public interface IGameVaultPlugin
{
    string Name { get; }
    string Description { get; }
    void Execute(PluginContext context);
}
```

Primjer pluginova:

```txt
GameVault.Plugin.Statistics.dll
GameVault.Plugin.AchievementRules.dll
GameVault.Plugin.ThemePack.dll
```

Desktop ih učita pomoću:

```csharp
Assembly.LoadFrom(path)
```

### Pokriva

- statička/dinamička biblioteka
- kod u DLL-u
- klase/metode u biblioteci
- opcionalno resursi u DLL-u

---

## 4.25 Resource DLL

### Opcionalno

Napraviti DLL/class library s embedded resources:

- default cover image
- achievement icons
- app logo

Desktop učita resurs iz DLL-a i prikaže ga.

### Pokriva

- resurs smješten u dinamičkoj biblioteci

---

## 5. Mapiranje na profesorove funkcionalnosti

> Ovaj dio služi da na obrani znaš točno pokazati gdje je što implementirano.

| # | Zahtjev iz kolegija | GameVault implementacija | Prioritet |
|---|---|---|---|
| 1 | Minimalno dvije klase s atributima i metodama | User, Game, Achievement, ActivityLog, servisi | MUST |
| 2 | Minimalno tri dijaloga/forme i komunikacija | LoginView, MainWindow, GameDetailsView, SettingsView; prijenos logged-in usera | MUST |
| 3 | Sučelje na 2 jezika | HR/ENG resources, prevesti 5 ekrana | SHOULD |
| 4 | INI ili Registry postavke | `settings.ini` za theme/language/download folder/speed | SHOULD |
| 5 | XML ili JSON CRUD | export/import games JSON, achievements XML | MUST |
| 6 | Custom binary format | `.gvault` backup format | SHOULD |
| 7 | Baza + CRUD nad 2 tablice | SQLite + EF Core; Games, Users, Achievements, Friends | MUST |
| 8 | Sort/filter + calculated/lookup field | sort/filter games; Genre lookup; total playtime calculated | MUST |
| 9 | BLOB field | avatar/cover image/achievement icon as byte[] | MUST |
| 10 | Report + PDF + master-detail | PDF Library/User report; User -> Games | SHOULD |
| 11 | Parallel tasks / threads | parallel downloads/hash/analysis | SHOULD |
| 12 | Safe UI update from threads | progress bar via Dispatcher/IProgress | SHOULD |
| 13 | Lock/mutex | shared log writing with lock/SemaphoreSlim | SHOULD |
| 14 | Process A starts B | Desktop pokreće BackupTool.exe ili game exe | MUST |
| 15 | TCP client-server | MatchServer PING/PLAYER_COUNT | NICE |
| 16 | UDP client-server | DiscoveryServer broadcast | NICE |
| 17 | HTTP client download | download ZIP from home lab server with progress/speed limit | MUST |
| 18 | External SOAP service | optional NumberConversion/TextCasing | OPTIONAL |
| 19 | Local SOAP service | skip unless needed | OPTIONAL |
| 20 | External REST service | ipify/restcountries/daily fun widget | NICE |
| 21 | Own REST service | GameVault.API with games/friends/status | SHOULD |
| 22 | REST auth/authorization | Admin vs User role for API | SHOULD |
| 23 | AES | encrypted backups | SHOULD |
| 24 | RSA | asymmetric crypto/signature | SHOULD |
| 25 | SHA-256 | password/file hash | MUST |
| 26 | Digital signature | sign exported library.json, verify import | SHOULD |
| 27 | Static/class library | GameVault.Core/Crypto/Reports | MUST |
| 28 | Dynamic DLL | plugin DLL or class library DLL usage | SHOULD |
| 29 | DLL dialog | optional WPF window from plugin DLL | OPTIONAL |
| 30 | Resource in DLL | embedded achievement icons/logo | OPTIONAL |

---

## 6. Milestone plan

## Milestone 1 — Empty WPF shell

### Goal

Aplikacija se pokreće i ima osnovnu navigaciju.

### Tasks

- Create solution
- Create `GameVault.Desktop`
- Create `GameVault.Core`
- Create `GameVault.Data`
- Add project references
- Add MainWindow
- Add HomeView
- Add GamesView
- Add SettingsView
- Add basic navigation

### Codex prompt

```txt
Implementiraj minimalni WPF MVVM shell za GameVault.
Projekti su GameVault.Desktop, GameVault.Core i GameVault.Data.
Dodaj MainWindow s navigacijom na HomeView, GamesView i SettingsView.
Nemoj dodavati bazu još.
Nakon implementacije objasni View, ViewModel, Binding i Command.
```

### Done when

- app se pokrene
- mogu kliknuti Home/Games/Settings
- nema baze još

---

## Milestone 2 — Core models

### Goal

Dodati osnovne domain klase.

### Tasks

- Add User
- Add Game
- Add Genre
- Add Achievement
- Add ActivityLog

### Codex prompt

```txt
Dodaj osnovne domain modele u GameVault.Core: User, Game, Genre, Achievement, UserAchievement i ActivityLog.
Svaka klasa treba imati minimalno 2 atributa i barem 2 smislene metode gdje ima smisla.
Kod mora biti jednostavan za objasniti na obrani.
```

### Done when

- modeli postoje
- build prolazi

---

## Milestone 3 — SQLite + EF Core

### Goal

Lokalna baza radi.

### Tasks

- Add EF Core packages
- Add GameVaultDbContext
- Add DbSets
- Add seed data
- Configure SQLite

### Codex prompt

```txt
Dodaj EF Core + SQLite u GameVault.Data.
Napravi GameVaultDbContext s tablicama Users, Games, Genres, Achievements, UserAchievements i ActivityLogs.
Dodaj seed podatke za 3 igre i 2 žanra.
Poveži Desktop s Data projektom dovoljno da GamesView može prikazati igre iz baze.
Objasni što je DbContext i zašto je u zasebnom projektu.
```

### Done when

- baza se kreira
- GamesView prikazuje igre iz SQLite baze

---

## Milestone 4 — Game CRUD

### Goal

Dodavanje, uređivanje i brisanje igara.

### Tasks

- Add GameService
- Add AddGame dialog/view
- Add EditGame dialog/view
- Delete game
- Validation

### Codex prompt

```txt
Implementiraj CRUD za igre u WPF aplikaciji.
Dodaj GameService, AddGameView i EditGameView.
Korisnik mora moći dodati, urediti i obrisati igru iz SQLite baze.
Nakon toga objasni kako WPF ViewModel komunicira sa servisom i bazom.
```

### Done when

- mogu dodati igru
- mogu editati igru
- mogu obrisati igru

---

## Milestone 5 — Search, filter, sort

### Goal

Game Library postaje korisna.

### Tasks

- search po title
- filter po genre
- sort by title/year/playtime
- calculated field display

### Codex prompt

```txt
Dodaj search, filter i sort u GamesView.
Search filtrira po naslovu igre.
Filter filtrira po žanru.
Sort može biti po naslovu, godini i ukupnom vremenu igranja.
Dodaj calculated display property za veličinu igre i status instalacije.
```

### Done when

- filteri rade
- sortiranje radi

---

## Milestone 6 — Add local EXE + Play button

### Goal

Launcher stvarno pokreće lokalne igre.

### Tasks

- browse `.exe`
- save path
- Play button
- Process.Start
- playtime tracking

### Codex prompt

```txt
Dodaj mogućnost da korisnik za igru odabere lokalnu .exe datoteku pomoću file pickera.
Spremi putanju u bazu.
Dodaj PLAY gumb koji pokreće igru pomoću Process.Start.
Ako je moguće, prati koliko je proces trajao i spremi playtime.
Objasni kako ova funkcionalnost demonstrira komunikaciju s drugim procesom.
```

### Done when

- mogu dodati path do igre/appa
- Play pokrene exe
- activity log zapiše pokretanje

---

## Milestone 7 — Login + profile

### Goal

Korisnici i profil.

### Tasks

- Register/Login
- Password hash
- current user state
- ProfileView
- avatar upload

### Codex prompt

```txt
Implementiraj lokalni login/register sustav.
Koristi SHA-256 hash servis za lozinke za sada.
Dodaj ProfileView s avatarom, bio, country i favorite game.
Avatar spremi kao BLOB u bazu.
Nakon implementacije objasni zašto se lozinka ne sprema kao plain text.
```

### Done when

- register radi
- login radi
- profil se može urediti
- avatar se sprema/prikazuje

---

## Milestone 8 — Settings INI + localization

### Goal

Postavke i dva jezika.

### Tasks

- settings.ini
- language HR/ENG
- theme setting
- default download folder
- speed limit

### Codex prompt

```txt
Dodaj SettingsService koji čita i piše settings.ini.
Postavke: Language, Theme, DefaultDownloadFolder i DownloadSpeedLimit.
Dodaj osnovnu HR/ENG lokalizaciju za Login, Dashboard, Games, Profile i Settings ekran.
Objasni kako se postavke spremaju i učitavaju.
```

### Done when

- postavke se spremaju nakon restartanja appa
- HR/ENG radi barem osnovno

---

## Milestone 9 — Download manager from home lab

### Goal

Skidanje ZIP igre s home lab servera.

### Tasks

- DownloadUrl field
- HttpClient download
- progress bar
- cancellation
- speed limit
- unzip

### Codex prompt

```txt
Implementiraj DownloadManager.
Za igru koja ima DownloadUrl, korisnik može kliknuti Install.
Aplikacija skida ZIP preko HttpClienta, prikazuje progress bar, podržava cancel i jednostavni speed limit.
Nakon skidanja raspakira ZIP u DefaultDownloadFolder i označi igru kao installed.
Objasni kako radi async download i progress update u WPF-u.
```

### Done when

- mogu skinuti ZIP s home lab HTTP servera
- progress bar radi
- ZIP se raspakira

---

## Milestone 10 — Activity feed + achievements

### Goal

Aplikacija dobiva Steam-like feeling.

### Tasks

- ActivityLogService
- feed on dashboard
- AchievementService
- unlock rules

### Codex prompt

```txt
Dodaj Activity Feed i Achievement sustav.
Activity feed prikazuje zadnje aktivnosti korisnika.
Achievementi: First Launch, Collector, First Download, First Backup.
Dodaj AchievementService koji provjerava i otključava achievemente nakon relevantnih akcija.
```

### Done when

- logovi se prikazuju
- achievement se otključa

---

## Milestone 11 — JSON/XML import/export

### Goal

Datotečni import/export.

### Tasks

- export games to JSON
- import games from JSON
- export achievements to XML
- import achievements from XML
- edit/delete from file demonstration

### Codex prompt

```txt
Dodaj ImportExportService.
Implementiraj export i import game libraryja u JSON.
Implementiraj export i import achievementa u XML.
Dodaj jednostavne akcije za čitanje, pisanje, uređivanje i brisanje zapisa u tim datotekama.
Objasni razliku između JSON i XML pristupa.
```

### Done when

- export JSON radi
- import JSON radi
- XML achievement export/import radi

---

## Milestone 12 — Custom .gvault binary backup

### Goal

Vlastiti binarni format.

### Tasks

- header class
- record class
- binary writer
- binary reader

### Codex prompt

```txt
Implementiraj vlastiti binarni backup format .gvault.
Dodaj GameVaultBinaryHeader i GameVaultBinaryRecord klase.
Dodaj GameVaultBinaryService s WriteBackup i ReadBackup metodama.
Format mora imati magic string, version, createdAt i broj zapisa.
Objasni kako BinaryWriter i BinaryReader rade.
```

### Done when

- mogu exportati `.gvault`
- mogu importati `.gvault`

---

## Milestone 13 — Crypto DLL

### Goal

Crypto funkcionalnosti.

### Tasks

- GameVault.Crypto project
- SHA-256 file hash
- AES backup encrypt/decrypt
- RSA sign/verify

### Codex prompt

```txt
Dodaj GameVault.Crypto class library.
Implementiraj HashService za SHA-256 string/file hash.
Implementiraj AesService za šifriranje i dešifriranje backup datoteka.
Implementiraj RsaService za digitalni potpis i verifikaciju exportiranog JSON-a.
Poveži Desktop s Crypto DLL-om.
Objasni razliku između SHA-256, AES i RSA.
```

### Done when

- file hash radi
- AES encrypt/decrypt radi
- JSON sign/verify radi

---

## Milestone 14 — PDF reports

### Goal

Izvještaji.

### Tasks

- GameVault.Reports
- library report
- user activity report
- master-detail report

### Codex prompt

```txt
Dodaj GameVault.Reports class library.
Implementiraj PDF izvještaj za library korisnika: igre, žanr, installed status i playtime.
Dodaj master-detail izvještaj User -> Installed Games -> Achievements.
Poveži s Desktop aplikacijom kroz Generate Report gumb.
```

### Done when

- PDF se generira
- PDF se može otvoriti

---

## Milestone 15 — REST API

### Goal

Vlastiti REST servis.

### Tasks

- GameVault.API
- controllers
- games endpoint
- friends endpoint
- status endpoint
- Desktop HttpClient

### Codex prompt

```txt
Dodaj GameVault.API kao ASP.NET Core Web API projekt.
Implementiraj REST resurse /api/games, /api/friends i /api/status.
Za games dodaj GET, POST, PUT i DELETE.
Za status dodaj GET /api/status/time i GET /api/status/health.
Poveži Desktop aplikaciju da može pozvati GET /api/games i prikazati online katalog.
Objasni što je REST i zašto je API zaseban projekt.
```

### Done when

- API se pokrene
- Swagger radi ako je dodan
- Desktop pročita igre s API-ja

---

## Milestone 16 — REST auth/authorization

### Goal

Role-based access.

### Tasks

- token auth/JWT
- Admin role
- User role
- protect endpoints

### Codex prompt

```txt
Dodaj autentifikaciju i autorizaciju u GameVault.API.
Dovoljno je jednostavno JWT rješenje.
Admin može dodavati/brisati igre iz online kataloga.
User može samo čitati katalog i upravljati friend listom.
Dodaj dva testna korisnika: admin i user.
Objasni razliku između autentifikacije i autorizacije.
```

### Done when

- admin može POST/DELETE games
- user ne može POST/DELETE games

---

## Milestone 17 — Friend list

### Goal

Simple social feature.

### Tasks

- local or API friend list
- requests
- accept/reject
- show friends

### Codex prompt

```txt
Implementiraj Friend List.
Korisnik može poslati friend request drugom korisniku, prihvatiti zahtjev i vidjeti popis prijatelja.
Ako je API već dostupan, koristi REST endpoint /api/friends.
Ako nije, implementiraj lokalno u SQLite, ali ostavi strukturu da se kasnije može prebaciti na API.
```

### Done when

- friend request radi
- accepted friends se prikazuju

---

## Milestone 18 — BackupTool process

### Goal

Drugi proces.

### Tasks

- GameVault.BackupTool console app
- command-line args
- exit codes
- Desktop launches it

### Codex prompt

```txt
Dodaj GameVault.BackupTool console app.
Prima argumente --source i --output.
Radi backup SQLite baze ili game library podataka u .gvault format.
Vraća exit code 0 ako je uspješno, 1 ako je greška.
Desktop aplikacija ga pokreće pomoću Process.Start i prikazuje rezultat korisniku.
```

### Done when

- Desktop pokrene BackupTool
- BackupTool vrati status

---

## Milestone 19 — TCP MatchServer

### Goal

TCP komunikacija.

### Tasks

- TcpListener
- TcpClient in Desktop
- commands

### Codex prompt

```txt
Dodaj GameVault.MatchServer console app.
Server koristi TcpListener.
Podržava komande PING, PLAYER_COUNT i GAME_STATUS <name>.
Desktop aplikacija ima mali test ekran koji se spoji na server i prikaže odgovor.
Objasni TCP client-server komunikaciju.
```

### Done when

- PING -> PONG radi
- Desktop prikaže player count

---

## Milestone 20 — UDP DiscoveryServer

### Goal

UDP komunikacija.

### Tasks

- UdpClient server
- Desktop broadcast
- response

### Codex prompt

```txt
Dodaj GameVault.DiscoveryServer console app.
Server sluša UDP poruke.
Ako primi DISCOVER_GAMEVAULT, vraća GAMEVAULT_SERVER_FOUND s IP i portom.
Desktop aplikacija ima Discover Server gumb koji šalje UDP broadcast i prikaže rezultat.
Objasni razliku između TCP i UDP.
```

### Done when

- Discovery radi na lokalnoj mreži ili localhostu

---

## Milestone 21 — Threading + lock demo

### Goal

Dretve i zaključavanje.

### Tasks

- parallel hash
- parallel downloads simulation
- shared log file lock

### Codex prompt

```txt
Dodaj Diagnostics/Threading demo ekran.
Pokreni tri paralelna zadatka: hashiranje datoteka, analiza playtime statistike i simulirani download.
Prikazuj progress u UI-ju.
Dodaj shared log file u koji taskovi pišu uz lock ili SemaphoreSlim.
Objasni zašto je zaključavanje potrebno.
```

### Done when

- 3 taska rade paralelno
- UI se ne smrzava
- log file se ne korumpira

---

## Milestone 22 — Plugin DLL

### Goal

Dynamic DLL plugin.

### Tasks

- plugin interface
- plugin loader
- sample plugin

### Codex prompt

```txt
Dodaj jednostavni plugin sustav.
U GameVault.Core definiraj IGameVaultPlugin.
Desktop aplikacija učitava DLL-ove iz Plugins foldera pomoću Assembly.LoadFrom.
Napravi sample plugin GameVault.Plugin.Statistics koji vraća tekstualnu statistiku libraryja.
Objasni kako se DLL dinamički učitava.
```

### Done when

- plugin DLL se učita
- plugin rezultat se prikaže u UI-ju

---

## Milestone 23 — Polish / defense mode

### Goal

Projekt izgleda spremno za obranu.

### Tasks

- dashboard clean UI
- seed data
- demo data
- error handling
- loading states
- README
- defense checklist

### Codex prompt

```txt
Dodaj finalni polish za GameVault.
Poboljšaj UI bez velikog refaktora.
Dodaj seed/demo podatke.
Dodaj osnovni error handling.
Napravi README.md s uputama za pokretanje.
Napravi DEFENSE_CHECKLIST.md koji mapira implementirane funkcionalnosti na zahtjeve kolegija.
```

### Done when

- možeš pokrenuti demo od početka do kraja
- znaš pokazati svaku funkcionalnost

---

## 7. Minimalni MVP ako vrijeme krene curiti

Ako ostane malo vremena, fokus na ovo:

```txt
1. WPF app shell
2. SQLite + EF Core
3. Game CRUD
4. Add local EXE + Play
5. Login + SHA-256
6. Cover image BLOB
7. Search/filter/sort
8. JSON export/import
9. PDF report
10. Download ZIP with progress
11. Crypto class library
12. BackupTool process
```

To je već solidan projekt.

---

## 8. Feature prioritization

### MUST HAVE

- WPF desktop app
- SQLite/EF Core
- User/Game models
- Login/register
- Game CRUD
- Add local EXE
- Play button
- Search/filter/sort
- Cover image BLOB
- JSON/XML import/export
- SHA-256
- HTTP ZIP download
- Activity logs

### SHOULD HAVE

- Profile page
- Achievements
- PDF reports
- AES encrypted backup
- RSA digital signature
- INI settings
- HR/ENG localization
- REST API
- Friend list
- BackupTool process
- threading demo

### NICE TO HAVE

- TCP MatchServer
- UDP DiscoveryServer
- plugin DLL
- external REST API
- SOAP client
- resource DLL

### OPTIONAL / only if time

- local SOAP service
- DLL dialog
- complex auto-updater
- real multiplayer/chat

---

## 9. Suggested folders inside GameVault.Desktop

```txt
GameVault.Desktop
│
├── Views
│   ├── LoginView.xaml
│   ├── DashboardView.xaml
│   ├── GamesView.xaml
│   ├── GameDetailsView.xaml
│   ├── AddEditGameView.xaml
│   ├── ProfileView.xaml
│   ├── FriendsView.xaml
│   ├── AchievementsView.xaml
│   ├── DownloadsView.xaml
│   ├── ReportsView.xaml
│   └── SettingsView.xaml
│
├── ViewModels
│   ├── LoginViewModel.cs
│   ├── MainViewModel.cs
│   ├── DashboardViewModel.cs
│   ├── GamesViewModel.cs
│   ├── GameDetailsViewModel.cs
│   ├── ProfileViewModel.cs
│   ├── FriendsViewModel.cs
│   ├── AchievementsViewModel.cs
│   ├── DownloadsViewModel.cs
│   ├── ReportsViewModel.cs
│   └── SettingsViewModel.cs
│
├── Services
│   ├── NavigationService.cs
│   ├── DialogService.cs
│   ├── FilePickerService.cs
│   └── ThemeService.cs
│
├── Resources
│   ├── Strings.hr.resx
│   ├── Strings.en.resx
│   └── Styles.xaml
│
└── App.xaml
```

---

## 10. Suggested Core classes

```txt
GameVault.Core
│
├── Models
│   ├── User.cs
│   ├── Game.cs
│   ├── Genre.cs
│   ├── Achievement.cs
│   ├── UserAchievement.cs
│   ├── Friendship.cs
│   ├── ActivityLog.cs
│   ├── GameSession.cs
│   └── AppSetting.cs
│
├── DTOs
│   ├── GameDto.cs
│   ├── UserProfileDto.cs
│   ├── FriendDto.cs
│   └── LoginResultDto.cs
│
├── Interfaces
│   ├── IGameService.cs
│   ├── IUserService.cs
│   ├── IAchievementService.cs
│   ├── IActivityLogService.cs
│   ├── IImportExportService.cs
│   └── IGameVaultPlugin.cs
│
└── Enums
    ├── UserRole.cs
    ├── FriendshipStatus.cs
    ├── GameInstallStatus.cs
    └── ActivityLogType.cs
```

---

## 11. Suggested Data classes

```txt
GameVault.Data
│
├── GameVaultDbContext.cs
│
├── Repositories
│   ├── GameRepository.cs
│   ├── UserRepository.cs
│   ├── AchievementRepository.cs
│   └── ActivityLogRepository.cs
│
├── Services
│   ├── GameService.cs
│   ├── UserService.cs
│   ├── AuthService.cs
│   ├── AchievementService.cs
│   └── ActivityLogService.cs
│
└── Migrations
```

---

## 12. Demo scenario for defense

Na obrani pokazati ovim redom:

1. Pokrenuti GameVault Desktop
2. Login kao demo korisnik
3. Pokazati HR/ENG switch
4. Otvoriti Game Library
5. Search/filter/sort igre
6. Dodati novu lokalnu igru s `.exe` pathom
7. Kliknuti PLAY i pokrenuti vanjski proces
8. Pokazati activity log
9. Pokazati profile + avatar iz BLOB-a
10. Pokazati achievement unlock
11. Export library u JSON
12. Export achievements u XML
13. Napraviti `.gvault` binary backup
14. Šifrirati backup AES-om
15. Potpisati export RSA digitalnim potpisom
16. Generirati PDF report
17. Skinuti ZIP s home lab servera uz progress bar
18. Pozvati REST API online catalog
19. Demonstrirati role: Admin može dodati igru, User ne može
20. Pokrenuti BackupTool kao drugi proces
21. Ako ima vremena: TCP PING/PONG
22. Ako ima vremena: UDP discovery
23. Ako ima vremena: plugin DLL

---

## 13. Objašnjenja za obranu

### Što je GameVault?

GameVault je desktop aplikacija za osobnu kolekciju igara. Korisnik može evidentirati igre, dodati lokalni executable path, pokretati igre iz aplikacije, pratiti vrijeme igranja, skidati testne ZIP pakete, generirati izvještaje i raditi backup podataka.

### Zašto više projekata u solutionu?

Zato da se odvoje odgovornosti:

- Desktop je korisničko sučelje
- Core sadrži modele i pravila
- Data radi s bazom
- Crypto sadrži sigurnosne funkcije
- API je zaseban REST servis
- BackupTool je zaseban proces
- MatchServer/DiscoveryServer demonstriraju mrežnu komunikaciju

### Što je REST?

REST je način da desktop aplikacija komunicira sa serverom preko HTTP zahtjeva. Desktop šalje zahtjev, npr. `GET /api/games`, a server vraća JSON podatke.

### Što je DLL?

DLL je biblioteka koda koju koristi glavna aplikacija. U ovom projektu `GameVault.Crypto.dll` sadrži AES/RSA/SHA funkcije, a plugin DLL-ovi se mogu učitati dinamički.

### Zašto SQLite?

SQLite je lokalna baza koja je idealna za desktop aplikacije jer ne zahtijeva poseban server, a i dalje omogućava relacijske tablice, upite, CRUD i BLOB polja.

### Zašto WPF?

WPF omogućava modernije desktop sučelje, binding podataka i MVVM arhitekturu, što odvaja UI od poslovne logike.

---

## 14. Anti-feature-creep pravila

Ne dodavati ove stvari dok osnovna aplikacija ne radi:

- chat
- voice chat
- pravi multiplayer
- pravi payment/store
- kompleksni auto-updater
- online account sustav s produkcijskom sigurnošću
- cloud sync koji mora raditi preko Interneta

Ovo je studentski projekt. Cilj je demonstrirati funkcionalnosti, ne napraviti pravi Steam.

---

## 15. Finalni savjet za Codex workflow

Za svaki milestone koristi ovakav prompt:

```txt
Radimo GameVault prema GAMEVAULT_CODEX_ROADMAP.md.
Trenutni milestone je: [IME MILESTONEA].
Pregledaj postojeći kod.
Napravi samo promjene potrebne za ovaj milestone.
Nemoj refaktorirati nepovezane dijelove.
Nakon promjena napiši:
1. Koje datoteke si promijenio
2. Kako testirati
3. Što trebam razumjeti za obranu
```

---

## 16. Prvi prompt koji trebaš dati Codexu

```txt
Pregledaj ovaj roadmap i pomozi mi krenuti od Milestone 1.
Želim napraviti GameVault u C# WPF-u koristeći .NET 8, MVVM, SQLite i EF Core.
Za sada napravi samo solution structure i minimalni WPF shell s navigacijom Home/Games/Settings.
Nemoj dodavati bazu, REST, crypto ni download manager još.
Nakon implementacije objasni mi sve kao junior developeru koji prvi put radi WPF.
```

