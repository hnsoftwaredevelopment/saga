# Milestone 51 — meervoudige selectie in de boekenkast

## Status en doel

De gebruiker heeft het voorstel voor Ctrl-/Shift-selectie, duidelijke selectieaanduiding, bestaande metadata-bulkbewerking, toetsenbord en betrouwbaar gedrag bij groepen/filters/weergaven goedgekeurd en opdracht gegeven te bouwen. Deze specificatie legt de uitvoeringskeuzes binnen die scope vast; geen extra akkoordronde. Basis: gemergede PR #51/main 5360aed plus de documentatieafronding van PR #52. Branch codex/milestone-51-bookshelf-selection. Geen schemawijziging, dependency of nieuwe opslagroute.

Doel: dezelfde bruikbare meervoudige boekselectie als in de grids beschikbaar maken bij de omslagtegels. Eén centrale selectie van unieke boek-ID's, onafhankelijk van gerealiseerde WPF-tegels. Een gewoon gekozen boek blijft in het detailpaneel zichtbaar; metadata wijzigen gebruikt de bestaande bulkdialoog en resultaatverwerking.

## Gedrag en acceptatie

1. Gewone klik kiest één boek en vervangt de selectie. Ctrl+klik voegt toe of haalt weg. Shift+klik kiest de inclusieve reeks vanaf het anker; Ctrl+Shift voegt die reeks toe. Zonder geldig anker begint de reeks bij het gekozen boek. Selecteren opent geen boekbestand.
2. Een duidelijke geselecteerde rand en focusaanduiding werken in beide thema's en blijven correct na scrollen/virtualisatie. Selectie wordt op boek-ID bepaald, nooit op een toevallig zichtbare container. Een gelokaliseerde telling toont het aantal unieke geselecteerde boeken en de bestaande actie Metadata wijzigen is beschikbaar.
3. De gegroepeerde boekenkast ondersteunt dezelfde acties. Rangevolgorde is de getoonde volgorde van open groepen, met subgroepen vóór directe boeken zoals de huidige templates. Ingeklapte groepen worden niet nieuw door Shift meegenomen. Bestaande bewuste selecties blijven bij inklappen behouden en blijven meetellen. Een boek dat door tags/auteurs in meerdere groepen voorkomt telt één keer en heeft overal dezelfde geselecteerde aanduiding. Voor een reeks geldt de eerste zichtbare verschijning per ID.
4. Pijltjes/Home/End kiezen een andere tegel; Shift breidt uit. Ctrl+pijltje verplaatst alleen de focus; Ctrl+Spatie schakelt het gefocuste boek om. Tab kan de tegelweergave verlaten. Escape wist de selectie. Focus en scroll volgen de gekozen tegel, ook voorbij de huidige viewport; invoer in zoek-/detailvelden wordt niet onderschept. Bestaande groepuitklapbediening blijft werken.
5. Zoeken/filteren behoudt de doorsnede van selectie en de nieuwe zichtbare resultaten; verborgen boeken worden niet ongemerkt bewerkt of opnieuw geselecteerd bij wissen van de zoektekst. Sorteren/hergroeperen en weergavewissel behouden geselecteerde zichtbare ID's. Een bibliotheekwissel wist de selectie en het anker. De hoofdboekselectie voor details en de bulkselectie hebben elk hun bestaande betekenis.
6. De boekenkast sluit aan op de bestaande Metadata wijzigen-opdracht, zonder tweede bewerkingsdialoog of nieuwe opslaglogica. Annuleren behoudt de selectie; bestaande succes-/foutselectie na opslag blijft gelden en wordt ook op tegels zichtbaar. Alleen geselecteerde unieke boeken worden verwerkt.
7. Verversen van items en wisselen tussen grids en boekenkast mag geen lege/tijdelijke UI-selectie terugschrijven naar de centrale selectie. Bestaande gridselectie blijft werken. Opnieuw gemaakte rijen met dezelfde ID krijgen de actuele geselecteerde staat.
8. Alleen benodigde tegels worden gerealiseerd; selecteren of een reeks berekenen leest geen boekbestanden en laadt geen omslagen. Bestaande VirtualizingWrapPanel en AsyncCoverImage behouden hun werk. Geen volledige lijst met containers aanmaken om selectie te tekenen.

