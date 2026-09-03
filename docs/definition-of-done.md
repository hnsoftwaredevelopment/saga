# Saga Definition of Done

Deze Definition of Done is de vaste kwaliteitsgrens voor iedere wijziging aan Saga. Een onderdeel is pas klaar wanneer alle toepasselijke punten aantoonbaar zijn afgehandeld. Niet-toepasselijke punten worden bewust benoemd in de PR in plaats van stilzwijgend overgeslagen.

## 1. Doel en afbakening

- Het afgesproken gebruikersdoel en de acceptatiecriteria zijn duidelijk.
- De wijziging vormt één begrijpelijke, zelfstandig te beoordelen slice.
- Werk buiten de afgesproken scope is niet ongemerkt meegenomen.
- Bekende vervolgpunten en bewuste beperkingen staan in de documentatie of PR.

## 2. Veilige git-werkwijze

- De branch begint bij een actuele `main` en gebruikt standaard het voorvoegsel `codex/`.
- Commits zijn klein, logisch gegroepeerd en beschrijven het doel van de wijziging.
- Er staan geen lokale instellingen, databases, licentiesleutels, geheimen of buildbestanden in de commit.
- Er wordt een gewone, niet-draft PR geopend.
- Alleen de gebruiker mergt de PR, tenzij de gebruiker uitdrukkelijk anders vraagt.

## 3. Gedrag en gegevensveiligheid

- Nieuw of gewijzigd gedrag is eerst met een falende test aangetoond wanneer dat praktisch mogelijk is.
- Geldige bestaande gegevens worden niet onverwacht overschreven.
- Invoer wordt gevalideerd en lege, ongeldige en verouderde toestanden zijn afgevangen.
- Annuleren laat gegevens ongewijzigd.
- Fouten geven een duidelijke melding en laten de actuele opgeslagen toestand zien.
- Wijzigingen die boekmetadata raken gebruiken de bestaande SQLite-, sidecar- en ondersteunde ebook-write-backroute.
- Na opslaan worden relevante schermen, tellingen, selecties en filters direct bijgewerkt.

## 4. Interface en toegankelijkheid

- Teksten en acties zijn begrijpelijk voor een gebruiker en passen bij de geselecteerde context.
- Acties zijn alleen zichtbaar of beschikbaar wanneer ze uitvoerbaar zijn.
- De workflow is volledig met het toetsenbord te bedienen.
- Focus, standaardknop, annuleren met `Escape`, tooltips en toegankelijke namen zijn waar nodig ingesteld.
- Vensters en panelen blijven bruikbaar bij langere teksten en gangbare schermgroottes.
- De bestaande Saga-stijl, thema’s en interactiepatronen zijn gevolgd.

## 5. Lokalisatie

- Iedere nieuwe gebruikersgerichte tekst heeft een resource-sleutel.
- Engels, Nederlands, Duits, Frans, Spaans en Italiaans bevatten begrijpelijke vertalingen.
- Er verschijnen geen interne sleutels of onvertaalde technische termen in de interface.
- Lokalisatietests bewaken aanwezigheid en bruikbaarheid van de nieuwe teksten.

## 6. Geautomatiseerde controle

- Gerichte tests voor het nieuwe gedrag slagen.
- De volledige testset slaagt zonder nieuwe overgeslagen tests.
- `dotnet test EbookManager.sln -c Debug` slaagt.
- `dotnet build src/EbookManager.App/EbookManager.App.csproj -c Debug --no-restore` slaagt met 0 waarschuwingen en 0 fouten.
- Onder `Builds` staat alleen de bedoelde actuele Debug-build, met precies één `Saga.exe` in `Builds\Debug`.
- De bestandsversie en aanmaaktijd van de verse testbuild worden in de PR vermeld.

## 7. Handmatige controle

- Een nieuwe of gewijzigde gebruikersworkflow heeft een checklist onder `docs/manual-tests`.
- De checklist behandelt de hoofdroute, annuleren, fouten, toetsenbordgebruik, directe actualisatie en relevante regressies.
- De gebruiker heeft de toepasselijke controlepunten uitgevoerd, of een bewuste afwijking staat expliciet in de PR.
- De gebruikte testbuild is aantoonbaar de actuele build uit `Builds\Debug`.

## 8. Documentatie

- README, featurebeschrijving en ontwerpdocumentatie zijn bijgewerkt wanneer gedrag, status of gebruik verandert.
- Ieder aangemaakt of gewijzigd Markdown-bestand is exact gespiegeld naar `C:\Devops\Obsidian\markdown\Development\HNSoftwareDevelopment\Ebook Manager`, met behoud van het relatieve pad.
- De gelijkheid van bron en Obsidian-spiegel is gecontroleerd.

## 9. Review en PR-controles

- De volledige diff is beoordeeld op correctheid, eenvoud, architectuur, beveiliging en prestaties.
- Een gevonden fout is bij voorkeur voorzien van een regressietest.
- GitHub meldt dat de PR mergeable is en geen mergeconflicten heeft.
- Beschikbare GitHub-controles zoals CodeQL en GitGuardian zijn groen.
- CodeRabbit-opmerkingen zijn beoordeeld en relevante bevindingen zijn opgelost.
- Een CodeRabbit-limiet blokkeert de PR niet wanneer de eigen review, tests, build en overige controles aantoonbaar groen zijn.
- Algemene toolwaarschuwingen worden inhoudelijk beoordeeld; ze worden niet blind opgelost wanneer ze niet bij de projectconventies passen.

## 10. Oplevering

- De PR-beschrijving vat het gebruikersresultaat, de belangrijkste technische keuzes en de uitgevoerde controles samen.
- Er zijn geen onverwachte of niet-gecommitte wijzigingen achtergebleven.
- Na de merge wordt voor vervolgwerk opnieuw begonnen vanaf een bijgewerkte `main`.

## Verkorte eindcontrole

Een wijziging is klaar wanneer het afgesproken gedrag werkt, gegevens veilig blijven, 0 tests falen, de Debug-build schoon is, de handmatige controle is afgehandeld, documentatie en Obsidian gelijk zijn en de PR zonder onopgeloste relevante bevindingen kan worden gemerged.
