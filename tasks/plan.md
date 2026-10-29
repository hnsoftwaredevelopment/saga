# Milestone 52: tags beheren

Scope op 29 oktober 2026 door de gebruiker goedgekeurd. Specificatie: docs/superpowers/specs/2026-10-29-saga-milestone-52-tag-management-design.md. Basis geaccepteerde en gemergede PR #53 / main e8e024c, branch codex/milestone-52-tag-management. Geen schemawijziging, dependency, bibliotheekconversie of agents.

Uitgevoerd in geteste increments; handmatige acceptatie staat open. 1053 gewone tests en twintig interactieve WPF-scenario's slagen. Debug-build 2026.10.29.47 van 29 oktober om 15:07:43 (Europe/Amsterdam) heeft 0 waarschuwingen/fouten en is de enige Saga.exe onder Builds/Debug. Eigen vijfassige review en visuele controle in beide thema's afgerond. De aanvullende auteursvariantenwens is als latere, afzonderlijke feature vastgelegd.

1. **Lichte leescontracten en paginering.** Afzonderlijke tag-IDs, zoeken met aantallen en boeken op titel/ID. Verificatie: SQLite-pagina's met meer dan 200 tags/boeken en Unicode; geen coverblobs of ebookbestanden voor lijsten.
2. **Transactioneel hernoemen.** Bron-ID/naam en bevestigde boekset opnieuw controleren; een unieke doelnaam wijzigen of de bestaande doelidentiteit hergebruiken, bronkoppelingen dedupliceren en de lege bron verwijderen; alleen betrokken UpdatedUtc wijzigen. Verificatie: eerst falende tests, behoud van andere tags/volgorde en alle boekvelden, 510 boeken, rollback, gewijzigde bronkoppelingen, ongewijzigde/ongeldige naam en annulering.
3. **Bestandssynchronisatie na commit.** Bestaande BookService-route en afzonderlijke bestandswaarschuwingen. Verificatie: eerst falende echte metadata.json-/waarschuwingsproeven en voltooiing bij annulering na commit.
4. **Beheerviewmodel.** Zoeken, paginering, selectie, bevestiging, annuleren, bezetstatus en nieuwe naam behouden. Verificatie: gerichte tests; extra eerst falende regressies voor stale preview en eerlijk opgeslagen-maar-niet-ververst resultaat zijn opgelost.
5. **Venster, werkbalk en talen.** Bestaande SeriesManagement-bediening volgen, tagicoon, zichtbare splitter, bereikbare knoppen en zes talen. Verificatie: XAML-/resourcetests en echte WPF-focustests voor laden, fout, nieuwere focus, zoektekst en annuleren/opslaan. Licht/donker op minimale grootte bekeken.
6. **Opleveren.** Volledige suite, schone Debug-build, eigen review, actuele README/featurestatus, handmatige checklist en exacte Obsidian-spiegels. Gewone niet-draft PR; alleen de gebruiker accepteert en mergt.

Afhankelijkheden: 1 vóór 2/4, 2 vóór 3, 3/4 vóór 5, 6 na alles. Checkpoints en commits volgen deze volgorde; de venster-/resourceaansluiting is apart gecommit. Opslagrisico's zijn afgedekt met transactie/ID-setcontrole, bestandsproblemen worden na commit onderscheiden en focus gebruikt het beproefde herstelpatroon van seriesbeheer. De aanvullende gebruikersopdracht breidt deze slice wel uit met wijzigen/verwijderen van de brontag op geselecteerde boeken. Expliciet globaal hernoemen naar een bestaande tag voegt de bron samen met het doel. Splitsen, zelfstandig globaal verwijderen en auteurs samenvoegen blijven buiten scope.
Aanvulling na gebruikerstest: bron-/doelboeklijsten worden na een selectieactie opnieuw uit opslag geladen. Nieuwe of bestaande doeltag wordt hergebruikt; een reeds aanwezige doeltag blijft op haar bestaande positie en alleen de brontag verdwijnt. Native boekselectie wordt expliciet gesynchroniseerd bij paginering en reset bij een andere bron. Eerst falende repository/service/viewmodel/dialoog/resourceproeven en twee extra echte WPF-proeven zijn groen. De oorspronkelijke hernoemactie is expliciet globaal benoemd; moeilijke bestandswaarschuwingen hoeven niet handmatig te worden geforceerd.

## Historie: Milestone 51 — geaccepteerd en gemerged
# Milestone 51: meervoudige boekenkastselectie

Actuele verificatie 29 oktober: de gedeelde boek-ID krijgt nu ook de juiste groepsvermelding voor focus en scroll. De nieuwe WPF-regressie bewijst de focus op de bedoelde groepslijst, verder navigeren en Shift/Ctrl. 993 tests en vijftien WPF-scenario's groen, inclusief de normale Debug-build 2026.10.29.9 van 08:59:31 (Europe/Amsterdam), met 0 waarschuwingen/fouten en één Saga.exe onder Builds. Op 29 oktober zijn alle resterende checklistpunten door de gebruiker getest en akkoord bevonden. Eigen review opnieuw zonder aanvullende bevindingen; CodeQL en GitGuardian groen. CodeRabbit kan de laatste fix door haar limiet niet beoordelen; de eigen review, regressietests en gebruikersacceptatie zijn vastgelegd. Oudere build-/testblokken hieronder zijn uitvoeringshistorie; het onterecht actuele blok van 27 oktober is historisch gemarkeerd.

