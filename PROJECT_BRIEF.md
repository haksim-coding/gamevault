# GameVault Project Brief

GameVault je studentski WPF desktop projekt u C#/.NET.

Koncept:
Personal game library + launcher inspiriran Steamom, Playniteom i GOG Galaxyjem.

Cilj:
Napraviti aplikaciju koja izgleda kao pravi desktop software, ali je dovoljno jednostavna da se može objasniti na obrani.

Glavna pravila:
- Koristi WPF + MVVM.
- Radi male milestoneove.
- Ne over-engineerati.
- Ne dodavati featuree koje korisnik nije tražio.
- Kod mora biti čitljiv i razumljiv za studentsku obranu.
- Nakon svake promjene objasni što je napravljeno i zašto.

Solution struktura:
- src/GameVault.Desktop - WPF UI
- src/GameVault.Core - modeli i business logika
- src/GameVault.Data - pristup podacima

Reference:
- GameVault.Desktop -> GameVault.Core
- GameVault.Desktop -> GameVault.Data
- GameVault.Data -> GameVault.Core

Kratkoročni cilj:
U 10 dana napraviti demonstrabilan projekt koji pokriva što više funkcionalnosti iz profesorovih uputa.

Trenutni prioriteti:
1. WPF layout i navigacija
2. SQLite baza
3. CRUD za igre
4. Pokretanje lokalnog .exe fajla
5. Cover slike / BLOB
6. User profile
7. Achievements
8. JSON/XML import/export
9. AES/SHA256
10. PDF report
11. Download ZIP-a s home lab servera
12. Minimalni REST API
13. Minimalni DLL/plugin demo