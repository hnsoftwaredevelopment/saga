# Milestone 42 handmatige testchecklist

Gebruik de actuele Debug-build:

`Builds\Debug\Saga.exe` — versie `2026.10.21.23`

## Voorbereiding

- [ ] Maak zo nodig eerst een bibliotheekback-up of kies testboeken waarvan de metadata veilig gewijzigd mag worden.
- [ ] Kies een gewoon boek in de bibliotheek; het hoeft niet op de Quality Page als mogelijk omgewisseld te zijn gemeld.
- [ ] Noteer titel, auteur(s) en enkele andere velden, bijvoorbeeld taal, serie en tags.

## Knop en bediening

- [ ] Naast het veld `Titel` staat een knop met twee pijlen.
- [ ] De tooltip van de knop is `Wissel titel en auteur om`.
- [ ] De knop werkt met de muis.
- [ ] De knop is met Tab bereikbaar en werkt met Enter of Spatie.

## Omwisselen en ongedaan maken

- [ ] Klik bij een boek met één auteur op de omwisselknop. De auteur wordt de nieuwe titel en de voormalige titel wordt de nieuwe auteur.
- [ ] De wijziging blijft eerst alleen in het detailscherm staan; zij wordt niet automatisch opgeslagen.
- [ ] Kies `Ongedaan maken`. De oorspronkelijke titel en auteur komen terug.
- [ ] Taal, serie, serienummer, tags, omschrijving en bestandsinformatie zijn niet gewijzigd.

## Meerdere auteurs en leestekens

- [ ] Test een boek met meerdere auteurs. Na omwisselen vormen de auteurs samen één leesbare nieuwe titel.
- [ ] Test zo mogelijk een titel met een puntkomma, bijvoorbeeld `Deel 1; Deel 2`. Na omwisselen en opslaan blijft dit precies één auteur en wordt de tekst niet gesplitst.

## Opslaan en verversen

- [ ] Wissel titel en auteur opnieuw om en kies `Opslaan`.
- [ ] De zichtbare boekregel toont direct de nieuwe titel en auteur.
- [ ] Hetzelfde boek blijft geselecteerd wanneer het nog binnen de actieve weergave valt.
- [ ] De auteursfilters zijn bijgewerkt: ongebruikte oude auteurs verdwijnen en de nieuwe auteur verschijnt met het juiste aantal.
- [ ] Herhaal dit met een actieve zoektekst die vóór de wijziging op de oude auteur zoekt. De gefilterde lijst wordt na opslaan opnieuw correct opgebouwd.
- [ ] Sluit Saga af, start opnieuw en controleer dat de gewijzigde titel en auteur bewaard zijn gebleven.

## Resultaat

- [ ] Alle controles zijn geslaagd.
- [ ] Eventuele afwijkingen zijn genoteerd met het gekozen boek, de stappen en de zichtbare melding.
