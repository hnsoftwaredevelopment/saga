# Saga Milestone 43: meerdere ontbrekende serienamen herstellen

## Aannames

1. Deze slice geldt uitsluitend voor geselecteerde rijen binnen `Serienummer zonder serie`.
2. Eén gekozen bestaande of nieuwe serienaam wordt op alle geselecteerde boeken toegepast.
3. Elk boek behoudt zijn eigen serienummer; alleen de ontbrekende serienaam verandert.
4. Het bestaande serievenster toont bij één boek de titel en het serienummer en bij meerdere boeken het aantal geselecteerde boeken plus de uitleg dat de individuele serienummers behouden blijven.
5. Het serievenster is zelf de expliciete bevestiging; er volgt geen tweede bevestigingsvenster.
6. Ieder boek wordt vlak voor opslaan opnieuw gelezen. Een inmiddels ingevulde serienaam wordt nooit overschreven.
7. De bestaande resultaatgedreven opslag blijft leidend: volledig mislukte boeken blijven zichtbaar, terwijl boeken met een geslaagde SQLite-opslag en een write-backwaarschuwing de actuele serie tonen.
8. De eerder afgesproken Splash-verbetering wordt als afzonderlijke, kleine commit uitgevoerd: titel en subtitel worden via het WPF-layoutsysteem gecentreerd in plaats van met verticale correctiemarges.

## Doel

Laat een gebruiker één of meerdere boeken met een serienummer maar zonder serienaam selecteren en in één handeling dezelfde serienaam toepassen. De workflow hergebruikt de bestaande seriesuggesties, vrije invoer, opslagroute, batchresultaten en herevaluatie van kwaliteitssignalen.

De slice is geslaagd wanneer ieder boek zijn eigen serienummer behoudt, selectie en deelresultaten begrijpelijk blijven en de Quality Page, hoofdbibliotheek en seriefilters direct de werkelijk opgeslagen toestand tonen.

## Gebruikersworkflow

1. De gebruiker kiest `Serienummer zonder serie` en selecteert één of meer boeken met muis of toetsenbord.
2. `Serie wijzigen` is beschikbaar voor iedere niet-lege selectie binnen deze categorie.
3. Het serievenster opent:
   - bij één boek met de huidige titel en het bestaande serienummer;
   - bij meerdere boeken met het aantal geselecteerde boeken;
   - bij meerdere boeken met een duidelijke melding dat ieder serienummer behouden blijft;
   - met dezelfde live gefilterde lijst van bekende series en vrije invoer voor een nieuwe serie;
   - met een bevestigingsknop die duidelijk maakt hoeveel boeken worden gewijzigd.
4. Annuleren of sluiten verandert geen enkel boek.
5. Na bevestigen controleert Saga ieder geselecteerd boek opnieuw en slaat alleen nog toepasselijke reparaties op.
6. Geslaagde en reeds elders herstelde boeken worden direct opnieuw beoordeeld en verdwijnen uit deze kwaliteitscategorie wanneer het probleem is opgelost.
7. Volledig mislukte boeken blijven zichtbaar en opnieuw geselecteerd.
8. Niet meer bestaande boeken verdwijnen uit het dashboard.
9. Saga toont een korte groene succesmelding wanneer alles slaagt en anders een gelokaliseerde samenvatting van successen, write-backwaarschuwingen, overgeslagen, verdwenen en mislukte boeken.

## Resultaatregels

- `Succeeded`: boek opnieuw inlezen, alle kwaliteitssignalen herevalueren en hoofdbibliotheek verversen.
- `SavedWithWriteBackErrors`: dezelfde actuele SQLite-weergave gebruiken en een waarschuwing tellen.
- `NotApplicable`: de actuele geldige serienaam en het serienummer behouden, boek herevalueren en neutraal als overgeslagen tellen.
- `NotFound`: boek uit het dashboard verwijderen en als niet meer beschikbaar tellen.
- `Failed` of ontbrekend resultaat: oorspronkelijke dashboardrij behouden en als mislukt tellen.
- Na een gemengd resultaat worden alle volledig mislukte rijen geselecteerd; zonder mislukkingen wordt de logisch volgende rij geselecteerd.
- Een onverwachte fout vóór een bruikbaar batchresultaat laat de nog niet gereconcilieerde rijen zichtbaar en toont een algemene foutmelding.

## Opslag en gegevensveiligheid

- Hergebruik `MetadataQualitySeriesRepairService` en `BookService.SaveAsync` voor SQLite, `metadata.json` en ondersteunde ebook-write-back.
- Boek-id’s komen uitsluitend uit de actuele selectie binnen de actieve `series-number-without-series`-rijen.
- De ingevoerde serienaam wordt getrimd en mag niet leeg zijn.
- Alleen `BookMetadata.Series` en `UpdatedUtc` wijzigen; ieder `SeriesNumber` en alle overige metadata blijven behouden.
- Ieder boek wordt vlak voor opslaan opnieuw gelezen. Een bestaande serienaam wordt niet overschreven.
- Voeg geen schemawijziging, externe dienst of dependency toe.

## Interface, toegankelijkheid en lokalisatie

