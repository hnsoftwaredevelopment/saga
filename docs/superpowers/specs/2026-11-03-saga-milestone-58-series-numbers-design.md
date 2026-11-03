# Milestone 58 — meerdere serievolgnummers toekennen

De gebruiker keurt de voorgestelde slice goed: meerdere boeken binnen één serie selecteren, gewenste volgorde bepalen, startnummer invoeren en huidige/voorgestelde nummers vergelijken vóór opslag. Basis main 14ed4dd (PR #59 gemerged), met de lokale afrondingsnotitie f010815. Branch codex/milestone-58-series-numbers.

## Gedrag

- In Series beheren ondersteunt de boekenlijst Ctrl/Shift-meervoudige selectie, inclusief Meer laden. Wisselen van serie wist de boekselectie. Eén boek behoudt de bestaande Volgnummer wijzigen-route; meerdere geselecteerde boeken krijgen Volgnummers toekennen.
- Een verse preview bevat exact de geselecteerde boek-IDs, titels, auteurs, oorspronkelijke nummers en exacte serienaam per boek. Geen opnieuw aanvinken en geen verborgen, niet-geselecteerde boeken toevoegen. De eerste volgorde is de zichtbare boekenvolgorde.
- Het dialoogvenster toont huidig en nieuw nummer. De gebruiker verplaatst een gekozen boek met Omhoog/Omlaag, ook met toetsenbordbediening. Startnummer standaard 1; gehele of decimale invoer volgens de huidige cultuur. Zoals bij de bestaande losse nummerwijziging zijn nul en negatieve getallen toegestaan. Nieuwe nummers lopen telkens met één op. Ongeldige/lege invoer en decimal-overflow blokkeren bevestigen. Er is geen automatisch wissen of bibliotheekbrede hernummering.
- Gelijke voorgestelde nummers bij niet-geselecteerde boeken worden informatief getoond; bevestigen blijft mogelijk. De bestaande nummers van die andere boeken veranderen niet. Alleen echte nummerwijzigingen worden opgeslagen.
- Annuleren of Escape in de preview verandert niets. Expliciet bevestigen schrijft alle gewijzigde nummers en update-tijdstippen in één transactie. De exacte geselecteerde snapshots worden opnieuw gecontroleerd; ontbrekende/veranderde titel, auteurs, serie of nummer blokkeert de gehele transactie. Nieuwe wijzigingen aan andere velden blijven behouden. Een gewijzigde andere serie of nieuw nummer bij een niet-geselecteerd boek blokkeert niet onnodig.
- Na commit volgt de bestaande metadata.json-/ebook-synchronisatie voor de gewijzigde boeken; annulering na commit verhindert die afhandeling niet. Bestandsproblemen zijn afzonderlijke waarschuwingen en doen niet alsof de nummers niet opgeslagen zijn. Tijdens opslaan blijft Series beheren veilig geblokkeerd voor sluiten/bewerken.
- De geladen boekenprefix wordt met numerieke sortering opnieuw geladen, in pagina's van 200. Selectie op ID herstellen voor nog zichtbare boeken; bij buiten de geladen prefix verplaatste boeken volgt uitleg. De hoofdweergave/details verversen bij sluiten via de bestaande gewijzigde IDs.

## Bouw en verificatie

Gerichte Domain-modellen en repositorycontract, EF-partial, Application-orkestratie, testbaar previewmodel en managementpartial, klein WPF-venster. Bestaande één-boekroute behouden. Geen schema, externe dienst, dependency of agents. Query's lezen uitsluitend gekozen basisgegevens/auteurs in batches van maximaal 500 IDs en relevante andere nummers; geen cover-BLOBs. Voortgang van bestandssynchronisatie valt onder de bestaande manager-wachtindicatie.

Eerst gerichte tests voor volgorde/startnummer/overflow, exacte geselecteerde scope, bestaande andere gegevens, >500 IDs, stale snapshot/rollback, doublures en bestandswaarschuwingen. Daarna bestaande en nieuwe WPF-routes, toetsenbord/focus/selectie/Meer laden, minimumgrootte en licht/donker. Zes talen, volledige suite, eigen vijfassige review en expliciete schone enige Debug-build. Handmatige checklist, README/featurestatus, exacte Obsidian-spiegels en gewone niet-draft PR. De gebruiker accepteert en mergt.

Het bestaande Excel-overzicht blijft behouden. Pipeline-item P22 wordt M58, geen extra scope-rij; eerst Todo/In uitvoering, bij oplevering Te testen en pas na gebruikersacceptatie Done.
