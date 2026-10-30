# Milestone 52 — tags beheren

Reviewafronding na gebruikersacceptatie: de checklistindex in README bevat ook Milestone 49–52. Bestandswaarschuwingen blijven beschikbaar wanneer het herladen van de boekenlijst na een opgeslagen samenvoeging mislukt. Deze fout is eerst met een falende test aangetoond en daarna hersteld; opgeslagen toestand, betrokken boek-ID's en waarschuwingen blijven behouden. De functionele gebruikersacceptatie betreft build 2026.10.29.47; de kleine foutpadcorrectie is automatisch geverifieerd en vraagt geen geforceerde handmatige bestandsfout.

Testbuild: `Builds\Debug\Saga.exe`, versie **2026.10.29.51**, gebouwd op **29 oktober 2026 om 16:05:02 Europe/Amsterdam**. Eén Saga.exe onder Builds; 0 waarschuwingen en 0 fouten. De productwijziging is gebouwd en automatisch geverifieerd. De gebruiker heeft op 29 oktober 2026 bevestigd: “Ja nu werkt het zoals verwacht. Deze slice is akkoord”. Hiermee is de volledige slice geaccepteerd, inclusief de selectieacties en globaal samenvoegen. De afzonderlijke oorspronkelijke vinkjes en opmerkingen blijven als testhistorie behouden; niet handmatig uitgelokte fouten blijven automatisch afgedekt. PR #54 is op 30 oktober 2026 gemerged.

Gebruik voor wijzigingen bij voorkeur een kleine testtag of noteer de oorspronkelijke waarden. Tag wijzigen en Tag verwijderen gelden uitsluitend voor geselecteerde boeken; Tag overal hernoemen geldt voor alle gekoppelde boeken. Geen selectie betekent nooit automatisch alle boeken. De onderstaande eerdere gebruikersvinkjes en opmerkingen zijn behouden; de nieuwe hercontrole staat onderaan.

## Openen, zoeken en bekijken

- [X] Open **Tags beheren** via het tagicoon naast Auteurs en Series in de hoofdwerkbalk. De lijst toont tags met aantallen boeken; selecteren toont de juiste titels en auteurs.
- [X] Typ een deel van een tagnaam, ook met meer letters achter elkaar. Resultaten volgen de laatste zoektekst en focus blijft in het zoekveld. Zoek iets dat niet bestaat: een begrijpelijke melding, geen fout of vastloper.
- [X] Selecteer opeenvolgende tags met de pijltjestoetsen. Ook na het laden blijven de pijltjes werken. De aantallen en boeken horen steeds bij de gekozen tag.
- [X] Heeft een tag meer dan 200 boeken, gebruik dan Meer laden bij de boeken. De aanvullende boeken staan op titelvolgorde en verschijnen niet dubbel. Gebruik zo mogelijk ook Meer laden onder een grote taglijst.

## Hernoemen en annuleren

- [ ] Kies een testtag en klik **Tag overal hernoemen**. De bevestiging toont de huidige naam en het juiste aantal boeken. De nieuwe naam is direct met het toetsenbord bewerkbaar.
- [X] Vul een unieke nieuwe naam in en bevestig. De tag wordt voor alle gekoppelde boeken hernoemd, ook als niet alle boeken in de eerste pagina zichtbaar waren. Een korte groene melding toont het aantal boeken en de nieuwe tag blijft geselecteerd/vindbaar.
- [ ] Controleer een boek met meerdere tags: alleen de gekozen tagnaam is veranderd; de overige tags en hun volgorde blijven gelijk. Titel, auteurs, serie, leesstatus en omslag blijven behouden.
- [X] Eerdere test: een bestaande doelnaam werd geweigerd. Zie mijn opmerking. Dit gedrag is op gebruikersverzoek vervangen door samenvoegen; zie de nieuwe hercontrole onderaan.
- [X] Controleer alleen een schrijfwijzecorrectie met andere hoofdletters. Dat mag voor dezelfde tag. Leeg, alleen spaties, puntkomma's of regeleinden zijn niet bruikbaar; de hernoemknop in het invoervenster blijft dan uitgeschakeld.

## Terug in de bibliotheek

- [X] Sluit het beheer. De nieuwe naam staat direct in boekdetails, tagfilters en groepering waar van toepassing. Eerder geladen omslagen blijven zichtbaar en boeken blijven normaal selecteerbaar/bewerkbaar.
- [X] Zet de testtag desgewenst terug naar de oude naam en herstart Saga: de opgeslagen naam blijft correct. Bestaand tags bewerken bij één boek, bulkbewerking en Tags opschonen blijven werken zoals eerder.

## Venster, focus en moeilijke fouten

- [X] Verklein/vergroot het venster en verplaats de splitter. Zoeken, boekenlijst, resultaatmelding, hernoemknop en Sluiten blijven bereikbaar. Controleer het gebruikelijke thema.
- [X] Ga direct na een laadactie naar het zoekveld of de hernoemdialoog. Een vertraagde focuscorrectie mag je niet terug naar de taglijst brengen. Enter/Escape en Tab blijven werken; bij opslaan is een tweede hernoemactie geblokkeerd.
- [ ] Alleen indien dit vanzelf optreedt: bij bestandswaarschuwingen vermeldt Saga dat de tag in de bibliotheek wel is hernoemd, met afzonderlijke betrokken boeken. Een conflict schrijft niets; een verversingsfout na opslaan meldt juist dat de wijziging wel is opgeslagen. Geen bestanden blokkeren of echte boekgegevens beschadigen om dit uit te lokken. Niet kunnen testen; dit pad is automatisch afgedekt en hoeft niet handmatig te worden geforceerd

