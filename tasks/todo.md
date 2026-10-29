# Milestone 52: tags beheren

- [x] Scope door gebruiker bevestigd op 29 oktober; PR #53 gemerged en basis main e8e024c. Branch codex/milestone-52-tag-management.
- [x] Auteursvariantenwens afzonderlijk voor later vastgelegd; bestaande auteursbeheer-/hernoemregels blijven ongewijzigd.
- [x] Specificatie en geordende uitvoering met testcheckpoints vastgelegd; bestaande tagidentiteit/koppelingen en opslagpatronen hergebruiken.
- [x] Lichte SQL-zoek-/boekpagina's en transactioneel hernoemen met ID-setcontrole, naamvalidatie, bestaande doelnaam, rollback en behoud van overige tags/volgorde. Eerst falende tests groen; 510 boeken en Unicode-paginering gecontroleerd.
- [x] Bestaande metadata.json-/ebook-synchronisatie na commit, aparte bestandswaarschuwingen en annulering afgedekt met eerst falende serviceproeven en echte tijdelijke bestanden.
- [x] Viewmodel met zoeken/paginering, selectie/annuleren, nieuwe naam behouden, foutfeedback en blokkeren tijdens opslaan. Extra stale-preview- en refresh-na-commit-regressies opgelost.
- [x] Tagbeheervenster, hernoemdialoog, werkbalkicoon en zes talen aangesloten; herkenbare splitter en begrensde lijsten. Echte WPF-focusproeven en licht/donker bij minimale grootte gecontroleerd.
- [x] Volledige suite: 1047 geslaagd, 0 mislukt/overgeslagen. Negentien interactieve WPF-scenario's groen. Debug-build 2026.10.29.40 van 29 oktober 14:20:14 (Europe/Amsterdam), 0 waarschuwingen/fouten en één Saga.exe onder Builds. Eigen review op correctheid, eenvoud, architectuur, veiligheid en prestaties afgerond.
- [x] Nieuwe checklist, featurestatus, README, specificatie en taken bijgewerkt; Markdown wordt exact naar Obsidian gespiegeld en met gelijke hashes gecontroleerd.
- [x] Gebruikersfeedback verwerkt: expliciete boekselectie met telling/zichtbare markering, Tag wijzigen, Tag verwijderen en afzonderlijk Tag overal hernoemen. Bronboeklijst en bron-/doelaantallen opnieuw laden; bestaande doeltag per boek dedupliceren met behoud van overige tags en niet-geselecteerde boeken. Eerst falende tests en echte WPF-acties/paginering groen; oorspronkelijke gebruikersvinkjes en niet-uitgelokte bestandswaarschuwing behouden.
- [ ] Gebruiker voert docs/manual-tests/milestone-52-checklist.md uit en accepteert de slice; alleen de gebruiker mergt de gewone niet-draft PR.
## Historie: Milestone 51 — geaccepteerd en gemerged
# Milestone 51: meervoudige boekenkastselectie

- [x] Aanvullende CodeRabbit-focusbevinding voor dubbele tagvermeldingen eerst falend gereproduceerd en hersteld: boek én doelgroep blijven samen tot focus/scroll in de juiste bladlijst. ID-selectie blijft uniek. 993 tests en vijftien WPF-scenario's groen met tijdelijke geïsoleerde build 2026.10.29.8 van 29 oktober 08:57. Verouderde actuele cijfers in specificatie/plan expliciet historisch gemarkeerd; eigen vijfassige review uitgevoerd.
- [x] Na afsluiten van Saga normale Debug-build 2026.10.29.9 geplaatst op 29 oktober om 08:59:31 (Europe/Amsterdam), 0 waarschuwingen/fouten en één Saga.exe onder Builds. Alle vijftien WPF-scenario's slagen ook tegen die build; 993 gewone tests groen. Eerdere acceptatie blijft behouden.
- [x] Gebruiker bevestigt op 29 oktober ook de resterende controlepunten, inclusief focus bij hetzelfde boek in twee taggroepen. Volledige checklist akkoord; eigen review zonder aanvullende bevindingen, CodeQL en GitGuardian groen. CodeRabbit-limiet blokkeert afronding niet; laatste fix is door eigen review en regressietests afgedekt.

- [x] CodeRabbit-opmerking 4389990878 onderzocht en eerst falend bewezen: gedeeltelijke groepsrijen verschoven Up/Down naar het verkeerde boek. Groepslokale navigatie met expliciete groepsovergangen hersteld; globale unieke Shift-volgorde behouden. 993 tests en veertien WPF-scenario's slagen. Debug-build 2026.10.29.5 van 29 oktober 08:15, 0 waarschuwingen/fouten, één Saga.exe. Eigen vijfassige review uitgevoerd.
- [x] Hercontrole voor gedeeltelijke groepsrijen/Shift en overige detailchecks op 29 oktober door gebruiker getest en akkoord. Alleen de gebruiker mergt PR #53.

