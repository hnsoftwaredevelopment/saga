# Milestone 45 implementatieplan: bulk titel/auteur omwisselen

Goedgekeurde scope en details: docs/superpowers/specs/2026-10-22-saga-milestone-45-bulk-title-author-swap-design.md.

1. Gebruik de bestaande lijstselectie en snapshotcontrole; test dat alleen geselecteerde boeken worden verwerkt.
2. Dashboard verwerkt geschikte geselecteerde regels; test annuleren, gedeeltelijke fouten, overgeslagen boeken en verversing.
3. Korte Ja/Nee-bevestiging met aantal in zes talen; bestaand enkelvoudig venster behouden.
4. Volledige tests, eigen review, checklist, Obsidian-spiegel, gewone PR en controles.

Afhankelijkheden: model vóór dashboard/UI, alles vóór oplevering. De gebruiker bepaalt de selectie in de lijst; een tweede selectie is na gebruikerstest vervallen. Opslag vergelijkt actuele titel/auteur met de oorspronkelijke waarden. Andere metadata wordt door de bestaande service behouden. Geen databasewijziging of dependency.

## Aanvulling binnen Milestone 45: minder onterechte omwisselmeldingen

De gebruiker heeft expliciet gevraagd deze verbetering in dezelfde slice mee te nemen. Eerst de 32 correcte praktijkgevallen en de bevestigde echte omwisseling als regressietests opnemen, dan naamvormen ondersteunen en positieve boektitelaanwijzingen eisen. Daarna veilige opslag en dashboardregressies controleren, volledige tests en Debug-build uitvoeren, zelf reviewen en documentatie/Obsidian/PR bijwerken. De bulkactie blijft geaccepteerd; alleen de aanvullende detectie vraagt nieuwe handmatige controle. Geen metadata-, opslag-, UI- of dependencywijziging.

## Afgerond plan Milestone 44

De gebruiker heeft opdracht gegeven deze slice uit te voeren. Specificatie: docs/superpowers/specs/2026-10-22-saga-milestone-44-bulk-tag-cleanup-design.md.

1. Afzonderlijke previews met oorspronkelijke tags en eigen opgeschoonde tags; tests voor behoud en lege uitkomst.
2. Dashboardselectie en bestaande service aansluiten; oorspronkelijke tags controleren en fouten per boek afhandelen. Hergebruik bestaande batchreconciliatie.
3. Scrollbaar overzicht met expliciete bevestiging en zes vertalingen; gericht testcheckpoint.
4. Volledige tests, Debug-build, eigen review, documentatie, Obsidian en gewone PR.

Stap 2 volgt op 1; de interface volgt op het model; oplevering volgt op alle stappen. Grootste risico is taglijsten vermengen: aparte voorstellen en opslagtests voorkomen dat. Verouderde tags worden vóór schrijven vergeleken. Geen nieuwe opslagroute of dependency.

## Afgerond plan Milestone 43

## Overzicht

Milestone 43 breidt het bestaande enkelvoudige serieherstel uit naar één of meerdere geselecteerde boeken. De bestaande service kan al een verzameling boek-id’s veilig verwerken; de belangrijkste wijzigingen liggen daarom in bulkselectie, dialoogcontext, hergebruik van generieke batchreconciliatie, lokalisatie en testbewijs. De afgesproken Splash-uitlijning wordt vooraf als een afzonderlijke kleine UI-commit uitgevoerd.

## Architectuurbeslissingen

- `MetadataQualitySeriesRepairService` blijft de enige opslagroute voor serieherstel.
- `ReconcileBatchRepair` verwerkt ook serieresultaten; er komt geen derde vrijwel gelijke resultaatverwerker.
- `MetadataQualitySeriesRepairViewModel` krijgt dezelfde één-/meerdere-boekencontext als de auteur- en taalvensters.
- Bestaande serienummers worden uitsluitend als context getoond en nooit gewijzigd.
- De Splash gebruikt een Grid met twee automatische kolommen en gecentreerde kinderen; optische positionering gebeurt alleen op de container.
- Geen databasewijziging, dependency of nieuwe opslagroute.

## Taak 1: robuuste Splash-uitlijning

**Beschrijving:** Vervang de horizontale titel-StackPanel door een Grid dat titel en subtitel onafhankelijk van lettergrootte en DPI verticaal centreert. Behoud de bestaande uitstraling en algemene positie.

**Acceptatiecriteria:**
- [x] Titel en subtitel staan in afzonderlijke automatische Grid-kolommen.
- [x] Beide teksten gebruiken `VerticalAlignment="Center"`.
- [x] De kinderen hebben geen verticale correctiemarges; alleen de titelgroep bepaalt haar schermpositie.

**Verificatie:** Eerst een falende statische Splash-layouttest, daarna gerichte groene test en Debug-build.

**Waarschijnlijk geraakte bestanden:**
- `src/EbookManager.App/Views/Splash.xaml`
- nieuwe of bestaande layouttest onder `tests/EbookManager.Tests/App/Views`.

**Omvang:** Klein.

## Taak 2: serievenster geschikt maken voor één of meerdere boeken

**Beschrijving:** Breid het serieviewmodel en venster uit met context voor één titel/serienummer of een bulkselectie. Behoud de snelle bestaande suggestielijst en vrije invoer.

**Acceptatiecriteria:**
- [x] Eén boek toont ongewijzigd titel en serienummer.
- [x] Meerdere boeken tonen het aantal, de behoudmelding en een bevestigingsknop met het aantal.
- [x] Bekende series en vrije invoer werken even vlot als bij één boek.
- [x] Alle nieuwe teksten bestaan in zes talen en kunnen omlopen.

