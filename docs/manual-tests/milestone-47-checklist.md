# Milestone 47: afronding betrouwbaarheid Quality

Testbuild: `Builds\Debug\Saga.exe`, versie `2026.10.22.65`, gebouwd op 22 oktober 2026 om 14:34 (Europe/Amsterdam). Automatisch: 888 tests geslaagd, 0 mislukt of overgeslagen. Debug-build met 0 waarschuwingen en fouten. De overige handmatige controles zijn door de gebruiker goedgekeurd; het herstelconflict is niet handmatig gereproduceerd en blijft hieronder onafgevinkt.

- [X] Open Instellingen / Quality / genegeerde meldingen beheren. Zet enkele geselecteerde meldingen terug: alleen deze verdwijnen; de telling klopt.
- [X] Selecteer andere regels en herhaal: selectie en telling blijven correct. De meldingen worden bij de volgende opening van Quality opnieuw gecontroleerd.
- [X] Alles terugzetten vraagt nog steeds om bevestiging; Nee/Escape wijzigt niets. Bevestigen maakt de lijst leeg.
- [X] Tijdens een eventuele langere opslag zijn beide terugzetacties uitgeschakeld. Een latere selectie verandert niet welke oorspronkelijke regels worden teruggezet. Dit uitgestelde scenario is ook automatisch getest.
- [ ] Reproduceer indien mogelijk veilig een herstelconflict in een testbibliotheek. Kies daarna Dit is correct voor die melding: de oude hersteldetails verdwijnen.
- [X] Gebruik toetsenbord en muis; het beheervenster en de bevestiging blijven bruikbaar. Controleer zo mogelijk een andere Saga-taal.
- [X] De zes bestaande kwaliteitscontroles en herstelacties blijven herkenbaar werken.

Opslagfouten, retry en annulering worden automatisch getest met testdubbels. Forceer hiervoor geen fouten of gewijzigde rechten in de echte bibliotheek. De foutmelding moet begrijpelijk zijn en geen technische details bevatten; regels verdwijnen pas na succesvolle opslag. Noteer veilig handmatig geteste foutgevallen afzonderlijk.

## Gebruikersbevinding

De gebruiker meldt dat alle overige controles goed werken. Alleen het herstelconflict kon niet worden gereproduceerd; dit scenario is dus niet handmatig uitgevoerd. De automatische regressietests controleren dat een bevestigde Dit is correct-actie oude foutdetails opruimt, zowel bij succesvol als bij mislukt markeren. De reeds door de gebruiker afgevinkte controles zijn behouden.
