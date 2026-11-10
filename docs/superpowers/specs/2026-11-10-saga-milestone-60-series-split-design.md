# Milestone 60 — een serie afsplitsen

## Gebruik

Open **Series beheren**, kies één serie en selecteer een of meer boeken uit die serie. **Geselecteerde boeken afsplitsen** is beschikbaar als ten minste één boek in de bronserie achterblijft. Voor het hernoemen van de hele serie blijft **Serie hernoemen** beschikbaar.

Het bevestigingsvenster toont de bronserie, de geselecteerde boektitels met hun huidige volgnummers en hoeveel boeken achterblijven. Het veld voor de nieuwe serienaam begint leeg en krijgt de focus. De naam moet na trimmen nieuw zijn, ook bij verschillen in hoofdletters. Annuleren laat de selectie en gegevens intact.

## Opslag en resultaat

De bestaande reekshernoeming verwerkt de afsplitsing als een expliciete selectie. De transactie controleert de leden van de bronserie, de getoonde titels en nummers van de geselecteerde boeken en de beschikbaarheid van de bestemming. Alleen de geselecteerde boeken krijgen de nieuwe serie. Ontbrekende, gelijke en decimale volgnummers blijven behouden. De bestaande synchronisatie verwerkt alleen de gewijzigde boeken en meldt eventuele bestandswaarschuwingen.

Na succes wordt de nieuwe serie getoond en is de index bijgewerkt. De hoofdweergave krijgt dezelfde wijzigingsmelding als bij serie hernoemen. De wijziging gebruikt de bestaande gegevensstructuur en metadatawriters.