## Structuur en technische aanpak

Houd selectieregels in een gerichte Presentation-partial/klein selectiemodel met gewone IDs en unit-tests. Verplaats de bestaande SetSelectedBooks-implementatie uit het grote LibraryViewModel naar die gerichte partial. De gedeelde SelectedBooks-verzameling blijft de bron voor de bulkopdracht; een bindbare rijstatus is alleen de afgeleide visuele staat. Hergebruik GetOrCreateBookRow en de bestaande gegroepeerde rijen: dubbele groepsvermeldingen delen hetzelfde boek.

BookshelfView handelt modificatietoetsen, focus en lokale lijstcontainers af. Nested ListBox-selecties worden niet als afzonderlijke bibliotheekselecties behandeld. Bewaak programmagestuurde selectie en synchronisatie zodat refresh geen feedbacklus of geselecteerde-ID-verlies veroorzaakt. Sluit bestaande grids via een beperkte helper aan voor herstel bij weergavewissel. Bouw geen nieuw generiek UI-framework en breid het grote LibraryViewModel niet met nieuwe UI-details uit.

Stijl: bestaande CommunityToolkit.Mvvm ObservableObject/ObservableProperty, getypeerde records/IDs en xUnit/FluentAssertions. Voorbeeld van de bestaande semantiek: `SetSelectedBooks(VisibleBooks.Where(row => selectedIds.Contains(row.Id)));`. Alleen de huidige resultaat-ID's blijven geselecteerd; geen verborgen bibliotheekwijzigingen.

Nieuwe gebruikerstaal staat in AppResources in en/nl/de/fr/es/it. WPF-focustests gebruiken het bestaande losse Windows-testprogramma met fictieve gegevens; normale Saga-startup en echte bibliotheek worden niet gestart.

## Verificatie en opdrachten

- Gerichte unit-tests eerst falend: gewoon/Ctrl/Shift/Ctrl+Shift, ontbrekend anker, unieke IDs over groepen, filterdoorsnede, opnieuw gemaakte rijen, viewwissel en bulkgebruik.
- Echte WPF-controles: muis-/toetsenbordroutes, focus tijdens selectie/refresh, groepen, nieuwe containers bij scrollen en selectie bij heen/terug wisselen van view. Bestaande series-focuscontroles blijven behouden.
- Resource-/layouttests: telling, selectie/focusstijl, bestaande virtualisatie en zes talen.
- Volledig: `dotnet test EbookManager.sln -c Debug --no-restore`.
- WPF: `dotnet run --project tests/EbookManager.WpfSmokeTests/EbookManager.WpfSmokeTests.csproj -c Debug --no-restore` op een interactief Windows-bureaublad.
- Build: `dotnet build src/EbookManager.App/EbookManager.App.csproj -c Debug --no-restore`.
- Diff: `git diff --check`.

## Grenzen en oplevering

Altijd: unieke boek-ID's, bestaande bulkfeedback en opslag hergebruiken, eerst regressietests, toetsenbordfocus en virtualisatie bewaken, gebruikersvinkjes behouden, alle Markdown exact naar Obsidian spiegelen. Eerst afstemmen: nieuwe afhankelijkheid, schema, grotere wijziging aan opslag of nieuwe bewerkingsacties. Nooit: bij selecteren opslaan, onzichtbare filterresultaten meebewerken, voor selectie alle omslagen laden, gebruikersdata voor fouttests wijzigen of zelf een PR mergen.

Opleveren met kleine geteste commits, volledige suite, Debug-build zonder waarschuwingen/fouten, eigen vijfassige review en een nieuwe handmatige checklist. Normale niet-draft PR. Handmatige acceptatie blijft open tot de gebruiker de nieuwe build heeft getest.
