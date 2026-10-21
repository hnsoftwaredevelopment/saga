# Saga Milestone 41: meerdere ontbrekende of onbekende talen herstellen

## Aannames

1. Deze slice geldt uitsluitend voor geselecteerde rijen binnen `Ontbrekende of onbekende taal`.
2. Eén gekozen geldige taal wordt op alle geselecteerde boeken toegepast.
3. Het bestaande taalvenster toont bij één boek de titel en bij meerdere boeken het aantal geselecteerde boeken.
4. Het taalvenster vormt zelf de expliciete bevestiging; er volgt geen tweede bevestigingsvenster.
5. Ieder boek wordt vlak voor opslaan opnieuw gelezen. Een inmiddels geldige taal wordt nooit overschreven.
6. De bestaande resultaatgedreven opslag blijft leidend: volledig mislukte boeken blijven zichtbaar, terwijl boeken met een geslaagde SQLite-opslag en een write-backwaarschuwing de actuele opgeslagen taal tonen.

## Doel

Laat een gebruiker één of meerdere boeken met een ontbrekende of ongeldige taal selecteren en in één handeling dezelfde geldige taalcode toepassen. De workflow hergebruikt de bestaande talenlijst, taalnormalisatie, opslagroute en herevaluatie van kwaliteitssignalen.

De slice is geslaagd wanneer selectie, bevestiging, opslag en gedeeltelijke foutafhandeling begrijpelijk blijven en de Quality Page na afloop exact de werkelijk opgeslagen bibliotheektoestand toont.

## Gebruikersworkflow

1. De gebruiker kiest `Ontbrekende of onbekende taal` en selecteert één of meer boeken met muis of toetsenbord.
2. `Taal wijzigen` is beschikbaar voor iedere niet-lege selectie in deze categorie.
3. Het taalvenster opent:
   - bij één boek met de huidige boektitel;
   - bij meerdere boeken met het aantal geselecteerde boeken;
   - met dezelfde doorzoekbare, gelokaliseerde lijst van geldige talen en taalcodes;
   - met een bevestigingsknop die duidelijk maakt hoeveel boeken worden gewijzigd.
4. Annuleren of sluiten verandert geen enkel boek.
5. Na bevestigen controleert Saga ieder geselecteerd boek opnieuw en slaat alleen nog toepasselijke reparaties op.
6. Geslaagde en reeds elders herstelde boeken worden direct opnieuw beoordeeld en verdwijnen uit deze kwaliteitscategorie wanneer het probleem is opgelost.
7. Volledig mislukte boeken blijven zichtbaar en worden opnieuw geselecteerd.
8. Niet meer bestaande boeken verdwijnen uit het dashboard.
9. Saga toont een korte groene succesmelding wanneer alles slaagt en anders een gelokaliseerde samenvatting van successen, write-backwaarschuwingen, overgeslagen, verdwenen en mislukte boeken.

## Resultaatregels

- `Succeeded`: boek opnieuw inlezen, alle kwaliteitssignalen herevalueren en hoofdbibliotheek verversen.
- `SavedWithWriteBackErrors`: dezelfde actuele SQLite-weergave gebruiken en een waarschuwing tellen.
- `NotApplicable`: de actuele geldige taal behouden, boek herevalueren en neutraal als overgeslagen tellen.
- `NotFound`: boek uit het dashboard verwijderen en als niet meer beschikbaar tellen.
- `Failed` of ontbrekend resultaat: oorspronkelijke dashboardrij behouden en als mislukt tellen.
- Na een gemengd resultaat worden alle volledig mislukte rijen geselecteerd; zonder mislukkingen wordt de logisch volgende rij geselecteerd.
- Een onverwachte fout vóór een bruikbaar batchresultaat laat de nog niet gereconcilieerde rijen zichtbaar en toont een algemene foutmelding.

## Opslag en gegevensveiligheid

- Hergebruik `MetadataQualityLanguageRepairService` en `BookService.SaveAsync` voor SQLite, `metadata.json` en ondersteunde ebook-write-back.
- Boek-id’s komen uitsluitend uit de actuele selectie binnen de actieve `unknown-language`-rijen.
- De gekozen waarde wordt door `MetadataQualityLanguageRules` tot een geldige opgeslagen taalcode genormaliseerd.
- Alleen `BookMetadata.Language` en `UpdatedUtc` wijzigen; overige metadata en kwaliteitsbeslissingen blijven behouden.
- Ieder boek wordt vlak voor opslaan opnieuw gelezen. Een bestaande geldige taal wordt niet overschreven.
- Voeg geen schemawijziging, externe dienst of dependency toe.

