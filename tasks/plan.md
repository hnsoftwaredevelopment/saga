# Actief Plan: Milestone 28 Kwaliteitsuitzonderingen

## Resultaat

Een gebruiker kan één kwaliteitsmelding voor één boek als correct markeren. Saga bewaart die uitzondering per bibliotheek, verbergt alleen die exacte melding en laat haar via Instellingen herstellen.

## Afhankelijkheidsvolgorde

1. Stabiele signaalsleutels en domeincontract.
2. Additief SQLite-model en EF Core-migratie.
3. Repositorygedrag voor opslaan, ophalen en herstellen.
4. Dashboardfiltering en de actie `Dit is correct`.
5. Dashboardintegratie, selectiegedrag en foutfeedback.
6. Beheer-viewmodel en beheerwindow.
7. Koppeling met Instellingen en de actieve bibliotheek.
8. Volledige lokalisatie, documentatie en verificatie.

## Ontwerpbron

Zie `docs/superpowers/specs/2026-08-27-saga-milestone-28-quality-issue-exclusions-design.md`.

## Uitvoeringsplan

Zie `docs/superpowers/plans/2026-08-27-saga-milestone-28-quality-issue-exclusions.md`.

## Werkafspraken

- Iedere gedragswijziging begint met een falende test.
- Iedere afgeronde taak laat tests en build in werkende toestand achter.
- De migratie wordt gegenereerd met de bestaande EF Core-tooling; bestaande migraties worden niet aangepast.
- De uiteindelijke pull request wordt direct als normale PR geopend, niet als draft.
