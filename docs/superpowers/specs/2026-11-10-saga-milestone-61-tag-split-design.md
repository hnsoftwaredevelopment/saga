# Milestone 61 — een tag splitsen

## Gebruik

Kies een tag in **Tags beheren**, selecteer de boeken die je wilt wijzigen en open **Tag splitsen**. Voer twee of meer verschillende vervangende tags in, één per regel. Het voorstel toont de ingevoerde namen na het verwijderen van spaties aan de uiteinden en de titels van de geselecteerde boeken. De bevestiging is beschikbaar zodra alle namen geldig en uniek zijn. Hoofdletters en spaties aan de uiteinden tellen bij vergelijking niet mee. De oorspronkelijke tag mag niet tussen de vervangers staan.

## Opslag en resultaat

Saga hergebruikt bestaande tags met hun opgeslagen schrijfwijze. Een al aanwezige koppeling blijft op haar plek. De eerste nieuwe koppeling gebruikt de vrijgekomen plek van de brontag. Volgende nieuwe koppelingen komen achteraan in invoervolgorde, zodat andere koppelingen op hun plek blijven.

De bestaande transactie voor boek-tagkoppelingen controleert de brontag, de bevestigde boeken en hun titels. Nieuwe tags en gewijzigde koppelingen worden samen opgeslagen. De bestaande bestandssynchronisatie verwerkt de geselecteerde boeken na de commit en toont eventuele waarschuwingen afzonderlijk. De tagindex en boekenlijst worden opnieuw geladen. Annuleren houdt de selectie en gegevens intact.
