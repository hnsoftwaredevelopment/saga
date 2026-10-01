# Saga Milestone 39: meerdere ontbrekende auteurs herstellen

## Aannames

1. Deze slice geldt uitsluitend voor geselecteerde rijen binnen `Ontbrekende auteur`.
2. Eén gekozen bestaande of nieuwe auteur wordt op alle geselecteerde boeken toegepast.
3. Het auteursvenster toont het aantal geselecteerde boeken en vormt zelf de expliciete bevestiging; er volgt geen tweede bevestigingsvenster.
4. De bestaande service verwerkt ieder boek afzonderlijk. Volledige atomiciteit over SQLite, sidecars en ebookbestanden is niet mogelijk zonder risico op gegevensverlies; Saga toont daarom altijd de werkelijk opgeslagen uitkomst.
5. Een boek dat vlak voor opslaan al een geldige auteur heeft, wordt nooit overschreven.
6. Een volledig mislukt boek blijft zichtbaar op de Quality Page. Een boek waarvan SQLite is bijgewerkt maar write-back naar een bestand faalt, verdwijnt uit `Ontbrekende auteur` en levert een waarschuwing op.

## Doel

Laat een gebruiker meerdere boeken zonder auteur selecteren en één auteur veilig op alle geselecteerde boeken toepassen. De workflow hergebruikt de bestaande auteursuggesties, vrije invoer, metadata-opslag en herevaluatie van kwaliteitssignalen.

De slice is geslaagd wanneer de selectie, bevestiging, opslag en gedeeltelijke foutafhandeling begrijpelijk blijven en het dashboard na afloop exact de opgeslagen bibliotheektoestand toont.

## Gebruikersworkflow

1. De gebruiker kiest `Ontbrekende auteur` en selecteert één of meer boeken met muis of toetsenbord.
2. `Auteur wijzigen` is beschikbaar voor iedere niet-lege selectie in deze categorie.
3. Het bestaande auteursvenster opent:
   - bij één boek met de huidige boektitel;
   - bij meerdere boeken met het aantal geselecteerde boeken;
   - met dezelfde bekende-auteursuggesties en vrije invoer;
   - met een bevestigingsknop die duidelijk maakt dat de auteur wordt gewijzigd.
4. Annuleren of sluiten verandert geen enkel boek.
5. Na bevestigen controleert de service ieder geselecteerd boek opnieuw, zodat een inmiddels geldige auteur niet wordt overschreven.
6. Geslaagde en reeds elders herstelde boeken worden direct opnieuw beoordeeld en verdwijnen uit `Ontbrekende auteur` wanneer het probleem is opgelost.
7. Volledig mislukte boeken blijven zichtbaar en worden opnieuw geselecteerd, zodat de gebruiker ze kan herkennen en opnieuw kan proberen.
8. Niet meer bestaande boeken verdwijnen uit het dashboard.
9. Saga toont een gelokaliseerde samenvatting wanneer een deel faalde of alleen met write-backwaarschuwingen werd opgeslagen.

## Resultaatregels

- `Succeeded`: boek opnieuw inlezen, alle kwaliteitssignalen herevalueren en hoofdbibliotheek verversen.
- `SavedWithWriteBackErrors`: dezelfde actuele SQLite-weergave gebruiken en een waarschuwing tellen.
- `NotApplicable`: de actuele geldige auteur behouden, boek herevalueren en neutraal als overgeslagen tellen.
- `NotFound`: boek uit het dashboard verwijderen en als niet meer beschikbaar tellen.
- `Failed` of ontbrekend resultaat: oorspronkelijke dashboardrij behouden en als mislukt tellen.
- Na een gemengd resultaat worden alle volledig mislukte rijen geselecteerd; zonder mislukkingen wordt de logisch volgende rij geselecteerd.
- Een onverwachte fout vóór een bruikbaar batchresultaat laat alle nog niet gereconcilieerde rijen zichtbaar en toont een algemene foutmelding.

## Opslag en gegevensveiligheid

- Hergebruik `MetadataQualityAuthorRepairService` en `BookService.SaveAsync` voor SQLite, `metadata.json` en ondersteunde ebook-write-back.
- Boek-id’s komen uitsluitend uit de actuele selectie binnen de actieve `missing-author`-rijen.
- De auteur wordt getrimd en blijft ongeldig bij leegte of `Unknown`.
- Alleen `BookMetadata.Authors` en `UpdatedUtc` wijzigen; overige metadata en kwaliteitsbeslissingen blijven behouden.
- Ieder boek wordt vlak voor opslaan opnieuw gelezen. Bestaande geldige auteurs worden niet overschreven.
- Voeg geen schemawijziging, externe dienst of dependency toe.