Reviewstatus 29 oktober 2026: de nog geldige CodeRabbit-opmerking over Up/Down bij gedeeltelijke boekenplankgroepen is eerst falend bewezen en gericht hersteld. Groepslokale rij/kolom en expliciete overgang vervangen de globale indexstap; Shift-reeksen behouden hun bestaande unieke volgorde. 993 tests en veertien WPF-scenario's groen, Debug-build 2026.10.29.5 om 08:15 zonder waarschuwingen/fouten en één Saga.exe. Eigen vijfassige review uitgevoerd; nieuwe handmatige hercontrole toegevoegd zonder eerdere acceptatie te wijzigen. Eerdere kolombreedte-/documentatieopmerkingen waren al verwerkt. Geen nieuwe afhankelijkheden of agents.

Actuele gebruikersacceptatie, 27 oktober 2026: meerdere boeken selecteren en metadata bewerken werkt in alle drie de weergaven zoals verwacht. De gegroepeerde-rijencorrectie is akkoord; niet afzonderlijk bevestigde detailchecks blijven in de checklist staan. PR #53 staat nog open. Geen codewijziging of nieuwe build voor deze acceptatieregistratie; onderstaande stappen bewaren de uitvoeringshistorie.

Voorstel en bouwopdracht zijn door de gebruiker goedgekeurd. Specificatie: docs/superpowers/specs/2026-10-27-saga-milestone-51-bookshelf-selection-design.md. Basis main 5360aed plus acceptatiedocumentatie PR #52. Geen nieuwe dependency, schema of opslagroute; geen agents nodig.

Historische verificatie van 27 oktober 2026: onderstaande stappen en reviewcorrecties zijn uitgevoerd in geteste commits. Destijds slaagden 993 gewone tests en dertien interactieve WPF-scenario's. Debug-build 2026.10.27.34 van 27 oktober 16:03 (Europe/Amsterdam) heeft 0 waarschuwingen/fouten en is de enige Saga.exe onder Builds/Debug. De gebruiker heeft de knopcorrectie en groepsselectie op de boekenplank bevestigd. De nieuwe melding over enkelvoudige selectie in gegroepeerde Lijst/Gedetailleerd is eerst falend bewezen en hersteld met gedeelde inputafhandeling en stijl. Beide views houden hun eigen groepering en dezelfde geselecteerde IDs. Eigen vijfassige review en visuele rijcontrole uitgevoerd. Handmatige acceptatie blijft gedeeltelijk open voor de resterende groeps-/weergave-/filterpunten, smalle-paneelnavigatie en de nieuwe gegroepeerde-rijencontroles.

Reviewafronding: de dubbele knop is door de gebruiker geaccepteerd als verwijderd. CodeRabbit wees op een afwijkende kolomberekening bij scrollbar/padding; navigatie gebruikt nu de werkelijke ColumnCount van het paneel. Een extra WPF-regressie controleert gerenderde posities in vier layouts. README/checklist/spec/taken geven dezelfde gedeeltelijk open acceptatie aan. PR #52 is gemerged; main ea65412 is geïntegreerd. De onderstaande lijst loopt ononderbroken door.

1. **Selectie bewijzen.** Eerst falende tests voor Ctrl/Shift/range en unieke IDs; gerichte Presentation-selectiepartial en afgeleide rijstatus. Circa 3–5 bestanden. Verificatie: selectie blijft na filterdoorsnede en rijvervanging juist.

2. **Tegels aansluiten.** Eén centrale selectie voor gewone en open gegroepeerde tegels, duidelijke rand/focus en modificatietoetsen. Circa 2–4 bestanden. Verificatie: echte WPF-selectie en dubbele groepsvermeldingen.
3. **Toetsenbord en scroll.** Pijltjes/Home/End/Spatie/Escape binnen tegels, Ctrl-focus zonder selectiewijziging, virtualisatie behouden. Circa 2–4 bestanden. Verificatie: voorbij de viewport en geen focusverlies.

   Checkpoint: unieke-ID-selectie en inputroutes bewezen vóór aansluiting van viewwissels en bulkuitkomsten.

4. **Weergavewissel en bulk.** Bestaande grids veilig synchroniseren; tijdelijke resetselectie niet terugschrijven. Bestaande metadata-opdracht verwerkt dezelfde IDs en houdt foutselectie zichtbaar. Opsplitsen in increments van circa 3–5 bestanden. Verificatie: grid→tegels→grid, filters, annuleren en fouten.
5. **Telling en talen.** Telling/uitleg bij de tegels, de bestaande actie in de hoofdwerkbalk en zes talen in kleine increments. Verificatie: resources, thema's en kleine werkgebieden.
6. **Opleveren.** Volledige suite en WPF-controles, schone enige Debug-build, eigen review, checklist/README/featurestatus en exacte Obsidian-spiegels. Gewone PR; alleen gebruiker mergt en accepteert de handmatige checklist.

Afhankelijkheden: 1 vóór 2–3; 2–3 vóór 4; 5 na bewezen interactie; 6 na alles. Grootste risico's: native ListBox-selectie versus centrale selectie, dubbele taggroepen, gridresets, rijen met nieuwe referenties en focus op niet-gerealiseerde containers. Mitigatie: identiteit centraal, beperkte synchronisatiebewaking en echte WPF-tests met fictieve gegevens. Het goedgekeurde voorstel bepaalt de scope; de technische uitwerking vraagt geen nieuwe akkoordronde.

## Historie: Milestone 50

# Milestone 50: seriesbeheer

Richting en concrete specificatie door gebruiker bevestigd: docs/superpowers/specs/2026-10-24-saga-milestone-50-series-management-design.md. Gebouwd op codex/milestone-50-series-management vanaf geaccepteerde main f8ad6fa. Onderstaande increments zijn uitgevoerd met gerichte tests en commits. Numerieke SQLite-paginering, transactioneel hernoemen, bestandssynchronisatie, viewmodel, vensters en zes talen zijn aangesloten. 980 tests slagen; Debug-build 2026.10.24.52 van 24 oktober 15:01 (Europe/Amsterdam), 0 waarschuwingen/fouten en één Saga.exe. Eigen vijfassige review en WPF-rendercontrole uitgevoerd. Geen schemawijziging of nieuwe dependency. De gebruiker heeft ook de aanvullende toetsenbordcontrole op build 2026.10.24.61 geaccepteerd; PR #51 is gemerged naar main 5360aed. Milestone 50 is afgerond; onderstaande stappen blijven uitvoeringshistorie.

