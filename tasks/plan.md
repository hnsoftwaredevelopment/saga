# Milestone 36 Implementatieplan: Rommelige tags herstellen

## Overzicht

Milestone 36 voltooit de laatste directe herstelactie op de Quality Page. De gebruiker krijgt een controleerbaar voorstel voor één boek, kan de tags aanpassen en laat Saga daarna via de bestaande veilige opslagroute uitsluitend de tags wijzigen.

## Architectuurbeslissingen

- Eén canonieke normalisatiefunctie wordt gedeeld door voorstel, validatie en opslag.
- Het herstel blijft een afzonderlijke verticale route naast auteur-, taal-, serie- en titel/auteurherstel.
- Er komt geen databasewijziging en geen nieuwe dependency.
- Komma’s blijven onderdeel van de bestaande kwaliteitsdefinitie; legitieme uitzonderingen lopen via `Dit is correct`.

## Afhankelijkheden

```text
Tag-normalisatie
      │
      ├── veilige herstelservice
      │
      └── bewerkbaar herstelviewmodel
              │
              └── dashboardopdracht en WPF-venster
                          │
                          └── lokalisatie, praktijktest en PR
```

## Taak 1: Canonieke normalisatie en veilige opslag

**Beschrijving:** Voeg de pure tag-normalisatie en een herstelservice toe die het actuele boek opnieuw controleert en uitsluitend tags plus wijzigingsdatum bewaart.

**Acceptatiecriteria:**

- [x] Komma’s, regeleinden, lege waarden, witruimte en dubbelen worden deterministisch genormaliseerd.
- [x] De service weigert een ontbrekend, ongeldig of niet meer toepasselijk boek veilig.
- [x] Opslagresultaten onderscheiden succes, write-backwaarschuwing en mislukking.

**Verificatie:** De nieuwe tests faalden eerst op de ontbrekende normalisatie en service; daarna zijn 8 gerichte servicetests groen.

**Afhankelijkheden:** Geen.

**Waarschijnlijke bestanden:** applicatieservice, servicetests en eventueel een klein normalisatietype.

## Taak 2: Bewerkbaar herstelvoorstel

**Beschrijving:** Bouw het viewmodel dat huidige tags toont, het opgeschoonde voorstel als regels aanbiedt en alleen een betekenisvolle wijziging laat bevestigen.

**Acceptatiecriteria:**

- [x] Het initiële voorstel gebruikt exact dezelfde normalisatie als de service.
- [x] Handmatige regels worden opnieuw veilig genormaliseerd.
- [x] Een lege taglijst is geldig, terwijl een ongewijzigde uitkomst niet kan worden opgeslagen.

**Verificatie:** De nieuwe tests faalden eerst op het ontbrekende viewmodel; daarna zijn 4 viewmodeltests en alle 8 servicetests groen.

**Afhankelijkheden:** Taak 1.

**Waarschijnlijke bestanden:** herstelviewmodel en viewmodeltests.

## Checkpoint 1

- [x] Normalisatie, service en viewmodeltests zijn groen.
- [x] Alleen tags en `UpdatedUtc` kunnen door de nieuwe route wijzigen.
- [x] Tussentijdse diff is beoordeeld op eenvoud, foutpaden en toekomstige bulkcompatibiliteit.

## Taak 3: Quality Page-coördinatie

**Beschrijving:** Voeg de specifieke dashboardopdracht toe, open het herstelvoorstel voor de geselecteerde rommelige-tagregel en herbeoordeel het boek na opslag.

**Acceptatiecriteria:**

- [x] De opdracht is alleen actief bij `messy-tags` en een geldige geselecteerde rij.
- [x] Annuleren roept de service niet aan.
- [x] Succes, gedeeltelijke write-back, niet toepasselijk, verdwenen boek en fout worden correct verwerkt.

**Verificatie:** De nieuwe tests faalden eerst op de ontbrekende dashboardopdracht; daarna zijn 7 taghersteltests en alle 62 dashboardtests groen.

**Afhankelijkheden:** Taken 1 en 2.

**Waarschijnlijke bestanden:** dashboardviewmodel en afzonderlijke dashboardtests.

## Taak 4: Toegankelijk WPF-herstelvenster

**Beschrijving:** Voeg een resizable modaal venster toe met huidige tags, bewerkbare nieuwe tags, duidelijke hulptekst en precieze actieknoppen.

**Acceptatiecriteria:**

