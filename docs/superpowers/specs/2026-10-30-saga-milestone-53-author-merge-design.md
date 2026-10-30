# Milestone 53 — auteurs bewust samenvoegen

## Opdracht en uitgangspunten

De gebruiker heeft op 30 oktober 2026 de voorgestelde slice goedgekeurd en opdracht tot bouw gegeven. PR #54 is gemerged naar main 7d3ed6e. Branch: codex/milestone-53-author-merge. Deze uitwerking concretiseert de goedgekeurde werkwijze zonder een nieuwe akkoordronde: expliciete meervoudige selectie in Auteurs beheren, bestemming is één bestaande geselecteerde auteur, keuzes per profielonderdeel, één bevestigde transactie. Automatische suggesties en een nieuwe doelnaam vallen buiten deze eerste slice.

## Gebruikersgedrag en acceptatie

1. Auteurs beheren ondersteunt Ctrl/Shift-selectie van geladen auteurs. Meer laden behoudt de selectie. Een nieuwe zoekopdracht wist de meervoudige selectie; er worden nooit verborgen auteurs automatisch meegenomen. De telling geeft het aantal geselecteerde auteurs. Bij meerdere geselecteerde auteurs zijn profielbewerking en hernoemen uitgeschakeld; Samenvoegen is beschikbaar vanaf twee auteurs, zonder onopgeslagen profielwijzigingen.
2. Het samenvoegvenster toont alle geselecteerde namen met aantallen boeken en het aantal unieke betrokken boeken. De gebruiker kiest één geselecteerde auteur als bestemming. De overige auteurrecords worden na bevestiging verwijderd; alle gekoppelde boeken worden behouden. Annuleren schrijft niets.
3. Per foto, website en biografie wordt de te bewaren bron expliciet gekozen. Eén unieke waarde kan vooraf gekozen zijn; bij verschillende waarden moet de gebruiker een keuze maken. Leeg behouden is expliciet mogelijk. De gekozen foto wordt onder de doelidentiteit opgeslagen voordat de database ernaar verwijst. De bestemming behoudt haar naam en ID. Profielvelden worden niet stil samengevoegd of aaneengeplakt.
4. Per betrokken boek blijft de bestemming eenmaal bestaan. Een reeds aanwezige doelvermelding behoudt haar positie; anders vervangt de bestemming de eerste bronpositie. Overige mede-auteurs houden hun relatieve volgorde. Andere metadata en bestanden blijven via de bestaande opslagroute behouden.
5. Dubbelboekcontrole herberekent de bestaande DuplicateKey op basis van titel en uiteindelijke auteurs. Een botsing tussen betrokken boeken of met een ander boek weigert de hele samenvoeging, zonder boeken samen te voegen of gegevens gedeeltelijk te wijzigen.
6. Voor schrijven worden alle geselecteerde profielen en de volledige gekoppelde boeksets opnieuw vergeleken met de bevestigde preview. Verouderde selectie, gewijzigde namen/profielen of koppelingen geven een begrijpelijk conflict. Ongeldige of onbekende auteur, minder dan twee unieke IDs of een bestemming buiten de selectie worden geweigerd. SQL is geparametriseerd en pagina's blijven licht; omslagen worden niet geladen.
7. Auteurkoppelingen, profielkeuzes, verwijderde bronrecords en DuplicateKey/UpdatedUtc worden in één SQLite-transactie opgeslagen. Een fout of annulering vóór commit rolt alles terug. Foto-opruiming verwijdert nooit een nog verwezen bestand; opruimproblemen worden als afzonderlijke waarschuwing gemeld.
8. Na commit synchroniseert BookService de actuele metadata van gewijzigde bronboeken, ook als de aanvrager daarna annuleert. Bestandswaarschuwingen zijn afzonderlijk zichtbaar. Bij een verversingsfout na commit blijven de gewijzigde boek-IDs en waarschuwingen beschikbaar en meldt Saga eerlijk dat de wijziging wel is opgeslagen.
9. Na succes wordt de doelauteur met haar actuele profiel, foto, boeken en telling geselecteerd. De bronvermeldingen verdwijnen uit de index. Na sluiten verversen hoofdscherm, filters, groepering en boekdetails via de bestaande reconciliatie, met behoud van omslagen. Geen nieuwe globale bibliotheekscan of boekmapverplaatsing.
10. Zoekveld, selectie, dialoog en bevestigen/annuleren werken met het toetsenbord zonder vertraagde focusdiefstal. Knoppen blijven bij verkleinen bereikbaar. Teksten staan in alle zes talen. Tijdens opslag zijn nieuwe acties en sluiten geblokkeerd.

## Structuur en stijl

