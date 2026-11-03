# Milestone 57 — geselecteerde boeken exporteren

Testbuild: `Builds\Debug\Saga.exe`. Verificatie: 1189 geautomatiseerde tests en 30 echte WPF-proeven geslaagd, 0 mislukt/overgeslagen. Debug-build **2026.11.3.18** van **3 november 2026 om 10:27:08 Europe/Amsterdam**, 0 waarschuwingen/fouten en precies één Saga.exe onder Builds. Eigen review op correctheid, leesbaarheid, architectuur, veiligheid en prestaties uitgevoerd. Licht/donker op minimale venstergrootte gerenderd en bekeken. De gebruiker heeft op 3 november 2026 de teststappen en aanvullende tests uitgevoerd zonder issues en de gehele slice geaccepteerd. PR #59 staat nog open.

Gebruik enkele bekende boeken en een aparte lege doelmap. Het gaat om kopiëren van bestaande formaten; niet om conversie. Er is geen e-reader nodig. Met één beschikbaar bestand kun je de hoofdroute al testen; ontbrekende formaten en meerdere bestanden per boek zijn alleen van toepassing als je die voorbeelden hebt.

## Selectie en voorbereiding

- [X] Zonder boekselectie is de nieuwe exportknop in de hoofdwerkbalk uitgeschakeld. Selecteer meerdere boeken en open **Geselecteerde boeken exporteren**. De bestaande selectie staat in de preview; opnieuw aanvinken is niet nodig.
- [X] Controleer deze route vanuit Boekenplank, Lijst en Gedetailleerd. Selecteer zo mogelijk boeken uit verschillende groepen. Alleen de geselecteerde boeken worden getoond; na sluiten blijven de oorspronkelijke selectie en metadata behouden.
- [X] De formaatkeuze toont beschikbare formaten, standaard EPUB als dit aanwezig is. Wissel van formaat: aantallen en regels veranderen direct. Boeken zonder gekozen formaat worden herkenbaar overgeslagen. Meerdere bestanden van hetzelfde formaat bij één boek tellen allemaal mee als bestanden.
- [X] Voorbereiden houdt Saga bedienbaar. Sluiten tijdens voorbereiding maakt geen kopieën. Zonder beschikbaar bestand of zonder doelmap kun je niet starten.
- [X] Kies een opslagmap. Open de mapkiezer opnieuw en annuleer: de eerdere mapkeuze blijft staan. Sluit het exportvenster zonder te starten: de doelmap blijft ongewijzigd.

## Export en resultaten

- [X] Klik **Exporteren**. De voortgang en huidige titel zijn zichtbaar; formaat en map zijn tijdens verwerking niet te wijzigen. De doelmap bevat kopieën van uitsluitend de geselecteerde boeken in het gekozen formaat.
- [X] Open een geëxporteerd ebook. Het bestand is leesbaar en het oorspronkelijke boek blijft in Saga aanwezig. Andere formaten, auteurs, tags, omslagen en overige metadata veranderen niet.
- [X] Exporteren naar dezelfde map via een opnieuw geopend exportvenster bewaart de bestaande kopieën en maakt genummerde nieuwe namen. Controleer dat eerder aanwezige bestanden niet zijn overschreven.
- [X Het resultaat toont per bestand de uitkomst en het doelbestand. Een volledige succesvolle export krijgt een korte groene melding. Boeken zonder gekozen formaat zijn duidelijk overgeslagen; dat veroorzaakt geen rode foutmelding. Een afgeronde export kan vanuit hetzelfde venster niet per ongeluk opnieuw starten.
- [X] Kies een boek of bestand waarvan je veilig weet dat het niet beschikbaar is, of gebruik een aparte onbruikbare doelmap als dat eenvoudig kan. De betrokken regel krijgt een begrijpelijke reden, en overige uitvoerbare bestanden worden verwerkt. Verander of verwijder hiervoor geen echte bibliotheekbestanden. Als dit niet veilig te reproduceren is: noteer niet van toepassing; geautomatiseerde tests dekken ontbrekende/gewijzigde bronnen, lees- en kopieerfouten.

## Annuleren, toetsenbord en venster

- [X] Bij een export die lang genoeg duurt: annuleer tijdens uitvoering. Voltooide kopieën blijven staan; de lopende gedeeltelijke kopie verdwijnt en niet-verwerkte regels zijn herkenbaar. Bronnen blijven behouden. Een kleine selectie kan te snel klaar zijn; dat is geen fout.
- [X] Indien mogelijk herhaal met Escape of het sluitkruis tijdens kopiëren. Het venster wacht op veilig stoppen en toont de resultaten; daarna kun je sluiten. Saga sluit niet onverwacht af.
- [X] Bedien formaatkeuze, mapknop, starten, annuleren en sluiten met het toetsenbord. Focus blijft bruikbaar; na afloop staat die op Sluiten. Escape sluit het venster als er geen export meer loopt.
- [X] Verklein het venster en probeer licht/donker. De knoppen blijven bereikbaar, langere resultaten zijn te lezen en het volledige doelpad is via de tooltip beschikbaar. Wissel zo gewenst de applicatietaal en controleer de exportteksten.
- [X] Controleer als regressie dat één formaat exporteren vanuit boekdetails nog werkt.

## Testnotities

De gebruiker heeft op 3 november 2026 de teststappen en aanvullende tests uitgevoerd zonder issues en de gehele slice geaccepteerd. PR #59 staat nog open. De door de gebruiker afgevinkte controlepunten zijn behouden.