## Interface, toegankelijkheid en lokalisatie

- De bestaande uitgebreide DataGrid-selectie blijft het selectiepatroon.
- `Taal wijzigen` wordt bij meerdere geselecteerde boeken beschikbaar; alle andere herstel- en openacties blijven enkelvoudig, behalve de reeds bestaande bulkauteuractie en `Dit is correct`.
- Het taalvenster blijft volledig bruikbaar met `Tab`, pijltoetsen, `Enter` en `Escape`.
- Focus start in de talenlijst en de bestaande tekstzoekfunctie blijft beschikbaar.
- Nieuwe context- en resultaatteksten komen in Engels, Nederlands, Duits, Frans, Spaans en Italiaans.
- Langere vertalingen moeten zonder afkappen kunnen omlopen.

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
- Dashboard- en taalviewmodel: `src/EbookManager.Presentation/ViewModels`.
- WPF-venster, interactieservice en resources: `src/EbookManager.App`.
- Tests: `tests/EbookManager.Tests`.
- Specificatie, checklist en voortgang: `docs` en `tasks`.

De implementatie volgt de bestaande resultaatgedreven bulkverwerking:

```csharp
foreach (var result in batch.Items)
{
    if (result.Book is { } currentBook)
    {
        ReconcileBook(currentBook);
    }
}
```

## Teststrategie

- Dashboardtests starten rood voor bulkbeschikbaarheid, annuleren, volledig succes, gemengde resultaten, verouderde talen, selectie en tellingen.
- Viewmodeltests bewaken enkelvoudige titelcontext, bulkcontext, geldige taalkeuze en de bevestigingstekst.
- Bestaande servicetests bewijzen dat meerdere boeken worden verwerkt en geldige actuele talen niet worden overschreven; ontbrekende randgevallen worden eerst als falende tests toegevoegd.
- Layout- en lokalisatietests bewaken de zichtbare context, toegankelijke namen en alle zes talen.
- LibraryViewModel-tests bewijzen dat de actieve bibliotheek en taalfilters na meerdere reparaties éénmaal worden bijgewerkt.
- De handmatige checklist behandelt muis, toetsenbord, zoeken, annuleren, gemengde resultaten en regressies van enkelvoudig taalherstel.

## Grenzen

### Altijd

- Alleen actuele geselecteerde `unknown-language`-boeken verwerken.
- De bestaande veilige opslagroute hergebruiken.
- Geldige actuele talen nooit overschrijven.
- Werkelijke deelresultaten tonen en alle zichtbare staten direct herevalueren.
- Tests eerst schrijven en iedere increment afzonderlijk verifiëren.
- Gewijzigde Markdown exact naar Obsidian spiegelen.

### Eerst overleggen

- Een database- of bestandsformaatwijziging.
- Een poging om de volledige batch over database en bestanden atomair te maken.
- Een nieuwe dependency of externe metadata-/taaldienst.

### Niet in deze slice

- Bulkherstel voor serie, tags, omslagen of titel/auteur.
- Automatisch raden van de taal uit titel, beschrijving of boekinhoud.
- Selectie over meerdere kwaliteitscategorieën.
- De bredere metadatazoekfunctie of BoekenBase-integratie.
- Het aanpassen van de kwaliteitsregel voor onbekende talen.
- De tekstactie `Dit is correct` vervangen door een icoon.

## Acceptatiecriteria

- Eén of meerdere rijen onder `Ontbrekende of onbekende taal` activeren `Taal wijzigen`.
- Het venster toont bij bulk het juiste aantal en biedt dezelfde geldige talen als bij één boek.
- Annuleren wijzigt niets.
- Bevestigen past één geldige genormaliseerde taalcode toe op ieder nog toepasselijk geselecteerd boek.
- Geldige actuele talen worden nooit overschreven.
- Geslaagde, gedeeltelijk geslaagde, verouderde, verwijderde en mislukte boeken worden afzonderlijk correct verwerkt.
- Volledig mislukte boeken blijven zichtbaar en herkenbaar; waarschuwingen en aantallen zijn begrijpelijk en gelokaliseerd.
- Dashboard, hoofdbibliotheek, signalen, tellingen, selectie en taalfilters tonen zonder herstart de actuele toestand.
- Enkelvoudig taalherstel, bulkauteurherstel en alle andere kwaliteitsacties blijven werken.
- Volledige tests, schone Debug-build, handmatige checklist en GitHub-controles voldoen aan de Saga Definition of Done.

## Open vragen

Er zijn geen blokkerende open vragen. De slice gebruikt bewust dezelfde resultaatgedreven verwerking als het geaccepteerde bulkauteurherstel.
