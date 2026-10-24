# Milestone 48: auteursbeheer met foto en website

## Status en doel

Specificatie en technische keuzes zijn door de gebruiker bevestigd; implementatie op codex/milestone-48-author-profiles vanaf actuele main 1de190f. Een doorzoekbaar auteursbeheer met foto, optionele website, korte biografie en boeken van de auteur is gebouwd. Handmatige gebruikersacceptatie staat nog open. Opslagbesluit: docs/decisions/001-author-profile-storage.md.

## Eerste slice

- Een eigen venster Auteurs beheren, bereikbaar vanuit het hoofdscherm bij een geopende bibliotheek. Links een doorzoekbare auteurslijst met aantallen boeken; rechts het profiel en de boeken van de geselecteerde auteur.
- De bestaande auteursnaam is zichtbaar maar in deze slice niet wijzigbaar. Foto, website en biografie zijn optioneel en handmatig te onderhouden. Een profiel opslaan verandert geen auteursnamen of andere metadata in boeken.
- Foto kiezen, vervangen en verwijderen met voorbeeld; Opslaan bevestigt wijzigingen. Annuleren bewaart het oude profiel en de oude foto. Bij wisselen van auteur of sluiten met wijzigingen: Opslaan, Wijzigingen verwerpen of Annuleren.
- Website opslaan en op expliciete klik in de standaardbrowser openen. Leegmaken verwijdert de website. Geen automatisch ophalen van pagina's, foto's of metadata.
- De boekenlijst gebruikt de bestaande auteurkoppeling op identiteit, ook bij meerdere auteurs; geen deelwoordvergelijking. Alleen boeken uit de actieve bibliotheek worden getoond. Deze lijst is in de eerste slice alleen een overzicht, geen nieuwe boekbewerkingsroute.
- Profielen blijven bibliotheekgebonden en verhuizen mee met de bibliotheek. Een ontbrekende foto geeft een rustig standaardbeeld, geen crash.
- De auteurslijst toont auteurs met boeken en auteurs met opgeslagen profielinhoud. Een auteur met nul boeken en profielinhoud blijft bewaard. De bestaande opschoning van lege, ongebruikte auteurs blijft mogelijk. Het scherm maakt nog geen losse nieuwe auteurs aan; bekende onbekend-auteurplaatsaanduidingen krijgen geen profiel.

## Technische inventarisatie en voorgestelde keuze

Er bestaan al Authors en BookAuthors met stabiele Guid-identiteiten en een unieke NormalizedName. Hergebruik deze identiteit: geen tweede onafhankelijk auteursregister en geen migratie op basis van naamvergelijkingen. Voorgesteld worden optionele velden Biography, WebsiteUrl en PhotoRelativePath op AuthorEntity, met een additieve EF-migratie. Bestaande auteurs, koppelingen, sorteervolgorde en namen blijven ongewijzigd. Alternatief is een aparte één-op-één-profieltabel; die introduceert voor deze drie optionele velden extra koppelingen zonder nu benodigde aparte levenscyclus. Na bevestiging wordt deze keuze als geaccepteerd architectuurbesluit vastgelegd.

Beide RemoveOrphanedMetadataAsync-varianten in EfBookRepository verwijderen momenteel ongebruikte auteurs. Pas beide gericht aan: verwijder alleen auteurs zonder boeken én zonder profielinhoud. Test verwijderen, samenvoegen en auteur wijzigen, inclusief opnieuw koppelen aan een eerder bewaard profiel. Profielgegevens worden nooit automatisch aan een andere auteursidentiteit overgedragen.

Een gerichte Application-service en repositorycontract verzorgen overzicht, boeken en profielopslag; een afzonderlijk Presentation-viewmodel beheert selectie, conceptwijzigingen en bezetstatus. Geen extra omvangrijke functionaliteit in LibraryViewModel. Infrastructure verzorgt EF en fotobestanden; WPF verzorgt bestandskiezer en browser. Concrete contractnamen worden tijdens implementatie gekozen volgens bestaande conventies.

