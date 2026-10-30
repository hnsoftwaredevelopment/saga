# Milestone 56 — volgnummer wijzigen in Series beheren

Testbuild: `Builds\Debug\Saga.exe` **2026.10.31.17**, gebouwd op **31 oktober 2026 om 00:54:49 Europe/Amsterdam**. **1166 tests en 29 echte WPF-scenario's geslaagd**, 0 mislukt/overgeslagen. De Debug-build heeft 0 waarschuwingen/fouten en is de enige Saga.exe onder Builds. Eigen review uitgevoerd; het nieuwe venster is in licht/donker op minimale grootte gerenderd en bekeken. Handmatige acceptatie staat nog open.

Gebruik enkele boeken waarvan je het oorspronkelijke volgnummer weet. Deze wijziging raakt één geselecteerd boek. Het wijzigen of wissen van een nummer verandert de serienaam niet.

## Selecteren, invoeren en annuleren

- [ ] Open **Series beheren**, kies een serie en selecteer één boek in de boekenlijst rechts. **Volgnummer wijzigen** is beschikbaar wanneer een boek is geselecteerd. Zonder boekselectie of tijdens verwerking is de knop uitgeschakeld.
- [ ] Open **Volgnummer wijzigen**. Titel, auteurs en serie zijn zichtbaar; het huidige nummer staat in het invoerveld en de focus staat daar. Een nog ongewijzigde waarde kan niet worden opgeslagen.
- [ ] Typ tekst die geen nummer is. Een begrijpelijke melding verschijnt en opslaan is uitgeschakeld. Vervang die tekst door een geldig nummer: de melding verdwijnt en je kunt verder typen zonder focusverlies. De invoer volgt de geselecteerde taal/cultuur; gebruik bijvoorbeeld `1,5` bij Nederlands. Duizendtalscheiding en exponenten worden niet geaccepteerd.
- [ ] Typ een nieuw nummer en annuleer met Annuleren of Escape. Het oorspronkelijke nummer, de boekvolgorde en de selectie blijven gelijk. De actieknop krijgt focus terug. Open het venster opnieuw: het oorspronkelijke nummer staat er nog.

## Wijzigen, dubbele nummers en wissen

- [ ] Geef een ander geheel nummer en sla op. De boekenlijst staat direct opnieuw in numerieke volgorde, met dezelfde boektitels en hetzelfde aantal boeken. De serienaam en je seriezoektekst blijven gelijk. Het gewijzigde boek blijft geselecteerd als het nog in de geladen lijst staat; de knop krijgt focus terug.
- [ ] Geef een decimaal nummer, bijvoorbeeld `1,5`. Controleer dat dit tussen 1 en 2 wordt gesorteerd, en dat nummer 10 na 2 komt. Andere nummers veranderen niet.
- [ ] Geef een nummer dat al bij een ander boek in dezelfde serie voorkomt. De informatie daarover verschijnt tijdens invoeren. Opslaan blijft mogelijk. Na opslaan zijn beide nummers behouden en noemt de resultaatmelding de overeenkomst. Een gelijk nummer in een andere serie telt hiervoor niet mee.
- [ ] Wis het nummer door het invoerveld leeg te maken en sla op. Het boek blijft in dezelfde serie, maar heeft geen volgnummer meer en staat na de genummerde boeken. Er wordt niet automatisch een vervangend nummer gekozen.
- [ ] Controleer de korte groene resultaatmelding. Bij een bestandsprobleem staat expliciet dat het nummer wel in Saga is opgeslagen maar bestanden niet konden worden bijgewerkt; de betrokken boektitel is beschikbaar onder de waarschuwingen. Deze fout hoeft niet met echte boeken geforceerd te worden.
- [ ] Sluit Series beheren en controleer boekdetails en de gebruikelijke lijstweergave/groepering in het hoofdscherm. Het nieuwe of gewiste nummer is bijgewerkt; titels, auteurs, tags, omslagen, serienaam en overige gegevens zijn behouden.

## Grote lijsten en bediening

- [ ] Indien je een serie met meer dan 200 boeken hebt: gebruik Meer laden, wijzig het nummer van een later geladen boek en controleer de nieuwe volgorde. De eerder geladen hoeveelheid blijft beschikbaar. Als een nummer een boek buiten de geladen lijst verplaatst, verdwijnt de rijselectie en legt de resultaatmelding dit uit. Via Meer laden kun je verder bladeren. Het programma laadt daarvoor niet vanzelf de gehele serie.
- [ ] Gebruik Tab en pijltjestoetsen om een boek en de actie te kiezen. Controleer Enter om op te slaan en Escape om te annuleren. Verklein/vergroot het venster; het invoerveld is via scrollen bereikbaar en de knoppen blijven onderaan beschikbaar. Controleer je gebruikelijke thema.
- [ ] Controleer kort dat Serie hernoemen, Duplicaten zoeken/samenvoegen en Meer laden nog hetzelfde werken. Eerdere geaccepteerde checklists hoeven niet geheel opnieuw te worden uitgevoerd.

## Automatische dekking

Tijdelijke SQLite-bibliotheken bewaken dat uitsluitend SeriesNumber en UpdatedUtc van het geselecteerde boek worden geschreven. Een gewijzigde uitgever sinds openen blijft behouden, evenals tags/auteurs/omslagen en andere boeken. Gewijzigde of verwijderde boeken, namen, nummers, titels of auteurs sinds bevestiging geven conflict zonder overschrijven. Bestaande doelwaarden schrijven niets; annulering en databasefouten laten geen gedeeltelijke update achter. Gelijke nummers worden bij opslaan opnieuw geteld en blokkeren niet, ook wanneer een ander boek sinds openen is aangepast.

Servicetests controleren echte metadata.json, bestandswaarschuwingen en voltooiing na annulering van de aanvrager ná commit. Beheertests bewaken waarschuwingen/gewijzigde boek-ID's en een eerlijke opgeslagen-maar-niet-vernieuwd-melding bij refreshfouten. Lijsten van 600 reeds geladen boeken blijven via begrensde pagina's werken; een buiten de geladen lijst verplaatst boek blijft niet verborgen geselecteerd. Dialoogtests bestrijken Nederlandse en Engelse decimale invoer, lege/ongeldige waarden, overflow, ongewijzigd en toegestane dubbele nummers. De echte WPF-route test openen, focus, invalide invoer, annuleren, decimalen, dubbele nummers, wissen en direct sorteren; beide thema's zijn op minimumgrootte bekeken. Tests lezen of wijzigen geen echte bibliotheek.
