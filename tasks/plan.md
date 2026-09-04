# Milestone 33 Implementatieplan: Ontbrekende omslag zoeken

## Overzicht

Milestone 33 voegt één volledige herstelroute voor `Geen omslag` toe. Na de Nederlandse praktijktest wordt deze route uitgebreid met de Google Books-compatibiliteitsfeed, een lokaal gegenereerde noodomslag en een blijvende actie in het detailscherm om ook bestaande omslagen te vervangen.

## Afhankelijkheden

```text
Zoekcontract en Open Library-client
        │
        ├── veilig beheerd coverbestand
        │           │
        └── herstelservice
                    │
                    ├── keuzemodel en WPF-venster
                    │           │
                    └── Quality Page en bibliotheekverversing
                                │
                                └── lokalisatie, checklist en Debug-build
```

## Taak 1: Open Library-zoekroute

**Beschrijving:** Definieer het brononafhankelijke zoekcontract en implementeer een begrensde Open Library-client met de ingebouwde HTTP- en JSON-functionaliteit.

**Acceptatiecriteria:**

- [x] Titel en auteurs vormen een correct geëncodeerde zoekvraag; een beschikbaar ISBN voegt een exacte zoekroute toe en beide resultaatsets worden samengevoegd.
- [x] Alleen unieke numerieke Cover ID's worden geaccepteerd en maximaal twaalf kandidaten teruggegeven.
- [x] Time-outs, annulering, ongeldige JSON, te grote antwoorden en serverfouten leveren een gecontroleerd resultaat.

**Verificatie:** Eerst falende tests met een gecontroleerde HTTP-handler; daarna alle zoekclienttests groen.

**Afhankelijkheden:** Geen.

**Waarschijnlijk geraakte bestanden:** Zoekcontract en modellen, Open Library-client, registratie en gerichte tests.

**Omvang:** Middelgroot, opgesplitst in kleine productie- en testbestanden.

## Taak 2: Veilige download en beeldvalidatie

**Beschrijving:** Download miniaturen en de definitieve grote JPEG via een vaste Open Library-host en controleer type, grootte en afmetingen voordat bytes de applicatielaag bereiken.

**Acceptatiecriteria:**

- [x] Alleen providergegenereerde Cover ID's kunnen een download starten; willekeurige URL's zijn onmogelijk.
- [x] Lege, niet-JPEG, te kleine, buitenproportionele of grotere dan 10 MiB afbeeldingen worden geweigerd.
- [x] Kandidaten bevatten betrouwbare breedte en hoogte en kunnen op oppervlak worden gesorteerd.

**Verificatie:** Gerichte tests voor geldige JPEG, afgekapt bestand, foutief type, grenswaarden en annulering.

**Afhankelijkheden:** Taak 1.

**Waarschijnlijk geraakte bestanden:** Open Library-client, kleine JPEG-inspecteur en tests.

**Omvang:** Klein tot middelgroot, maximaal 4 bestanden.

## Checkpoint 1

- [x] Alle externe invoer is begrensd en getest zonder werkelijk internetverkeer.
- [x] De applicatielaag kent Open Library niet bij naam.
- [x] De solution bouwt zonder waarschuwingen.

## Taak 3: Beheerde omslag veilig opslaan

**Beschrijving:** Voeg een apart opslagcontract en een herstelservice toe die het actuele boek opnieuw valideert, `cover.jpg` atomair schrijft en na boekopslag de werkelijke database-uitkomst controleert.

**Acceptatiecriteria:**

- [x] Alleen `books/<boek-id>/cover.jpg` binnen de actieve bibliotheek kan worden geschreven.
- [x] Alleen coverbytes, relatief coverpad en wijzigingstijd veranderen; alle overige boekgegevens blijven gelijk.
- [x] Niet gevonden, niet meer van toepassing, opslagfout en write-backwaarschuwing zijn expliciete uitkomsten; het bestand wordt alleen opgeruimd wanneer herladen bewijst dat de database niet is bijgewerkt.

**Verificatie:** Eerst falende pad-, bestands- en servicetests; daarna gerichte groene tests.

**Afhankelijkheden:** Taak 2.

**Waarschijnlijk geraakte bestanden:** Omslagopslagcontract, beheerde implementatie, huidige-bibliotheekadapter, herstelservice en tests.

**Omvang:** Middelgroot; productiecode en tests worden in afzonderlijke bestanden gehouden.

## Taak 4: Keuzevenster

**Beschrijving:** Bouw een modaal WPF-venster dat tijdens het zoeken voortgang toont en daarna kandidaten met miniatuur, bron en resolutie laat kiezen.

**Acceptatiecriteria:**

- [x] De gebruiker ziet een duidelijke laad-, leeg-, fout- en resultaatstatus.
- [x] `Deze omslag gebruiken` is alleen actief bij een geldige selectie; Enter, dubbelklik, Escape en annuleren werken.
- [x] Sluiten annuleert actief netwerkwerk en alle teksten komen uit resources.

**Verificatie:** Viewmodeltests, layouttests en een WPF-build.

**Afhankelijkheden:** Taken 1 en 2.

**Waarschijnlijk geraakte bestanden:** Keuzeviewmodel, kandidaatviewmodel, venster, interactiecontract en tests.

**Omvang:** Middelgroot, per laag opgesplitst.

## Checkpoint 2

- [x] Zoeken, selecteren en annuleren werken end-to-end met testdubbels.
- [x] Een mislukte zoekactie kan nooit een boek of bestand wijzigen.
- [x] Toetsenbord- en toegankelijkheidstests zijn groen.