Stijlvoorbeeld uit bestaande code: `public async Task<string> SaveAsync(Guid bookId, byte[] coverBytes, CancellationToken cancellationToken)` in ManagedLibraryCoverStore. Volg dezelfde async/cancellation-, namespace- en nullable-conventies, maar overschrijf een bestaande profielfoto niet voordat de profielopslag veilig is afgerond. Tests gebruiken de bestaande xUnit/FluentAssertions-stijl.

## Foto-opslag, validatie en foutafhandeling

Voorgestelde relatieve indeling: authors/<eerste twee hextekens auteur-id>/<auteur-id>-<foto-id>.jpg. Zo ontstaat niet opnieuw één platte map met tienduizenden auteursdirectories. De bestandsnaam is onafhankelijk van de auteursnaam. Het oorspronkelijke gekozen bestand blijft onaangetast; alleen een verwerkte kopie komt in de bibliotheek.

Voorgestelde grenzen: JPEG/PNG, maximaal 10 MiB invoer, maximaal 25 miljoen bronpixels, verwerkt naar maximaal 1024 pixels aan de langste zijde. Controleer inhoud en afbeeldingsafmetingen vóór volledige verwerking; een extensie alleen is onvoldoende. Ongeldige, te grote of beschadigde afbeeldingen geven begrijpelijke feedback en laten de oude foto intact. Decodeer buiten de UI-thread, met begrensde verwerking en zonder bestanden langdurig open te houden.

Schrijf eerst een nieuwe unieke beheerde foto, sla daarna de nieuwe relatieve verwijzing op en verwijder pas na succesvolle databaseopslag de oude beheerde foto. Een mislukte opslag behoudt het oude profiel; ruim uitsluitend het nieuwe eigen bestand op. Een procesonderbreking kan een ongebruikt nieuw bestand achterlaten, maar geen bestaande foto overschrijven. Bij mislukte opruiming na geslaagde opslag: correcte successtatus met aparte waarschuwing, niet doen alsof het profiel niet is opgeslagen. Geen automatische brede schijfopschoning in deze slice.

Valideer bibliotheekrelatieve paden en bewaak mapgrenzen/reparse points volgens ManagedLibraryStorageLayout. Verwijder nooit het gekozen bronbestand of een niet-beheerd bestand. Foto's van andere auteurs blijven intact.

Website: alleen absolute HTTP/HTTPS-adressen met host, zonder ingebouwde gebruikersnaam/wachtwoord; overige schema's zoals file en javascript weigeren. Stel maximaal 2048 tekens voor, biografie maximaal 10000 tekens als gewone tekst. Open uitsluitend op gebruikersactie via een afgeschermde browseractie; geen shellopdracht opbouwen en geen netwerkverzoek door Saga. Lege waarden zijn geldig. Toon geen technische exceptions of SQL in foutmeldingen.

Bij opslaan: oorspronkelijke auteursidentiteit en profielwaarden controleren; inmiddels gewijzigde/verwijderde gegevens niet stil overschrijven. Een fout behoudt het bewerkbare concept voor retry. Annulering en bibliotheekwissel mogen geen laat resultaat in een ander profiel tonen.

## Prestaties en bediening

Overzicht en zoeken laden alleen benodigde namen, aantallen en identiteiten, niet alle boeken of foto's. Begrens/gevirtualiseer de lijst, zoek asynchroon met korte debounce en verwerp verouderde resultaten. Geen database- of afbeeldingswerk op de UI-thread en geen query per zichtbare auteur voor tellingen. De geselecteerde auteur laadt één foto en een begrensde boekenlijst. Controleer op een representatieve grote testbibliotheek en vervolgens handmatig op de gebruikersbibliotheek, zonder productiedata voor fouttests te beschadigen.

Toetsenbordbediening, duidelijke focus, zichtbare splitter en begrensde detailvelden. Alle nieuwe teksten in de bestaande zes talen; korte groene bevestiging bij succes, concrete foutmelding bij fouten. Geen ruwe Engelstalige technische veldnamen in de interface.

