# Saga Milestone 37: Eigen tabblad Kwaliteit in Instellingen

## Doel

Geef kwaliteitsbeheer een herkenbare eigen plaats in Instellingen. De bestaande toegang tot genegeerde kwaliteitsmeldingen verdwijnt uit `Duplicaten` en komt ongewijzigd onder een nieuw tabblad `Kwaliteit` te staan.

## Aannames en afbakening

- De bestaande beheerfunctie en het bestaande beheervenster blijven ongewijzigd werken.
- Het tabblad bevat in deze slice alleen uitleg en de actie om genegeerde kwaliteitsmeldingen te beheren.
- Het tabblad is bedoeld als vaste plaats voor toekomstige kwaliteitsinstellingen, zonder die vervolgfuncties nu al toe te voegen.
- Er verandert niets aan kwaliteitsregels, uitsluitingen, databaseopslag of boekmetadata.
- Bulkselectie, bulkherstel en de toekomstige icoonknop voor `Dit is correct` vallen buiten deze milestone.

## Gebruikersroute

1. De gebruiker opent Instellingen.
2. Naast de bestaande tabbladen staat een gelokaliseerd tabblad `Kwaliteit`.
3. Het tabblad legt uit dat eerder als correct gemarkeerde kwaliteitsmeldingen hier kunnen worden bekeken en hersteld.
4. De actie opent het bestaande beheervenster voor de actieve bibliotheek.
5. Onder `Duplicaten` staan alleen nog instellingen die werkelijk bij duplicaten horen.

## Technische aanpak

- Verplaats het bestaande XAML-blok met `ManageMetadataQualityExclusionsButton` naar een eigen `TabItem`.
- Voeg `SettingsQualitySection` toe aan alle zes ondersteunde resourcebestanden.
- Houd opdrachtbinding, toegankelijke naam, focusgedrag en het bestaande beheervenster intact.
- Voeg een structurele XAML-test toe die bewijst dat de actie onder `Kwaliteit` staat en niet meer onder `Duplicaten`.
- Breid de lokalisatietest uit met de nieuwe tabtitel.

## Teststrategie

- Eerst een falende layouttest voor de nieuwe tabstructuur.
- Gerichte tests voor de Settings-layout en kwaliteitslokalisatie.
- Volledige Debug-testset en een schone Debug-build.
- Handmatige controle in de actuele build: tab zichtbaar, duplicatentab opgeschoond, beheeractie opent en herstellen blijft werken.

## Acceptatiecriteria

- Instellingen bevat een eigen, correct vertaald tabblad `Kwaliteit`.
- De bestaande kwaliteitsbeheeractie staat uitsluitend onder dit tabblad.
- `Duplicaten` bevat geen kwaliteitsbeheer meer.
- De knop blijft met muis en toetsenbord bruikbaar en heeft een toegankelijke naam.
- Het bestaande bekijken en herstellen van genegeerde kwaliteitsmeldingen werkt ongewijzigd.
- Geen database, kwaliteitsregel of boekmetadata wordt gewijzigd door deze verplaatsing.

## Verificatieopdrachten

- `dotnet test tests/EbookManager.Tests/EbookManager.Tests.csproj -c Debug --filter "FullyQualifiedName~SettingsWindowQualityExclusionsLayoutTests|FullyQualifiedName~MetadataQualityLocalizationTests"`
- `dotnet test EbookManager.sln -c Debug`
- `dotnet build src/EbookManager.App/EbookManager.App.csproj -c Debug --no-restore`

## Open vragen

Geen blokkerende open vragen. De door de gebruiker gekozen volgorde is: eerst dit tabblad, daarna bulkbeslissingen en vervolgens bulkherstel.
