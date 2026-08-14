# Milestone 21 - Filterlijst Zoeken

## Voorbereiding

- Start Saga met een bibliotheek met voldoende auteurs, series, tags en eventueel custom metadatawaarden.
- Gebruik bij voorkeur een grote bibliotheek, zodat lange filterlijsten zichtbaar zijn.

## Testen

- Open het filterpaneel links.
- Klap `Auteurs`, `Series`, `Tags`, `Taal`, `Type` en een custom metadatafilter open.
- Controleer dat korte filterlijsten geen zoekveld tonen.
- Controleer dat lange filterlijsten wel een zoekveld tonen.
- Typ in het zoekveld van een filtergroep en controleer dat alleen matchende filteritems zichtbaar blijven.
- Controleer dat de teller naast het zoekveld wijzigt, bijvoorbeeld `3 / 120`.
- Selecteer een filteritem, zoek daarna binnen dezelfde filtergroep naar iets anders en controleer dat de selectie behouden blijft.
- Maak het zoekveld leeg en controleer dat alle filteritems weer zichtbaar zijn.
- Controleer dat de algemene bibliotheekzoekbalk boven de boekenlijst nog los werkt van de zoekvelden in het filterpaneel.

## Verwacht Resultaat

- Filterzoekvelden werken direct terwijl je typt.
- Niet-matchende filteritems verdwijnen uit de lijst zonder dat selecties verloren gaan.
- De boekenlijst wordt alleen gefilterd door geselecteerde filters en de algemene zoektekst, niet door de filterzoektekst zelf.