## Uitvoering en verificatie

Structuur: Domain-contracten onder src/EbookManager.Domain, use-cases onder src/EbookManager.Application, database/foto-opslag onder src/EbookManager.Infrastructure, viewmodel onder src/EbookManager.Presentation en venster/vertalingen onder src/EbookManager.App. Tests onder tests/EbookManager.Tests in de bijpassende bestaande deelstructuren.

Eerst falende tests voor migratie/behoud van bestaande gegevens, profielopslag en orphan-behoud. Daarna fotovalidatie/padveiligheid, opslaan versus annuleren, websitevalidatie, auteurkoppelingen, fout/retry, verouderde resultaten en bibliotheekwissel. Test ook nul boeken, meerdere auteurs en een kapotte foto. Een gekopieerde tijdelijke testbibliotheek blijft bruikbaar zonder het oorspronkelijke fotobestand.

- Gericht: `dotnet test tests/EbookManager.Tests/EbookManager.Tests.csproj -c Debug --filter FullyQualifiedName~Author --no-restore`.
- Volledig: `dotnet test EbookManager.sln -c Debug --no-restore`.
- Build: `dotnet build src/EbookManager.App/EbookManager.App.csproj -c Debug --no-restore`.
- Diffcontrole: `git diff --check`.

Aanvullende foto-, migratie-, layout- en lokalisatietests expliciet uitvoeren: het Author-filter dekt niet automatisch alles. Handmatige checklist: docs/manual-tests/milestone-48-checklist.md. Volg docs/definition-of-done.md; eigen code-review vóór een gewone niet-draft PR. Alleen de gebruiker mergt. Handmatige acceptatie wordt niet vooraf afgevinkt.

## Implementatie en review

De auteurrepository geeft geprojecteerde pagina's van maximaal 200 regels aan het scherm, met tellingen in de database. Foto's worden alleen voor de geselecteerde auteur geladen. Een modal venster bindt alle acties aan de geopende bibliotheek; de hoofdpagina kan daardoor niet tussendoor naar een andere bibliotheek schakelen. Het eigen viewmodel beheert concept, status, paginering en verouderde selectieresultaten. De fotokeuze verwerkt JPEG/PNG buiten de UI-thread, controleert afmetingen en encodeert een begrensde JPEG zonder de oorspronkelijke bestandsverwijzing te bewaren.

Eigen review omvat correctheid, eenvoud, architectuur, veiligheid en prestaties. Extra regressiedekking voor een mislukte selectie die anders een oud profiel bewerkbaar liet, en voor databasefouten vóór/na commit: geen verwezen foto verwijderen. Als ook herlezen mislukt, blijven bestanden bewust behouden. De splitter heeft in dit venster een lokale, thema-afhankelijke zichtbare greep. Geen nieuwe dependency, online bron, boekmetadatawijziging of bestaande opslagconversie.

Een tijdelijke WPF-rendercontrole met fictieve auteurs bevestigt de indeling, zichtbare splitter en minimale titelkolombreedte. De echte beeldverwerking is daarbij gecontroleerd met PNG-naar-JPEG en een ongeldig afbeeldingsbestand. Dit vervangt niet de handmatige test op de gebruikersbibliotheek; de actuele test-/buildgegevens staan in de checklist.

## Buiten scope en grenzen

Geen auteurs hernoemen/samenvoegen, aliassen/pseudoniemen, geboorte-/sterfdatums, online profielzoeken, BoekenBase-koppeling, wijziging van ebookbestanden of nieuwe synchronisatiedienst. OneDrive mag de bibliotheek zoals andere bestanden blijven synchroniseren, maar gelijktijdige bewerking op twee computers is geen nieuwe ondersteunde functie. Geen dependency toevoegen zonder afstemming. Geen echte bibliotheek migreren tijdens voorbereiding. Alle gewijzigde Markdown naar Obsidian spiegelen.
