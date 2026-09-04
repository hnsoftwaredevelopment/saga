# Saga Milestone 33: Ontbrekende omslag zoeken en herstellen

## Aannames

- Deze milestone behandelt eerst `Geen omslag`; `Rommelige tags` volgt als afzonderlijke slice.
- Zoeken begint alleen na een expliciete gebruikersactie en gebeurt nooit automatisch op de achtergrond.
- Saga zoekt bij Open Library en via de sleutelvrije Google Books-feed die Calibre als compatibiliteitsbron gebruikt.
- Wanneer beide online bronnen geen kandidaat opleveren, maakt Saga lokaal één eenvoudige omslag met titel en auteur.
- Saga toont gevonden kandidaten en kiest nooit zelfstandig een omslag.
- Het venster toont maximaal twaalf geldige kandidaten, gesorteerd op afbeeldingsoppervlak.
- De gekozen omslag wordt als beheerd `cover.jpg` opgeslagen; native write-back in ebookbestanden blijft buiten scope.
- De eerder voorgestelde aparte instellingenpagina `Kwaliteit` blijft gewenst, maar volgt na de twee resterende herstelacties van de Quality Page.

Deze aannames zijn functioneel goedgekeurd op 4 september 2026.

## Doel

Een gebruiker kan op de Quality Page één boek zonder omslag selecteren, online naar passende omslagen zoeken en bewust één resultaat kiezen. Na bevestiging bewaart Saga de gekozen afbeelding veilig in de actieve bibliotheek en verdwijnen de melding en de rij direct wanneer het boek niet langer aan `Geen omslag` voldoet. Vanuit het detailscherm kan de gebruiker dezelfde keuze altijd openen, ook wanneer het boek al een omslag heeft. Daar wordt de wijziging samen met de overige boekgegevens pas definitief bij `Opslaan`.

De ervaring neemt het gedrag van Calibre als referentie: zoeken met bestaande metadata, kandidaten in een visuele keuzelijst tonen, resultaten valideren, bron en resolutie zichtbaar maken en toetsenbordselectie ondersteunen. Saga kopieert geen Calibre-code.

## Gebruikersverloop

1. De gebruiker selecteert `Geen omslag` en precies één boek.
2. De knop `Omslag zoeken` wordt beschikbaar.
3. Saga opent een modaal venster en zoekt op titel plus auteur en, wanneer aanwezig, aanvullend op ISBN.
4. Geldige resultaten verschijnen met miniatuur, bron en resolutie.
5. De gebruiker selecteert één kandidaat met muis of toetsenbord.
6. `Omslag gebruiken`, dubbelklik of Enter bevestigt de selectie; `Annuleren` of Escape verandert niets.
7. Saga downloadt en valideert de gekozen grote afbeelding opnieuw, schrijft `cover.jpg` veilig en slaat het bijgewerkte boek op.
8. Quality Page, hoofdgrid, boekenplank en detailpaneel worden direct bijgewerkt.

## Zoekbronnen en lokale noodomslag

Open Library wordt benaderd via de gedocumenteerde Search API en Covers API:

- `https://openlibrary.org/search.json` zoekt relevante werken en edities.
- Alleen noodzakelijke velden worden opgevraagd, waaronder titel, auteurs, ISBN en `cover_i`.
- Een aanwezig ISBN levert een exacte zoekroute; een tweede titel-en-auteurroute vindt ook omslagen van andere edities. De resultaten worden daarna samengevoegd.
- Omslagen worden via een numerieke Cover ID van `https://covers.openlibrary.org` opgehaald.
- Kandidaten worden op Cover ID gededupliceerd en tot maximaal twaalf resultaten beperkt.
- Saga stuurt een herkenbare User-Agent en respecteert annulering, time-outs en serverfouten.
- Saga crawlt niet en doet geen zoekopdracht zonder expliciete gebruikersactie.

Google Books wordt aanvullend benaderd via de openbare Atom-feed die Calibre gebruikt. Deze route vereist geen sleutel, maar is minder stabiel en daarom een compatibiliteitsbron: fouten worden geïsoleerd en verhinderen Open Library of de lokale noodomslag niet. Saga accepteert uitsluitend geldige boek-ID's en bouwt afbeeldingsadressen zelf op voor vaste Google Books-hostnamen. Google Afbeeldingen wordt niet gescrapet.

De twee bronnen worden gelijktijdig geraadpleegd. Hun geldige resultaten worden afwisselend samengevoegd, gededupliceerd en samen tot twaalf kandidaten begrensd, zodat één bron de andere niet verdringt. Wanneer geen online kandidaat overblijft, genereert Saga lokaal één omslag van 1200 bij 1600 pixels met de actuele titel en auteur. Ook deze kandidaat wordt nooit automatisch gekozen.

