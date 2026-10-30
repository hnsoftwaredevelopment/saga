# Milestone 55 — seriesvarianten vinden en samenvoegen

Afronding 31 oktober 2026: de gebruiker heeft het samenvoegen van series opnieuw getest zonder problemen en bevestigt dat PR #56 en PR #57 zijn gemerged. GitHub bevestigt beide merges; lokale main is bijgewerkt naar 373d45f. Milestones 54 en 55 zijn afgerond. Onderstaande uitvoeringsnotities en individuele checklistvakjes blijven als historie behouden. De UI-/icoonharmonisatie blijft een afzonderlijke wens voor later.

## Opdracht en basis

De gebruiker heeft de voorgestelde slice goedgekeurd: mogelijke dubbelen vinden, boeken/serienummers vergelijken, één bestaande serienaam kiezen en alle betrokken boeken daaronder samenbrengen. Nummers en overige metadata blijven behouden; gelijke nummers worden zichtbaar gemaakt zonder automatisch te hernummeren. De vorige UI-/icoonharmonisatie is uitgesteld.

Bij aanvang staat PR #56 nog open. Branch codex/milestone-55-series-merge begint vanaf diens geteste head 56c4f49. Als PR #56 tijdens de bouw wordt gemerged, herplaats het vervolg op actuele main met controle dat de bronboom gelijk blijft. Anders een gewone vervolg-PR op de vorige branch; alleen de gebruiker mergt.

## Gebruikersroute en criteria

1. Series beheren krijgt Duplicaten zoeken. Een apart venster toont mogelijke paren met beide serienamen, boekaantallen en reden. Een geselecteerd paar kan worden vergeleken; er wordt niets automatisch samengevoegd.
2. Detectie is conservatief: verschillen in spaties, leestekens of diakritische tekens bij dezelfde letter-/cijferreeks. Hoofdletter-/trimvarianten zijn al één groep volgens SeriesNames en worden niet opnieuw voorgesteld. Cijfers blijven behouden, dus Serie 1 en Serie 2 of 01 en 1 worden niet stil gelijkgesteld. Geen afkortings-, taal- of pseudoniemgissingen.
3. Gebruik een lichte index van bestaande series, met filteren buiten de UI-thread, annulering, bescherming tegen late resultaten en maximaal 1000 zichtbare voorstellen met verfijningsmelding. Namen en aantallen, geen omslagen of ebookbestanden tijdens detectie.
4. Het vergelijkingsvenster toont voor het gehele paar de huidige serie, nummer, boektitel en auteur(s), numeriek gesorteerd, ontbrekende nummers onderaan. Toon eerst maximaal 200 rijen met Meer laden; de preview/samenvoeging omvat ook niet-zichtbare boeken.
5. Gelijke niet-lege serienummers bij verschillende boeken worden expliciet aangeduid. Dit is informatie, geen blokkade of opdracht om nummers te wijzigen. Lege nummers en numerieke waarden zoals 1,5 blijven exact behouden.
6. Kies één bestaande serienaam uit het paar; geen vrije nieuwe doelnaam in deze route. De bevestiging toont bronseries en totale aantal boeken. Annuleren met knop/Escape schrijft niets. Na bevestigen krijgen de betrokken boeken de gekozen naam, zonder boeken samen te voegen of te verwijderen. Bestaande exact passende doelboeken worden niet opnieuw geschreven.
7. Voor opslag wordt de volledige bevestigde boekset opnieuw gecontroleerd, inclusief opgeslagen serienaam, nummer, titel en auteurs. Verouderde gegevens of gewijzigde koppelingen geven een conflict. De databasewijziging bestaat uitsluitend uit Series en UpdatedUtc voor daadwerkelijk gewijzigde boeken, in één transactie. Geen serietabel, schemawijziging, boekmapverplaatsing of nieuwe dependency.
8. Bestaande BookService-synchronisatie volgt na commit en loopt door na annulering van de aanvrager. Bestandswaarschuwingen zijn afzonderlijk zichtbaar; een verversingsfout na opslag meldt eerlijk dat de wijziging wel is opgeslagen en bewaart waarschuwingen/gewijzigde IDs. Geen nieuwe ebookwriter.
9. Na succes verversen voorstellen en seriesbeheer; na sluiten toont seriesbeheer de ongefilterde serielijst, het gekozen seriedetail en actuele boekaantallen. Hoofdscherm, details en seriefilter gebruiken de bestaande metadatareconciliatie. Zoektekst bij annuleren blijft behouden.
10. Bestaand afzonderlijk hernoemen blijft werken en blijft een bestaande andere doelnaam weigeren. Deze slice voegt alleen de expliciete voorstellen-/samenvoegroute toe, geen algemene vrije meervoudige serie-editor. Toetsenbord, focus, verkleinen en beide thema's blijven bruikbaar. Alle nieuwe tekst in zes talen.

## Structuur, risico's en verificatie

Gerichte immutable modellen onder Domain/Series en mergecontract onder Abstractions. EfSeriesRepository krijgt een gerichte partial; Application/Series detecteert namen en orkestreert bestaande BookService. Presentation bevat zoek-, vergelijkings- en beheerpartial; App de WPF-vensters. Bestaande async CancellationToken/ObservableObject-patronen volgen, geen generiek beheerframework of agents.

