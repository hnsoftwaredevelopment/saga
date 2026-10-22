# Milestone 43 handmatige testchecklist: meerdere serienamen herstellen

Gebruik de actuele Debug-build:

`Builds\Debug\Saga.exe` — versie `2026.10.22.6`

## Voorbereiding

- [x] Maak zo nodig eerst een bibliotheekback-up. ✅ 2026-10-21
- [x] Zorg voor minstens twee testboeken met een verschillend serienummer maar zonder serienaam. ✅ 2026-10-21
- [x] Noteer van deze boeken titel, auteur, serienummer en enkele andere metadatavelden. ✅ 2026-10-21

## Splash-scherm

- [x] Start Saga en controleer dat `Saga` en de subtitel visueel netjes rond dezelfde verticale middenlijn staan. ✅ 2026-10-22
- [x] Controleer dat de onderlinge uitlijning natuurlijk oogt en niet door een opvallende lege ruimte of afkapping wordt verstoord. ✅ 2026-10-21

## Selectie en bulkcontext

- [x] Open de Quality Page en kies `Serienummer zonder serie`. ✅ 2026-10-21
- [x] Selecteer met Ctrl minstens twee niet-aaneengesloten boeken. ✅ 2026-10-21
- [x] Controleer dat `Serie wijzigen` beschikbaar is bij deze meervoudige selectie. ✅ 2026-10-21
- [x] Open het serievenster en controleer dat het juiste aantal geselecteerde boeken wordt genoemd. ✅ 2026-10-21
- [x] Controleer dat het venster duidelijk meldt dat ieder boek zijn huidige serienummer behoudt. ✅ 2026-10-21
- [x] Controleer dat de bevestigingsknop vermeldt voor hoeveel boeken de serie wordt gewijzigd. ✅ 2026-10-21

## Suggesties, toetsenbord en annuleren

- [x] Typ snel enkele letters van een bekende serie en controleer dat het veld direct reageert en de suggesties correct filteren. ✅ 2026-10-21
- [x] Kies een suggestie met de muis; de volledige serienaam wordt in het invoerveld overgenomen. ✅ 2026-10-21
- [x] Herhaal dit met de pijltoetsen en Enter. ✅ 2026-10-21
- [x] Sluit met `Annuleren` of Escape en controleer dat geen enkel boek is gewijzigd. ✅ 2026-10-21

## Bestaande serie toepassen

- [x] Open het venster opnieuw, kies een bekende serie en bevestig. ✅ 2026-10-21
- [x] Bij volledig succes verschijnt alleen een korte groene melding met het aantal gewijzigde boeken. ✅ 2026-10-21
- [x] De herstelde boeken verdwijnen uit `Serienummer zonder serie` en de telling wordt bijgewerkt. ✅ 2026-10-21
- [x] Controleer in het hoofdscherm dat alle gekozen boeken dezelfde serienaam hebben. ✅ 2026-10-21
- [x] Controleer dat ieder boek exact zijn eigen oorspronkelijke serienummer heeft behouden. ✅ 2026-10-21
- [x] Controleer dat het seriefilter direct de gekozen serie met het juiste aantal toont. ✅ 2026-10-21

## Nieuwe serie en regressies

- [x] Herhaal de bulkroute met andere testboeken en voer een volledig nieuwe serienaam in. ✅ 2026-10-21
- [x] Controleer dat de nieuwe serie na opslaan in bibliotheek en seriefilter verschijnt. ✅ 2026-10-21
- [x] Open daarna één boek met dit probleem en controleer dat titel en serienummer zoals voorheen afzonderlijk worden getoond. ✅ 2026-10-21
- [x] Controleer kort dat bulk auteur, bulk taal, `Dit is correct` en de overige enkelvoudige herstelacties nog beschikbaar zijn in hun eigen categorieën. ✅ 2026-10-21

## Gegevensbehoud en herstart

- [x] Controleer dat titel, auteurs, taal, tags, omslag, beschrijving, uitgever, ISBN, leesstatus en formaten niet zijn gewijzigd. ✅ 2026-10-21
- [x] Sluit Saga, open dezelfde bibliotheek opnieuw en controleer dat serienaam en individuele serienummers behouden zijn gebleven. ✅ 2026-10-21

## Resultaat

- [x] Het serieherstel en de definitieve Splash-uitlijning zijn volledig geslaagd. ✅ 2026-10-22
- [x] De oorspronkelijke afwijking en de gekozen correctie zijn vastgelegd. ✅ 2026-10-22
