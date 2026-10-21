# Milestone 41 implementatieplan: bulk taalherstel

## Overzicht

Milestone 41 breidt het bestaande herstel van `Ontbrekende of onbekende taal` uit van één boek naar één of meerdere geselecteerde boeken. De bestaande applicatieservice verwerkt al meerdere boek-id’s en blijft de veilige opslagroute. De wijziging richt zich daarom op een duidelijke bulkcontext in het taalvenster, resultaatgedreven dashboardverwerking, één verversing van de hoofdbibliotheek en begrijpelijke gelokaliseerde terugkoppeling.

## Architectuurbeslissingen

- `MetadataQualityLanguageRepairService` blijft verantwoordelijk voor opnieuw inlezen, toepasbaarheidscontrole, normalisatie en opslag per boek.
- `MetadataQualityDashboardViewModel` verzamelt uitsluitend actuele geselecteerde rijen uit `unknown-language` en verwerkt ieder batchresultaat afzonderlijk.
- De bestaande bulkauteurworkflow is het patroon voor selectieherstel, tellingen, waarschuwingen en één gebundelde bibliotheekverversing.
- `MetadataQualityLanguageRepairViewModel` krijgt dezelfde enkelvoudige/bulkcontext als het auteursvenster, maar behoudt de bestaande gesloten lijst van geldige talen.
- Andere herstelacties blijven enkelvoudig; er komt geen databasewijziging, dependency of externe taaldetectie.

## Afhankelijkheden

```text
Taaldialoogcontext en contract
        │
        └── Dashboardbulkverwerking
                  │
                  └── Lokalisatie en WPF-layout
                            │
                            └── Integratie, documentatie en acceptatiebuild
```

## Taak 1: taaldialoog geschikt maken voor één of meerdere boeken

**Beschrijving:** Breid het taalviewmodel uit met een enkelvoudige titelcontext, bulkcontext, geselecteerd aantal en passende bevestigingstekst zonder de bestaande taalkeuze te veranderen.

**Acceptatiecriteria:**
- [x] Eén boek toont de boektitel en de bestaande actie `Taal wijzigen`.
- [x] Meerdere boeken tonen het aantal en een expliciete bulkbevestiging.
- [x] Een geldige taalkeuze blijft vereist; annuleren heeft geen effect.

**Verificatie:** Eerst falende en daarna groene `MetadataQualityLanguageRepairViewModelTests`.

**Afhankelijkheden:** Geen.

**Waarschijnlijk geraakte bestanden:**
- `src/EbookManager.Presentation/ViewModels/MetadataQualityLanguageRepairViewModel.cs`
- `tests/EbookManager.Tests/App/ViewModels/MetadataQualityLanguageRepairViewModelTests.cs`

**Omvang:** Klein.

## Taak 2: geselecteerde taalproblemen als batch verwerken

**Beschrijving:** Maak `Taal wijzigen` beschikbaar voor iedere niet-lege selectie binnen de taalcategorie en verwerk alle service-uitkomsten volgens de werkelijke opgeslagen toestand.

**Acceptatiecriteria:**
- [x] De opdracht ontvangt alle unieke geselecteerde boek-id’s in stabiele volgorde.
- [x] Succes, write-backwaarschuwing, niet meer toepasselijk, verwijderd en mislukt worden afzonderlijk gereconcilieerd.
- [x] Volledig mislukte rijen blijven geselecteerd; anders wordt de logisch volgende rij geselecteerd.
- [x] De hoofdbibliotheek en taalfilters worden eenmaal met alle gereconcilieerde boeken bijgewerkt.

**Verificatie:** Eerst falende en daarna groene `MetadataQualityDashboardViewModelTests`, inclusief annuleren en onverwachte fout.

**Afhankelijkheden:** Taak 1.

**Waarschijnlijk geraakte bestanden:**
- `src/EbookManager.Presentation/ViewModels/MetadataQualityDashboardViewModel.cs`
- `tests/EbookManager.Tests/App/ViewModels/MetadataQualityDashboardViewModelTests.cs`

**Omvang:** Middelgroot.

## Checkpoint na taken 1-2

- [x] Gerichte viewmodel- en dashboardtests zijn groen.
- [x] Enkelvoudig taalherstel en bulkauteurherstel zijn niet veranderd.
- [x] De branch bevat twee afzonderlijk verifieerbare increments.

## Taak 3: bulkcontext en uitkomsten begrijpelijk tonen

**Beschrijving:** Bind contextlabel, contexttekst en bevestigingstekst in het taalvenster en voeg alle benodigde teksten toe in de zes ondersteunde talen.

**Acceptatiecriteria:**
- [x] Het venster toont bij bulk het juiste aantal geselecteerde boeken.
- [x] Volledig succes gebruikt een korte groene melding; gemengde resultaten tonen alle relevante aantallen.
- [x] Teksten en toegankelijke namen zijn aanwezig in Engels, Nederlands, Duits, Frans, Spaans en Italiaans.

