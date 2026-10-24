# Auteursbeheer: veilig hernoemen

## Status en build

De gebruiker heeft de slice met hernoemen, auteuricoon en vensterindeling getest en geaccepteerd op de eerdere build 2026.10.22.109. De actuele Debug-build met de twee reviewcorrecties is **2026.10.24.7**, 24 oktober 2026 om 12:50 (Europe/Amsterdam), onder Builds/Debug/Saga.exe. 936 tests geslaagd, 0 mislukt of overgeslagen; build zonder waarschuwingen/fouten en precies één Saga.exe onder Builds. Eigen review uitgevoerd; geen nieuwe layout of vertalingen. De eerdere WPF-rendercontrole omvatte 820 × 480 clientruimte.

De eerdere profielcontroles blijven behouden in [Milestone 48](milestone-48-checklist.md). Onderstaande acceptatie is geen claim dat elk afzonderlijk randgeval handmatig is getest.

## Gebruikersacceptatie

- [x] Gebruiker bevestigt dat het hernoemen van een auteur goed werkt.
- [x] Gebruiker bevestigt dat het eigen icoon op de knop goed zichtbaar is.
- [x] Gebruiker bevestigt dat verkleinen en vergroten alle knoppen beschikbaar houdt; slice akkoord.
- [x] Gebruiker testte een auteur met 318 boeken zonder problemen; andere auteurs blijven bestaan na hernoemen.
- [x] Gebruiker bevestigt dat onbruikbare foto's worden geweigerd en een reeds aanwezige foto behouden blijft.

## Optionele aanvullende controles

- [x] Gebruiker testte hernoemen bij een auteur met meer dan 200 boeken (318); geen problemen gemeld. Paginering en titelvolgorde zijn ook automatisch met 205 boeken afgedekt.
- [ ] Probeer een bestaande doelnaam: begrijpelijke weigering, geen samenvoeging of gedeeltelijke wijziging. Wijs boeken desgewenst afzonderlijk via boekdetails toe.
- [ ] Controleer na hernoemen behoud van foto, website, biografie, mede-auteurs en hun volgorde; auteursfilters en geselecteerde boekdetails actualiseren na sluiten.
- [ ] Annuleer de bevestiging; er wijzigt niets. Openstaande profielwijzigingen volgen het bestaande opslaan/verwerpen/annuleren-patroon.

## Automatisch afgedekte veiligheid

Behoud van auteuridentiteit en profiel; nul boeken; bezette naam ook zonder boeken; ongeldige naam; schrijfwijzecorrectie; verouderde bevestiging; alle boeken voorbij paginagrenzen; duplicaatbescherming; volledige rollback bij databasefout; geen tweede opdracht tijdens opslag; metadata-bestandswaarschuwingen na succesvolle databasecommit; oude waarschuwingen wissen bij auteurswissel. Geen gebruikersbibliotheek beschadigen om deze fouten op te wekken. Database en bestanden vormen bewust geen gezamenlijke transactie.

PR #49 is door de gebruiker gemerged. Ook de laatste reviewcorrecties zijn handmatig getest en akkoord bevonden.

## Gerichte nacontrole van de reviewcorrecties op 24 oktober

De bovenstaande gebruikersacceptatie blijft geldig. De gebruiker heeft ook de twee latere reviewcorrecties getest en akkoord bevonden vóór afronding van de slice.

- [x] Hernoem een auteur naar een vrije naam die alfabetisch ver achteraan staat. Na succes bevat het zoekveld de nieuwe naam en blijft de auteur zichtbaar en geselecteerd.
- [x] Sluit auteursbeheer na hernoemen; boeken, details en auteursfilters tonen de nieuwe naam en bestaande omslagen blijven zichtbaar. Een volledige hertest van foto-opslag of vensterindeling is niet nodig.
