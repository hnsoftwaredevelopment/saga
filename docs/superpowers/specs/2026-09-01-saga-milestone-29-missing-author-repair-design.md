# Saga Milestone 29: Ontbrekende Auteur Herstellen

## Aannames

1. Deze eerste herstelslice behandelt uitsluitend het kwaliteitssignaal `missing-author`.
2. De gebruiker herstelt in deze milestone één geselecteerd boek per keer.
3. De herstelactie vult precies één auteur in. Het boek heeft op dat moment geen bruikbare auteur of uitsluitend `Unknown`, waardoor geen geldige auteurs worden overschreven.
4. De auteur mag nieuw zijn, maar tijdens het typen toont Saga passende auteurs die al in de actieve bibliotheek voorkomen.
5. Herstel van meerdere geselecteerde boeken met één gedeelde auteur is expliciet vervolgwerk. De gekozen contracten mogen deze uitbreiding niet blokkeren.
6. De actie start vanuit de Quality Page en opent een compact modaal herstelvenster; na opslaan blijft de gebruiker in dezelfde kwaliteitsworkflow.

## Doel

Geef de gebruiker een veilige, begrijpelijke manier om een boek zonder bruikbare auteur direct vanuit de metadata Quality Page te herstellen. De gebruiker kiest een bestaande auteur uit suggesties of voert een nieuwe naam in. Na een succesvolle opslag controleert Saga het boek opnieuw en werkt het dashboard onmiddellijk bij.

Deze milestone bewijst het patroon voor directe kwaliteitsreparaties. Latere slices kunnen hetzelfde patroon uitbreiden naar taal, serie, titel/auteur, tags, omslagen en uiteindelijk bulkherstel.

## Gebruikersworkflow

1. De gebruiker opent de metadata Quality Page en kiest `Ontbrekende auteur`.
2. De gebruiker selecteert één boek.
3. De actie `Herstellen` wordt beschikbaar.
4. Saga opent `Ontbrekende auteur herstellen` met:
   - de boektitel als alleen-lezen context;
   - een bewerkbaar auteursveld;
   - auteursuggesties uit de actieve bibliotheek;
   - `Opslaan` en `Annuleren`.
5. Tijdens het typen wordt de suggestielijst gefilterd. Een bekende auteur kan met muis of toetsenbord worden gekozen. Niet-bestaande tekst blijft geldige vrije invoer.
6. `Opslaan` is alleen beschikbaar voor een niet-lege, getrimde auteur die niet gelijk is aan `Unknown`.
7. Na opslaan gebruikt Saga de bestaande metadata-opslag en write-backroute.
8. Saga leest het opgeslagen boek opnieuw, beoordeelt alle kwaliteitssignalen opnieuw en werkt rijen, waarden, selectie en aantallen direct bij.
9. Als de opslag mislukt, blijft het herstelvenster open en ziet de gebruiker een begrijpelijke foutmelding.
10. `Annuleren` of sluiten verandert niets.

## Auteursuggesties

- De bron bestaat uitsluitend uit auteurs van boeken in de actieve bibliotheek.
- Lege waarden en `Unknown` worden niet aangeboden.
- Waarden worden getrimd en hoofdletterongevoelig ontdubbeld.
- Suggesties die met de invoer beginnen komen eerst; overige gedeeltelijke treffers volgen daarna.
- Binnen iedere groep wordt alfabetisch gesorteerd volgens de huidige cultuur.
- Zonder invoer mag de lijst alle bekende auteurs alfabetisch tonen.
- Een geselecteerde suggestie neemt de bestaande schrijfwijze exact over.
- Vrije invoer wordt getrimd opgeslagen en wordt na succesvol herstel beschikbaar als suggestie voor een volgende reparatie in hetzelfde dashboard.

## Dashboardgedrag na herstel