**Verificatie:** Rode en groene viewmodel-, layout- en lokalisatietests.

**Waarschijnlijk geraakte bestanden:**
- `src/EbookManager.Presentation/ViewModels/MetadataQualitySeriesRepairViewModel.cs`
- `src/EbookManager.App/Views/MetadataQualitySeriesRepairWindow.xaml`
- `src/EbookManager.App/Resources/Strings/AppResources*.resx`
- bijbehorende tests.

**Omvang:** Middelgroot; zes resources vormen één mechanische set.

## Checkpoint na taken 1-2

- [x] Splash-layouttest en serievenstertests zijn groen.
- [x] Enkelvoudige seriecontext is niet gewijzigd.
- [x] Geen productlogica of opslagroute is gedupliceerd.

## Taak 3: bulkselectie en resultaatverwerking aansluiten

**Beschrijving:** Maak `Serie wijzigen` beschikbaar voor iedere geldige selectie binnen de seriecategorie en verwerk de bestaande servicebatch via de generieke reconciliatie.

**Acceptatiecriteria:**
- [x] Eén of meerdere geselecteerde serierijen activeren de actie.
- [x] Annuleren schrijft niets; bevestigen stuurt alle unieke geselecteerde ids en één serienaam.
- [x] Succes, write-backwaarschuwing, verouderd, verwijderd en mislukt worden per boek correct verwerkt.
- [x] Volledig mislukte boeken blijven zichtbaar en geselecteerd; volledig succes geeft een korte groene melding.

**Verificatie:** Eerst falende dashboardtests voor beschikbaarheid, annuleren, succes en gemengde resultaten; daarna groen met minimale productcode.

**Waarschijnlijk geraakte bestanden:**
- `src/EbookManager.Presentation/ViewModels/MetadataQualityDashboardViewModel.cs`
- `tests/EbookManager.Tests/App/ViewModels/MetadataQualityDashboardSeriesRepairTests.cs`
- lokalisatieresources en tests.

**Omvang:** Middelgroot.

## Taak 4: gegevensbehoud en bibliotheekverversing bewijzen

**Beschrijving:** Bewijs dat verschillende serienummers behouden blijven, inmiddels ingevulde series niet worden overschreven en de hoofdbibliotheek plus seriefilters éénmaal verversen.

**Acceptatiecriteria:**
- [x] Meerdere boeken krijgen dezelfde serie en behouden ieder hun oorspronkelijke serienummer.
- [x] Een inmiddels geldige serienaam levert `NotApplicable` op zonder write.
- [x] De bibliotheekregels en seriefilters tonen direct de actuele waarden.
- [x] Overige metadata blijft intact.

**Verificatie:** Gerichte service- en LibraryViewModel-tests, gevolgd door een gericht checkpoint.

**Waarschijnlijk geraakte bestanden:**
- `tests/EbookManager.Tests/Metadata/MetadataQualitySeriesRepairServiceTests.cs`
- `tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs`
- alleen bij bewezen gebrek minimale productcode.

**Omvang:** Klein tot middelgroot.

## Checkpoint na taken 3-4

- [x] Alle gerichte bulkserietests zijn groen.
- [x] Enkelvoudig herstel en bestaande bulkacties blijven groen.
- [x] Opslag, dashboard en bibliotheek tonen dezelfde toestand.

## Taak 5: documentatie, build en oplevering

**Beschrijving:** Werk status en checklist bij, spiegel Markdown, voer de volledige Definition of Done uit en open een gewone niet-draft PR.

**Acceptatiecriteria:**
- [x] README en quality-roadmap beschrijven Milestone 43 zonder vervolgwensen als gerealiseerd te presenteren.
- [x] De handmatige checklist behandelt Splash, selectie, suggesties, verschillende nummers, annuleren, resultaten, filters en herstart.
- [x] Volledige tests en schone Debug-build slagen zonder waarschuwingen of overgeslagen tests.
- [x] De complete diff is zelf beoordeeld en de branch bevat alleen Milestone 43.
- [x] Alle gewijzigde Markdown heeft gelijke hashes in de Obsidian-spiegel.

**Verificatie:** Volledige tests, clean/build, `git diff --check`, hashcontrole, eigen codereview en GitHub-controles in PR #44.

**Omvang:** Middelgroot, hoofdzakelijk verificatie en documentatie.

## Risico's en maatregelen

| Risico | Impact | Maatregel |
|---|---|---|
| Verschillende serienummers worden gelijkgetrokken | Hoog | Service- en acceptatietest met afwijkende nummers; serie-index nooit als input meegeven |
| Inmiddels ingevulde serie wordt overschreven | Hoog | Bestaande `Applies`-controle per boek behouden en expliciet testen |
| Derde batchverwerker wijkt af van auteur/taal | Middel | Seriestatus naar bestaande `MetadataQualityBatchRepairItem` mappen |
| Bulkvenster verliest snel typen of suggestiekeuze | Middel | Bestaande inputevents behouden en regressietests plus handmatige controle |
| Splash verschuift bij DPI of vertaling | Laag | Layoutuitlijning in Grid; geen verticale kindmarges |
| Langere vertalingen worden afgekapt | Middel | Tekst laten omlopen en layouttest uitbreiden |

## Goedkeuringscheckpoint

- [x] Hoofdfunctionaliteit en robuuste Splash-aanpak door gebruiker goedgekeurd.
- [x] Volledige specificatie en implementatieplan door gebruiker bevestigd.
- [x] Implementatie testgestuurd en incrementeel afgerond.
- [x] Definition of Done en gewone PR afgerond; alle automatische en handmatige controles zijn geslaagd.
