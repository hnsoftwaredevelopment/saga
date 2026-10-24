# Milestone 50 — Series beheren

Test met `Builds\Debug\Saga.exe`, versie **2026.10.24.52**, gebouwd op **24 oktober 2026 om 15:01** (Europe/Amsterdam). Handmatige acceptatie staat open; onderstaande punten zijn nog niet door de gebruiker afgevinkt.

## Zoeken en bekijken

- [ ] Open **Series beheren** via het boekenrij-icoon naast Auteurs beheren in de hoofdwerkbalk. Links staan series met boekaantallen; boeken zonder serie staan hier niet tussen.
- [ ] Zoek op een deel van een serienaam, vul snel meer letters in en wis de zoektekst. De lijst reageert en er verschijnen geen oude resultaten over de nieuwe zoekopdracht.
- [ ] Zoek een niet-bestaande naam: **Geen series gevonden.** verschijnt. Wissen toont de series weer.
- [ ] Selecteer een serie. Rechts staan de volledige boektelling, serienummer, titel en auteur(s). Verschillende auteurs mogen in dezelfde serie voorkomen.
- [ ] Controleer waar beschikbaar nummers zoals 1, 1,5, 2 en 10; nummers ontbreken onderaan. De lijst is alleen-lezen.
- [ ] Gebruik **Meer laden** bij een lange series- of boekenlijst als die beschikbaar is. Boeken blijven op volgorde zonder doublures.

## Hernoemen

Kies een serie waarvan de naam tijdelijk gewijzigd mag worden; hernoem haar na de test terug.

- [ ] Klik **Serie hernoemen**. De huidige naam en het volledige aantal betrokken boeken staan vermeld; de nieuwe naam heeft focus.
- [ ] Voer een andere naam in en annuleer, ook eenmaal met Escape. Er is niets gewijzigd.
- [ ] Een lege naam kan niet worden bevestigd. Een naam van een andere bestaande serie wordt met duidelijke uitleg geweigerd; beide series blijven bestaan.
- [ ] Bevestig een nieuwe vrije naam, eventueel met Enter. Er verschijnt een korte groene melding. De nieuwe naam staat in het zoekveld en blijft geselecteerd.
- [ ] Alle betrokken boeken hebben de nieuwe serienaam. Serienummers, auteurs, titels, tags en overige gegevens zijn behouden. Een hoofdlettercorrectie van dezelfde naam mag ook.
- [ ] Sluit Series beheren. Het hoofdscherm, details en seriefilter tonen de nieuwe naam; geladen omslagen blijven zichtbaar. Hernoem de serie zo nodig terug.

## Venster en regressie

- [ ] Vergroot/verklein het venster en versleep de zichtbare splitter. Zoekveld, boekenlijst, hernoemen en sluiten blijven bereikbaar; rechts kan worden gescrold.
- [ ] Gebruik Tab, pijltjes, Enter en Escape. Tijdens opslaan is een tweede wijziging/serie-selectie geblokkeerd.
- [ ] Auteurs beheren, gewone boekdetails en meervoudige metadatawijzigingen werken zoals voorheen.

## Automatisch afgedekt

980 tests slagen, waaronder Unicode-/schrijfvariantgroepering, exacte numerieke paginering voorbij 200 boeken, hernoemen van 510 boeken, bezette doelnaam, gewijzigde bron/boekset, volledige rollback, annulering vóór opslag, echte metadata.json, bestandswaarschuwingen, doorwerken na annulering ná commit, behoud van overige metadata en relaties, late zoekresultaten en zes talen. Deze fouten hoeven niet met echte boeken te worden opgewekt. Bestandswaarschuwingen noemen de boeken in een uitklapbare lijst; de databasewijziging blijft dan opgeslagen.
