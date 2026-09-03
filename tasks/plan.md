# Milestone 32 Implementatieplan: Titel en auteur omwisselen

## Overzicht

Milestone 32 voegt één bevestigde herstelroute toe aan het bestaande metadata-kwaliteitsscherm. De implementatie hergebruikt de patronen van milestones 29 tot en met 31 en voegt geen databasewijziging of dependency toe.

## Afhankelijkheden

```text
Veilige applicatieservice
        │
        ├── bevestigingsviewmodel en venster
        │           │
        └── dashboardcommando en bibliotheekverversing
                    │
                    └── lokalisatie, checklist en Debug-build
```

## Taak 1: Veilige omwisselservice

**Beschrijving:** Voeg een applicatieservice toe die het actuele boek ophaalt, de kwaliteitsregel opnieuw controleert, titel en enige auteur omwisselt en via `BookService` opslaat.

**Acceptatiecriteria:**

- [ ] Alleen een nog toepasselijk boek met één bruikbare auteur wordt gewijzigd.
- [ ] Alleen titel, auteur en `UpdatedUtc` veranderen.
- [ ] Succes, niet van toepassing, niet gevonden, conflict en write-backwaarschuwing worden expliciet gerapporteerd.

**Verificatie:** Eerst falende servicetests, daarna gerichte groene tests.

**Omvang:** Klein, 2 bestanden.

## Taak 2: Bevestigingsmodel en WPF-venster

**Beschrijving:** Toon huidige en nieuwe titel/auteur in een compact modaal venster met `Omwisselen`, `Annuleren` en `Escape`.

**Acceptatiecriteria:**

- [ ] De voor/na-weergave bevat geen bewerkbare velden en is direct begrijpelijk.
- [ ] De standaard- en annuleeractie werken volledig met toetsenbord.
- [ ] Alle zichtbare en toegankelijke teksten komen uit resources.

**Verificatie:** Eerst falende viewmodel- en layouttests, daarna WPF-build.

**Omvang:** Middelgroot, 5 bestanden.

## Checkpoint 1

- [ ] Service-, viewmodel- en layouttests zijn groen.
- [ ] De solution bouwt zonder waarschuwingen.
- [ ] De feature is nog niet zichtbaar zolang de dashboardkoppeling ontbreekt.

## Taak 3: Quality Page en bibliotheek koppelen

**Beschrijving:** Voeg de contextgebonden actie toe, verwerk het resultaat en ververs dashboard, hoofdweergave en filters via de bestaande reparatiecallback.

**Acceptatiecriteria:**

- [ ] De actie is uitsluitend beschikbaar voor één geselecteerde rij onder het juiste signaal.
- [ ] Annuleren en fouten laten de rij intact; succes herevalueert alle signalen en vervolgt logisch de selectie.
- [ ] Bestaande herstel-, negeer-, navigatie- en splitteracties blijven ongewijzigd werken.

**Verificatie:** Eerst falende dashboard- en LibraryViewModel-tests, daarna gerichte regressietests.

**Omvang:** Middelgroot, maximaal 5 kernbestanden plus tests.

## Taak 4: Lokalisatie en afronding

**Beschrijving:** Voeg zes vertalingen, featurestatus, handmatige checklist en een actuele Debug-build toe.

**Acceptatiecriteria:**

- [ ] Basis/Engels, Nederlands, Duits, Frans, Spaans en Italiaans bevatten alle nieuwe sleutels.
- [ ] De checklist dekt succes, annuleren, verouderde gegevens, fouten, sidecar en toetsenbord.
- [ ] Alle Markdown is exact naar Obsidian gespiegeld.
- [ ] Volledige tests, Release-build, zelfreview en definitieve Debug-build zijn geslaagd.

**Verificatie:** Volledige DoD-controle en handmatige gebruikerschecklist.

**Omvang:** Middelgroot door resources en documentatie.

## Risico's en maatregelen

| Risico | Impact | Maatregel |
|---|---|---|
| Verouderde dashboardrij overschrijft actuele metadata | Hoog | Boek vlak vóór opslag opnieuw laden en signaal opnieuw evalueren. |
| De voor/na-betekenis is onduidelijk | Middel | Twee expliciet gelabelde blokken en één concrete bevestigingsactie. |
| Onbruikbare auteur wordt nieuwe titel | Hoog | Alleen precies één niet-lege, niet-`Unknown` auteur accepteren. |
| Bestaande Quality Page-acties regresseren | Middel | Context- en layouttests uitbreiden zonder bestaand gedrag te herschrijven. |
| Sidecar-update mislukt na databasesucces | Middel | Opnieuw geladen boek tonen en een duidelijke waarschuwing geven. |

## Niet in deze milestone

- Vrije invoer, bulkherstel, omslag- of tagherstel.
- Heuristiekwijzigingen, databaseschemawijzigingen of optimistic concurrency.

## Open vragen

Geen blokkerende vragen. Functionele richting goedgekeurd op 3 september 2026.