- [x] Het veld `Nieuwe tags` krijgt focus en ondersteunt toetsenbordbewerking met één tag per regel.
- [x] `Tags wijzigen` volgt `CanSave`; annuleren blijft veilig.
- [x] Labels, automation names, contrast en bestaande themaresources worden gebruikt.

**Verificatie:** De 2 XAML-layouttests zijn groen en de WPF-app bouwt zonder waarschuwingen of fouten.

**Afhankelijkheden:** Taak 2.

**Waarschijnlijke bestanden:** venster-XAML, code-behind en layouttest.

## Taak 5: Compositie en lokalisatie

**Beschrijving:** Verbind service, interactie, dashboard en applicatiecompositie en voeg alle zichtbare teksten aan de bestaande talen toe.

**Acceptatiecriteria:**

- [x] De echte applicatie opent het nieuwe venster vanuit de Quality Page.
- [x] De herstelde boekgegevens verschijnen direct in Quality Page en bibliotheek.
- [x] Alle ondersteunde resx-bestanden bevatten dezelfde nieuwe sleutels.

**Verificatie:** Compositie, 6 taalbestanden, 21 gerichte tagtests en een schone Debug-build zijn gecontroleerd.

**Afhankelijkheden:** Taken 3 en 4.

**Waarschijnlijke bestanden:** interactie-interface en -service, app-compositie, bibliotheekviewmodel en resourcebestanden.

## Checkpoint 2

- [x] Gerichte tests, volledige testsuite en opmaakcontrole zijn groen.
- [x] Debug-build bevat nul waarschuwingen en nul fouten.
- [x] Zelfreview op correctheid, eenvoud, architectuur, beveiliging, toegankelijkheid en prestaties is afgerond.

## Taak 6: Praktijkacceptatie en oplevering

**Beschrijving:** Lever een korte handmatige checklist en actuele Debug-build, spiegel documentatie naar Obsidian en open een normale PR.

**Acceptatiecriteria:**

- [ ] Voorstel, handmatige bewerking, lege uitkomst, annuleren en opslaan zijn gecontroleerd.
- [ ] De Quality Page en bibliotheek verversen zonder herstart.
- [ ] Definition of Done, documentatiespiegel en PR-controles zijn afgerond.

**Verificatie:** Handmatige checklist met herkenbare rommelige tags in een echte bibliotheek.

**Afhankelijkheden:** Checkpoint 2.

## Risico’s en beheersing

| Risico | Impact | Beheersing |
|---|---|---|
| Een komma hoort echt bij één tag | Onbedoelde splitsing | Toon voorvertoning en behoud `Dit is correct` als expliciete uitzondering |
| Voorstel en opslag normaliseren anders | Verrassende opgeslagen waarden | Gebruik één canonieke normalisatiefunctie |
| Opslag wijzigt andere metadata | Gegevensverlies | Kopieer alle overige velden ongewijzigd en test dit expliciet |
| Rij verdwijnt ondanks gedeeltelijke mislukking | Misleidende status | Lees database opnieuw en baseer dashboard op de opgeslagen werkelijkheid |
| Lege invoer wordt onbedoeld geblokkeerd | Gebruiker kan lege rommel niet verwijderen | Sta een lege eindlijst toe wanneer die verschilt van de huidige tags |

## Open vragen

Geen blokkerende vragen; implementatie start na bevestiging van deze specificatie en volgorde.

## Herstelspoor: OneDrive-omslagen hydrateren

Na de opslagmigratie rapporteert Windows 32.344 van de 32.644 omslagen als `Pinned + Offline`. Met akkoord van de gebruiker wordt een eenmalige, hervatbare onderhoudsactie toegevoegd en uitgevoerd die uitsluitend `cover.jpg` hydrateert.

### Taak H1: Veilige onderhoudsactie

- [x] Alleen gevalideerde Saga-bibliotheken en exacte `cover.jpg`-bestanden verwerken.
- [x] Lokale bestanden overslaan, offline bestanden sequentieel lezen en fouten isoleren.
- [x] Scanmodus, voortgang, logbestand en natuurlijke hervatting ondersteunen.

### Taak H2: Gecontroleerde uitvoering

- [ ] Tests en scan op de echte bibliotheek uitvoeren.
- [ ] Hydratatie starten en voortgang bewaken.
- [ ] Na voltooiing offline-aantal en Saga-performance opnieuw controleren.
