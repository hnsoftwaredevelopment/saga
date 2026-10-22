# Milestone 44: tags van meerdere boeken opschonen

## Doel en goedgekeurde scope

De gebruiker heeft de voorgestelde slice goedgekeurd en opdracht gegeven te starten. Selecteer meerdere boeken onder Rommelige tags, bekijk per boek de huidige tags en het voorstel en bevestig één keer. Elk boek houdt zijn eigen opgeschoonde tags. Eén boek behoudt het bestaande bewerkvenster.

## Uitwerking

- De bestaande normalizer bepaalt splitsing op komma en regelovergangen, witruimte en ontdubbeling.
- Het bulkoverzicht is alleen-lezen en scrollbaar met toetsenbord en muis; individuele vrije bewerking blijft via de enkelvoudige route beschikbaar.
- Alle voorstellen worden vóór bevestiging opgebouwd; annuleren schrijft niets.
- De bestaande service wordt per boek aangeroepen. Een fout blokkeert volgende boeken niet; annulering wordt doorgegeven.
- De opgeslagen tags worden vóór ieder herstel gecontroleerd tegen de getoonde oorspronkelijke tags. Gewijzigde tags worden niet overschreven vanuit een verouderde preview.
- Bestaande opslag en generieke batchreconciliatie verwerken successen, waarschuwingen, inmiddels opgeloste, verdwenen en mislukte boeken. Eén bibliotheekverversing per batch.
- Volledig succes geeft een korte groene melding; gemengde resultaten geven aantallen en houden mislukte boeken geselecteerd.
- Nieuwe teksten komen in alle zes ondersteunde talen. Geen databasewijziging of dependency.

## Structuur en conventies

Application bevat normalizer en herstelservice; Presentation bevat preview en dashboardcoördinatie; App bevat WPF en resources. Tests staan onder tests/EbookManager.Tests. Bestaande records, async/await en cancellation worden gevolgd.

Voorbeeld: boek A met ` Thriller ` krijgt `Thriller`; boek B met `Historie, Reizen` krijgt twee eigen tags. Er wordt nooit één gezamenlijke taglijst op beide boeken gezet.

## Implementatie en verificatie

1. Previewmodel en tests: afzonderlijke voorstellen, lege uitkomst en enkelvoudige regressies.
2. Dashboard en service: selectie, annuleren, behoud, gewijzigde tags, gedeeltelijke fouten, verversing.
3. WPF-overzicht en zes vertalingen; gericht testcheckpoint.
4. Volledige tests, Debug-build, eigen review, checklist, Obsidian-spiegel en gewone PR.

Commando's: `dotnet test EbookManager.sln -c Debug`, `dotnet build src/EbookManager.App/EbookManager.App.csproj -c Debug --no-restore`, `git diff --check`.

## Grenzen

Altijd: tests en bestaande opslagroute, gegevensbehoud, lokalisatie en Markdown-spiegel controleren. Nieuwe schema's, dependencies en externe bronnen vallen buiten deze opdracht. Nooit licenties, databases of buildbestanden committen. Alleen de gebruiker mergt de PR.

## Acceptatie

Meervoudige selectie opent een overzicht met correcte voor/na-waarden per boek. Annuleren bewaart alles; bevestigen schrijft elk afzonderlijk voorstel veilig. Tellingen, bibliotheek en tagfilters verversen. Fouten blijven herstelbaar. De gebruiker voert de handmatige checklist uit vóór merge.
