# Milestone 31 Handmatige Testchecklist

Gebruik uitsluitend de actuele Debug-build uit `Builds\Debug\Saga.exe`.

## Hoofdroute

- [ ] Open een bibliotheek met minstens één boek dat wel een serienummer maar geen serienaam heeft.
- [ ] Open de Quality Page en selecteer `Serienummer zonder serie`.
- [ ] Controleer dat alleen de herstelknop `Serie wijzigen` zichtbaar is en dat deze actief wordt zodra een boek is geselecteerd.
- [ ] Open `Serie wijzigen` en controleer dat de juiste boektitel en het bestaande serienummer zichtbaar zijn.
- [ ] Typ een deel van een bestaande serienaam en controleer dat passende series uit de actieve bibliotheek als suggestie verschijnen.
- [ ] Kies een suggestie met het toetsenbord, sla op en controleer dat de melding voor dit boek direct verdwijnt.
- [ ] Controleer in het hoofdscherm dat de serienaam is gewijzigd en dat het serienummer gelijk is gebleven.
- [ ] Controleer dat het seriefilter direct de nieuwe waarde en telling toont.

## Nieuwe serienaam en validatie

- [ ] Herhaal de route voor een ander boek en voer een volledig nieuwe serienaam in.
- [ ] Controleer dat de nieuwe naam na opslaan in de bibliotheek en het seriefilter verschijnt.
- [ ] Controleer dat alleen spaties invoeren de knop `Opslaan` uitgeschakeld laat.

## Annuleren en gegevensveiligheid

- [ ] Wijzig de invoer en annuleer met `Annuleren`; controleer dat niets is opgeslagen.
- [ ] Open het venster opnieuw en annuleer met `Escape`; controleer opnieuw dat niets is opgeslagen.
- [ ] Controleer bij het herstelde boek dat titel, auteur, taal, tags, omslag en leesstatus niet zijn gewijzigd.
- [ ] Verander de serienaam van het boek buiten de open Quality Page, probeer daarna de oude melding te herstellen en controleer dat Saga de geldige actuele serienaam niet overschrijft.

## Toetsenbord en venstergrootte

- [ ] Doorloop de volledige route met `Tab`, pijltjestoetsen, `Enter` en `Escape` zonder de muis te gebruiken.
- [ ] Controleer dat de invoer direct focus krijgt en dat `Enter` de geselecteerde suggestie overneemt.
- [ ] Maak het herstelvenster smaller en groter en controleer dat titel, serienummer, uitleg en knoppen leesbaar en bereikbaar blijven.
- [ ] Maak de Quality Page smaller en controleer dat de onderste acties bruikbaar blijven bij langere teksten.

## Foutafhandeling en regressie

- [ ] Controleer, indien praktisch na te bootsen, dat een opslagfout een duidelijke melding geeft en de actuele boekgegevens zichtbaar laat.
- [ ] Controleer dat `Auteur wijzigen`, `Taal wijzigen`, `Dit is correct` en `Openen in bibliotheek` nog steeds werken in hun eigen context.
- [ ] Sluit Saga, open dezelfde bibliotheek opnieuw en controleer dat de opgeslagen serienaam en het serienummer behouden zijn.
