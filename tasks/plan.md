# Milestone 37 Implementatieplan: tabblad Kwaliteit

## Overzicht

Milestone 37 verplaatst het bestaande beheer van genegeerde kwaliteitsmeldingen uit `Duplicaten` naar een eigen gelokaliseerd tabblad `Kwaliteit`. De bestaande opdracht en het beheervenster blijven intact; de wijziging is uitsluitend een duidelijke hergroepering van de interface.

## Architectuurbeslissingen

- Geen nieuwe viewmodel- of opslaglaag: de bestaande `ShowMetadataQualityExclusionsCommand` blijft de enige route.
- De tabtitel krijgt een eigen resource-sleutel in alle ondersteunde talen.
- Een structurele XAML-test bewaakt zowel de nieuwe plaats als het verdwijnen uit `Duplicaten`.
- De lokale, niet bij deze milestone horende wijzigingen in `BookDetailsView.xaml` en de kleurresource in `SettingsWindow.xaml` worden niet gecommit.

## Taak 1: Contract en lokalisatie bewaken

**Beschrijving:** Leg met eerst falende tests vast dat Instellingen een tabblad Kwaliteit bevat, dat de beheeractie daarin staat en dat iedere taal een begrijpelijke tabtitel heeft.

**Acceptatiecriteria:**
- [x] De layouttest vindt één kwaliteitstab met de bestaande beheeractie.
- [x] De duplicatentab bevat die actie niet meer.
- [x] Alle zes resourcebestanden bevatten `SettingsQualitySection`.

**Verificatie:** De 7 gerichte layout- en lokalisatietests faalden vóór en slagen na implementatie.

**Bestanden:** layouttest, lokalisatietest en resourcebestanden.

## Taak 2: Interface verplaatsen

**Beschrijving:** Verplaats de bestaande uitleg en knop naar een eigen tabblad zonder opdrachtbinding of toegankelijkheid te wijzigen.

**Acceptatiecriteria:**
- [x] De kwaliteitsactie staat uitsluitend onder `Kwaliteit`.
- [x] De knop behoudt opdracht, focusbaarheid en toegankelijke naam.
- [x] Alle duplicateninstellingen blijven onder `Duplicaten` staan.

**Verificatie:** Gerichte tests zijn groen en de Debug-publicatie slaagt zonder waarschuwingen of fouten.

**Bestanden:** `SettingsWindow.xaml`.

## Taak 3: Documentatie, build en oplevering

**Beschrijving:** Werk featurestatus, README en handmatige checklist bij, spiegel Markdown, bouw één actuele Debug-build en open een gewone PR.

**Acceptatiecriteria:**
- [x] Volledige testset slaagt: 746 van 746 tests groen.
- [x] Debug-build heeft 0 waarschuwingen en 0 fouten.
- [x] `Builds\Debug` bevat precies één actuele `Saga.exe`, versie `2026.10.1.45`, gebouwd op 1 oktober 2026 om 14:41.
- [x] Alle gewijzigde Markdown is identiek gespiegeld naar Obsidian.

**Verificatie:** Definition of Done, diffreview en GitHub-controles.

## Risico's en maatregelen

| Risico | Maatregel |
|---|---|
| De knop blijft per ongeluk ook bij Duplicaten staan | Test de oudertab van de knop en de afwezigheid onder Duplicaten |
| Vertaalde tabtitel ontbreekt | Bestaande lokalisatietest uitbreiden voor alle zes talen |
| Lokale gebruikerswijziging belandt in de PR | Alleen milestone-hunks stagen en einddiff expliciet controleren |
| OneDrive wordt extra belast | Geen bibliotheek- of omslagbestanden aanraken tijdens deze UI-slice |

## Checkpoint

- [x] Gerichte tests groen.
- [x] Volledige tests en build groen.
- [x] Handmatige checklist klaar voor de gebruiker.
- [x] Gewone PR #38 geopend; mergeability en controles worden op GitHub bewaakt.

## Gepauzeerd onderhoud

De eenmalige hydratatie van OneDrive-omslagen blijft gepauzeerd totdat OneDrive de opslagmigratie ook online volledig heeft verwerkt.