- [x] Gebruikersacceptatie op 27 oktober 2026: meervoudige selectie én metadata bewerken werken in alle drie de weergaven zoals verwacht. De gemelde groepsselectiefout in Lijst/Gedetailleerd is akkoord. Niet afzonderlijk bevestigde detailcontroles blijven zichtbaar; PR #53 staat nog open en wordt uitsluitend door de gebruiker gemerged.

- [x] Gebruiker keurt het voorstel goed en geeft opdracht te bouwen: Ctrl/Shift, duidelijke selectie/telling, bestaande bulkactie, toetsenbord, groepen/filters/views.
- [x] Bestaande tegeltemplates, groepsduplicaten, virtualisatie, centrale selectie en grid-/bulkroute onderzocht; concrete spec en plan vastgelegd.
- [x] Gerichte selectieregels en ID-/rijsynchronisatie gebouwd. Eerst falende tests voor actuele rijreferenties bij dezelfde ID en selectiestaat; Ctrl/Shift, unieke groeps-IDs, filterdoorsnede, view-/bibliotheekwissel en annuleren getest.
- [x] Gewone/gegroepeerde tegels met geselecteerde rand en afzonderlijke focusrand, centrale selectie en muisinteracties aangesloten. Gesloten groepen worden niet door een nieuwe reeks meegenomen; dubbele vermeldingen delen één staat.
- [x] Pijltjes/Home/End/Spatie/Escape en virtualisatie op echte WPF-schermen bewezen. Ver weg gelegen groepstegel eerst falend getest en correct gerealiseerd; nieuwere tekstfocus heeft voorrang op uitgesteld navigatiefocus.
- [x] Expliciete synchronisatie voor actieve grids en centrale selectie. Heen/terug wisselen en native gridselectie getest. Bestaande bulkroute wijzigt alleen geselecteerde boeken; na succes verdwijnen selectieranden en annuleren behoudt selectie.
- [x] Selectietelling, bestaande metadata-actie en gelokaliseerde uitleg in zes talen aangesloten; resourcetests en visuele WPF-controle uitgevoerd. Tegelmarges houden beide nieuwe randen binnen de bestaande tegelmaat.
- [x] Volledige suite: 993 geslaagd, 0 mislukt/overgeslagen. Tien interactieve WPF-scenario's groen. Debug-build 2026.10.27.23 op 27 oktober 09:17, 0 waarschuwingen/fouten en één Saga.exe in Builds/Debug. Eigen review op correctheid, eenvoud, architectuur, veiligheid en prestaties; nieuwe checklist/featurestatus/README opgesteld. Geen productlicentie- of opslagwijziging; de proef hergebruikt de bestaande lokale licentieresolver.
- [x] Alle tien gewijzigde project-Markdown-bestanden exact naar Obsidian gespiegeld met gelijke hashes. Gewone niet-draft PR #53 geopend en gekoppeld: https://github.com/hnsoftwaredevelopment/saga/pull/53. Mergeable bij opening; CodeQL en GitGuardian zijn nog bezig.
- [x] Handmatige acceptatie volledig bevestigd op 29 oktober, inclusief de resterende groeps-/weergave-/filterpunten en reviewcorrecties op build 2026.10.29.9. PR #53 staat open; alleen de gebruiker mergt.

- [x] Gebruikersmelding gegroepeerde Lijst/Gedetailleerd opgelost: standaard enkelvoudige bladlijsten en de oude altijd-vervangende rijhandler eerst falend bewezen via echte WPF-muisinput. Gedeelde preview-inputhelper en selectiestijl ondersteunen Ctrl/Shift, groepsgrenzen, toetsenbord, dubbele tags en nieuwere tekstfocus. Iedere view behoudt haar groepering en gedeelde geselecteerde IDs. 993 gewone tests en dertien WPF-scenario's groen; Debug-build 2026.10.27.34 van 27 oktober 16:03 heeft 0 waarschuwingen/fouten en één Saga.exe. Eigen vijfassige review en visuele controle uitgevoerd; nieuwe gebruikershercontrole blijft open.

- [x] CodeRabbit-review: werkelijke paneelkolommen gebruiken voor Pijl-omhoog/-omlaag, eerst falend bewezen met echte tegelposities op smalle breedte. Vier layoutvarianten slagen; totaal elf WPF-scenario's en 993 gewone tests groen. Debug-build 2026.10.27.29 van 27 oktober 15:25 heeft 0 waarschuwingen/fouten en één Saga.exe. Eigen vijfassige review herhaald. Acceptatiestatus gelijkgetrokken zonder open gebruikersvinkjes te veranderen; plannummering hersteld. PR #52 gemerged geverifieerd en actuele main ea65412 geïntegreerd.