- De bestaande uitgebreide DataGrid-selectie blijft het selectiepatroon.
- `Serie wijzigen` wordt bij meerdere geselecteerde boeken beschikbaar; andere herstel- en openacties blijven enkelvoudig, behalve bestaande bulkacties.
- Het serievenster blijft bruikbaar met `Tab`, pijltoetsen, `Enter` en `Escape`.
- Focus start in het serieveld; snel typen, muiskeuze en toetsenbordkeuze blijven correct werken.
- Nieuwe bulkcontext- en resultaatteksten komen in Engels, Nederlands, Duits, Frans, Spaans en Italiaans.
- Langere vertalingen moeten zonder afkappen kunnen omlopen.
- In het Splash-scherm staan titel en subtitel in twee automatische Grid-kolommen en worden beide verticaal gecentreerd. Alleen de volledige titelgroep gebruikt een positioneringsmarge.

## Technische omgeving en opdrachten

- Platform: Windows WPF op .NET 10.
- Presentatie: CommunityToolkit.Mvvm en bestaande Saga-stijlen.
- Opslag: EF Core/SQLite plus de bestaande `BookService`-write-backroute.
- Tests: xUnit en FluentAssertions.

```powershell
dotnet test EbookManager.sln -c Debug --no-restore
dotnet build src\EbookManager.App\EbookManager.App.csproj -c Debug --no-restore
dotnet publish src\EbookManager.App\EbookManager.App.csproj -c Debug -o Builds\Debug --no-restore
git diff --check
```

## Projectstructuur en stijl

- Applicatieservice: `src/EbookManager.Application/Metadata`.
- Dashboard- en serieviewmodel: `src/EbookManager.Presentation/ViewModels`.
- WPF-vensters, interactieservice en resources: `src/EbookManager.App`.
- Tests: `tests/EbookManager.Tests`.
- Specificatie, checklist en voortgang: `docs` en `tasks`.

De serieactie sluit aan op de bestaande generieke batchreconciliatie voor auteur en taal. Er komt geen derde zelfstandige resultaatverwerker.

## Teststrategie

- Dashboardtests starten rood voor bulkbeschikbaarheid, annuleren, volledig succes, gemengde resultaten, verouderde series, selectie en tellingen.
- Viewmodeltests bewaken enkelvoudige titel-/nummercontext, bulkcontext, geldige vrije invoer en suggesties.
- Servicetests bewijzen verwerking van meerdere boeken met verschillende serienummers en bescherming van inmiddels ingevulde series.
- Layout- en lokalisatietests bewaken zichtbare bulkcontext, toegankelijke namen, de dynamische bevestigingstekst en alle zes talen.
- LibraryViewModel-tests bewijzen dat bibliotheek en seriefilters na een batch éénmaal worden bijgewerkt.
- Een Splash-layouttest bewijst Grid-kolommen en gecentreerde verticale uitlijning zonder verticale kindmarges.
- De handmatige checklist behandelt muis, toetsenbord, snel typen, annuleren, verschillende serienummers, gemengde resultaten, herstart en Splash-uitlijning.

## Grenzen

### Altijd

- Alleen actuele geselecteerde boeken met het signaal `Serienummer zonder serie` verwerken.
- De bestaande veilige opslag- en batchreconciliatieroutes hergebruiken.
- Geldige actuele series nooit overschrijven en individuele serienummers nooit wijzigen.
- Werkelijke deelresultaten tonen en alle zichtbare staten direct herevalueren.
- Tests eerst schrijven en iedere increment afzonderlijk verifiëren.
- Gewijzigde Markdown exact naar Obsidian spiegelen.

### Eerst overleggen

- Een database- of bestandsformaatwijziging.
- Een poging om de volledige batch over database en bestanden atomair te maken.
- Een nieuwe dependency of externe metadatadienst.

### Niet in deze slice

- Bulkherstel voor tags, omslagen of titel/auteur.
- Automatisch afleiden van een serienaam uit titel, auteur of serienummer.
- Eén bewerking die meerdere verschillende series toewijst.
- Serienummers aanpassen, hernummeren of automatisch sorteren.
- Selectie over meerdere kwaliteitscategorieën.
- De bredere metadatazoekfunctie of BoekenBase-integratie.
- De tekstactie `Dit is correct` vervangen door een icoon.

## Acceptatiecriteria

- Eén of meerdere rijen onder `Serienummer zonder serie` activeren `Serie wijzigen`.
- Het venster toont bij bulk het juiste aantal en legt uit dat serienummers behouden blijven.
- Bekende series worden vlot gefilterd en kunnen met muis of toetsenbord worden gekozen; een nieuwe serienaam kan vrij worden ingevoerd.
- Annuleren wijzigt niets.
- Bevestigen past één getrimde serienaam toe op ieder nog toepasselijk geselecteerd boek.
- Ieder boek behoudt zijn eigen serienummer en alle overige metadata.
- Geldige actuele series worden nooit overschreven.
- Geslaagde, gedeeltelijk geslaagde, verouderde, verwijderde en mislukte boeken worden afzonderlijk correct verwerkt.
- Dashboard, hoofdbibliotheek, signalen, tellingen, selectie en seriefilters tonen zonder herstart de actuele toestand.
- Enkelvoudig serieherstel, bestaande bulkacties en andere kwaliteitsacties blijven werken.
- Splash-titel en subtitel zijn via layoutuitlijning visueel gecentreerd zonder verticale kindmarges.
- Volledige tests, schone Debug-build, handmatige checklist en GitHub-controles voldoen aan de Saga Definition of Done.

## Open vragen

Er zijn geen blokkerende open vragen. De slice gebruikt dezelfde resultaatgedreven verwerking als de geaccepteerde bulkacties voor auteur en taal.
