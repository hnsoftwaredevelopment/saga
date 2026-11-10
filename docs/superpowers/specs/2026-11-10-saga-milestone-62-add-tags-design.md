# Milestone 62 — tags toevoegen aan geselecteerde boeken

Kies in **Tags beheren** een tag en selecteer de boeken waaraan je tags wilt toevoegen. Open **Tags toevoegen**. Zoek een bestaande tag of typ een nieuwe naam. Zet één of meer namen in het voorstel en controleer het aantal geselecteerde boeken. Je kunt namen weer uit het voorstel halen. Annuleren behoudt de boekselectie en de bibliotheekgegevens.

Saga zoekt bestaande tags op hun genormaliseerde naam en bewaart hun schrijfwijze. Ontbrekende koppelingen worden in invoervolgorde achter de bestaande tags geplaatst. Boeken die alle gekozen tags al hebben, worden overgeslagen. De database controleert de geselecteerde bronkoppelingen opnieuw en slaat nieuwe tags en koppelingen in één transactie op. De bestaande bestandssynchronisatie verwerkt daarna uitsluitend de gewijzigde boeken. De bronlijst, doeltellingen en hoofdweergave worden bijgewerkt via de bestaande beheerroute.
