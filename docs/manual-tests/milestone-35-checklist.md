# Milestone 35 Handmatige Testchecklist

Gebruik uitsluitend de actuele Debug-build uit `Builds\Debug\Saga.exe`. Deze controle wijzigt de boekenopslag niet; de bestaande bibliotheek kan normaal worden gebruikt.

## Voorbereiding

- [ ] Sluit een eventueel geopende Saga-versie.
- [ ] Controleer dat onder `Builds` alleen de map `Debug` staat en start `Builds\Debug\Saga.exe`.
- [ ] Wacht tot de echte bibliotheek met ongeveer 34.447 boeken volledig zichtbaar is.
- [ ] Kies een boek waarvan de omslag lokaal beschikbaar is en één boek zonder geldige omslag als herkenbare controlepunten.

## Zoeken en selectie

- [ ] Selecteer een boek en controleer dat titel, auteur en overige details rechts normaal verschijnen.
- [ ] Tik vlot meerdere letters in het algemene zoekveld. Controleer dat typen direct blijft reageren en dat na de korte typepauze alleen het laatste zoekwoord wordt toegepast.
- [ ] Controleer dat een geselecteerd boek dat in de resultaten blijft staan geselecteerd blijft en dat het detailpaneel niet zichtbaar leegloopt of opnieuw knippert.
- [ ] Wis het zoekveld snel met Backspace of `Ctrl+A` en Delete; controleer dat de volledige lijst na de korte typepauze terugkomt.
- [ ] Zoek op een tekst die niets oplevert en ga daarna terug naar een bestaand zoekwoord; controleer dat beide toestanden correct en zonder vastlopen verschijnen.
- [ ] Gebruik daarna een auteurs-, taal- of formaatfilter en wijzig de sortering; deze acties horen direct te reageren.

## Scrollen en omslagen

- [ ] Test de gedetailleerde weergave met muiswiel, scrollbar, Page Down en Page Up.
- [ ] Sleep de scrollbar meerdere keren snel een groot stuk omlaag en omhoog en wissel ook snel van richting.
- [ ] Controleer dat titels direct verschijnen en dat omslagen eventueel kort daarna invullen, zonder pauzes van één à twee seconden voor de volgende titels.
- [ ] Controleer tijdens snel scrollen dat nooit kort de omslag van een ander boek bij een rij blijft staan.
- [ ] Controleer dat een ontbrekende of beschadigde omslag rustig de bestaande placeholder houdt en geen foutmelding veroorzaakt.
- [ ] Herhaal de scrollcontrole in de lijstweergave, inclusief een zichtbare omslagkolom.
- [ ] Herhaal de scrollcontrole in de boekenplankweergave.
- [ ] Wissel enkele keren tussen gedetailleerd, lijst en boekenplank; controleer dat titels, selectie en omslagen bij de juiste boeken blijven.

## OneDrive en regressie

- [ ] Controleer enkele omslagen die OneDrive mogelijk eerst moet ophalen. De lijst moet tijdens dat ophalen bedienbaar blijven.
- [ ] Open een willekeurig boekbestand vanuit het detailpaneel en controleer dat dit nog normaal werkt.
- [ ] Open de Quality Page en keer terug; controleer dat de boekenlijst en actieve filters intact blijven.
- [ ] Controleer dat groeperen en het tonen of verbergen van de omslagkolom in de configureerbare lijst nog normaal werken.
- [ ] Sluit Saga en start dezelfde Debug-build opnieuw; herhaal kort zoeken en Page Up/Page Down.

## Praktijkresultaat

- [ ] Noteer of zoeken merkbaar sneller en vloeiender is dan vóór deze wijziging.
- [ ] Noteer of scrollen merkbaar sneller en vloeiender is dan vóór deze wijziging.
- [ ] Meld eventuele verkeerde omslag met de zichtbare titel en auteur van de betreffende rij.
- [ ] Meld een eventuele merkbare pauze met de gebruikte weergave en handeling, bijvoorbeeld `gedetailleerd - Page Down`.

De foutpaden voor geannuleerde achtergrondtaken, ontbrekende bestanden en beschadigde afbeeldingen worden geautomatiseerd afgevangen. Wanneer deze situaties in de echte bibliotheek niet voorkomen, hoeven ze niet kunstmatig te worden gemaakt.
