# GameVault - Functionality Matrix

## Legenda

- [x] Implementirano i demonstrabilno
- [~] Djelomicno implementirano
- [ ] Nije implementirano
- [-] Opcionalno / preskace se

## Trenutno stanje projekta

- WPF desktop aplikacija na .NET 10
- MVVM arhitektura
- Projekti: `GameVault.Desktop`, `GameVault.Core`, `GameVault.Data`, `GameVault.Api`, `GameVault.DiscoveryServer`, `GameVault.BackupTool`, `GameVault.ManifestSigner`
- SQLite + EF Core lokalna baza za desktop podatke
- SQLite + EF Core server baza za REST API
- Navigacija: Home, Library, Catalog, Profile, Friends, Account, Reports, Settings
- Game CRUD, search, genre filter, sorting, launcher i cover slike
- Server catalog preko HTTP manifesta
- Catalog cover slike se ucitavaju iz `coverUrl` ili fallback putanje `/files/covers/{catalogId}.png`
- Download manager za ZIP igre s progressom, cancellationom, speed limitom, SHA256 provjerom i unzip installom
- RSA potpisani download manifest mora biti verificiran prije prikaza Cataloga
- Privremeni ZIP se brise nakon uspjeha, otkazivanja ili neuspjele instalacije
- Catalog instalacije podrzavaju sigurno brisanje fileova samo unutar GameVault install foldera
- Lokalni korisnicki profil s avatarom spremljenim kao BLOB
- Osnovni achievement sustav
- Statistika biblioteke i PDF report
- INI postavke za accent boju, jezik, install folder i download speed limit
- JSON game library transfer, XML readable Activity History export/open/read
- Custom `.gvactivity` compact binary Activity Archive
- Secure `.gvb` backup s AES-GCM enkripcijom
- Interna SHA256 provjera integriteta secure backupa
- Recent Activity feed na Home ekranu
- Online account login/register, JWT session, role i friends flow
- Public stats sync prema REST API-ju

---

# Professor Requirement Matrix

## 1. Korisnicke klase

- [x] `Game`
- [x] `UserProfile`
- [x] `Achievement`
- [x] `UserAchievement`
- [x] `ActivityLog`
- [x] Server modeli: `AppUser`, `Friendship`, `PublicGameStats`
- [x] Repository i service klase s metodama

Status: **Implementirano**

## 2. Dijalozi / forme i komunikacija

- [x] MainWindow
- [x] Home View
- [x] Library View
- [x] Catalog View
- [x] Add/Edit Game dialog
- [x] Profile View
- [x] Friends View
- [x] Account/Auth View
- [x] Reports View
- [x] Settings View
- [x] Password dialog za secure backup
- [x] Prijenos podataka kroz ViewModel, binding i commands

Status: **Implementirano**

## 3. Lokalizacija

- [x] Hrvatski resource file
- [x] English resource file / fallback
- [x] Settings omogućuje izbor jezika
- [x] Language preference se sprema u INI
- [x] UI se prikazuje na odabranom jeziku
- [x] Prevedeni su glavni ekrani, gumbi, dialog poruke i statusi

Status: **Implementirano**

## 4. INI / Registry postavke

- [x] Accent color
- [x] Language preference
- [x] Default install folder
- [x] Download speed limit
- [x] Ucitavanje postavki pri pokretanju
- [x] `Documents/GameVault/settings.ini`

Status: **Implementirano**

## 5. JSON / XML

- [x] JSON = Game Library transfer
- [x] JSON export biblioteke
- [x] JSON import biblioteke
- [x] XML = readable Activity History export/open/read
- [x] XML export ActivityLog zapisa za review/analysis
- [x] Open Activity History cita XML, validira strukturu i prikazuje summary/preview
- [x] XML se zapisuje i cita
- [x] JSON i XML koriste razlicite strukture podataka
- [x] XML radi nad kolekcijom ActivityLog zapisa
- [x] Validacija neispravne datoteke bez rusenja aplikacije
- [x] JSON import preskace duplikate po naslovu i godini

Status: **Implementirano**

## 6. Vlastiti binarni format

- [x] `.gvactivity` = custom compact binary Activity Archive
- [x] BinaryWriter / BinaryReader format
- [x] Header magic string `"GVACT"`
- [x] Format version
- [x] Multiple ActivityLog records
- [x] UI prikazuje format kao Activity Archive, ne kao tehnicki BinaryWriter feature
- [x] Create Activity Archive sprema vise ActivityLog zapisa
- [x] Open Activity Archive cita header i prikazuje summary bez mergeanja u bazu
- [x] Validacija provjerava magic/version i odbija neispravne datoteke
- [x] `.gvb` = encrypted secure backup, odvojen od `.gvactivity`
- [x] AES-GCM payload sa saltom, nonceom i hashom

