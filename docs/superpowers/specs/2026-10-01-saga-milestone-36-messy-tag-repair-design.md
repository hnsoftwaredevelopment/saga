# Saga Milestone 36: Rommelige tags herstellen

## Doel

Geef de gebruiker op de Quality Page een veilige en begrijpelijke herstelactie voor één boek met rommelige tags. Saga toont de bestaande tags, maakt een bewerkbaar schoonmaakvoorstel en bewaart pas na expliciete bevestiging. Na succesvol opslaan beoordeelt Saga het boek opnieuw en werkt de Quality Page en bibliotheek direct bij.

## Aannames en afbakening

- Deze slice herstelt één geselecteerd boek tegelijk.
- Een komma in een tag geldt als scheiding tussen meerdere tags, overeenkomstig de bestaande kwaliteitsregel.
- Een legitieme tag met een komma wordt niet automatisch behouden; daarvoor blijft `Dit is correct` beschikbaar.
- Het voorstel is bewerkbaar met één tag per regel.
- Saga verwijdert lege waarden, trimt en normaliseert witruimte, en voegt hoofdletter-onafhankelijke dubbelen samen.
- De eerste schrijfwijze en de bestaande volgorde van unieke tags blijven behouden.
- Een lege uiteindelijke lijst is toegestaan en verwijdert alle tags van het boek.
- Bulkherstel, aanpassingen aan de detectieregel en het aparte instellingentabblad `Kwaliteit` vallen buiten deze slice.

## Gebruikersverloop

1. De gebruiker opent de Quality Page en selecteert `Rommelige tags`.
2. Na selectie van een boek verschijnt de actie `Tags opschonen`.
3. Het herstelvenster toont de boektitel en de huidige tags als alleen-lezen informatie.
4. Het veld `Nieuwe tags` bevat het automatisch opgeschoonde voorstel, met één tag per regel.
5. De gebruiker kan regels wijzigen, toevoegen of verwijderen.
6. `Tags wijzigen` is alleen beschikbaar wanneer de genormaliseerde uitkomst afwijkt van de opgeslagen tags.
7. Annuleren wijzigt niets. Bevestigen leest het boek opnieuw en controleert dat `messy-tags` nog van toepassing is.
8. Saga wijzigt uitsluitend de tags en `UpdatedUtc`, en gebruikt daarna de bestaande veilige `BookService`-opslagroute.
9. De Quality Page beoordeelt alle signalen opnieuw. De rij verdwijnt uit `Rommelige tags` wanneer het probleem is opgelost, maar blijft zichtbaar bij andere nog geldige signalen.

## Normalisatieregels

Voor zowel het automatische voorstel als de bevestigde invoer gelden dezelfde regels:

1. splits iedere invoerwaarde op komma’s en regeleinden;
2. verwijder lege onderdelen;
3. vervang opeenvolgende witruimte door één gewone spatie;
4. verwijder witruimte aan begin en einde;
5. verwijder dubbelen met `CurrentCultureIgnoreCase` en behoud de eerste schrijfwijze;
6. behoud de volgorde waarin de unieke waarden voor het eerst voorkomen.

Voorbeeld:

```text
Huidig:  [" Thriller ", "Misdaad, Spanning", "thriller", " "]
Voorstel: ["Thriller", "Misdaad", "Spanning"]
```

## Technische inpassing

- **Domein:** de bestaande sleutel `MetadataQualitySignalKeys.MessyTags` en evaluator blijven ongewijzigd.
- **Applicatielaag:** een gerichte herstelservice leest het actuele boek opnieuw, normaliseert de gekozen tags, controleert toepasbaarheid en bewaart via `BookService`.
- **Presentatielaag:** een herstelviewmodel levert huidige tags, bewerkbare tekst, genormaliseerde tags en `CanSave`.
- **WPF-laag:** een modaal, resizable venster volgt de bestaande Quality Page-herstelvensters en is volledig met toetsenbord bedienbaar.
- **Dashboard:** een aparte opdracht is uitsluitend zichtbaar en actief bij `messy-tags` en een geldige boekselectie.
- **Compositie:** de bestaande dependency-injection- en `IUserInteractionService`-routes worden uitgebreid; er komt geen nieuwe dependency of databasewijziging.

## Projectstructuur

