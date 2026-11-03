# Milestone 59 — geselecteerde series samenvoegen

Testbuild: `Builds\Debug\Saga.exe`. Verificatie: **1236 geautomatiseerde tests en 32 echte WPF-proeven geslaagd**, 0 mislukt/overgeslagen. Schone enige Debug-build **2026.11.3.48**, **3 november 2026 14:00:46 Europe/Amsterdam**, 0 waarschuwingen/fouten. Eigen vijfassige review uitgevoerd; licht/donker op minimale venstergrootte gerenderd en bekeken. Nieuwe doelnaam en niet-geselecteerde bestaande doelnaam eerst falend getest; 503 boeken over drie series, behoud nummers/overige metadata, echte metadata.json, bestaande rollback/conflict/bestandsroute, paging/selectie/focus en oude duplicaatroute gecontroleerd. Gebruikersacceptatie staat nog open. Kies twee of drie series die je bewust wilt samenvoegen en noteer hun boekaantallen en volgnummers. Deze actie combineert serietoewijzingen, geen boekrecords.

## Selecteren en vergelijken

- [ ] Open Series beheren. Zonder selectie of bij één geselecteerde serie is Geselecteerde series samenvoegen uitgeschakeld. Eén geselecteerde serie behoudt de bestaande boek-, hernoem- en nummeracties.
- [ ] Selecteer twee of meer series met Ctrl of Shift. Het geselecteerde aantal is zichtbaar en de samenvoegactie wordt beschikbaar. Het boekpaneel is niet bewerkbaar zolang meerdere series geselecteerd zijn.
- [ ] Indien er meer dan 200 series zijn: klik Meer laden en controleer dat de serie-selectie behouden blijft. Een nieuwe zoekopdracht wist de selectie; verborgen series worden niet meegenomen.
- [ ] Open Geselecteerde series samenvoegen. Alle boeken uit de gekozen series zijn aanwezig, ook boeken die niet in de beheerboekenlijst geladen waren. Huidige serie, titel, auteurs en volgnummer zijn zichtbaar. Bij een grote vergelijking gebruik je Meer laden.
- [ ] Gelijke nummers en ontbrekende nummers worden informatief getoond. Er wordt geen nummer toegekend, gewist of gewijzigd. Je hoeft de gekozen series of boeken niet opnieuw aan te vinken.

## Naam kiezen, annuleren en opslaan

- [ ] Aanvankelijk is geen bestemming gekozen en kun je niet bevestigen. Kies één van de geselecteerde serienamen in de keuzelijst: bevestigen wordt beschikbaar.
- [ ] Typ in het aparte veld een nieuwe serienaam. De bestaande naamkeuze wordt leeg; typen blijft vlot en behoudt focus. Lege invoer of alleen spaties zonder bestaande naamkeuze blokkeert bevestigen. Kies daarna een bestaande naam: het nieuwe-naamveld wordt leeggemaakt.
- [ ] Wijzig een naamkeuze en annuleer met Annuleren of Escape. Niets verandert; de serie-selectie en zoektekst blijven behouden en de actieknop krijgt focus terug.
- [ ] Voeg twee of meer series samen onder één gekozen bestaande naam. De gezamenlijke serie bevat alle verwachte boeken. Nummers, titels, auteurs, omslagen en overige metadata zijn gelijk gebleven; niet-geselecteerde series veranderen niet.
- [ ] Herhaal zo mogelijk met een volledig nieuwe naam. Alleen de gekozen series verdwijnen als afzonderlijke vermeldingen; hun boeken staan samen onder de nieuwe naam. Omringende spaties worden verwijderd.
- [ ] Probeer als nieuwe naam de naam van een niet-geselecteerde bestaande serie. Saga weigert dit met uitleg; niets verandert en de selectie blijft behouden. Selecteer die serie vervolgens ook, of kies een andere nieuwe naam.
- [ ] Na succesvol samenvoegen is het zoekveld leeg en staat de gezamenlijke serie geselecteerd met haar juiste aantal boeken. Je kunt haar boeken direct bekijken en de bestaande één-serieacties weer gebruiken.
- [ ] Sluit Series beheren en controleer in het hoofdscherm de seriegegevens, groepering/filter en details van enkele boeken. De nieuwe naam is verwerkt, zonder verlies of hernummering van boeken.

## Bediening en uitzonderingen

- [ ] Bedien selectie en vergelijking met toetsenbord, Tab, Enter en Escape. Het nieuwe naamveld behoudt focus bij iedere letter. Na annuleren keert focus terug naar de actie; na succes naar de gezamenlijke serie.
- [ ] Verklein het venster en controleer licht/donker. De bestemming, boeken en knoppen blijven bereikbaar. Wissel indien gewenst de taal voor de nieuwe teksten.
- [ ] Controleer als regressie dat Duplicaten zoeken voor series en het bestaande vergelijkingsvenster blijven werken. De bestaande voorstelroute vraagt nog steeds een naam uit het getoonde paar.
- [ ] Bestandswaarschuwingen hoeven niet met echte boeken geforceerd te worden. Als ze verschijnen, is duidelijk dat de samenvoeging wel opgeslagen is en welke bestanden niet konden worden bijgewerkt. Geautomatiseerde tests dekken de bestaande bestandsroute.
- [ ] Veranderde/verdwenen/toegevoegde boeken en technische opslagfouten zijn geautomatiseerd afgedekt: de hele transactie wordt geblokkeerd of teruggedraaid. Hiervoor hoef je geen echte bibliotheekbestanden te beschadigen.

## Notities

Nog door de gebruiker in te vullen.