Status: **Implementirano**

## 7. Baza podataka

- [x] Desktop SQLite baza `gamevault.db`
- [x] API SQLite baza
- [x] Entity Framework Core
- [x] `AppDbContext`
- [x] `ServerDbContext`
- [x] `DbSet<Game>`, `ActivityLog`, `UserProfile`, `Achievement`, `UserAchievement`
- [x] `DbSet<AppUser>`, `Friendship`, `PublicGameStats`
- [x] Automatsko kreiranje tablica
- [x] Seed za desktop igre/profil/achievemente
- [x] Seed za server admin/demo podatke
- [x] CRUD nad igrama

Status: **Implementirano**

## 8. Search / filter / calculated fields

- [x] Search igara po naslovu
- [x] Filter po zanru
- [x] Sortiranje po naslovu
- [x] Sortiranje po godini izlaska
- [x] Sortiranje po playtimeu
- [x] Sortiranje po datumu dodavanja
- [x] Sortiranje radi zajedno sa searchom i filterom
- [x] Ukupan broj igara
- [x] Ukupan playtime
- [x] Formatirani playtime (`0 min`, `1 h 20 min`, `42 h`)
- [x] Najcesci zanr
- [x] Najnovije dodana igra
- [x] Broj otkljucanih achievementa

Status: **Implementirano**

## 9. BLOB

- [x] Avatar se ucitava kao `byte[]`
- [x] Avatar se sprema u SQLite `BLOB` stupac
- [x] Avatar se ponovno ucitava i prikazuje u WPF-u
- [~] Cover slike koriste `CoverImagePath` / HTTP URL, nisu spremljene kao BLOB

Status: **Implementirano za avatar**

## 10. Report

- [x] Reports View
- [x] Statistika biblioteke
- [x] PDF generiranje pomocu QuestPDF-a
- [x] Profil korisnika u PDF-u
- [x] Datum generiranja
- [x] Tablica igara
- [x] Playtime podaci
- [x] Popis i status achievementa
- [x] Spremanje u `Documents/GameVault/Reports`

Status: **Implementirano**

## 11. Threading

- [x] Download se pokrece u background tasku
- [x] Async HTTP download
- [x] Paralelni refresh friends podataka (`Task.WhenAll`)
- [x] Stats sync koristi async service

Status: **Implementirano**

## 12. Sigurno azuriranje UI-ja iz dretvi

- [x] `IProgress<DownloadState>` za download progress
- [x] Dispatcher update u `CatalogViewModel`
- [x] Dispatcher update u `MainWindowViewModel` za download state
- [x] UI prikazuje status, progress, speed i greske downloada

Status: **Implementirano**

## 13. Sinkronizacija

- [x] `SemaphoreSlim` ogranicava paralelne downloade
- [x] `ConcurrentDictionary` prati aktivne downloade i cancel tokene
- [x] `SemaphoreSlim` lock za public stats sync

Status: **Implementirano**

## 14. Procesi

- [x] GameVault pokrece lokalni `.exe` pomocu `Process.Start`
- [x] Provjera postoji li executable
- [x] Jasna poruka za neispravan path
- [x] Activity log nakon uspjesnog pokretanja
- [x] Zasebni `GameVault.BackupTool.exe` i povratni exit code
- [x] `GameVault.Desktop` pokrece vanjski proces `GameVault.BackupTool.exe`
- [x] Desktop procesu prosljeduje path do `.gvb` datoteke kroz argumente
- [x] Desktop salje password kroz redirected `StandardInput`
- [x] Desktop cita `StandardOutput`/`StandardError` i `ExitCode`
- [x] `ExitCode` se koristi za prikaz rezultata korisniku

Status: **Implementirano**

## 15. TCP

- [x] `GameVault.Desktop` kao TCP klijent
- [x] `GameVault.DiscoveryServer` kao zasebna server aplikacija
- [x] TCP se koristi za pouzdan dohvat statusa i konfiguracije servera
- [x] Desktop salje zahtjev `GAMEVAULT_STATUS_V1`
- [x] DiscoveryServer vraca JSON status/config s `apiBaseUrl`, `filesBaseUrl` i `manifestUrl`
- [x] Network Diagnostics prikazuje TCP status/config bez brisanja zadnjih valjanih URL-ova pri gresci
- [x] Ovo nije chat, calculator, screenshot ili file transfer primjer

Status: **Implementirano**

## 16. UDP

