# Milestone 52 — tags beheren

## Status en doel

Voorstel ter beoordeling. De gebruiker heeft gevraagd de volgende slice op te pakken na de geaccepteerde en gemergede Milestone 51 / PR #53. Tags beheren is nog niet inhoudelijk goedgekeurd; deze voorbereiding verandert geen productcode of bibliotheekgegevens. Basis main e8e024c, branch codex/milestone-52-tag-management.

Doel: één herkenbaar beheervenster zoals Auteurs beheren en Series beheren, waarin de gebruiker tags doorzoekt, gekoppelde boeken bekijkt en een tag expliciet hernoemt voor al haar boeken. Voorbeeld: Sciencefiction wijzigen naar Science fiction zonder andere tags op die boeken te verliezen.

Uitgangspunten ter bevestiging: bibliotheekbreed hernoemen, één tag per actie, bestaande doelnaam blokkeren zonder samenvoegen, overige tags/volgorde behouden. Verwijderen, samenvoegen en splitsen zijn afzonderlijke vervolgstappen.

## Gebruikersgedrag en acceptatie

1. Tags beheren opent vanuit de hoofdwerkbalk in de actieve bibliotheek. Het venster volgt de bestaande beheervensters, met herkenbaar tagicoon, zoekveld, aantal boeken per tag, boekenlijst en zichtbare splitter.
2. Zoeken filtert de taglijst. Tags en boeken laden in pagina's van maximaal 200; late zoekresultaten vervangen geen nieuwere zoekopdracht. De lijst toont alleen gebruikte tags. Selecteren/zoeken laadt geen omslagen of volledige ebookbestanden.
3. Een gekozen tag toont haar gekoppelde boeken met titel en auteur(s), gesorteerd op titel met stabiele ID-volgorde bij gelijke titels. Meer laden voegt geen dubbele boeken toe. Nul zoekresultaten en leesfouten geven een begrijpelijke melding.
4. Hernoemen toont de huidige naam, een bewerkbare nieuwe naam en vóór toepassen een bevestiging met oude naam, nieuwe naam en aantal betrokken boeken. Annuleren verandert niets en behoudt de selectie. Een lege/onbruikbare naam wordt geweigerd. Een puntkomma wordt geweigerd omdat die in de huidige boekeditors meerdere tags scheidt.
5. Naamvergelijking volgt de bestaande tagopslag: trimmen en dezelfde normalisatie als EfBookRepository, geen nieuwe gelijkheidsregels. Alleen hoofdlettergebruik corrigeren mag voor dezelfde tag. Een andere bestaande doelnaam, ook een nog aanwezige ongebruikte tag, wordt geblokkeerd; geen stil samenvoegen of vervangen.
6. Dezelfde tagidentiteit blijft bestaan. Boek-tagkoppelingen en hun Order veranderen niet; alle overige metadata blijft behouden, behalve UpdatedUtc van daadwerkelijk gewijzigde boeken. Ongewijzigde invoer geeft geen schijnwijziging of bestandsschrijfactie.
7. De databasewijziging gebeurt in één transactie. Controleer vóór commit opnieuw de bronidentiteit/naam en de volledige bevestigde set gekoppelde boek-IDs. Gewijzigde koppelingen sinds de bevestiging geven een herstelbaar conflict en schrijven niets. Een bezette doelnaam of mislukte transactie laat de oorspronkelijke toestand intact.
8. Na commit worden actuele opgeslagen metadata van alle betrokken boeken via de bestaande BookService.SynchronizeStoredMetadataAsync-route gesynchroniseerd. Metadata.json en ondersteunde ebook-write-back behouden hun bestaande gedrag. Bestandswaarschuwingen betekenen dat de bibliotheekwijziging wel is opgeslagen; toon de betrokken boeken afzonderlijk, zonder automatische herhaling van de hernoemopdracht.
9. Na succes blijft de nieuwe tag geselecteerd en zo nodig vindbaar onder de nieuwe zoeknaam. Een korte groene resultaatmelding volstaat bij volledig succes. Fouten en bestandswaarschuwingen krijgen de bestaande herkenbare samenvatting met begrensde details. Na sluiten worden hoofdscherm, tagfilter, groepering, details en eventuele bulkselectie gereconcilieerd met behoud van geladen omslagen.
10. Typen, Tab, pijltjes, Enter en Escape werken zoals bij de andere beheervensters. Laden/hernoemen steelt geen nieuwere focuskeuze; het invoervenster behoudt focus. Knoppen blijven bij verkleinen bereikbaar. Tijdens opslaan kan geen tweede hernoemopdracht of bibliotheekwissel worden uitgevoerd.

