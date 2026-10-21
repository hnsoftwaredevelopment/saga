# Milestone 41 handmatige testchecklist: meerdere talen herstellen

Gebruik uitsluitend de actuele Debug-build uit `Builds\Debug\Saga.exe` (versie 2026.10.21.16). Kies bij voorkeur minstens drie boeken onder `Ontbrekende of onbekende taal`, waarvan je de metadata veilig kunt aanpassen.

## Selectie en beschikbare acties

- [X] Open de Quality Page en kies `Ontbrekende of onbekende taal`.
- [X] Selecteer één boek en controleer dat `Taal wijzigen` beschikbaar is.
- [X] Selecteer twee of meer boeken met `Ctrl` of `Shift` en controleer dat `Taal wijzigen` beschikbaar blijft.
- [X] Controleer dat `Openen in bibliotheek`, omslag zoeken, serie wijzigen, tags opschonen en titel/auteur omwisselen bij deze meervoudige selectie niet beschikbaar zijn.
- [X] Controleer kort dat `Dit is correct` bij meerdere geselecteerde regels nog wel beschikbaar is.

## Taalvenster en annuleren

- [X] Open `Taal wijzigen` voor één boek en controleer dat de boektitel zichtbaar is.
- [X] Sluit het venster met `Annuleren` en controleer dat niets is gewijzigd.
- [X] Selecteer opnieuw twee of meer boeken en open `Taal wijzigen`.
- [X] Controleer dat het venster het juiste aantal geselecteerde boeken toont en dat de bevestigingsknop dit aantal noemt.
- [X] Open de talenlijst, typ de eerste letters van een taal en controleer dat de juiste gelokaliseerde taalnaam met taalcode gevonden kan worden.
- [X] Controleer met `Tab`, pijltoetsen, `Enter` en `Escape` dat het venster volledig met het toetsenbord bruikbaar blijft.
- [X] Sluit opnieuw zonder te bevestigen en controleer dat alle geselecteerde boeken ongewijzigd zijn.

## Meerdere boeken wijzigen

- [X] Open het venster opnieuw voor minstens twee boeken, kies bijvoorbeeld `Nederlands (nl)` en bevestig.
- [X] Controleer dat een korte groene melding aangeeft hoeveel boeken zijn gewijzigd.
- [X] Controleer dat de gewijzigde boeken direct uit `Ontbrekende of onbekende taal` verdwijnen.
- [X] Open de gewijzigde boeken in het hoofdscherm en controleer dat de taal direct zichtbaar is zonder Saga opnieuw te starten.
- [X] Controleer in het taalfilter dat de gekozen taal het juiste nieuwe aantal boeken bevat.
- [X] Open voor minstens één gewijzigd boek de bijbehorende `metadata.json` en controleer dat de genormaliseerde taalcode is opgeslagen.

## Veilige hercontrole en regressies

- [X] Laat indien praktisch één geselecteerd boek vóór bevestigen via een andere route al een geldige taal krijgen; controleer dat Saga deze taal niet overschrijft en het boek als overgeslagen behandelt.
- [ ] Controleer dat volledig mislukte boeken zichtbaar en geselecteerd blijven wanneer je een opslagfout veilig kunt nabootsen. Sla deze stap over wanneer dat niet verantwoord mogelijk is.
- [X] Controleer kort dat bulkauteurherstel onder `Ontbrekende auteur` nog werkt.
- [X] Controleer bij één boek dat serie wijzigen, tags opschonen, omslag zoeken, titel/auteur omwisselen en openen in de bibliotheek nog in hun eigen categorie werken.
- [X] Sluit Saga normaal en open de bibliotheek opnieuw; controleer dat de gewijzigde talen behouden zijn.

## Verwacht resultaat

- Eén gekozen geldige taal wordt uitsluitend toegepast op de geselecteerde boeken waarvoor het taalprobleem nog bestaat.
- Annuleren wijzigt niets en bestaande geldige talen worden nooit overschreven.
- Dashboard, hoofdbibliotheek, taalfilters, `metadata.json` en ondersteunde ebookmetadata blijven onderling consistent.
- Succes gebruikt een korte groene melding; een gemengd resultaat noemt de afzonderlijke aantallen zonder een volledig succes voor te stellen.

## Acceptatieresultaat

- [X] Handmatige acceptatie afgerond op 21 oktober 2026.
- De werkwijze is herkenbaar en consistent met de overige herstelpagina's.
- De gekozen taal of talen worden correct ingesteld.
- De opslagfoutcontrole is bewust overgeslagen omdat een veilige fout niet kon worden nagebootst; dit scenario wordt door een geautomatiseerde regressietest afgedekt.
