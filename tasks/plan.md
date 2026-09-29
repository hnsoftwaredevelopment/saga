# Milestone 35 Implementatieplan: Responsieve boekenlijst

## Overzicht

Milestone 35 pakt de twee gemeten oorzaken van stroperigheid afzonderlijk aan: herhaald volledig zoeken en detailherladen bij iedere toetsaanslag, en synchroon lezen en decoderen van OneDrive-omslagen tijdens scrollen.

## Afhankelijkheden

```text
Reproduceerbare metingen
        │
        ├── herbruikbare zoekindex
        │       │
        │       └── uitgesteld filteren en stabiele selectie
        │
        └── asynchrone, begrensd gecachte omslagen
                        │
                        └── praktijktest op echte bibliotheek
```

## Taak 1: Zoekwerk meten en indexeren

**Beschrijving:** Leg het huidige zoekwerk vast met een grote deterministische gegevensset en hergebruik daarna de kostbare zoekwaarden per ongewijzigd boek.

**Acceptatiecriteria:**

- [x] Dezelfde velden en cultuurafhankelijke weergavewaarden blijven doorzoekbaar.
- [x] Een herhaalde zoekactie bouwt de zoekwaarden niet opnieuw op.
- [x] De filterfase over minstens 30.000 representatieve boeken blijft onder 250 ms op de ontwikkellaptop.

**Verificatie:** Eerst falende tests in `BookSearchServiceTests`; daarna 22 gerichte zoektests groen. De gemeten filteractie over 30.000 boeken blijft onder 250 ms.

## Taak 2: Typen en selectie ontkoppelen van duur werk

**Beschrijving:** Voeg een korte annuleerbare zoekvertraging toe en hergebruik boekregels zodat alleen de laatste invoer filtert en dezelfde selectie geen details herlaadt.

**Acceptatiecriteria:**

- [ ] Snel opeenvolgende zoekteksten passen alleen de laatste zoekwaarde toe.
- [ ] Facet-, sorteer- en expliciete verversingsacties blijven direct.
- [ ] Een geselecteerd boek dat zichtbaar blijft veroorzaakt geen nieuwe detailquery.

**Verificatie:** Eerst falende `LibraryViewModelTests`; daarna gerichte viewmodeltests en Checkpoint 1.

## Checkpoint 1

- [ ] Gerichte zoek- en viewmodeltests zijn groen.
- [ ] De gemeten filterfase voldoet aan de afgesproken grens.
- [ ] De volledige oplossing bouwt zonder waarschuwingen.
- [ ] Tussentijdse diff is beoordeeld op correctheid, eenvoud en threadveiligheid.

## Taak 3: Asynchrone, begrensde omslaglader

**Beschrijving:** Bouw een kleine WPF-specifieke lader die direct een placeholder toont, bestanden buiten de UI-thread decodeert, verouderde resultaten negeert en recente afbeeldingen begrensd hergebruikt.

**Acceptatiecriteria:**

- [ ] Schijf- en OneDrive-I/O vindt niet op de UI-thread plaats.
- [ ] Een gerecyclede regel kan nooit de omslag van een eerder boek tonen.
- [ ] Ontbrekende of ongeldige afbeeldingen blijven een stille placeholder.
- [ ] De cache heeft een vaste bovengrens.

**Verificatie:** Eerst falende lader- en cachetests; daarna gerichte tests groen.

## Taak 4: Boekenweergaven omschakelen

**Beschrijving:** Gebruik de nieuwe omslaglader in de boekenplank, detailgrid en configureerbare lijstweergave zonder de detailpaneel-editor te wijzigen.

**Acceptatiecriteria:**

- [ ] Alle drie bibliotheekweergaven tonen titels direct en laden omslagen daarna.
- [ ] Scrollen, Page Up/Page Down, selectie en weergavewissels blijven correct.
- [ ] Thema's, placeholders en kolomzichtbaarheid blijven intact.

**Verificatie:** XAML/layouttests, gerichte UI-logica-tests en Checkpoint 2.

## Checkpoint 2

- [ ] Volledige testsuite slaagt.
- [ ] Debug-build bevat 0 waarschuwingen en 0 fouten.
- [ ] Zelfreview op correctheid, architectuur, beveiliging en prestaties is afgerond.

## Taak 5: Praktijkacceptatie en oplevering

**Beschrijving:** Maak een handmatige checklist en actuele Debug-build voor de echte bibliotheek, spiegel documentatie naar Obsidian en open een normale PR.

**Acceptatiecriteria:**

- [ ] Typen, wissen, geen resultaten, selectie en detailweergave zijn gecontroleerd.
- [ ] Scrollbar, muiswiel, Page Up/Page Down en snel wisselen van richting zijn gecontroleerd.
- [ ] OneDrive-omslagen verschijnen zonder zichtbare blokkade en ontbrekende omslagen blijven veilig.
- [ ] Definition of Done, documentatiespiegel en PR-controles zijn afgerond.

**Verificatie:** Handmatige checklist op circa 34.447 boeken en vergelijking van `performance.log` vóór en na.

## Risico's en beheersing

| Risico | Impact | Beheersing |
|---|---|---|
| Een achtergrondresultaat komt bij een gerecyclede rij terecht | Verkeerde omslag bij boek | Pad- en versienummer opnieuw controleren vóór UI-toewijzing |
| Cache gebruikt te veel geheugen | Langdurig groeiend geheugengebruik | Vaste bovengrens en alleen gedecodeerde miniaturen bewaren |
| Debounce vertraagt andere filters | Interface voelt minder direct | Alleen algemene tekstinvoer vertragen; overige acties direct laten |
| Zoekindex raakt verouderd na metadatawijziging | Onjuiste zoekresultaten | Index koppelen aan de actuele onveranderlijke boekinstantie en vervangen bij update |

## Open vragen

Geen blokkerende vragen; implementatie start pas na goedkeuring van specificatie en plan.
