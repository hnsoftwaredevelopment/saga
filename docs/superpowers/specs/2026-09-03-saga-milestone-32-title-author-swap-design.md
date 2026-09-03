# Saga Milestone 32: Titel en auteur omwisselen

## Aannames

- De bestaande kwaliteitsregel `Mogelijk titel en auteur omgewisseld` blijft bepalen welke boeken worden aangeboden.
- De actie werkt in deze milestone op precies één geselecteerd boek met precies één bruikbare auteur.
- De gebruiker bewerkt geen tekst in het bevestigingsvenster; de correctie wisselt de volledige titel en de enige auteur om.
- Bij twijfel kan de gebruiker annuleren of de bestaande actie `Dit is correct` gebruiken.

Deze aannames sluiten aan op het voorstel dat de gebruiker op 3 september 2026 heeft goedgekeurd.

## Doel

Geef de gebruiker op de metadata Quality Page een veilige, begrijpelijke herstelactie voor een boek waarvan titel en auteur vermoedelijk zijn omgewisseld. Voor het opslaan toont Saga expliciet de huidige en nieuwe waarden. Na bevestiging worden alleen titel, auteur en wijzigingsdatum aangepast en wordt de actuele bibliotheek direct opnieuw beoordeeld.

## Gebruikersroute

1. De gebruiker kiest `Mogelijk titel en auteur omgewisseld` en selecteert één boek.
2. Alleen in die context is `Titel en auteur omwisselen` zichtbaar en beschikbaar.
3. Een modaal bevestigingsvenster toont twee blokken: `Huidig` en `Na omwisselen`.
4. `Annuleren` of `Escape` sluit zonder wijziging; `Omwisselen` bevestigt de correctie.
5. Saga leest het boek vlak voor opslag opnieuw en controleert of de kwaliteitsregel nog geldt.
6. Saga slaat via de bestaande `BookService` op, leest de opgeslagen werkelijkheid opnieuw in en actualiseert dashboard, hoofdbibliotheek, filters en `metadata.json`.

## Veiligheidsregels

- Alleen de titel en de lijst met auteurs worden omgewisseld; beschrijving, taal, uitgever, datum, tags, serie, serienummer, ISBN, omslag, leesstatus en bestandskoppelingen blijven gelijk.
- De nieuwe titel is de huidige enige auteur; de nieuwe enige auteur is de huidige titel.
- Lege waarden, `Unknown`, meerdere auteurs en boeken waarvoor het signaal niet meer geldt worden niet opgeslagen.
- Een opslagconflict of fout houdt de rij zichtbaar en levert een begrijpelijke melding op.
- Een geslaagde databasewijziging met een mislukte sidecar/write-back blijft zichtbaar als waarschuwing, zonder de reeds opgeslagen werkelijkheid te verbergen.

## Technische aansluiting

- **Stack:** .NET/WPF, CommunityToolkit.Mvvm, EF Core/SQLite, xUnit en FluentAssertions.
- **Applicatielaag:** een kleine herstelservice naar het patroon van auteur-, taal- en serieherstel.
- **Presentatielaag:** een bevestigingsviewmodel en uitbreiding van `MetadataQualityDashboardViewModel`.
- **WPF-laag:** een compact modaal venster via `IUserInteractionService`, met bestaande stijlen en lokalisatie.
- **Opslag:** uitsluitend via `BookService.SaveAsync`; geen nieuw schema, repositorycontract of dependency.

## Projectstructuur

- `src/EbookManager.Application/Metadata` — veilige herstelbewerking en resultaatstatus.
- `src/EbookManager.Presentation/ViewModels` — bevestigingsgegevens en dashboardorkestratie.
- `src/EbookManager.App/Views` — WPF-bevestigingsvenster.
- `src/EbookManager.App/Resources/Strings` — teksten voor alle zes ondersteunde talen.
- `tests/EbookManager.Tests` — service-, viewmodel-, dashboard-, layout- en lokalisatietests.
- `docs/manual-tests` — handmatige acceptatiechecklist.

## Codestijl

Volg de bestaande immutable record-kopie en expliciete resultaatstatussen:

```csharp
var updatedBook = currentBook with
{
    Metadata = currentBook.Metadata with
    {
        Title = currentAuthor,
        Authors = [currentTitle]
    },
    UpdatedUtc = DateTimeOffset.UtcNow
};
```

Nieuwe namen zijn Engelstalig in code, gebruikersgerichte tekst komt uitsluitend uit resources en tests beschrijven zichtbaar gedrag.

## Commando's

- Gerichte tests: `dotnet test tests/EbookManager.Tests/EbookManager.Tests.csproj -c Release --filter FullyQualifiedName~MetadataQualityTitleAuthor`
- Volledige tests: `dotnet test EbookManager.sln -c Release --no-restore`
- Build: `dotnet build EbookManager.sln -c Release --no-restore`
- Debug-build voor handmatige test: `dotnet publish src/EbookManager.App/EbookManager.App.csproj -c Debug -o Builds/Debug`

## Teststrategie

- Servicetests bewijzen de omwisseling, behoud van overige gegevens, verouderde meldingen en opslagfouten.
- Viewmodel- en dashboardtests bewijzen de voor/na-weergave, annulering, commandobeschikbaarheid, melding en gerichte herevaluatie.
- Layout- en resourcetests bewijzen bindings, toetsenbordbediening, toegankelijke namen en volledige lokalisatie.
- De handmatige test controleert de volledige route, `metadata.json`, dashboard en hoofdbibliotheek.

## Grenzen

- **Altijd:** actuele gegevens herlezen, alleen toepasselijke boeken wijzigen, bestaande opslagroute gebruiken en alle tests/buildcontroles uitvoeren.
- **Eerst overleggen:** databaseschema, nieuwe dependency, wijziging van de detectieheuristiek of een bewerkbaar correctievenster.
- **Nooit:** automatisch omwisselen zonder bevestiging, meerdere boeken tegelijk wijzigen of geldige recente metadata overschrijven.

## Acceptatiecriteria

- De actie verschijnt alleen bij het juiste signaal en één geselecteerd herstelbaar boek.
- Het venster maakt vóór bevestiging ondubbelzinnig zichtbaar wat titel en auteur worden.
- Annuleren wijzigt niets; bevestigen wisselt uitsluitend titel en auteur om.
- Een verouderde of ongeldige melding wordt veilig geweigerd.
- Dashboard, hoofdbibliotheek, filters en sidecar tonen na succes direct de nieuwe waarden.
- De route is toetsenbordtoegankelijk, werkt bij langere vertalingen en is volledig vertaald.
- Gerichte en volledige tests slagen en de Release-build bevat geen waarschuwingen of fouten.

## Buiten scope

- Vrije bewerking van titel of auteur in dit venster.
- Bulkherstel.
- Aanpassing van de detectieheuristiek.
- Native ebook-write-back toevoegen.
- Optimistic concurrency projectbreed invoeren.

## Open vragen

Geen blokkerende vragen.
