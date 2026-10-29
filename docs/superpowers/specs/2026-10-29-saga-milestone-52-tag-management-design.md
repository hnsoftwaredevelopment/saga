# Milestone 52 — tags beheren

## Status en doel

De gebruiker heeft op 29 oktober 2026 de concrete afbakening goedgekeurd en opdracht tot uitvoering gegeven. Basis is de geaccepteerde en gemergede Milestone 51 / PR #53, main e8e024c, branch codex/milestone-52-tag-management. De aanvullende wens voor auteursvarianten is afzonderlijk voor later vastgelegd; geen onderdeel van deze slice.

Doel: één herkenbaar beheervenster zoals Auteurs beheren en Series beheren, waarin de gebruiker tags doorzoekt, gekoppelde boeken bekijkt en een tag expliciet hernoemt voor al haar boeken. Voorbeeld: Sciencefiction wijzigen naar Science fiction zonder andere tags op die boeken te verliezen.

Bevestigde uitgangspunten: bibliotheekbreed hernoemen, één tag per actie, bestaande doelnaam blokkeren zonder samenvoegen, overige tags/volgorde behouden. Verwijderen, samenvoegen en splitsen zijn afzonderlijke vervolgstappen.

## Gebruikersgedrag en acceptatie

### Uitbreiding na de gebruikerstest

De gebruiker meldt op 29 oktober dat een selectie van één boek geen zichtbaar verplaatsingsresultaat gaf, wil de gekozen tag van geselecteerde boeken kunnen verwijderen en wil bij vervangen door een reeds aanwezige tag geen blokkade of dubbele tag. Verduidelijkt: dit gebeurde met Tag hernoemen in Tags beheren. De huidige actie is bibliotheekbreed en gebruikt de boekselectie niet; die onduidelijke actiereikwijdte wordt hersteld. Deze nieuwe opdracht breidt de eerdere afbakening uit voor **geselecteerde boeken**, zonder globaal tagrecords samen te voegen.

- Benoem de bestaande bibliotheekbrede actie expliciet als Tag overal hernoemen. Zij behoudt de bestaande transactie en bevestiging voor alle gekoppelde boeken, ook buiten geladen pagina's. Een andere bestaande globale doelnaam blijft voor deze actie geblokkeerd; de melding verwijst naar Tag wijzigen voor geselecteerde boeken.
- De boekenlijst ondersteunt expliciete meervoudige selectie met een zichtbare telling. Tag wijzigen en Tag verwijderen zijn alleen voor geselecteerde boeken beschikbaar. Geen selectie betekent niet automatisch alle boeken. Ctrl+A betreft de geladen rijen; de telling toont de feitelijke selectie. Meer laden bewaart de selectie, een andere brontag kiezen wist haar.
- Tag wijzigen bevestigt brontag, doelnaam en geselecteerd aantal. Gebruik een nieuwe of bestaande doeltag. Per geselecteerd boek vervangt de nieuwe tag de bronpositie als zij nog ontbreekt. Staat de doeltag al op dat boek, verwijder dan uitsluitend de brontag en behoud de bestaande doeltagpositie; geen dubbele koppeling. Alle andere tags houden hun relatieve volgorde. Niet-geselecteerde boeken blijven ongewijzigd.
- Tag verwijderen vraagt expliciete bevestiging en verwijdert alleen de brontagkoppeling van geselecteerde boeken. Het boek zelf en overige tags blijven behouden, ook als dit de laatste tag was. Ongebruikte tagrecords hoeven niet globaal te worden verwijderd; de index toont alleen gebruikte tags.
- Na toepassen worden de bronboeklijst, bron-/doelaantallen en de tagindex uit actuele opslag herladen, met behoud van zoektekst en eerdere laadomvang. Gewijzigde boeken verdwijnen direct uit de bronlijst. Heeft de bron geen boeken meer, wis de bronselectie en de boekenlijst. De doeltag is via haar index/zoeknaam direct te controleren. Annuleren behoudt selectie en gegevens; na toepassen is de verwerkte boekselectie leeg.
- Afzonderlijk ID-gebaseerd assignment-contract en transactionele opslag: bronnaam en exacte geselecteerde lidmaatschappen opnieuw controleren, bestaande doelidentiteit hergebruiken, bronkoppelingen verwijderen vóór eventuele vervangingskoppelingen met dezelfde Order worden toegevoegd. De unieke BookId/Order- en BookId/TagId-indexen mogen niet tijdelijk conflicteren. Tagbeheer zelf blijft zonder schemawijziging.
- Bestaande bestandssynchronisatie na commit, fout-/waarschuwingsemantiek en hoofdschermverversing blijven leidend. Nieuwe regressies bewaken deelkeuze, bestaande doeltag op hetzelfde boek, de laatste tag verwijderen, onverwerkte boeken, rollback, actuele bron-/doellijsten, selectiebehoud bij Meer laden en focus na bevestiging/annuleren.
- De gebruiker heeft de overige oorspronkelijke controles positief getest; eigen vinkjes en opmerkingen blijven behouden. Een bestandswaarschuwing kon niet handmatig worden uitgelokt en blijft automatisch afgedekt. Nieuwe controles blijven open tot de aangepaste build is getest.

De oorspronkelijke onderstaande criteria beschrijven de bibliotheekbrede hernoemactie en blijven daarvoor gelden. De nieuwe selectieacties hebben de hierboven expliciet begrensde reikwijdte.

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

