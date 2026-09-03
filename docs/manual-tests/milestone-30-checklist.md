# Milestone 30 Handmatige Testchecklist

Gebruik bij voorkeur een testbibliotheek of een bibliotheek met een actuele back-up. Deze workflow wijzigt de taal in SQLite, de draagbare `metadata.json`-sidecar en ondersteunde ebookmetadata via Saga's bestaande opslagroute.

## Contextafhankelijke acties

- [ ] Open de Quality Page en selecteer `Ontbrekende auteur`; controleer dat alleen `Auteur wijzigen` zichtbaar is.
- [ ] Selecteer `Onbekende taal`; controleer dat alleen `Taal wijzigen` zichtbaar is wanneer een boek is geselecteerd.
- [ ] Selecteer een andere kwaliteitscategorie; controleer dat geen herstelknop zichtbaar is.

## Taal kiezen en opslaan

- [ ] Selecteer onder `Onbekende taal` een boek zonder taal en kies `Taal wijzigen`.
- [ ] Controleer dat het venster de juiste boektitel en begrijpelijke uitleg toont.
- [ ] Open de talenlijst en controleer dat talen met hun naam en taalcode worden getoond.
- [ ] Typ de eerste letters van een taal om snel naar die taal te springen.
- [ ] Controleer dat opslaan pas beschikbaar is nadat een taal is gekozen.
- [ ] Kies een taal, sla op en controleer dat de kwaliteitsrij direct verdwijnt.
- [ ] Herhaal dit voor een boek met een ongeldige bestaande taalwaarde.

## Directe actualisatie

- [ ] Controleer dat categorie- en totaalaantallen direct veranderen.
- [ ] Controleer dat de logisch volgende rij wordt geselecteerd, of niets wanneer de categorie leeg is.
- [ ] Controleer dat andere kwaliteitsmeldingen voor hetzelfde boek zichtbaar blijven.
- [ ] Controleer dat de taal direct in de hoofdbibliotheek en het taalfilter staat.
- [ ] Sluit en heropen Saga en controleer dat de gekozen taal bewaard is gebleven.

## Annuleren en toetsenbord

- [ ] Kies een taal en daarna `Annuleren`; controleer dat het boek ongewijzigd blijft.
- [ ] Herhaal dit met `Escape`.
- [ ] Bedien de talenlijst, opslaan en annuleren volledig met het toetsenbord.
- [ ] Controleer dat de focus na sluiten logisch terugkeert naar de Quality Page.

## Fouten en lokalisatie

- [ ] Controleer dat een boek dat inmiddels een geldige taal heeft niet wordt overschreven.
- [ ] Simuleer indien praktisch een opslagfout en controleer dat Saga een duidelijke melding toont.
- [ ] Simuleer indien praktisch een mislukte bestandsupdate; controleer dat Saga de opgeslagen taal toont en een duidelijke waarschuwing geeft.
- [ ] Controleer de Nederlandse teksten op begrijpelijkheid.
- [ ] Wissel steekproefsgewijs naar een andere ondersteunde interfacetaal en controleer dat geen interne sleutel zichtbaar is.

## Eindresultaat

- [ ] Alle relevante controles zijn geslaagd zonder regressie in `Auteur wijzigen`, `Dit is correct`, openen, dubbelklik, splitter, filters of bibliotheekwissel.
