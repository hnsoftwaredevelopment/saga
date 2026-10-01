# Milestone 38 Implementatieplan: bulk kwaliteitsbeslissingen

## Overzicht

Milestone 38 maakt meervoudige selectie op de Quality Page mogelijk en past `Dit is correct` veilig toe op alle geselecteerde rijen binnen één categorie. Enkelvoudige herstel- en navigatieacties blijven beperkt tot precies één boek.

## Architectuurbeslissingen

- Het viewmodel bezit de betekenisvolle selectie; de WPF DataGrid geeft `SelectedItems` via één event door.
- De bestaande enkelvoudige selectie blijft de primaire rij voor bestaande acties en navigatie.
- Meerdere beslissingen worden als één repositorybatch opgeslagen en door één SQLite-transactie beschermd.
- Alleen meervoudige acties vragen bevestiging; het huidige snelle enkelvoudige gedrag blijft bestaan.
- De lokale gebruikerswijzigingen in `BookDetailsView.xaml` en `SettingsWindow.xaml` blijven buiten deze milestone.

## Taak 1: Selectie- en commandocontract

**Beschrijving:** Voeg eerst falende viewmodeltests toe voor multiselectie, bevestigen, annuleren, opslagfout en enkelvoudige acties.

**Acceptatiecriteria:**
- [x] Meerdere geldige rijen activeren `Dit is correct`.
- [x] Open- en herstelacties zijn bij meerdere rijen uitgeschakeld.
- [x] Annuleren en fouten behouden alle rijen.

**Verificatie:** Gerichte dashboardtests falen vóór en slagen na implementatie.

**Bestanden:** dashboardviewmodel en dashboardtests.

## Taak 2: Transactionele batchopslag

**Beschrijving:** Bescherm de bestaande batchtoevoeging van uitsluitingen met één SQLite-transactie.

**Acceptatiecriteria:**
- [x] Alle unieke sleutels worden samen opgeslagen.
- [x] Een fout kan geen gedeeltelijk zichtbare batch achterlaten.
- [x] Bestaande enkelvoudige en dubbele toevoegingen blijven idempotent.

**Verificatie:** Gerichte repository-integratietests groen.

**Bestanden:** EF-repository en bestaande integratietest.

## Taak 3: WPF-selectie en bevestiging

**Beschrijving:** Maak de grid meervoudig selecteerbaar, verbind de selectie met het viewmodel en voeg een gelokaliseerde bevestiging toe.

**Acceptatiecriteria:**
- [x] DataGrid gebruikt uitgebreide volledige-rijselectie.
- [x] Selectiewijzigingen bereiken het viewmodel.
- [x] Twee of meer rijen tonen een bevestiging met het juiste aantal.

**Verificatie:** Layout-, lokalisatie- en compositietests groen.

**Bestanden:** dashboardvenster, interactiecontract/-service, resourcebestanden en tests.

## Taak 4: Documentatie en oplevering

**Beschrijving:** Werk featurestatus, README en handmatige checklist bij, spiegel Markdown, bouw één actuele Debug-build en open een gewone PR.

**Acceptatiecriteria:**
- [x] Volledige testset en Debug-build zijn groen.
- [x] `Builds\Debug` bevat precies één actuele `Saga.exe`.
- [x] Alle Markdown is identiek naar Obsidian gespiegeld.
- [x] Branchdiff bevat de lokale gebruikerswijzigingen niet.

**Verificatie:** Definition of Done, diffreview en GitHub-controles.

## Risico's en maatregelen

| Risico | Maatregel |
|---|---|
| Meervoudige selectie activeert per ongeluk een enkelvoudige reparatie | Alle enkelvoudige opdrachten delen de voorwaarde `SelectedBookCount == 1` |
| Opslag faalt halverwege | Volledige repositorybatch in één transactie |
| UI-selectie en viewmodel lopen uiteen | Eén `SelectionChanged`-brug en gerichte tests voor selectieovergangen |
| Verkeerde categorie of verouderde rij wordt opgeslagen | Selectie vlak voor opslag filteren op de actuele categorie |
| OneDrive wordt belast | Geen bibliotheek- of omslagbestanden aanraken in deze slice |

## Checkpoint

- [x] Gerichte tests groen.
- [x] Volledige tests en Debug-build groen (754 tests; Saga 2026.10.1.54).
- [x] Handmatige checklist klaar.
- [ ] Gewone, mergeable PR geopend.

## Gepauzeerd onderhoud

De eenmalige hydratatie van OneDrive-omslagen blijft gepauzeerd totdat OneDrive de opslagmigratie online volledig heeft verwerkt.
