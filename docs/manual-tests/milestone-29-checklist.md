# Milestone 29 Handmatige Testchecklist

Gebruik bij voorkeur een testbibliotheek of een bibliotheek met een actuele back-up. Deze workflow wijzigt de auteur in SQLite, de draagbare `metadata.json`-sidecar en ondersteunde ebookmetadata via Saga's bestaande opslagroute.

De kernworkflow met auteursuggesties, vrije invoer, annuleren, directe dashboardupdate en bijgewerkte hoofdbibliotheek is op 1 september 2026 door de gebruiker goedgekeurd. De knoptekst is op verzoek verduidelijkt naar `Auteur wijzigen`.

## Beschikbaarheid en venster

- [ ] Open de Quality Page en selecteer `Ontbrekende auteur`.
- [ ] Controleer dat `Auteur wijzigen` alleen beschikbaar is wanneer een boek in deze categorie is geselecteerd.
- [ ] Selecteer een andere kwaliteitscategorie en controleer dat de actie niet beschikbaar is.
- [ ] Open het herstelvenster en controleer de boektitel, uitleg, auteursinvoer en knoppen.
- [ ] Controleer dat de focus in de auteursinvoer staat.

## Bekende auteur kiezen

- [ ] Typ enkele letters van een auteur die al in de actieve bibliotheek voorkomt.
- [ ] Controleer dat treffers die met de invoer beginnen vóór andere gedeeltelijke treffers staan.
- [ ] Controleer dat lege waarden, `Unknown` en dubbele schrijfwijzen niet als suggestie verschijnen.
- [ ] Kies een suggestie met de muis en controleer dat de bekende schrijfwijze wordt overgenomen.
- [ ] Herhaal dit met pijltoetsen en `Enter`.
- [ ] Sla op en controleer dat uitsluitend de auteur van het geselecteerde boek wijzigt.

## Nieuwe auteur invoeren

- [ ] Open een ander testboek zonder auteur.
- [ ] Voer een auteursnaam in die nog niet in de bibliotheek voorkomt.
- [ ] Controleer dat de getrimde nieuwe naam kan worden opgeslagen.
- [ ] Open de herstelactie voor een volgend boek en controleer dat de nieuwe auteur nu als suggestie beschikbaar is.
- [ ] Controleer dat lege invoer, alleen spaties en `Unknown` niet kunnen worden opgeslagen.

## Annuleren en toetsenbord

- [ ] Wijzig de invoer en kies `Annuleren`; controleer dat het boek ongewijzigd blijft.
- [ ] Herhaal dit met `Escape`.
- [ ] Bedien invoer, suggesties, opslaan en annuleren volledig met het toetsenbord.
- [ ] Controleer dat focus na sluiten logisch terugkeert naar de Quality Page.

## Directe herevaluatie

- [ ] Controleer dat de herstelde rij direct uit `Ontbrekende auteur` verdwijnt.
- [ ] Controleer dat categorie- en totaalaantallen direct veranderen.
- [ ] Controleer dat de logisch volgende rij wordt geselecteerd, of niets wanneer de categorie leeg is.
- [ ] Controleer dat andere geldige kwaliteitsmeldingen voor hetzelfde boek zichtbaar blijven.
- [ ] Controleer dat een eerder via `Dit is correct` genegeerde melding niet opnieuw verschijnt.
- [ ] Controleer dat de nieuwe auteur direct in de hoofdbibliotheek en het auteursfilter staat.

## Bibliotheekgrenzen en fouten

- [ ] Wissel van bibliotheek en controleer dat alleen auteurs uit de actieve bibliotheek als suggestie verschijnen.
- [ ] Controleer dat een boek dat inmiddels een geldige auteur heeft niet via deze herstelroute wordt overschreven.
- [ ] Simuleer indien praktisch een opslagfout en controleer dat Saga een duidelijke melding toont en de actuele opgeslagen toestand blijft weergeven.
- [ ] Simuleer indien praktisch dat de auteur wel wordt opgeslagen maar een bijbehorend metadata- of ebookbestand niet kan worden bijgewerkt; controleer dat de kwaliteitsrij verdwijnt en Saga een duidelijke waarschuwing toont.

## Lokalisatie

- [ ] Controleer de Nederlandse teksten, inclusief `Auteur wijzigen`, op begrijpelijkheid.
- [ ] Wissel steekproefsgewijs naar een andere ondersteunde taal en controleer dat geen interne sleutel zichtbaar is.

## Eindresultaat

- [ ] Alle relevante controles zijn geslaagd zonder regressie in `Dit is correct`, openen, dubbelklik, splitter, filters of bibliotheekwissel.
