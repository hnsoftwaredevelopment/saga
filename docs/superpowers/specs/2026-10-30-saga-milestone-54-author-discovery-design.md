# Milestone 54 — auteursduplicaten vinden en vergelijken

## Opdracht en basis

De gebruiker heeft de twee vervolgverbeteringen goedgekeurd: eerst beschikbare auteursgegevens naast elkaar vergelijken, daarna Duplicaten zoeken. De bestaande handmatige samenvoeging werkt volgens de gebruiker. PR #55 staat bij aanvang nog open; deze aparte branch codex/milestone-54-author-duplicate-discovery bouwt daarom voort op diens geteste head 877fb91. PR #55 is inmiddels gemerged en de vervolgbranch is zonder bronwijzigingen herplaatst op main 3377d8e; de gewone vervolg-PR richt zich op main. Alleen de gebruiker mergt.

## Gebruikersgedrag

- Het samenvoegvenster toont de geselecteerde auteurs naast elkaar, met namen, boekaantallen, foto's, volledige scrollbare biografieën en websites. Direct bij de zichtbare bronwaarden kan de gebruiker kiezen wat behouden blijft. Het resultaatpaneel toont de gekozen naam, biografie, website en foto. Bestaande expliciete veldkeuzes en Leeg laten blijven werken; geen extra boekselectie of automatische samenvoeging.
- Auteurs beheren krijgt Duplicaten zoeken. Een apart overzicht toont mogelijke paren, boekaantallen en een begrijpelijke reden. De gebruiker kiest een voorstel, opent de vergelijking en bepaalt zelf of het dezelfde persoon betreft. Een voorstel wijzigen of sluiten schrijft niets.
- Herken conservatief verschillen in hoofdletters, diakritische tekens, spaties/punten, achternaam-komma-voornaam en passende initialen tegenover uitgeschreven voornamen. Houd Nederlandse tussenvoegsels en samengestelde achternamen bij de achternaam. Alleen dezelfde achternaam is onvoldoende. Verschillende volledig uitgeschreven voornamen worden niet via een gedeelde initiaal automatisch gelijkgesteld.
- Lees alleen lichte auteur-ID/naam/aantal-projecties voor de actieve bibliotheek, inclusief pagina's voorbij 200. Maak vergelijkingen binnen achternaam/initiaalbuckets, niet kwadratisch over alle auteurs. Zoek invoer filtert voorstellen op beide namen. Maximaal 1000 zichtbare voorstellen per zoekopdracht, met expliciete melding en verfijnen van zoektekst bij bereiken van de grens. Voorstellen zijn paren; geen transitieve automatische groepen of vooraf aangevinkte samenvoegingen.
- Zoeken en berekenen gebeurt buiten de UI-thread, met annulering en bescherming tegen late resultaten. Foto's worden pas in de vergelijking geladen, beperkt tot zichtbare kaarten en met begrensde gelijktijdigheid. Niet alle auteursfoto's in het geheugen laden.
- Samenvoegen gebruikt exact de bestaande transactionele AuthorMergeService-route: expliciete bestemming/veldbronnen, stale checks, dubbelboekweigering, profiel-/fotoveiligheid, metadata.json/ebook-synchronisatie en afzonderlijke waarschuwingen. Na succes verversen voorstellen, doelauteur, aantallen en uiteindelijk het hoofdscherm. Geen foutwaarschuwingen verbergen bij verversen.
- Toetsenbord, focus, annuleren, minimumformaat, zichtbaar resultaat en beide thema's worden gecontroleerd. Alle nieuwe teksten in zes talen. Handmatige vergelijking van zelf geselecteerde auteurs blijft beschikbaar.

## Uitvoering en verificatie

Geen schema, dependency, externe zoekbron, nieuwe doelnaam, pseudoniemherkenning of persistente negeerlijst. Geen automatische gegevenswijziging tijdens detectie. Naamnormalisatie voor voorstellen verandert geen opgeslagen namen of bestaande identiteitsregels.

Gerichte pure matchlogica onder Application/Authors met xUnit-voorbeelden en negatieve gevallen. Testbare zoek-/vergelijkingsmodellen onder Presentation; WPF onder App/Views. Bestaande repositories en BookService blijven verantwoordelijk voor opslag. Test eerst kleine betekenisvolle gevallen rood/groen, daarna geïntegreerde WPF-workflows met fictieve data. Geen subagents nodig.

