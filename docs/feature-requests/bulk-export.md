# Geselecteerde boeken exporteren

## Milestone 57

Gebouwd op de [goedgekeurde specificatie](../superpowers/specs/2026-11-03-saga-milestone-57-bulk-export-design.md), vanaf gemergede PR #58. De gebruiker heeft de checklist en aanvullende tests op 3 november 2026 zonder issues uitgevoerd en de slice geaccepteerd; de actuele controlepunten staan in [checklist 57](../manual-tests/milestone-57-checklist.md).

Selecteer boeken in Boekenplank, Lijst of Gedetailleerd, met of zonder groepering. Open **Geselecteerde boeken exporteren** in de hoofdwerkbalk. Het venster gebruikt die selectie; je hoeft geen boeken opnieuw aan te vinken. Zonder selectie is de actie uitgeschakeld.

Kies één beschikbaar formaat en een opslagmap. EPUB is de standaard als dat beschikbaar is. De preview toont per boek het aantal bestanden in dat formaat en welke boeken worden overgeslagen. Alle bestanden in het gekozen formaat worden meegenomen als één boek er meerdere heeft. Annuleren van de mapkiezer bewaart een eerdere mapkeuze. Pas **Exporteren** begint met kopiëren.

## Bestanden en resultaten

De bestaande auteur/titel-bestandsnamen blijven behouden, met opschoning van ongeldige tekens. Bij een bestaande bestands- of mapnaam volgt een genummerde naam. Saga schrijft naar een eigen tijdelijk bestand en maakt de definitieve naam pas na volledig kopiëren beschikbaar, zonder overschrijven. Bronnen, bibliotheekmetadata en boekselectie veranderen niet.

Het venster toont voortgang en de huidige titel. **Annuleren**, Escape of sluiten tijdens kopiëren stopt de verdere verwerking en wacht op het opruimen van het lopende tijdelijke bestand. Reeds voltooide kopieën blijven bestaan. Na afloop zie je per bestand of overgeslagen boek het resultaat en zo nodig een reden. Het volledige doelpad is beschikbaar via de tooltip. Verwachte ontbrekende formaten zijn geen fouten. Foutloze uitvoering geeft een korte groene melding; bij fouten blijven de betrokken regels zichtbaar. Een afgeronde export kan niet onbedoeld nogmaals starten; open het venster opnieuw voor een nieuwe export.

## Technische grenzen en controle

De voorbereiding leest uitsluitend geselecteerde titels, auteurs en bestandsgegevens, met SQL-batches van maximaal 500 IDs. Omslagen en ebookinhoud worden daarvoor niet geladen. Voor de uitvoering vergelijkt Saga een verse databaseweergave met de bevestigde titel, auteurs en gekozen bestanden. Een gewijzigd of verdwenen boek wordt overgeslagen met uitleg. Een ontbrekende bron of kopieerfout bij één bestand blokkeert andere bestanden niet.

Voorbereiden en kopiëren lopen buiten de UI-thread. De resultaatlijst is gevirtualiseerd en voortgangsupdates zijn begrensd. De route gebruikt alleen leesquery's en bestandkopieën; geen databasewijziging, schema, conversie of nieuwe afhankelijkheden. Nieuwe teksten zijn beschikbaar in alle zes talen. De tests gebruiken fictieve bestanden en tijdelijke bibliotheken.

Automatische apparaatherkenning, e-readerondersteuning, formaatconversie, exportsjablonen en een volledige bibliotheek-back-up vallen buiten deze slice. De bestaande export per formaat in boekdetails blijft beschikbaar.

Verificatie: 1189 geautomatiseerde tests en 30 echte WPF-proeven geslaagd, 0 mislukt/overgeslagen. Debug-build **2026.11.3.18** van **3 november 2026 om 10:27:08 Europe/Amsterdam**, 0 waarschuwingen/fouten en precies één Saga.exe onder Builds. Eigen review op correctheid, leesbaarheid, architectuur, veiligheid en prestaties uitgevoerd. Licht/donker op minimale venstergrootte gerenderd en bekeken. De gebruiker heeft op 3 november 2026 de teststappen en aanvullende tests uitgevoerd zonder issues en de gehele slice geaccepteerd. PR #59 staat nog open.
