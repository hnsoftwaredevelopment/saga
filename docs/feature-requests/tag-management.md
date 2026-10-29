# Tags beheren

## Status

Milestone 52 is gebouwd na het scopeakkoord van 29 oktober 2026, vanaf geaccepteerde en gemergede PR #53 / main e8e024c. Beschikbaar via het tagicoon naast Auteurs en Series in de hoofdwerkbalk. De oorspronkelijke globale hernoemactie heet nu Tag overal hernoemen. Na de gebruikerstest zijn Tag wijzigen en Tag verwijderen voor geselecteerde boeken toegevoegd. Deze acties verversen direct de bronboeklijst en bron-/doelaantallen. Bestandssynchronisatie gebruikt de bestaande BookService-route. Geen schemawijziging of bibliotheekconversie.

1047 tests en negentien interactieve WPF-scenario's slagen. Debug-build 2026.10.29.40 van 29 oktober 14:20:14 (Europe/Amsterdam) heeft 0 waarschuwingen/fouten en is de enige Saga.exe onder Builds. Eigen vijfassige review uitgevoerd; handmatige gebruikersacceptatie staat open in de [checklist](../manual-tests/milestone-52-checklist.md).

## Eerste slice

- Herkenbaar beheervenster zoals Auteurs en Series beheren.
- Tags doorzoeken met het aantal gekoppelde boeken en de bijbehorende boeken bekijken.
- Eén tag expliciet hernoemen voor alle gekoppelde boeken, met bevestiging.
- Overige tags, hun volgorde en alle overige boekgegevens behouden.
- Voor geselecteerde boeken de brontag vervangen door een nieuwe of bestaande doeltag. Heeft het boek de doeltag al, dan verdwijnt uitsluitend de brontag; geen dubbele koppeling.
- De brontag expliciet uit geselecteerde boeken verwijderen zonder boeken of overige tags te verwijderen.
- Alleen de afzonderlijke globale hernoemactie blokkeert een bestaande andere doelnaam; de melding verwijst naar de selectieactie.
- Bestaande opslag/bestandssynchronisatie en begrijpelijke waarschuwingen hergebruiken.
- Zoek-/lijstfocus, paginering en geladen omslagen behouden.

Globaal tagrecords samenvoegen/splitsen/verwijderen en online metadata blijven afzonderlijke vervolgstappen. Het wijzigen/verwijderen van boek-tagkoppelingen voor een expliciete selectie is wel onderdeel van deze slice, na de aanvullende gebruikersopdracht.

Concrete acceptatie, opslagkeuzes en tests: [Milestone 52-specificatie](../superpowers/specs/2026-10-29-saga-milestone-52-tag-management-design.md). De afzonderlijke gebruikerswens voor [auteursvarianten samenvoegen](author-variants.md) is voor later vastgelegd en verandert deze slice niet.
