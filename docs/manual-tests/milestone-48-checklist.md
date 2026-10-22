# Milestone 48: auteursbeheer

## Status en testbuild

Handmatige gebruikersacceptatie: nog niet uitgevoerd. Start de actuele Builds/Debug/Saga.exe, versie **2026.10.22.88**, gebouwd op **22 oktober 2026 om 15:38 (Europe/Amsterdam)**. Build: 0 waarschuwingen en fouten; precies één Saga.exe onder Builds. Volledige testset: 911 geslaagd, 0 mislukt of overgeslagen. Eigen review en WPF-render-/beeldverwerkingscontrole uitgevoerd met fictieve gegevens. Gebruik bij voorkeur een kopie van de bibliotheek voor verwijderen/ongeldige bestanden en behoud een back-up vóór gebruik van een nieuwe versie met een additieve databasewijziging.

## Openen, zoeken en navigatie

- [ ] Open Auteurs beheren vanuit de hoofdwerkbalk. De lijst toont auteurs en boekaantallen van de huidige bibliotheek, zonder onbekend-auteurplaatsaanduiding.
- [ ] Typ enkele letters; de lijst wordt vlot gefilterd. Vul aan en wis de tekst. Een zoekterm zonder resultaat geeft een duidelijke lege toestand.
- [ ] Kies een auteur. Naam, profiel en boeken horen bij dezelfde auteur; boeken met meerdere auteurs tellen ook mee. Een naam die slechts gedeeltelijk overeenkomt levert geen boeken van een andere auteur op.
- [ ] Meer laden toont volgende auteurs/boeken wanneer beschikbaar. Scrollen of zoeken laadt niet alle auteursfoto's tegelijk.
- [ ] Versleep de zichtbare splitter. Verklein het venster; labels, velden, boeken en knoppen blijven bereikbaar.

## Profiel onderhouden

- [ ] De auteursnaam is alleen-lezen. Vul een website en korte biografie in; Opslaan geeft een korte groene bevestiging.
- [ ] Sluit en heropen het venster; de opgeslagen waarden staan er nog. Boekmetadata en auteursnamen zijn ongewijzigd.
- [ ] Website openen opent het ingevulde adres in de standaardbrowser. Zonder geldig adres is de knop niet beschikbaar.
- [ ] Vul bijvoorbeeld file:///c:/test als website in. Opslaan wordt begrijpelijk geweigerd en de geldige opgeslagen waarde blijft behouden.
- [ ] Maak website en biografie leeg en sla op; beide velden zijn na heropenen leeg.
- [ ] Wijzig een veld en kies Annuleren onderaan: de vorige opgeslagen waarden verschijnen weer.
- [ ] Wissel van auteur of sluit met wijzigingen: Ja slaat op, Nee verwerpt, Annuleren houdt het concept geopend. Controleer ook sluiten met Escape.

## Foto

- [ ] Kies een JPEG of PNG. Het voorbeeld verschijnt; pas na Opslaan blijft de foto bewaard bij heropenen.
- [ ] Vervang een foto en annuleer de wijzigingen: de oude foto verschijnt weer. Vervang daarna en sla op: de nieuwe foto blijft bewaard.
- [ ] Foto verwijderen en Opslaan verwijdert de verwijzing; na heropenen staat er Geen foto. Het oorspronkelijke gekozen fotobestand blijft bestaan.
- [ ] Kies een ongeldig/beschadigd afbeeldingsbestand of een bestand groter dan 10 MB. De melding is begrijpelijk; de vorige foto blijft intact.
- [ ] Optioneel op een tijdelijke bibliotheekkopie: verplaats de bibliotheek naar een andere map en open deze daar. De opgeslagen profielfoto blijft werken zonder het oorspronkelijke fotobestand.

## Behoud en regressies

- [ ] Optioneel op een tijdelijke bibliotheekkopie: verwijder of wijzig de auteur van het laatste boek van een auteur met profielinhoud. Het profiel blijft met nul boeken beschikbaar. Koppel later een boek aan precies die auteur; de profielgegevens blijven behouden.
- [ ] Controleer bestaande hoofdscherm-, import-, kwaliteits- en instellingenroutes kort; deze slice verandert hun gedrag niet.
- [ ] Tab, Shift+Tab, Enter en Escape werken herkenbaar. Controleer ook een andere taal en een ander thema.

## Moeilijk handmatig op te wekken situaties

Opslagconflict, leesfout, fout vóór/na databasecommit en annulering tijdens schrijven zijn automatisch afgedekt. Geen echte bibliotheek beschadigen of een database extern aanpassen om deze fouten op te wekken. Noteer niet-uitgevoerde handmatige scenario's expliciet; ze worden niet vooraf als geslaagd beschouwd.
