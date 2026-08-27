# Saga Milestone 28 Quality Issue Exclusions Implementation Plan

## Overzicht

Deze milestone voegt een omkeerbare actie `Dit is correct` toe aan het metadata quality dashboard. De opslag is bibliotheekgebonden, gebruikt een stabiele combinatie van boek-id en signaalsleutel en volgt het bestaande patroon voor duplicate exclusions. Automatische metadatareparatie blijft buiten deze milestone.

## Architectuurbeslissingen

- Persistente sleutels zijn niet-gelokaliseerd en staan los van titels, beschrijvingen en heuristieken.
- `MetadataQualityExclusions` gebruikt `(BookId, SignalKey)` als samengestelde primaire sleutel en cascade delete naar `Books`.
- Het dashboard verwijdert een regel pas nadat de repository de uitzondering succesvol heeft opgeslagen.
- Het dashboard beheert observeerbare issue-rijen en selectie; `LibraryViewModel` levert de actieve bibliotheekrepository.
- Instellingenbeheer hergebruikt de opzet van duplicate exclusions, maar krijgt een eigen domeincontract en viewmodel.
- Geen nieuwe dependency of algemene architectuurlaag is nodig; een afzonderlijke ADR zou het bestaande repositorypatroon alleen herhalen en wordt daarom niet toegevoegd.

## Afhankelijkheidsgraaf

```text
Stabiele signaalsleutels en repositorycontract
    |
    +-- EF-entiteit en DbContext-model
    |       |
    |       +-- Gegenereerde migratie
    |       |
    |       +-- Repository-implementatie en integratietests
    |               |
    |               +-- Dashboardfiltering en negeeractie
    |               |       |
    |               |       +-- Dashboard-WPF en bibliotheekintegratie
    |               |
    |               +-- Beheer-viewmodel
    |                       |
    |                       +-- Beheerwindow en Instellingen
    |
    +-- Lokalisatie en documentatie na stabiele UI-contracten
```

## Taak 1: Stabiele Signaalsleutels En Domeincontract

**Beschrijving:** Introduceer de blijvende identiteit van de zes kwaliteitssignalen en het repositorycontract waarmee uitsluitingen zonder UI- of EF-afhankelijkheid kunnen worden opgeslagen en beheerd.

**Acceptatiecriteria:**

- [ ] De zes sleutels zijn uniek, niet leeg en exact gelijk aan de goedgekeurde kebab-case waarden.
- [ ] Een uitzonderingssleutel identificeert exact één boek en één signaal.
- [ ] Het repositorycontract ondersteunt lijst, details, toevoegen, geselecteerd verwijderen en alles verwijderen.

**Verificatie:**

- [ ] Schrijf eerst falende domeintests voor de bekende sleutels en sleutelidentiteit.
- [ ] Voer `dotnet test tests/EbookManager.Tests/EbookManager.Tests.csproj --filter FullyQualifiedName~MetadataQualityExclusion` uit.

**Dependencies:** Geen.

**Waarschijnlijke bestanden:**

- `src/EbookManager.Domain/Metadata/MetadataQualitySignalKeys.cs`
- `src/EbookManager.Domain/Metadata/MetadataQualityExclusion.cs`
- `src/EbookManager.Domain/Abstractions/IMetadataQualityExclusionRepository.cs`
- `tests/EbookManager.Tests/Metadata/MetadataQualityExclusionTests.cs`

**Omvang:** M, 4 bestanden.

## Taak 2: Databasegedrag Modelleren

**Beschrijving:** Voeg het EF Core-model voor kwaliteitsuitzonderingen toe en bewijs via een falende integratietest dat unieke opslag en cascade delete nodig zijn.

**Acceptatiecriteria:**

- [ ] Het model heeft `BookId`, `SignalKey` en `CreatedAt` met een samengestelde primaire sleutel.
- [ ] De boekrelatie gebruikt cascade delete.
- [ ] Een database die uit het actuele model wordt opgebouwd accepteert verschillende signalen voor hetzelfde boek, maar geen dubbele combinatie.

**Verificatie:**