Uitvoering: eerst falende opslag-/naamtests, dan transactie en bestaande bestandssynchronisatie, testbare preview/zoeken, WPF-aansluiting, zes talen en integrale oplevering. Tests met tijdelijke SQLite-bibliotheken bewaken behoud van alle scalairen/nummers, gelijke en ontbrekende nummers, niet-betrokken boeken, meer dan 500 boeken, stale snapshots, rollback en annulering. Servicetests bewaken metadata.json en waarschuwingen na commit. WPF-proeven gebruiken fictieve gegevens en bewaken zoeken, expliciete keuze, annuleren, paginering, sluiten en directe verversing.

Commando's: `dotnet test EbookManager.sln -c Debug --no-restore`; `dotnet run --project tests/EbookManager.WpfSmokeTests/EbookManager.WpfSmokeTests.csproj -c Debug --no-restore`; `dotnet build src/EbookManager.App/EbookManager.App.csproj -c Debug --no-restore`. Project-DoD geldt: eigen vijfassige review, schone enige Debug-build met versie/tijd, actuele checklist/documentatie, exacte Obsidian-spiegels en gewone PR. Gebruiker accepteert en mergt. Buiten scope: nummers aanpassen/afleiden, ontbrekende delen, leesvolgorde, nieuwe doelnaam, vrij gekozen willekeurige series samenvoegen, nieuwe metadataformaten of UI-/icoonharmonisatie.

## Uitvoering en review

Gebouwd in afzonderlijke commits voor specificatie, opslag/detectie en WPF-workflow op basis 56c4f49 van de nog openstaande PR #56. Geen schema, dependencies of boekmapwijzigingen. De gewone vervolg-PR richt zich op die basisbranch; eerst PR #56 mergen en daarna deze PR naar main richten. Alleen de gebruiker accepteert en mergt.

Eerst falende opslagtests bewezen het ontbreken van de transactie; daarna zijn detectie, bestandssynchronisatie en vergelijking geverifieerd. De volledige suite slaagt: **1131 tests, 0 mislukt/overgeslagen**. **28 echte WPF-scenario's groen**, inclusief 205 boeken, annuleren, keuze, filtering/focus en directe actualisatie van voorstellen en beheer. Beide thema's zijn gerenderd en bekeken op minimale grootte. De nummerkolom is verbreed zodat de kop leesbaar blijft.

Eigen review op vijf assen uitgevoerd: alleen Series/UpdatedUtc schrijven met volledige snapshotcontrole en rollback; kleine gerichte partials en bestaande BookService-route; geen nieuwe algemene beheerabstractie; geparametriseerde EF-queries en geen nieuwe externe bron of geheimen; lichte gecachete naamindex buiten UI-thread, begrensde voorstellen en gevirtualiseerde pagina's zonder omslagen. Cijfers blijven ook buiten de basis-Unicodeletters behouden. Een filter zonder passende namen veroorzaakt geen volledige kwadratische parenvergelijking.

Extra grensproeven bewaken 511 boeken met rollback na de eerste updatebatch, gewijzigde/verwijderde boeken en gewijzigde auteurs sinds de preview, bestaande doelboeken zonder nieuwe schrijfactie, echte metadata.json, annulering na commit en behoud van waarschuwingen/boek-ID's bij een refreshfout. De foutmelding onderscheidt niet opgeslagen van wel opgeslagen maar niet vernieuwd.

Definitieve Debug-build **2026.10.31.6**, bestandsdatum **31 oktober 2026 00:08:58 Europe/Amsterdam**, met **0 waarschuwingen/fouten** en precies één Saga.exe onder Builds. De [Milestone 55 checklist](../../manual-tests/milestone-55-checklist.md) is klaar; de gebruiker heeft de gehele slice op 31 oktober 2026 geaccepteerd (“Deze slice is akkoord”). De merge staat nog open.

Gewone niet-draft [PR #57](https://github.com/hnsoftwaredevelopment/saga/pull/57) geopend op de branch van PR #56 en aan de chat gekoppeld. Mergeable bij opening; GitGuardian groen en CodeRabbit overgeslagen. CodeQL heeft op deze vervolgbranch nog geen resultaat; na omrichten naar main de beschikbare controles opnieuw bekijken. Alle zes gewijzigde Markdown-bestanden zijn exact gespiegeld en met SHA256 gecontroleerd.

## Aanvullende reviewcorrectie uit PR #56

Op 31 oktober is de afhankelijke branch bijgewerkt met commit 0060401 uit PR #56, via een gewone merge zonder geschiedenis te herschrijven. Doorlopende Tab-navigatie in de auteursvergelijking is eerst falend en daarna groen bewezen. De gezamenlijke suite slaagt met 1131 tests en 28 echte WPF-proeven. Debug 2026.10.31.6 van 31 oktober 00:08:58 Europe/Amsterdam is de enige Saga.exe onder Builds en heeft 0 waarschuwingen/fouten. De serieslice blijft door de gebruiker geaccepteerd. PR #56 is nog open; eerst die mergen, daarna #57 naar main richten en de controles opnieuw bekijken.