- [x] Gebruiker meldt geslaagde tests en vraagt vervolgens de dubbele bewerkingsknop te verwijderen. Lokale knop en overbodige WrapPanel verwijderd; telling/uitleg blijven. De bestaande hoofdwerkbalk is de enige bewerkingsactie. Eigen checklistvinkjes behouden. 993 tests opnieuw groen; Debug-build 2026.10.27.25 van 27 oktober 11:43, 0 waarschuwingen/fouten en één Saga.exe. Eigen review: alleen visuele vereenvoudiging, geen selectie-/opslagwijziging. Gebruiker heeft ook deze visuele controle afgevinkt. De overige eigen vinkjes blijven behouden; vier groeps-/weergave-/filterpunten staan nog niet afgevinkt.

## Historie: Milestone 50

# Milestone 50: series beheren

- [x] Gebruiker vindt seriesbeheer de gewenste volgende stap: zoeken, aantallen, boeken op serienummer en veilig hernoemen zonder samenvoegen.
- [x] PR #50 geverifieerd gemerged naar main f8ad6fa; laatste annuleercontrole geaccepteerd. Nieuwe codex/milestone-50-series-management gestart vanaf bijgewerkte main; geen bestaande wijzigingen aanwezig.
- [x] Bestaand Series/SeriesNumber-model, seriefiltervergelijking, lichte projecties, auteursbeheer en hoofdschermreconciliatie onderzocht; concrete spec, featurebeschrijving en geordend plan voorbereid.
- [x] Concrete keuzes bevestigd: bestaande naamgroepering, numerieke volgorde met ontbrekende nummers onderaan, alleen hernoemen en geen conversie.
- [x] Lichte namen-/boekenprojecties: eerst falende SQLite-test, daarna exacte decimalen vóór paginering; 210 boeken zonder verlies/doublures en Unicode-/schrijfvariantgroepering bewezen. Alleen namen/aantallen en sleutels van geselecteerde serie; auteurs per geladen pagina, geen omslagen.
- [x] Transactioneel hernoemen testgestuurd gebouwd: bron/doel en volledige ID-/bronwaardenset hercontroleren, casecorrectie toestaan, 510 boeken en rollback bij latere updatefout. Alle overige scalairen, auteursvolgorde, tags, bestanden en custom waarden behouden; annulering vóór opslag en gewijzigde boekset afgedekt.
- [x] Bestaande bestandssynchronisatie na commit aangesloten: echte metadata.json, bestandswaarschuwingen en afmaken van alle boeken bij annulering ná commit getest; geen nieuwe writer of folderverplaatsingen.
- [x] Afzonderlijk viewmodel, werkbalkactie, modaal venster/bevestiging en zes talen gebouwd. Nieuwe zoeknaam/selectie, tweede opdracht blokkeren, late resultaten en aparte foutmelding ná commit afgedekt. WPF-render op 1040×650 en 800×480 gecontroleerd; splittercontrast gecorrigeerd.
- [x] Volledige suite: 980 geslaagd, 0 mislukt/overgeslagen. Debug-build 2026.10.24.52 op 24 oktober 15:01 (Europe/Amsterdam), 0 waarschuwingen/fouten en precies één Saga.exe in Builds/Debug. Eigen review op correctheid, eenvoud, architectuur, veiligheid en prestaties; README, featurestatus, ontwerp en nieuwe checklist bijgewerkt.
- [x] Alle acht nieuwe/gewijzigde project-Markdown-bestanden exact naar Obsidian gespiegeld met gelijke SHA256-hashes. Gewone niet-draft PR #51 geopend en gekoppeld: https://github.com/hnsoftwaredevelopment/saga/pull/51. Mergeable; CodeQL is bij opening nog bezig.
- [x] Gebruiker heeft de hoofdroute afgevinkt; eigen vinkjes behouden. De ene CodeRabbit-codebevinding is geldig: IsBusy schakelt lijst/knop uit en verliest focus. Echt WPF-venster eerst falend getest, daarna focus bewaren/herstellen met actieve-venster-, zichtbaarheids-, opdrachtversie- en nieuwere-focusbewaking; dialoog en aanroepende knop behouden focus. Algemene docstring-drempel is geen aanvullende projecteis.
- [x] Aanvullende verificatie: 980 gewone tests en vijf interactieve WPF-focusscenario's groen, inclusief eerst falende regressie voor vertraagd herstel dat een nieuwere focuskeuze overschrijft. Debug-build 2026.10.24.61 van 24 oktober 15:35, 0 waarschuwingen/fouten en één Saga.exe. Eigen vijfassige review; opslag, schema en overige schermen ongewijzigd. Los WPF-testprogramma zonder nieuwe pakketten, bewust buiten de gewone suite wegens vereiste interactieve desktop.
- [x] Gebruiker accepteert ook de toetsenbordhercontrole op build 2026.10.24.61; alle eigen checklistvinkjes behouden. PR #51 is door de gebruiker gemerged (main 5360aed), met CodeQL, CodeRabbit en GitGuardian groen. Milestone 50 volledig afgerond; eindstatus vastgelegd zonder nieuwe productcode of build.

