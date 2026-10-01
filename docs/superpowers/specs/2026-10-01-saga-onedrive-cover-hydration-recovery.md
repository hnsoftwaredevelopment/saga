# Specificatie: OneDrive-omslagen eenmalig lokaal herstellen

## Doel

Herstel uitsluitend de `cover.jpg`-bestanden in de actieve Saga-bibliotheek die Windows na de opslagmigratie nog als offline rapporteert. De actie is bedoeld voor de huidige bibliotheek van de ontwikkelaar en brengt de oorspronkelijke blader- en zoeksnelheid terug zonder ebookbestanden te openen of te downloaden.

## Vastgestelde beginsituatie

- Bibliotheek: `C:\Users\hnijk\OneDrive\ELibrary`.
- Er zijn 32.644 omslagen met samen circa 5,56 GB.
- 32.344 omslagen zijn `Pinned + Offline`; circa 300 zijn werkelijk lokaal.
- De shardindeling met 256 mappen blijft behouden.

## Uitvoering

- Een afzonderlijke PowerShell-onderhoudsactie verwerkt exact bestanden met de naam `cover.jpg` onder `books`.
- Reeds lokale omslagen worden overgeslagen.
- Offline omslagen worden één voor één volledig gelezen, waardoor OneDrive de inhoud hydrateert.
- Voortgang, geslaagde bestanden en fouten worden gelogd buiten de bibliotheek.
- Hervatten is impliciet: een volgende uitvoering slaat inmiddels lokale bestanden over.
- Eén mislukte omslag stopt de overige verwerking niet.

## Commando's

- Test: `Invoke-Pester scripts/tests/Hydrate-LibraryCovers.Tests.ps1`
- Controle: `powershell -File scripts/Hydrate-LibraryCovers.ps1 -LibraryPath <pad> -ScanOnly`
- Uitvoering: `powershell -File scripts/Hydrate-LibraryCovers.ps1 -LibraryPath <pad>`

## Projectstructuur en stijl

- `scripts/Hydrate-LibraryCovers.ps1`: onderhoudsactie met gevalideerde parameters en een gestructureerd eindresultaat.
- `scripts/tests/Hydrate-LibraryCovers.Tests.ps1`: geïsoleerde tests met tijdelijke bibliotheken.
- Bestaande broncode, database en bibliotheekindeling worden niet gewijzigd.
- Functies en parameters krijgen beschrijvende PowerShell-namen; paden worden altijd letterlijk behandeld.

## Teststrategie

- Een geldige tijdelijke bibliotheek vindt uitsluitend `cover.jpg`.
- Scanmodus leest of wijzigt geen bestanden.
- Lokale bestanden worden standaard overgeslagen.
- Een testmodus kan lokale testomslagen lezen zonder OneDrive nodig te hebben.
- Een ontbrekende `library.db` of `books`-map wordt geweigerd.

## Grenzen

- Altijd: één bestand tegelijk, loggen, fouten isoleren en veilig kunnen hervatten.
- Alleen na expliciet akkoord: de echte bibliotheek hydrateren en ongeveer 5,56 GB lokaal ophalen. Dit akkoord is op 1 oktober 2026 gegeven.
- Nooit: ebooks lezen, databasegegevens wijzigen, mappen verplaatsen of bestanden verwijderen.

## Succescriteria

- Alle bereikbare offline omslagen zijn volledig gelezen en niet langer offline.
- Mislukte bestanden staan met reden in het log en kunnen bij een volgende uitvoering opnieuw worden geprobeerd.
- Saga kan daarna door de bibliotheek scrollen en zoeken zonder OneDrive-downloadvertraging per omslag.

## Open vragen

Geen blokkerende vragen. Een blijvende lokale thumbnailcache blijft een afzonderlijke latere optimalisatie.
