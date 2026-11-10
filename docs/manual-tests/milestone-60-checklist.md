# Milestone 60 — een serie afsplitsen

Open `Builds\Debug\Saga.exe`, versie **2026.11.10.9** van **10 november 2026 om 10:30:10** (Europe/Amsterdam). Gebruik bij voorkeur een testserie met ten minste drie boeken, waaronder een boek met een decimaal of ontbrekend volgnummer. Noteer de oorspronkelijke titels en nummers.

- [x] Open **Series beheren** en kies de testserie. Zonder boekselectie en met alle boeken geselecteerd is **Geselecteerde boeken afsplitsen** uitgeschakeld. Met één of meer boeken geselecteerd en minstens één boek over is de knop beschikbaar.
- [x] Selecteer een deel van de boeken met Ctrl of Shift. Open het voorstel. Controleer de bronserie, precies de geselecteerde titels met hun volgnummers en het aantal boeken dat achterblijft.
- [x] Het lege naamveld heeft direct de focus. Een lege naam, spaties en de bronnaam met andere hoofdletters houden de bevestigingsknop uitgeschakeld. Typen houdt de focus in het veld.
- [x] Typ een nieuwe naam en druk op Escape. De bronserie, nummers en selectie zijn ongewijzigd en de focus keert terug naar de actie.
- [x] Probeer een al bestaande andere serienaam. Saga meldt het naamconflict. Controleer dat geen boek is verplaatst.
- [x] Geef de selectie een unieke naam en bevestig met Enter. Controleer dat de nieuwe serie geselecteerd is en alleen de gekozen boeken met hun oorspronkelijke volgnummers bevat. De bronserie toont het resterende aantal en de resterende boeken.
- [x] Sluit **Series beheren** en controleer de serievelden in de hoofdweergave en de boekdetails. Controleer ook of **Serie hernoemen**, **Volgnummers toekennen** en **Geselecteerde series samenvoegen** beschikbaar blijven in hun gebruikelijke context.
- [x] Test het voorstel in het lichte en donkere thema. Verklein het venster en controleer of de selectie, invoer en bevestigingsknoppen bereikbaar blijven.

Bij een geslaagde afsplitsing met een bestandswaarschuwing blijven de opgeslagen seriewijzigingen behouden. De melding toont bij hoeveel boeken de bestandsverwerking mislukte en welke boeken het betreft. Een conflict vóór het opslaan laat de serietoewijzingen ongewijzigd.

Geautomatiseerd gecontroleerd met tijdelijke bibliotheken. **1.246 tests en 33 WPF-proeven geslaagd**. De Debug-build heeft 0 waarschuwingen en 0 fouten. Het voorstel is in beide thema’s op de minimale venstergrootte gerenderd en bekeken.

Op 10 november 2026 bevestigde de gebruiker dat alle handmatige tests zijn geslaagd en dat de werkwijze duidelijk is. De gebruiker heeft PR #62 dezelfde dag gemerged. Milestone 60 is afgerond.
