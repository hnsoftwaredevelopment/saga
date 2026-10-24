# Milestone 50: series beheren

## Status en doel

De gebruiker heeft deze concrete functionele en technische uitwerking bevestigd. Implementatie is afgerond op codex/milestone-50-series-management vanaf main f8ad6fa na geaccepteerde PR #50. 980 tests slagen; Debug-build 2026.10.24.52 van 24 oktober 15:01 (Europe/Amsterdam) heeft 0 waarschuwingen en fouten. Eigen review en WPF-rendercontrole op groot en klein werkgebied uitgevoerd; handmatige gebruikersacceptatie van Milestone 50 staat open. Geen schemawijziging, nieuwe dependency of conversie.

Doel: een centraal, bibliotheekgebonden scherm zoals Auteurs beheren waarin de gebruiker series kan zoeken, hun boeken op volgorde kan bekijken en een serienaam veilig voor alle gekoppelde boeken kan wijzigen. Geen samenvoegen, nieuwe nummering, online lookup of automatische herkenning van complete/ontbrekende delen.

## Uitgangspunten en bestaande situatie

Books bevat Series als optionele string en SeriesNumber als nullable decimal. Er is geen aparte seriesentiteit. Het bestaande seriefilter groepeert namen na Trim met StringComparer.OrdinalIgnoreCase. Boekdetails en bulkbewerking wijzigen series al per boek; dit nieuwe scherm geeft een andere, expliciete context: de volledige geselecteerde serie hernoemen.

Voorstel: behoud het bestaande opslagmodel zonder migratie. Gebruik dezelfde trim-/hoofdletterongevoelige groepering als het huidige seriefilter. SAS en sas horen daardoor bij dezelfde logische serie, ook als de opgeslagen schrijfwijze verschilt. Alleen lezen/groeperen wijzigt geen data. Andere spelfouten, accenten of verschillende namen worden niet op basis van gelijkenis samengevoegd. De naam is leidend, niet de auteur; een serie met verschillende auteurs blijft één groep, net als in het huidige filter.

Voor een reproduceerbare vertegenwoordiging van een groep wordt een bestaande getrimde naam deterministisch gekozen. Bewaar in de lees-/bevestigingssnapshot ook de echte opgeslagen schrijfvarianten; voer Unicode-vergelijking niet ongemerkt anders uit in de SQL- en .NET-paden. Implementeer één gerichte vergelijking binnen de seriesfunctionaliteit en test die tegen het bestaande filtergedrag.

## Gedrag en acceptatie