1. **Bewijs namenindex en boekvolgorde.** Gerichte seriesrecords/repositorycontract en lichte EF-projecties met eerst falende tests. Acceptatie: juiste groepering/aantallen, alleen geselecteerde serie, exacte numerieke nummering vóór paginering, null onderaan en meer dan 200 boeken correct. Verificatie: tijdelijke SQLite-tests; circa 3–5 bestanden per increment. Bevestig de numerieke queryaanpak vóór verdere bouw.
2. **Bouw transactioneel hernoemen.** Eerst tests voor bron/doelvalidatie, volledige bevestigde ID-set, schrijfwijzecorrectie en rollback. Acceptatie: alles in de database of niets; alleen Series/UpdatedUtc verandert, nummers en duplicaatsleutels blijven behouden. Verificatie: transactionele repositorytests inclusief tussentijdse bronwijziging. Circa 3–5 bestanden; geen per-boek-databaseloop als vervanging voor de transactie.
3. **Sluit bestanden en uitkomsten aan.** Application-orkestratie na commit via bestaande BookService-route. Acceptatie: huidige metadata schrijven, één waarschuwing per probleemboek, geen onterechte volledige mislukking of automatische herhaling. Verificatie: echte metadata.json- en fout-/annuleringsgevallen in tijdelijke bibliotheek. Circa 3–5 bestanden.

Checkpoint na 1–3: geen migratie nodig, juiste namen en nummering, atomiciteit en bestandsgrenzen bewezen vóór UI-aansluiting.

4. **Bouw beheer-viewmodel.** Zoeken, paginering, selectie, bevestiging, bezetstatus, behoud van nieuwe zoeknaam en late resultaten bewaken. Acceptatie: hoofdschermvoorraad niet dupliceren en geen omslagen laden voor lijsten. Verificatie: gerichte viewmodeltests; circa 2–4 bestanden.
5. **Sluit venster en talen aan.** Werkbalkactie, zichtbare splitter, begrensde boeken-/waarschuwingslijst en bereikbare knoppen bij verkleinen. Acceptatie: toetsenbord/focus, huidige serie zichtbaar na hernoemen en afsluitende bestaande hoofdschermreconciliatie. Verificatie: layout-/resource-/integratietests en WPF-render met fictieve gegevens. Opsplitsen in venster, interacties en vertaalincrements van hoogstens circa 3–5 bestanden.
6. **Verifieer en lever op.** Volledige suite en schone Debug-build, eigen vijfassige review, README/featurestatus en handmatige checklist, exacte Obsidian-spiegels. Normale PR; gebruikersacceptatie blijft open tot de verse build getest is. Moeilijke fouten alleen automatisch in tijdelijke bibliotheken.

Afhankelijkheden: 1 vóór 2 en 4; 2 vóór 3; 3–4 vóór 5; 6 na alles. Risico's: afwijkende naamvergelijking, tekstsortering van nummers, veranderde bron tussen bevestiging en commit, en foutmeldingen na commit. Mitigatie: één vergelijking, SQLite-proof vóór paginering, transactionele hercontrole en aparte bestands-/verversingsstatus. Geen agents of nieuwe dependency nodig. De specificatiegate is met het gebruikersakkoord gepasseerd. Eigen review corrigeerde nog splittercontrast en feedback bij een weergavefout ná commit; regressietest groen.

Aanvulling na gebruikerstoets: hoofdroute akkoord volgens de eigen vinkjes; resterend focusverlies door tijdelijk uitschakelen gereproduceerd met een echte WPF-regressieproef en gericht hersteld. Eerst falende extra proef bewijst dat vertraagd herstel een nieuwere focuskeuze niet mag overnemen. 980 tests en vijf interactieve WPF-scenario's slagen; build 2026.10.24.61 van 15:35 heeft 0 waarschuwingen/fouten. Eigen review opnieuw uitgevoerd. Ook de toetsenbordhercontrole is inmiddels afgevinkt en geaccepteerd. Bestaande gebruikersvinkjes zijn behouden.

## Historie: Milestone 49 meervoudige metadatawijzigingen

Laatste status: oorspronkelijke slice en invoerverbetering zijn volledig door de gebruiker geaccepteerd. Alle vijf CodeRabbit-voorstellen zijn daarna goedgekeurd en verwerkt met gerichte regressietests: juiste beschikbaarheidsnotificaties, feedback behouden bij annuleren, eerlijke fase-afhankelijke foutmelding en twee testversterkingen. 959 tests slagen; Debug-build 2026.10.24.37 is schoon geplaatst op 24 oktober om 14:21 (Europe/Amsterdam). Alleen de korte annuleercontrole onderaan de checklist staat nog open. Historische uitvoeringsstappen hieronder blijven behouden.

Aanvulling na gebruikersacceptatie: de oorspronkelijke slice is handmatig akkoord. Op verzoek is de standaardtekstinvoer voor auteur, serie, tags en taal vereenvoudigd met automatisch aanvinken bij typen en uitvinken bij wissen, terwijl handmatig leeg aanvinken en gevuld uitvinken blijven werken. Tags is direct bewerkbaar; swap, tagmodi, custom velden en leesstatus behouden hun bestaande betekenis. 955 tests slagen en build 2026.10.24.33 is schoon geplaatst. Alleen de vier nieuwe controles onderaan de checklist staan nog open; eerdere gebruikersvinkjes blijven behouden.

