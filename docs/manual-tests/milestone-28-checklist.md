# Milestone 28 Handmatige Testchecklist

Gebruik een testbibliotheek of een bibliotheek waarvan een actuele back-up beschikbaar is. De actie `Dit is correct` wijzigt geen boekmetadata; de verwijdertest hieronder verwijdert wel een testboek.

Uitvoering afgerond op 1 september 2026. De dashboardactie, herstelfunctie, splitter en begrijpelijke Nederlandse teksten zijn door de gebruiker bevestigd. Bibliotheekisolatie, cascade-opruiming en alle ondersteunde vertalingen zijn aanvullend automatisch gecontroleerd.

## Dashboard en exacte melding

- [ ] Open het metadata-kwaliteitsscherm en controleer dat de linker splitter zichtbaar en bedienbaar blijft.
- [ ] Selecteer een boek waarvoor minstens twee verschillende kwaliteitsmeldingen bestaan.
- [ ] Kies bij één melding `Dit is correct` en controleer dat alleen die boek-/signaalcombinatie verdwijnt.
- [ ] Controleer dat de andere melding voor hetzelfde boek zichtbaar blijft.
- [ ] Controleer dat het aantal bij de categorie en het totale aantal direct worden bijgewerkt.
- [ ] Herhaal dit voor de eerste, een middelste en de laatste rij; controleer telkens de logische vervolgselectie.
- [ ] Maak één categorie leeg en controleer dat de boekselectie verdwijnt en boekacties worden uitgeschakeld.
- [ ] Sluit het dashboard, open het opnieuw en controleer dat genegeerde meldingen verborgen blijven.

## Toetsenbord en bestaande navigatie

- [ ] Bereik `Dit is correct` met `Tab` en activeer de knop met `Spatie` of `Enter`.
- [ ] Open een boek met `Openen in bibliotheek` en controleer dat zoeken, filters, groepering en scrollen nog correct worden afgehandeld.
- [ ] Open een boek via dubbelklik en controleer hetzelfde navigatiegedrag.
- [ ] Bedien de splitter met muis en toetsenbord en controleer dat geen paneel buiten bereik raakt.

## Herstellen via Instellingen

- [ ] Open Instellingen, ga naar Duplicaten en kies `Genegeerde kwaliteitsmeldingen beheren`.
- [ ] Controleer dat iedere rij boektitel, auteurs, begrijpelijke kwaliteitsmelding en datum toont.
- [ ] Selecteer meerdere rijen en kies `Geselecteerde herstellen`; controleer dat alleen die rijen verdwijnen.
- [ ] Open het dashboard opnieuw en controleer dat herstelde meldingen opnieuw worden beoordeeld en zo nodig terugkeren.
- [ ] Kies `Alles herstellen`, antwoord eerst `Nee` en controleer dat niets verandert.
- [ ] Kies opnieuw `Alles herstellen`, antwoord `Ja` en controleer de duidelijke lege toestand.
- [ ] Sluit het beheerwindow zonder actie en controleer dat niets verandert.
- [ ] Bedien selectie, herstelknoppen en sluiten volledig met het toetsenbord.

## Bibliotheekgrenzen en opruimen

- [ ] Negeer een melding in bibliotheek A, wissel naar bibliotheek B en controleer dat de melding uit A daar niet wordt getoond.
- [ ] Wissel terug naar bibliotheek A en controleer dat de genegeerde melding daar nog bestaat.
- [ ] Negeer een melding voor een verwijderbaar testboek, verwijder dat boek en controleer dat de melding niet meer in het beheerwindow staat.

## Lokalisatie

- [ ] Controleer de Nederlandse knop-, beschrijvings-, lege-toestand- en bevestigingsteksten op begrijpelijkheid.
- [ ] Wissel steekproefsgewijs naar een andere ondersteunde taal en controleer dat geen interne sleutel zoals `MetadataQuality...` of een signaalsleutel zichtbaar is.

## Eindresultaat

- [ ] Alle bovenstaande controles zijn geslaagd zonder regressie in openen, dubbelklik, splitter of bibliotheekwissel.
