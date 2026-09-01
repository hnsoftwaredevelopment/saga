# Milestone 29 Implementatieplan: Ontbrekende Auteur Herstellen

## Overzicht

Milestone 29 voegt de eerste directe herstelactie toe aan de metadata Quality Page. De gebruiker kan voor één geselecteerd boek zonder bruikbare auteur een bekende auteur kiezen of een nieuwe auteur invoeren. Saga slaat de wijziging op via de bestaande `BookService`, beoordeelt het boek opnieuw en werkt het dashboard zonder heropenen bij.

De implementatie blijft bewust verticaal en uitbreidbaar: het interne herstelverzoek accepteert een verzameling boek-id's, maar de UI levert in deze milestone precies één id. Daardoor kan een latere bulkactie dezelfde opslagroute gebruiken.

## Architectuurbeslissingen

- De bestaande `BookService.SaveAsync` blijft de enige schrijfroute voor boekmetadata, sidecar en ondersteunde ebook-write-back.
- Een kleine applicatieservice haalt actuele boeken op, vervangt uitsluitend de auteur en leest het resultaat na opslag opnieuw in.
- De zes kwaliteitsregels worden uit het dashboardviewmodel gehaald en ondergebracht in één herbruikbare evaluator. Initiële opbouw en herevaluatie gebruiken daardoor exact dezelfde regels.
- Het auteursherstel-viewmodel beheert invoer, validatie en suggesties, maar schrijft zelf geen metadata.
- Het WPF-venster retourneert uitsluitend een bevestigde auteur. De dashboardworkflow orkestreert dialoog, opslag en herevaluatie.
- Bekende auteurs worden afgeleid uit de actuele boeken van de actieve bibliotheek; er is geen nieuw repositorycontract of databaseschema nodig.
- Na een auteurswijziging wordt alleen het betrokken boek opnieuw verwerkt. Het volledige dashboard of de volledige bibliotheek wordt niet opnieuw geladen.
- Alle nieuwe UI blijft toetsenbordtoegankelijk en gebruikt de bestaande lokalisatie-infrastructuur.

## Afhankelijkheden

```text
Gedeelde kwaliteitsregel-evaluator
        │
        ├── gerichte dashboard-herevaluatie
        │
Veilige auteursherstelservice
        │
        ├── herstel-viewmodel met suggesties
        │       │
        │       └── compact WPF-herstelvenster
        │
        └── dashboardcommando en LibraryViewModel-koppeling
                │
                └── lokalisatie, documentatie en handmatige controle
```

## Fase 1: Veilige fundamenten

### Taak 1: Herbruikbare kwaliteitsevaluatie

**Beschrijving:** Verplaats de zes bestaande kwaliteitspredicaten naar één presentatie-onafhankelijke evaluator die per boek de toepasselijke stabiele signaalsleutels teruggeeft. Laat het dashboard deze evaluator gebruiken zonder zichtbaar gedrag te wijzigen.

**Acceptatiecriteria:**

- [ ] Alle zes bestaande signalen leveren exact dezelfde resultaten als vóór de extractie.
- [ ] De evaluator kan één boek zelfstandig opnieuw beoordelen.
- [ ] Bestaande uitzonderingen blijven uitsluitend de exacte boek-/signaalcombinatie verbergen.

**Verificatie:**

- [ ] Gerichte evaluatortests en bestaande dashboardtests slagen.
- [ ] `dotnet test tests/EbookManager.Tests/EbookManager.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~MetadataQuality` slaagt.

**Afhankelijkheden:** Geen.

**Waarschijnlijk geraakte bestanden:**

- `src/EbookManager.Application/Metadata/MetadataQualitySignalEvaluator.cs`
- `src/EbookManager.Presentation/ViewModels/MetadataQualityDashboardViewModel.cs`
- `tests/EbookManager.Tests/Metadata/MetadataQualitySignalEvaluatorTests.cs`
- `tests/EbookManager.Tests/App/ViewModels/MetadataQualityDashboardViewModelTests.cs`

**Omvang:** Middelgroot.

### Taak 2: Veilige applicatieservice voor auteursherstel

**Beschrijving:** Voeg een applicatieservice toe die één of meer actuele boeken ophaalt, uitsluitend hun auteur vervangt, via `BookService.SaveAsync` opslaat en de opgeslagen boeken opnieuw leest. De eerste UI gebruikt één id, maar de service rapporteert resultaten per boek voor later bulkgebruik.