- `src/EbookManager.Application/Metadata` — herstelservice en resultaatstatussen.
- `src/EbookManager.Presentation/ViewModels` — herstelviewmodel en dashboardcoördinatie.
- `src/EbookManager.App/Views` — WPF-herstelvenster.
- `src/EbookManager.App/Resources/Strings` — gelokaliseerde teksten.
- `tests/EbookManager.Tests/Metadata` — servicetests.
- `tests/EbookManager.Tests/App/ViewModels` — viewmodel- en dashboardtests.
- `tests/EbookManager.Tests/App/Views` — statische toegankelijkheids- en layoutcontrole.

## Code- en interactiestijl

- Gebruik dezelfde resultaatstatussen en waarschuwingen als de bestaande auteur-, taal- en serieherstelroutes.
- Houd normalisatie in één canonieke, pure functie zodat voorstel en opslag nooit uiteenlopen.
- Laat annuleren en validatiefouten zonder metadatawijziging eindigen.
- Gebruik gelokaliseerde zichtbare labels en automation names; de invoer krijgt bij openen focus.
- Voeg geen generieke formulierarchitectuur of nieuwe bibliotheek toe voor één venster.

## Commando’s

- Gerichte tests: `dotnet test tests/EbookManager.Tests/EbookManager.Tests.csproj --filter FullyQualifiedName~MetadataQualityTagRepair --no-restore`
- Volledige tests: `dotnet test EbookManager.sln --no-restore`
- Opmaakcontrole: `dotnet format EbookManager.sln --verify-no-changes --no-restore`
- Debug-build: `dotnet publish src/EbookManager.App/EbookManager.App.csproj -c Debug -o Builds/Debug --no-restore`

## Teststrategie

- Pure normalisatietests voor komma’s, regeleinden, witruimte, dubbelen, volgorde en lege uitkomst.
- Servicetests voor alleen-tags-wijziging, hercontrole, ontbrekend boek, niet meer toepasselijk probleem, opslagfout en write-backwaarschuwing.
- Viewmodeltests voor voorstel, handmatige bewerking en `CanSave`.
- Dashboardtests voor zichtbaarheid, annuleren, succes, foutstatus en directe herbeoordeling.
- XAML-layouttest voor bindingen, toetsenbordfocus, resizable venster en toegankelijke namen.
- Handmatige controle met echte rommelige tags in de actuele Debug-build.

## Grenzen

### Altijd

- Het actuele boek vlak vóór opslag opnieuw lezen.
- Alleen tags en `UpdatedUtc` wijzigen.
- Geannuleerde of mislukte acties laten de bestaande kwaliteitsrij intact.
- Na opslag de databasewerkelijkheid opnieuw tonen.
- Alle gewijzigde Markdown exact naar de afgesproken Obsidian-map spiegelen.

### Eerst overleggen

- De definitie van `messy-tags` verbreden of versoepelen.
- Automatisch meerdere boeken tegelijk wijzigen.
- Een databasewijziging of nieuwe dependency toevoegen.

### Niet doen

- Tags zonder voorvertoning stil automatisch aanpassen.
- Titel, auteur, taal, serie, omslag, leesstatus of bestanden buiten de bestaande write-backroute wijzigen.
- Een komma als letterlijk onderdeel van een herstelde tag bewaren; gebruik daarvoor `Dit is correct`.

## Acceptatiecriteria

- De actie `Tags opschonen` verschijnt alleen voor een geselecteerde regel onder `Rommelige tags`.
- Het voorstel volgt alle normalisatieregels en blijft volledig bewerkbaar.
- Annuleren verandert niets; bevestigen verandert alleen tags en wijzigingsdatum.
- De herstelde rij en aantallen worden direct opnieuw beoordeeld zonder herstart.
- Ontbrekende boeken, niet meer toepasselijke meldingen, opslagfouten en gedeeltelijke write-back tonen begrijpelijke resultaten.
- Alle nieuwe teksten zijn gelokaliseerd in de bestaande ondersteunde talen.
- Gerichte tests, volledige tests, opmaakcontrole en Debug-build slagen zonder waarschuwingen.

## Open vragen

Geen blokkerende vragen. Bulkherstel en een apart instellingentabblad `Kwaliteit` blijven expliciete vervolgstappen.
