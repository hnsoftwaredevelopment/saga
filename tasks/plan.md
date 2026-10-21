# Milestone 42 implementatieplan: detailactie titel en auteur omwisselen verifiëren

## Overzicht

Milestone 42 bouwt geen tweede omwisselfunctie, maar rondt de bestaande detailactie aantoonbaar af. De kernroute bestaat al in `BookDetailsView` en `BookDetailsViewModel`. De uitvoering concentreert zich daarom op ontbrekend testbewijs voor opslaan, bibliotheekverversing, filters, toegankelijkheid en lokalisatie. Alleen een door een falende test aangetoond functioneel gat wordt met een minimale codewijziging hersteld.

## Architectuurbeslissingen

- `BookDetailsViewModel.SwapTitleAndAuthorsCommand` blijft de enige detailactie voor het staged omwisselen.
- `SaveCommand`, `BookService` en het bestaande `BookSaved`-event blijven de opslag- en verversingsroute.
- `UndoCommand` blijft de veilige terugweg vóór opslag; er komt geen extra bevestigingsvenster.
- De bestaande knoppositie, het pijlenpictogram en de gelokaliseerde tooltip blijven behouden wanneer de verificatie slaagt.
- Een titel die scheidingstekens bevat blijft na omwisselen één auteur; de bestaande `singleAuthorText`-route bewaakt dit.
- De Quality Page-heuristiek en algemene multibewerking blijven buiten scope.

## Afhankelijkheden

```text
Bestaand detailgedrag inventariseren
        │
        ├── Opslagroute en randgevallen bewijzen
        │          │
        │          └── Bibliotheekregel en auteursfilters bewijzen
        │
        └── XAML, pictogram, toegankelijkheid en lokalisatie bewijzen
                         │
                         └── Documentatie, acceptatiebuild en review
```

## Taak 1: staged omwisselen en opslaan volledig bewijzen

**Beschrijving:** Voeg eerst falende tests toe die de bestaande gebruikersroute als één geheel beschrijven: omwisselen, staged toestand, opslaan en behoud van overige metadata. Leg ook het randgeval vast waarin de voormalige titel een puntkomma bevat en toch één auteur moet blijven.

**Acceptatiecriteria:**
- [ ] De knop verwisselt de huidige titel en auteurs zonder onmiddellijk naar de repository te schrijven.
- [ ] `Ongedaan maken` herstelt de oorspronkelijke waarden vóór opslag.
- [ ] `Opslaan` schrijft de verwisselde waarden via de bestaande opslagroute en laat overige metadata intact.
- [ ] Een voormalige titel met een puntkomma wordt als precies één auteur opgeslagen.

**Verificatie:** Rode test vastleggen, daarna groene `BookDetailsViewModelTests`; alleen bij bewezen afwijking minimale productcode aanpassen.

**Afhankelijkheden:** Geen.

**Waarschijnlijk geraakte bestanden:**
- `tests/EbookManager.Tests/App/ViewModels/BookDetailsViewModelTests.cs`
- zo nodig `src/EbookManager.Presentation/ViewModels/BookDetailsViewModel.cs`

**Omvang:** Klein.

## Taak 2: bibliotheekverversing en filters bewijzen

**Beschrijving:** Bewijs op `LibraryViewModel`-niveau dat een vanuit details opgeslagen omwisseling de zichtbare rij, actieve zoekresultaten, selectie en auteursfilters bijwerkt zonder herstart.

**Acceptatiecriteria:**
- [ ] De zichtbare boekregel toont na opslaan de nieuwe titel en auteur.
- [ ] Het oude auteursfilter verdwijnt wanneer het niet meer gebruikt wordt en het nieuwe filter verschijnt met het juiste aantal.
- [ ] Een actief zoekfilter wordt opnieuw toegepast op de gewijzigde metadata.
- [ ] De detailselectie blijft op het opgeslagen boek staan wanneer het nog in de actieve weergave past.

**Verificatie:** Eerst falende en daarna groene gerichte `LibraryViewModelTests`.

**Afhankelijkheden:** Taak 1.

**Waarschijnlijk geraakte bestanden:**
- `tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs`
- zo nodig `src/EbookManager.Presentation/ViewModels/LibraryViewModel.cs`

**Omvang:** Klein tot middelgroot.

## Checkpoint na taken 1-2

- [ ] De volledige opslagroute is door waarneembare toestand bewezen.
- [ ] Geen tweede opslag- of verversingsroute is toegevoegd.
- [ ] Gerichte detail- en bibliotheektests zijn groen.

## Taak 3: knop, pictogram, toetsenbordtoegang en zes talen bewijzen

**Beschrijving:** Vul statische WPF-layout- en lokalisatietests aan voor de bestaande omwisselknop. De test controleert commandbinding, pictogram, tooltip en toegankelijke naam zonder de huidige schermindeling onnodig te wijzigen.

**Acceptatiecriteria:**
- [ ] De knop is aan `SwapTitleAndAuthorsCommand` gebonden en gebruikt het bestaande `SwapTitleAuthorIconGeometry`.
- [ ] De knop blijft een echte WPF-`Button` en is daardoor met Tab, Enter en Spatie bedienbaar.
- [ ] Tooltip en `AutomationProperties.Name` gebruiken dezelfde gelokaliseerde sleutel.
- [ ] De lokalisatiesleutel bevat een niet-lege, geldige tekst in Engels, Nederlands, Duits, Frans, Spaans en Italiaans.

