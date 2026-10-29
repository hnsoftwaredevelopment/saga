# Milestone 51 — meerdere boeken selecteren in de boekenkast

**Gebruikersacceptatie, 27 oktober 2026:** meervoudige selectie werkt in alle drie de weergaven zoals verwacht; ook metadata bewerken werkt overal goed. Daarmee is de gemelde beperking van gegroepeerde Lijst/Gedetailleerd door de gebruiker akkoord bevonden. De niet afzonderlijk bevestigde detailcontroles hieronder blijven herkenbaar staan; hun geautomatiseerde dekking staat onder Geautomatiseerde verificatie.

Test met `Builds\Debug\Saga.exe`, versie **2026.10.29.5**, gebouwd op **29 oktober 2026 om 08:15** (Europe/Amsterdam). Eerdere gebruikersacceptatie van selectie en metadata bewerken in alle drie de weergaven blijft behouden. De nieuwe reviewcorrectie raakt alleen Pijl-omhoog/-omlaag tussen boekenplankgroepen; daarvoor staat onderaan een gerichte hercontrole. Niet afzonderlijk bevestigde detailcontroles blijven zichtbaar.

## Selecteren met de muis

- [X] Open de boekenkast met omslagen. Boven de tegels staan het aantal geselecteerde boeken en een korte bedieningsuitleg. Metadata wijzigen gebruikt de bestaande knop in de hoofdwerkbalk.
- [X] Klik één boek: het heeft een duidelijke selectierand, de telling is 1 en het detailpaneel toont het boek.
- [X] Ctrl+klik op twee andere boeken: de selectie groeit. Ctrl+klik opnieuw op een geselecteerd boek: alleen dat boek wordt uit de selectie gehaald.
- [X] Klik een startboek en Shift+klik verderop: de tussenliggende boeken worden geselecteerd. Controleer ook terugwaarts en Ctrl+Shift om een reeks aan een bestaande selectie toe te voegen.
- [X] Scroll weg en terug. De selectieranden blijven bij de juiste boeken horen; omslagen blijven laden zoals voorheen.

## Toetsenbord

- [X] Ga met Tab naar de tegels. Pijltjes en Home/End kiezen een boek; ook een boek verderop wordt zichtbaar en krijgt focus. Tab kan de tegelweergave weer verlaten.
- [X] Shift+pijltje breidt de selectie uit. Ctrl+pijltje verplaatst alleen de focus; Ctrl+Spatie voegt het gefocuste boek toe of verwijdert het. De telling blijft kloppen.
- [X] Escape wist de selectie. De bewerkingsactie is bij nul geselecteerde boeken uitgeschakeld.
- [X] Ga direct na een tegelkeuze naar een tekstveld en typ verder. Vertraagd focusherstel mag je niet terug naar een tegel halen.

## Groepen, zoeken en weergaven

- [X] Groepeer bijvoorbeeld op auteur of tags en open twee groepen. Met Ctrl kun je boeken uit beide groepen selecteren; kiezen in de tweede groep wist de eerste selectie niet. De gebruiker bevestigt dit voor de boekenplank; Lijst/Gedetailleerd worden afzonderlijk hieronder hercontroleerd.
- [ ] Komt een boek onder meerdere tags/auteurs voor, dan hebben de vermeldingen dezelfde selectiestaat en telt het boek één keer. Shift volgt de unieke volgorde van de open groepen; ingeklapte groepen worden niet nieuw meegenomen.
- [ ] Wissel boekenkast → lijst → detailweergave → boekenkast. Dezelfde zichtbare boeken blijven geselecteerd. Wijzig ook eens de selectie in de lijst en ga terug naar de boekenkast.
- [ ] Zoek of filter zodat een deel van de geselecteerde boeken niet meer bij de resultaten hoort. Die boeken verdwijnen uit de selectie. Wissen van de zoektekst selecteert ze niet ongemerkt opnieuw.

## Metadata wijzigen

Gebruik enkele boeken waarvan een tijdelijk gewijzigd veld na afloop kan worden teruggezet.

- [X] Selecteer twee of drie boeken en open de bestaande metadata-bewerking. Het aantal klopt. Annuleren behoudt de selectie en wijzigt geen boekgegevens.
- [X] Wijzig een veld en bevestig: alleen de geselecteerde boeken zijn aangepast. Na volledig succes is de bulkselectie leeg; de selectieranden en telling zijn daarmee in overeenstemming.
- [X] Controleer dat gewone boekdetails en de bestaande bulkbewerking vanuit lijst/detailweergave blijven werken. Foutdetails en behoud van mislukte selectie volgen de bestaande werkwijze; geen fouten met echte boeken forceren.
- [X] Controleer de selectie-/focusranden in het gebruikelijke thema en na verkleinen van het venster. Telling en uitleg blijven bereikbaar; bewerken gebruikt de hoofdwerkbalk.

