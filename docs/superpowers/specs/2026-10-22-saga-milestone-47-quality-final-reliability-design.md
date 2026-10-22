# Milestone 47: betrouwbaarheid afronding Quality

## Goedgekeurd doel en scope

De gebruiker heeft opdracht gegeven de drie bevindingen uit de afrondingsreview te verbeteren: veilige foutafhandeling bij terugzetten van genegeerde kwaliteitsmeldingen, verwerking van de oorspronkelijke selectie bij een selectie die tijdens opslag verandert, en opruimen van oude hersteldetails bij Dit is correct. Geen nieuwe kwaliteitscontrole of herstelroute.

Branch codex/quality-final-reliability begint op ff97e5a, de gemergde PR #47. De zes controles en herstelacties blijven functioneel ongewijzigd. Deze slice sluit alleen de afgesproken randgevallen.

## Gedrag en acceptatiecriteria

- Terugzetten van geselecteerde of alle genegeerde meldingen vangt opslagfouten op. De gebruiker ziet een begrijpelijke melding; de zichtbare regels en selectie worden bij een mislukte opslag niet alvast verwijderd. Geen ruwe exceptiontekst, SQL of bestandspaden in de melding.
- Een nieuwe poging wist de oude melding en kan normaal slagen. Annulering blijft annulering, geen nieuwe foutmelding; de bezetstatus wordt ook dan vrijgegeven.
- Beide terugzetacties zijn tijdens opslag geblokkeerd, ook bij een rechtstreekse tweede opdracht. De boeken-/meldingenlijst mag intussen een andere selectie krijgen.
- De actie bewaart vóór opslag een snapshot van de geselecteerde regels. Na succesvolle opslag verdwijnen uitsluitend deze regels uit het overzicht. Een inmiddels geselecteerde andere regel blijft zichtbaar en geselecteerd.
- Dit is correct wist eerdere hersteldetails nadat de gebruiker de actie heeft bevestigd, zowel als het markeren slaagt als wanneer het zelf mislukt. Annuleren van de bestaande bulkbevestiging blijft zonder nieuwe actie of gegevenswijziging.
- De melding bij terugzetten bestaat in alle zes talen. De bestaande bevestiging voor alles terugzetten en toetsenbordbediening blijven behouden.

## Technische aanpak en stijl

MetadataQualityExclusionsViewModel deelt de verwerking van geselecteerd/alles terugzetten via één RestoreAsync met try/catch/finally en een vaste regelmomentopname. Gebruik bestaande ObservableProperty en AsyncRelayCommand. Voorbeeld van de bestaande stijl: `new AsyncRelayCommand(RestoreAllAsync, () => CanRestoreAll)`. Alleen een begrijpelijke resourcekey gaat naar StatusMessage; technische exceptions worden niet aan de gebruiker doorgegeven.

De WPF-weergave en code-behind gebruiken CanRestoreAll; het statusveld heeft tekstomloop en de bestaande DangerBrush. In MetadataQualityDashboardViewModel wordt de bestaande ClearRepairFeedback gebruikt, niet een tweede opruimfunctie. Application-, opslag- en detectiecode veranderen niet.

## Structuur en verificatie

Bronnen onder src/EbookManager.Presentation/ViewModels en src/EbookManager.App/Views; vertalingen onder src/EbookManager.App/Resources/Strings. Tests onder tests/EbookManager.Tests/App/ViewModels, App/Views en App/Resources. Volg bestaande C#-records/nullable annotaties en xUnit/FluentAssertions. De opslagtestdubbels simuleren fouten en uitgestelde afronding zonder een echte bibliotheek te wijzigen.

- Gericht: `dotnet test tests/EbookManager.Tests/EbookManager.Tests.csproj -c Debug --filter FullyQualifiedName~MetadataQuality --no-restore`.
- Volledig: `dotnet test EbookManager.sln -c Debug --no-restore`.
- Build: `dotnet build src/EbookManager.App/EbookManager.App.csproj -c Debug --no-restore`.
- Controle: `git diff --check`; handmatige checklist docs/manual-tests/milestone-47-checklist.md.

Eerst falende tests voor de ontbrekende veilige foutmelding, stale hersteldetails, UI-binding en vertalingen. Aanvullende dekking voor oorspronkelijke versus latere selectie, retry, tweede opdracht tijdens opslag en annulering. De project-DoD blijft gelden; handmatige acceptatie en actuele buildgegevens worden niet voortijdig afgevinkt.

## Grenzen

Altijd gegevens behouden, kleine geteste wijzigingen maken, regressies bewaken en alle gewijzigde Markdown exact naar Obsidian spiegelen. Eerst afstemmen bij nieuwe functies, dependencies of schemawijzigingen. Nooit duplicaatbescherming omzeilen, echte bibliotheken voor fouttests beschadigen, technische foutdetails tonen of de PR zelf mergen. Gewone niet-draft PR; alleen de gebruiker mergt.

## Uitgevoerde verificatie

Volledige suite: 888 geslaagd, 0 mislukt of overgeslagen. Debug-build 2026.10.22.65, 22 oktober 2026 om 14:34 (Europe/Amsterdam): 0 waarschuwingen en fouten. Eigen review op correctheid, eenvoud, architectuur, veiligheid en prestaties uitgevoerd. Tijdens review is met een eerst falende test bewaakt dat alles terugzetten de lijst met één Reset leegt; geselecteerd terugzetten behoudt de latere selectie. Een fout en een geslaagde retry, annulering, een rechtstreekse tweede opdracht tijdens opslag en het verdwijnen van oude hersteldetails zijn automatisch getest. Geen Application-, database-, detectie- of dependencywijzigingen. De gebruiker heeft de overige handmatige controles goedgekeurd. Het herstelconflict kon niet handmatig worden gereproduceerd; dit staat uitdrukkelijk als niet uitgevoerd in de checklist. De regressietests voor oude hersteldetails blijven de automatische dekking voor dit scenario.