**Verificatie:** Eerst falende en daarna groene layout- en lokalisatietests.

**Afhankelijkheden:** Taken 1-2.

**Waarschijnlijk geraakte bestanden:**
- `src/EbookManager.App/Views/MetadataQualityLanguageRepairWindow.xaml`
- `src/EbookManager.App/Resources/Strings/AppResources*.resx`
- bijbehorende layout- en lokalisatietests onder `tests/EbookManager.Tests/App`.

**Omvang:** Middelgroot; de resourcebestanden zijn één mechanische vertaalset.

## Taak 4: bibliotheekintegratie en regressies bewijzen

**Beschrijving:** Bewijs dat meerdere opgeslagen taalwijzigingen in één keer de actieve bibliotheek, zichtbare rijen en taalfilters bijwerken en dat andere kwaliteitsacties hun selectiegrenzen behouden.

**Acceptatiecriteria:**
- [x] De bibliotheek ontvangt alle gereconcilieerde boeken in één callback.
- [x] Taalfilters en zichtbare metadata zijn zonder herstart actueel.
- [x] Openen, omslag, serie, tags en titel/auteur blijven bij meerdere selectie uitgeschakeld; bulkauteurherstel blijft werken.

**Verificatie:** Gerichte `LibraryViewModelTests` en dashboardregressietests.

**Afhankelijkheden:** Taken 2-3.

**Waarschijnlijk geraakte bestanden:**
- `tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs`
- zo nodig een minimale aanpassing in `src/EbookManager.Presentation/ViewModels/LibraryViewModel.cs`.

**Omvang:** Klein.

## Checkpoint na taken 3-4

- [x] Alle gerichte tests zijn groen.
- [x] De WPF-app bouwt zonder waarschuwingen of fouten.
- [x] De volledige bulktaalstroom is klaar voor handmatige acceptatie.

## Taak 5: documentatie, volledige verificatie en oplevering

**Beschrijving:** Werk featurestatus, README, voortgang en handmatige checklist bij, spiegel Markdown naar Obsidian en voer de volledige Definition of Done uit.

**Acceptatiecriteria:**
- [x] De handmatige checklist behandelt één boek, meerdere boeken, zoeken, annuleren, gemengde resultaten en regressies.
- [x] De volledige testset en schone Debug-build zijn groen met .NET 10.
- [x] De branchdiff bevat uitsluitend Milestone 41 en PR #42 is als gewone, niet-draft PR geopend.

**Verificatie:** Volledige tests, build/publish, `git diff --check`, eigen codereview en GitHub-controles.

**Afhankelijkheden:** Taken 1-4.

**Waarschijnlijk geraakte bestanden:**
- `README.md`
- `docs/feature-requests/metadata-quality-dashboard.md`
- `docs/manual-tests/milestone-41-checklist.md`
- `tasks/plan.md`
- `tasks/todo.md`

**Omvang:** Middelgroot.

## Risico’s en maatregelen

| Risico | Impact | Maatregel |
|---|---|---|
| Een boek krijgt intussen een geldige taal | Middel | Service leest ieder boek opnieuw en retourneert `NotApplicable` zonder overschrijven |
| Een deel van de batch faalt | Middel | Resultaten per boek verwerken; mislukte rijen zichtbaar en geselecteerd houden |
| Dashboard en hoofdbibliotheek lopen uiteen | Hoog | Actuele teruggelezen boeken verzamelen en één gebundelde callback gebruiken |
| Bulkactie wordt buiten de taalcategorie actief | Middel | Commandvoorwaarden en regressietests per signaal en selectieomvang |
| Vertalingen maken de knop of context onleesbaar | Laag | Dynamische knoptekst, tekstomloop en layouttests voor langere labels |

## Open vragen

Er zijn geen blokkerende open vragen. De geaccepteerde specificatie en het bulkauteurpatroon bepalen het gedrag.

## Checkpoint

- [x] Specificatie door gebruiker goedgekeurd.
- [x] Implementatieplan en taakvolgorde door gebruiker goedgekeurd.
- [x] Rode en groene tests per increment vastgelegd.
- [x] Eigen codereview afgerond; de gedeelde batchresultaatverwerking voorkomt dubbele auteur- en taallogica.
- [x] Gewone, niet-draft PR #42 geopend.
- [x] CodeRabbit-bevinding over een onverwachte fout per boek met een regressietest opgelost.
- [x] Handmatige acceptatie door gebruiker afgerond; de workflow is herkenbaar en talen worden correct opgeslagen.
- [x] Volledige Definition of Done afgerond.
- [x] GitHub-controles na de laatste correctie opnieuw beoordeeld: CodeQL en GitGuardian groen; CodeRabbit-bevinding opgelost.