1. Een actie Series beheren in de hoofdwerkbalk opent een modaal venster voor de actieve bibliotheek. Geen boeken zonder serienaam in de serieslijst. Series worden alleen uit bestaande boeken afgeleid; geen lege series aanmaken of bewaren.
2. Links een doorzoekbare, gepagineerde/gevirtualiseerde lijst met serienaam en boekaantal. Zoeken filtert alleen die lijst, niet de hoofdboekenselectie. Geen foto-/omslagbytes voor de lijsten laden. Bij geen resultaten een begrijpelijke lege toestand.
3. Rechts de geselecteerde naam, totaal aantal boeken en een alleen-lezen boekenlijst met serienummer, titel en auteur(s). Bekende nummers numeriek oplopend: 1, 1,5, 2, 10; geen tekstsortering. Boeken zonder nummer onderaan. Gelijke nummers zijn toegestaan en sorteren vervolgens op titel en ID. Bestaande decimalen, nulwaarden en andere opgeslagen nummers blijven intact; niets opnieuw nummeren.
4. Boekenlijsten laden in begrensde pagina's, maar sorteren de hele geselecteerde serie correct vóór paginering. Meer laden verliest of dupliceert geen boeken. Voor de sorteerkeuze eerst een echte SQLite-test met decimalen en meer dan 200 boeken; geen onnauwkeurige omzetting naar double als vervanging voor decimal.
5. Een aparte actie Serie hernoemen toont huidige naam, nieuwe naam en het volledige aantal betrokken boeken, ook voorbij de zichtbare pagina. Annuleren/Escape wijzigt niets. Het venster bevat geen inline bewerking van serienummers of andere boekvelden.
6. Een niet-lege vrije doelnaam wordt getrimd en voor de volledige geselecteerde groep opgeslagen. Hoofdletters corrigeren binnen dezelfde groep mag. Een doelnaam die volgens dezelfde vergelijking bij een andere groep bestaat wordt geweigerd: geen impliciet samenvoegen, ook niet via spatie-/hoofdlettervarianten. Bij geen daadwerkelijke wijziging geen schijnbaar succes.
7. Alleen Series en de bijbehorende wijzigingstijd mogen veranderen. SeriesNumber, titel, auteurkoppelingen/-volgorde, tags, omslag, formaten, custom waarden, IDs, DuplicateKey en overige metadata blijven behouden. Geen map- of bestandsverplaatsing: de opslag is al ID-gebaseerd.
8. De databasewijziging gebeurt in één transactie. Bron, doel, volledige boek-ID-set en opgeslagen bronwaarden worden opnieuw gecontroleerd tegen de bevestigde snapshot. Nieuwe/verwijderde/verplaatste boeken of een tussentijds hernoemde serie geven een begrijpelijk conflict en vragen een nieuwe bevestiging; geen stille extra boeken meenemen. Richt de update alleen op Series, zodat andere actuele boekvelden niet uit een oude snapshot worden teruggeschreven.
9. Een databasefout of annulering vóór commit laat geen gedeeltelijk hernoemde serie achter. Na commit worden metadata.json en bestaande ondersteunde bestandsroutes via BookService.SynchronizeStoredMetadataAsync gesynchroniseerd met actuele opgeslagen metadata. Voltooi deze stap na commit; een bestandsprobleem meldt een waarschuwing per boek en stelt een geslaagde databasewijziging niet voor als volledig mislukt. Geen nieuwe native metadatawriter.
10. Na succes blijft de nieuwe naam zichtbaar en geselecteerd, ook buiten de eerste 200 ongefilterde series. Gebruik dezelfde nieuwe zoekcontext in zoekveld en resultaten. Nummering en paginering blijven correct. Sluiten van het venster ververst gewijzigde boeken, details, zoekresultaten en seriefilter in één bestaande reconciliatieroute; geladen omslagen blijven behouden.
11. Tweede opdrachten, selectie-/bibliotheekwissel en sluiten tijdens opslag zijn bewaakt. Verouderde zoek-/selectieresultaten mogen niet een nieuwere selectie overschrijven. Fouten tijdens latere schermverversing worden onderscheiden van een fout vóór opslag. Geen automatische herhaling van hernoemen na een onzeker resultaat.
12. Het venster heeft een zichtbare, bedienbare splitter en een begrensde/scrollbare rechterzijde, zodat knoppen bereikbaar blijven bij verkleinen. Toetsenbord, focus, Enter/Escape en toegankelijke namen volgen Auteurs beheren. Teksten in Engels, Nederlands, Duits, Frans, Spaans en Italiaans. Korte groene succesmelding; bij waarschuwingen/fouten compacte uitleg met uitklapbare, gevirtualiseerde per-boekdetails.

## Technische richting en structuur

Voeg gerichte seriescontracten en lichte leesmodellen toe onder Domain/Series en Domain/Abstractions. EF-implementatie in Infrastructure/Persistence/Repositories, toepassingsorkestratie in Application/Series, afzonderlijk viewmodel in Presentation/ViewModels en venster/interactie in App/Views. Hergebruik bestaande werkbalkstijl, splitterstijl, bevestigingspatroon en lokalisatie. Kopieer geen auteursprofielfoto-/biografielogica naar seriesbeheer; er is geen seriesprofiel in deze slice.