## Automatische dekking

**1054 tests geslaagd**, 0 mislukt/overgeslagen. **20 interactieve WPF-scenario's groen**, inclusief de vijftien bestaande controles, twee tagbeheerproeven, twee selectie-/paginaproeven en een globale samenvoegproef voor zoek-/lijstfocus, fouten, nieuwere focuskeuzes en annuleren/opslaan vanuit de echte dialoog. WPF-weergaven met fictieve gegevens op minimale grootte in licht/donker bekeken.

SQLite-tests bewaken tagidentiteit, behoud van alle boekvelden behalve UpdatedUtc, andere tags en hun volgorde, niet-betrokken boeken, 510 gekoppelde boeken over paginagrenzen, rollback, bestaande ongebruikte doelnamen, gewijzigde bronkoppelingen/naam, annulering en Unicode-zoekpaginering. Bestandstests gebruiken echte tijdelijke metadata.json-bestanden en gesimuleerde waarschuwingen; na databasecommit wordt de bestaande synchronisatie voltooid. Viewmodeltests bewaken oude resultaten, bevriezen tijdens opslaan, paginering, begrijpelijke feedback en het verschil tussen niet opgeslagen en niet ververst. Moeilijk handmatig te reproduceren fouten zijn daarmee automatisch gedekt.

## Nieuwe hercontrole — geselecteerde boeken

- [ ] Kies een brontag met meerdere boeken. Zonder boekselectie zijn Tag wijzigen en Tag verwijderen uitgeschakeld. Selecteer één boek: de telling is 1 en de rij blijft herkenbaar geselecteerd wanneer je naar een actieknop gaat. Tag overal hernoemen is afzonderlijk benoemd en geldt nog steeds voor alle gekoppelde boeken.
- [ ] Klik Tag wijzigen, voer een unieke nieuwe naam in en annuleer. De selectie en gegevens blijven behouden. Bevestig daarna dezelfde wijziging: alleen het geselecteerde boek verdwijnt direct uit de bronlijst, de bron-/doelaantallen zijn bijgewerkt en de nieuwe tag toont het gewijzigde boek wanneer je haar kiest. Niet-geselecteerde boeken houden hun brontag.
- [ ] Herhaal met een bestaande doeltag die het boek nog niet heeft. Vervolgens met jouw geval: een boek heeft zowel <Geen tag> als Roman; wijzig alleen <Geen tag> naar Roman via Tag wijzigen. De oude tag verdwijnt, Roman staat er één keer en overige tags houden hun volgorde. Er verschijnt geen blokkade wegens een bestaande doelnaam voor deze selectieactie.
- [ ] Selecteer enkele boeken en kies Tag verwijderen. Annuleren verandert niets. Na bevestigen verdwijnt alleen de gekozen tag uit die boeken; de boeken en overige tags blijven bestaan. Verwijder ook bij een testboek diens laatste tag: het boek blijft in de bibliotheek. Als de brontag geen boeken meer heeft, verdwijnt zij uit de tagindex en wordt het rechterpaneel leeggemaakt.
- [ ] Selecteer meer dan één boek, gebruik zo mogelijk Meer laden en controleer dat de selectie blijft staan. Ctrl+A selecteert de geladen rijen en de telling toont het werkelijke aantal. Een andere tag kiezen wist de oude boekselectie. Controleer na sluiten ook de boekdetails, tagfilters en groepering.

Aanvullende automatische dekking: 505 van 510 boeken over databasechunks, nieuwe/bestaande doeltag, bestaande doeltagpositie, alleen brontag verwijderen, een boek zonder tags behouden, niet-geselecteerde boeken ongemoeid, gedeeltelijk ontbrekende selectie weigeren en rollback van tagrecord/koppelingen/timestamps. Echte tijdelijke metadata.json-bestanden en bestandswaarschuwingen na commit zijn getest. De WPF-proeven bedienen de echte selectieknoppen en dialoog, controleren dat verwerkte boeken meteen uit de bronlijst verdwijnen, aantallen kloppen, annuleren selectie behoudt en native selectie na 205 geladen rijen/Ctrl+A klopt. Selectiemarkering is ook zichtbaar wanneer de actieknop focus heeft. Moeilijke bestandsfouten blijven automatisch gedekt.

## Nieuwe hercontrole — overal hernoemen naar een bestaande tag

- [ ] Kies een kleine testtag zonder boeken te selecteren. Kies Tag overal hernoemen en voer een bestaande doeltag in. De bevestiging vermeldt alle bronboeken en legt samenvoegen uit. Annuleren wijzigt niets.
- [ ] Bevestig de wijziging. Alle boeken van de brontag krijgen de bestaande doeltag, ook buiten de eerste geladen pagina. Een boek met beide tags houdt alleen de doeltag, één keer; overige tags blijven in hun relatieve volgorde staan. Boeken die alleen de doeltag hadden blijven ongewijzigd.
- [ ] De oude tag verdwijnt direct; de bestaande doeltag wordt geselecteerd met de volledige gecombineerde boekenlijst en het juiste aantal unieke boeken. De groene melding telt de gewijzigde bronboeken. Controleer na sluiten ook details, tagfilters en groepering. De afzonderlijke selectieacties blijven alleen de geselecteerde boeken wijzigen/verwijderen.

Automatisch afgedekt: eerst falende regressies voor bestaande/ongebruikte doelnaam, behoud van de bestaande doelpositie, 510 boeken over databasechunks, rollback van bron-/doeltoestand en timestamps, synchronisatie met bestandswaarschuwingen, viewmodelverversing en de echte globale WPF-knop zonder boekselectie.