- [ ] Voeg eerst de falende schema-/cascadetests toe.
- [ ] Voer de gerichte `LibraryDbContextTests` uit totdat het actuele model slaagt.

**Dependencies:** Taak 1.

**Waarschijnlijke bestanden:**

- `src/EbookManager.Infrastructure/Persistence/Entities/MetadataQualityExclusionEntity.cs`
- `src/EbookManager.Infrastructure/Persistence/LibraryDbContext.cs`
- `tests/EbookManager.Tests/Infrastructure/LibraryDbContextTests.cs`

**Omvang:** M, 3 bestanden.

## Taak 3: Additieve EF Core-Migratie

**Beschrijving:** Genereer de migratie vanuit het geteste model en controleer dat bestaande bibliotheken alleen de nieuwe tabel en vereiste index krijgen.

**Acceptatiecriteria:**

- [ ] De migratie maakt uitsluitend `MetadataQualityExclusions` en de benodigde foreign-keyindex aan.
- [ ] De down-migratie verwijdert uitsluitend de nieuwe tabel.
- [ ] De modelsnapshot komt overeen met het actuele DbContext-model.

**Verificatie:**

- [ ] Genereer met `dotnet tool run dotnet-ef migrations add AddMetadataQualityExclusions --project src/EbookManager.Infrastructure --startup-project src/EbookManager.Infrastructure --output-dir Persistence/Migrations`.
- [ ] Inspecteer migratie, designer en snapshot handmatig op onbedoelde schemawijzigingen.
- [ ] Voer de database-initialisatie- en migratietests uit.

**Dependencies:** Taak 2.

**Waarschijnlijke bestanden:**

- `src/EbookManager.Infrastructure/Persistence/Migrations/<timestamp>_AddMetadataQualityExclusions.cs`
- `src/EbookManager.Infrastructure/Persistence/Migrations/<timestamp>_AddMetadataQualityExclusions.Designer.cs`
- `src/EbookManager.Infrastructure/Persistence/Migrations/LibraryDbContextModelSnapshot.cs`

**Omvang:** M, 3 gegenereerde bestanden.

## Taak 4: Repositorygedrag Implementeren

**Beschrijving:** Maak kwaliteitsuitzonderingen beschikbaar via de bibliotheekgebonden repository en de dependency-injectionregistratie.

**Acceptatiecriteria:**

- [ ] Toevoegen is idempotent en bewaart het UTC-tijdstip.
- [ ] Lijsten en details bevatten alleen de actieve bibliotheek en leveren boekinformatie voor beheer.
- [ ] Geselecteerd verwijderen, alles verwijderen en cascade delete werken aantoonbaar.

**Verificatie:**

- [ ] Schrijf de repository-integratietests vóór de implementatie.
- [ ] Voer alle `LibraryDbContextTests` uit.
- [ ] Bouw de oplossing om interface- en DI-koppelingen te controleren.

**Dependencies:** Taken 1 tot en met 3.

**Waarschijnlijke bestanden:**

- `src/EbookManager.Infrastructure/Persistence/Repositories/EfBookRepository.cs`
- `src/EbookManager.App/Services/CurrentLibraryBookRepository.cs`
- `src/EbookManager.App/App.xaml.cs`
- `tests/EbookManager.Tests/Infrastructure/LibraryDbContextTests.cs`

**Omvang:** M, 4 bestanden.

## Checkpoint 1: Opslagfundament

- [ ] Alle domein- en repositorytests slagen.
- [ ] De migratiediff bevat geen wijzigingen buiten de nieuwe tabel.
- [ ] De volledige oplossing bouwt zonder waarschuwingen of fouten.
- [ ] Review met de gebruiker voordat de zichtbare dashboardactie wordt uitgebreid.

## Taak 5: Dashboardfiltering En Negeeractie

**Beschrijving:** Laat het dashboard bestaande uitzonderingen exact filteren en voeg het testgedreven commando toe dat een geselecteerde melding pas na succesvolle opslag verwijdert.

**Acceptatiecriteria:**

