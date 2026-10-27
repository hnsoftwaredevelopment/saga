# Meervoudige selectie in de boekenkast

## Status

Milestone 51 is gebouwd volgens het door de gebruiker goedgekeurde voorstel. De boekenkast heeft dezelfde centrale selectie als de bestaande bulkbewerking: klik, Ctrl, Shift en Ctrl+Shift; duidelijke selectie- en focusranden; een telling en de bestaande metadata-actie boven de tegels. De [handmatige checklist](../manual-tests/milestone-51-checklist.md) staat open voor build 2026.10.27.23. 993 tests en tien interactieve WPF-scenario's slagen.

## Werkwijze en grenzen

- Pijltjes/Home/End navigeren tussen tegels, Shift breidt uit, Ctrl+pijltje verplaatst alleen focus, Ctrl+Spatie schakelt selectie om en Escape wist haar.
- Open groepen bepalen de reeksvolgorde. Dubbele vermeldingen delen één boek-ID; een boek telt één keer. Voor reeksen geldt de eerste zichtbare vermelding per ID. Inklappen wist bestaande bewuste selecties niet; gesloten groepen worden niet door een nieuwe Shift-reeks meegenomen.
- Filteren bewaart de doorsnede met zichtbare resultaten. Wissen van de zoektekst herstelt verwijderde selecties niet. Sorteren, hergroeperen en wisselen van weergave behouden de overblijvende geselecteerde IDs.
- De bestaande metadata-bewerking, opslag, waarschuwingen en selectie van mislukte boeken worden hergebruikt. Alleen geselecteerde unieke boeken worden verwerkt; geen nieuwe opslagroute of automatische wijziging bij selecteren.
- Virtualisatie en begrensd omslagladen blijven behouden. Navigatie naar een niet-gerealiseerde tegel wordt door het bestaande wrap-panel ondersteund; er worden geen extra boeken/omslagen geladen om de selectie te bepalen.

Ontwerp en verificatie: [Milestone 51-specificatie](../superpowers/specs/2026-10-27-saga-milestone-51-bookshelf-selection-design.md). Geen nieuw schema, externe dienst, dependency, bewerkingsfunctie of e-readerondersteuning in deze slice.
