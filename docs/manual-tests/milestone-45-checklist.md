# Milestone 45: titel en auteur voor meerdere boeken omwisselen

Testbuild: `Builds\Debug\Saga.exe`, versie `2026.10.22.32`, gebouwd op 22 oktober 2026 om 11:54.

## Voorbereiding

- [x] Maak zo nodig een back-up en noteer titel, auteur en overige metadata van testboeken.
- [x] Open Quality en kies Mogelijk omgewisselde auteur/titel.

## Selectie en bevestiging

- [x] Selecteer twee of meer boeken die werkelijk moeten worden omgewisseld, met Ctrl of Shift.
- [x] Kies Titel en auteur omwisselen: alleen een korte bevestiging verschijnt met het juiste aantal.
- [x] Er verschijnt geen tweede boekenoverzicht en je hoeft geen vinkjes te zetten.
- [x] Kies Nee of Escape: geen van de geselecteerde boeken is gewijzigd.
- [x] Herhaal en bevestig: alle geschikte boeken uit de lijstselectie worden omgewisseld.
- [x] Een niet geselecteerd boek blijft ongewijzigd.
- [x] Er volgt bij volledig succes een korte groene melding met het werkelijk gewijzigde aantal.
- [x] Hoofdlijst, details en auteursfilters tonen de actuele waarden; overige metadata en bestanden blijven behouden.
- [x] Controleer na herstart dat de wijzigingen behouden zijn.

## Regressies en veiligheid

- [x] Eén geselecteerd boek toont nog de bestaande enkelvoudige vóór/na-bevestiging.
- [x] De detailactie en overige bulkacties werken zoals eerder.
- [x] Controleer zo mogelijk een andere taal.
- [x] Indien veilig reproduceerbaar: ongeschikte of inmiddels gewijzigde boeken worden overgeslagen en andere boeken gaan door.
- [x] Indien veilig reproduceerbaar: mislukte boeken blijven geselecteerd en de resultaatmelding noemt de juiste aantallen.

Deze foutpaden zijn automatisch getest; niet veilig reproduceerbare handmatige foutcontroles mogen als niet uitgevoerd worden genoteerd.

## Acceptatie

- [x] Alle toepasselijke controles zijn geslaagd; afwijkingen zijn genoteerd.
De gebruiker heeft bevestigd dat alle bovenstaande checklistcontroles zijn uitgevoerd en geslaagd; de bulkherstelactie is geaccepteerd.

## Aanvulling: terughoudender omwisselherkenning

De gebruiker heeft deze aanvullende wijziging handmatig getest en geaccepteerd. Testbuild: `Builds\Debug\Saga.exe`, versie `2026.10.22.37`, gebouwd op 22 oktober 2026 om 12:23 (Europe/Amsterdam). De eerder geaccepteerde bulkcontroles hierboven zijn met versie 32 uitgevoerd.

- [x] Open opnieuw Quality / Mogelijk omgewisselde auteur/titel: de lijst is kleiner; onder meer Dossier Gifmoord / Ed van Eeden en Dokter Finley Assistent / A.J. Cronin worden niet meer onterecht gemeld.
- [x] Indien nog aanwezig en niet eerder als correct gemarkeerd: Duenas, Maria / De wereld vergeten blijft zichtbaar. Eerder herstelde of uitgesloten boeken hoeven niet terug te komen.
- [x] Controleer één echte omwisseling; na bevestigen verdwijnt deze melding en verschijnen de juiste waarden in de bibliotheek.
- [x] Selecteer meerdere echte omwisselingen: de korte bevestiging gebruikt nog steeds direct de lijstselectie, zonder extra vinkjes. Annuleren wijzigt niets.
- [x] Eerdere beslissingen Dit is correct blijven behouden. Handmatig omwisselen via de boekdetails blijft beschikbaar, ook voor boeken die niet in deze lijst staan.

Extra gebruikerstest geslaagd: voeg het correct ingevulde boek toe, en probeer daarna het fout ingevulde exemplaar naar dezelfde titel/auteur om te wisselen. Saga blokkeert deze wijziging terecht. Bekend verbeterpunt buiten deze slice: de melding toont `Mislukt: 1` zonder de oorzaak `Boek bestaat al` te vermelden. De gebruiker heeft de slice ook met deze beperking geaccepteerd.
