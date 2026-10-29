# Tags beheren

## Status

Milestone 52 is gebouwd na het scopeakkoord van 29 oktober 2026, vanaf geaccepteerde en gemergede PR #53 / main e8e024c. Beschikbaar via het tagicoon naast Auteurs en Series in de hoofdwerkbalk. De database behoudt dezelfde tagidentiteit en koppelingen; bestandssynchronisatie gebruikt de bestaande BookService-route. Geen schemawijziging of bibliotheekconversie.

1024 tests en zeventien interactieve WPF-scenario's slagen. Debug-build 2026.10.29.25 van 29 oktober 10:35:47 (Europe/Amsterdam) heeft 0 waarschuwingen/fouten en is de enige Saga.exe onder Builds. Eigen vijfassige review uitgevoerd; handmatige gebruikersacceptatie staat open in de [checklist](../manual-tests/milestone-52-checklist.md).

## Eerste slice

- Herkenbaar beheervenster zoals Auteurs en Series beheren.
- Tags doorzoeken met het aantal gekoppelde boeken en de bijbehorende boeken bekijken.
- Eén tag expliciet hernoemen voor alle gekoppelde boeken, met bevestiging.
- Overige tags, hun volgorde en alle overige boekgegevens behouden.
- Een bestaande andere doelnaam blokkeren, zonder stil samenvoegen.
- Bestaande opslag/bestandssynchronisatie en begrijpelijke waarschuwingen hergebruiken.
- Zoek-/lijstfocus, paginering en geladen omslagen behouden.

Samenvoegen, splitsen, verwijderen en online metadata zijn afzonderlijke vervolgstappen.

Concrete acceptatie, opslagkeuzes en tests: [Milestone 52-specificatie](../superpowers/specs/2026-10-29-saga-milestone-52-tag-management-design.md). De afzonderlijke gebruikerswens voor [auteursvarianten samenvoegen](author-variants.md) is voor later vastgelegd en verandert deze slice niet.
