# Series beheren

## Status

Milestone 50 in voorbereiding na geaccepteerde en gemergede PR #50 (Milestone 49). Richting door gebruiker verwelkomd; [concrete specificatie](../superpowers/specs/2026-10-24-saga-milestone-50-series-management-design.md) ter bevestiging. Nog geen productcode, schema of nieuwe build gewijzigd.

## Eerste slice

- Herkenbaar beheervenster zoals Auteurs beheren, met zoeken en aantallen.
- Groeperen zoals het huidige seriefilter: namen trimmen en hoofdletterongevoelig vergelijken; geen gelijkenisherkenning.
- Boeken per serie met serienummer, titel en auteur(s), numeriek gesorteerd en gepagineerd; nummers ontbreken onderaan.
- Volledige serie expliciet hernoemen met bevestiging en behoud van serienummers en alle overige boekgegevens.
- Een bestaande andere doelserie blokkeren; geen samenvoegen.
- Transactionele databasewijziging, bestaande bestandsverwerking met aparte waarschuwingen en directe actualisatie van hoofdscherm/filter.
- Geen serietabel of bibliotheekconversie; geen foto-/biografieprofiel voor series in deze slice.

## Buiten scope

Serienummers wijzigen/opnieuw toekennen, series samenvoegen/splitsen, lege series aanmaken, online seriegegevens, ontbrekende delen herkennen, leesvolgorde over meerdere series en nieuwe ebookmetadatawriters. Losse boekdetails en bulkbewerking behouden hun bestaande betekenis.