- [ ] Alleen de exacte boek-/signaalcombinatie wordt verborgen; andere signalen voor hetzelfde boek blijven staan.
- [ ] Succes werkt issuecount, totaal, selectie en commandostatus direct bij.
- [ ] Bij een opslagfout blijft de rij geselecteerd en zichtbaar en wordt gelokaliseerde statusfeedback gezet.

**Verificatie:**

- [ ] Schrijf eerst falende tests voor filtering, eerste/middelste/laatste rij, lege categorie en foutafhandeling.
- [ ] Voer alleen de dashboard-viewmodeltests uit en controleer dat bestaande navigatietests groen blijven.

**Dependencies:** Taak 4.

**Waarschijnlijke bestanden:**

- `src/EbookManager.Presentation/ViewModels/MetadataQualityDashboardViewModel.cs`
- `tests/EbookManager.Tests/App/ViewModels/MetadataQualityDashboardViewModelTests.cs`

**Omvang:** S, 2 bestanden.

## Taak 6: Dashboardactie Verbinden

**Beschrijving:** Verbind de actieve repository met het dashboard en voeg de toegankelijke WPF-actie toe zonder de bestaande open- en dubbelklikflow te veranderen.

**Acceptatiecriteria:**

- [ ] Het dashboard laadt uitzonderingen voordat het zijn issue-rijen toont.
- [ ] `Dit is correct` is via muis en toetsenbord bereikbaar en alleen actief bij een geldige selectie.
- [ ] Bestaande navigatie, splitter en sluitactie behouden hun gedrag.

**Verificatie:**

- [ ] Voeg eerst falende LibraryViewModel- en XAML-structuurtests toe.
- [ ] Voer dashboard-, LibraryViewModel- en layouttests uit.
- [ ] Bouw de WPF-app om bindingsfouten te vinden.

**Dependencies:** Taak 5.

**Waarschijnlijke bestanden:**

- `src/EbookManager.Presentation/ViewModels/LibraryViewModel.cs`
- `src/EbookManager.App/Views/MetadataQualityDashboardWindow.xaml`
- `tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs`
- `tests/EbookManager.Tests/App/Views/MetadataQualityDashboardWindowLayoutTests.cs`

**Omvang:** M, 4 bestanden.

## Checkpoint 2: Dashboardflow

- [ ] Opslaan, direct verbergen en heropenen zijn automatisch getest.
- [ ] Opslagfouten houden de melding zichtbaar.
- [ ] Bestaande open-in-bibliotheektests en splittertests slagen.
- [ ] Korte handmatige controle van selectie en toetsenbordbediening is geslaagd.

## Taak 7: Beheer-Viewmodel

**Beschrijving:** Bouw een onafhankelijk beheer-viewmodel voor laden, geselecteerd herstellen en alles herstellen, inclusief een veilige terugval voor onbekende signaalsleutels.

**Acceptatiecriteria:**

- [ ] Rijen tonen boek, auteurs, gelokaliseerd signaal en datum.
- [ ] Herstel geselecteerd verwijdert alleen de gekozen sleutels en werkt de telling bij.
- [ ] Herstel alles leegt de lijst; onbekende sleutels tonen de ruwe waarde.

**Verificatie:**

- [ ] Schrijf eerst falende viewmodeltests voor laden, selectie, geselecteerd herstellen, alles herstellen en onbekende sleutel.
- [ ] Voer alleen de nieuwe beheer-viewmodeltests uit.

**Dependencies:** Taak 4.

**Waarschijnlijke bestanden:**

- `src/EbookManager.Presentation/ViewModels/MetadataQualityExclusionsViewModel.cs`
- `tests/EbookManager.Tests/App/ViewModels/MetadataQualityExclusionsViewModelTests.cs`

**Omvang:** S, 2 bestanden.

## Taak 8: Beheerwindow En Interactiecontract

**Beschrijving:** Voeg het modale beheerwindow toe en maak het via de bestaande user-interactiongrens beschikbaar aan Presentation.

**Acceptatiecriteria:**

