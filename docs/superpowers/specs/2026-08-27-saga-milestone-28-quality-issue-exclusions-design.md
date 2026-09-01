# Saga Milestone 28: Kwaliteitsmelding Als Correct Markeren

## Doel

Geef de gebruiker op de metadata quality-pagina de mogelijkheid om een gevonden melding bewust als correct te markeren. Saga onthoudt deze keuze per bibliotheek en per combinatie van boek en kwaliteitssignaal, zodat een geaccepteerde uitzondering niet opnieuw verschijnt terwijl andere meldingen voor hetzelfde boek zichtbaar blijven.

Deze slice vormt de basis voor latere herstelacties zonder nu al metadata te wijzigen.

## Gebruikersresultaat

- Bij een geselecteerd boek en kwaliteitssignaal is de actie `Dit is correct` beschikbaar.
- De actie slaat uitsluitend de geselecteerde combinatie van boek en signaal op.
- De geaccepteerde melding verdwijnt direct uit de huidige lijst en de aantallen worden bijgewerkt.
- Het dashboard selecteert daarna de eerstvolgende logische boekregel, of niets wanneer de categorie leeg is.
- Andere kwaliteitsmeldingen voor hetzelfde boek blijven zichtbaar.
- Bij een volgende opening van het dashboard blijft de geaccepteerde melding verborgen.
- De gebruiker kan genegeerde kwaliteitsmeldingen via Instellingen bekijken en herstellen.

## Interactieontwerp

### Quality-pagina

Het bestaande modale dashboard krijgt naast `Openen in bibliotheek` en `Sluiten` een actie `Dit is correct`.

- De actie is alleen beschikbaar wanneer zowel een kwaliteitssignaal als een boekregel is geselecteerd.
- Een enkele melding wordt zonder bevestigingsdialoog genegeerd, omdat de handeling via Instellingen omkeerbaar is.
- Na succes blijft het dashboard geopend.
- Als er nog regels in de categorie staan, selecteert Saga de regel die op dezelfde positie is terechtgekomen; bij de laatste regel wordt de voorgaande regel geselecteerd.
- Als de categorie leeg raakt, wordt de boekselectie gewist en worden boekacties uitgeschakeld.
- Als opslaan mislukt, blijft de regel staan en toont Saga een gelokaliseerde foutmelding.
- `Openen in bibliotheek` en dubbelklik behouden hun bestaande gedrag.

### Beheer via Instellingen

De bestaande sectie voor duplicaten en diagnostiek krijgt een actie `Genegeerde kwaliteitsmeldingen beheren` voor de actieve bibliotheek.

Het beheerwindow toont per uitzondering:

- titel en auteur(s) van het boek;
- de gelokaliseerde naam van het kwaliteitssignaal;
- het moment waarop de melding als correct is gemarkeerd.

De gebruiker kan:

- één of meer geselecteerde uitzonderingen herstellen;
- alle uitzonderingen herstellen na een bevestiging;
- het window sluiten zonder wijzigingen.

Na herstel verschijnt een melding opnieuw zodra het boek nog steeds aan de betreffende kwaliteitsregel voldoet. Een onbekende signaalsleutel uit een latere of oudere versie wordt met de ruwe sleutel getoond en blijft herstelbaar.

## Signaalidentiteit

Iedere kwaliteitsregel krijgt een stabiele, niet-gelokaliseerde sleutel. De eerste sleutels zijn:

| Signaal | Persistente sleutel |
|---|---|
| Auteur ontbreekt | `missing-author` |
| Onbekende taal | `unknown-language` |
| Omslag ontbreekt | `missing-cover` |
| Reeksnummer zonder reeks | `series-number-without-series` |
| Mogelijk titel en auteur verwisseld | `possible-title-author-swap` |
| Rommelige tags | `messy-tags` |

De sleutel is een opslagcontract en mag niet veranderen wanneer de vertaling, titel, beschrijving of detectieheuristiek wordt aangepast.

## Opslagcontract

De bibliotheekdatabase krijgt een additieve tabel `MetadataQualityExclusions` met:

- `BookId`: verwijzing naar het boek;
- `SignalKey`: stabiele signaalsleutel;
- `CreatedAt`: UTC-tijdstip van vastlegging;
- samengestelde primaire sleutel `(BookId, SignalKey)` om dubbele invoer te voorkomen;
- foreign key naar `Books` met cascade delete, zodat verwijderde boeken geen uitzonderingen achterlaten.

SQLite blijft hiervoor de gezaghebbende opslag. Een kwaliteitsuitzondering is gebruikersworkflowstatus en geen boekmetadata; er wordt daarom geen `metadata.json`-sidecar aangepast.

Bij het openen van het dashboard laadt Saga alle uitzonderingssleutels voor de actieve bibliotheek. Alleen een exacte combinatie van boek-id en signaalsleutel wordt uit de resultaten gefilterd.

## Architectuur

De implementatie volgt het bestaande patroon voor duplicate exclusions:

- het Domain-project bevat het sleutel-/detailmodel en de repository-interface;
- Infrastructure bevat de EF Core-entiteit, configuratie, migratie en SQLite-repository;
- het dashboard-viewmodel gebruikt stabiele signaalsleutels en verwijdert een succesvol opgeslagen resultaat uit zijn observeerbare state;
- `LibraryViewModel` verbindt de actieve bibliotheekrepository met het dashboard en het instellingenbeheer;
- de WPF-laag verzorgt windows, selectieoverdracht, bevestigingen en gelokaliseerde gebruikersfeedback.

Een beoogde eenvoudige sleutelvorm is:

```csharp
public readonly record struct MetadataQualityExclusionKey(
    Guid BookId,
    string SignalKey);
```