**Verificatie:** Eerst falende en daarna groene `BookDetailsViewLayoutTests` en lokalisatietests.

**Afhankelijkheden:** Geen; uitvoering volgt na het eerste checkpoint om commits klein te houden.

**Waarschijnlijk geraakte bestanden:**
- `tests/EbookManager.Tests/App/Views/BookDetailsViewLayoutTests.cs`
- relevante lokalisatietest onder `tests/EbookManager.Tests/App/Resources`.
- alleen bij een bewezen fout `src/EbookManager.App/Views/BookDetailsView.xaml` of `src/EbookManager.App/Resources/Strings/AppResources*.resx`.

**Omvang:** Klein; eventuele resourcewijzigingen vormen één mechanische set.

## Taak 4: status en handmatige acceptatie voorbereiden

**Beschrijving:** Werk de actuele projectstatus bij, leg de bestaande detailactie vast en maak een compacte handmatige checklist voor herkenbaarheid, staged gedrag, opslaan, ongedaan maken en bibliotheekverversing.

**Acceptatiecriteria:**
- [ ] README vermeldt Milestone 41 als geaccepteerd en verwijst naar de actuele Milestone 42-checklist.
- [ ] De kwaliteitsdocumentatie noemt de geverifieerde detailactie zonder de heuristiek als nauwkeuriger voor te stellen.
- [ ] De checklist behandelt muis, toetsenbord, meerdere auteurs, puntkomma in titel, opslaan, ongedaan maken, filters en herstart.
- [ ] Alle gewijzigde Markdown is met gelijke hashes naar Obsidian gespiegeld.

**Verificatie:** `git diff --check`, linkcontrole door inspectie en hashvergelijking met de Obsidian-spiegel.

**Afhankelijkheden:** Taken 1-3.

**Waarschijnlijk geraakte bestanden:**
- `README.md`
- `docs/feature-requests/metadata-quality-dashboard.md`
- `docs/manual-tests/milestone-42-checklist.md`
- `tasks/plan.md`
- `tasks/todo.md`

**Omvang:** Middelgroot, hoofdzakelijk documentatie.

## Checkpoint na taken 3-4

- [ ] Alle gerichte tests zijn groen.
- [ ] De interface volgt de bestaande vormgeving en toegankelijkheidspatronen.
- [ ] De acceptatiechecklist en projectstatus zijn actueel en gespiegeld.

## Taak 5: Definition of Done en oplevering

**Beschrijving:** Voer de projectbrede kwaliteitsbar uit en lever een gewone, niet-draft PR op voor handmatige acceptatie.

**Acceptatiecriteria:**
- [ ] Volledige testset is groen zonder overgeslagen tests.
- [ ] Schone Debug-build slaagt zonder waarschuwingen of fouten en alleen `Builds/Debug` bevat de actuele build.
- [ ] De complete diff is beoordeeld op correctheid, eenvoud, architectuur, beveiliging en prestaties.
- [ ] De branch bevat uitsluitend Milestone 42 en de PR is geen draft.

**Verificatie:** Volledige tests, clean/build, `git diff --check`, eigen codereview, GitHub-controles en daarna de handmatige checklist.

**Afhankelijkheden:** Taken 1-4.

**Waarschijnlijk geraakte bestanden:** Geen nieuwe; verificatie en eventuele kleine correcties blijven binnen de voorgaande taakgrenzen.

**Omvang:** Klein.

## Risico's en maatregelen

| Risico | Impact | Maatregel |
|---|---|---|
| Bestaande functie dubbel implementeren | Middel | Eerst bestaande route testen; alleen aangetoonde gaten wijzigen |
| Een titel met puntkomma wordt meerdere auteurs | Hoog | Expliciete opslagtest voor één voormalige titel met scheidingsteken |
| Opslag ververst rij maar niet auteursfilters of zoeken | Hoog | Integratietest op `LibraryViewModel` met waarneembare filters en zichtbare rijen |
| Knop is visueel aanwezig maar onvoldoende toegankelijk | Middel | XAML-test voor echte `Button`, command, tooltip en automation name |
| Documentatie suggereert dat de heuristiek alle gevallen vindt | Laag | Detailactie beschrijven als handmatige aanvulling, niet als heuristiekverbetering |
| Bestaande gebruikersaanpassingen aan detailscherm raken verloren | Hoog | Layout niet herschrijven; huidige plaatsing en stijl behouden |

## Open vragen

Er zijn geen blokkerende open vragen. De goedgekeurde specificatie bepaalt dat de bestaande staged workflow, knoppositie en het pictogram behouden blijven.

## Goedkeuringscheckpoint

- [x] Specificatie en afbakening door gebruiker goedgekeurd.
- [x] Implementatieplan en taakvolgorde door gebruiker goedgekeurd.
- [ ] Implementatie testgestuurd en incrementeel afgerond.
- [ ] Definition of Done afgerond.
- [ ] Gewone, niet-draft PR geopend en handmatig geaccepteerd.
