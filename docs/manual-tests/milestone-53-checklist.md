# Milestone 53 — auteurs bewust samenvoegen

Testbuild: Builds\Debug\Saga.exe **2026.10.30.20**, gebouwd op **30 oktober 2026 om 19:25:45 Europe/Amsterdam**. 0 waarschuwingen/fouten, precies één Saga.exe onder Builds. **1074 tests en 22 interactieve WPF-scenario's slagen.** Eigen review uitgevoerd. Handmatige gebruikersacceptatie staat open.

Gebruik bij voorkeur enkele kleine testauteurs. Samenvoegen is een bewuste bibliotheekwijziging: de gekozen doelauteur blijft bestaan, de overige geselecteerde auteurvermeldingen verdwijnen en hun boeken blijven behouden. De bestemming moet in deze eerste slice één van de geselecteerde auteurs zijn.

## Selecteren en annuleren

- [ ] Open Auteurs beheren. Met één auteur geselecteerd blijven profiel bewerken en hernoemen werken. Selecteer met Ctrl of Shift twee of meer auteurs: de telling klopt, Samenvoegen is actief en het gewone profiel rechts is niet bewerkbaar.
- [ ] Bij een onopgeslagen profielwijziging wordt vóór wisselen van selectie gevraagd om opslaan/verwerpen/annuleren. Annuleren behoudt de oorspronkelijke selectie en invoer; er worden geen verborgen wijzigingen samengevoegd.
- [ ] Kies Samenvoegen. Het venster toont de geselecteerde auteurs met aantallen en het aantal unieke boeken. Kies een bestemming en annuleer met de knop of Escape. Auteurs, boeken, foto's en selectie blijven behouden. Focus keert terug naar de samenvoegknop.

## Profielkeuzes en opslaan

- [ ] Kies een doelauteur uit de selectie. Bij verschillende biografieën, websites of foto's moet je per onderdeel kiezen welke behouden blijft; bevestigen is pas mogelijk na alle benodigde keuzes. Eén unieke waarde mag vooraf ingevuld zijn. Leeg laten is een expliciete keuze.
- [ ] Kies zo mogelijk een foto van een andere geselecteerde auteur: de voorvertoning toont de gekozen foto. Een onleesbare foto meldt dit; kies een andere of Leeg laten. Verschillende biografieën en websites zijn te beoordelen via de getoonde waarden.
- [ ] Bevestig. De bestemming houdt haar bestaande naam en krijgt precies de gekozen biografie, website en foto. De andere geselecteerde auteurvermeldingen verdwijnen direct. Niet-geselecteerde auteurs blijven bestaan.
- [ ] De overgebleven auteur is zichtbaar geselecteerd en toont alle unieke gekoppelde boeken met het juiste aantal. Een groene melding vermeldt het aantal gewijzigde boeken. Ook boeken buiten een eerder geladen pagina zijn verwerkt.
- [ ] Controleer een boek dat twee geselecteerde auteurvarianten had: de bestemming staat eenmaal in de auteurslijst. Was de doelauteur al aanwezig, dan behoudt zij haar positie; anders vervangt zij de eerste bronpositie. Overige mede-auteurs houden hun relatieve volgorde. Titel, serie, tags, taal, leesstatus en omslag blijven behouden.
- [ ] Sluit Auteurs beheren: details, auteursfilters, groepering en hoofdweergaven tonen de nieuwe auteursnamen zonder opnieuw te starten. Heropen het beheer of herstart Saga: profielkeuzes en foto zijn opgeslagen.

## Selectie, toetsenbord en venster

- [ ] Bij meer dan 200 auteurs behoudt Meer laden de bestaande selectie. Een nieuwe zoekopdracht wist de meervoudige selectie; er worden nooit onzichtbare auteurs meegenomen. Zonder minstens twee auteurs is Samenvoegen uitgeschakeld.
- [ ] Selecteren met pijltjes/Ctrl/Shift, typen in het zoekveld, Tab, Enter en Escape werken zonder focusverlies. Na een geslaagde samenvoeging blijft de doelauteur geselecteerd, ook na even wachten. Verklein en vergroot de vensters; keuzes kunnen scrollen, bevestigen/annuleren blijven bereikbaar. Controleer het gebruikelijke thema.

## Fouten en automatische dekking

- [ ] Alleen als dit eenvoudig met testgegevens kan: twee boeken met dezelfde titel die na samenvoegen dezelfde auteurs zouden krijgen, blokkeren de hele actie met een duidelijke melding dat een boek al bestaat. Er verdwijnen geen auteurs of boeken. Dit pad is ook automatisch getest; echte gegevens niet beschadigen om een fout uit te lokken.

Verouderde profielen/koppelingen, annulering vóór commit, terugdraaien bij een databasefout, 510 boeken, botsingen tussen bronboeken en met bestaande doelboeken, kopiëren/opruimen van foto's, onleesbare/onopslaanbare foto's en metadatawaarschuwingen zijn automatisch afgedekt. Na commit wordt bestaande bestandssynchronisatie afgerond, ook na annulering. Bij een verversingsfout na opslag blijven gewijzigde boek-ID's en bestandswaarschuwingen bewaard. Moeilijk te reproduceren bestandsfouten hoeven niet handmatig te worden geforceerd.