## Taak 5: Quality Page en bibliotheek koppelen

**Beschrijving:** Voeg de contextgebonden actie toe, voer na keuze de herstelservice uit en ververs dashboard, hoofdgrid, boekenplank en detailpaneel via de bestaande reparatiecallback.

**Acceptatiecriteria:**

- [x] De actie is uitsluitend beschikbaar voor één geselecteerde rij onder `Geen omslag`.
- [x] Succes herevalueert alle signalen, werkt tellingen bij en verwijdert de herstelde rij.
- [x] Annuleren, geen resultaten en alle foutstatussen laten rij en metadata intact.

**Verificatie:** Eerst falende dashboard- en LibraryViewModel-tests; daarna gerichte regressietests.

**Afhankelijkheden:** Taken 3 en 4.

**Waarschijnlijk geraakte bestanden:** Dashboardviewmodel, LibraryViewModel-koppeling, interactieservice, dashboard-XAML en tests.

**Omvang:** Middelgroot, maximaal 5 kernbestanden plus gerichte tests.

## Taak 6: Lokalisatie en afronding

**Beschrijving:** Voeg zes vertalingen, featurestatus, handmatige checklist en de definitieve Debug-build toe.

**Acceptatiecriteria:**

- [x] Basis/Engels, Nederlands, Duits, Frans, Spaans en Italiaans bevatten alle nieuwe zichtbare en toegankelijke teksten.
- [x] De checklist dekt zoeken met en zonder ISBN, keuze, annuleren, lege resultaten, echte netwerkfout en directe verversing.
- [x] Alle Markdown is exact naar Obsidian gespiegeld.
- [x] Volledige tests, Debug-build en zelfreview zijn geslaagd; handmatige acceptatie staat nog open.

**Verificatie:** Volledige Definition of Done en daarna handmatige gebruikerscontrole.

**Afhankelijkheden:** Taak 5.

**Waarschijnlijk geraakte bestanden:** Resources, resource- en layouttests, README, featuredocument en checklist.

**Omvang:** Middelgroot door zes resourcebestanden en documentatie.

## Risico's en maatregelen

| Risico | Impact | Maatregel |
|---|---|---|
| Externe bron is traag of niet beschikbaar | Middel | Annuleerbare korte time-outs, duidelijke foutstatus en geen metadatawijziging. |
| Kwaadaardig of zeer groot antwoord | Hoog | Vaste hosts, numerieke ID's, streaminglimieten en strikte JPEG-validatie. |
| Verkeerde editie wordt gekozen | Hoog | Nooit automatisch kiezen; titel/auteur/context, bron en resolutie tonen. |
| Coverbestand en database raken uit sync | Hoog | Tijdelijk bestand, atomair vervangen, database opnieuw lezen en alleen opruimen wanneer de cover niet is opgeslagen. |
| Open Library beperkt verzoeken | Middel | Alleen expliciet zoeken, dedupliceren, maximaal twaalf kandidaten en geen crawling. |
| Groot venster of veel beelden belast geheugen | Middel | Resultaatlimiet, begrensde bytes en alleen noodzakelijke afbeeldingsgrootten. |
| GPL-code komt onbedoeld in Saga terecht | Hoog | Alleen gedrag bestuderen en zelfstandig C# ontwerpen; geen code kopiëren. |

## Niet in deze milestone

- Rommelige tags herstellen.
- Een apart instellingen-tabblad `Kwaliteit`.
- Google Books, Google Afbeeldingen of andere providers.
- Lokale bestandskeuze, bestaande omslagen vervangen of bulkherstel.
- Native cover-write-back in ebookbestanden.

## Open vragen

Geen blokkerende vragen. Functionele richting, aannames en dit uitvoeringsplan zijn goedgekeurd op 4 september 2026.

## Uitbreiding na Nederlandse praktijktest

### Taak 7: Meerdere omslagbronnen

- [ ] Maak kandidaat-ID's brongebonden en ondoorzichtig en routeer downloads alleen naar geregistreerde bronnen.
- [ ] Voeg de sleutelvrije Google Books-feed als begrensde, fouttolerante compatibiliteitsbron toe.
- [ ] Voeg Open Library- en Google-resultaten eerlijk samen tot maximaal twaalf kandidaten.
- [ ] Toon alleen wanneer beide online bronnen leeg zijn één lokaal door Saga gegenereerde omslag met titel en auteur.

### Taak 8: Omslag wijzigen in details

- [ ] Toon voor ieder geladen boek een duidelijke actie `Omslag wijzigen`, ook als al een omslag bestaat.
- [ ] Zoek met de actuele waarden uit het detailscherm en neem de gekozen omslag zichtbaar maar nog niet definitief over.
- [ ] Laat het bestaande `Opslaan` omslag en metadata veilig samen bewaren; `Ongedaan maken` herstelt de oorspronkelijke omslag.
- [ ] Houd de bestaande directe herstelroute op de Quality Page ongewijzigd.

### Taak 9: Afronding uitbreiding

- [ ] Lever alle nieuwe teksten in zes talen en breid de handmatige checklist uit.
- [ ] Voer gerichte tests, volledige tests, Debug-build en zelfreview uit.
- [ ] Maak uitsluitend `Builds/Debug` opnieuw en werk PR #34 bij.

Deze uitbreiding is functioneel goedgekeurd op 4 september 2026.
