# Milestone 36 Handmatige Testchecklist

Gebruik uitsluitend de actuele Debug-build uit `Builds\Debug\Saga.exe`.

## Voorstel en bewerking

- [ ] Open de Quality Page en kies `Rommelige tags`.
- [ ] Selecteer een boek met tags die komma’s, overtollige spaties, lege waarden of dubbele waarden bevatten.
- [ ] Controleer dat alleen `Tags opschonen` als specifieke herstelactie zichtbaar is.
- [ ] Kies `Tags opschonen` en controleer dat de cursor direct in `Nieuwe tags` staat.
- [ ] Controleer dat de huidige tags alleen-lezen zijn en dat het voorstel één opgeschoonde tag per regel toont.
- [ ] Controleer dat waarden rond komma’s zijn gesplitst, overtollige witruimte is verwijderd en dubbele tags slechts eenmaal voorkomen.
- [ ] Pas het voorstel handmatig aan en controleer dat `Tags wijzigen` alleen actief is wanneer de uiteindelijke lijst werkelijk verschilt.

## Opslaan en annuleren

- [ ] Kies `Annuleren` en controleer dat tags, wijzigingsdatum en Quality Page ongewijzigd blijven.
- [ ] Open het venster opnieuw, wijzig de tags en kies `Tags wijzigen`.
- [ ] Controleer dat het boek direct uit `Rommelige tags` verdwijnt en dat telling en selectie logisch worden bijgewerkt.
- [ ] Open het boek in het hoofdscherm en controleer dat de nieuwe tags direct zichtbaar zijn zonder Saga opnieuw te starten.
- [ ] Sluit en heropen Saga en controleer dat de opgeschoonde tags behouden blijven.

## Lege lijst en uitzonderingen

- [ ] Verwijder bij een testboek alle voorgestelde regels en controleer dat een lege taglijst kan worden opgeslagen.
- [ ] Controleer daarna dat het boek geen tags meer toont en niet opnieuw onder `Rommelige tags` verschijnt.
- [ ] Gebruik bij een boek waar een komma bewust bij de tag hoort `Dit is correct`; controleer dat de metadata niet wijzigt en de melding verdwijnt.
- [ ] Herstel die uitzondering via Instellingen en controleer dat de melding opnieuw op de Quality Page verschijnt.

## Toetsenbord en regressie

- [ ] Bedien het venster met `Tab`, `Shift+Tab`, `Enter` en `Escape`; controleer dat focus, opslaan en annuleren logisch werken.
- [ ] Maak het venster kleiner en groter en controleer dat beide tagvelden en de actieknoppen bereikbaar blijven.
- [ ] Controleer dat titel, auteur, omslag, taal, uitgever, beschrijving, serie, serienummer, ISBN, leesstatus en formaten niet zijn gewijzigd.
- [ ] Controleer kort dat `Auteur wijzigen`, `Taal wijzigen`, `Serie wijzigen`, `Titel en auteur omwisselen`, `Omslag zoeken` en `Openen in bibliotheek` nog in hun eigen context werken.