1. Pure suggestielogica en lichte paginering, met tests voor genoemde voorbeelden, tussenvoegsels, onjuiste naamparen, grens en annulering.
2. Bronkaarten en resultaatvergelijking op de bestaande dialoog, expliciete veldknoppen en begrensde foto-loading; dialoog-/asset-/WPF-tests.
3. Voorstellenoverzicht, zoek-/selectie-/vergelijkingsroute, herladen na samenvoegen en behoud van waarschuwingen; viewmodel- en WPF-tests.
4. Zes talen, volledige suite, schone enige Debug-build, eigen vijfassige review, checklist, README/status en exact gecontroleerde Obsidian-spiegels. Handmatige gebruikersacceptatie blijft apart.

Commando's: `dotnet test EbookManager.sln -c Debug --no-restore`; `dotnet run --project tests/EbookManager.WpfSmokeTests/EbookManager.WpfSmokeTests.csproj -c Debug --no-restore`; `dotnet build src/EbookManager.App/EbookManager.App.csproj -c Debug --no-restore`. Project-DoD geldt; alleen de gebruiker accepteert en mergt.

## Uitvoering en verificatie

PR #55 is tijdens de uitvoering gemerged naar main 3377d8e. De aparte vervolgbranch is daarop herplaatst; de bronboom vóór en na deze rebase is exact gelijk gecontroleerd. De vervolg-PR richt zich daarom normaal op main, zonder afhankelijkheid van een nog open PR.

Gebouwd in afzonderlijke geteste commits. De zoeklogica gebruikt lichte auteurpagina's van 500, een alleen in het venster gecachte index, achternaam/initiaalbuckets, twee positieve redenklassen en expliciete paren. Querywerk draait buiten de UI-thread, annulering/versies bewaken late resultaten, en de limiet vermeldt dat verfijnen mogelijk is. Opmaaknormalisatie wijzigt geen opgeslagen namen. Onbruikbare namen met cijfers of scheidingstekens voor meerdere personen worden niet als bewijs gebruikt.

Bronkaarten zijn horizontaal gevirtualiseerd. Namen en bestemming blijven zichtbaar bij scrollen; volledige biografieën zijn bereikbaar. Bronfoto's laden met maximaal twee gelijktijdige verzoeken, worden bij recycling gewist en krijgen versiecontrole tegen late resultaten. De resultaatfoto blokkeert bevestigen tijdens laden of bij een fout. Bronknoppen veranderen uitsluitend de expliciete veldkeuzes in de bestaande dialoog; samenvoegen gebruikt AuthorMergeService ongewijzigd.

Eigen review op correctheid, eenvoud, architectuur, veiligheid en prestaties vond aanvullend een bestaand verversingspad dat een mislukte indexreload na commit niet eerlijk meldde. Twee eerst falende regressies voor handmatige en voorgestelde paren zijn hersteld: waarschuwingen/boek-ID's blijven behouden, oude tellingen worden niet als nieuw gepresenteerd en de gebruiker krijgt opgeslagen-maar-niet-ververst feedback.

De volledige suite slaagt met **1099 tests, 0 mislukt/overgeslagen**. **25 echte WPF-scenario's groen**, inclusief bronknoppen met echte kleine JPEG's, foto-concurrentie, lange biografieën, zoeken, annuleren en expliciete samenvoeging vanuit voorstellen. Beide nieuwe vensters zijn in licht/donker gerenderd en bekeken. Bestaande tests gebruiken uitsluitend fictieve of tijdelijke bibliotheken. De [checklist](../../manual-tests/milestone-54-checklist.md) staat klaar voor gebruikersacceptatie; moeilijke bestandsfouten hoeven niet handmatig te worden geforceerd.

Definitieve Debug-build: **2026.10.30.45**, **30 oktober 2026 22:53:25 Europe/Amsterdam**, **0 waarschuwingen/fouten** en precies één Saga.exe onder Builds/Debug. README, featurestatus, checklist, specificatie en taken worden exact naar Obsidian gespiegeld en met gelijke hashes gecontroleerd. Alleen handmatige gebruikersacceptatie en merge staan nog open.

Aanvulling na de gebruikerstest: na een geslaagde handmatige of voorgestelde samenvoeging en sluiten van het betreffende venster wordt het zoekveld in Auteurs beheren gewist. De ongefilterde index wordt expliciet herladen; geen vertraagde tekstzoekactie die opnieuw op de doelauteur filtert. De doelauteur en haar gegevens, melding en waarschuwingen blijven behouden; als zij in de geladen pagina staat wordt de rij opnieuw geselecteerd. Annuleren zonder samenvoeging bewaart de zoektekst. De echte WPF-route test beide paden met een niet-betrokken auteur en met paginering voorbij 200. De wens om de vergelijking inclusief iconen meer op boekduplicaten te laten aansluiten is als afzonderlijke vervolgverbetering vastgelegd.