## Historie: Milestone 49 meervoudige metadatawijzigingen

- [x] Gebruiker accepteert laatste PR #49-tests en meldt merge; GitHub bevestigt MERGED, main 96fe288 opgehaald en nieuwe codex/-branch gemaakt.
- [x] Richting akkoord: korte succesmelding, begrijpelijke per-boekfouten, successen behouden, mislukte selectie beschikbaar voor een volgende bewuste poging.
- [x] Bestaande bulkroute, custom opslag, BookService en Quality-resultaatverwerking onderzocht; concrete specificatie en geordend plan vastgelegd.
- [x] Concrete uitwerking bevestigd, inclusief deels opgeslagen/custom wijzigingen en geen automatische herhaling van een oude swapopdracht.
- [x] Bestaande bulksemantiek naar gerichte partial verplaatst; negen bestaande tests groen. Logica hoort nu bij een gerichte Application-opdracht en transformatie, zonder WPF-/Presentation-afhankelijkheid.
- [x] Getypeerde resultaten en veilige standaard-/bestandsverwerking aangesloten; eerst falende regressie voor algemene opslagfout, behoud van succes en mislukte selectie. Tijdelijke SQLite-tests voor conflict, ontbrekend boek, echte metadata.json en bestandsfout na commit.
- [x] Custom deelresultaten en foutgrenzen: custom-only/ongewijzigd, behoud van eerder opgeslagen velden, veilige veldnamen bij fouten en geen stil succes zonder custom-opslag. Fout ná commit en ontbrekende bevestiging afgedekt; swap nooit herhaald.
- [x] Eenmalige reconciliatie, selectieversie, tweede opdracht blokkeren, bibliotheekwissel blokkeren, oude feedback opruimen en resultaatfeedback in zes talen. Begrensde/gevirtualiseerde details en visuele WPF-controle met fictieve gegevens.
- [x] Zes talen en volledige testset: 949 geslaagd, 0 mislukt/overgeslagen. Definitieve Debug-build 2026.10.24.29 op 24 oktober om 13:31 (Europe/Amsterdam), 0 waarschuwingen/fouten en één Saga.exe onder Builds/Debug. Tijdelijke bestandsvergrendeling opgelost na afsluiten Saga; daarna opnieuw schoon gebouwd.
- [x] Eigen vijfassige review, echte metadata.json-controle, fout vóór/na commit, annulering, selectie en WPF-render met fictieve gegevens. Geen nieuw schema, afhankelijkheid, generiek opslagcontract of online bron. Een themapadprobleem in het tijdelijke preview-harnas is daar geïsoleerd; geen afzonderlijke product-themafout vastgesteld.
- [x] README, featurestatus, ontwerp en handmatige checklist bijgewerkt; alle gewijzigde Markdown exact naar Obsidian gespiegeld met gelijke SHA256-hashes. Gewone niet-draft PR #50 geopend en gekoppeld: https://github.com/hnsoftwaredevelopment/saga/pull/50. Mergeable; GitGuardian groen bij opening, CodeQL en CodeRabbit nog bezig.
- [x] Gebruiker heeft alle oorspronkelijke tests op build 2026.10.24.29 uitgevoerd en geaccepteerd; eigen checklistvinkjes behouden.
- [x] Aanvullende invoerverbetering goedgekeurd en testgestuurd gebouwd: auteur/serie/tags/taal direct typen met automatisch aanvinken; wissen vinkt uit; handmatig leeg aanvinken en gevuld uitsluiten blijven mogelijk. Tags-container niet langer geblokkeerd. Swap, tagmodi, custom en leesstatus ongewijzigd; uitleg in zes talen.
- [x] Aanvullende verificatie: 955 tests geslaagd, 0 mislukt/overgeslagen; Debug-build 2026.10.24.33 op 24 oktober om 13:52 (Europe/Amsterdam), 0 waarschuwingen/fouten. Eigen review op correctheid, eenvoud, architectuur, veiligheid en prestaties; geen opslag- of contractwijziging.
- [x] Gebruiker accepteert ook alle vier aanvullende invoercontroles; eigen checklistvinkjes behouden.
- [x] Alle vijf CodeRabbit-voorstellen gecontroleerd, goedgekeurd en verwerkt: beschikbaarheidsnotificaties, behoud van feedback bij annuleren, fase-afhankelijke foutmelding, begrensde testwachttijd en expliciet controleren dat technische fouttekst niet wordt getoond. Drie eerst falende regressietests plus aanvullende test voor onverwachte batchfout na eerdere commit.
- [x] Reviewverificatie: 959 tests geslaagd, 0 mislukt/overgeslagen; Debug-build 2026.10.24.37 op 24 oktober om 14:21 (Europe/Amsterdam), 0 waarschuwingen/fouten. Eigen vijfassige review; geen nieuwe interface, afhankelijkheid, schemawijziging, vertaling of opslaggedrag.
- [x] Gebruiker bevestigt dat resultaatmelding blijft staan na annuleren en heeft PR #50 gemerged; Milestone 49 volledig geaccepteerd.

