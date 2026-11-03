# Milestone 58 — volgnummers aan meerdere serieboeken toekennen

Testbuild: `Builds\Debug\Saga.exe`. Verificatie: **1219 geautomatiseerde tests en 31 echte WPF-proeven geslaagd**, 0 mislukt/overgeslagen. Schone enige Debug-build **2026.11.3.33**, **3 november 2026 11:57:01 Europe/Amsterdam**, 0 waarschuwingen/fouten. Eigen vijfassige review uitgevoerd; licht/donker op minimale venstergrootte gerenderd en bekeken. SQL-batches boven 500 IDs, rollback, annulering, behoud overige metadata, echte metadata.json, bestandswaarschuwingen, paging/selectie/focus en bestaande één-boekroute gecontroleerd. De gebruiker heeft Milestone 58 op 3 november 2026 geaccepteerd: de uitgevoerde handmatige tests werken goed. Twee handmatige controles zijn niet uitgevoerd: bestandswaarschuwingen en conflict/rollback. Deze scenario's zijn geautomatiseerd afgedekt; de twee checklistvakjes blijven bewust open. Alleen de merge van PR #60 staat nog open. Gebruik een kleine serie waarvan je de oorspronkelijke nummers kent.

## Selecteren en voorstel

- [x] Open Series beheren, kies een serie en selecteer één boek. De bestaande knop Volgnummer wijzigen werkt nog. Zonder meervoudige selectie is Volgnummers toekennen uitgeschakeld.
- [x] Selecteer meerdere boeken met Ctrl of Shift. Volgnummers toekennen wordt beschikbaar; de losse één-boekactie is uitgeschakeld. Bij Meer laden blijft de bestaande selectie behouden. Wisselen van serie wist de boekselectie.
- [x] Open Volgnummers toekennen. Alleen geselecteerde boeken staan in het voorstel, met titel, auteurs, huidige en nieuwe nummers. Je hoeft niet opnieuw aan te vinken. De beginvolgorde volgt de boekenlijst en de cursor staat bij Startnummer.
- [x] Vul een startnummer in: de nieuwe nummers lopen vanaf daar telkens met één op. Een decimaal startnummer volgt de taalinstelling, bijvoorbeeld `1,5` in het Nederlands. Huidige nummers veranderen nog niet. De oorspronkelijke enkelvoudige editor blijft beschikbaar voor losse afwijkende nummers.
- [x] Typ ongeldige tekst of maak Startnummer leeg. Een duidelijke melding verschijnt en toekennen is uitgeschakeld; focus blijft in het tekstveld. Typ een geldig nummer en controleer dat de melding verdwijnt en je kunt blijven typen.
- [x] Selecteer een rij in het voorstel en gebruik Omhoog/Omlaag, of Ctrl+pijl-omhoog/omlaag terwijl de lijst focus heeft. Alleen de gekozen rij verplaatst; de nieuwe nummers passen zich aan en de rij blijft geselecteerd. De huidige nummers blijven zichtbaar.
- [x] Als een nieuw nummer ook bij een niet-geselecteerd boek in dezelfde serie bestaat, verschijnt informatie daarover. Toekennen blijft mogelijk. Dat andere boek wordt niet aangepast.

## Annuleren en opslaan

- [x] Wijzig startnummer en volgorde en annuleer met Annuleren of Escape. Alle oorspronkelijke nummers, boekgegevens en de selectie in Series beheren blijven behouden; de actieknop krijgt focus terug.
- [x] Open opnieuw, maak een voorstel en bevestig. Alleen geselecteerde boeken krijgen de getoonde nieuwe nummers. Niet-geselecteerde boeken en serienaam, titel, auteurs, tags, taal, omslagen en overige metadata blijven behouden.
- [x] De lijst wordt direct numeriek gesorteerd; de geselecteerde boeken blijven geselecteerd zolang ze binnen de geladen lijst staan. De resultaatmelding noemt kort hoeveel boeken werkelijk zijn bijgewerkt. Ongewijzigde nummers tellen niet als wijziging.
- [x] Sluit Series beheren en controleer boekdetails, lijst en groepering in het hoofdscherm: de nieuwe nummers zijn zichtbaar. Controleer ook dat de bestaande losse nummerwijziging nog werkt.
- [x] Indien je een grote serie hebt: laad meer boeken, selecteer boeken over meerdere geladen pagina's en pas hun nummers aan. De geladen hoeveelheid blijft behouden. Verplaatst een boek buiten de geladen lijst, dan legt Saga uit dat Meer laden nodig is; een onzichtbaar boek blijft niet geselecteerd.

## Fouten, bediening en weergave

- [x] Probeer indien eenvoudig een getoond nieuw nummer te kiezen dat gelijk is aan een bestaand nummer. Alleen informatie verschijnt; geen onverwachte blokkade of wijziging aan het andere boek.
- [ ] Bij een herkenbare bestandswaarschuwing staat duidelijk dat de nummers in Saga opgeslagen zijn en welke boeken een bestandsprobleem hadden. Forceer hiervoor geen fouten in echte boeken. Geautomatiseerde tests dekken bestandsfouten en annulering ná databasecommit.
- [X] Bedien het volledige voorstel met Tab, pijlen, Omhoog/Omlaag, Enter en Escape. Typen behoudt focus. Verklein het venster en controleer licht/donker; huidige/nieuwe nummers en knoppen blijven bruikbaar.
- [ ] Technische conflict-/rollbacktests zijn geautomatiseerd: een gewijzigd of verdwenen geselecteerd boek blokkeert de hele opslag; er wordt geen deel van de selectie aangepast. Dit hoeft niet met echte bibliotheekbestanden geforceerd te worden.

## Notities

De gebruiker heeft Milestone 58 op 3 november 2026 geaccepteerd: de uitgevoerde handmatige tests werken goed. Twee handmatige controles zijn niet uitgevoerd: bestandswaarschuwingen en conflict/rollback. Deze scenario's zijn geautomatiseerd afgedekt; de twee checklistvakjes blijven bewust open. Alleen de merge van PR #60 staat nog open.
