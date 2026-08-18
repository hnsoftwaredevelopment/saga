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
- Open `Instellingen > Duplicaten`.
- Klik op `Genegeerde duplicaten beheren`.
- Controleer dat het genegeerde paar in het overzicht staat.
- Selecteer het paar en klik op `Geselecteerde terugzetten`.
- Open het duplicatenvenster opnieuw en controleer dat het paar weer zichtbaar is.
- Markeer opnieuw een paar als geen duplicaat, open beheer opnieuw en klik op `Alles terugzetten`.
- Controleer dat het beheerwindow leeg is.
- Verwijder of merge een boek uit een andere duplicate-groep en controleer dat het bestaande gedrag blijft werken.

## Verwacht Resultaat

- Genegeerde duplicate-relaties blijven verborgen.
- Genegeerde duplicate-relaties kunnen door de gebruiker worden teruggezet via de gebruikersinterface, zonder handmatige database-ingreep.
- De hoofdboekenlijst wordt pas ververst bij sluiten van het duplicatenvenster.
