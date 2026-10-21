# Milestone 42: titel en auteur omwisselen vanuit boekdetails

## Doel

Milestone 42 rondt de al aanwezige knop voor het omwisselen van titel en auteur in het detailpaneel aantoonbaar af. De gebruiker kan hiermee ook een boek herstellen dat niet door de conservatieve kwaliteitscontrole is herkend. De bestaande interactie blijft leidend: de knop wijzigt eerst alleen de bewerkvelden, `Opslaan` maakt de wijziging definitief en `Ongedaan maken` herstelt de oorspronkelijke waarden.

De slice bouwt geen tweede omwisselfunctie. Zij bewijst de volledige route van knop tot opslag en bibliotheekverversing, vult ontbrekende interface- en integratietests aan en documenteert de functie voor handmatige acceptatie.

## Bestaande uitgangssituatie

- `BookDetailsView` bevat rechts naast het titellabel een compacte knop met een pictogram van twee tegengestelde pijlen.
- `BookDetailsViewModel.SwapTitleAndAuthorsCommand` verwisselt de bewerkbare titel en auteurs.
- De wijziging is voorlopig totdat de gebruiker `Opslaan` kiest.
- `Ongedaan maken` herstelt de waarden van het geladen boek.
- De knop heeft al een gelokaliseerde tooltip en toegankelijke naam.
- Een bestaande unit test controleert omwisselen en ongedaan maken, maar de volledige opslag- en bibliotheekroute is nog niet expliciet als één gebruikersverhaal bewezen.

## Gewenst gedrag

1. De omwisselknop is alleen bruikbaar wanneer een boek in het detailpaneel kan worden bewerkt.
2. Eén klik verwisselt de huidige inhoud van de titel- en auteursvelden zonder automatisch op te slaan.
3. Eén of meerdere auteurs worden leesbaar samengevoegd als de nieuwe titel.
4. De voormalige titel blijft één auteur, ook wanneer de titel zelf een scheidingsteken zoals een puntkomma bevat.
5. `Ongedaan maken` herstelt titel en auteurs zolang de wijziging niet is opgeslagen.
6. `Opslaan` gebruikt de bestaande veilige boekopslag, sidecar- en ondersteunde ebookmetadata-route.
7. Na succesvol opslaan worden de zichtbare bibliotheekregel, zoekresultaten en auteursfilters bijgewerkt. Een volgende kwaliteitscontrole beoordeelt de nieuwe metadata.
8. Bij een opslagfout blijft de wijziging zichtbaar voor herstel of een nieuwe poging en wordt de bestaande foutmelding gebruikt.
9. De knop blijft met muis en toetsenbord bedienbaar en behoudt een begrijpelijke tooltip en toegankelijke naam in alle zes ondersteunde talen.

## Buiten scope

- Een extra bevestigingsvenster vóór het omwisselen.
- Automatisch opslaan direct na het indrukken van de knop.
- De heuristiek voor `Mogelijk omgewisselde auteur/titel` aanpassen.
- Meerdere boeken tegelijk omwisselen; die mogelijkheid bestaat al in de algemene metadata-multibewerking.
- Andere velden wijzigen of online metadata opzoeken.
- Een nieuw pictogram of een tweede knop ontwerpen wanneer de bestaande knop aan de controles voldoet.

## Techniek en projectstructuur

- WPF op .NET 10 met MVVM en CommunityToolkit.Mvvm.
- Detailinterface: `src/EbookManager.App/Views/BookDetailsView.xaml`.
- Detailgedrag: `src/EbookManager.Presentation/ViewModels/BookDetailsViewModel.cs`.
- Bibliotheekverversing: `src/EbookManager.Presentation/ViewModels/LibraryViewModel.cs`.
- Lokalisatie: `src/EbookManager.App/Resources/Strings/AppResources*.resx`.
- Tests: `tests/EbookManager.Tests/App/ViewModels` en `tests/EbookManager.Tests/App/Views`.
- Handmatige acceptatie: `docs/manual-tests`.

## Commando's

