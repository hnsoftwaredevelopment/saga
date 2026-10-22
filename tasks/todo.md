# Milestone 45 todo

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
- [ ] PR bijwerken en nieuwe GitHub-controles beoordelen.
- [ ] Korte aanvullende handmatige hercontrole van de detectie door gebruiker.

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