Status: richting en concrete specificatie bevestigd; implementatie in geteste increments uitgevoerd. Specificatie: docs/superpowers/specs/2026-10-24-saga-milestone-49-multi-edit-results-design.md. PR #49 is gemerged en geaccepteerd; nieuwe branch vanaf actuele main 96fe288. Per-boekresultaten, eerlijke opslagfases, custom deelresultaten, selectie en zes talen zijn aangesloten. Eigen review en WPF-rendercontrole uitgevoerd; actuele verificatie in tasks/todo.md. Handmatige acceptatie van deze nieuwe slice staat open. De onderstaande stappen blijven als uitvoeringshistorie behouden.

1. **Maak de bestaande bulksemantiek afzonderlijk testbaar.** Verplaats alleen bulklogica naar gerichte partial/gerichte opdrachtmapping; expliciet Application-contract zonder Presentation-afhankelijkheid. Acceptatie: bestaande velden, tagmodi, custom parsing en swapgedrag onveranderd. Test: bestaande MultiEdit-tests plus regressies voor opdrachtmapping. Circa 3–5 bestanden per increment.
2. **Bewijs resultaten en opslagfase.** Eerst falende tests voor gemengd succes/conflict, ontbrekend boek, algemene fout en geen wijziging. Gebruik BookService en controleer opgeslagen toestand; bestandwaarschuwingen onderscheiden van niet-opgeslagen wijzigingen. Acceptatie: volgende boek wordt verwerkt en successen blijven behouden. Service/resultaat/tests in kleine increments van circa 3–5 bestanden.
3. **Bewaak custom deelresultaten.** Eerst tests voor custom-only, standaardconflict zonder custom writes, standaard succesvol/custom mislukt en fout na enkele custom velden. Acceptatie: eerlijke deels-opgeslagen uitkomst, geen automatische herhaling van oude swapopdracht en geen stil succes bij ontbrekende custom opslag. Circa 3–5 bestanden; geen nieuwe gezamenlijke transactielaag.

Checkpoint na 1–3: alle bestaande semantiek blijft, batchuitkomsten zijn betrouwbaar en gegevens-/bestandsgrenzen getest vóór nieuwe feedback-UI.

4. **Sluit reconciliatie en selectie aan.** Gerichte partial/viewmodeltests; één eindverversing, geladen omslagen behouden, falende/deels opgeslagen zichtbare boeken blijven geselecteerd zonder nieuwere context te overschrijven. Acceptatie: details, filters en zoeken tonen werkelijk opgeslagen waarden. Verificatie: MultiEdit-, selectie- en omslagtests. Circa 3–5 bestanden.
5. **Toon begrijpelijke feedback.** Korte groene succesroute; compact resultaat met uitklapbare, begrensde/gevirtualiseerde fout-/waarschuwingsdetails. Geen extra selectie-/previewvenster. Tests voor layout, aantallen en oude feedback verwijderen. Zes talen in kleine vertaalincrements. Circa 3–5 bestanden per increment.
6. **Verifieer en lever op.** Volledige tests, schone Debug-build, eigen vijfassige review, README/featurestatus/checklist en gecontroleerde Markdown-spiegels. Normale PR na bevestigde scope; gebruikersacceptatie pas na nieuwe buildtest. Gericht handmatig: alles goed, conflict naast succes, volgende poging en annuleren; moeilijke fouten automatisch in tijdelijke bibliotheek.

Afhankelijkheden: 1 vóór 2; 2 vóór 3; 2–3 vóór 4–5; 6 na alles. Risico's zijn faseverlies na commit, custom gedeeltelijke opslag en niet-idempotent omwisselen. Mitigatie: expliciete per-boekuitkomsten, herlezen, selectie-/retrygrenzen en eerst falende tests. Geen parallelle agents of nieuwe dependencies nodig. De specificatiegate is door gebruikersakkoord gepasseerd.

## Historie: auteursbeheer hernoemen

Status: aanpak goedgekeurd en onderstaande stappen uitgevoerd binnen de bestaande open PR #49. De gebruiker accepteert de slice inclusief 318 boeken, het eigen icoon, foto-validatie en vensterindeling. De twee vervolgopmerkingen van CodeRabbit zijn op 24 oktober testgestuurd verwerkt: zoeken op de nieuwe auteursnaam en metadata in begrensde groepen ophalen zonder geladen omslagen kwijt te raken. 936 tests geslaagd; Debug-build 2026.10.24.7 heeft 0 waarschuwingen/fouten. Eigen review afgerond; alleen de twee reviewcorrecties hebben nog een gerichte handmatige nacontrole, geen volledige heracceptatie. Specificatie: docs/superpowers/specs/2026-10-22-saga-author-rename-design.md. De oorspronkelijke gebruikersvinkjes blijven behouden; PR #49 is niet gemerged. Onderstaande volgorde blijft als uitvoeringshistorie behouden.

1. **Transactioneel hernoemen bewijzen.** Eerst falende repositorytests voor dezelfde auteur-id/profiel, gekoppelde boeken en bezette naam. Voeg gericht contract/resultaat en een EF-transactie toe; valideer bron/doel opnieuw en herbereken alle betrokken duplicaatsleutels. Circa 3–4 bestanden. Acceptatie: alles in de database of niets, geen auteurs samenvoegen. Verificatie: Author-repositorytests, inclusief mede-auteurs, nul boeken, pagina-overstijgend en rollback.
2. **Bestandssynchronisatie aansluiten.** Inventariseer en test eerst de gedeelde BookService/MetadataSidecarWriter-route; eventuele minimale extractie als eigen increment. Voeg daarna Application-orkestratie toe voor uitsluitend de succesvol hernoemde boeken. Maximaal circa 5 bestanden per increment. Acceptatie: geen nieuwere metadata overschrijven en opgeslagen databasewijziging niet als geheel mislukt presenteren bij een bestandwaarschuwing. Verificatie: sidecar-/servicetests, commitgrens en annulering.

