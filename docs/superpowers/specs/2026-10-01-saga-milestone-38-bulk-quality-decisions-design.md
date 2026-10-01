# Saga Milestone 38: Meerdere kwaliteitsmeldingen als correct markeren

## Doel

Laat de gebruiker op de Quality Page meerdere boeken binnen één kwaliteitscategorie selecteren en de gekozen boek-/signaalcombinaties in één handeling als correct markeren. Dit maakt het opschonen van grote aantallen fout-positieve meldingen sneller, zonder boekmetadata te wijzigen.

## Aannames en afbakening

- Meervoudige selectie geldt alleen voor de boekenlijst van de momenteel gekozen kwaliteitscategorie.
- Gewone klik selecteert één rij; `Ctrl` en `Shift` volgen het gebruikelijke Windows-selectiegedrag.
- Eén geselecteerde melding behoudt de bestaande directe actie zonder extra bevestiging.
- Twee of meer geselecteerde meldingen vragen eerst om een gelokaliseerde bevestiging met het aantal boeken.
- `Openen in bibliotheek` en alle metadataherstelacties zijn alleen beschikbaar bij precies één geselecteerd boek.
- Bulkherstel, selectie over meerdere categorieën en de toekomstige icoonknop voor `Dit is correct` vallen buiten deze milestone.

## Gebruikersroute

1. De gebruiker kiest één kwaliteitscategorie.
2. De gebruiker selecteert één of meerdere boeken in de rechterlijst.
3. Bij één boek werkt `Dit is correct` zoals nu.
4. Bij meerdere boeken toont Saga een bevestiging met het aantal geselecteerde meldingen.
5. Annuleren laat alle meldingen en opgeslagen beslissingen ongewijzigd.
6. Bevestigen bewaart alle geselecteerde boek-/signaalcombinaties samen en verwijdert daarna alle gekozen rijen uit de actuele lijst.
7. Saga selecteert een logische naburige rij, of niets wanneer de categorie leeg wordt.

## Gegevensveiligheid

- De bestaande `MetadataQualityExclusions`-tabel en stabiele `(BookId, SignalKey)`-sleutel blijven leidend.
- De repository schrijft de volledige batch in één SQLite-transactie.
- Bij een opslagfout wordt de transactie teruggedraaid, blijven alle rijen zichtbaar en verschijnt de bestaande foutmelding.
- Andere kwaliteitssignalen voor dezelfde boeken blijven zichtbaar.
- Boekmetadata, sidecars, ebookbestanden en omslagen worden niet gewijzigd.

## Interface en toegankelijkheid

- De DataGrid gebruikt `Extended` selectie en volledige rijen.
- De geselecteerde rijen worden via één expliciete selectiebrug aan het viewmodel doorgegeven.
- De knop `Dit is correct` blijft via toetsenbord bereikbaar.
- De bevestiging gebruikt begrijpelijke tekst in Engels, Nederlands, Duits, Frans, Spaans en Italiaans.
- Dubbelklik en `Openen in bibliotheek` werken alleen wanneer precies één boek is geselecteerd.

## Technische aanpak

- Voeg een geselecteerde-boekenverzameling en `SetSelectedBooks` toe aan `MetadataQualityDashboardViewModel`.
- Laat enkelvoudige acties een gedeelde voorwaarde `precies één geldige selectie` gebruiken.
- Laat `MarkSelectedIssueCorrectCommand` een snapshot van alle geldige geselecteerde rijen verwerken.
- Voeg `ConfirmQualityIssuesCorrectAsync` toe aan `IUserInteractionService` en de WPF-implementatie.
- Maak `AddMetadataQualityExclusionsAsync` transactioneel voor de volledige batch.
- Breid viewmodel-, layout-, lokalisatie- en repositorytests uit.

## Teststrategie

- Eerst falende tests voor meerdere geselecteerde rijen, bevestigen, annuleren, foutafhandeling en uitschakeling van enkelvoudige acties.
- Een layouttest bewaakt `SelectionMode="Extended"`, volledige rijselectie en de selectie-eventbrug.
- Lokalisatietests bewaken titel en bericht van de bevestiging in alle talen.
- De bestaande repository-integratietest bewijst batchopslag; de implementatie gebruikt één transactie.
- Daarna de volledige Debug-testset en een schone Debug-build.

## Acceptatiecriteria

- De gebruiker kan meerdere boeken binnen één kwaliteitscategorie selecteren.
- `Dit is correct` bewaart en verwijdert exact alle geselecteerde boek-/signaalcombinaties.
- Bij meerdere boeken verschijnt vóór opslag een bevestiging met het juiste aantal.
- Annuleren en opslagfouten veranderen niets aan de zichtbare of opgeslagen selectie.
- Andere kwaliteitssignalen en boekmetadata blijven ongewijzigd.
- Open- en herstelacties zijn alleen uitvoerbaar bij precies één selectie.
- Alle nieuwe teksten zijn begrijpelijk vertaald en de workflow is toetsenbordbedienbaar.

## Verificatieopdrachten

- `dotnet test tests/EbookManager.Tests/EbookManager.Tests.csproj -c Debug --filter "FullyQualifiedName~MetadataQualityDashboard|FullyQualifiedName~MetadataQualityLocalization|FullyQualifiedName~LibraryDbContextTests"`
- `dotnet test EbookManager.sln -c Debug`
- `dotnet build src/EbookManager.App/EbookManager.App.csproj -c Debug --no-restore`

## Open vragen

Geen blokkerende open vragen. Bulkherstel met één auteur voor meerdere geselecteerde boeken volgt als afzonderlijke slice.
