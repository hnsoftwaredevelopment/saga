# Milestone 43 handmatige testchecklist: meerdere serienamen herstellen

Gebruik de actuele Debug-build:

`Builds\Debug\Saga.exe` — versie `2026.10.21.32`

## Voorbereiding

- [ ] Maak zo nodig eerst een bibliotheekback-up.
- [ ] Zorg voor minstens twee testboeken met een verschillend serienummer maar zonder serienaam.
- [ ] Noteer van deze boeken titel, auteur, serienummer en enkele andere metadatavelden.

## Splash-scherm

- [ ] Start Saga en controleer dat `Saga` en de subtitel visueel netjes rond dezelfde verticale middenlijn staan.
- [ ] Controleer dat de onderlinge uitlijning natuurlijk oogt en niet door een opvallende lege ruimte of afkapping wordt verstoord.

## Selectie en bulkcontext

- [ ] Open de Quality Page en kies `Serienummer zonder serie`.
- [ ] Selecteer met Ctrl minstens twee niet-aaneengesloten boeken.
- [ ] Controleer dat `Serie wijzigen` beschikbaar is bij deze meervoudige selectie.
- [ ] Open het serievenster en controleer dat het juiste aantal geselecteerde boeken wordt genoemd.
- [ ] Controleer dat het venster duidelijk meldt dat ieder boek zijn huidige serienummer behoudt.
- [ ] Controleer dat de bevestigingsknop vermeldt voor hoeveel boeken de serie wordt gewijzigd.

## Suggesties, toetsenbord en annuleren

- [ ] Typ snel enkele letters van een bekende serie en controleer dat het veld direct reageert en de suggesties correct filteren.
- [ ] Kies een suggestie met de muis; de volledige serienaam wordt in het invoerveld overgenomen.
- [ ] Herhaal dit met de pijltoetsen en Enter.
- [ ] Sluit met `Annuleren` of Escape en controleer dat geen enkel boek is gewijzigd.

## Bestaande serie toepassen

- [ ] Open het venster opnieuw, kies een bekende serie en bevestig.
- [ ] Bij volledig succes verschijnt alleen een korte groene melding met het aantal gewijzigde boeken.
- [ ] De herstelde boeken verdwijnen uit `Serienummer zonder serie` en de telling wordt bijgewerkt.
- [ ] Controleer in het hoofdscherm dat alle gekozen boeken dezelfde serienaam hebben.
- [ ] Controleer dat ieder boek exact zijn eigen oorspronkelijke serienummer heeft behouden.
- [ ] Controleer dat het seriefilter direct de gekozen serie met het juiste aantal toont.

## Nieuwe serie en regressies

- [ ] Herhaal de bulkroute met andere testboeken en voer een volledig nieuwe serienaam in.
- [ ] Controleer dat de nieuwe serie na opslaan in bibliotheek en seriefilter verschijnt.
- [ ] Open daarna één boek met dit probleem en controleer dat titel en serienummer zoals voorheen afzonderlijk worden getoond.
- [ ] Controleer kort dat bulk auteur, bulk taal, `Dit is correct` en de overige enkelvoudige herstelacties nog beschikbaar zijn in hun eigen categorieën.

## Gegevensbehoud en herstart

- [ ] Controleer dat titel, auteurs, taal, tags, omslag, beschrijving, uitgever, ISBN, leesstatus en formaten niet zijn gewijzigd.
- [ ] Sluit Saga, open dezelfde bibliotheek opnieuw en controleer dat serienaam en individuele serienummers behouden zijn gebleven.

## Resultaat

- [ ] Alle controles zijn geslaagd.
- [ ] Eventuele afwijkingen zijn genoteerd met de gekozen boeken, stappen en zichtbare melding.
