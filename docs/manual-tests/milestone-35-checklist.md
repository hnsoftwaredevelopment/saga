# Milestone 35 Handmatige Testchecklist

Gebruik uitsluitend de actuele Debug-build uit `Builds\Debug\Saga.exe`. Deze controle wijzigt de boekenopslag niet; de bestaande bibliotheek kan normaal worden gebruikt.

## Voorbereiding

- [X] Sluit een eventueel geopende Saga-versie.
- [X] Controleer dat onder `Builds` alleen de map `Debug` staat en start `Builds\Debug\Saga.exe`.
- [X] Wacht tot de echte bibliotheek met ongeveer 34.447 boeken volledig zichtbaar is.
- [X] Kies een boek waarvan de omslag lokaal beschikbaar is en één boek zonder geldige omslag als herkenbare controlepunten.

## Zoeken en selectie

- [X] Selecteer een boek en controleer dat titel, auteur en overige details rechts normaal verschijnen.
- [X] Tik vlot meerdere letters in het algemene zoekveld. Controleer dat typen direct blijft reageren en dat na de korte typepauze alleen het laatste zoekwoord wordt toegepast.
- [X] Controleer dat een geselecteerd boek dat in de resultaten blijft staan geselecteerd blijft en dat het detailpaneel niet zichtbaar leegloopt of opnieuw knippert.
- [X] Wis het zoekveld snel met Backspace of `Ctrl+A` en Delete; controleer dat de volledige lijst na de korte typepauze terugkomt.
- [X] Zoek op een tekst die niets oplevert en ga daarna terug naar een bestaand zoekwoord; controleer dat beide toestanden correct en zonder vastlopen verschijnen.

> Bij de eerste praktijktest werkte de zoekactie goed, maar meldde Saga ten onrechte dat de bibliotheek leeg was. Dit is aangepast naar `Geen boeken gevonden die overeenkomen met de huidige zoekopdracht of filters.` De hertest bevestigde dat deze tekst duidelijk is en de zoekactie correct blijft werken.
- [X] Gebruik daarna een auteurs-, taal- of formaatfilter en wijzig de sortering; deze acties horen direct te reageren.

## Scrollen en omslagen

- [X] Test de gedetailleerde weergave met muiswiel, scrollbar, Page Down en Page Up.
- [X] Sleep de scrollbar meerdere keren snel een groot stuk omlaag en omhoog en wissel ook snel van richting.
- [X] Controleer dat titels direct verschijnen en dat omslagen eventueel kort daarna invullen, zonder pauzes van één à twee seconden voor de volgende titels.
- [X] Controleer tijdens snel scrollen dat nooit kort de omslag van een ander boek bij een rij blijft staan.
- [X] Controleer dat een ontbrekende of beschadigde omslag rustig de bestaande placeholder houdt en geen foutmelding veroorzaakt.
- [X] Herhaal de scrollcontrole in de lijstweergave, inclusief een zichtbare omslagkolom.
- [X] Herhaal de scrollcontrole in de boekenplankweergave.
- [X] Wissel enkele keren tussen gedetailleerd, lijst en boekenplank; controleer dat titels, selectie en omslagen bij de juiste boeken blijven.

## OneDrive en regressie

- [X] Controleer enkele omslagen die OneDrive mogelijk eerst moet ophalen. De lijst moet tijdens dat ophalen bedienbaar blijven.
- [X] Open een willekeurig boekbestand vanuit het detailpaneel en controleer dat dit nog normaal werkt.
- [X] Open de Quality Page en keer terug; controleer dat de boekenlijst en actieve filters intact blijven.
- [X] Controleer dat groeperen en het tonen of verbergen van de omslagkolom in de configureerbare lijst nog normaal werken.
- [X] Sluit Saga en start dezelfde Debug-build opnieuw; herhaal kort zoeken en Page Up/Page Down.

## Praktijkresultaat

- [X] Noteer of zoeken merkbaar sneller en vloeiender is dan vóór deze wijziging. Resultaat: vloeiender.
- [X] Noteer of scrollen merkbaar sneller en vloeiender is dan vóór deze wijziging. Resultaat: vloeiender.
- [X] Meld eventuele verkeerde omslag met de zichtbare titel en auteur van de betreffende rij. Resultaat: niet waargenomen.
- [X] Meld een eventuele merkbare pauze met de gebruikte weergave en handeling, bijvoorbeeld `gedetailleerd - Page Down`. Resultaat: niet waargenomen.

De foutpaden voor geannuleerde achtergrondtaken, ontbrekende bestanden en beschadigde afbeeldingen worden geautomatiseerd afgevangen. Wanneer deze situaties in de echte bibliotheek niet voorkomen, hoeven ze niet kunstmatig te worden gemaakt.

De praktijktest op 29 september 2026 bevestigde dat zoeken en scrollen merkbaar vloeiender zijn. De afsluitende hertest op 1 oktober 2026 bevestigde ook de verbeterde tekst bij nul zoekresultaten. Daarmee zijn alle functionele controlepunten geslaagd.
