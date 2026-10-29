# Tags beheren

## Status

Milestone 52 is gebouwd na het scopeakkoord van 29 oktober 2026, vanaf geaccepteerde en gemergede PR #53 / main e8e024c. Beschikbaar via het tagicoon naast Auteurs en Series in de hoofdwerkbalk. De oorspronkelijke globale hernoemactie heet nu Tag overal hernoemen. Na de gebruikerstest zijn Tag wijzigen en Tag verwijderen voor geselecteerde boeken toegevoegd. Deze acties verversen direct de bronboeklijst en bron-/doelaantallen. Bestandssynchronisatie gebruikt de bestaande BookService-route. Geen schemawijziging of bibliotheekconversie.

1054 tests en twintig interactieve WPF-scenario's slagen. Debug-build 2026.10.29.51 van 29 oktober 16:05:02 (Europe/Amsterdam) heeft 0 waarschuwingen/fouten en is de enige Saga.exe onder Builds. Eigen vijfassige review uitgevoerd; de gebruiker heeft de volledige slice op 29 oktober 2026 geaccepteerd, inclusief de selectieacties en globaal samenvoegen. Zie de [checklist](../manual-tests/milestone-52-checklist.md). PR #54 wacht op merge door de gebruiker.

## Eerste slice

- Herkenbaar beheervenster zoals Auteurs en Series beheren.
- Tags doorzoeken met het aantal gekoppelde boeken en de bijbehorende boeken bekijken.
- Eén tag expliciet hernoemen voor alle gekoppelde boeken, met bevestiging.
- Overige tags, hun volgorde en alle overige boekgegevens behouden.
- Voor geselecteerde boeken de brontag vervangen door een nieuwe of bestaande doeltag. Heeft het boek de doeltag al, dan verdwijnt uitsluitend de brontag; geen dubbele koppeling.
- De brontag expliciet uit geselecteerde boeken verwijderen zonder boeken of overige tags te verwijderen.
- Tag overal hernoemen mag ook een bestaande doelnaam gebruiken: voeg alle bronboeken toe aan de bestaande doeltag, verwijder dubbele bronkoppelingen en verwijder daarna de lege brontag. De bestaande doeltagnaam en -identiteit blijven behouden; het scherm toont direct haar volledige boekenlijst en telling.
- Bestaande opslag/bestandssynchronisatie en begrijpelijke waarschuwingen hergebruiken.
- Zoek-/lijstfocus, paginering en geladen omslagen behouden.

Splitsen, zelfstandig globale tagrecords verwijderen en online metadata blijven afzonderlijke vervolgstappen. Samenvoegen door expliciet overal naar een bestaande tag te hernoemen is na aanvullende gebruikersfeedback onderdeel van deze slice. Het wijzigen/verwijderen van boek-tagkoppelingen voor een expliciete selectie is eveneens onderdeel van deze slice.

Concrete acceptatie, opslagkeuzes en tests: [Milestone 52-specificatie](../superpowers/specs/2026-10-29-saga-milestone-52-tag-management-design.md). De afzonderlijke gebruikerswens voor [auteursvarianten samenvoegen](author-variants.md) is voor later vastgelegd en verandert deze slice niet.
