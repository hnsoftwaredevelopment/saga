# Series beheren

## Status

Milestone 50 is gebouwd op de goedgekeurde [specificatie](../superpowers/specs/2026-10-24-saga-milestone-50-series-management-design.md), vanaf geaccepteerde en gemergede PR #50. Beschikbaar via **Series beheren** in de hoofdwerkbalk. De gebruiker heeft de hoofdroute afgevinkt. Debug-build 2026.10.24.61 herstelt de resterende toetsenbordfocus bij laden en hernoemen, zonder een nieuwere focuskeuze of het hernoemvenster te verstoren. 980 geautomatiseerde tests en vijf interactieve WPF-focusscenario's slagen. Alleen de gerichte toetsenbordhercontrole in de [handmatige checklist](../manual-tests/milestone-50-checklist.md) staat open.

## Eerste slice

- Herkenbaar beheervenster zoals Auteurs beheren, met zoeken en aantallen.
- Groeperen zoals het huidige seriefilter: namen trimmen en hoofdletterongevoelig vergelijken; geen gelijkenisherkenning.
- Boeken per serie met serienummer, titel en auteur(s), numeriek gesorteerd en gepagineerd; nummers ontbreken onderaan.
- Volledige serie expliciet hernoemen met bevestiging en behoud van serienummers en alle overige boekgegevens.
- Een bestaande andere doelserie blokkeren; geen samenvoegen.
- Transactionele databasewijziging, bestaande bestandsverwerking met aparte waarschuwingen en directe actualisatie van hoofdscherm/filter.
- Geen serietabel of bibliotheekconversie; geen foto-/biografieprofiel voor series in deze slice.

De lijst groepeert alleen bestaande namen; lezen wijzigt de opgeslagen schrijfwijzen niet. Na bevestigd hernoemen hebben alle boeken in de groep dezelfde getrimde naam. Een bestandswaarschuwing noemt de betrokken boeken; de wijziging in de bibliotheek is dan wel opgeslagen. Na sluiten worden hoofdscherm, details en seriefilter via de bestaande route bijgewerkt met behoud van geladen omslagen.

## Buiten scope

Serienummers wijzigen/opnieuw toekennen, series samenvoegen/splitsen, lege series aanmaken, online seriegegevens, ontbrekende delen herkennen, leesvolgorde over meerdere series en nieuwe ebookmetadatawriters. Losse boekdetails en bulkbewerking behouden hun bestaande betekenis.