- Gerichte tests: `dotnet test tests/EbookManager.Tests/EbookManager.Tests.csproj --no-restore --filter "FullyQualifiedName~BookDetailsViewModelTests|FullyQualifiedName~BookDetailsViewLayoutTests|FullyQualifiedName~LibraryViewModelTests"`
- Volledige tests: `dotnet test EbookManager.sln --no-restore`
- Schone build: `dotnet clean EbookManager.sln --configuration Debug` gevolgd door `dotnet build EbookManager.sln --configuration Debug --no-restore`
- Diffcontrole: `git diff --check`

## Codestijl

Gebruik de bestaande command- en propertypatronen zonder een tweede opslagroute toe te voegen:

```csharp
public IRelayCommand SwapTitleAndAuthorsCommand =>
    swapTitleAndAuthorsCommand ??= new RelayCommand(SwapTitleAndAuthors, CanEdit);
```

- Publieke C#-namen gebruiken PascalCase; lokale waarden gebruiken camelCase.
- Tests benoemen het waarneembare gedrag en controleren toestand in plaats van interne aanroepen.
- XAML gebruikt bestaande stijlen, dynamische themabronnen en lokalisatiebindings.
- De kleinste wijziging die ontbrekende dekking of fout gedrag oplost heeft de voorkeur.

## Teststrategie

1. Breid eerst een test uit of voeg een falende test toe voor ieder werkelijk ontbrekend gedrag.
2. Bewijs in de detail-viewmodeltest dat omwisselen plus opslaan de verwisselde metadata door de bestaande opslagroute schrijft.
3. Bewijs op bibliotheekniveau dat de zichtbare regel en auteursfilters na opslaan overeenkomen met de nieuwe metadata.
4. Controleer statisch in de XAML-layouttest dat de knop aan het juiste command is gebonden, het bestaande pictogram gebruikt en een gelokaliseerde tooltip en toegankelijke naam heeft.
5. Controleer de lokalisatiesleutel in alle zes resourcebestanden.
6. Voer daarna de volledige testset, schone Debug-build en handmatige checklist uit.

## Grenzen

### Altijd

- De bestaande `SaveCommand`, `UndoCommand` en `BookSaved`-route hergebruiken.
- Alleen ontbrekende dekking of tijdens verificatie aangetoonde fouten aanpassen.
- Titel, auteurs en alle overige metadata na opslaan consistent houden in database, sidecar en ondersteunde ebookbestanden.
- Nieuwe of gewijzigde Markdown naar de afgesproken Obsidian-map spiegelen.

### Eerst overleggen

- Een extra bevestigingsvenster toevoegen.
- De betekenis of verwerking van meerdere auteurs veranderen.
- Een database-, sidecar- of ebookmetadataformaat wijzigen.
- Nieuwe afhankelijkheden of instellingen toevoegen.

### Nooit

- De Quality Page-heuristiek verruimen als onderdeel van deze slice.
- Automatisch opslaan zonder de bestaande knop `Opslaan`.
- Een geldig boek wijzigen voordat de gebruiker de staged wijziging bevestigt.
- Bestaande tests verwijderen of verzwakken om de wijziging passend te maken.

## Acceptatiecriteria

- De gebruiker kan bij ieder bewerkbaar boek titel en auteur met één herkenbare pictogramknop omwisselen.
- De velden veranderen direct, maar opslag gebeurt uitsluitend via `Opslaan`.
- `Ongedaan maken` herstelt de oorspronkelijke titel en auteurs vóór opslag.
- Na opslag bevatten de repository en zichtbare bibliotheekregel de verwisselde waarden en zijn auteursfilters bijgewerkt.
- Een titel met een puntkomma wordt na omwisselen als één auteur opgeslagen.
- Alle overige metadata blijft ongewijzigd.
- De knop is toetsenbordtoegankelijk en heeft in zes talen een begrijpelijke tooltip en toegankelijke naam.
- De handmatige checklist is afgerond; volledige tests en een schone build zijn groen.
- README en featurestatus vermelden dat Milestone 41 is geaccepteerd en dat de detailactie is geverifieerd.

## Aannames en open vragen

- De bestaande plaatsing naast `Titel`, afmetingen en het pijlenpictogram blijven behouden wanneer de layoutcontrole slaagt.
- De bestaande staged workflow met `Opslaan` en `Ongedaan maken` is voldoende bevestiging; er is geen extra dialoog nodig.
- Meerdere auteurs worden, zoals nu, met komma's tot één nieuwe titel samengevoegd.
- Er zijn geen open vragen die de planning blokkeren.
