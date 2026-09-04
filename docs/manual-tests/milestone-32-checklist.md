# Milestone 32 Handmatige Testchecklist

Gebruik uitsluitend de actuele Debug-build uit `Builds\Debug\Saga.exe`.

## Hoofdroute

- [ ] Open een bibliotheek met een boek dat onder `Mogelijk titel/auteur omgewisseld` verschijnt: de titel lijkt op een persoonsnaam, het boek heeft precies één bruikbare auteur en die auteur lijkt niet op een persoonsnaam.
- [ ] Open de Quality Page, kies deze melding en selecteer het boek.
- [ ] Controleer dat alleen de herstelknop `Titel en auteur omwisselen` zichtbaar is en actief wordt.
- [ ] Open de actie en controleer dat `Huidig` de bestaande titel en auteur toont.
- [ ] Controleer dat `Na omwisselen` de huidige auteur als nieuwe titel en de huidige titel als nieuwe auteur toont.
- [ ] Kies `Omwisselen` en controleer dat de melding voor dit boek direct verdwijnt.
- [ ] Controleer in het hoofdscherm dat titel en auteur zijn omgewisseld en dat het auteursfilter direct de nieuwe auteur toont.

## Annuleren en gegevensveiligheid

- [ ] Herhaal de route voor een ander boek en kies `Annuleren`; controleer dat niets is gewijzigd.
- [ ] Open het venster opnieuw en annuleer met `Escape`; controleer opnieuw dat niets is gewijzigd.
- [ ] Controleer na een geslaagde omwisseling dat beschrijving, taal, uitgever, publicatiedatum, tags, serie, serienummer, ISBN, omslag, leesstatus en beschikbare formaten gelijk zijn gebleven.
- [ ] Open `metadata.json` naast het herstelde boekbestand en controleer dat alleen titel en auteur zijn aangepast.
- [ ] Controleer dat het ebookbestand zelf niet wordt gewijzigd; native ebook-write-back is nog niet ondersteund.

## Niet-herstelbare en verouderde meldingen

- [ ] Selecteer binnen deze categorie, indien aanwezig, een boek met een lege of `Unknown` auteur en controleer dat de omwisselknop uitgeschakeld blijft.
- [ ] Corrigeer titel of auteur buiten de nog open Quality Page en probeer daarna de oude melding te herstellen; controleer, indien praktisch na te bootsen, dat Saga de actuele geldige gegevens niet overschrijft.
- [ ] Controleer dat `Dit is correct` beschikbaar blijft wanneer de voorgestelde omwisseling niet gewenst is.

## Toetsenbord, venstergrootte en regressie

- [ ] Doorloop de volledige route met `Tab`, `Enter` en `Escape` zonder de muis te gebruiken.
- [ ] Maak het bevestigingsvenster smaller en groter en controleer dat huidige en nieuwe waarden en beide knoppen leesbaar en bereikbaar blijven.
- [ ] Controleer dat lange titel- en auteursnamen worden afgebroken en volledig leesbaar blijven via de verticale scrollbar.
- [ ] Controleer, indien een gecontroleerde fouttest beschikbaar is, dat een opslag- of sidecarfout een begrijpelijke melding geeft en de actuele boekgegevens zichtbaar laat.
- [ ] Controleer dat `Auteur wijzigen`, `Taal wijzigen`, `Serie wijzigen`, `Dit is correct` en `Openen in bibliotheek` nog steeds werken in hun eigen context.
- [ ] Sluit Saga, open dezelfde bibliotheek opnieuw en controleer dat de omgewisselde titel en auteur behouden zijn.
