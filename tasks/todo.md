# Milestone 47: betrouwbaarheid Quality afronden

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