## Historie: auteursbeheer hernoemen

- [x] Gebruiker bevestigt: hernoemen werkt voor de bestaande auteur en alle gekoppelde boeken; boekdetails blijven alleen de auteur van dat boek wijzigen.
- [x] Gebruiker kiest expliciet geen samenvoegen bij een bestaande doelnaam; dan verwijzen naar aanpassen via boekdetails. Twee personen met dezelfde naam onderscheiden valt buiten scope.
- [x] Technische inventarisatie en voorstel vastgelegd; oorspronkelijke gebruikersvinkjes behouden.
- [x] Technische aanpak met transactionele databasewijziging en afzonderlijke bestandssynchronisatie bevestigd.
- [x] PR #49 is open; bestaande branch vanaf main 1de190f uitgebreid zonder merge te veronderstellen.
- [x] Identiteitsbehoudend hernoemen, unieke naam en duplicaatsleutels testgestuurd gebouwd.
- [x] Bestandsverwerking met veilige resultaatstatus en commit-/annuleringsgrenzen aangesloten.
- [x] Hernoemactie, bevestiging, selectie, hoofdschermactualisatie en zes talen gebouwd/getest; eigen SVG van gebruiker verwerkt.
- [x] Definitief: 933 tests geslaagd, 0 mislukt/overgeslagen; Debug-build 2026.10.22.109, 22 oktober 2026 om 18:09 (Europe/Amsterdam), 0 waarschuwingen/fouten. Eigen vijfassige review uitgevoerd; regressies voor oude waarschuwingen en paginering bij 205 boeken afgedekt. Werkbalkicoon en kleine-vensterindeling uit CodeRabbit-review verwerkt.
- [x] Gebruiker bevestigt: hernoemen werkt goed en het icoon ziet er goed uit.
- [x] README, featurestatus, ontwerp en beide checklists bijgewerkt; gebruikersvinkjes behouden. Alle gewijzigde Markdown exact naar Obsidian gespiegeld en SHA256-hashes gelijk.
- [x] Gebruiker accepteert de volledige slice: ook auteur met 318 boeken, behoud van andere auteurs, weigeren van onbruikbare foto's met behoud van oude foto en venster verkleinen/vergroten met alle knoppen bereikbaar.
- [x] CodeRabbit-vervolgopmerkingen inhoudelijk bevestigd en op verzoek verwerkt: nieuwe auteursnaam als zoekterm behouden; metadata in begrensde groepen lezen met één context, zonder lijst-omslagblobs en met behoud van reeds geladen omslagen. Eerst falende regressietests en echte SQLite-test over 501 boeken; ontbrekende en dubbele ID's afgedekt.
- [x] Verificatie reviewcorrecties op 24 oktober: 936 tests geslaagd, 0 mislukt/overgeslagen; Debug-build 2026.10.24.7 om 12:50 (Europe/Amsterdam), 0 waarschuwingen/fouten en precies één Saga.exe onder Builds/Debug. Eigen vijfassige review uitgevoerd; geen nieuwe afhankelijkheid, schemawijziging, layout of vertalingen.
- [x] Laatste reviewcorrecties handmatig door gebruiker geaccepteerd; PR #49 gemerged naar main 96fe288.

## Historie: Milestone 48 auteursbeheer voorbereiden

