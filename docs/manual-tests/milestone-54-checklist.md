# Milestone 54 — auteursduplicaten vinden en vergelijken

Testbuild: Builds\Debug\Saga.exe **2026.10.30.45**, gebouwd op **30 oktober 2026 om 22:53:25 Europe/Amsterdam**. **1099 tests en 25 WPF-scenario's slagen**; 0 waarschuwingen/fouten en precies één Saga.exe onder Builds/Debug. Eigen vijfassige review uitgevoerd. De gebruiker heeft de gehele slice inclusief de zoekveldcorrectie geaccepteerd: “Ja met deze wijziging werkt het goed, deze slice is voltooid”. Individuele vakjes blijven als testhistorie behouden; moeilijke foutpaden zijn automatisch gedekt. De UI-/icoonharmonisatie is uitgesteld. PR #56 wacht op merge door de gebruiker. Gebruik voor samenvoegen bij voorkeur enkele kleine testauteurs of een voorstel waarvan je zeker weet dat het dezelfde auteur betreft. Zoeken en vergelijken wijzigen niets zonder bevestiging.

## Mogelijke dubbelen vinden

- [ ] Open Auteurs beheren en kies Duplicaten zoeken. Er verschijnt een apart overzicht met twee namen per voorstel, hun boekaantallen en de reden van overeenkomst. Er is nog niets geselecteerd of samengevoegd.
- [ ] Controleer enkele bekende naamvarianten als die in je bibliotheek voorkomen: J. Nowee, J.Nowee, Nowee, J. en Johannes Nowee; ook A.K. Straatsma tegenover Anne Klaas Straatsma. Alleen dezelfde achternaam is onvoldoende. De voorstellen zijn mogelijke overeenkomsten; jij bepaalt of het dezelfde persoon is.
- [ ] Typ een deel van een naam; de zoektekst mag bij beide namen passen. Typ meerdere letters achter elkaar en wis ze weer. Het resultaat volgt de laatste invoer en de focus blijft in het zoekveld. Een nieuwe zoekopdracht wist de oude rijselectie. Zoek iets dat niet voorkomt: een duidelijke melding, geen vastloper of fouttekst.
- [ ] Kies één voorstel en open Gegevens vergelijken. Annuleer met Escape of Annuleren. Het voorstel blijft geselecteerd, de knop krijgt focus terug en auteurs/boeken blijven ongewijzigd.
- [ ] Gebruik Vernieuwen. Het overzicht gebruikt de actuele auteurs. Bij meer dan 1000 voorstellen staat expliciet dat je de zoektekst kunt verfijnen; dit grensgeval hoeft niet handmatig te worden opgebouwd.

## Gegevens zichtbaar vergelijken

- [ ] De beschikbare auteurs staan naast elkaar met naam, boekaantal, foto, volledige biografie en website. Een ontbrekend veld is herkenbaar als Geen gegevens of Geen foto. Bij meer auteurs kun je horizontaal door de bronkaarten bladeren.
- [ ] Lees een lange biografie door te scrollen: de auteursnaam blijft zichtbaar. De biografie is volledig beschikbaar, niet alleen een verkorte tekst in een keuzelijst. Bij een kleiner venster blijven bronkaarten en resultaat afzonderlijk bereikbaar en staan bevestigen/annuleren onderaan.
- [ ] Kies Deze auteur behouden bij de juiste naam. Kies daarna bij zichtbare bronvelden Deze foto/website/biografie gebruiken. Het resultaat rechts toont direct welke gegevens en welke naam je behoudt. Dit mag uit verschillende auteurs worden samengesteld.
- [ ] Gebruik ook Leeg laten in het resultaat, of kies een leeg bronveld. Bij verschillende beschikbare waarden vereist samenvoegen een expliciete keuze. Zolang een gekozen foto nog laadt of niet leesbaar is, kan niet worden bevestigd; een andere foto of Leeg laten blijft mogelijk.
- [ ] Test Tab, pijltjestoetsen in de keuzelijsten, Enter en Escape. Een foto die later geladen wordt mag geen eerdere keuze of focus terugzetten. Controleer het gebruikelijke thema en een kleiner/groter venster.

## Samenvoegen en actualiseren

