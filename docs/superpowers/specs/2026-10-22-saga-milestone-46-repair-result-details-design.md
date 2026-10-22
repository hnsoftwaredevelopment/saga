# Milestone 46: begrijpelijke resultaten bij alle kwaliteitsherstelacties

## Status en doel

De gebruiker heeft de specificatie en aanpak bevestigd op 22 oktober 2026. PR #46 (Milestone 45) is door de gebruiker gemerged; implementatie loopt op codex/milestone-46-repair-result-details vanaf origin/main (6bee6f0). De lokale voorbereiding is behouden. De onderstaande acceptatiecriteria blijven de uitvoeringsgrens.

De gebruiker moet bij een mislukte herstelactie weten welk boek niet is aangepast en waarom. Aanleiding: titel/auteur omwisselen wordt terecht geblokkeerd wanneer het juiste boek al bestaat, maar toont alleen Mislukt: 1. De bestaande gegevensbescherming blijft ongewijzigd.

## Aannames en scope

- Alle zes herstelroutes binnen Quality vallen onder deze slice: ontbrekende auteur, onbekende taal, ontbrekende serie, titel/auteur omwisselen, tags opschonen en ontbrekende omslag.
- Eén boek en bestaande bulkacties gebruiken dezelfde begrijpelijke resultaatweergave. Er komt geen nieuwe bulkfunctie voor omslagen.
- Succes blijft kort en groen, zonder nul-foutenopsomming. Bestaande succesvolle enkelvoudige acties mogen zoals nu zonder extra melding afronden.
- Fouten, waarschuwingen en veilig overgeslagen boeken krijgen begrijpelijke uitleg. Details staan bij de resultaatmelding in het kwaliteitsscherm, niet in een extra bevestigingsstap.
- Duplicaten samenvoegen, algemeen opslaan in boekdetails, Dit is correct en herstel van genegeerde meldingen in Instellingen veranderen niet in deze slice. Dit betreft de zes metadataherstelroutes, niet ieder scherm met het woord herstellen.

## Gewenst gedrag

1. Bij volledig succes blijft de huidige korte groene samenvatting behouden; geen rood resultaat of uitgebreide details.
2. Bij afwijkingen blijft een compacte samenvatting zichtbaar. Een uitklapbaar onderdeel met begrensde hoogte toont per betrokken boek titel, auteur en reden. Geen lijst van alle geslaagde boeken.
3. Een oorzaak zoals Boek bestaat al met deze titel en auteur is in de geselecteerde Saga-taal beschikbaar. Leid deze af uit een gestructureerde reden/status, niet uit het ontleden van exceptiontekst.
4. Onderscheid minimaal: bestaand boek/conflict, boek niet meer beschikbaar, inmiddels gewijzigde of niet meer toepasselijke metadata, niet kunnen opslaan, gedeeltelijk mislukte bestandsbijwerking en omslagdownload/-verwerking mislukt. Bij onvoldoende informatie volgt een eerlijke algemene reden; verzin geen specifieke oorzaak.
5. Bij gedeeltelijke bestandsbijwerking staat expliciet dat de wijziging wel in de bibliotheek is opgeslagen. Noem dit niet volledig mislukt. Toon geen ruwe SQL, stacktrace, exceptionbericht of lokaal volledig bestandspad als gebruikersuitleg.
6. Bij bulk blijft een fout per boek beperkt: andere boeken kunnen doorgaan, geslaagde resultaten blijven behouden, werkelijke resultaten en aantallen komen overeen. Mislukte, nog bestaande regels blijven waar mogelijk geselecteerd; verdwenen of inmiddels opgeloste regels worden niet kunstmatig teruggezet.
7. Details horen bij één uitgevoerde actie. Een volgende bevestigde actie vervangt het eerdere resultaat; een categoriewissel toont geen oude details alsof ze bij de nieuwe categorie horen. Annuleren vóór uitvoering schrijft niets en maakt geen nieuw foutresultaat.
8. Details bekijken/sluiten voert geen herstelactie opnieuw uit. De weergave is met toetsenbord en schermlezer toegankelijk, werkt met lange titels en auteurs en houdt de actiebalk en boekenlijst bruikbaar.

Voorbeeld: 2 gewijzigd, 1 mislukt. In de details staat De wereld vergeten — Duenas, Maria: Dit boek bestaat al met deze titel en auteur. Als alleen bestandsbijwerking faalt: Gewijzigd in de bibliotheek; niet alle boekbestanden konden worden bijgewerkt.

## Onderzochte bestaande code en aanpak

- BookService.SaveAsync onderscheidt BookSaveStatus.Conflict en geeft Message/FileResults terug. De zes reparatieservices hebben eigen resultaatrecords met status en doorgaans Message/FileResults.
- De reparatieservices vertalen Conflict momenteel naar Failed; het dashboard verliest vervolgens Message/FileResults bij MetadataQualityBatchRepairItem. ReconcileBatchRepair telt resultaten, maar toont geen redenen. Omslagherstel heeft een afzonderlijke resultaatroute die dezelfde feedback moet gebruiken.
- Voeg de kleinste gedeelde, getypeerde foutreden toe aan de bestaande resultaatroute. Behoud onderscheid tussen opgeslagen, niet opgeslagen en bestandswaarschuwing. Bestaande contracten blijven bruikbaar; geen aparte opslagroute of tweede herstelmodel.
- Houd vertaling en presentatie buiten de opslagservice. Deel feedbackopbouw in een gericht Presentation-model/helper; maak het al grote dashboardviewmodel niet groter met zes afzonderlijke tekstformatteerders.
- Hergebruik de bestaande reconciliatie voor selecties, tellingen, bibliotheek en filters. Behoud snapshotcontroles en duplicaatbescherming. Geen schemawijziging, dependency, nieuwe zoekbron of heuristiekaanpassing.
- Gebruik alleen daadwerkelijk bekende categorieën. Een conflictstatus bewijst een conflict; alleen exceptiontekst is geen betrouwbare bron voor specifiekere oorzaken zoals toegang geweigerd.