Checkpoint na 1–2: hernoemen houdt identiteit, profiel en duplicaatbescherming intact; foutmeldingen onderscheiden weigering vóór opslag van een waarschuwing ná opslag. Gerichte tests groen vóór UI-aansluiting.

3. **Actie en bevestiging toevoegen.** Eigen hernoeminteractie in het auteursvenster, oude/nieuwe naam en boekaantal, bestaande conceptbewaking, bezetstatus en behoud van selectie. Gedeeld model/viewmodel plus tests in kleine increments van circa 3–5 bestanden. Acceptatie: gewone profielopslag blijft niet-hernoemend en boekdetails veranderen niet van betekenis.
4. **Actualisatie en talen afronden.** Gewijzigde boekidentiteiten aansluiten op bestaande hoofdschermreconciliatie; geen volledige boekenverzameling in het auteursviewmodel. Nieuwe teksten verdeeld over kleine vertaalincrements in zes talen. Verificatie: hoofdscherm-, viewmodel-, layout- en lokalisatietests.
5. **Opleveren.** Volledige suite, Debug-build, eigen vijfassige review, nieuwe handmatige checklist, README/featurestatus en Obsidian-spiegels. Gewone PR volgens projectworkflow; gebruikersacceptatie pas afvinken na test van de nieuwe functie.

Afhankelijkheden: 1 vóór 2 en 3, 3 vóór 4, 5 na alles. Risico's: verouderde duplicaatsleutels, identiteitsverlies via de bestaande per-boek-route en onjuiste succes/foutstatus na commit. Mitigatie: transactietests, expliciete hernoemopdracht en afzonderlijke bestandsresultaten. Geen agents of nieuwe dependency nodig. Geen schemawijziging voorzien.

## Historie: Milestone 48 voorbereiding auteursbeheer

Status: specificatie en technische keuzes door de gebruiker bevestigd; geïmplementeerd op codex/milestone-48-author-profiles vanaf actuele main 1de190f na merge van PR #48. Specificatie: docs/superpowers/specs/2026-10-22-saga-milestone-48-author-profiles-design.md. De oorspronkelijke voorbereiding is behouden. Handmatige gebruikersacceptatie staat nog open.

1. Bevestig scope, optionele profielvelden, behoud zonder boeken en fotogrenzen; leg daarna het opslagbesluit vast. Eerst migratie- en repositorytests voor bestaande auteurs en behoud van verrijkte auteurs. Kleine increments: entity/configuratie/migratie/snapshot/test, daarna beide orphan-routes en regressietests.
2. Implementeer gerichte profielcontracten en opslagservice met eerst falende tests voor identiteit, validatie, conflict en retry. Circa 3–5 bestanden per increment. Geen boekmetadata wijzigen.
3. Bouw beheerde foto-opslag met apart geteste padveiligheid en validatie; daarna coördinatie met databaseopslag. Unieke nieuwe bestanden, oude foto pas na commit opruimen. Test fout en annuleren vóór aansluiting op de interface.
4. Voeg afzonderlijk auteursviewmodel toe: doorzoeken, aantallen, selectie, boeken, concept en navigatiebewaking. Tests voor late resultaten, bibliotheekwissel en lege lijst; laad geen foto's voor de hele lijst.
5. Sluit WPF-venster, bestandskiezer, gecontroleerde browseractie en navigatie aan. Deel op in venster/layout, interacties en vertalingen zodat ieder increment controleerbaar blijft. Zes talen en toetsenbord-/splittercontrole.
6. Volledige tests, Debug-build onder Builds/Debug, eigen review op correctheid/architectuur/eenvoud/veiligheid/prestaties, handmatige checklist en README bijwerken, Markdown spiegelen. Gewone PR; gebruikersacceptatie blijft open tot de nieuwe build is getest.

Afhankelijkheden: 1 vóór 2; 2 en 3 vóór opslag-UI; 4 vóór 5; 6 na alles. Checkpoint na 1: bestaande bibliotheek ongewijzigd leesbaar en profiel verdwijnt niet bij laatste boek. Na 3: fouten behouden oude gegevens en foto. Na 5: volledige profielroute zonder online dienst. Grootste risico's zijn orphan-opruiming, database/bestand-atomiciteit en traag laden; specifieke tests en begrensde verwerking staan in de spec. Geen agents of parallelle wijzigingen nodig.

## Historie: Milestone 47 betrouwbaarheid Quality afronden

De gebruiker heeft de drie concrete reviewbevindingen goedgekeurd. Scope: docs/superpowers/specs/2026-10-22-saga-milestone-47-quality-final-reliability-design.md. Branch vanaf actuele main ff97e5a; geen nieuwe kwaliteitsfunctionaliteit.

1. Fout- en uitgestelde-selectietests voor terugzetten; gedeelde verwerking met snapshot, bezetstatus en finally. Bewaak retry, annulering en blokkering van een tweede opdracht.
2. Eerst falende dashboardtest: Dit is correct ruimt oude hersteldetails op. Hergebruik ClearRepairFeedback na bevestiging en vóór opslag.
3. Bind veilige foutmelding en CanRestoreAll in het venster, met layouttest en tekst in zes talen. Geen wijzigingen aan de bestaande bevestiging of opslaglaag.
4. Volledige tests, Debug-build, eigen review, korte handmatige checklist en Markdown-spiegel. Gewone PR; handmatige acceptatie blijft open tot de gebruiker heeft getest.