## Structuur en opslagkeuzes

.NET 10, WPF, CommunityToolkit.Mvvm en EF Core/SQLite blijven leidend. De bestaande Tags- en BookTags-tabellen hebben al stabiele IDs, een unieke NormalizedName en geordende koppelingen. Geen schemawijziging, migratie, dependency, boekmapverplaatsing of nieuwe ebookwriter nodig.

Gerichte contracten onder src/EbookManager.Domain/Tags en Abstractions; EF-implementatie onder Infrastructure/Persistence/Repositories. Een Application/Tags-service orkestreert databasecommit en bestaande bestandssynchronisatie. Presentation/ViewModels bevat het testbare beheerviewmodel; App/Views de WPF-vensters en bestaande interacties/registratie. Gebruik SeriesManagement als patroon, zonder het hele venster of repository tot een nieuw generiek beheerframework te verbouwen.

Behoud de bestaande codeconventies: getypeerde resultaten, async CancellationToken, ObservableObject en expliciete opdrachten. Voorbeeld uit het bestaande pad: `await books.SynchronizeStoredMetadataAsync(book.Id, CancellationToken.None);` na een geslaagde commit. Een geannuleerde UI-opdracht mag een begonnen commit-/synchronisatiefase niet als volledig teruggedraaid presenteren. Werk alleen bevestigde bronboeken bij; geen per-boek databasehernoemlus of herimport.

## Verificatie

- Eerst falende SQLite-tests: zoeken/aantallen/paginering, normalisatie, hernoemen over meer dan 200 boeken, schrijfwijzecorrectie, bestaande doelnaam, leeg/ongeldig/ongewijzigd, bronwijziging en rollback. Controleer behoud van TagId, overige tags en volgorde en dat niet-betrokken boeken niet veranderen.
- Application-tests met tijdelijke bestanden: commit vóór synchronisatie, actuele opgeslagen metadata, afzonderlijke bestandswaarschuwingen en annulering vóór/tijdens de juiste fase. Nooit foutscenario's op de echte bibliotheek forceren.
- Viewmodeltests: paginering, stale zoek-/boekresultaten, selectiebehoud, bevestiging/annuleren, nieuwe naam zichtbaar, bezetstatus, duidelijke feedback en begrensde hoofdschermverversing.
- WPF-proeven met fictieve gegevens: zoek-/lijstfocus, opeenvolgende selectie, hernoemdialoog en nieuwere focuskeuze; layout en bereikbare knoppen. Bestaande vijftien scenario's blijven slagen.
- Alle nieuwe teksten in en/nl/de/fr/es/it; resourcecontroles en een nieuwe handmatige Milestone 52-checklist vóór oplevering.
- `dotnet test EbookManager.sln -c Debug --no-restore`
- `dotnet run --project tests/EbookManager.WpfSmokeTests/EbookManager.WpfSmokeTests.csproj -c Debug --no-restore`
- `dotnet build src/EbookManager.App/EbookManager.App.csproj -c Debug --no-restore`
- `git diff --check`

## Grenzen en oplevering

Altijd: eerst regressietests, transacties en invoer valideren, alleen actieve bibliotheek, paginering en behoud van omslagen, bestaande opslagroute, eigen vijfassige review en alle Markdown exact naar Obsidian spiegelen. Afstemmen bij nieuwe dependencies/schema of uitbreiding naar verwijderen/samenvoegen/splitsen. Nooit: automatisch tags samenvoegen, ongemerkt boekmetadata wijzigen tijdens bladeren, verlies van overige tags, geheimen committen of zelf de PR mergen.

Na scopeakkoord volgt de uitvoering in kleine geteste increments: opslagcontract/lezen → transactioneel hernoemen → bestandssynchronisatie → viewmodel → venster/werkbalk/talen → integrale verificatie. Per increment circa 3–5 bestanden en een aantoonbaar testcheckpoint; ondersteunende venster-/resourceaansluiting zo nodig splitsen. Geen agents nodig. Gewone niet-draft PR na implementatie; een schone enige Debug-build met versie/tijd en checklist voor gebruikersacceptatie. CodeRabbit-limieten blokkeren niet wanneer eigen review, regressietests en overige controles aantoonbaar voldoen.