- [x] `GameVault.Desktop` salje UDP broadcast na port 47891
- [x] `GameVault.DiscoveryServer` odgovara s IP adresom servera i TCP portom
- [x] UDP se koristi samo za LAN discovery
- [x] Nakon UDP odgovora Desktop koristi TCP za detaljnu konfiguraciju
- [x] Network Diagnostics prikazuje LAN server IP pronadjen preko UDP DiscoveryServera
- [x] Ovo nije chat, calculator, screenshot ili file transfer primjer

Status: **Implementirano**

## 17. HTTP

- [x] HTTP dohvat download manifesta
- [x] HTTP prikaz cover slika u Catalogu
- [x] Fallback cover URL `/files/covers/{catalogId}.png`
- [x] Download ZIP datoteka
- [x] Progress, speed prikaz i speed limit
- [x] Cancel downloada
- [x] SHA256 provjera skinutog ZIP-a

Status: **Implementirano**

## 18. Online SOAP

- [-] Opcionalno

## 19. Lokalni SOAP

- [-] Opcionalno

## 20. Vanjski REST servis

- [x] `GameVault.Desktop` koristi vanjski REST API ipify
- [x] Endpoint: `https://api.ipify.org?format=json`
- [x] `HttpClient` GET zahtjev
- [x] JSON response se deserijalizira u `PublicIpResponse` DTO
- [x] Public client IP prikazuje se u Network Diagnostics panelu
- [x] Implementirano je rukovanje greskama ako vanjski servis nije dostupan

Status: **Implementirano**

## 21. Vlastiti REST servis

- [x] `GameVault.Api`
- [x] Swagger
- [x] SQLite server baza
- [x] Auth endpoints
- [x] Profile endpoints
- [x] Friends endpoints
- [x] Public stats endpoints
- [x] Admin users endpoint

Status: **Implementirano**

## 22. Autentifikacija i autorizacija

- [x] Register
- [x] Login
- [x] Logout na desktopu
- [x] Password hashing
- [x] JWT token
- [x] Bearer auth
- [x] Role u tokenu
- [x] Admin-only endpoint

Status: **Implementirano**

## 23. AES

- [x] AES-GCM sifriranje secure `.gvb` backupa
- [x] AES ostaje odvojen od javnog download manifesta; manifest se potpisuje, ne enkriptira
- [x] AES-GCM desifriranje pri restoreu
- [x] Kljuc izveden iz lozinke pomocu PBKDF2-SHA256
- [x] Slucajni salt i nonce
- [x] Lozinka se unosi kroz Password dialog
- [x] Pogresna lozinka ili ostecen backup prekidaju restore

Status: **Implementirano**

## 24. RSA

- [x] `GameVault.ManifestSigner` generira RSA private/public key pair
- [x] Private key potpisuje originalne byteove `downloads.json`
- [x] Desktop sadrzi samo public key i verificira manifest
- [x] RSA-SHA256 s PKCS#1 v1.5 paddingom koristi se jednako za sign/verify
- [x] Neispravan ili nedostajuci potpis blokira Catalog i download

Status: **Implementirano za potpisani server manifest**

## 25. SHA-256

- [x] SHA256 hash originalnog JSON payload-a u secure backupu
- [x] Hash je spremljen unutar AES-enkriptiranog `.gvb` sadrzaja
- [x] Automatska provjera nakon dekripcije i prije importa
- [x] SHA256 provjera ZIP downloada iz server manifesta
- [x] SHA256 se koristi unutar RSA potpisa manifesta, odvojeno od ZIP hash provjere
- [x] Neuspjesna provjera integriteta blokira import/install
- [x] Plain JSON/XML/`.gvactivity` namjerno nema SHA256

Status: **Implementirano**

## 26. Digitalni potpis

- [x] `downloads.json.sig` je odvojeni digitalni potpis server manifesta
- [x] Desktop verificira potpis prije JSON parsiranja i prikaza Cataloga
- [x] Promijenjen manifest bez novog potpisa odbija se kao nepouzdan

Status: **Implementirano za download manifest**

## 27. Staticka biblioteka

- [x] `GameVault.Core`
- [x] `GameVault.Data`
- [x] Dijeljeni modeli i data/repository sloj

Status: **Implementirano**

## 28. Dinamicka biblioteka

- [ ] Plugin DLL

Status: **Nije implementirano**

## 29. DLL dijalog

- [ ] Plugin management prozor

Status: **Nije implementirano**

## 30. Resource DLL

- [ ] Ikone, slike ili prijevodi iz zasebnog DLL-a

Status: **Nije implementirano**

---

# Implementirani milestoneovi

## Milestone 0 - Solution i struktura