- De gerepareerde boek-/signaalrij verdwijnt alleen wanneer `missing-author` na opslag niet meer van toepassing is.
- Andere nog geldige kwaliteitsmeldingen voor hetzelfde boek blijven zichtbaar.
- Omdat een auteurswijziging ook `possible-title-author-swap` kan beïnvloeden, worden alle zes huidige signalen voor het boek opnieuw beoordeeld.
- Bestaande keuzes via `Dit is correct` blijven gerespecteerd.
- Zichtbare boekwaarden, categorieaantallen en het totale aantal worden zonder heropenen bijgewerkt.
- De selectie gaat naar de logisch volgende rij; bij een lege categorie verdwijnt de boekselectie.
- Het nieuwe auteursgegeven wordt ook zichtbaar in de hoofdbibliotheek wanneer het dashboard wordt gesloten of `Openen in bibliotheek` wordt gebruikt.

## Opslag en architectuur

- Gebruik de bestaande `BookService.SaveAsync`-route, zodat database, `metadata.json`-sidecar en ondersteunde ebook-write-back hetzelfde gedrag houden als de detailbewerking.
- Haal vlak voor opslaan het actuele boek op via de actieve bibliotheekrepository om verlies van tussentijdse wijzigingen te voorkomen.
- Wijzig uitsluitend `BookMetadata.Authors` en `UpdatedUtc`; alle andere boekmetadata, leesstatus, formaten en kwaliteitsuitzonderingen blijven behouden.
- Lees het boek na de opslag opnieuw uit de repository voordat het dashboard wordt bijgewerkt.
- Voeg geen databasekolom, migratie of externe dependency toe.
- Houd de reparatie-invoer en validatie in een zelfstandig, testbaar presentatie-viewmodel.
- Houd de signaalevaluatie op één plaats, zodat initiële dashboardopbouw en herevaluatie exact dezelfde regels gebruiken.
- Vorm het herstelverzoek intern rond een verzameling boek-id's en één auteur, ook al bevat die verzameling in deze milestone precies één id. Dit bereidt bulkherstel voor zonder het al zichtbaar te maken.

## Toegankelijkheid en lokalisatie

- Alle nieuwe zichtbare teksten worden toegevoegd aan basis/Engels, Nederlands, Duits, Frans, Spaans en Italiaans.
- De herstellink, auteursinvoer, suggestielijst, foutmelding en knoppen hebben begrijpelijke toegankelijke namen.
- De volledige workflow werkt met toetsenbord:
  - `Tab` bereikt alle interactieve onderdelen;
  - pijltoetsen navigeren door suggesties;
  - `Enter` kiest een suggestie of slaat geldige invoer op;
  - `Escape` sluit zonder wijziging.
- Focus start in het auteursveld en keert na sluiten logisch terug naar de Quality Page.

## Technische omgeving en opdrachten

- Platform: Windows WPF op .NET 10.
- Presentatie: CommunityToolkit.Mvvm en bestaande WPF-stijlen.
- Opslag: EF Core met SQLite via de actieve bibliotheekrepository.
- Testframework: xUnit met FluentAssertions.

Volledige controles:

```powershell
dotnet test EbookManager.sln -c Release --no-restore
dotnet build EbookManager.sln -c Release --no-restore
git diff --check
```

Gerichte tests worden tijdens implementatie uitgevoerd met filters voor `MetadataQualityAuthorRepair`, `MetadataQualityDashboard` en `LibraryViewModel`.

## Projectstructuur en stijl

- Domein- en applicatiecontracten: `src/EbookManager.Domain` en `src/EbookManager.Application`.
- Herstel- en dashboard-viewmodels: `src/EbookManager.Presentation/ViewModels`.
- WPF-herstelvenster en interactieservice: `src/EbookManager.App`.
- Viewmodel-, integratie-, layout- en lokalisatietests: `tests/EbookManager.Tests`.
- Ontwerp, plan en handmatige checklist: `docs` en `tasks`.

Nieuwe code volgt de bestaande constructorinjectie, async-command- en onveranderlijke `Book`-patronen. Bijvoorbeeld:

```csharp
var updatedBook = currentBook with
{
    Metadata = currentBook.Metadata with { Authors = [normalizedAuthor] },
    UpdatedUtc = DateTimeOffset.UtcNow
};
```

## Teststrategie

- Viewmodeltests bewijzen invoervalidatie, vrije invoer, filtering, sortering, ontdubbeling en selectie van bekende auteurs.
- Dashboardtests bewijzen commandobeschikbaarheid, annuleren, foutgedrag, herevaluatie van alle signalen, selectie en actuele aantallen.
- Library-/integratietests bewijzen dat uitsluitend de auteur wijzigt en dat de bestaande `BookService`-route wordt gebruikt.
- Layouttests bewijzen bindingen, labels, toegankelijke namen en toetsenbordinstellingen.
- Lokalisatietests bewijzen volledige sleutelpariteit in zes talen en voorkomen zichtbare interne sleutels.
- Een handmatige checklist controleert suggesties, nieuwe auteur, annuleren, opslag, directe dashboardupdate en terugkeer naar de bibliotheek.

## Grenzen

### Altijd

- Bestaande metadataopslag en write-back hergebruiken.
- Alleen de geselecteerde ontbrekende auteur wijzigen.
- Alle kwaliteitssignalen na herstel opnieuw beoordelen.
- Fouten zichtbaar en herstelbaar houden.
- Tests en Release-build vóór iedere functionele checkpoint uitvoeren.
- Gewijzigde Markdown naar de Obsidian-spiegel kopiëren.

### Eerst overleggen

- Een databaseschemawijziging.
- Een nieuwe externe UI-dependency.
- Automatisch raden of extern opzoeken van auteurs.
- Een wijziging die meer metadata dan auteurs aanpast.

### Nooit in deze milestone

- Meerdere boeken tegelijk wijzigen.
- Een auteur automatisch opslaan zonder expliciete gebruikersbevestiging.
- Een geldige bestaande auteur overschrijven.
- Kwaliteitsmeldingen automatisch als correct markeren.
- Omslag-, taal-, serie-, tag- of titel/auteurherstel toevoegen.

## Acceptatiecriteria

- Alleen bij een geselecteerde rij onder `Ontbrekende auteur` is `Herstellen` beschikbaar.
- Het compacte herstelvenster toont de juiste boektitel en focust de auteursinvoer.
- Bekende auteurs verschijnen tijdens het typen, zonder lege of dubbele waarden.
- Een volledig nieuwe auteur kan worden ingevoerd en opgeslagen.
- Lege invoer en `Unknown` kunnen niet worden opgeslagen.
- Opslaan wijzigt uitsluitend de auteur van het actuele geselecteerde boek via de bestaande veilige opslagroute.
- Na succes wordt het boek opnieuw gelezen en worden alle kwaliteitssignalen, rijen en aantallen direct bijgewerkt.
- Annuleren en opslagfouten laten het dashboard en het boek ongewijzigd of tonen duidelijk eventuele bestaande write-backstatus.
- De nieuwe workflow is toetsenbordtoegankelijk en volledig vertaald in alle zes ondersteunde talen.
- Alle geautomatiseerde tests en de Release-build slagen zonder waarschuwingen.

## Expliciet vervolgwerk

- Meerdere boeken op de Quality Page selecteren en één gekozen of nieuw ingevoerde auteur op alle geselecteerde boeken toepassen.
- Bulkresultaten tonen wanneer een deel van de boeken niet kon worden opgeslagen.
- Direct herstel voor onbekende taal, ontbrekende serie, titel/auteur-omwisseling en rommelige tags.
- Een algemene omslagkiezer en herstel voor ontbrekende omslagen.
- De tekstknop `Dit is correct` vervangen door een passende toegankelijke icoonknop.

## Open vragen

Er zijn voor deze eerste slice geen blokkerende open vragen. Meerdere auteurs per enkel boek blijven via de bestaande detailbewerking beschikbaar; deze compacte kwaliteitsreparatie vult in Milestone 29 één auteur in.
