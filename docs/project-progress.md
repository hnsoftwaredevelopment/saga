# Saga: voortgang bijhouden

Het door de gebruiker geaccepteerde Excel-overzicht staat lokaal in `artifacts\outputs\saga-roadmap-20261103\Saga-voortgang.xlsx`. De gebruiker wil dit bestaande bestand blijven gebruiken voor het overzicht van onderwerpen, slices, Done/Todo en de grafiek. Het bestand is een lokaal werkdocument en staat niet in Git.

Werk het bestaande bestand gericht bij aan het begin en einde van een slice en bij gebruikersacceptatie/merge. Behoud gebruikerswijzigingen, Excel-tabel, filters, formules, validaties en de grafiek. Bouw het overzicht niet opnieuw op vanuit een oude dataset. De tabbladen zijn **Voortgang** en **Slices**; zoek slices op ID in plaats van op een vast rijnummer.

- **Status** blijft Done of Todo. Een gebouwde slice wacht op gebruikersacceptatie en blijft tot die acceptatie Todo.
- **Fase** geeft bijvoorbeeld In uitvoering, Te testen of Afgerond aan. Een geaccepteerde slice kan Done zijn terwijl de PR nog open is; registreer de merge vervolgens afzonderlijk.
- Verwachte toekomstige slices zijn schattingen van de bekende scope. Voeg nieuwe wensen toe met een passende basis/bron en markeer die niet als goedgekeurd zonder besluit van de gebruiker.
- Percentages tellen slices, geen uren of inspanning. Ze zijn een voortgangsbeeld van de bekende scope, geen opleveringsdatum.

Stand na gebruikersacceptatie van Milestone 57, 3 november 2026: 60 Done en 42 Todo, samen 102 slices over 17 onderwerpen; afgerond 59% en 41%. Milestone 56 is geaccepteerd en PR #58 gemerged. Milestone 57 staat op Done / Afgerond; PR #59 is op 3 november 2026 gemerged. De tabel/filter, validatie en grafiek zijn na de wijziging in Excel gecontroleerd zonder het bestand tijdens die controle op te slaan.

Milestone 58: het bestaande pipeline-item P22 is op 3 november 2026 na goedkeuring omgezet naar M58 (Seriesbeheer, volgnummers van meerdere boeken). De gebruiker heeft de slice op 3 november 2026 geaccepteerd, met twee niet-uitgevoerde handmatige controles (bestandswaarschuwing en conflict/rollback), geautomatiseerd afgedekt. Status Done, fase Afgerond; PR #60 is op 3 november 2026 gemerged. De actuele telling is 61 Done en 41 Todo van 102 slices, afgerond 60% en 40%.

Milestone 59: pipeline-item P23 is na expliciete goedkeuring op 3 november 2026 omgezet naar M59 (zelf gekozen series samenvoegen). De gebruiker heeft op 3 november 2026 alle checklisttests uitgevoerd en de slice geaccepteerd: alles werkt zoals beschreven. M59 staat op Done / Afgerond. PR #61 is op 10 november 2026 gemerged. De actuele telling is 62 Done / 40 Todo van 102 slices, afgerond 61% / 39%. PR #60 is gemerged en M58 staat op Afgerond.

Milestone 60: pipeline-item P24 is op 10 november 2026 omgezet naar M60 (een serie afsplitsen). Op dezelfde dag zijn alle handmatige tests geslaagd, heeft de gebruiker de slice geaccepteerd en is PR #62 gemerged. M60 staat op Done / Afgerond. De telling is 63 Done en 39 Todo van 102 slices, afgerond 62% en 38%.

Milestone 61: de proef **Tag splitsen** is op 10 november 2026 ingetrokken en PR #63 is gesloten. In het voortgangsoverzicht is de rij teruggezet naar P26 met het voorstel **Tags toevoegen aan geselecteerde boeken**. De bestaande tags van elk boek blijven bij die voorgestelde actie behouden. P26 staat op Todo / Later. De telling blijft 63 Done en 39 Todo.

Milestone 62: pipeline-item P26 is uitgevoerd als **Tags toevoegen aan geselecteerde boeken**. De gebruiker heeft de herziene invoer op 10 november 2026 geaccepteerd en PR #64 dezelfde dag gemerged. M62 staat op Done / Afgerond. De telling is 64 Done en 38 Todo van 102 slices, afgerond 63% en 37%.

Milestone 63: **Serie wijzigen** is toegevoegd als afzonderlijke uitbreiding van Milestone 60. M63 staat op Todo / Te testen. Het overzicht telt 64 Done en 39 Todo van 103 slices, afgerond 62% en 38%.