Stap 1 vormt de basis voor 3; stap 2 is een kleine onafhankelijke correctie. Na iedere geteste samenhangende wijziging committen. Risico is selectie die wijzigt tijdens await: de regressietest bewaart een andere latere selectie en vergelijkt UI met de repository. Geen echte bibliotheekwijzigingen nodig voor fouttests. Commando's en grenzen staan in de specificatie en project-DoD.

## Afgerond: Milestone 46 voorbereiding en implementatie

Status: specificatie en aanpak door gebruiker bevestigd; implementatie gestart. Specificatie: docs/superpowers/specs/2026-10-22-saga-milestone-46-repair-result-details-design.md. PR #46 is gemerged; branch codex/milestone-46-repair-result-details begint op actuele origin/main (6bee6f0). De lokale voorbereiding is behouden.

## Implementatievolgorde en checkpoints

1. **Bewijs de conflictreden bij titel/auteur.** Voeg een kleine gedeelde, getypeerde reden toe aan de bestaande reparatieresultaten en behoud BookSaveStatus.Conflict. Acceptatie: een dubbel boek blijft geblokkeerd, de reden is structureel beschikbaar en overige metadata verandert niet. Eerst rood/groen testen. Verwacht: gedeeld Application-contract, titel/auteurservice en servicetests (3 bestanden).
2. **Bouw gedeelde resultaatfeedback.** Voeg een gericht Presentation-model/helper toe en verbind de titel/auteurroute met de bestaande reconciliatie. Acceptatie: per boek een reden, aantallen consistent, successen behouden, mislukte regels geselecteerd; geen technische exceptiontekst als UI-uitleg. Verificatie: feedback- en dashboardtests, inclusief gemengd resultaat, annulering en verouderde waarden. Circa 4 bestanden: helper/model, dashboard en bijbehorende tests.
3. **Maak details zichtbaar.** Voeg begrensde uitklapbare resultaatdetails toe bij de bestaande dashboardmelding. Acceptatie: korte groene succesroute blijft, details zijn met toetsenbord bereikbaar en lange teksten nemen niet de hele boekenlijst over. Verificatie: WPF-layouttests en Nederlandse/Engelse resource- en placeholdertests. Circa 5 bestanden: view, layouttest, twee resources en lokalisatietest.

Checkpoint na 1–3: titel/auteurconflict is end-to-end begrijpelijk zonder opslaggedrag te wijzigen; gerichte tests en Debug-build schoon. Algemene feedback blijft los van het grote dashboardviewmodel; geen zes parallelle formatteerders.

4. **Sluit auteur en taal aan.** Gebruik dezelfde redenstructuur in beide services. Acceptatie: één/bulk gebruikt dezelfde feedback en een algemene onverwachte fout blijft eerlijk algemeen. Verificatie: service- en dashboardtests per route; circa 4 bestanden per klein increment, zo nodig service- en dashboardaansluiting apart.
5. **Sluit serie en tags aan.** Behoud onderscheid tussen niet meer toepasselijk, inmiddels gewijzigd, conflict en bestandwaarschuwing. Acceptatie: geen nieuwere gegevens overschrijven; rest van de selectie wordt verwerkt. Verificatie: service- en dashboardtests per route; opnieuw increments van hoogstens circa 5 bestanden.
6. **Sluit omslag aan.** Verbind de bestaande enkelvoudige download-/opslaanroute met dezelfde feedback. Acceptatie: onderscheid download-/verwerkingsfout versus opgeslagen bibliotheekwijziging met bestandswaarschuwing; geen nieuwe bulk- of zoekfunctie. Verificatie: omslagservice-, dashboard- en feedbacktests, inclusief bekende status en algemene fallback; circa 4–5 bestanden.

Checkpoint na 4–6: alle zes routes gedragen zich herkenbaar; oude feedback wordt niet aan een nieuwe categorie/actie toegeschreven. Successen, selecties, tellingen en bibliotheekupdates blijven correct. Geen opslag-, duplicaat-, heuristiek- of databasewijziging buiten de minimale redenoverdracht.

7. **Rond talen af in kleine stappen.** Eerst Duits/Frans, dan Spaans/Italiaans, met dezelfde resourcekeys en placeholders als Nederlands/Engels. Acceptatie: alle zes talen bevatten begrijpelijke, veilige uitleg. Verificatie: lokalisatietests; per stap 2 resourcebestanden plus test.
8. **Verifieer en lever op.** Volledige suite, schone Debug-build, eigen vijfassige review, README/featurestatus/checklist en Obsidian-spiegels. Gewone niet-draft PR met buildversie en tijd; alleen de gebruiker mergt. Handmatige acceptatie volgens docs/manual-tests/milestone-46-checklist.md. CodeRabbit-limieten worden niet als vervanging van de eigen review gebruikt.

## Afhankelijkheden, risico's en grenzen

Stap 1 gaat vóór feedback en UI; stappen 4–6 hergebruiken dezelfde feedback; stap 8 volgt op alles. Geen agentdelegatie of parallelle wijzigingen nodig. Beschikbare redenen en veilige onbekend-fallback eerst testen, zodat er geen schijnzekerheid uit exceptionteksten ontstaat. Ondersteun SavedWithWriteBackErrors expliciet: opgeslagen metadata mag niet als geheel mislukt worden beschreven. Behoud huidige snapshotcontrole en batchreconciliatie.

Commando's en project-DoD staan in de specificatie en docs/definition-of-done.md. De implementatie is in geteste increments uitgevoerd. Resultaatverwerking staat in een gerichte partial Results naast het bestaande dashboard, zodat alle routes dezelfde redenopbouw en reconciliatie gebruiken. Tijdens de eigen review zijn late categoriewissels en onverwachte leesfouten bij auteur/serie met eerst falende regressietests afgevangen; de detailijst is gevirtualiseerd. Definitieve test-/buildgegevens staan in tasks/todo.md en de handmatige checklist.

