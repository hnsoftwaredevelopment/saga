# Milestone 39 implementatieplan: bulkherstel ontbrekende auteur

## Overzicht

Milestone 39 breidt het bestaande veilige auteurherstel uit van één naar meerdere geselecteerde boeken binnen `Ontbrekende auteur`. De applicatieservice ondersteunt batches al; het werk concentreert zich op dashboardselectie, begrijpelijke context, resultaatverwerking en regressiebewaking.

## Architectuurbeslissingen

- De huidige `MetadataQualityAuthorRepairService` blijft de enige opslagroute en verwerkt ieder boek onafhankelijk.
- De dashboardselectie wordt vlak voor uitvoering vastgelegd en gefilterd op de actuele categorie.
- Het auteursvenster is de expliciete bevestiging en toont enkelvoudige of bulkcontext zonder een redundante tweede vraag.
- Het dashboard reconcilieert ieder itemresultaat met de werkelijk opnieuw ingelezen `Book`.
- Volledig mislukte rijen blijven zichtbaar en geselecteerd; opgeslagen boeken verdwijnen wanneer het signaal is opgelost.
- De lokale gebruikerswijzigingen in `BookDetailsView.xaml` en `SettingsWindow.xaml` blijven buiten deze milestone.

## Taak 1: bulkcontract van auteursinvoer

**Beschrijving:** Voeg eerst falende tests toe voor enkelvoudige en meervoudige context in het auteurvenster en breid het viewmodel minimaal uit.

**Acceptatiecriteria:**
- [x] Eén boek toont de bestaande titelcontext.
- [x] Meerdere boeken tonen het juiste aantal.
- [x] Suggesties, vrije invoer en validatie blijven gelijk.

**Verificatie:** Gerichte `MetadataQualityAuthorRepairViewModelTests` gaan rood en daarna groen.

**Bestanden:** auteurherstelviewmodel en bijbehorende tests.

## Taak 2: dashboardbatch en deelresultaten

**Beschrijving:** Schrijf falende dashboardtests en laat de bestaande herstelopdracht alle actuele geselecteerde ontbrekende-auteurrijen verwerken.

**Acceptatiecriteria:**
- [x] De opdracht accepteert één of meerdere rijen uitsluitend onder `Ontbrekende auteur`.
- [x] Annuleren schrijft niets; succes verwerkt alle geselecteerde id’s.
- [x] Gemengde resultaten verversen opgeslagen boeken en behouden mislukte rijen en selectie.

**Verificatie:** Gerichte dashboardtests bewijzen succes, annuleren, waarschuwing, stale, not-found, fout en onverwachte uitzondering.

**Bestanden:** dashboardviewmodel en dashboardtests.

## Taak 3: WPF-context en gelokaliseerde samenvatting

**Beschrijving:** Maak bulkcontext en resultaatmeldingen begrijpelijk en toegankelijk in alle zes talen.

**Acceptatiecriteria:**
- [x] Het venster toont het juiste aantal geselecteerde boeken en een duidelijke bevestigingsactie.
- [x] Resultaatmeldingen onderscheiden volledig succes, write-backwaarschuwingen en mislukkingen.
- [x] Toetsenbordgedrag, focus en auteursuggesties regresseren niet.

**Verificatie:** Layout-, lokalisatie- en compositietests groen.

**Bestanden:** auteurvenster, resources, interactiecompositie en bijbehorende tests.

## Taak 4: bibliotheekverversing en oplevering

**Beschrijving:** Bewaak de doorwerking naar hoofdbibliotheek en auteursfilters, werk documentatie bij en lever een actuele Debug-build en gewone PR.

**Acceptatiecriteria:**
- [x] Alle gerepareerde boeken en auteursfilters zijn zonder herstart actueel.
- [x] Volledige tests en Debug-build zijn groen (761 tests; Saga 2026.10.1.67).
- [x] Handmatige checklist en Obsidian-spiegel zijn gereed.
- [x] De branchdiff bevat de lokale gebruikerswijzigingen niet.

**Verificatie:** LibraryViewModel-tests, volledige Definition of Done, diffreview en GitHub-controles.

**Bestanden:** LibraryViewModel-tests, README, featurestatus, checklist en taakstatus.

## Risico’s en maatregelen

| Risico | Maatregel |
|---|---|
| Een geldige auteur wordt door verouderde dashboarddata overschreven | Service leest elk boek opnieuw en retourneert `NotApplicable` |
| Een fout halverwege wordt ten onrechte als volledig succes getoond | Ieder item heeft een status; UI reconcilieert en rapporteert per uitkomstgroep |
| Mislukte rijen verdwijnen uit beeld | Alleen actuele opgeslagen boeken worden herevalueerd; `Failed` blijft geselecteerd |
| Grote selectie blokkeert de interface langdurig | Bestaande asynchrone opdracht gebruiken en geen extra database- of netwerkwerk toevoegen |
| Bulkselectie activeert andere enkelvoudige reparaties | Alleen auteurherstel krijgt bulkondersteuning; overige opdrachten vereisen `SelectedBookCount == 1` |

## Checkpoint

- [x] Specificatie en plan goedgekeurd.
- [x] Gerichte tests per increment groen.
- [x] Volledige tests en Debug-build groen (761 tests; Saga 2026.10.1.67).
- [x] Handmatige checklist gereed.
- [x] Gewone PR #40 geopend; mergebaarheid en controles worden op GitHub gevolgd.

## Gepauzeerd onderhoud

De eenmalige hydratatie van OneDrive-omslagen blijft gepauzeerd totdat OneDrive de opslagmigratie online volledig heeft verwerkt.
