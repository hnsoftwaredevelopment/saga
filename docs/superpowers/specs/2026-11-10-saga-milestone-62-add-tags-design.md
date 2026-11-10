# Milestone 62 — tags toevoegen aan geselecteerde boeken

Kies in **Tags beheren** een tag en selecteer de boeken waaraan je tags wilt toevoegen. Open **Tags toevoegen**. Tijdens het typen toont Saga alle bestaande tags die met de invoer beginnen in een scrollbare lijst. Met pijltjes en Enter kies je een suggestie. Zonder gekozen suggestie bevestigt Enter de huidige naam. Spaties blijven onderdeel van de tag. Bij plakken wordt iedere regel één tag. Als een regel ongeldig is, blijven de invoer en het voorstel intact en verschijnt een melding.

Een bevestigde tag staat als verwijderbare chip in het voorstel. De cursor gaat naar de volgende invoerregel. Controleer de chips en het aantal geselecteerde boeken. De knop **Tags toevoegen** slaat het voorstel op. Annuleren behoudt de boekselectie en de bibliotheekgegevens.

Saga zoekt bestaande tags op hun genormaliseerde naam en bewaart hun schrijfwijze. Ontbrekende koppelingen worden in invoervolgorde achter de bestaande tags geplaatst. Boeken die alle gekozen tags al hebben, worden overgeslagen. De database controleert de geselecteerde bronkoppelingen opnieuw en slaat nieuwe tags en koppelingen in één transactie op. De bestaande bestandssynchronisatie verwerkt daarna uitsluitend de gewijzigde boeken. De bronlijst, doeltellingen en hoofdweergave worden bijgewerkt via de bestaande beheerroute.
