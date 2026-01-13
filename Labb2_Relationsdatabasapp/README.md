# MusicLibrary (WPF / .NET 10)

En enkel WPF-applikation för att hantera ett musikbibliotek (artister, album, låtar och spellistor). Applikationen använder EF Core och en lokal SQL Server-databas.

## Förutsättningar

- Visual Studio 2022 (med workload **.NET desktop development**)
- .NET SDK som kan rikta mot **.NET 10**
- SQL Server LocalDB (alternativt SQL Server Express/Developer)
- (valfritt) SQL Server Management Studio (SSMS)

## 1) Klona repot
git clone https://github.com/MS-Flow/MusicLibrary.git cd MusicLibrary

## 2) Återställ databasen från `.bak`

Du ska även bifoga/ta emot en databasbackup (t.ex. `MusicDb.bak`). Den behöver återställas i din lokala SQL Server-instans.

1. Öppna SQL Server Management Studio
2. Anslut till `(localdb)\MSSQLLocalDB` (eller din egen SQL Server-instans)
3. Högerklicka på **Databases** → **Restore Database...**
4. Välj `.bak`-filen och kör restore

## 3) Konfigurera anslutningssträngen (obligatoriskt)

Applikationen letar efter en connection string med namnet `MusicDb`. Den kan läsas från:

- `appsettings.json` (valfritt), eller
- **User Secrets** (i Debug), eller
- miljövariabler

### User Secrets

I Visual Studio:

1. Högerklicka projektet → **Manage User Secrets**
2. Lägg in:
{ "ConnectionStrings": {
"MusicDb": "Server=localhost,1433;Database=everyloop;TrustServerCertificate=True;Integrated security=True;MultipleActiveResultSets=True" } }

## 4) Starta applikationen

- Öppna lösningen i Visual Studio
- Sätt startup project till `MusicLibrary`
- Tryck **Start** / **F5**

## Tangentbordsgenvägar

- `F1` Skapa spellista
- `F2` Ta bort spellista
- `F3` Lägg till (artist/album/låt)
- `F4` Redigera vald artist (endast namn)
- `F5` Ta bort vald artist (inkl. album/låtar)
- `F6` Lägg till album/låt till vald artist
- `F7` Redigera vald låt (namn och längd)

- `F11` Fullskärm av/på
- `F12` Avsluta

## Felsökning

- Om du får fel om att connection string `MusicDb` saknas: kontrollera steg 3.
- Om programmet inte hittar databasen: kontrollera att databasen faktiskt heter `MusicDb` (eller uppdatera `Database=...` i connection string).
