# Milestone 63 — serie wijzigen

## Hertest met één invoerveld

Open `Builds\Debug\Saga.exe`, versie **2026.11.11.3** van **11 november 2026 om 15:53:37** (Europe/Amsterdam). **1.255 geautomatiseerde tests en 34 WPF-proeven zijn geslaagd.** De build heeft 0 waarschuwingen en 0 fouten. De invoer is in beide thema’s bij minimale venstergrootte gecontroleerd.

- [ ] Open **Serie wijzigen** en typ een deel van een bestaande serienaam in **Doelserie**. Kies de suggestie met de muis en herhaal met de pijltjestoetsen en Enter. Controleer de ingevulde naam en eventuele overlap.
- [ ] Typ de volledige naam van een bestaande serie. Saga herkent haar als bestaande doelserie en toont eventuele dubbele nummers. Verander de invoer en controleer dat de waarschuwing van de vorige bestemming verdwijnt.
- [ ] Typ een nieuwe naam. Bij een lege lijst kun je direct bevestigen. Bij vergelijkbare namen kun je de getypte naam expliciet als nieuwe serie kiezen. Annuleren behoudt de oorspronkelijke selectie en gegevens.
- [ ] Selecteer alle boeken van een bronserie. Controleer dat je ze naar een bestaande serie kunt verplaatsen. Voor een nieuwe naam verwijst Saga naar **Serie hernoemen**.

## Uitgevoerd met de vorige invoer

De gebruiker heeft alle onderstaande stappen uitgevoerd met build **2026.11.10.32**. Het verplaatsen werkte zoals verwacht. De twee tabbladen zijn daarna vervangen door één invoerveld.

Open `Builds\Debug\Saga.exe`, versie **2026.11.10.32** van **10 november 2026 om 16:58:59** (Europe/Amsterdam). Kies een bronserie met minstens twee testboeken en een doelserie waarin een van hun volgnummers al voorkomt.

- [X] Kies in **Series beheren** één of meer boeken en open **Serie wijzigen**. Controleer de titels, huidige volgnummers en het aantal achterblijvers.
- [X] Zoek op het tabblad **Bestaande serie** naar de bestemming en kies deze. Controleer dat Saga het dubbele volgnummer toont en dat bevestigen mogelijk blijft.
- [X] Annuleer. De serie en de nummers blijven behouden. De boekselectie blijft staan.
- [X] Herhaal de actie en bevestig. Saga toont de doelserie. De geselecteerde boeken staan daar met hun oorspronkelijke volgnummers. De bronserie en doelserie tonen de juiste aantallen.
- [X] Kies een deel van een andere serie en gebruik **Nieuwe serie**. Controleer dat Saga de nieuwe serie aanmaakt en alleen de geselecteerde boeken verplaatst. Een al bestaande naam vraagt om een expliciete keuze onder **Bestaande serie**.
- [X] Verplaats het laatste boek uit een testserie naar een bestaande serie. Controleer dat de lege bron uit de serielijst verdwijnt. Als je alle boeken een nieuwe naam wilt geven, verwijst **Nieuwe serie** naar **Serie hernoemen**.
- [X] Controleer de boekdetails na het sluiten van het beheer. Controleer ook de bestaande acties **Serie hernoemen**, **Volgnummers toekennen** en **Geselecteerde series samenvoegen**.
- [X] Test met toetsenbord en muis in beide thema’s. Verklein het venster en controleer of je de waarschuwing, boeklijst en bevestigingsknoppen kunt bereiken.

**1.255 geautomatiseerde tests en 34 WPF-proeven zijn geslaagd.** De Debug-build heeft 0 waarschuwingen en 0 fouten. Het voorstel is in beide thema’s bij minimale venstergrootte gecontroleerd.
