# Milestone 49: resultaten bij meervoudig metadata bewerken

## Status en testbuild

De gebruiker heeft de oorspronkelijke slice getest en geaccepteerd op build 2026.10.24.29; alle eigen vinkjes hieronder zijn behouden. Alleen de aanvullende invoerverbetering onderaan vraagt nog een korte nacontrole. Start daarvoor Builds/Debug/Saga.exe, versie **2026.10.24.33**, gebouwd op **24 oktober 2026 om 13:52 (Europe/Amsterdam)**. Volledige suite: 955 tests geslaagd, 0 mislukt of overgeslagen. Build: 0 waarschuwingen/fouten; precies één Saga.exe onder Builds/Debug. Eigen review en lokalisatie-/layouttests uitgevoerd; de eerdere WPF-render met fictieve gegevens blijft van toepassing op de ongewijzigde resultaatweergave.

Gebruik bij voorkeur een bibliotheekkopie voor conflict-/verwijdertests. De afgevinkte controles zijn door de gebruiker uitgevoerd; nieuwe controles worden niet vooraf als geslaagd beschouwd. Moeilijke opslag- en bestandsfouten zijn automatisch getest; beschadig daarvoor geen echte bibliotheek.

## Alles gelukt

- [X] Selecteer twee of meer boeken in de gewone Detail- of Lijstweergave en open Metadata in meerdere boeken bewerken. Het bestaande formulier en de aangevinkte-veldenwerkwijze zijn behouden; geen extra selectie-/previewvenster.
- [X] Kies één vrije serie of een auteur en pas toe. De betrokken boeken, geselecteerde details, zoekresultaten en filters actualiseren direct. Niet-geselecteerde boeken behouden hun waarden.
- [X] Onderaan verschijnt een korte groene melding met het aantal bijgewerkte boeken, zonder 0 fouten of een uitgebreid resultaatvenster.
- [X] Voer dezelfde waarde nogmaals in voor die boeken. Ongewijzigde boeken tellen apart en zijn geen fout. Na volledig succes of ongewijzigd resultaat is de batchselectie leeg.
- [X] Controleer tags vervangen/toevoegen/verwijderen, taal, leesstatus en serie leegmaken. Alleen aangevinkte velden veranderen; een lege serie maakt ook het serienummer leeg. Bestaande omslagen blijven zichtbaar.
- [X] Kies één aangepast veld, pas een geldige waarde toe en controleer het resultaat in details, custom kolom/filter en na heropenen. Controleer ook een leeg aangevinkt aangepast veld.

## Gemengd succes en conflict

Voor een gecontroleerd duplicaatconflict op een testkopie: boek A heeft titel Gedeeld en auteur Oorspronkelijk, boek B titel Gedeeld en auteur Doelauteur, boek C een unieke titel en auteur Oorspronkelijk. Selecteer A en C, niet B, en wijzig de auteur naar Doelauteur. Bestaande vergelijkbare testboeken mogen ook worden gebruikt; geen echte gegevens wijzigen alleen om een fout te forceren.

- [X] Het niet-conflicterende boek wordt bijgewerkt; het conflicterende boek blijft ongewijzigd. De melding geeft aan dat een boek aandacht vraagt.
- [X] Klap Details open. Titel, auteur en begrijpelijke oorzaak staan bij het probleemboek, bijvoorbeeld Boek bestaat al met deze titel en auteur. Er verschijnen geen exceptionteksten, interne resource-sleutels of onnodige succesregels.
- [X] Het mislukte boek blijft geselecteerd voor zover het nog zichtbaar is. Geslaagde boeken vallen uit de batchselectie. Open het bestaande bewerkvenster opnieuw en kies bewust passende velden voor een nieuwe poging.
- [X] Bij een conflict in de standaardmetadata worden eventueel tegelijk aangevraagde custom wijzigingen van dat boek ook overgeslagen.
- [X] Een nieuwe bewerking vervangt de oude resultaatdetails; filters worden niet ongemerkt gewist om verborgen boeken te tonen.

## Annuleren en bediening

- [X] Open het venster, wijzig conceptwaarden en sluit met Annuleren of Escape. Geen boeken wijzigen.
- [X] Als verwerken lang genoeg duurt: tweede bulkopdracht en bibliotheek openen/aanmaken zijn tijdelijk niet beschikbaar; daarna weer wel. Dit is ook automatisch gecontroleerd, niet forceren als de batch te snel klaar is.
- [X] Als je tijdens verwerken een andere selectie maakt, blijft die nieuwere selectie behouden na afloop.
- [X] Details is met toetsenbord open/dicht te klappen; een lange lijst scrollt binnen een begrensd vlak en houdt de boekenlijst bruikbaar. Controleer vensterformaat en een ander thema/andere taal.
- [X] Na bibliotheekwissel staan de vorige bulkresultaten niet meer in beeld. Auteursbeheer en Quality-routes blijven normaal werken.

## Automatisch gecontroleerde moeilijke situaties

Algemene fout bij één boek met succes bij een volgend boek; ontbrekend boek; fout vóór en na databasecommit; onzekere herlezing; geen automatische herhaling van titel/auteur omwisselen; annulering vóór commit met Niet verwerkt voor resterende boeken; custom-only/ongewijzigde waarden; deels opgeslagen custom waarden met veldnamen; ontbrekende custom-opslag; bestandsfout na succesvolle databaseopslag; echte SQLite- en metadata.json-synchronisatie; blokkering van tweede opdrachten; nieuwere selectie behouden; zes talen en begrensde/gevirtualiseerde feedback. Deze automatische dekking is geen claim dat dezelfde fout handmatig is opgewekt.

De oorspronkelijke auteursbeheeracceptatie blijft vastgelegd in de eerdere checklist. Alleen de gebruiker mergt de nieuwe PR.

## Aanvulling: automatisch aanvinken bij standaardtekstvelden

Deze vier controles zijn de enige nieuwe handmatige nacontrole; de geaccepteerde hoofdroute hoeft niet volledig opnieuw getest te worden.

- [ ] Open bulkbewerking. Auteur, serie, tags en taal kunnen direct worden ingevuld zonder eerst een vinkje te zetten. Tekst invoeren vinkt alleen het betreffende veld automatisch aan.
- [ ] Wis de ingevoerde tekst: het vinkje gaat uit, ook bij alleen spaties. Vink het lege veld daarna zelf aan: dit blijft mogelijk om opgeslagen waarden bewust leeg te maken. Bij Tags geldt leegmaken alleen voor Vervangen; Toevoegen/Verwijderen met lege tekst wijzigen niets.
- [ ] Vul tekst in en vink het veld daarna handmatig uit. De concepttekst blijft staan, maar dit veld wordt niet toegepast. Ga je opnieuw typen, dan wordt het veld weer aangevinkt.
- [ ] Titel/auteur omwisselen blijft auteurvervanging blokkeren. De uitleg bovenaan beschrijft automatisch selecteren en bewust leegmaken; aangepaste velden en leesstatus behouden hun bestaande bediening.