## Historie: Milestone 45 implementatieplan, bulk titel/auteur omwisselen

Goedgekeurde scope en details: docs/superpowers/specs/2026-10-22-saga-milestone-45-bulk-title-author-swap-design.md.

1. Gebruik de bestaande lijstselectie en snapshotcontrole; test dat alleen geselecteerde boeken worden verwerkt.
2. Dashboard verwerkt geschikte geselecteerde regels; test annuleren, gedeeltelijke fouten, overgeslagen boeken en verversing.
3. Korte Ja/Nee-bevestiging met aantal in zes talen; bestaand enkelvoudig venster behouden.
4. Volledige tests, eigen review, checklist, Obsidian-spiegel, gewone PR en controles.

Afhankelijkheden: model vóór dashboard/UI, alles vóór oplevering. De gebruiker bepaalt de selectie in de lijst; een tweede selectie is na gebruikerstest vervallen. Opslag vergelijkt actuele titel/auteur met de oorspronkelijke waarden. Andere metadata wordt door de bestaande service behouden. Geen databasewijziging of dependency.

## Aanvulling binnen Milestone 45: minder onterechte omwisselmeldingen

De gebruiker heeft expliciet gevraagd deze verbetering in dezelfde slice mee te nemen. Eerst de 32 correcte praktijkgevallen en de bevestigde echte omwisseling als regressietests opnemen, dan naamvormen ondersteunen en positieve boektitelaanwijzingen eisen. Daarna veilige opslag en dashboardregressies controleren, volledige tests en Debug-build uitvoeren, zelf reviewen en documentatie/Obsidian/PR bijwerken. De bulkactie blijft geaccepteerd; alleen de aanvullende detectie vraagt nieuwe handmatige controle. Geen metadata-, opslag-, UI- of dependencywijziging.

## Afgerond plan Milestone 44

De gebruiker heeft opdracht gegeven deze slice uit te voeren. Specificatie: docs/superpowers/specs/2026-10-22-saga-milestone-44-bulk-tag-cleanup-design.md.

1. Afzonderlijke previews met oorspronkelijke tags en eigen opgeschoonde tags; tests voor behoud en lege uitkomst.
2. Dashboardselectie en bestaande service aansluiten; oorspronkelijke tags controleren en fouten per boek afhandelen. Hergebruik bestaande batchreconciliatie.
3. Scrollbaar overzicht met expliciete bevestiging en zes vertalingen; gericht testcheckpoint.
4. Volledige tests, Debug-build, eigen review, documentatie, Obsidian en gewone PR.

Stap 2 volgt op 1; de interface volgt op het model; oplevering volgt op alle stappen. Grootste risico is taglijsten vermengen: aparte voorstellen en opslagtests voorkomen dat. Verouderde tags worden vóór schrijven vergeleken. Geen nieuwe opslagroute of dependency.

## Afgerond plan Milestone 43

## Overzicht

Milestone 43 breidt het bestaande enkelvoudige serieherstel uit naar één of meerdere geselecteerde boeken. De bestaande service kan al een verzameling boek-id’s veilig verwerken; de belangrijkste wijzigingen liggen daarom in bulkselectie, dialoogcontext, hergebruik van generieke batchreconciliatie, lokalisatie en testbewijs. De afgesproken Splash-uitlijning wordt vooraf als een afzonderlijke kleine UI-commit uitgevoerd.

## Architectuurbeslissingen

- `MetadataQualitySeriesRepairService` blijft de enige opslagroute voor serieherstel.
- `ReconcileBatchRepair` verwerkt ook serieresultaten; er komt geen derde vrijwel gelijke resultaatverwerker.
- `MetadataQualitySeriesRepairViewModel` krijgt dezelfde één-/meerdere-boekencontext als de auteur- en taalvensters.
- Bestaande serienummers worden uitsluitend als context getoond en nooit gewijzigd.
- De Splash gebruikt een Grid met twee automatische kolommen en gecentreerde kinderen; optische positionering gebeurt alleen op de container.
- Geen databasewijziging, dependency of nieuwe opslagroute.

## Taak 1: robuuste Splash-uitlijning

**Beschrijving:** Vervang de horizontale titel-StackPanel door een Grid dat titel en subtitel onafhankelijk van lettergrootte en DPI verticaal centreert. Behoud de bestaande uitstraling en algemene positie.

**Acceptatiecriteria:**
- [x] Titel en subtitel staan in afzonderlijke automatische Grid-kolommen.
- [x] Beide teksten gebruiken `VerticalAlignment="Center"`.
- [x] De kinderen hebben geen verticale correctiemarges; alleen de titelgroep bepaalt haar schermpositie.

**Verificatie:** Eerst een falende statische Splash-layouttest, daarna gerichte groene test en Debug-build.

**Waarschijnlijk geraakte bestanden:**
- `src/EbookManager.App/Views/Splash.xaml`
- nieuwe of bestaande layouttest onder `tests/EbookManager.Tests/App/Views`.

**Omvang:** Klein.

## Taak 2: serievenster geschikt maken voor één of meerdere boeken

**Beschrijving:** Breid het serieviewmodel en venster uit met context voor één titel/serienummer of een bulkselectie. Behoud de snelle bestaande suggestielijst en vrije invoer.

**Acceptatiecriteria:**
- [x] Eén boek toont ongewijzigd titel en serienummer.
- [x] Meerdere boeken tonen het aantal, de behoudmelding en een bevestigingsknop met het aantal.
- [x] Bekende series en vrije invoer werken even vlot als bij één boek.
- [x] Alle nieuwe teksten bestaan in zes talen en kunnen omlopen.