**Acceptatiecriteria:**

- [ ] Lege invoer, `Unknown` en een lege id-verzameling worden afgewezen zonder schrijfactie.
- [ ] Alleen `Authors` en `UpdatedUtc` veranderen; overige metadata en boekstatus blijven gelijk.
- [ ] Conflicten en opslagfouten worden per boek teruggegeven en niet verborgen.
- [ ] Een succesvol resultaat bevat het opnieuw ingelezen actuele boek.

**Verificatie:**

- [ ] Testen bewijzen succes, validatie, gedeeltelijke foutafhandeling en behoud van overige metadata.
- [ ] Bestaande `BookService`-tests blijven groen.

**Afhankelijkheden:** Geen.

**Waarschijnlijk geraakte bestanden:**

- `src/EbookManager.Application/Metadata/MetadataQualityAuthorRepairService.cs`
- `tests/EbookManager.Tests/Metadata/MetadataQualityAuthorRepairServiceTests.cs`

**Omvang:** Klein.

## Checkpoint 1: Fundament

- [ ] Gerichte tests voor evaluator en herstelservice zijn groen.
- [ ] De solution bouwt zonder waarschuwingen.
- [ ] Zelfreview bevestigt dat geen schemawijziging, dependency of onbedoelde metadatawijziging is toegevoegd.

## Fase 2: Invoer en compact herstelvenster

### Taak 3: Auteursinvoer en suggesties testgedreven modelleren

**Beschrijving:** Bouw een zelfstandig `MetadataQualityAuthorRepairViewModel` voor boektitel, vrije auteursinvoer, gefilterde suggesties, validatie en bevestigbaarheid.

**Acceptatiecriteria:**

- [ ] Lege en `Unknown`-waarden zijn ongeldig; een nieuwe getrimde auteur is geldig.
- [ ] Lege/ongeldige auteurs verdwijnen uit de bron en bekende auteurs worden hoofdletterongevoelig ontdubbeld.
- [ ] Prefixtreffers verschijnen vóór overige gedeeltelijke treffers en iedere groep is cultuurgevoelig alfabetisch.
- [ ] Selectie van een suggestie neemt de bestaande schrijfwijze exact over.

**Verificatie:**

- [ ] Viewmodeltests dekken lege invoer, vrije invoer, filtering, sortering, ontdubbeling en selectie.

**Afhankelijkheden:** Geen.

**Waarschijnlijk geraakte bestanden:**

- `src/EbookManager.Presentation/ViewModels/MetadataQualityAuthorRepairViewModel.cs`
- `tests/EbookManager.Tests/App/ViewModels/MetadataQualityAuthorRepairViewModelTests.cs`

**Omvang:** Klein.

### Taak 4: Herstelvenster en interactiecontract

**Beschrijving:** Voeg een compact modaal WPF-venster toe met boektitel, bewerkbare auteursinvoer, live suggestielijst, validatiemelding, opslaan en annuleren. Verbind het venster via de bestaande gebruikersinteractieservice.

**Acceptatiecriteria:**

- [ ] Het auteursveld krijgt bij openen focus en behoudt vrije invoer.
- [ ] De suggestielijst reageert tijdens typen en is met toetsenbord en muis bedienbaar.
- [ ] Opslaan sluit alleen bij geldige invoer; annuleren en `Escape` geven geen resultaat terug.
- [ ] Alle interactieve onderdelen hebben duidelijke toegankelijke namen.

**Verificatie:**

- [ ] Layouttests bewijzen bindings, toegankelijkheidsnamen, standaard-/annuleerknoppen en minimumafmetingen.
- [ ] De WPF-projectbuild slaagt.

**Afhankelijkheden:** Taak 3.

**Waarschijnlijk geraakte bestanden:**

- `src/EbookManager.Presentation/Abstractions/IUserInteractionService.cs`
- `src/EbookManager.App/Services/UserInteractionService.cs`
- `src/EbookManager.App/Views/MetadataQualityAuthorRepairWindow.xaml`
- `src/EbookManager.App/Views/MetadataQualityAuthorRepairWindow.xaml.cs`
- `tests/EbookManager.Tests/App/Views/MetadataQualityAuthorRepairWindowLayoutTests.cs`

**Omvang:** Middelgroot.

## Checkpoint 2: Invoerervaring

