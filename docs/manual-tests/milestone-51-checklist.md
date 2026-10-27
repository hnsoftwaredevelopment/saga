# Milestone 51 — meerdere boeken selecteren in de boekenkast

Test met `Builds\Debug\Saga.exe`, versie **2026.10.27.23**, gebouwd op **27 oktober 2026 om 09:17** (Europe/Amsterdam). Deze nieuwe slice wacht op handmatige gebruikersacceptatie; onderstaande punten zijn nog niet afgevinkt.

## Selecteren met de muis

- [ ] Open de boekenkast met omslagen. Boven de tegels staan het aantal geselecteerde boeken, de bestaande actie voor metadata wijzigen en een korte bedieningsuitleg.
- [ ] Klik één boek: het heeft een duidelijke selectierand, de telling is 1 en het detailpaneel toont het boek.
- [ ] Ctrl+klik op twee andere boeken: de selectie groeit. Ctrl+klik opnieuw op een geselecteerd boek: alleen dat boek wordt uit de selectie gehaald.
- [ ] Klik een startboek en Shift+klik verderop: de tussenliggende boeken worden geselecteerd. Controleer ook terugwaarts en Ctrl+Shift om een reeks aan een bestaande selectie toe te voegen.
- [ ] Scroll weg en terug. De selectieranden blijven bij de juiste boeken horen; omslagen blijven laden zoals voorheen.

## Toetsenbord

- [ ] Ga met Tab naar de tegels. Pijltjes en Home/End kiezen een boek; ook een boek verderop wordt zichtbaar en krijgt focus. Tab kan de tegelweergave weer verlaten.
- [ ] Shift+pijltje breidt de selectie uit. Ctrl+pijltje verplaatst alleen de focus; Ctrl+Spatie voegt het gefocuste boek toe of verwijdert het. De telling blijft kloppen.
- [ ] Escape wist de selectie. De bewerkingsactie is bij nul geselecteerde boeken uitgeschakeld.
- [ ] Ga direct na een tegelkeuze naar een tekstveld en typ verder. Vertraagd focusherstel mag je niet terug naar een tegel halen.

## Groepen, zoeken en weergaven

- [ ] Groepeer bijvoorbeeld op auteur of tags en open twee groepen. Met Ctrl kun je boeken uit beide groepen selecteren; kiezen in de tweede groep wist de eerste selectie niet.
- [ ] Komt een boek onder meerdere tags/auteurs voor, dan hebben de vermeldingen dezelfde selectiestaat en telt het boek één keer. Shift volgt de unieke volgorde van de open groepen; ingeklapte groepen worden niet nieuw meegenomen.
- [ ] Wissel boekenkast → lijst → detailweergave → boekenkast. Dezelfde zichtbare boeken blijven geselecteerd. Wijzig ook eens de selectie in de lijst en ga terug naar de boekenkast.
- [ ] Zoek of filter zodat een deel van de geselecteerde boeken niet meer bij de resultaten hoort. Die boeken verdwijnen uit de selectie. Wissen van de zoektekst selecteert ze niet ongemerkt opnieuw.

## Metadata wijzigen

Gebruik enkele boeken waarvan een tijdelijk gewijzigd veld na afloop kan worden teruggezet.

- [ ] Selecteer twee of drie boeken en open de bestaande metadata-bewerking. Het aantal klopt. Annuleren behoudt de selectie en wijzigt geen boekgegevens.
- [ ] Wijzig een veld en bevestig: alleen de geselecteerde boeken zijn aangepast. Na volledig succes is de bulkselectie leeg; de selectieranden en telling zijn daarmee in overeenstemming.
- [ ] Controleer dat gewone boekdetails en de bestaande bulkbewerking vanuit lijst/detailweergave blijven werken. Foutdetails en behoud van mislukte selectie volgen de bestaande werkwijze; geen fouten met echte boeken forceren.
- [ ] Controleer de selectie-/focusranden in het gebruikelijke thema en na verkleinen van het venster. Telling, uitleg en bewerkingsknop blijven bereikbaar.

## Geautomatiseerde verificatie

993 gewone tests slagen. Aanvullend slagen tien interactieve WPF-scenario's: de vijf bestaande series-focuscontroles en vijf boekenkastcontroles voor virtuele navigatie, nieuwere tekstfocus, selectie/filterdoorsnede, groepen en native gridselectie met heen/terug wisselen. Unit-tests bewaken Ctrl/Shift, unieke IDs, hergebruik/vervanging van rijen, bibliotheekwissel, annuleren en het daadwerkelijk toepassen op alleen geselecteerde boeken. De WPF-proef gebruikt uitsluitend fictieve boeken en leest geen echte bibliotheek.
