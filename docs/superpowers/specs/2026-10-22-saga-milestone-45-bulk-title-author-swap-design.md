# Milestone 45: titel en auteur in bulk omwisselen

## Doel en bijgestelde werkwijze

De gebruiker kiest de boeken in de kwaliteitslijst en vraagt titel/auteur omwisselen. Na gebruikerstest is de tweede selectie met vinkjes vervallen: de bestaande selectie geeft de bedoeling al aan. Saga toont alleen een korte bevestiging met het aantal boeken.

## Gedrag en acceptatie

- Meerdere geselecteerde regels bij Mogelijk omgewisselde auteur/titel krijgen een korte Ja/Nee-bevestiging, zonder extra overzicht of vinkjes.
- Bevestigen verwerkt alle geschikte boeken uit de selectie; Nee of Escape schrijft niets.
- Ongeschikte boeken blijven ongewijzigd; indien aanwezig noemt de bevestiging het aantal geschikte en overgeslagen boeken.
- Eén geselecteerd boek behoudt de bestaande enkelvoudige vóór/na-bevestiging.
- Titel en volledige auteurslijst worden vóór opslag vergeleken met de oorspronkelijke waarden. Gewijzigde en verdwenen boeken worden veilig overgeslagen.
- De bestaande service behoudt overige metadata en gebruikt SQLite, sidecar en ebook-write-back.
- Een fout onderbreekt andere boeken niet. Mislukte boeken blijven geselecteerd; bibliotheek en auteursfilters verversen samen.
- Korte groene melding bij volledig succes; aantallen bij waarschuwingen, overgeslagen en mislukte boeken.
- De bevestiging is gelokaliseerd in zes talen en gebruikt bestaande toetsenbordbediening.

## Techniek en controles

Application bevat gedeelde herstelbaarheidsregels, snapshots en opslag. Presentation verwerkt de lijstselectie en batchresultaten. UserInteractionService toont de standaardbevestiging; het enkelvoudige WPF-venster blijft behouden. De bestaande xUnit/FluentAssertions-tests bewijzen selectie, annuleren, snapshots, fouten en gegevensbehoud.

Voorbeeld: selecteer twee verkeerd geïmporteerde boeken, kies omwisselen en bevestig één keer. Beide worden verwerkt; een derde, niet geselecteerd boek verandert niet.

Commando's: `dotnet test EbookManager.sln -c Debug --no-restore`, `dotnet build src/EbookManager.App/EbookManager.App.csproj -c Debug --no-restore`, `git diff --check`.

## Grenzen

De gebruiker heeft aanvullend opdracht gegeven de detectieverbetering in deze slice mee te nemen. Naamherkenning ondersteunt initialen, tussenvoegsels en achternaam/voornaam. Een naamachtige titel alleen is onvoldoende: ook het auteursveld moet positieve aanwijzingen bevatten voor een boektitel (herkend beginlidwoord, minstens drie woorden en minstens twee kleineletterwoorden buiten de naam-tussenvoegsels). Ambigue gevallen worden niet gemeld. De 32 correct ingevulde praktijkvoorbeelden mogen niet worden gemeld; `Duenas, Maria` / `De wereld vergeten` moet wel worden gemeld en herstelbaar blijven. De detectie verandert geen metadata of uitsluitingen. Geen bibliotheekbrede zoekactie of netwerkverkeer.

De controle kiest bewust precisie boven volledigheid. De beperkingen staan in docs/feature-requests/title-author-swap-detection.md. Geen wijziging aan de detailactie, schema of dependencies. Altijd gegevensbehoud, tests en Markdown-spiegel controleren. Alleen de gebruiker mergt. De bulkactie is al handmatig geaccepteerd; de detectieverbetering vraagt nog een korte hercontrole.