## Interface, toegankelijkheid en lokalisatie

- De bestaande uitgebreide DataGrid-selectie blijft het selectiepatroon.
- `Auteur wijzigen` wordt bij meerdere geselecteerde boeken beschikbaar; alle andere herstel- en openacties blijven enkelvoudig.
- Het auteursvenster blijft volledig bruikbaar met `Tab`, pijltoetsen, `Enter` en `Escape`.
- Focus start in het auteursveld en suggesties blijven tijdens typen direct reageren.
- Nieuwe teksten en resultaatmeldingen komen in Engels, Nederlands, Duits, Frans, Spaans en Italiaans.
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
- Dashboard- en auteursviewmodel: `src/EbookManager.Presentation/ViewModels`.
- WPF-venster, interactieservice en resources: `src/EbookManager.App`.
- Tests: `tests/EbookManager.Tests`.
- Specificatie, checklist en voortgang: `docs` en `tasks`.

Nieuwe code volgt de bestaande resultaatgedreven verwerking:

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

- Dashboardtests starten rood voor bulkbeschikbaarheid, annuleren, alle-successen, gemengde resultaten, stale auteurs, selectie en tellingen.
- Viewmodeltests bewaken enkelvoudige titelcontext, bulkcontext, geldige vrije invoer en suggesties.
- Bestaande servicetests bewijzen dat de batch doorgaat na een fout en geldige auteurs niet overschrijft; alleen ontbrekende gevallen worden toegevoegd.
- Layout- en lokalisatietests bewaken de zichtbare context, toegankelijke namen en alle zes talen.
- LibraryViewModel-tests bewijzen dat de actieve bibliotheek en auteursfilters na meerdere reparaties worden bijgewerkt.
- De handmatige checklist behandelt muis, toetsenbord, bekende en nieuwe auteur, annuleren, gemengd resultaat en regressies van enkelvoudig herstel.

## Grenzen

### Altijd

- Alleen actuele geselecteerde `missing-author`-boeken verwerken.
- De bestaande veilige opslagroute hergebruiken.
- Geldige auteurs nooit overschrijven.
- Werkelijke deelresultaten tonen en alle zichtbare staten direct herevalueren.
- Tests eerst schrijven en iedere increment afzonderlijk verifiëren.
- Gewijzigde Markdown exact naar Obsidian spiegelen.

### Eerst overleggen

- Een database- of bestandsformaatwijziging.
- Een poging om de volledige batch over database en bestanden atomair te maken.
- Een nieuwe dependency of extern auteurzoeken.

### Niet in deze slice

- Bulkherstel voor taal, serie, tags, omslagen of titel/auteur.
- Meerdere auteurs tegelijk aan één boek toekennen.
- Selectie over meerdere kwaliteitscategorieën.
- Automatisch raden van auteurs.
- De tekstactie `Dit is correct` vervangen door een icoon.

## Acceptatiecriteria

- Eén of meerdere `Ontbrekende auteur`-rijen activeren `Auteur wijzigen`.
- Het venster toont bij bulk het juiste aantal en biedt bekende auteurs plus vrije invoer.
- Annuleren wijzigt niets.
- Bevestigen past één geldige auteur toe op ieder nog toepasselijk geselecteerd boek.
- Geldige actuele auteurs worden nooit overschreven.
- Geslaagde, gedeeltelijk geslaagde, verouderde, verwijderde en mislukte boeken worden afzonderlijk correct verwerkt.
- Volledig mislukte boeken blijven zichtbaar en herkenbaar; waarschuwingen en aantallen zijn begrijpelijk en gelokaliseerd.
- Dashboard, hoofdbibliotheek, signalen, tellingen, selectie en auteursfilters tonen zonder herstart de actuele toestand.
- Enkelvoudig auteurherstel en alle andere kwaliteitsacties blijven werken.
- Volledige tests, schone Debug-build, handmatige checklist en GitHub-controles voldoen aan de Saga Definition of Done.

## Open vragen

Er zijn geen blokkerende open vragen. De bewuste keuze voor resultaatgedreven verwerking in plaats van schijnbare volledige atomiciteit volgt uit de bestaande opslagroute over SQLite en meerdere bestanden.
