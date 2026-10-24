# Milestone 49: betrouwbare resultaten bij meervoudig metadata bewerken

## Status en doel

De gebruiker keurt de richting goed na merge en acceptatie van PR #49 (auteursbeheer). Dit document specificeert de voorgestelde uitwerking ter bevestiging; nog geen productcode gewijzigd. Basis: main 96fe288, branch codex/milestone-49-multi-edit-results. Geen nieuwe testbuild voor deze voorbereiding.

Doel: vanuit de gewone boekenlijst meerdere boeken bewerken en precies weten welke wijzigingen gelukt zijn, welke niet en waarom. De bestaande velden en het bestaande invulvenster blijven behouden. Geen nieuwe online bron, auteurs samenvoegen, e-readerfunctie, batchhistorie, undo of extra selectie-/previewvenster.

## Bestaande situatie

LibraryViewModel.ShowMetadataMultiEditAsync neemt de geselecteerde boek-ID's over en opent MetadataMultiEditViewModel. ApplyMetadataMultiEditAsync verwerkt auteurs, serie, tags, taal, leesstatus, titel/auteur omwisselen en aangepaste velden. Alleen duplicaatconflicten worden apart geteld. Standaardvelden gebruiken nu rechtstreeks IBookRepository.UpdateAsync; custom velden worden afzonderlijk opgeslagen, ook nadat standaardvelden door een conflict niet konden worden opgeslagen. Algemene lees-/schrijffouten kunnen de rest onderbreken. Resultaten, bestandssynchronisatie en selectie zijn niet gelijk aan de recente herstelroutes.

BookService.SaveAsync verzorgt databaseopslag en de bestaande metadata.json-/ondersteunde adapterroute. De kwaliteitsherstelservices controleren na opslag de actuele database, omdat een bestandsfout ook na een geslaagde databasewijziging kan optreden. Die grens blijft ook hier expliciet. Geen nieuwe native ebook-write-backimplementatie.

## Gedrag en acceptatie

1. Werk met een vaste selectie van unieke boek-ID's en een vaste opdracht uit het bestaande venster. Alleen aangevinkte velden wijzigen. Lege aangevinkte velden, tagmodi, serienummers en titel/auteur omwisselen behouden hun bestaande betekenis.
2. Valideer invoer, inclusief geselecteerde custom waarden, vóór opslag. Ongeldige invoer geeft een begrijpelijke melding zonder boeken te wijzigen. Gebruik geen ruwe exceptionteksten als gebruikersuitleg.
3. Verwerk elk boek onafhankelijk. Een niet-bestaand boek, leesfout, duplicaatconflict of opslagfout verhindert niet dat volgende boeken worden verwerkt. Een duplicaatconflict in de standaardvelden slaat voor dat boek ook de aangevraagde custom wijzigingen over; zo ontstaan geen onbedoelde half toegepaste opdrachten bij een bekende weigering.
4. Alle aangevraagde wijzigingen gelukt: korte groene melding, bijvoorbeeld 3 boeken bijgewerkt, zonder 0 fouten. Ongewijzigde boeken tellen apart, niet als fout en niet als nieuwe wijziging. Alleen custom wijzigingen tellen ook als een bijgewerkt boek.
5. Bij fouten of waarschuwingen verschijnt een compacte samenvatting met een uitklapbaar, begrensd en gevirtualiseerd overzicht per boek: titel, auteur en begrijpelijke reden. Voorbeelden: Boek bestaat al, Boek niet meer beschikbaar, Kon boek niet lezen, Wijzigingen konden niet worden opgeslagen, Bibliotheek bijgewerkt; bestand niet volledig bijgewerkt. Succesregels vullen de detailijst niet onnodig.
6. Resultaten onderscheiden niet opgeslagen, deels opgeslagen en bibliotheek opgeslagen met bestandswaarschuwing. Succes wordt nooit teruggedraaid omdat een ander boek faalt. Database en bestanden zijn geen gezamenlijke transactie. Een bestandswaarschuwing mag geen volledige mislukking suggereren.
7. Custom metadata blijft ondersteund. Als standaardmetadata al opgeslagen is of enkele custom velden al zijn geschreven voordat een custom fout optreedt, meldt de regel expliciet Deels bijgewerkt en welke onderdelen aandacht vragen. Geen belofte van atomair herstel van alle velden; geen stil verlies van eerdere successen. Ontbrekende custom-opslag is geen succes wanneer custom wijzigingen gevraagd zijn.
8. Na afloop worden daadwerkelijk opgeslagen waarden in rijen, details, auteurs-/tag-/taal-/custom filters en zoekresultaten in één eindverversing gereconcilieerd. Geen volledige herlaadactie of per-boekfilterverversing. Bestaande omslagen behouden; alleen noodzakelijke details volledig laden.
9. Mislukte en deels bijgewerkte boeken blijven geselecteerd voor zover zichtbaar in de actuele lijst. Volledig geslaagde en ongewijzigde boeken verdwijnen uit de batchselectie. Bestandswaarschuwingen worden duidelijk vermeld, zonder de standaardmetadata opnieuw te laten toepassen. Bij gewijzigde bibliotheek of nieuwere selectie geen oude selectie terugzetten; filters niet ongemerkt wissen om verborgen boeken zichtbaar te maken.
10. Opnieuw proberen gebeurt door de resterende selectie opnieuw te openen en bewust velden te kiezen; geen automatische herhaling van de oude opdracht of nieuwe retryknop. Reeds opgeslagen velden niet automatisch opnieuw uitvoeren, vooral titel/auteur omwisselen. Bij deels opgeslagen wijzigingen wordt de gebruiker gevraagd de velden te controleren vóór opnieuw toepassen.
11. Een tweede bewerkingsopdracht tijdens verwerken is geblokkeerd; bezetstatus wordt altijd vrijgegeven. Sluiten/annuleren van het invulvenster verandert niets. Als tijdens de batch annulering optreedt, blijven al opgeslagen resultaten zichtbaar; nog niet begonnen boeken worden als niet verwerkt beschouwd. Geen nieuwe annuleerknop of langdurige voortgangsworkflow in deze slice.
12. Oude resultaatdetails verdwijnen bij een nieuwe opdracht. Teksten in zes talen; toetsenbord, thema's en venster-/paneelbegrenzing volgen bestaande Saga-patronen. Het formulier krijgt geen tweede bevestigings- of selectieoverzicht.