## Projectstructuur, stijl en opdrachten

Application: src/EbookManager.Application/Metadata en bestaande BookService onder Application/Books. Presentation: src/EbookManager.Presentation/ViewModels. WPF: MetadataQualityDashboardWindow.xaml. Vertalingen: bestaande resourcebestanden voor Nederlands, Engels, Duits, Frans, Spaans en Italiaans. Tests: tests/EbookManager.Tests/Metadata, App/ViewModels, App/Views en App/Resources.

Volg bestaande C#-records, expliciete status-switches, nullable annotaties en xUnit/FluentAssertions. Voorbeeld van de bestaande stijl: `new(bookId, MetadataQualityTitleAuthorRepairStatus.NotApplicable, currentBook)`. Houd toepassingsstatus en gelokaliseerde tekst gescheiden. Geen herkenning via `Message.Contains(...)`.

- Gerichte tests: `dotnet test tests/EbookManager.Tests/EbookManager.Tests.csproj -c Debug --filter FullyQualifiedName~MetadataQuality --no-restore`.
- Volledig: `dotnet test EbookManager.sln -c Debug`.
- Build: `dotnet build src/EbookManager.App/EbookManager.App.csproj -c Debug --no-restore`.
- Whitespace: `git diff --check`.
- Testapp na een geslaagde build: `& '.\Builds\Debug\Saga.exe'`.

## Teststrategie en acceptatie

Eerst falende regressietests voor conflictreden en behoud van succesvolle resultaten. Test gestructureerde redenen en onbekende fallback, per-boekdetails en aantallen, alle zes routes, één/bulk, uitzonderingen, gedeeltelijke bestandsbijwerking, annulering, stale data, ontbrekende boeken en vervangen van oude feedback. Controleer geen ongewenste writes en geen technische berichtlekken.

WPF-layouttests bewaken bindingen, begrensde details, toegankelijke bediening en zichtbaarheid. Lokalisatietests controleren alle zes talen en placeholders. Volledige suite en Debug-build moeten schoon zijn. De handmatige checklist staat in docs/manual-tests/milestone-46-checklist.md; forceer geen riskante foutscenario's in de echte bibliotheek.

## Grenzen en oplevering

- Altijd: bestaande gegevens en gebruikerswijzigingen behouden, tests vóór gedrag wijzigen, algemene verwerking delen waar dat eenvoud geeft, alle gewijzigde Markdown exact naar Obsidian spiegelen en hashes controleren.
- Eerst afstemmen: schemawijzigingen, dependencies, uitbreiding buiten de zes routes, automatische retries, blijvende foutgeschiedenis of nieuwe beheerpagina's.
- Nooit: duplicaatbescherming omzeilen, fouttekst als instructie behandelen, technische exceptions ongefilterd tonen, herstel opnieuw uitvoeren bij openen van details, bestaande fouten als geslaagd presenteren of de PR zelf mergen.

Pas gereed na de project-DoD: gerichte en volledige tests, Debug-build met 0 waarschuwingen/fouten, precies één Saga.exe onder Builds/Debug, eigen review, handmatige acceptatie, Markdown-spiegel en een gewone niet-draft PR. De gebruiker bewaakt de huidige CodeRabbit-review; deze voorbereiding start geen nieuwe review.

## Implementatie en verificatie

De zes routes zijn aangesloten op gedeelde resultaatfeedback in MetadataQualityDashboardViewModel.Results.cs. Een begrensde gevirtualiseerde ListBox binnen een Expander toont alleen afwijkende resultaten; bekende foutredenen hebben teksten in zes talen. Ruwe Message/FileResults worden niet als UI-uitleg gebruikt. De extra FailureReason in de reparatieresultaten heeft een veilige standaardwaarde; bestaande opslag-, snapshot- en conflictroutes blijven behouden.

Eerst falende tests bewaken onder meer de ontbrekende conflictreden, de omslagfeedback, late afronding na categoriewissel en leesfouten midden in een auteur-/seriebatch. Andere regressies controleren gemengde resultaten, behoud van uitsluitingen, annuleren, vervangen van oude details, veilige onbekende reden, alle zes talen en de begrensde toegankelijke weergave. Onverwachte fouten gebruiken bewust neutrale uitleg: er wordt niet zonder bewijs beweerd dat alle metadata ongewijzigd bleef.

Volledige suite: 882 geslaagd, 0 mislukt of overgeslagen. Debug-build 2026.10.22.56 op 22 oktober 2026 om 13:25 (Europe/Amsterdam): 0 waarschuwingen en fouten. Eigen review op correctheid, eenvoud, architectuur, beveiliging en prestaties is uitgevoerd. Gevonden categoriewissel- en leesfoutrandgevallen zijn verholpen; resultaatopzoeking hergebruikt een dictionary en de foutlijst virtualiseert de zichtbare regels. Er is geen nieuwe dependency of databasewijziging. Handmatige acceptatie staat nog open.
