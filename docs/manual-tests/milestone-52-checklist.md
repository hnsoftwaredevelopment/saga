# Milestone 52 — tags beheren

Testbuild: `Builds\Debug\Saga.exe`, versie **2026.10.29.25**, gebouwd op **29 oktober 2026 om 10:35:47 Europe/Amsterdam**. Eén Saga.exe onder Builds; 0 waarschuwingen en 0 fouten. De productwijziging is gebouwd en automatisch geverifieerd; handmatige gebruikersacceptatie staat open.

Gebruik voor hernoemen bij voorkeur een kleine testtag of noteer de oorspronkelijke naam zodat je die kunt terugzetten. Het is geen samenvoeg- of verwijderactie.

## Openen, zoeken en bekijken

- [ ] Open **Tags beheren** via het tagicoon naast Auteurs en Series in de hoofdwerkbalk. De lijst toont tags met aantallen boeken; selecteren toont de juiste titels en auteurs.
- [ ] Typ een deel van een tagnaam, ook met meer letters achter elkaar. Resultaten volgen de laatste zoektekst en focus blijft in het zoekveld. Zoek iets dat niet bestaat: een begrijpelijke melding, geen fout of vastloper.
- [ ] Selecteer opeenvolgende tags met de pijltjestoetsen. Ook na het laden blijven de pijltjes werken. De aantallen en boeken horen steeds bij de gekozen tag.
- [ ] Heeft een tag meer dan 200 boeken, gebruik dan Meer laden bij de boeken. De aanvullende boeken staan op titelvolgorde en verschijnen niet dubbel. Gebruik zo mogelijk ook Meer laden onder een grote taglijst.

## Hernoemen en annuleren

- [ ] Kies een testtag en klik **Tag hernoemen**. De bevestiging toont de huidige naam en het juiste aantal boeken. De nieuwe naam is direct met het toetsenbord bewerkbaar.
- [ ] Annuleer of druk Escape. De tag en gekoppelde boeken blijven ongewijzigd; selectie blijft staan en focus keert terug naar de hernoemknop.
- [ ] Vul een unieke nieuwe naam in en bevestig. De tag wordt voor alle gekoppelde boeken hernoemd, ook als niet alle boeken in de eerste pagina zichtbaar waren. Een korte groene melding toont het aantal boeken en de nieuwe tag blijft geselecteerd/vindbaar.
- [ ] Controleer een boek met meerdere tags: alleen de gekozen tagnaam is veranderd; de overige tags en hun volgorde blijven gelijk. Titel, auteurs, serie, leesstatus en omslag blijven behouden.
- [ ] Probeer een naam die al bij een andere tag hoort. De actie wordt met een duidelijke melding geweigerd; tags worden niet samengevoegd en er verdwijnt niets.
- [ ] Controleer alleen een schrijfwijzecorrectie met andere hoofdletters. Dat mag voor dezelfde tag. Leeg, alleen spaties, puntkomma's of regeleinden zijn niet bruikbaar; de hernoemknop in het invoervenster blijft dan uitgeschakeld.

## Terug in de bibliotheek

- [ ] Sluit het beheer. De nieuwe naam staat direct in boekdetails, tagfilters en groepering waar van toepassing. Eerder geladen omslagen blijven zichtbaar en boeken blijven normaal selecteerbaar/bewerkbaar.
- [ ] Zet de testtag desgewenst terug naar de oude naam en herstart Saga: de opgeslagen naam blijft correct. Bestaand tags bewerken bij één boek, bulkbewerking en Tags opschonen blijven werken zoals eerder.

## Venster, focus en moeilijke fouten

- [ ] Verklein/vergroot het venster en verplaats de splitter. Zoeken, boekenlijst, resultaatmelding, hernoemknop en Sluiten blijven bereikbaar. Controleer het gebruikelijke thema.
- [ ] Ga direct na een laadactie naar het zoekveld of de hernoemdialoog. Een vertraagde focuscorrectie mag je niet terug naar de taglijst brengen. Enter/Escape en Tab blijven werken; bij opslaan is een tweede hernoemactie geblokkeerd.
- [ ] Alleen indien dit vanzelf optreedt: bij bestandswaarschuwingen vermeldt Saga dat de tag in de bibliotheek wel is hernoemd, met afzonderlijke betrokken boeken. Een conflict schrijft niets; een verversingsfout na opslaan meldt juist dat de wijziging wel is opgeslagen. Geen bestanden blokkeren of echte boekgegevens beschadigen om dit uit te lokken.

## Automatische dekking

**1024 tests geslaagd**, 0 mislukt/overgeslagen. **17 interactieve WPF-scenario's groen**, inclusief de vijftien bestaande controles en twee tagbeheerproeven voor zoek-/lijstfocus, fouten, nieuwere focuskeuzes en annuleren/opslaan vanuit de echte dialoog. WPF-weergaven met fictieve gegevens op minimale grootte in licht/donker bekeken.

SQLite-tests bewaken tagidentiteit, behoud van alle boekvelden behalve UpdatedUtc, andere tags en hun volgorde, niet-betrokken boeken, 510 gekoppelde boeken over paginagrenzen, rollback, bestaande ongebruikte doelnamen, gewijzigde bronkoppelingen/naam, annulering en Unicode-zoekpaginering. Bestandstests gebruiken echte tijdelijke metadata.json-bestanden en gesimuleerde waarschuwingen; na databasecommit wordt de bestaande synchronisatie voltooid. Viewmodeltests bewaken oude resultaten, bevriezen tijdens opslaan, paginering, begrijpelijke feedback en het verschil tussen niet opgeslagen en niet ververst. Moeilijk handmatig te reproduceren fouten zijn daarmee automatisch gedekt.