Er wordt geen nieuwe dependency toegevoegd en de kwaliteitspredicaten worden in deze slice inhoudelijk niet gewijzigd.

## Technische Basis

- .NET 10 en C# met nullable reference types en warnings-as-errors.
- WPF met CommunityToolkit.Mvvm en de bestaande Syncfusion-gridcomponenten.
- Entity Framework Core met de bibliotheekgebonden SQLite-database.
- xUnit en FluentAssertions voor unit-, integratie- en XAML-structuurtests.

## Projectstructuur

- `src/EbookManager.Domain`: uitsluitingsmodel en repositorycontract.
- `src/EbookManager.Infrastructure/Persistence`: EF-entiteit, DbContext-configuratie, migratie en repository.
- `src/EbookManager.Presentation/ViewModels`: dashboardstate, commando's en beheer-viewmodel.
- `src/EbookManager.App/Views`: dashboard- en beheerwindow.
- `src/EbookManager.App/Resources/Strings`: teksten voor alle ondersteunde talen.
- `tests/EbookManager.Tests`: domein-, repository-, viewmodel-, interactie- en layouttests.
- `docs/manual-tests`: handmatige checklist voor deze milestone.

## Commando's

```powershell
dotnet restore EbookManager.sln
dotnet test EbookManager.sln -c Release --no-restore
dotnet build EbookManager.sln -c Release --no-restore
dotnet run --project src/EbookManager.App/EbookManager.App.csproj
```

Voor een nieuwe EF Core-migratie wordt de bestaande projectconfiguratie gebruikt; er worden geen migrations handmatig nagebootst of herschreven.

## Teststrategie

- Repository-integratietests bewijzen unieke opslag, ophalen, geselecteerd herstellen, alles herstellen en cascade delete.
- Dashboard-viewmodeltests bewijzen exacte filtering per boek/signaal, directe verwijdering, selectie na verwijdering en correcte aantallen.
- Beheer-viewmodeltests bewijzen selectie, geselecteerd herstellen, alles herstellen en onbekende signaalsleutels.
- Library-/interactietests bewijzen dat alleen de actieve bibliotheek wordt gebruikt en dat opslagfouten geen regel uit de UI verwijderen.
- XAML-structuurtests bewaken de aanwezigheid, bindingen en bereikbaarheid van de nieuwe acties.
- De volledige testsuite en Release-build moeten schoon slagen.
- Een handmatige checklist dekt heropenen van het dashboard, meerdere signalen voor één boek, instellingenbeheer, toetsenbordbediening en foutfeedback.

## Grenzen

### Altijd doen

- Filteren op de stabiele combinatie van boek-id en signaalsleutel.
- Alle zichtbare teksten lokaliseren voor de zes ondersteunde talen; Engelse terugvaltekst is toegestaan waar een betrouwbare vertaling ontbreekt.
- De huidige dashboardnavigatie en verstelbare panelen behouden.
- Databasewijzigingen additief en terugwaarts veilig uitvoeren.
- Tests schrijven vóór gedragsimplementatie en de volledige suite vóór publicatie uitvoeren.
- Gewijzigde Markdown-documentatie naar de Obsidian-projectmap spiegelen.

### Eerst opnieuw afstemmen

- Het volledig uitschakelen van een kwaliteitsregel voor de hele bibliotheek.
- Het negeren van meerdere regels of boeken in één bulkactie.
- Het aanpassen van bestaande detectieheuristieken.
- Het opslaan van uitzonderingen in sidecars of algemene applicatie-instellingen.
- Het toevoegen van dependencies of wijzigen van CI-configuratie.

### Nooit doen binnen deze slice

- Boekmetadata aanpassen, raden of automatisch herstellen.
- Een heel boek verbergen wanneer slechts één signaal als correct is gemarkeerd.
- Gelokaliseerde tekst als persistente signaalsleutel gebruiken.
- Een regel uit de UI verwijderen voordat de opslag succesvol is afgerond.
- Reviewtests uitschakelen of bestaande migraties achteraf wijzigen.

## Acceptatiecriteria

- `Dit is correct` is alleen actief bij een geselecteerde melding en boekregel.
- Na succesvolle opslag verdwijnt uitsluitend de geselecteerde boek-/signaalcombinatie en worden selectie en aantallen direct correct bijgewerkt.
- Andere signalen voor hetzelfde boek blijven staan.
- De uitzondering blijft na sluiten en opnieuw openen van Saga behouden in de betreffende bibliotheek.
- Een verwijderde boekrecord verwijdert gekoppelde uitzonderingen automatisch.
- Instellingen toont de uitzonderingen met boek, signaal en datum en kan geselecteerde of alle uitzonderingen herstellen.
- Een herstelde melding verschijnt opnieuw wanneer de kwaliteitsregel nog steeds van toepassing is.
- Opslagfouten houden de melding zichtbaar en geven begrijpelijke, gelokaliseerde feedback.
- Bestaande dashboardnavigatie, splittergedrag en bibliotheekweergaven blijven ongewijzigd werken.
- Alle automatische tests en de Release-build slagen zonder waarschuwingen of fouten.
- De handmatige milestone-checklist is volledig uitgevoerd.

## Buiten Scope

- Directe metadatareparatie en een herstelpaneel.
- De tekstknop `Dit is correct` vervangen door een passende, toegankelijke icoonknop; dit blijft als visuele vervolgverbetering bewaard.
- Bulkselectie, bulkreparatie of bulk negeren.
- Configureerbare kwaliteitsregels en ernstniveaus.
- Export van kwaliteitswerklijsten.
- Undo/history voor metadatawijzigingen.
- Wijzigingen aan de bestaande zes detectieheuristieken.

## Open Vragen

Geen. De functionele aannames voor deze slice zijn op 27 augustus 2026 door de gebruiker bevestigd.
