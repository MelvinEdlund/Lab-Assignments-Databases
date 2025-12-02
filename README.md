# Labb 1 – Relationsdatabas för Bokhandel

Detta repo innehåller lösningen för Labb 1 i kursen Relationsdatabaser. Projektet går ut på att designa och implementera en databas för en bokhandel med flera butiker.

## Innehåll
- SQL-skript för tabeller, relationer och constraints  
- Testdata för exempelposter  
- Vyer för rapportering och analys  
- ER-diagram över databasens struktur

## Beskrivning
Databasen är normaliserad och innehåller tabeller för bland annat böcker, författare, kategorier, butiker och lagersaldo. Relationerna hanteras via primär- och främmande nycklar. Vyer används för att presentera sammanslagen information om böcker, lagerstatus och butikernas utbud.

## Komma igång
1. Kör `create_tables.sql` i SQL Server.  
2. Kör `insert_data.sql` för att fylla databasen.  
3. Kör `views.sql` för att skapa vyerna.  
4. Testa databasen genom egna SELECT-, UPDATE- och JOIN-frågor.

## Syfte
Uppgiften tränar datamodellering, normalisering, SQL-skriptning och praktisk hantering av relationsdatabaser.
