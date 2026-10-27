# Meervoudige selectie in de boekenkast

## Status

Milestone 51 is gebouwd volgens het door de gebruiker goedgekeurde voorstel. De boekenkast heeft dezelfde centrale selectie als de bestaande bulkbewerking: klik, Ctrl, Shift en Ctrl+Shift; duidelijke selectie- en focusranden; een telling en bedieningsuitleg boven de tegels. Bewerken gebeurt met de bestaande knop in de hoofdwerkbalk; er staat geen tweede bewerkingsknop in de boekenkast. De gebruiker heeft de knopcorrectie en selectie over boekenplankgroepen bevestigd, maar ontdekte dat gegroepeerde lijst-/detailrijen slechts één boek selecteerden. Build 2026.10.27.34 herstelt dit met gedeelde muis-/toetsenbordselectie en zichtbare randen. Iedere weergave houdt haar eigen groepering; geselecteerde boek-IDs blijven behouden. 993 tests en dertien interactieve WPF-scenario's slagen. De [handmatige checklist](../manual-tests/milestone-51-checklist.md) blijft gedeeltelijk open voor de resterende groeps-/weergave-/filterpunten, verticale navigatie bij smalle panelen en de nieuwe controle van gegroepeerde lijst-/detailrijen.

## Werkwijze en grenzen

- Pijltjes/Home/End navigeren tussen tegels, Shift breidt uit, Ctrl+pijltje verplaatst alleen focus, Ctrl+Spatie schakelt selectie om en Escape wist haar.
- In gegroepeerde Lijst en Gedetailleerd gelden dezelfde klik-/Ctrl-/Shift-combinaties. Omhoog/omlaag en Home/End navigeren over open groepen; Shift breidt uit, Ctrl verplaatst alleen focus, Ctrl+Spatie schakelt om en Escape wist. Selectie en focus hebben afzonderlijke randen; groepskoppen behouden hun eigen bediening.
- Open groepen bepalen de reeksvolgorde. Dubbele vermeldingen delen één boek-ID; een boek telt één keer. Voor reeksen geldt de eerste zichtbare vermelding per ID. Inklappen wist bestaande bewuste selecties niet; gesloten groepen worden niet door een nieuwe Shift-reeks meegenomen.
- Filteren bewaart de doorsnede met zichtbare resultaten. Wissen van de zoektekst herstelt verwijderde selecties niet. Sorteren, hergroeperen en wisselen van weergave behouden de overblijvende geselecteerde IDs.
- De bestaande metadata-bewerking, opslag, waarschuwingen en selectie van mislukte boeken worden hergebruikt. Alleen geselecteerde unieke boeken worden verwerkt; geen nieuwe opslagroute of automatische wijziging bij selecteren.
- Virtualisatie en begrensd omslagladen blijven behouden. Navigatie naar een niet-gerealiseerde tegel wordt door het bestaande wrap-panel ondersteund; er worden geen extra boeken/omslagen geladen om de selectie te bepalen.

Ontwerp en verificatie: [Milestone 51-specificatie](../superpowers/specs/2026-10-27-saga-milestone-51-bookshelf-selection-design.md). Geen nieuw schema, externe dienst, dependency, bewerkingsfunctie of e-readerondersteuning in deze slice.
