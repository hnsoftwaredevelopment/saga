# Duplicaten Markeren Als Geen Duplicaat

## Doel

Soms hebben twee boeken dezelfde titel, maar zijn het inhoudelijk verschillende boeken. Saga moet de gebruiker dan de mogelijkheid geven om zo'n duplicate-groep te markeren als `geen duplicaat`, zodat deze groep niet telkens opnieuw in het duplicatenvenster verschijnt.

## Eerste slice

- Voeg in het duplicatenvenster een actie `Dit boek markeren als geen duplicaat` toe.
- De actie werkt op het gekozen boek binnen de zichtbare duplicate-groep.
- Saga bewaart paren tussen het gekozen boek en de andere boeken in die groep in de actieve bibliotheekdatabase.
- Na markeren verdwijnt het gekozen boek uit die duplicate-groep.
- Als er in de groep nog minimaal twee onderlinge duplicaten overblijven, blijft die kleinere groep zichtbaar.
- Als er geen onderlinge duplicaten overblijven, verdwijnt de groep volledig.
- Bij opnieuw openen van het duplicatenvenster blijven genegeerde groepen verborgen.
- Als een betrokken boek wordt verwijderd of samengevoegd, ruimt de database de bijbehorende uitsluitingen automatisch op.

## Later

- Beheerscherm om genegeerde duplicaten te bekijken en eventueel terug te zetten.
- Mogelijk een bulkactie voor meerdere geselecteerde duplicate-groepen.
- Duidelijkere iconografie als er een definitief icon beschikbaar is.
