# Milestone 39 handmatige checklist

## Voorbereiding

- [X] Start de actuele Debug-versie van `Builds\Debug\Saga.exe`.
- [X] Open een bibliotheek met minstens vier boeken onder `Ontbrekende auteur` en minstens één reeds bekende auteur.
- [X] Reserveer twee boeken voor de bulkronde met een bekende auteur en twee andere boeken voor de bulkronde met een nieuwe auteur.
- [X] Open het kwaliteitsscherm en kies `Ontbrekende auteur`.

## Selectie en beschikbare acties

- [X] Selecteer met `Ctrl` meerdere niet-aaneengesloten boeken.
- [X] Selecteer met `Shift` een aaneengesloten reeks boeken.
- [X] Controleer dat `Auteur wijzigen` bij meerdere geselecteerde boeken beschikbaar is.
- [X] Controleer dat openen, taal, serie, titel/auteur, omslag en tags bij een bulkselectie niet beschikbaar zijn.
- [X] Controleer dat `Dit is correct` beschikbaar blijft voor de geselecteerde kwaliteitsmeldingen.

## Annuleren

- [X] Kies `Auteur wijzigen` met meerdere boeken geselecteerd.
- [X] Controleer dat het venster het juiste aantal geselecteerde boeken toont.
- [X] Controleer dat de bevestigingsknop vermeldt voor hoeveel boeken de auteur wordt gewijzigd.
- [X] Sluit met `Annuleren` of `Escape` en controleer dat geen boek is gewijzigd en de selectie behouden blijft.

## Bekende auteur toepassen

- [X] Open het auteursvenster opnieuw en typ een deel van een bekende auteursnaam.
- [X] Controleer dat de suggesties direct en correct worden gefilterd.
- [X] Kies de auteur met muis of `Enter` en controleer dat de volledige naam in het invoerveld staat.
- [X] Bevestig en controleer dat alle geselecteerde boeken uit `Ontbrekende auteur` verdwijnen.
- [X] Controleer dat de resultaatsamenvatting de juiste aantallen toont.
- [X] Sluit het kwaliteitsscherm en controleer in de hoofdbibliotheek dat alle boeken de gekozen auteur tonen.
- [X] Controleer dat het auteursfilter direct de nieuwe aantallen toont en `Unknown` voor deze boeken verdwenen is.

## Nieuwe auteur en enkelvoudige regressie

- [X] Selecteer meerdere andere boeken zonder auteur en voer een volledig nieuwe auteursnaam in.
- [X] Controleer dat geldige vrije invoer kan worden bevestigd en op alle geselecteerde boeken verschijnt.
- [X] Open de actie daarna voor één boek en controleer dat de boektitel wordt getoond en de knop alleen `Auteur wijzigen` zegt.
- [X] Controleer dat het bestaande enkelvoudige herstel nog normaal werkt.

## Toetsenbord, talen en foutafhandeling

- [X] Doorloop selectie, auteursuggestie, bevestigen en annuleren volledig met het toetsenbord.
- [X] Schakel Saga naar een andere taal en controleer bulkcontext, knop en resultaatsamenvatting.
- [X] Indien een opslag- of write-backfout veilig kan worden nagebootst: controleer dat volledig mislukte boeken zichtbaar en geselecteerd blijven en dat bestandswaarschuwingen duidelijk worden gemeld.
- [X] Controleer kort dat de overige kwaliteitscategorieën en herstelacties nog normaal werken met één geselecteerd boek.

## Resultaat

- [X] Alle bovenstaande controles zijn geslaagd, of afwijkingen zijn met de exacte stappen genoteerd.

## Bevinding tijdens de test

- [X] Een volledig geslaagde bulkwijziging toont alleen een korte groene bevestiging.
- [X] Een resultaat met fouten, waarschuwingen of overgeslagen boeken toont de uitgebreide rode uitsplitsing.

Deze verbetering is handmatig bevestigd met Debug-build `2026.10.6.8`.