Laat de naamindex alleen namen en aantallen projecteren. Wanneer trim-/Unicode-hoofdlettergroepering niet exact naar SQL vertaalt, mag een dunne naamindex in .NET worden gegroepeerd en gepagineerd; laad geen volledige bibliotheekboeken of omslagen om namen te tonen. Projecteer voor de geselecteerde serie alleen nodige ID-, titel-, auteurs- en nummerinformatie. De numerieke paginering wordt eerst praktisch bewezen; zo nodig een dunne sorteersleutellijst per geselecteerde serie, niet de volledige bibliotheekvoorraad.

De hernoemrepository valideert en wijzigt uitsluitend series binnen één transactie. Orkestratie synchroniseert daarna bestanden en geeft gewijzigde boek-ID's en aparte waarschuwingen terug. Hergebruik LibraryViewModel.ReconcileExternalMetadataChangesAsync voor afsluitende actualisatie in plaats van een nieuwe volledige herlaadroute. Geen uitbreiding van het al grote LibraryViewModel met nieuwe seriesbedrijfslogica; houd aansluiting beperkt tot openen/actualiseren.

Stijlvoorbeeld uit de bestaande code: `await bookService.SynchronizeStoredMetadataAsync(bookId, CancellationToken.None);` ná bevestigde databasecommit. Gebruik getypeerde records/statussen, async/await, nullable conventies, CancellationToken en bestaande xUnit/FluentAssertions-tests. Geen ruwe exceptiontekst in het scherm.

## Bouwvolgorde en verificatie

Eerst identiteit/groepering en numerieke lees-/pagineringstests, daarna transactioneel hernoemen, daarna bestandssynchronisatie en resultaatgrenzen, vervolgens viewmodel/UI en zes talen. Iedere geteste samenhangende stap krijgt een eigen commit. Geen agents of parallelle productiewijzigingen nodig.

- Tijdelijke SQLite-tests: alleen juiste serieboeken, schrijfvarianten, accent-/Unicodegevallen, nummering 1/1,5/2/10/null, gelijke nummers en meer dan 200 boeken zonder verkeerde sortering of doublures.
- Hernoemtests: bezette doelnaam, schrijfwijzecorrectie, lege naam, ongewijzigd, behoud van alle overige metadata/duplicaatsleutels, gewijzigde boek-ID-set/bronwaarden, rollback met gecontroleerde fout, annulering vóór commit.
- Service-/bestandstests: actuele metadata.json, fout na commit als waarschuwing per boek, geen stille rollback, geen herhaling en correcte verwerking voorbij de eerste pagina. Alleen tijdelijke bibliotheken voor fouttests.
- Viewmodel-/UI-tests: snelle zoekresultaten zonder freeze, nieuwere selectie behouden, tweede opdracht blokkeren, selectie na hernoemen buiten de eerste pagina, eigen zoekveld/resultaten gelijk, juiste aantal, begrensde details en zes talen.
- Hoofdschermregressies: seriefilter, geselecteerde details, zoekresultaten en omslagen actualiseren; bestaande auteur-, bulk- en Quality-routes blijven groen.
- Gericht: `dotnet test tests/EbookManager.Tests/EbookManager.Tests.csproj -c Debug --no-restore --filter FullyQualifiedName~Series`.
- Volledig: `dotnet test EbookManager.sln -c Debug --no-restore`.
- Build: `dotnet build src/EbookManager.App/EbookManager.App.csproj -c Debug --no-restore`.
- Diff: `git diff --check`.

Opleveren met een verse enige Debug-build onder Builds/Debug, versie/tijd in de PR, eigen vijfassige review en handmatige checklist. De bestaande 959 tests/build 2026.10.24.37 bewijzen deze nieuwe functie nog niet. Handmatige acceptatie pas na een nieuwe testbuild afvinken. Gewone niet-draft PR; alleen gebruiker mergt. Iedere gewijzigde Markdown exact naar Obsidian spiegelen met behoud van relatieve paden en gelijke hashes.

## Grenzen en bevestiging

