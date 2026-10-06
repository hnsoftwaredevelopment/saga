# Saga Milestone 40: duplicate-merge sidecarconsistentie

## Doel

Na het samenvoegen van duplicate boeken moeten SQLite en de draagbare `metadata.json`-sidecars dezelfde definitieve metadata bevatten. Hiermee wordt GitHub-issue #1 afgerond zonder de bestaande veilige mergekeuzes of verversingsregels te veranderen.

## Aannames

1. SQLite blijft de gezaghebbende opslag tijdens de merge.
2. Een sidecarfout na een database-merge mag de geslaagde merge niet als teruggedraaid voorstellen.
3. Iedere unieke map met een gekoppeld doelbestand hoort één `metadata.json` te krijgen.
4. De bestaande `JsonMetadataSidecarStore` bepaalt formaat en veilige schrijfmethode.
5. Native metadata-write-back in ebookbestanden hoort niet bij deze slice.

## Gewenst gedrag

1. Saga leest bron- en doelboek en past de gekozen metadatavelden toe.
2. Saga koppelt de bronbestanden aan het doelboek en verwijdert het overbodige bronrecord via de bestaande repositoryroute.
3. Saga slaat de definitieve doelmetadata in SQLite op.
4. Saga leest alle actuele doelbestanden en bepaalt hun unieke absolute mappen.
5. Saga schrijft de definitieve doelmetadata eenmaal per map via `IMetadataSidecarStore`.
6. Bij volledig succes werkt de bestaande duplicate-workflow ongewijzigd.
7. Bij een sidecarfout blijft de merge uitgevoerd, verdwijnt het bronboek uit het overzicht en ziet de gebruiker een duidelijke waarschuwing.

## Technische context

- Applicatielogica: `src/EbookManager.Application/Books`.
- WPF-compositie en vertaalde meldingen: `src/EbookManager.App`.
- Bibliotheekcoördinatie: `src/EbookManager.Presentation/ViewModels/LibraryViewModel.cs`.
- Servicetests: `tests/EbookManager.Tests/Books`.
- Viewmodel- en lokalisatietests: `tests/EbookManager.Tests/App`.

## Teststrategie

- Schrijf eerst falende servicetests voor definitieve metadata, één schrijfopdracht per unieke map en meerdere unieke mappen.
- Schrijf eerst falende foutpaden voor een sidecarstore die een uitzondering geeft.
- Bewijs in `LibraryViewModel` dat een sidecarwaarschuwing wordt getoond en de merge toch als wijziging wordt verwerkt.
- Laat bestaande duplicate-, database-, compositie- en lokalisatietests meelopen.
- Sluit af met de volledige testset en een schone Debug-build.

## Grenzen

### Altijd

- Bestaande abstraheringen voor repository, bestandsopslag en sidecars hergebruiken.
- Annulering als `OperationCanceledException` doorgeven.
- Een merge- en sidecarresultaat ondubbelzinnig rapporteren.
- Markdownwijzigingen naar de afgesproken Obsidian-map spiegelen.

### Eerst overleggen

- Een database-schemawijziging.
- Een nieuwe externe dependency.
- Native ebookmetadata tijdens de merge herschrijven.

### Nooit

- Een API-key of ander geheim in broncode of documentatie vastleggen.
- Een sidecarfout verbergen achter een volledige succesmelding.
- Bronboekbestanden verwijderen buiten de bestaande expliciete mergeworkflow.

## Acceptatiecriteria

- Na een geslaagde merge bevatten SQLite en iedere relevante `metadata.json` dezelfde samengevoegde metadata.
- Dezelfde map wordt niet dubbel verwerkt.
- Een schrijffout wordt zichtbaar gemeld als sidecarwaarschuwing terwijl de uitgevoerde merge correct wordt gereflecteerd.
- Bestaande merge-, bestandsbehoud- en verversingsfunctionaliteit blijft werken.
- Regressietests, volledige tests en Debug-build zijn groen.