- [ ] De grid ondersteunt meerdere selectie en duidelijke lege toestand.
- [ ] `Geselecteerde herstellen` volgt de selectie; `Alles herstellen` vraagt bevestiging.
- [ ] Sluiten zonder actie wijzigt niets en alle acties zijn toetsenbordtoegankelijk.

**Verificatie:**

- [ ] Voeg XAML-structuurtests toe voor grid, bindings, knoppen en toegankelijke labels.
- [ ] Bouw de WPF-app en voer een gerichte handmatige windowcontrole uit.

**Dependencies:** Taak 7.

**Waarschijnlijke bestanden:**

- `src/EbookManager.App/Views/MetadataQualityExclusionsWindow.xaml`
- `src/EbookManager.App/Views/MetadataQualityExclusionsWindow.xaml.cs`
- `src/EbookManager.Presentation/Abstractions/IUserInteractionService.cs`
- `src/EbookManager.App/Services/UserInteractionService.cs`
- `tests/EbookManager.Tests/App/Views/MetadataQualityExclusionsWindowLayoutTests.cs`

**Omvang:** M, 5 bestanden.

## Taak 9: Beheer Vanuit Instellingen

**Beschrijving:** Voeg de beheeractie toe aan de bestaande duplicaten-/diagnostieksectie en verbind haar met de actieve bibliotheek.

**Acceptatiecriteria:**

- [ ] De actie is alleen beschikbaar met een actieve bibliotheek en opent het geladen beheer-viewmodel.
- [ ] Wisselen van bibliotheek toont nooit uitzonderingen uit de vorige bibliotheek.
- [ ] Herstelde meldingen worden bij een volgende dashboardopening opnieuw geëvalueerd.

**Verificatie:**

- [ ] Schrijf eerst falende LibraryViewModel-tests voor geen bibliotheek, actieve bibliotheek en bibliotheekwissel.
- [ ] Voer LibraryViewModel- en Settings-layouttests uit.

**Dependencies:** Taken 7 en 8.

**Waarschijnlijke bestanden:**

- `src/EbookManager.Presentation/ViewModels/LibraryViewModel.cs`
- `src/EbookManager.App/Views/SettingsWindow.xaml`
- `tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs`
- `tests/EbookManager.Tests/App/Views/SettingsWindowLayoutTests.cs`

**Omvang:** M, 4 bestanden.

## Checkpoint 3: Beheerflow

- [ ] Dashboard negeren en Instellingen herstellen werken end-to-end.
- [ ] Bibliotheekisolatie en cascade delete zijn bewezen.
- [ ] Alle gerichte tests en de WPF-build slagen.
- [ ] Review met de gebruiker vóór vertalingen en afronding.

## Taak 10: Basislokalisatie

**Beschrijving:** Voeg de definitieve dashboard-, beheer- en foutteksten toe aan de neutrale, Nederlandse en Duitse resources en bewaak de vereiste sleutels.

**Acceptatiecriteria:**

- [ ] Neutraal Engels, Nederlands en Duits bevatten alle nieuwe sleutels.
- [ ] Knoppen, beschrijvingen, bevestiging, lege toestand en fouten zijn begrijpelijk.
- [ ] Een automatische test detecteert ontbrekende featurekeys.

**Verificatie:**

- [ ] Voeg eerst een falende resourceconsistentietest toe.
- [ ] Voer de lokalisatietest en WPF-build uit.

**Dependencies:** Taken 6, 8 en 9.

**Waarschijnlijke bestanden:**

- `src/EbookManager.App/Resources/Strings/AppResources.resx`
- `src/EbookManager.App/Resources/Strings/AppResources.nl.resx`
- `src/EbookManager.App/Resources/Strings/AppResources.de.resx`
- `tests/EbookManager.Tests/App/Resources/MetadataQualityLocalizationTests.cs`

**Omvang:** M, 4 bestanden.

## Taak 11: Overige Talen

**Beschrijving:** Vul dezelfde goedgekeurde featurekeys aan voor Frans, Spaans en Italiaans en laat de resourceconsistentietest alle zes talen controleren.

**Acceptatiecriteria:**