Open Library-documentatie:

- https://openlibrary.org/dev/docs/api/search
- https://openlibrary.org/dev/docs/api/covers

## Architectuur

### Zoekcontract

De applicatielaag definieert een klein, brononafhankelijk contract met:

- een zoekvraag met titel, auteurs en optioneel ISBN;
- een kandidaat met een ondoorzichtige kandidaat-ID, bron, boekcontext, afbeeldingsbytes en afmetingen;
- een asynchrone zoekactie met annulering;
- een aparte actie die de gekozen grote afbeelding definitief ophaalt.

Iedere online bron implementeert hetzelfde broncontract met een vaste bronsleutel. Een samengestelde zoekservice verdeelt downloads uitsluitend naar een geregistreerde bron en voegt de resultaten begrensd samen. Open Library en Google Books staan in de infrastructuurlaag; de lokale WPF-omslaggenerator staat in de app-laag. UI- en applicatielagen bouwen zelf geen externe URL op.

### Netwerk- en afbeeldingsgrenzen

Alle gegevens van Open Library en Google Books zijn onbetrouwbare externe invoer. De implementatie:

- gebruikt uitsluitend HTTPS en vaste Open Library- en Google Books-hostnamen;
- accepteert alleen numerieke Open Library Cover ID's en streng begrensde Google Books-ID's uit zoekantwoorden;
- begrenst antwoordgrootte, aantal resultaten, downloadtijd en iedere afbeelding tot 10 MiB, gelijk aan Saga's bestaande importgrens voor omslagen;
- accepteert alleen een decodeerbare JPEG met redelijke afmetingen;
- weigert afbeeldingen kleiner dan 50 bij 50 pixels, lege bestanden en buitensporig grote afbeeldingen;
- toont een gelokaliseerde fout zonder het boek te wijzigen wanneer zoeken of downloaden mislukt.

### Opslag

Saga heeft al `CoverBytes` en `CoverRelativePath`, maar nog geen afzonderlijke vervangactie voor een beheerde omslag. Deze milestone voegt een klein omslagopslagcontract toe dat:

- uitsluitend binnen `books/<boek-id>/cover.jpg` van de actieve bibliotheek schrijft;
- eerst naar een tijdelijk bestand schrijft en dit daarna atomair vervangt;
- het relatieve pad teruggeeft;
- het nieuwe bestand opruimt wanneer de daaropvolgende boekopslag mislukt.

De Quality Page-herstelservice haalt het actuele boek opnieuw op en controleert vlak voor schrijven dat `missing-cover` nog van toepassing is. Een algemene omslagwijzigingsservice verzorgt hetzelfde veilige bestandspad voor het detailscherm en staat daar ook vervanging van een bestaande omslag toe. In het detailscherm blijft de keuze eerst een niet-opgeslagen wijziging: `Opslaan` bewaart omslag en overige velden samen, terwijl `Ongedaan maken` de oorspronkelijke omslag herstelt. De bestaande `BookService` blijft verantwoordelijk voor SQLite, sidecarverwerking en bekende write-backstatussen.

## Technische basis

- .NET 10 en C#
- WPF en CommunityToolkit.Mvvm
- ingebouwde `HttpClient` en `System.Text.Json`; geen nieuwe NuGet-dependency
- bestaande repository-, bestandsopslag-, lokalisatie- en interactiepatronen van Saga

## Commando's

```powershell
dotnet restore EbookManager.sln
dotnet test EbookManager.sln -c Debug
dotnet build EbookManager.sln -c Debug
dotnet run --project src/EbookManager.App/EbookManager.App.csproj
```

Gerichte tests worden tijdens implementatie met `dotnet test` en een `FullyQualifiedName`-filter uitgevoerd.

## Projectstructuur

- `src/EbookManager.Application/Metadata` bevat zoekcontracten en de herstelworkflow.
- `src/EbookManager.Infrastructure/Metadata` bevat de Open Library-client en afbeeldingsvalidatie.
- `src/EbookManager.Infrastructure/Files` bevat veilige beheerde omslagopslag.
- `src/EbookManager.Presentation/ViewModels` bevat het keuzemodel en de dashboardkoppeling.
- `src/EbookManager.App/Views` bevat het WPF-keuzevenster.
- `src/EbookManager.App/Resources/Strings` bevat alle zichtbare en toegankelijke teksten.
- `tests/EbookManager.Tests` volgt dezelfde functionele indeling.

## Codestijl

Nieuwe afhankelijkheden worden via constructorinjectie aangeboden en asynchrone I/O geeft de `CancellationToken` altijd door:

```csharp
public interface IBookCoverSearchService
{
    Task<BookCoverSearchResult> SearchAsync(
        BookCoverSearchQuery query,
        CancellationToken cancellationToken);
}
```

Resultaatstatussen zijn expliciete records of enums; verwachte netwerk- en opslagfouten worden niet als ongefilterde exceptions aan de UI doorgegeven.

## Teststrategie

- Contracttests voor zoekopbouw, JSON-verwerking, deduplicatie, limieten, annulering en ongeldige antwoorden.
- Netwerktests gebruiken een gecontroleerde HTTP-handler en benaderen Open Library niet werkelijk.
- Opslagtests controleren padbeveiliging, tijdelijk schrijven, vervangen en opruimen wanneer de database aantoonbaar niet is bijgewerkt.
- Servicetests controleren herladen, opnieuw evalueren, behoud van overige metadata en foutstatussen.
- Viewmodel- en layouttests controleren knopcontext, laden, selectie, annuleren, Enter, dubbelklik, foutmeldingen en lokalisatie.
- Volledige bestaande tests en een Debug-build moeten groen blijven.
- Een handmatige checklist controleert de echte Open Library-route met een representatief boek met en zonder ISBN.
- Aanvullende tests controleren broncombinatie, Google-feedverwerking, lokale generatie en het vervangen, annuleren en ongedaan maken van een bestaande omslag in het detailscherm.

## Grenzen

### Altijd

- Het actuele boek en de kwaliteitsregel opnieuw controleren voordat Saga schrijft.
- Externe antwoorden begrenzen en valideren.
- De gebruiker één specifieke kandidaat laten bevestigen.
- Alle zichtbare en toegankelijke teksten in zes talen leveren.
- Bestaande metadata, formaten en leesstatus ongewijzigd laten.

### Eerst overleggen

- Een online bron buiten Open Library en de sleutelvrije Google Books-feed toevoegen, of een API-sleutel introduceren.
- Een nieuwe NuGet-dependency toevoegen.
- Het databaseschema of sidecarformaat wijzigen.
- Omslagen automatisch vervangen zonder expliciete keuze en normaal opslaan.

### Nooit in deze milestone

- Automatisch de eerste of grootste omslag opslaan.
- Willekeurige URL's uit externe antwoorden downloaden.
- Google Afbeeldingen scrapen.
- Omslagen in EPUB-, PDF- of andere ebookbestanden schrijven.
- Meerdere boeken tegelijk wijzigen.

## Acceptatiecriteria

- `Omslag zoeken` is uitsluitend actief voor één geselecteerd boek onder `Geen omslag`.
- `Omslag wijzigen` is in het detailscherm beschikbaar voor ieder geladen boek, ongeacht of al een omslag bestaat.
- Een zoekopdracht gebruikt de actuele titel en auteurs en zoekt aanvullend exact op ISBN wanneer dat beschikbaar is.
- Maximaal twaalf unieke, geldige kandidaten worden met bron en resolutie getoond.
- Geen resultaten, annuleren, een time-out of een ongeldige download verandert niets.
- Als beide online bronnen niets vinden, verschijnt één lokaal gegenereerde kandidaat met titel en auteur.
- Een gekozen omslag wordt veilig als beheerd `cover.jpg` opgeslagen en in SQLite als bytes en relatief pad vastgelegd.
- De Quality Page, hoofdgrid, boekenplank en het detailpaneel tonen de wijziging zonder herstart.
- De gekozen rij verdwijnt uit `Geen omslag` en alle tellingen blijven correct.
- Muis, Enter, dubbelklik, Escape en schermlezernamen werken.
- In het detailscherm kan de gebruiker de nieuwe omslag met `Opslaan` bewaren of met `Ongedaan maken` verwerpen.
- Alle geautomatiseerde tests en de Debug-build slagen zonder waarschuwingen.

## Buiten scope en vervolg

- `Rommelige tags` wordt de volgende afzonderlijke Quality Page-slice.
- Een apart tabblad `Kwaliteit` in Instellingen volgt na de resterende herstelacties.
- Andere bronnen waarvoor een officiële toegang of account nodig is, kunnen later via hetzelfde zoekcontract worden toegevoegd.
- Handmatig een lokaal omslagbestand kiezen volgt later; vervangen via de zoek- en generatieroute valt nu binnen scope.
- Native cover-write-back in ebookbestanden blijft een afzonderlijke, formaatgerichte feature.

## Open vragen

Geen blokkerende vragen. De bestaande Saga-grens van 10 MiB wordt ook voor online omslagen gebruikt.