- [ ] Bevestig een correct voorstel. De bestaande veilige samenvoeging wordt uitgevoerd. De bronvermelding verdwijnt, de gekozen auteur blijft bestaan met de gekozen gegevens, boeken en overige mede-auteurs blijven behouden. Een dubbelboekconflict wordt nog steeds geweigerd.
- [ ] Het voorstellenoverzicht wordt direct vernieuwd: het verwerkte paar is weg. De resultaatmelding blijft zichtbaar tijdens zoeken/vernieuwen. Sluit het overzicht: Auteurs beheren toont de doelauteur met actuele boeken en aantallen. Na sluiten van auteursbeheer kloppen hoofdscherm, filters, groepering en details ook.
- [ ] Selecteer ook zelf twee auteurs in Auteurs beheren en kies Auteurs samenvoegen. Deze handmatige route gebruikt dezelfde nieuwe vergelijking en blijft werken zonder de zoekactie. Bij onopgeslagen profielwijzigingen blijft de bekende opslaan/verwerpen/annuleren-vraag bestaan.

## Aanvullende hercontrole — zoekveld na samenvoegen

- [ ] Voer in Auteurs beheren een zoektekst in, voeg via Duplicaten zoeken een correct paar samen en sluit het duplicatenvenster. Het zoekveld in Auteurs beheren is leeg en de ongefilterde lijst met auteurs is terug, met Meer laden indien nodig. De doelauteur en haar gegevens blijven rechts zichtbaar; resultaatmelding en eventuele bestandswaarschuwingen blijven behouden.
- [ ] Herhaal via de handmatige Auteurs samenvoegen-knop. Na bevestigen wordt het zoekveld eveneens gewist en verschijnt de ongefilterde lijst. Annuleer ook een vergelijking: zonder geslaagde samenvoeging blijft de oorspronkelijke zoektekst behouden.

De volledige echte WPF-route is uitgebreid met een niet-betrokken auteur en meer dan 200 auteurs. Zij bewaakt zowel de handmatige route als het sluiten van het duplicatenoverzicht; de lijst blijft correct gepagineerd.

## Automatische dekking

Naamregels voor punten/spaties, komma-volgorde, diakritische tekens, tussenvoegsels en samengestelde achternamen zijn met positieve en negatieve voorbeelden getest. Verschillende volledig uitgeschreven voornamen, alleen een gedeelde achternaam en onbruikbare naamwaarden leveren geen automatische gelijkstelling op. Paren worden niet transitief tot één groep gemaakt.

De lichte index is voorbij 500 auteurs getest zonder profielen, boeken of foto's te laden. Filteren hergebruikt die index en loopt buiten de UI-thread. Annulering, late resultaten, de voorstelgrens, expliciete keuzes, foto-loading en verversingsfouten na opslag zijn automatisch afgedekt. Waarschuwingen en gewijzigde boek-ID's blijven bij een fout behouden. Moeilijke bestandsfouten hoeven niet met echte gegevens te worden uitgelokt. Alle bestaande WPF-proeven en de nieuwe echte vergelijkings-/zoekroute slagen; beide vensters zijn in licht/donker gerenderd en bekeken.


## Gerichte reviewhercontrole — Tab

Reviewcorrectie 31 oktober 2026: CodeRabbit-opmerking 4428707720 is eerst falend gereproduceerd in de echte WPF-proef. ComparisonCards gebruikt nu doorlopende Tab-navigatie; alle acht bronknoppen op twee auteurskaarten zijn bereikbaar en Tab gaat daarna naar de resultaatkeuzes. 1099 tests en 26 WPF-proeven slagen. Schone tussentijdse PR #56-build 2026.10.31.3 van 31 oktober 00:06:12 Europe/Amsterdam, 0 waarschuwingen/fouten. De correctie wordt ook in vervolg-PR #57 meegenomen; de uiteindelijke gecombineerde testbuild wordt daar geregistreerd.

- [ ] Open een vergelijking met twee auteurs. Gebruik Tab om alle bronknoppen van beide kaarten te bereiken; de focus moet vervolgens doorgaan naar de resultaatkeuzes. De volledige eerder geaccepteerde checklist hoeft hiervoor niet opnieuw te worden uitgevoerd.
