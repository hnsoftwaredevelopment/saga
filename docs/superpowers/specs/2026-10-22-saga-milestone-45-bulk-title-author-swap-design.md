# Milestone 45: titel en auteur in bulk omwisselen

## Doel en autorisatie

De gebruiker heeft bulk titel/auteur omwisselen met een controleoverzicht en expliciete keuze per boek goedgekeurd en opdracht gegeven te starten. De bestaande heuristiek geeft veel onterechte kandidaten; een melding op zichzelf geeft dus nooit toestemming om te schrijven.

## Gebruikersgedrag

- Meerdere geselecteerde regels bij Mogelijk omgewisselde auteur/titel openen één scrollbaar controleoverzicht.
- Per boek staan huidige titel/auteur en voorgestelde titel/auteur. Alle vinkjes beginnen uit.
- Alleen expliciet aangevinkte, herstelbare boeken worden na bevestiging verwerkt. Zonder vinkjes is bevestigen uitgeschakeld. Annuleren schrijft niets.
- Boeken met meerdere auteurs, onbruikbare auteur of lege titel kunnen niet worden aangevinkt. Ze blijven ongewijzigd.
- Eén geselecteerd boek behoudt de bestaande enkelvoudige bevestiging.
- Tijdens opslag worden de huidige titel en volledige auteurslijst vergeleken met de oorspronkelijke preview. Gewijzigde of verdwenen boeken worden overgeslagen zonder verouderde waarden toe te passen.
- De bestaande service wisselt alleen titel en de enkele auteur om, behoudt overige metadata en gebruikt SQLite, sidecar en ebook-write-back.
- Resultaten worden per gekozen boek verwerkt; een fout onderbreekt de overige boeken niet. Niet aangevinkte regels blijven zichtbaar. Mislukte boeken blijven geselecteerd. Bibliotheek en auteursfilters verversen gezamenlijk.
- Korte groene succesmelding bij volledig succes; aantallen bij fouten, waarschuwingen en overgeslagen boeken.
- Toetsenbordbediening, tekstomloop, virtualisatie en alle zes talen horen bij de slice.

## Structuur en conventies

Application bevat gedeelde herstelbaarheidsregels, snapshotcontrole en opslagservice. Presentation bevat de afzonderlijk aanvinkbare previews en dashboardcoördinatie. App bevat WPF en resources. Tests volgen de bestaande xUnit/FluentAssertions-structuur onder tests/EbookManager.Tests.

Voorbeeld: `Jan Jansen / De verdwenen stad` wordt alleen na aanvinken `De verdwenen stad / Jan Jansen`. Een tweede, niet aangevinkt boek verandert niet.

## Implementatieplan en verificatie

1. Previewmodel en servicecontrole met tests voor standaard uitgevinkte selectie, ongeldige rijen, snapshots en gegevensbehoud.
2. Dashboard: alleen gekozen ids verwerken, fouten isoleren en bestaande batchreconciliatie hergebruiken; tests voor annuleren, gemengde resultaten en bibliotheekverversing.
3. WPF-overzicht en zes vertalingen; gerichte controles en app-build.
4. Volledige tests, eigen review, documentatie, Obsidian, gewone PR en GitHub-controles. Handmatige checklist vóór merge.

Commando's: `dotnet test EbookManager.sln -c Debug --no-restore`, `dotnet build src/EbookManager.App/EbookManager.App.csproj -c Debug --no-restore`, `git diff --check`.

## Grenzen

Geen heuristiekaanpassing, automatisch aanvinken, bulkbewerking vanuit details, nieuwe dependency of schemawijziging. Altijd gegevensbehoud, bestaande opslagroute, tests en Markdown-spiegel controleren. Nooit geheimen, bibliotheken of builds committen. Alleen de gebruiker mergt.