Altijd: eerst regressietests, transacties en invoer valideren, alleen actieve bibliotheek, paginering en behoud van omslagen, bestaande opslagroute, eigen vijfassige review en alle Markdown exact naar Obsidian spiegelen. Afstemmen bij nieuwe dependencies/schema of globaal tagrecords verwijderen/samenvoegen/splitsen. De expliciet gevraagde wijzigingen aan geselecteerde boek-tagkoppelingen zijn binnen deze slice uitgevoerd. Nooit: automatisch tags samenvoegen, ongemerkt boekmetadata wijzigen tijdens bladeren, verlies van overige tags, geheimen committen of zelf de PR mergen.

Na scopeakkoord volgt de uitvoering in kleine geteste increments: opslagcontract/lezen → transactioneel hernoemen → bestandssynchronisatie → viewmodel → venster/werkbalk/talen → integrale verificatie. Per increment circa 3–5 bestanden en een aantoonbaar testcheckpoint; ondersteunende venster-/resourceaansluiting zo nodig splitsen. Geen agents nodig. Gewone niet-draft PR na implementatie; een schone enige Debug-build met versie/tijd en checklist voor gebruikersacceptatie. CodeRabbit-limieten blokkeren niet wanneer eigen review, regressietests en overige controles aantoonbaar voldoen.

## Uitvoering en verificatie

Gebouwd in afzonderlijke geteste commits. Gerichte ITagRepository/Tags-contracten gebruiken stabiele IDs. EfTagRepository zoekt en pagineert met lichte SQL-projecties, controleert de genormaliseerde doelnaam en de bevestigde bronnaam/volledige boek-ID-set, en past Tag.Name/NormalizedName plus betrokken UpdatedUtc-waarden in één transactie aan. BookTags en hun Order blijven ongewijzigd. De bestaande namenregel Trim().ToLowerInvariant() is behouden, zonder nieuwe fuzzy matching.

Eerst falende tests bewijzen dat lezen/hernoemen ontbreekt en dat alleen een databasewijziging niet genoeg is: de bestaande bestandsroute moet na commit werkelijk worden uitgevoerd en afzonderlijke waarschuwingen tonen. Bij annulering na commit wordt synchronisatie voltooid. Tests met 510 boeken bewaken rollback en paginagrenzen; Unicode en tagindexpaginering zijn afzonderlijk gecontroleerd. De hoofdschermverversing gebruikt de bestaande begrensde metadata-snapshotroute met behoud van omslagen.

TagManagementViewModel en de WPF-vensters volgen het bestaande SeriesManagement-patroon, zonder generiek beheerframework. Na hernoemen blijft de nieuwe naam zichtbaar en worden eerder geladen boekpagina's behouden. Zes talen, toegankelijke namen, zichtbare splitter en begrensde lijsten zijn aangesloten. Invoeren van één tag zonder puntkomma/regeleinde wordt in het invoervenster toegelicht. Echte WPF-proeven bewaken zoek-/lijstfocus, een fout, nieuwere focuskeuzes en annuleren/opslaan vanuit de dialoog; minimaal vensterformaat in beide thema's is visueel bekeken met fictieve gegevens.

Eigen review op correctheid, eenvoud, architectuur, veiligheid en prestaties leidde tot twee extra eerst falende regressies: bronnaam gewijzigd vóór preview mag niet tot een stille mislukking bij opslaan leiden; een indexverversingsfout ná commit moet expliciet opgeslagen-maar-niet-ververst melden. Beide zijn hersteld. Geen externe API, geheimen, nieuwe dependency, schemawijziging of nieuwe ebookwriter.

Actuele verificatie na de selectie-uitbreiding: 1047 tests geslaagd, 0 mislukt/overgeslagen; negentien interactieve WPF-scenario's groen. Debug-build 2026.10.29.40 van 29 oktober 2026 om 14:20:14 (Europe/Amsterdam), 0 waarschuwingen/fouten, precies één Saga.exe onder Builds/Debug. [Handmatige acceptatie](../../manual-tests/milestone-52-checklist.md) staat open. De auteursvariantenwens is uitsluitend als afzonderlijke latere feature vastgelegd. Alle gewijzigde Markdown wordt exact naar Obsidian gespiegeld en op gelijke hashes gecontroleerd.

Selectie-uitbreiding gebouwd na de verduidelijking van de gebruiker: geen boekselectie betekent geen selectieactie. ITagAssignmentRepository en TagAssignmentService houden het pad gescheiden van globaal hernoemen. Bronlidmaatschappen en naam worden transactioneel opnieuw gecontroleerd; bronkoppelingen worden verwijderd voordat vervangingskoppelingen op dezelfde Order worden toegevoegd. Dit respecteert beide unieke indexen. Een bestaande doeltag op het boek blijft op haar bestaande positie, een nieuwe vervangt de bronpositie. Sourcepagina's en de eerder geladen tagindex worden na commit opnieuw ingelezen; bij een lege bron verdwijnen paneel/bronselectie. Native DataGrid-selectie blijft correct bij Meer laden en wordt zichtbaar gemarkeerd zonder toetsenbordfocus. Eigen vijfassige review en visuele controle in beide thema's uitgevoerd. Gebruikersvinkjes blijven behouden en alleen nieuwe gerichte hercontroles zijn toegevoegd; bestandswaarschuwingen zijn automatisch gedekt.