- [ ] Alle herstel-viewmodel- en layouttests zijn groen.
- [ ] Het venster kan handmatig met bekende en nieuwe auteurs worden bediend.
- [ ] Toetsenbordfocus, `Enter` en `Escape` werken zoals gespecificeerd.
- [ ] Gebruiker keurt de invoerervaring goed voordat metadataopslag aan de Quality Page wordt gekoppeld.

## Fase 3: End-to-end Quality Page-herstel

### Taak 5: Dashboardcommando en gerichte herevaluatie

**Beschrijving:** Voeg `Herstellen` toe aan het dashboardviewmodel voor een geselecteerde `missing-author`-rij. Open het herstelvenster, roep de applicatieservice aan en verwerk het opnieuw ingelezen boek in alle kwaliteitscategorieën.

**Acceptatiecriteria:**

- [ ] `Herstellen` is uitsluitend beschikbaar voor een geselecteerd boek onder `Ontbrekende auteur`.
- [ ] Annuleren veroorzaakt geen schrijfactie of dashboardwijziging.
- [ ] Succes vervangt zichtbare boekwaarden, herevalueert alle signalen en actualiseert aantallen en selectie.
- [ ] Bestaande `Dit is correct`-uitzonderingen blijven gerespecteerd.
- [ ] Opslagfouten houden de kwaliteitsrij zichtbaar en tonen een begrijpelijke status.

**Verificatie:**

- [ ] Dashboardtests dekken commandobeschikbaarheid, annuleren, succes, fouten, andere signalen, uitzonderingen en vervolgselectie.

**Afhankelijkheden:** Taken 1 tot en met 4.

**Waarschijnlijk geraakte bestanden:**

- `src/EbookManager.Presentation/ViewModels/MetadataQualityDashboardViewModel.cs`
- `tests/EbookManager.Tests/App/ViewModels/MetadataQualityDashboardViewModelTests.cs`

**Omvang:** Middelgroot.

### Taak 6: Actieve bibliotheek en WPF-dashboard verbinden

**Beschrijving:** Registreer de herstelservice, geef haar vanuit `LibraryViewModel` aan het dashboard en voeg de zichtbare herstelactie aan het bestaande kwaliteitsscherm toe.

**Acceptatiecriteria:**

- [ ] De actieve bibliotheek bepaalt zowel de actuele boekopslag als de auteursuggesties.
- [ ] Wisselen van bibliotheek kan geen auteurs of boeken uit de vorige bibliotheek wijzigen.
- [ ] Na herstel toont de hoofdbibliotheek de nieuwe auteur zonder applicatieherstart.
- [ ] De bestaande splitter, `Dit is correct`, dubbelklik en `Openen in bibliotheek` blijven werken.

**Verificatie:**

- [ ] LibraryViewModel-tests bewijzen actieve-bibliotheekisolatie en bijgewerkte bibliotheekgegevens.
- [ ] Dashboard-layouttests bewijzen de nieuwe knopbinding en toegankelijke naam.

**Afhankelijkheden:** Taak 5.

**Waarschijnlijk geraakte bestanden:**

- `src/EbookManager.App/App.xaml.cs`
- `src/EbookManager.Presentation/ViewModels/LibraryViewModel.cs`
- `src/EbookManager.App/Views/MetadataQualityDashboardWindow.xaml`
- `tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs`
- `tests/EbookManager.Tests/App/Views/MetadataQualityDashboardWindowLayoutTests.cs`

**Omvang:** Middelgroot.

## Checkpoint 3: Werkende herstelflow

- [ ] Gerichte en volledige tests zijn groen.
- [ ] De Release-build heeft nul waarschuwingen en nul fouten.
- [ ] De gebruiker herstelt handmatig een bekende en een nieuwe auteur vanuit de Quality Page.
- [ ] De gebruiker bevestigt dat dashboard en hoofdbibliotheek direct correct worden bijgewerkt.

## Fase 4: Lokalisatie en afronding

### Taak 7: Volledige lokalisatie en toegankelijkheidscontrole

**Beschrijving:** Voeg alle nieuwe teksten toe aan de zes ondersteunde talen en versterk de resource- en layouttests.

**Acceptatiecriteria:**

- [ ] Basis/Engels, Nederlands, Duits, Frans, Spaans en Italiaans bevatten alle nieuwe sleutels.
- [ ] Geen interne resource- of signaalsleutel is zichtbaar in de workflow.
- [ ] Labels, uitleg, validatie en fouten zijn begrijpelijk en contextspecifiek.