- [ ] Frans, Spaans en Italiaans bevatten alle vereiste featurekeys.
- [ ] Geen resource gebruikt de persistente signaalsleutel als zichtbare tekst.
- [ ] De consistentietest controleert iedere ondersteunde resourcecultuur.

**Verificatie:**

- [ ] Laat de bestaande test eerst falen op de nog ontbrekende culturen.
- [ ] Voer de lokalisatietest en WPF-build opnieuw uit na de vertalingen.

**Dependencies:** Taak 10.

**Waarschijnlijke bestanden:**

- `src/EbookManager.App/Resources/Strings/AppResources.fr.resx`
- `src/EbookManager.App/Resources/Strings/AppResources.es.resx`
- `src/EbookManager.App/Resources/Strings/AppResources.it.resx`
- `tests/EbookManager.Tests/App/Resources/MetadataQualityLocalizationTests.cs`

**Omvang:** M, 4 bestanden.

## Taak 12: Documentatie En Volledige Verificatie

**Beschrijving:** Werk de gebruikersgerichte featurestatus en handmatige checklist bij, spiegel Markdown en voer de volledige kwaliteitscontrole uit.

**Acceptatiecriteria:**

- [ ] Featurebeschrijving en README noemen de omkeerbare kwaliteitsuitzonderingen zonder herstelacties te claimen.
- [ ] De handmatige checklist dekt dashboard, heropenen, meerdere signalen, Instellingen, bibliotheekisolatie, cascade en toetsenbordbediening.
- [ ] Alle project-Markdown is aantoonbaar naar Obsidian gespiegeld.

**Verificatie:**

- [ ] Voer `dotnet test EbookManager.sln -c Release --no-restore` uit.
- [ ] Voer `dotnet build EbookManager.sln -c Release --no-restore` uit en vereis nul waarschuwingen en nul fouten.
- [ ] Voer `git diff --check`, zelfreview en de volledige handmatige checklist uit.
- [ ] Open na goedkeuring een normale PR, niet een draft.

**Dependencies:** Taken 1 tot en met 11.

**Waarschijnlijke bestanden:**

- `docs/feature-requests/metadata-quality-dashboard.md`
- `docs/manual-tests/milestone-28-checklist.md`
- `README.md`
- `tasks/plan.md`
- `tasks/todo.md`

**Omvang:** M, 5 bestanden.

## Checkpoint 4: Compleet

- [ ] Alle acceptatiecriteria uit de goedgekeurde specificatie zijn aantoonbaar gehaald.
- [ ] Automatische tests en Release-build zijn schoon.
- [ ] De gebruiker heeft de handmatige checklist uitgevoerd.
- [ ] Alle relevante Markdown-spiegels zijn gecontroleerd.
- [ ] Zelfreview bevat geen openstaande vereiste bevindingen.
- [ ] De normale PR is gereed voor externe review.

## Risico's En Maatregelen

| Risico | Impact | Maatregel |
|---|---|---|
| Gelokaliseerde titels worden per ongeluk als opslagkey gebruikt | Hoog | Sleutels centraal in Domain vastleggen en afzonderlijk testen |
| UI verwijdert een melding terwijl opslaan faalt | Hoog | Repository eerst awaiten; foutpad expliciet testen |
| Uitzonderingen lekken tussen bibliotheken | Hoog | CurrentLibrary-repository gebruiken en bibliotheekwissel testen |
| Verwijderde boeken laten stale records achter | Middel | Databasecascade plus integratietest |
| Observeerbare aantallen/selectie raken inconsistent | Middel | Eerste, middelste, laatste en lege categorie testcases |
| Migratie bevat onbedoelde schemawijzigingen | Middel | Gegenereerde migratiediff handmatig begrenzen |
| Nieuwe beheer-UI verdringt bestaande instellingen | Laag | Bestaande sectie en designresources hergebruiken; layouttest |

## Open Vragen

Geen. Functionele scope en opslagkeuzes zijn goedgekeurd; implementatiedetails mogen alleen binnen deze grenzen worden ingevuld.
