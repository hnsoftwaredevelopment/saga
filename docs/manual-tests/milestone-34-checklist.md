# Milestone 34 Handmatige Testchecklist

Gebruik uitsluitend de actuele Debug-build uit `Builds\Debug\Saga.exe`. De eerste start wijzigt de mappenstructuur van de actieve bibliotheek. Saga maakt vóór de eerste verplaatsing zelf een SQLite-back-up, maar controleer ook dat OneDrive volledig is bijgewerkt voordat je begint.

## Voorbereiding

- [ ] Sluit Saga en andere programma's die `C:\Users\hnijk\OneDrive\ELibrary` gebruiken.
- [ ] Controleer dat OneDrive voor de bibliotheek `Bijgewerkt` meldt.
- [ ] Controleer dat station C minimaal 7 GB vrije ruimte heeft; de huidige `library.db` is ongeveer 5,98 GB en de veiligheidskopie is ongeveer even groot.
- [ ] Controleer dat `C:\Users\hnijk\OneDrive\ELibrary\books` nog de oude directe boekmappen bevat.
- [ ] Start daarna alleen `Builds\Debug\Saga.exe` en laat het proces volledig afronden.

## Eerste start en migratie

- [ ] Controleer dat het splash-scherm eerst `Bibliotheekopslag optimaliseren` toont met een bewegende voortgangsbalk terwijl de databaseback-up wordt gemaakt.
- [ ] Controleer dat daarna een teller verschijnt en oploopt tot alle gerefereerde opslagmappen zijn verwerkt. Bij de gecontroleerde uitgangssituatie zijn dit ongeveer 34.460 mappen.
- [ ] Laat Saga en Windows tijdens deze eerste praktijktest actief; de geautomatiseerde tests dekken het hervatten na een onderbreking.
- [ ] Controleer dat Saga na voltooiing de normale bibliotheek opent en geen foutmelding toont.
- [ ] Controleer dat `C:\Users\hnijk\OneDrive\ELibrary\backups\library-before-sharded-storage-v2.db` bestaat en niet leeg is.
- [ ] Sluit Saga en start dezelfde build opnieuw; controleer dat geen tweede lange migratie of tweede back-up wordt uitgevoerd.

## Nieuwe mappenstructuur

- [ ] Open `C:\Users\hnijk\OneDrive\ELibrary\books` en controleer dat de meeste directe mappen korte namen zoals `00`, `7f` en `ff` hebben.
- [ ] Open enkele shardmappen en controleer dat daar de volledige opslag-ID's met ebookbestanden en eventuele `cover.jpg` staan.
- [ ] Controleer dat niet duizenden boekmappen meer rechtstreeks onder `books` staan. De 23 vooraf gevonden, onverwezen GUID-mappen mogen bewust blijven staan en zijn niet automatisch verwijderd.
- [ ] Controleer dat geen `.staging`- of `.tmp`-bestand van een voltooide migratie is achtergebleven.

## Bestaande boeken en samengevoegde opslag

- [ ] Open meerdere willekeurige EPUB-, PDF- en stripboekbestanden vanuit het detailpaneel.
- [ ] Controleer dat omslagen en beschikbare formaten nog zichtbaar zijn.
- [ ] Open indien aanwezig `Aan de rivier` van Laura Ingalls Wilder, `Anders` van Anita Terpstra en één ander eerder samengevoegd boek; controleer dat bestand en omslag correct openen. Deze route controleert opslag-ID's die bewust van het huidige boek-ID verschillen.
- [ ] Exporteer of sla één formaat op naar een gekozen map en controleer dat het bestand leesbaar is.
- [ ] Sluit Saga en open haar opnieuw; controleer dezelfde boeken nogmaals.

## Nieuwe import en onderhoud

- [ ] Importeer één nieuw testboek en controleer dat de map direct als `books\<eerste-2-tekens>\<boek-id>` wordt gemaakt.
- [ ] Voeg indien praktisch een tweede formaat aan hetzelfde boek toe en controleer dat beide bestanden in dezelfde boekmap staan.
- [ ] Wijzig de auteur van het testboek, sla op en controleer dat de opslagmap niet wordt verplaatst of hernoemd.
- [ ] Wijzig de omslag van het testboek en controleer dat `cover.jpg` in dezelfde shardmap wordt opgeslagen.
- [ ] Verwijder het testboek en controleer dat alleen zijn volledige boekmap verdwijnt; andere mappen in dezelfde shard blijven bestaan.

## OneDrive en regressie

- [ ] Wacht tot OneDrive alle verplaatsingen en de databaseback-up heeft verwerkt.
- [ ] Controleer of de eerdere melding over te veel onderdelen rechtstreeks in `books` verdwenen is.
- [ ] Controleer dat zoeken, filteren, de Quality Page en het detailpaneel nog normaal werken.
- [ ] Noteer afzonderlijk of scrollen en filteren merkbaar veranderd zijn; de gerichte performanceverbetering volgt in een eigen slice.
- [ ] Controleer dat alleen de actuele applicatiebuild in `Builds\Debug` staat.

## Alleen bij een foutmelding

- [ ] Noteer het volledige pad uit de melding en verander of verwijder niet op eigen initiatief één van de genoemde mappen.
- [ ] Controleer of OneDrive een synchronisatie- of vergrendelingsprobleem voor dat pad meldt.
- [ ] Start Saga na herstel opnieuw; reeds afgeronde opslagmappen horen te worden overgeslagen.
- [ ] Bewaar `backups\library-before-sharded-storage-v2.db`; Saga overschrijft of verwijdert deze back-up niet automatisch.