**Verificatie:** Rode en groene viewmodel-, layout- en lokalisatietests.

**Waarschijnlijk geraakte bestanden:**
- `src/EbookManager.Presentation/ViewModels/MetadataQualitySeriesRepairViewModel.cs`
- `src/EbookManager.App/Views/MetadataQualitySeriesRepairWindow.xaml`
- `src/EbookManager.App/Resources/Strings/AppResources*.resx`
- bijbehorende tests.

**Omvang:** Middelgroot; zes resources vormen één mechanische set.

## Checkpoint na taken 1-2

- [x] Splash-layouttest en serievenstertests zijn groen.
- [x] Enkelvoudige seriecontext is niet gewijzigd.
- [x] Geen productlogica of opslagroute is gedupliceerd.

## Taak 3: bulkselectie en resultaatverwerking aansluiten

**Beschrijving:** Maak `Serie wijzigen` beschikbaar voor iedere geldige selectie binnen de seriecategorie en verwerk de bestaande servicebatch via de generieke reconciliatie.

**Acceptatiecriteria:**
- [x] Eén of meerdere geselecteerde serierijen activeren de actie.
- [x] Annuleren schrijft niets; bevestigen stuurt alle unieke geselecteerde ids en één serienaam.
- [x] Succes, write-backwaarschuwing, verouderd, verwijderd en mislukt worden per boek correct verwerkt.
- [x] Volledig mislukte boeken blijven zichtbaar en geselecteerd; volledig succes geeft een korte groene melding.

**Verificatie:** Eerst falende dashboardtests voor beschikbaarheid, annuleren, succes en gemengde resultaten; daarna groen met minimale productcode.

**Waarschijnlijk geraakte bestanden:**
- `src/EbookManager.Presentation/ViewModels/MetadataQualityDashboardViewModel.cs`
- `tests/EbookManager.Tests/App/ViewModels/MetadataQualityDashboardSeriesRepairTests.cs`
- lokalisatieresources en tests.

**Omvang:** Middelgroot.

## Taak 4: gegevensbehoud en bibliotheekverversing bewijzen

**Beschrijving:** Bewijs dat verschillende serienummers behouden blijven, inmiddels ingevulde series niet worden overschreven en de hoofdbibliotheek plus seriefilters éénmaal verversen.

**Acceptatiecriteria:**
- [x] Meerdere boeken krijgen dezelfde serie en behouden ieder hun oorspronkelijke serienummer.
- [x] Een inmiddels geldige serienaam levert `NotApplicable` op zonder write.
- [x] De bibliotheekregels en seriefilters tonen direct de actuele waarden.
- [x] Overige metadata blijft intact.

**Verificatie:** Gerichte service- en LibraryViewModel-tests, gevolgd door een gericht checkpoint.

**Waarschijnlijk geraakte bestanden:**
- `tests/EbookManager.Tests/Metadata/MetadataQualitySeriesRepairServiceTests.cs`
- `tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs`
- alleen bij bewezen gebrek minimale productcode.

**Omvang:** Klein tot middelgroot.

## Checkpoint na taken 3-4

- [x] Alle gerichte bulkserietests zijn groen.
- [x] Enkelvoudig herstel en bestaande bulkacties blijven groen.
- [x] Opslag, dashboard en bibliotheek tonen dezelfde toestand.

## Taak 5: documentatie, build en oplevering

**Beschrijving:** Werk status en checklist bij, spiegel Markdown, voer de volledige Definition of Done uit en open een gewone niet-draft PR.

**Acceptatiecriteria:**
- [x] README en quality-roadmap beschrijven Milestone 43 zonder vervolgwensen als gerealiseerd te presenteren.
- [x] De handmatige checklist behandelt Splash, selectie, suggesties, verschillende nummers, annuleren, resultaten, filters en herstart.
- [x] Volledige tests en schone Debug-build slagen zonder waarschuwingen of overgeslagen tests.
- [x] De complete diff is zelf beoordeeld en de branch bevat alleen Milestone 43.
- [x] Alle gewijzigde Markdown heeft gelijke hashes in de Obsidian-spiegel.

**Verificatie:** Volledige tests, clean/build, `git diff --check`, hashcontrole, eigen codereview en GitHub-controles in PR #44.

**Omvang:** Middelgroot, hoofdzakelijk verificatie en documentatie.

## Risico's en maatregelen

| Risico | Impact | Maatregel |
|---|---|---|
| Verschillende serienummers worden gelijkgetrokken | Hoog | Service- en acceptatietest met afwijkende nummers; serie-index nooit als input meegeven |
| Inmiddels ingevulde serie wordt overschreven | Hoog | Bestaande `Applies`-controle per boek behouden en expliciet testen |
| Derde batchverwerker wijkt af van auteur/taal | Middel | Seriestatus naar bestaande `MetadataQualityBatchRepairItem` mappen |
| Bulkvenster verliest snel typen of suggestiekeuze | Middel | Bestaande inputevents behouden en regressietests plus handmatige controle |
| Splash verschuift bij DPI of vertaling | Laag | Layoutuitlijning in Grid; geen verticale kindmarges |
| Langere vertalingen worden afgekapt | Middel | Tekst laten omlopen en layouttest uitbreiden |

## Goedkeuringscheckpoint

- [x] Hoofdfunctionaliteit en robuuste Splash-aanpak door gebruiker goedgekeurd.
- [x] Volledige specificatie en implementatieplan door gebruiker bevestigd.
- [x] Implementatie testgestuurd en incrementeel afgerond.
- [x] Definition of Done en gewone PR afgerond; alle automatische en handmatige controles zijn geslaagd.
