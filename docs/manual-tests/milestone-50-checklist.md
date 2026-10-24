# Milestone 50 — Series beheren

Test met `Builds\Debug\Saga.exe`, versie **2026.10.24.61**, gebouwd op **24 oktober 2026 om 15:35** (Europe/Amsterdam). De gebruiker heeft de hoofdroute op de oorspronkelijke build afgevinkt; die vinkjes blijven behouden. Alleen de toetsenbordcontrole en de gerichte aanvullende controles hieronder staan open.

## Zoeken en bekijken

- [X] Open **Series beheren** via het boekenrij-icoon naast Auteurs beheren in de hoofdwerkbalk. Links staan series met boekaantallen; boeken zonder serie staan hier niet tussen.
- [X] Zoek op een deel van een serienaam, vul snel meer letters in en wis de zoektekst. De lijst reageert en er verschijnen geen oude resultaten over de nieuwe zoekopdracht.
- [X] Zoek een niet-bestaande naam: **Geen series gevonden.** verschijnt. Wissen toont de series weer.
- [X] Selecteer een serie. Rechts staan de volledige boektelling, serienummer, titel en auteur(s). Verschillende auteurs mogen in dezelfde serie voorkomen.
- [X] Controleer waar beschikbaar nummers zoals 1, 1,5, 2 en 10; nummers ontbreken onderaan. De lijst is alleen-lezen.
- [X] Gebruik **Meer laden** bij een lange series- of boekenlijst als die beschikbaar is. Boeken blijven op volgorde zonder doublures.

## Hernoemen

Kies een serie waarvan de naam tijdelijk gewijzigd mag worden; hernoem haar na de test terug.

- [X] Klik **Serie hernoemen**. De huidige naam en het volledige aantal betrokken boeken staan vermeld; de nieuwe naam heeft focus.
- [X] Voer een andere naam in en annuleer, ook eenmaal met Escape. Er is niets gewijzigd.
- [X] Een lege naam kan niet worden bevestigd. Een naam van een andere bestaande serie wordt met duidelijke uitleg geweigerd; beide series blijven bestaan.
- [X] Bevestig een nieuwe vrije naam, eventueel met Enter. Er verschijnt een korte groene melding. De nieuwe naam staat in het zoekveld en blijft geselecteerd.
- [X] Alle betrokken boeken hebben de nieuwe serienaam. Serienummers, auteurs, titels, tags en overige gegevens zijn behouden. Een hoofdlettercorrectie van dezelfde naam mag ook.
- [X] Sluit Series beheren. Het hoofdscherm, details en seriefilter tonen de nieuwe naam; geladen omslagen blijven zichtbaar. Hernoem de serie zo nodig terug.

## Venster en regressie

- [X] Vergroot/verklein het venster en versleep de zichtbare splitter. Zoekveld, boekenlijst, hernoemen en sluiten blijven bereikbaar; rechts kan worden gescrold.
- [ ] Gebruik Tab, pijltjes, Enter en Escape. Tijdens opslaan is een tweede wijziging/serie-selectie geblokkeerd.
- [X] Auteurs beheren, gewone boekdetails en meervoudige metadatawijzigingen werken zoals voorheen.

## Automatisch afgedekt

980 tests slagen, waaronder Unicode-/schrijfvariantgroepering, exacte numerieke paginering voorbij 200 boeken, hernoemen van 510 boeken, bezette doelnaam, gewijzigde bron/boekset, volledige rollback, annulering vóór opslag, echte metadata.json, bestandswaarschuwingen, doorwerken na annulering ná commit, behoud van overige metadata en relaties, late zoekresultaten en zes talen. Deze fouten hoeven niet met echte boeken te worden opgewekt. Bestandswaarschuwingen noemen de boeken in een uitklapbare lijst; de databasewijziging blijft dan opgeslagen.

## Gerichte hercontrole toetsenbord — build 2026.10.24.61

- [ ] Ga met Tab naar de serieslijst en kies achtereenvolgens meerdere series met de pijltjestoetsen. Na het laden kun je direct met de volgende pijltjestoets verder, zonder opnieuw te tabben.
- [ ] Typ een zoeknaam met korte pauzes tussen de letters. De focus blijft in het zoekveld en de tekst blijft intact. Ga ook direct na een seriekeuze terug naar het zoekveld: een vertraagd focusherstel mag je daar niet uit halen.
- [ ] Open **Serie hernoemen** met het toetsenbord, typ een naam en annuleer met Escape. De naam-invoer blijft tijdens de dialoog actief; na sluiten staat de focus weer op de hernoemknop. Controleer dit zo mogelijk ook na bevestigen van een tijdelijke naam.

Aanvullend zijn vijf scenario's op het echte WPF-venster automatisch gecontroleerd: opeenvolgende toetsenbordselecties, terugkerende focus na een laadfout, respecteren van een nieuwere focuskeuze, zoektekst bij meerdere verversingen en de hernoemdialoog met annuleren/opslaan. Uitvoeren op een interactief Windows-bureaublad met `dotnet run --project tests/EbookManager.WpfSmokeTests/EbookManager.WpfSmokeTests.csproj -c Debug`. Dit losse testprogramma gebruikt alleen fictieve gegevens en start niet de normale Saga-app of een echte bibliotheek; het zit bewust buiten de gewone testset die ook zonder interactief bureaublad kan draaien.
