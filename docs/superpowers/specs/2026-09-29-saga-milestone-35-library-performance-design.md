# Saga Milestone 35: Responsieve boekenlijst en zoekfilter

## Doel

Saga moet met de echte bibliotheek van ongeveer 34.447 boeken vloeiend blijven reageren tijdens scrollen, Page Up/Page Down en typen in het algemene zoekveld. De bestaande zoekresultaten, sortering, selectie, groepering en metadataweergave blijven functioneel gelijk.

De beschikbare praktijkmeting toont bij vier zoekresultaten `ApplyFilter=2237 ms`: ongeveer 553 ms voor het doorzoeken van de boeken en 1684 ms voor selectie en detailverversing. Daarnaast worden zichtbare omslagen tijdens het scrollen synchroon vanuit de OneDrive-bibliotheek gelezen en gedecodeerd. Beide oorzaken vallen binnen deze milestone.

## Uitgangspunten

- De interface wacht tijdens scrollen niet op omslagbestanden of OneDrive.
- Een omslag mag kort als lege placeholder verschijnen en wordt daarna automatisch ingevuld.
- Alleen het laatst getypte zoekbegrip hoeft te worden toegepast; tussenliggende toetsaanslagen mogen worden samengevoegd.
- De bestaande brede zoekfunctie over titel, auteur, beschrijving, taal, uitgever, datums, tags, serie, ISBN, formaten, status en aangepaste metadata blijft behouden.
- De geselecteerde boekregel en het detailpaneel worden niet opnieuw geladen wanneer hetzelfde boek na filteren geselecteerd blijft.
- Omslagcaching is begrensd en houdt niet de volledige bibliotheek in het geheugen.
- Deze milestone wijzigt geen boekgegevens, database-indeling of opslagpaden.

## Technische richting

### Zoekindex

De kostbare, cultuurafhankelijke zoekwaarden van ieder geladen boek worden eenmaal opgebouwd en hergebruikt zolang dat boek ongewijzigd blijft. Een gewijzigde of opnieuw geladen boekinstantie krijgt een nieuwe index. Aangepaste metadatavelden blijven onderdeel van dezelfde zoekbare waarden.

### Uitgestelde filtertoepassing

Wijzigingen in het algemene zoekveld krijgen een korte wachttijd van ongeveer 200 tot 300 ms. Een nieuw teken annuleert alleen de nog niet uitgevoerde filteractie; expliciete verversingen, facetfilters, sortering en navigatie blijven direct werken. De vertraging wordt testbaar gehouden zonder afhankelijkheid van de WPF-interface.

### Hergebruik van zichtbare regels en selectie

Boekregels worden per actuele boekinstantie hergebruikt in plaats van na iedere toetsaanslag volledig vervangen door nieuwe objecten. Hierdoor blijft dezelfde selectie dezelfde objectreferentie gebruiken en hoeft Saga het detailboek niet opnieuw uit SQLite te laden wanneer alleen het zoekbegrip verandert.

### Asynchrone omslagen

Een WPF-omslagcomponent toont onmiddellijk een placeholder, leest en decodeert de afbeelding buiten de UI-thread en controleert vóór weergave of de regel nog steeds hetzelfde pad vertegenwoordigt. Een kleine begrensde cache voorkomt herhaald lezen bij terugscrollen. Ontbrekende, ongeldige of tijdelijk niet beschikbare OneDrive-bestanden laten de lijst bruikbaar en tonen geen foutvenster.

## Commando's

- Gerichte tests: `dotnet test tests/EbookManager.Tests/EbookManager.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~BookSearchServiceTests|FullyQualifiedName~LibraryViewModelTests|FullyQualifiedName~Cover"`
- Volledige tests: `dotnet test EbookManager.sln -c Debug --no-restore`
- Debug-build: `dotnet build src/EbookManager.App/EbookManager.App.csproj -c Debug --no-restore`
- Handmatige app: `Builds\Debug\Saga.exe`

## Projectstructuur en stijl

- Zoekgedrag blijft in `EbookManager.Application`.
- Filtercoördinatie en selectie blijven in `EbookManager.Presentation`.
- WPF-afbeeldingsladen en de begrensde afbeeldingscache blijven in `EbookManager.App`.
- Tests volgen de bestaande xUnit- en FluentAssertions-stijl onder `tests/EbookManager.Tests`.
- Nieuwe typen krijgen doelgerichte namen; er wordt geen algemene cache- of schedulerlaag voor hypothetisch later gebruik geïntroduceerd.

## Teststrategie

- Een deterministische prestatietest gebruikt een representatieve set van minstens 30.000 boeken en bewaakt dat herhaald zoeken de vooraf opgebouwde waarden hergebruikt.
- Viewmodeltests bewijzen dat snel opeenvolgende zoekteksten alleen het laatste filter toepassen en dat een behouden selectie geen nieuwe detail-load veroorzaakt.
- Tests voor de omslaglader bewijzen achtergrondladen, verouderde resultaten negeren, ontbrekende bestanden afvangen en de cachegrens handhaven.
- Bestaande zoek-, sorteer-, selectie-, groeperings- en weergavetests blijven groen.
- De echte bibliotheek vormt de handmatige eindcontrole voor scrollen, Page Up/Page Down en typen.

## Grenzen

### Altijd

- Eerst een falende test voor iedere gedragswijziging.
- Metingen vergelijken vóór en na op dezelfde representatieve gegevensset.
- UI-updates uitsluitend op de UI-thread uitvoeren.
- Bestandsfouten veilig afvangen zonder de boekenlijst te blokkeren.

### Eerst overleggen

- Een nieuwe externe dependency toevoegen.
- Zoekvelden verwijderen of zoekresultaten inhoudelijk veranderen.
- Paginering invoeren waardoor niet alle boeken meer lokaal beschikbaar zijn.

### Nooit

- De echte bibliotheek vanuit geautomatiseerde tests wijzigen.
- Onbegrensd alle omslagen in geheugen laden.
- OneDrive-hydratatie afdwingen tijdens scrollen.
- De Quality Page-slice `Rommelige tags` in deze performancewijziging meenemen.

## Acceptatiecriteria

- Typen in het zoekveld blijft direct zichtbaar; alleen het laatst ingevoerde zoekbegrip wordt na de korte wachttijd toegepast.
- Een zoekactie over circa 34.000 boeken besteedt na het opbouwen van de index minder dan 250 ms aan de filterfase op de ontwikkellaptop.
- Wanneer het geselecteerde boek zichtbaar blijft, veroorzaakt filteren geen nieuwe database-load van het detailboek.
- Scrollen en Page Up/Page Down wachten niet synchroon op omslagbestanden; titels verschijnen direct en omslagen volgen zonder de navigatie te blokkeren.
- De omslagcache is begrensd en ongeldige of ontbrekende afbeeldingen veroorzaken geen crash of dialoog.
- De bestaande functionele zoekresultaten, sortering, groepering en selectie blijven correct.
- Volledige tests, schone Debug-build, handmatige checklist, review en Definition of Done zijn afgerond.

## Buiten scope en vervolg

- Databasepaginering of server-side zoeken.
- Achtergrondgeneratie van permanente miniatuurbestanden.
- Wijzigingen aan de boekenopslag uit Milestone 34.
- `Rommelige tags` op de Quality Page; die volgt na deze performance-slice.

## Open vragen

Geen blokkerende vragen. De concrete tijdsgrens geldt voor de gemeten filterfase; de handmatige controle bepaalt daarnaast of scrollen en typen merkbaar vloeiend zijn op de echte OneDrive-bibliotheek.