Altijd: naamvergelijking consequent, serienummers en overige metadata behouden, eerst falende tests, gerichte projecties, expliciete commitgrens, veilige waarschuwingen en actuele selectie. Eerst afstemmen: aparte serietabel/migratie, nieuwe dependency, serienummers wijzigen, series samenvoegen/splitsen, nieuwe online bron, automatische volledigheidsdetectie of bredere wijziging aan generieke opslagcontracten. Nooit: serienummers automatisch corrigeren, gelijkende namen op gevoel combineren, data naar een oude snapshot terugzetten, een bestandswaarschuwing als volledige opslagmislukking tonen, echte boeken beschadigen voor tests of zelf mergen.

Richting en concrete keuzes zijn vóór de bouw door de gebruiker bevestigd: dezelfde naamgroepering als het bestaande filter, ontbrekende nummers onderaan, geen nummerbewerking en geen conversie.

## Uitgevoerde verificatie

De numerieke sortering gebeurt met exacte decimalen op dunne sleutels van alleen de geselecteerde serie, vóór paginering; auteurs worden alleen voor de geladen pagina opgehaald. SQLite-tests bewijzen Unicode-/schrijfvariantgroepering, 210 boeken zonder verlies of doublures en 510 boeken in een transactionele hernoeming. Een triggerfout in een latere updategroep bewijst volledige rollback. Toegevoegde boeken of gewijzigde bronwaarden maken de bevestiging ongeldig. Alle scalairen behalve Series/UpdatedUtc en de bestaande auteurvolgorde, tags, bestanden en custom waarden blijven behouden.

Bestandstests controleren echte metadata.json, een bestandsfout na commit en het afmaken van alle boeken bij annulering ná commit. Viewmodeltests bewaken late zoekresultaten, tweede opdrachten, nieuwe zoeknaam/selectie, waarschuwingen en een weergavefout na commit. Layout-/resourcetests controleren de begrensde lijsten, splitter en zes talen. Visuele WPF-controle met fictieve gegevens op 1040×650 en 800×480; geen echte bibliotheek gebruikt. De splitter kreeg dezelfde lokale contrastkleur als auteursbeheer; serienummers volgen de huidige cultuur.

Eigen review op correctheid, leesbaarheid/eenvoud, architectuur, veiligheid en prestaties: beperkte metadata-update, geparameteriseerde EF-query's, één transactie, gerichte projecties zonder omslagen, bestaand bestandspad en aparte feedback na commit. Geen open productbevindingen. Moeilijke foutscenario's zijn automatisch getest; gebruikersacceptatie blijft open in de nieuwe checklist.

## Aanvulling na gebruikerstoets en CodeRabbit

De gebruiker heeft de hoofdroute getest en afgevinkt, behalve toetsenbordgebruik. CodeRabbit bevestigde de oorzaak: IsBusy schakelt de lijst en hernoemknop tijdelijk uit via CanBrowse/CanRename, waardoor de focus verloren gaat. Een eerst falende proef op het echte WPF-venster reproduceerde dit. Het venster bewaart nu de focus vóór uitschakelen en herstelt haar pas na het opnieuw inschakelen, uitsluitend binnen het actieve venster en op een zichtbaar, ingeschakeld element. Een versiecontrole voorkomt herstel door een oudere opdracht; een vergelijking met de focus bij voltooiing respecteert een nieuwere focuskeuze. De hernoemdialoog houdt haar eigen focus en geeft die na sluiten terug aan de aanroepende knop.

Debug-build 2026.10.24.61 van 24 oktober 15:35 (Europe/Amsterdam) heeft 0 waarschuwingen/fouten. De volledige 980 tests en vijf interactieve WPF-focusscenario's slagen. Ook het timingprobleem bij overschakelen naar het zoekveld is eerst falend bewezen. Het blijvende WPF-testprogramma onder tests/EbookManager.WpfSmokeTests gebruikt uitsluitend fictieve gegevens, geen nieuwe pakketten en geen normale Saga-startup. Het draait los van de gewone suite omdat toetsenbordfocus een interactief Windows-bureaublad vereist. Eigen vijfassige review opnieuw uitgevoerd; opslag en gegevensmodel zijn ongewijzigd. De aanvullende handmatige toetsenbordcontrole blijft open.
