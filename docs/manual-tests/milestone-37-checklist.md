# Milestone 37 Handmatige Testchecklist

Gebruik uitsluitend de actuele Debug-build uit `Builds\Debug\Saga.exe`. Deze slice wijzigt geen boekmetadata of database-opslag zolang je in het beheervenster geen herstelactie bevestigt.

## Eigen tabblad Kwaliteit

- [ ] Open Instellingen en controleer dat het tabblad `Kwaliteit` zichtbaar is.
- [ ] Open `Kwaliteit` en controleer dat de uitleg over genegeerde kwaliteitsmeldingen duidelijk is.
- [ ] Controleer dat `Genegeerde kwaliteitsmeldingen beheren` met de muis opent.
- [ ] Sluit het beheervenster zonder iets te herstellen en controleer dat er niets is gewijzigd.

## Herstellen en lege toestand

- [ ] Controleer dat eerder als correct gemarkeerde meldingen met boektitel, kwaliteitsmelding en datum zichtbaar zijn.
- [ ] Herstel één geselecteerde melding en controleer dat alleen die melding uit de beheerlijst verdwijnt.
- [ ] Open de Quality Page opnieuw en controleer dat de herstelde melding terugkomt wanneer de kwaliteitsregel nog steeds geldt.
- [ ] Controleer, indien praktisch, dat een lege beheerlijst een duidelijke lege toestand toont.
- [ ] Gebruik `Alles herstellen` alleen wanneer dit voor de testbibliotheek veilig is en controleer de bevestiging voordat gegevens veranderen.

## Duplicaten en toegankelijkheid

- [ ] Open het tabblad `Duplicaten` en controleer dat daar geen kwaliteitsbeheer meer staat.
- [ ] Controleer dat instellingen voor exacte treffers, duplicate-uitzonderingen en samenvoegstandaarden nog aanwezig zijn.
- [ ] Bereik het tabblad `Kwaliteit` en de beheerknop met het toetsenbord en activeer de knop met `Enter` of `Spatie`.
- [ ] Wissel steekproefsgewijs naar een andere ondersteunde taal en controleer dat de tabtitel en uitleg vertaald zijn.
- [ ] Maak Instellingen smaller en groter en controleer dat uitleg en knop bruikbaar blijven.

## Regressie en afronding

- [ ] Open de Quality Page en controleer dat de bestaande herstelacties en `Dit is correct` nog beschikbaar zijn.
- [ ] Controleer dat Instellingen normaal kan worden opgeslagen en geannuleerd.
- [ ] Sluit en heropen Saga en controleer dat het tabblad `Kwaliteit` aanwezig blijft.
- [ ] Noteer eventuele afwijkingen voordat PR #38 wordt gemerged.