.NET 10, WPF, EF Core/SQLite en CommunityToolkit.Mvvm blijven leidend. Geen schemawijziging of dependency. Domain/Authors bevat preview/keuze/resultaat en Abstractions het gerichte mergecontract. EfAuthorProfileRepository krijgt een gerichte partial; Application/Authors orkestreert foto-opslag en bestaande BookService. Presentation krijgt een dialoogviewmodel en een mergepartial voor Auteurs beheren. App bevat het WPF-venster en selectieaansluiting.

Volg bestaande stijl: immutable records, getypeerde uitkomsten, async CancellationToken, ObservableObject, bestaande normalisatie en namenregels. Bijvoorbeeld `public sealed record AuthorMergeSelection(Guid TargetId, Guid? BiographyAuthorId, Guid? WebsiteAuthorId, Guid? PhotoAuthorId);`. Null kiest bewust leeg; geen willekeurige profieltekst of bestandspaden uit de dialoog accepteren.

## Teststrategie en commando's

Eerst falende xUnit/FluentAssertions-tests in EbookManager.Tests. Echte tijdelijke SQLite-bibliotheken bewaken deduplicatie, auteursvolgorde, alle boekvelden, profielkeuze, dubbelboekconflict, stale preview, annulering, rollback en meer dan 500 boeken. Servicetests bewaken foto-eigenaarschap, opruiming en metadata-/waarschuwingsroute na commit. Viewmodeltests bewaken expliciete keuzes, geen verborgen selectie, directe verversing en fouten. Echte WPF-proeven gebruiken uitsluitend fictieve data; handmatige checklist voor normale bediening en beide thema's.

- Gericht: `dotnet test tests/EbookManager.Tests/EbookManager.Tests.csproj -c Debug --filter FullyQualifiedName~AuthorMerge`
- Volledig: `dotnet test EbookManager.sln -c Debug --no-restore`
- WPF: `dotnet run --project tests/EbookManager.WpfSmokeTests/EbookManager.WpfSmokeTests.csproj -c Debug --no-restore`
- Build: `dotnet build src/EbookManager.App/EbookManager.App.csproj -c Debug --no-restore`

## Uitvoering en grenzen

Volg tasks/plan.md en tasks/todo.md in geteste increments. Eerst transactie/boekveiligheid, daarna foto-/bestandsroute, dialoogkeuzes, selectiescherm en integrale controle. Geen agents nodig. De volledige project-DoD blijft gelden: eigen vijfassige review, schone enige Debug-build met versie/tijd, gewone PR, actuele documentatie en exacte Obsidian-spiegels. Alleen de gebruiker mergt. Nieuwe dependencies of schemawijzigingen eerst bespreken. Nooit automatisch auteurs als dezelfde persoon behandelen, boeken stil samenvoegen, buiten de actieve bibliotheek schrijven, gevoelige bestanden committen of ongevraagd de echte bibliotheek aanpassen.

## Uitvoering en verificatie

Gebouwd in afzonderlijke geteste commits. De gerichte mergepartial valideert alle profielvelden en volledige boeksets transactioneel. De bestaande DuplicateKeyNormalizer bepaalt botsingen; 510 boeken en rollback zijn getest. De service kopieert een gekozen bronfoto naar een uniek doel-eigen pad vóór commit, ruimt daarna uitsluitend niet meer verwezen foto's op en rondt bestaande BookService-synchronisatie af ondanks annulering na commit. Lees-/schrijf-/opruimfouten bij foto's en bestandswaarschuwingen zijn afzonderlijk getest.

Het dialoogviewmodel vereist expliciete conflictoplossing. Het hoofdvenster houdt native selecties bij identiteit vast tijdens paging en wist ze bij nieuw zoeken. Eén versus meerdere geselecteerde auteurs bepaalt of het gewone profiel bewerkbaar is. Programmatische zoektekst na samenvoegen of hernoemen veroorzaakt geen tweede vertraagde zoekactie die de doelkeuze wist. Na een verversingsfout blijven de opgeslagen uitkomst en betrokken waarschuwingen bewaard.

Volledige suite: **1074 geslaagd, 0 mislukt/overgeslagen**. **22 echte WPF-scenario's groen**, inclusief twee nieuwe proeven voor auteursselectie, dialoog, annuleren/bevestigen, focus, doelauteurverversing, paging en zoekreset. Minimaal dialoogformaat in licht/donker gerenderd en bekeken. Debug-build **2026.10.30.20**, **30 oktober 2026 19:25:45 Europe/Amsterdam**, **0 waarschuwingen/fouten**, precies één Saga.exe onder Builds/Debug. Eigen review op correctheid, eenvoud, architectuur, veiligheid en prestaties uitgevoerd. [Handmatige acceptatie](../../manual-tests/milestone-53-checklist.md) staat open; echte bibliotheekgegevens zijn niet door de tests gewijzigd.