- [x] Gebruiker kiest auteursbeheer met foto en optionele website; voorbereiding akkoord.
- [x] Bestaande Authors/BookAuthors, profielwens, foto-opslag en beide orphan-opruimroutes onderzocht.
- [x] Conceptspecificatie en geordende technische aanpak vastgelegd; geen productcode, migratie of build gewijzigd.
- [x] Concrete scope en technische keuzes door gebruiker bevestigd.
- [x] Actuele main 1de190f gecontroleerd, codex/milestone-48-author-profiles gemaakt en geaccepteerd opslagbesluit vastgelegd.
- [x] Additieve profielvelden, migratie en behoud zonder boeken testgestuurd ingevoerd; beide orphan-routes afgedekt.
- [x] Profielservice, validatie, veilige foto-opslag en fout-/conflictgedrag getest, inclusief onzeker commitresultaat en annulering.
- [x] Doorzoekbaar auteursvenster, gepagineerde boekenlijst, websiteactie en zes talen gebouwd; WPF-render en echte beeldverwerking gecontroleerd met fictieve gegevens.
- [x] Volledige testset: 911 geslaagd, 0 mislukt of overgeslagen; Debug-build 2026.10.22.88, 22 oktober 2026 om 15:38 (Europe/Amsterdam), 0 waarschuwingen/fouten, precies één Saga.exe onder Builds/Debug. Eigen vijfassige review, README en checklist afgerond; Markdown exact naar Obsidian gespiegeld en hashes gecontroleerd.
- [x] Gewone niet-draft PR #49 geopend en gekoppeld: https://github.com/hnsoftwaredevelopment/saga/pull/49. Mergeable; GitGuardian groen bij openingscontrole. Overige review-/CI-status wordt afzonderlijk beoordeeld zodra beschikbaar.
- [x] Handmatige gebruikersacceptatie volgens docs/manual-tests/milestone-48-checklist.md; gebruikersvinkjes behouden.

## Historie: Milestone 47 betrouwbaarheid Quality afronden

- [x] Gebruiker geeft opdracht voor de drie reviewbevindingen; branch vanaf actuele main ff97e5a gemaakt.
- [x] Afgesproken scope en aanpak vastgelegd; eerst falende tests voor foutstatus, oude details, UI-binding en vertalingen.
- [x] Gedeelde veilige terugzetverwerking, selectiesnapshot, retry, annulering en blokkering van tweede opdracht getest.
- [x] Dit is correct ruimt oude hersteldetails op, ook als markeren zelf mislukt.
- [x] Foutmelding en beschikbaarheid in venster aangesloten, tekst in zes talen en layout-/lokalisatietests toegevoegd.
- [x] Volledige suite: 888 tests geslaagd, 0 mislukt of overgeslagen. Debug-build 2026.10.22.65 op 22 oktober 2026 om 14:34 (Europe/Amsterdam), 0 waarschuwingen/fouten en precies één Saga.exe onder Builds/Debug. Eigen review uitgevoerd; efficiënt alles terugzetten met een extra eerst falende regressietest bewaakt.
- [x] Gewijzigde Markdown exact naar Obsidian gespiegeld en hashes gecontroleerd; gewone niet-draft PR #48 geopend en gekoppeld: https://github.com/hnsoftwaredevelopment/saga/pull/48.
- [x] CodeQL, GitGuardian en CodeRabbit-status groen op cbc51a9; PR mergeable. Bij controle nog geen CodeRabbit-reviewbericht of inline bevindingen aanwezig; eigen review is uitgevoerd.
- [x] Overige handmatige controles door gebruiker goedgekeurd; herstelconflict niet reproduceerbaar, bewust niet handmatig afgevinkt en automatisch afgedekt.

## Afgerond: Milestone 46 resultaatdetails bij kwaliteitsherstel

