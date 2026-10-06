# Milestone 40 implementatieplan: duplicate-merge sidecarconsistentie

## Overzicht

Milestone 40 rondt de bestaande duplicate-merge af door na iedere geslaagde merge de definitieve doelmetadata ook naar `metadata.json` te schrijven. De database-merge blijft de gezaghebbende stap. Sidecars worden daarna éénmaal per unieke map van alle gekoppelde doelbestanden bijgewerkt. Een sidecarfout draait de reeds geslaagde database-merge niet terug, maar wordt wel als duidelijke waarschuwing aan de gebruiker gemeld.

## Architectuurbeslissingen

- `DuplicateMergeService` blijft eigenaar van de volledige mergevolgorde.
- De bestaande `IMetadataSidecarStore` en `ILibraryFileStore` worden hergebruikt; er komt geen tweede JSON-implementatie.
- Na het koppelen en bijwerken worden de actuele doelbestanden opnieuw uit de repository gelezen.
- Sidecars worden per unieke absolute map geschreven met platformpassende padvergelijking.
- Een sidecarfout levert een gestructureerd resultaat op. De merge blijft zichtbaar als uitgevoerd en de UI toont een gelokaliseerde waarschuwing.
- Native metadata-write-back in EPUB-, PDF- of andere boekbestanden blijft buiten scope.
- De lokale gebruikerswijzigingen in `BookDetailsView.xaml` en `SettingsWindow.xaml` worden afzonderlijk geverifieerd en gecommit.

## Taak 1: lokale schermaanpassingen veilig opnemen

**Beschrijving:** Verifieer en commit de twee reeds aanwezige gebruikerswijzigingen zonder ze met de mergefunctionaliteit te vermengen.

**Acceptatiecriteria:**
- [x] De knoppen in boekdetails behouden de door de gebruiker gekozen icoonweergave.
- [x] De bestaande instellingstekst gebruikt de gekozen resourcebinding.
- [x] De XAML- en compositietests blijven groen.

**Verificatie:** Gerichte layout-/compositietests en een Debug-build.

**Afhankelijkheden:** Geen.

## Taak 2: sidecarcontract testgestuurd uitbreiden

**Beschrijving:** Voeg eerst falende servicetests toe voor schrijven naar één of meerdere unieke boekmappen, met de definitieve samengevoegde metadata.

**Acceptatiecriteria:**
- [x] De doelmetadata wordt na een merge naar `metadata.json` geschreven.
- [x] Meerdere formaten in dezelfde map veroorzaken slechts één sidecarschrijfopdracht.
- [x] Verschillende gekoppelde mappen krijgen ieder dezelfde definitieve metadata.

**Verificatie:** Gerichte `DuplicateMergeServiceTests` gaan eerst rood en daarna groen.

**Afhankelijkheden:** Taak 1.

## Taak 3: veilige foutafhandeling en gebruikersmelding

**Beschrijving:** Modelleer het sidecarresultaat en laat de bibliotheeklaag bij een schrijffout een waarschuwing tonen, terwijl de geslaagde merge correct uit het duplicatenoverzicht verdwijnt.

**Acceptatiecriteria:**
- [x] Een sidecarfout wordt niet als volledig succes gemeld.
- [x] De gebruiker ziet dat de boeken wel zijn samengevoegd maar `metadata.json` niet volledig is bijgewerkt.
- [x] De duplicate finder en hoofdbibliotheek blijven volgens de bestaande verversingsregels werken.

**Verificatie:** Eerst falende service- en `LibraryViewModel`-tests, daarna groen; lokalisatietests voor alle zes talen.

**Afhankelijkheden:** Taak 2.

## Taak 4: documentatie, volledige verificatie en oplevering

**Beschrijving:** Werk featurestatus, README en handmatige checklist bij, spiegel Markdown naar Obsidian, voer de volledige Definition of Done uit en lever een actuele Debug-build en gewone PR.

**Acceptatiecriteria:**
- [x] GitHub-issue #1 is aantoonbaar afgedekt door regressietests.
- [x] Volledige tests en Debug-build zijn groen (772 tests; Saga 2026.10.6.25).
- [x] De handmatige checklist en Obsidian-spiegel zijn gereed.
- [x] De branchdiff bevat alleen Milestone 40 en de afzonderlijk overeengekomen schermaanpassingen.

**Verificatie:** Volledige testset, schone Debug-build, diffreview en GitHub-controles.

**Afhankelijkheden:** Taken 1-3.

## Risico's en maatregelen

| Risico | Maatregel |
|---|---|
| De database-merge slaagt maar een sidecar niet | Gestructureerde waarschuwing; geen onjuiste rollbackclaim |
| Dezelfde map wordt meerdere keren geschreven | Unieke absolute mappen verzamelen met platformpassende vergelijking |
| Verouderde bron- of doeldata wordt weggeschreven | Definitieve doelmetadata en actuele gekoppelde bestanden na de merge gebruiken |
| Bestaande embedded metadata verandert onverwacht | Alleen de sidecarstore aanroepen; native write-back buiten scope houden |

## Checkpoint

- [x] Specificatie en volgorde door gebruiker goedgekeurd.
- [x] Lokale schermaanpassingen afzonderlijk vastgelegd.
- [x] Rode en groene tests per increment vastgelegd.
- [ ] Volledige Definition of Done afgerond.
- [ ] Gewone, niet-draft PR geopend.