- [x] Solution struktura
- [x] Desktop, Core, Data i API projekti
- [x] Ispravne project reference
- [x] .NET 10 i WPF konfiguracija

## Milestone 1 - WPF shell

- [x] Moderni dark layout
- [x] Sidebar
- [x] Home
- [x] Library
- [x] Catalog
- [x] Profile
- [x] Friends
- [x] Account
- [x] Reports
- [x] Settings
- [x] Jednostavna MVVM navigacija
- [x] View, ViewModel, Binding i Command pristup

## Milestone 2 - SQLite i Game CRUD

- [x] EF Core + SQLite
- [x] Lokalna baza
- [x] Seed od tri igre
- [x] Add Game
- [x] Edit Game
- [x] Delete Game
- [x] Search po naslovu
- [x] Filter po zanru
- [x] Add/Edit dialog

## Milestone 3 - Launcher i cover slike

- [x] `ExecutablePath`
- [x] `InstallPath`
- [x] `CoverImagePath`
- [x] Browse executable
- [x] Browse install folder
- [x] Browse cover image
- [x] Cover prikaz i fallback placeholder
- [x] Catalog cover prikaz iz HTTP URL-a
- [x] Catalog cover fallback `/files/covers/{catalogId}.png`
- [x] Play button
- [x] `Process.Start`
- [x] Validacija executable patha
- [x] `Launched` activity log

## Milestone 4 - Profile, avatar BLOB i achievements

- [x] Lokalni user profile
- [x] Edit profila
- [x] Avatar upload
- [x] Avatar spremljen kao `byte[]` / SQLite BLOB
- [x] Avatar prikaz nakon ponovnog pokretanja
- [x] Achievement katalog
- [x] Locked / unlocked prikaz
- [x] `FIRST_GAME_ADDED`
- [x] `FIVE_GAMES_ADDED`
- [x] `FIRST_LAUNCH`

## Milestone 5 - Statistics i PDF report

- [x] Reports View
- [x] Reports ViewModel
- [x] Navigacija na Reports
- [x] Total games
- [x] Total playtime
- [x] Most common genre
- [x] Newest added game
- [x] Unlocked achievements count
- [x] Generate PDF Report
- [x] QuestPDF Community
- [x] Profil, statistika, tablica igara i achievementi u PDF-u
- [x] Automatsko spremanje u Documents

## Milestone 6 - Settings, import/export i crypto

- [x] Settings View
- [x] Settings ViewModel
- [x] Navigacija na Settings
- [x] Accent color postavka
- [x] HR/EN spremljena preferenca
- [x] Default install folder postavka
- [x] Download speed limit postavka
- [x] INI save/load pri startupu
- [x] JSON export i import
- [x] XML Activity History export/open/read
- [x] JSON i XML koriste razlicite modele podataka
- [x] Custom `.gvactivity` ActivityLog binary archive
- [x] BinaryWriter / BinaryReader create/open archive
- [x] Sigurno rukovanje neispravnim importom
- [x] Plain JSON/XML/`.gvactivity` export bez enkripcije i hash sidecara
- [x] Create Secure Backup
- [x] Restore Secure Backup
- [x] Check Backup File kao dodatna provjera kroz vanjski proces
- [x] GameVault `.gvb` ekstenzija
- [x] AES-GCM enkripcija secure backupa
- [x] Automatska SHA256 provjera internog JSON payload-a
- [x] Restore se prekida ako lozinka ili integrity provjera nisu valjani
- [x] Create/Restore ostaju u Desktop aplikaciji i nisu zamijenjeni vanjskim alatom
- [x] PBKDF2-SHA256 derivacija kljuca
- [x] Password dialog

## Milestone 6.5 - Activity Feed i Sorting Polish

- [x] Recent Activity na Home ekranu
- [x] Zadnjih 8 ActivityLog zapisa
- [x] Citljiv action, opis i timestamp
- [x] CRUD activity logovi
- [x] Launch activity log
- [x] Export i secure backup activity logovi
- [x] Sort by ComboBox u Libraryju
- [x] Title A-Z
- [x] Release Year
- [x] Play Time
- [x] Date Added
- [x] Sorting kompatibilan sa search/filterom
- [x] Formatirani playtime

## Milestone 7 - Server Catalog i Download Manager

- [x] Catalog View
- [x] HTTP manifest loader
- [x] Catalog item DTO
- [x] Install button
- [x] ZIP download
- [x] Progress percent
- [x] Download speed display
- [x] Download speed limit
- [x] Cancel download
- [x] `SemaphoreSlim` download queue
- [x] ZIP SHA256 integrity check
- [x] Extract install folder
- [x] Resolve executable from manifest
- [x] Download/store cover image
- [x] Catalog cover image loading from `/files/covers/{catalogId}.png`
- [x] Library state refresh after install/fail/cancel