## Technische richting en structuur

Gebruik een gerichte Application-service met getypeerde opdracht, per-boekuitkomst en fase-informatie. Presentation zet het bestaande dialogresultaat om naar die opdracht; Application krijgt geen afhankelijkheid op WPF of Presentation. Hergebruik parsing/validatie en bestaande metadata-/tagsemantiek zonder een tweede afwijkende implementatie te creëren. Tests bewaken verplaatsing van deze logica vóór functionele uitbreiding.

Gebruik de bestaande BookService-route voor standaardmetadata en expliciete controle van opgeslagen toestand bij fouten na databasecommit. Ontwerp de minimale opslagfase-informatie testgestuurd: een generieke Failed-status alleen bewijst niet dat de database ongewijzigd is. Houd custom opslagfase/resultaten apart. Onderzoek een lokale orkestratie vóór wijziging van het gedeelde BookSaveResult-contract; als dat contract toch moet wijzigen, behoud en test alle bestaande aanroepers.

Voeg een gerichte partial voor bulkbewerkingsorkestratie en resultaatfeedback naast LibraryViewModel toe in plaats van het al grote hoofdbestand verder uit te breiden. Verplaats uitsluitend relevante code. Houd de nieuwe feedback los van MetadataQualityDashboardViewModel; hergebruik bestaande neutrale detailcomponenten/resourcepatronen wanneer passend, geen duplicaat van alle Quality-logica en geen generiek framework voor één batchroute.

Bronnen: src/EbookManager.Application/Metadata en Books voor verwerking; src/EbookManager.Presentation/ViewModels voor dialogmapping, selectie en reconciliatie; src/EbookManager.App voor hoofdschermfeedback en Resources/Strings; tests/EbookManager.Tests voor xUnit/FluentAssertions-tests. Geen schemawijziging of dependency voorzien.

Stijlvoorbeeld uit bestaande code: `await bookService.SaveAsync(updatedBook, cancellationToken);`. Gebruik async/await, CancellationToken, expliciete records/enums, nullable-annotaties en bestaande resultaatconventies. Geen technische foutmeldingen rechtstreeks binden aan het scherm.

## Verificatie

- Eerst falende servicetests voor één conflict met een later geslaagd boek, algemene fout, ontbrekend boek, geen wijziging, alleen custom wijzigingen, deels opgeslagen custom wijzigingen en bestandswaarschuwing na databasecommit.
- Viewmodeltests voor aantallen, korte succesmelding, details, vaste selectie, mislukte selectie behouden, nieuwe selectie/bibliotheek behouden, tweede opdracht blokkeren, annulering en één eindverversing. Bewaak niet-idempotent titel/auteur omwisselen.
- Integratietest met tijdelijke SQLite-bibliotheek en echte metadata.json-sidecars; geen echte gebruikersbibliotheek aanpassen voor fouttests.
- Bestaande bulkbewerkings-, Quality-, auteurs-, filter-, custom- en omslagtests blijven groen. Lokalisatie/layouttests voor alle zes talen en begrensde details.
- Gericht: `dotnet test tests/EbookManager.Tests/EbookManager.Tests.csproj -c Debug --no-restore --filter FullyQualifiedName~MultiEdit`.
- Volledig: `dotnet test EbookManager.sln -c Debug --no-restore`.
- Build: `dotnet build src/EbookManager.App/EbookManager.App.csproj -c Debug --no-restore`.
- Diff: `git diff --check`.
- Verse enige Debug-build onder Builds/Debug, versie/tijd vastleggen; eigen vijfassige review, normale niet-draft PR en handmatige checklist. Alleen gebruiker mergt. Markdown exact naar Obsidian spiegelen en hashes controleren.

## Grenzen en besluit

Altijd: behoud van opgeslagen gegevens en gebruikerswijzigingen, eerst falende tests, veilige redenen, actuele resultaten en eerlijke fasegrenzen. Eerst afstemmen: schemawijzigingen, nieuwe dependencies, undo/historie, automatische retries, extra invoer-/previewvensters, bredere wijzigingen aan generieke opslagcontracten. Nooit: auteurs samenvoegen, bestandswaarschuwing als volledige mislukking tonen, een oude swapopdracht automatisch herhalen, echte bibliotheek beschadigen voor een fouttest, zelf mergen.

De richting is akkoord; bevestiging van deze concrete uitwerking en de omgang met deels opgeslagen/custom wijzigingen gaat vooraf aan implementatie.