- [x] Gebruiker keurt richting goed voor alle kwaliteitsherstelpagina's; alleen voorbereiding gevraagd tijdens review huidige PR.
- [x] Zes routes, huidige resultaatmodellen, BookService.Conflict en verlies van redenen in dashboard geïnventariseerd.
- [x] Conceptspec, plan en handmatige checklist vastgelegd; geen productcode, build of huidige PR gewijzigd.
- [x] Concrete specificatie en aanpak door gebruiker bevestigd.
- [x] PR #46 door gebruiker gemerged; actuele origin/main opgehaald en nieuwe codex/-branch vanaf 6bee6f0 gemaakt. Lokale voorbereidingsbestanden behouden.
- [x] Titel/auteurconflict eerst met falende test bewezen; minimale getypeerde reden doorgegeven zonder duplicaatbescherming te wijzigen.
- [x] Gedeelde feedbackopbouw en begrensde, gevirtualiseerde details toegevoegd; succesroute en batchreconciliatie behouden.
- [x] Auteur en taal aangesloten met gerichte tests voor één/bulk en foutreden.
- [x] Serie en tags aangesloten met gerichte tests voor stale data, overslaan en bestandswaarschuwingen.
- [x] Omslagdownload en -herstel op dezelfde feedback aangesloten; geen nieuwe bulk- of zoekfunctie.
- [x] Begrijpelijke vertalingen en resourcecontroles voor alle zes talen afgerond.
- [x] Volledige suite: 882 tests geslaagd, geen fouten of overgeslagen tests. Debug-build met 0 waarschuwingen/fouten; precies één Saga.exe onder Builds/Debug, versie 2026.10.22.56, 22 oktober 2026 om 13:25 (Europe/Amsterdam). Eigen vijfassige review uitgevoerd; geen open bevindingen. Randgevallen zijn testgestuurd gecorrigeerd.
- [x] Documentatie exact naar Obsidian gespiegeld en hashes gelijk. Gewone niet-draft PR #47 geopend en gekoppeld: https://github.com/hnsoftwaredevelopment/saga/pull/47; mergeable, GitGuardian groen bij opening.
- [x] CodeQL, GitGuardian en CodeRabbit-status groen op df358ce; PR mergeable en geen inline reviewbevindingen aanwezig bij acceptatiecontrole.
- [x] CodeRabbit-bevinding buiten de diff alsnog beoordeeld en opgelost: README verwijst nu naar de actuele checklist van Milestone 46; de checklistindex bevat ook Milestones 44–46. Commitbereik/optionele commitknoppen zijn geen inhoudelijke commitbevinding. Algemene docstringwaarschuwing niet blind toegepast conform project-DoD.
- [x] Gebruiker heeft alles getest en geaccepteerd: foutredenen duidelijk, werking goed en geen merkbare vertraging ten opzichte van eerdere versies.

## Historie: Milestone 45 todo

- [x] Scope goedgekeurd; bestaande code onderzocht; featurebranch gemaakt.
- [x] Specificatie en implementatieplan vastgelegd.
- [x] Bestaande lijstselectie en snapshotcontrole met tests; tweede selectie na gebruikerstest verwijderd.
- [x] Dashboard en bibliotheekverversing met tests.
- [x] Korte bevestiging met aantal en zes vertalingen.
- [x] Volledige set na interfacecorrectie: 822 tests geslaagd, 0 overgeslagen. Eigen review uitgevoerd; de oude checkboxreview is door het verwijderen van dat overzicht niet meer van toepassing.
- [x] Bijgewerkte Debug-build 2026.10.22.32 geplaatst na afsluiten Saga: 0 waarschuwingen, 0 fouten, gebouwd op 22 oktober 2026 om 11:54.
- [x] Documentatie en checklist bijgewerkt en exact naar Obsidian gespiegeld.
- [x] Gewone PR #46 geopend: https://github.com/hnsoftwaredevelopment/saga/pull/46.
- [x] CodeQL en GitGuardian groen; oude CodeRabbit-checkboxbevinding vervallen door het verwijderen van het overzicht. Herreview beperkt door de limiet.
- [x] De gebruiker heeft alle checklistcontroles uitgevoerd en de slice geaccepteerd.

## Aanvulling Milestone 45: detectie aanscherpen

- [x] Gebruiker vraagt de detectieverbetering binnen deze slice; scope en afweging vastgelegd.
- [x] Eerst falende regressietests: 42 fouten met de oorspronkelijke herkenning.
- [x] Alle 32 correcte praktijkgevallen worden niet meer gemeld; bevestigde Duenas-omwisseling blijft gedetecteerd en veilig herstelbaar.
- [x] Naamvormen en boektitelaanwijzingen aangescherpt; geen netwerk, bibliotheekbrede zoekactie, gegevenswijziging of nieuwe dependency.
- [x] Bestaande dashboardfixtures aangepast aan sterker onderbouwde omwisselingen; detailtests blijven ook niet-gedetecteerde gevallen afdekken.
- [x] Volledige testset: 874 geslaagd, 0 fouten, 0 overgeslagen. Eigen review op correctheid, eenvoud, architectuur, veiligheid en prestaties; geen open bevindingen.
- [x] Na afsluiten Saga nieuwe Debug-build 2026.10.22.37 geplaatst op 22 oktober 2026 om 12:23 (Europe/Amsterdam); 0 waarschuwingen en fouten, precies één Saga.exe onder Builds/Debug.
- [x] PR bijgewerkt; CodeQL, GitGuardian en CodeRabbit-status groen op code-/builddocumentatiecommit c4ba228, zonder mergeconflicten.
- [x] Gebruiker heeft de aanvullende tests uitgevoerd en geaccepteerd; lijst is veel korter en realistischer. Omwisseling naar een bestaand boek wordt terecht geblokkeerd.
- [ ] Vervolgverbetering buiten deze slice: toon bij een mislukte herstelactie ook de begrijpelijke oorzaak, bijvoorbeeld Boek bestaat al, in plaats van alleen Mislukt: 1.