## Korte hercontrole — dubbele knop verwijderd

- [X] In build 2026.10.27.25 staat boven de tegels geen tweede bewerkingsknop. Selectietelling en uitleg blijven zichtbaar. De bestaande knop in de hoofdwerkbalk opent de metadata-bewerking voor de geselecteerde boeken.

Voor deze visuele correctie zijn de 993 tests opnieuw geslaagd en is de Debug-build zonder waarschuwingen/fouten geplaatst. Selectie-, toetsenbord- en opslaglogica zijn ongewijzigd.

## Gerichte hercontrole na review — verticale navigatie

- [ ] Maak de boekenplank smaller tot er bijvoorbeeld drie tegels op een rij staan en er een verticale scrollbar is. Pijl-omlaag gaat naar de tegel recht onder de huidige; Pijl-omhoog komt terug in dezelfde kolom. Herhaal na vergroten tot een extra tegel op de rij past.

## Geautomatiseerde verificatie

993 gewone tests slagen. Aanvullend slagen veertien interactieve WPF-scenario's: de vijf bestaande series-focuscontroles, zes boekenkastcontroles, twee gegroepeerde lijst-/detailcontroles en de nieuwe groepsrij-navigatieproef. De proeven gebruiken echte WPF-muis- en toetsenbordroutes voor Ctrl/Shift/Ctrl+Shift, omschakelen, groepsgrenzen, dubbele tagvermeldingen met gedeelde selectieranden, Home/End, Ctrl-focus/Spatie, Escape, nieuwere tekstfocus, filters en weergavewissels met verschillende groeperingen. De nieuwe navigatieproef gebruikt groepen met 2, 5, 1 en 4 boeken en controleert beide richtingen, gedeeltelijke rijen, eerste/laatste grens, ingeklapte groepen, focus en de exacte Shift-selectie. Eerst falend bewezen: Down vanaf het eerste boek koos boek 4 in plaats van boek 3. Unit-tests bewaken ook rijidentiteit, bibliotheekwissel, annuleren en bulkgebruik. Alle gegevens zijn fictief; geen echte bibliotheek gelezen. De Debug-build heeft 0 waarschuwingen/fouten en is de enige Saga.exe onder Builds.

## Gerichte hercontrole — gegroepeerde Lijst en Gedetailleerd

- [X] Herhaal in **beide** weergaven: groepeer op auteur of tags, open twee groepen, klik één boek en Ctrl+klik een boek onder een andere kop. Beide blijven zichtbaar geselecteerd; Metadata wijzigen in de hoofdwerkbalk gebruikt het juiste aantal. Ctrl+klik verwijdert alleen het gekozen boek. De gebruiker bevestigt dat meervoudige selectie en metadata bewerken in alle drie de weergaven zoals verwacht werken.
- [ ] Klik een startboek en Shift+klik in een andere open groep. De tussenliggende unieke boeken worden geselecteerd. Controleer ook Ctrl+Shift en een boek dat onder meerdere tags staat: alle vermeldingen hebben dezelfde rand, het boek telt één keer.
- [ ] Omhoog/omlaag en Home/End kiezen een rij, ook in een andere open groep. Shift breidt uit; Ctrl+pijl verplaatst alleen focus; Ctrl+Spatie schakelt het gefocuste boek om; Escape wist de selectie. Tab en groepskoppen blijven werken.
- [ ] Gebruik verschillende groeperingen per weergave, selecteer enkele boeken en wissel tussen boekenplank, Lijst en Gedetailleerd. Iedere weergave houdt haar eigen groepering; dezelfde geselecteerde boeken krijgen ook in de gegroepeerde rijen een rand. Een ingeklapte groep eerst openen om haar boeken te zien.
- [ ] Filter een deel van de selectie weg, wis het filter en controleer dat verborgen selecties niet terugkomen. Ga direct na toetsenbordnavigatie naar het zoekveld: focus blijft daar. Controleer de selectie-/focusranden ook in het gebruikelijke thema.

## Gerichte hercontrole na review — gedeeltelijke boekenplankgroepen

- [ ] Groepeer de boekenplank zodat een korte groep gevolgd wordt door een grotere groep. Maak het venster breed genoeg voor meerdere tegels per rij. Ga met Pijl-omlaag vanuit de laatste rij van de korte groep naar de eerste rij van de volgende: de overeenkomstige kolom wordt gekozen, zonder boeken over te slaan. Controleer ook Pijl-omhoog terug. Is die kolom niet beschikbaar, dan wordt de dichtstbijzijnde beschikbare tegel gekozen.
- [ ] Herhaal met Shift+Pijl-omlaag en controleer het eindboek en de geselecteerde reeks voordat je Metadata wijzigen opent. Een ingeklapte groep wordt bij navigatie overgeslagen. Annuleer de bewerking; voor deze hercontrole hoef je geen metadata op te slaan.