## Milestone 8 - REST API, Auth i Friends

- [x] `GameVault.Api`
- [x] Swagger
- [x] JWT configuration
- [x] Register endpoint
- [x] Login endpoint
- [x] Desktop Account view
- [x] Auth session state
- [x] Profile endpoint
- [x] Public stats sync endpoint
- [x] Public stats lookup endpoint
- [x] Send friend request
- [x] List pending friend requests
- [x] Accept friend request
- [x] List friends with public stats
- [x] Remove friend
- [x] Admin-only users endpoint

## Milestone 9 - TCP/UDP Server Discovery

- [x] `GameVault.DiscoveryServer` zaseban console/worker-style projekt
- [x] UDP discovery server slusa `GAMEVAULT_DISCOVER_REQUEST` na portu 47891
- [x] UDP response vraca `serverName`, `serverIp` i `tcpStatusPort`
- [x] TCP status/config server slusa `GAMEVAULT_STATUS_V1` na portu 47892
- [x] TCP response vraca JSON s API URL-om, files URL-om, manifest URL-om, catalog summary i server time
- [x] Server IP i URL vrijednosti dolaze iz `appsettings.json`, environment varova ili fallback lokalnog IP-a
- [x] Desktop ima `ServerDiscoveryClient` / `IServerDiscoveryClient`
- [x] Settings ekran ima `Find GameVault Server`
- [x] Uspjesan TCP status/config automatski sprema `ApiBaseUrl`, `FilesBaseUrl` i `DownloadManifestUrl` u INI postavke
- [x] Neuspjesan TCP status ne mijenja postojece server postavke
- [x] ActivityLog biljezi discovery start/success/failure i settings update

## Milestone 10 - Signed Catalog i Safe Install Cleanup

- [x] `GameVault.ManifestSigner` generate-keys, sign i verify komande
- [x] RSA-SHA256 potpis `downloads.json.sig`
- [x] Desktop verificira originalne manifest byteove prije JSON parsiranja
- [x] Catalog prikazuje signature status i odbija missing/invalid signature
- [x] Desktop output sadrzi samo `public_key.pem`
- [x] ZIP se brise nakon success/cancel/failure toka
- [x] Neuspjeli extract cisti parcijalni install folder
- [x] Manual game delete nikada ne dira lokalne fileove
- [x] Catalog game nudi Library Only, Delete Files Too i Cancel
- [x] Rekurzivno brisanje dopusteno je samo za child folder DefaultInstallFoldera

## Milestone 11 - External REST i Network Diagnostics

- [x] UDP DiscoveryServer pronalazi LAN server IP
- [x] TCP DiscoveryServer vraca server config/status
- [x] Network Diagnostics panel prikazuje LAN server podatke i spremljene server URL-ove
- [x] Vanjski ipify REST servis vraca public client IP kroz `HttpClient` GET i JSON DTO
- [x] Diagnostics greske ne ruse UI niti brisu zadnje valjane server postavke

---

# Sljedece nije implementirano

- [x] Potpuna HR/ENG lokalizacija
- [ ] Plugin DLL i plugin management
- [ ] Resource DLL

---

# Prioriteti za obranu

1. Pokazati WPF MVVM navigaciju.
2. Pokazati SQLite tablice i Game CRUD.
3. Dodati igru, cover i executable path.
4. Pokrenuti lokalni program kroz Play.
5. Pokazati avatar kao pravi BLOB u bazi.
6. Pokazati otkljucane i zakljucane achievemente.
7. Otvoriti Reports statistiku.
8. Generirati i otvoriti PDF report.
9. Promijeniti accent, install folder ili speed limit i pokazati `settings.ini`.
10. Exportati i importati game library JSON.
11. Exportati i otvoriti readable Activity History XML report.
12. Kreirati i otvoriti Activity Archive `.gvactivity` te pokazati summary.
13. Kreirati secure `.gvb` backup s lozinkom.
14. Restoreati `.gvb` i pokazati da pogresna lozinka blokira import.
15. Objasniti SHA256 provjeru u secure restore toku i ZIP download toku.
16. Pokazati Recent Activity nakon dodavanja, uredjivanja ili pokretanja igre.
17. Kombinirati search, genre filter i sva cetiri Library sortiranja.
18. Otvoriti Catalog, prikazati covere, pokrenuti download i pratiti progress/speed.
19. Pokazati login/register, JWT session i Friends ekran.
