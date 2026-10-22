# Auteursbeheer: veilig hernoemen

## Status en build

De gebruiker heeft hernoemen en het aangeleverde auteuricoon getest en geaccepteerd. Definitieve Debug-build: **2026.10.22.109**, 22 oktober 2026 om 18:09 (Europe/Amsterdam), onder Builds/Debug/Saga.exe. 933 tests geslaagd, 0 mislukt of overgeslagen; build zonder waarschuwingen/fouten. Eigen review en WPF-render met fictieve gegevens uitgevoerd, ook op 820 × 480 clientruimte.

De eerdere profielcontroles blijven behouden in [Milestone 48](milestone-48-checklist.md). Onderstaande acceptatie is geen claim dat elk afzonderlijk randgeval handmatig is getest.

## Gebruikersacceptatie

- [x] Gebruiker bevestigt dat het hernoemen van een auteur goed werkt.
- [x] Gebruiker bevestigt dat het eigen icoon op de knop goed zichtbaar is.
- [ ] Gerichte nacontrole na de laatste review: verklein het auteursvenster, scroll het rechter profielpaneel en controleer dat Opslaan, Annuleren en Sluiten bereikbaar blijven. De zichtbare splitter moet bruikbaar blijven. De hernoemroute hoeft niet volledig opnieuw getest te worden.

## Optionele aanvullende controles

- [ ] Hernoem een auteur met meer dan 200 boeken; gebruik Meer laden en controleer dat de titelvolgorde behouden blijft zonder doublures. Dit is automatisch met 205 boeken afgedekt.
- [ ] Probeer een bestaande doelnaam: begrijpelijke weigering, geen samenvoeging of gedeeltelijke wijziging. Wijs boeken desgewenst afzonderlijk via boekdetails toe.
- [ ] Controleer na hernoemen behoud van foto, website, biografie, mede-auteurs en hun volgorde; auteursfilters en geselecteerde boekdetails actualiseren na sluiten.
- [ ] Annuleer de bevestiging; er wijzigt niets. Openstaande profielwijzigingen volgen het bestaande opslaan/verwerpen/annuleren-patroon.

## Automatisch afgedekte veiligheid

Behoud van auteuridentiteit en profiel; nul boeken; bezette naam ook zonder boeken; ongeldige naam; schrijfwijzecorrectie; verouderde bevestiging; alle boeken voorbij paginagrenzen; duplicaatbescherming; volledige rollback bij databasefout; geen tweede opdracht tijdens opslag; metadata-bestandswaarschuwingen na succesvolle databasecommit; oude waarschuwingen wissen bij auteurswissel. Geen gebruikersbibliotheek beschadigen om deze fouten op te wekken. Database en bestanden vormen bewust geen gezamenlijke transactie.

PR #49 blijft ter beoordeling; alleen de gebruiker mergt.