**Verificatie:**

- [ ] Gerichte lokalisatietests slagen voor alle zes resourcebestanden.
- [ ] Handmatige Nederlandse tekstcontrole is goedgekeurd.

**Afhankelijkheden:** Taken 4 tot en met 6.

**Waarschijnlijk geraakte bestanden:**

- `src/EbookManager.App/Resources/Strings/AppResources*.resx`
- `tests/EbookManager.Tests/App/Resources/MetadataQualityLocalizationTests.cs`

**Omvang:** Middelgroot door zes resourcebestanden, maar mechanisch en geïsoleerd.

### Taak 8: Documentatie, checklist en eindcontrole

**Beschrijving:** Werk featurestatus en README bij, voeg de handmatige Milestone 29-checklist toe, spiegel Markdown en voer de volledige kwaliteitscontrole uit.

**Acceptatiecriteria:**

- [ ] Documentatie beschrijft één-boeks auteursherstel en noemt bulkherstel expliciet als vervolg.
- [ ] De checklist dekt bekende auteur, nieuwe auteur, annuleren, fouten, herevaluatie, toetsenbord en bibliotheekisolatie.
- [ ] Alle gewijzigde Markdown is met gelijke SHA-256-hash in Obsidian aanwezig.
- [ ] Zelfreview vindt geen blokkerende correctheids-, toegankelijkheids-, beveiligings- of performanceproblemen.

**Verificatie:**

- [ ] `dotnet test EbookManager.sln -c Release --no-restore`
- [ ] `dotnet build EbookManager.sln -c Release --no-restore`
- [ ] `git diff --check`
- [ ] Normale, niet-draft PR gereed voor review.

**Afhankelijkheden:** Taken 1 tot en met 7.

**Waarschijnlijk geraakte bestanden:**

- `README.md`
- `docs/feature-requests/metadata-quality-dashboard.md`
- `docs/manual-tests/milestone-29-checklist.md`
- `tasks/plan.md`
- `tasks/todo.md`

**Omvang:** Middelgroot.

## Checkpoint 4: Milestone gereed

- [ ] Alle acceptatiecriteria en handmatige controles zijn geslaagd.
- [ ] Alle tests en de Release-build zijn groen zonder waarschuwingen.
- [ ] Markdownspiegel is gecontroleerd.
- [ ] Zelfreview is afgerond.
- [ ] Normale PR is geopend en klaar voor externe review.

## Risico's en maatregelen

| Risico | Impact | Maatregel |
|---|---|---|
| Opslag overschrijft recentere wijzigingen | Hoog | Boek vlak vóór herstel opnieuw ophalen en uitsluitend auteurs vervangen. |
| Dashboard gebruikt andere regels dan initiële scan | Hoog | Eén gedeelde evaluator voor opbouw en herevaluatie. |
| Bekende auteurs uit een andere bibliotheek lekken in suggesties | Hoog | Suggesties uitsluitend opbouwen uit de actuele actieve bibliotheek. |
| Write-back is gedeeltelijk succesvol | Middel | Bestaande `BookSaveResult` per bestand behouden, fout tonen en opnieuw ingelezen databasewerkelijkheid gebruiken. |
| Suggestielijst wordt traag bij grote bibliotheken | Middel | Eenmalig genormaliseerde, ontdubbelde auteurslijst in geheugen; alleen filteren tijdens typen. |
| Vrije invoer wordt per ongeluk vervangen door een suggestie | Middel | Selectie is expliciet; tekst blijft leidend totdat de gebruiker kiest. |
| Nieuwe herstelactie verstoort bestaande dashboardknoppen | Middel | Bestaande navigatie-, splitter- en exclusietests uitbreiden, niet vervangen. |
| Eerste contract blokkeert bulkherstel | Middel | Applicatieservice en resultaat per verzameling boek-id's modelleren; UI blijft voorlopig enkelvoudig. |

## Niet in deze milestone

- Meerdere boeken selecteren of in één actie herstellen.
- Meerdere auteurs in het compacte herstelvenster toevoegen.
- Auteurs extern opzoeken of automatisch raden.
- Andere kwaliteitsregels direct herstellen.
- De `Dit is correct`-tekstknop door een icoon vervangen.
- Database- of migratiewijzigingen.

## Open vragen

Geen blokkerende vragen. De specificatie is op 1 september 2026 door de gebruiker goedgekeurd.