## Afgerond Milestone 44

- [x] Opdracht en scope goedgekeurd; bestaande code en DoD onderzocht.
- [x] Branch, specificatie en plan gemaakt.
- [x] Afzonderlijke previews en veilige opslag testgestuurd implementeren.
- [x] Meervoudige selectie, resultaatverwerking en bibliotheekverversing.
- [x] Scrollbaar overzicht en zes vertalingen.
- [x] Gerichte controles: 200 tests; volledige set: 813 tests, geen fouten of overgeslagen tests. Debug-build 2026.10.22.15: 0 waarschuwingen en fouten. Eigen review uitgevoerd.
- [x] Documentatie en checklist bijgewerkt; Obsidian-spiegel gecontroleerd gelijk.
- [x] Gewone PR #45 geopend, zonder mergeconflicten: https://github.com/hnsoftwaredevelopment/saga/pull/45.
- [x] GitHub-controles groen; twee kleine CodeRabbit-opmerkingen over Duitse en Italiaanse terminologie gecontroleerd en opgelost.
- [x] De gebruiker heeft alle checklistcontroles uitgevoerd en de slice volledig geaccepteerd.

## Afgerond Milestone 43

- [x] PR #43 als gemerged vastgesteld en lokale `main` bijgewerkt.
- [x] Branch `codex/milestone-43-bulk-series-repair` vanaf actuele `main` gemaakt.
- [x] Lokale Splash-wijziging van de gebruiker behouden.
- [x] Bestaand enkelvoudig serieherstel, batchservice en bulkpatronen geïnventariseerd.
- [x] Scope voor bulk serieherstel en robuuste Splash-uitlijning door gebruiker goedgekeurd.
- [x] Volledige specificatie, aannames, grenzen en acceptatiecriteria vastgelegd.
- [x] Implementatieplan en taakvolgorde vastgelegd.
- [x] Volledige specificatie en plan door gebruiker bevestigd; oorspronkelijke Splash-margecorrectie mag door de Grid-variant worden vervangen.
- [x] Splash-layout eerst rood en daarna groen getest en afzonderlijk gecommit.
- [x] Serieviewmodel en venster voor één en meerdere boeken testgestuurd uitgebreid.
- [x] Bulkselectie en generieke resultaatreconciliatie testgestuurd aangesloten.
- [x] Meerdere serienummers, stale series en bibliotheek-/filterverversing bewezen.
- [x] Nieuwe teksten in zes talen toegevoegd en getest.
- [x] Gericht checkpoint uitgevoerd: 32 tests geslaagd, geen fouten of overgeslagen tests; increments afzonderlijk gecommit.
- [x] README, featurestatus en handmatige checklist bijgewerkt en met gelijke hashes naar Obsidian gespiegeld.
- [x] Volledige testset met .NET 10 uitgevoerd: 805 tests geslaagd, geen fouten of overgeslagen tests.
- [x] Schone Debug-build gecontroleerd: 0 waarschuwingen, 0 fouten, `Builds/Debug/Saga.exe` versie `2026.10.21.32`.
- [x] Branchdiff zelf beoordeeld op correctheid, eenvoud, architectuur, beveiliging en prestaties; één onvertaalde legacy-route verwijderd en opnieuw volledig geverifieerd.
- [x] Gewone, niet-draft PR #44 geopend.
- [x] GitHub-controles van PR #44 beoordeeld: CodeQL en GitGuardian groen; CodeRabbit vond in de volledige review geen inhoudelijke opmerkingen. De definitieve één-pixelcorrectie is aanvullend zelf beoordeeld doordat de herreview tegen de uurgrens liep.
- [x] Alle handmatige controles voor bulk serieherstel door de gebruiker geaccepteerd.
- [x] De definitieve 7-pixel lagere positie van de Splash-subtitel is door de gebruiker visueel goedgekeurd en vastgelegd in build `2026.10.22.6`.

## Buiten scope

- Serienummers wijzigen, gelijkmaken, hernummeren of automatisch afleiden.
- Verschillende serienaamwaarden binnen één bulkactie.
- Bulkherstel voor tags, omslagen of titel/auteur.
- Selectie over meerdere kwaliteitscategorieën.
- Quality Page-heuristieken aanpassen.
- Online metadata zoeken of BoekenBase-integratie.
- Database-, sidecar- of ebookmetadataformaatwijzigingen.
