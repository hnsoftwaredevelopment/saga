# Milestone 22 - Duplicaten Markeren Als Geen Duplicaat

## Voorbereiding

- Gebruik een bibliotheek met minimaal twee boeken met dezelfde titel die geen echte duplicaten zijn.
- Zorg dat `Duplicaten` vanuit de toolbar resultaten toont.

## Testen

- Open het duplicatenvenster.
- Zet eventueel de toggle uit zodat titel-only matches zichtbaar worden.
- Kies bij een boek in een duplicate-groep de actie `Dit boek markeren als geen duplicaat`.
- Controleer dat het gekozen boek direct uit die duplicate-groep verdwijnt.
- Controleer bij een groep met drie boeken dat de andere twee zichtbaar blijven als zij nog duplicaten van elkaar zijn.
- Sluit het duplicatenvenster en open het opnieuw.
- Controleer dat dezelfde groep niet opnieuw verschijnt.
- Verwijder of merge een boek uit een andere duplicate-groep en controleer dat het bestaande gedrag blijft werken.

## Verwacht Resultaat

- Genegeerde duplicate-relaties blijven verborgen.
- De hoofdboekenlijst wordt pas ververst bij sluiten van het duplicatenvenster.
- Er is in deze slice nog geen scherm om genegeerde duplicaten terug te zetten.
