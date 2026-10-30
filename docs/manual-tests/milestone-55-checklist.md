# Milestone 55 — seriesvarianten vinden en samenvoegen

Testbuild: `Builds\Debug\Saga.exe` **2026.10.30.59**, gebouwd op **30 oktober 2026 om 23:40:45 Europe/Amsterdam**. **1131 tests en 27 echte WPF-scenario's slagen**; 0 waarschuwingen/fouten en precies één Saga.exe onder Builds. Eigen review uitgevoerd en vergelijking in licht/donker op minimale venstergrootte bekeken. Handmatige acceptatie door de gebruiker staat nog open. PR #56 is bij oplevering nog open; deze slice bouwt daarop voort.

Gebruik voor de samenvoegtest twee serievarianten waarvan je zeker weet dat ze bij elkaar horen, bijvoorbeeld `Bob Evers` en `Bob-Evers`. Eventueel maak je met boekdetails enkele testboeken aan. Zoeken en vergelijken slaan niets op; samenvoegen wijzigt na bevestiging de serienaam van alle boeken in het gekozen paar, ook boeken die nog niet zichtbaar zijn.

## Voorstellen vinden

- [ ] Open **Series beheren** en kies **Duplicaten zoeken**. Een apart overzicht toont twee namen per voorstel, boekaantallen en de overeenkomst. Er wordt niets automatisch geselecteerd of gewijzigd.
- [ ] Controleer enkele verschillende schrijfwijzen in je bibliotheek. Verschillen in punten, streepjes, spaties en accenten kunnen voorstellen opleveren. Verschillende cijfers blijven betekenisvol: `Serie 1` en `Serie 2` worden niet als hetzelfde voorgesteld. Volledig andere namen of afkortingen worden niet geraden.
- [ ] Typ meerdere letters achter elkaar in het zoekveld en wis ze weer. Bij beide namen kan een zoektekst passen, de laatste invoer bepaalt de resultaten en de focus blijft in het zoekveld. Een nieuwe zoekopdracht wist de oude rijselectie. Een niet-bestaande zoektekst geeft een duidelijke melding.
- [ ] Kies **Vernieuwen**. De voorstellen gebruiken de actuele serielijst. Zonder geselecteerd voorstel blijft **Boeken vergelijken** uitgeschakeld.

## Boeken en volgnummers vergelijken

- [ ] Selecteer een voorstel en kies **Boeken vergelijken**. Het venster toont het totale aantal boeken, hun huidige serie, volgnummer, titel en auteurs. De nummers staan numeriek op volgorde; boeken zonder nummer staan onderaan.
- [ ] Indien aanwezig: controleer bijvoorbeeld nummer 1, 1,5, 2 en 10. Een nummer dat bij meerdere boeken voorkomt krijgt een melding; een ontbrekend nummer is ook herkenbaar. Deze informatie verhindert samenvoegen niet.
- [ ] Kies een bestaande serienaam uit de keuzelijst. Vóór een keuze is **Samenvoegen bevestigen** uitgeschakeld. Je kunt hier geen vrije nieuwe naam invoeren.
- [ ] Bij meer dan 200 boeken toont **Meer laden** ook de overige boeken. Het totale aantal bovenaan omvat ze al. Dit grensgeval hoeft niet handmatig te worden opgebouwd als je geen grote serievarianten hebt.
- [ ] Annuleer met de knop of Escape. Alle namen en nummers blijven gelijk; het voorstel blijft geselecteerd en de knop krijgt focus terug. Sluit het voorstellenoverzicht zonder samen te voegen: een oorspronkelijke zoektekst in Series beheren blijft staan.

## Samenvoegen en actualiseren

- [ ] Bevestig een correct paar. Alle boeken uit beide varianten krijgen de gekozen naam. De nummers blijven exact gelijk, ook gelijke of ontbrekende nummers. Er verdwijnen geen boeken en titels, auteurs, tags, omslagen en overige metadata blijven behouden.
- [ ] Het verwerkte voorstel verdwijnt direct. De korte groene resultaatmelding blijft staan tijdens zoeken of vernieuwen. Als boekbestanden niet konden worden bijgewerkt verschijnt een aparte waarschuwing met betrokken boektitels; de wijziging in Saga is dan wel opgeslagen.
- [ ] Sluit het voorstellenoverzicht. Series beheren toont de volledige serielijst met leeg zoekveld, terwijl rechts de gekozen serie met alle boeken en actuele telling zichtbaar blijft. Bij grote series blijft **Meer laden** werken.
- [ ] Sluit Series beheren. Controleer hoofdscherm, seriefilter, groepering en boekdetails: de gekozen naam en oorspronkelijke nummers moeten overal terugkomen. Een niet-betrokken serie blijft ongewijzigd.
- [ ] Controleer dat de gewone **Serie hernoemen**-actie blijft werken voor een nieuwe naam en een reeds bestaande andere serienaam blijft weigeren. Gebruik voor samenvoegen de expliciete vergelijkingsroute hierboven.
- [ ] Gebruik Tab, pijltjestoetsen, Enter en Escape. Verklein en vergroot de vensters. De boeken blijven bereikbaar en bevestigen/annuleren blijven zichtbaar. Controleer je gebruikelijke thema.

## Automatische dekking van moeilijke foutpaden

De tests gebruiken tijdelijke bibliotheken en fictieve gegevens. Zij bewaken behoud van alle boekvelden en serienummers, niet-betrokken boeken, bestaande doelboeken zonder onnodige schrijftoegang, metadata.json, meer dan 500 gewijzigde boeken en rollback na een fout ná de eerste batch. Toegevoegde/verwijderde boeken en gewijzigde auteurs, titels, namen of nummers sinds de vergelijking blokkeren opslag zonder nieuwe gegevens te overschrijven. Annulering vóór opslag schrijft niets; bestandssynchronisatie na commit wordt afgerond. Bestandswaarschuwingen en gewijzigde boek-ID's blijven behouden bij een verversingsfout na opslag, met een eerlijke melding dat de series al zijn samengevoegd.

De lichte naamindex is voorbij 500 series getest zonder boeken/omslagen te laden. Zoeken gebruikt een cache buiten de UI-thread, volgt de laatste invoer en negeert late resultaten na sluiten. De grens van 1000 voorstellen heeft een verfijningsmelding. De echte WPF-proef doorloopt zoeken, annuleren, expliciete keuze, 205 vergelijkingsrijen, minimumgrootte, focus, samenvoegen en de lege zoektekst/actuele lijsten na sluiten. Moeilijke bestands- of databasefouten hoeven niet met echte boeken te worden uitgelokt.
