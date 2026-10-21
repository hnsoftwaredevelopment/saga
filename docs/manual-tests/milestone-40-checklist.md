# Milestone 40 handmatige checklist

Gebruik uitsluitend de actuele Debug-build uit `Builds\Debug\Saga.exe`. Maak voor deze controle bij voorkeur een kleine testbibliotheek met twee boeken die Saga als duplicaten herkent en die ieder minstens één eigen boekbestand hebben.

## Lokale schermaanpassingen

- [X] Selecteer een boek en controleer dat de acties Opslaan, Ongedaan maken en Verwijderen als duidelijke icoonknoppen worden getoond.
- [X] Open Instellingen en controleer dat de toelichting bij de boekenplankinstelling leesbaar blijft in het actieve thema.

## Annuleren

- [X] Open het duplicatenoverzicht en selecteer één boek. Controleer dat de knop **Samenvoegen** zichtbaar maar nog uitgeschakeld is.
- [X] Houd Ctrl ingedrukt, selecteer een tweede boek uit dezelfde duplicaatgroep en controleer dat **Samenvoegen** actief wordt.
- [X] Klik op **Samenvoegen** en controleer dat het voorbeeldvenster zonder lange blokkade opent.
- [X] Annuleer het voorbeeldvenster en controleer dat beide boeken, hun metadata en hun bestanden ongewijzigd blijven.

## Responsiviteit

- [X] Selecteer en deselecteer enkele boeken en controleer dat het scherm direct blijft reageren.
- [X] Schakel **Alleen exacte matches** uit en controleer dat de uitgebreidere lijst binnen een werkbare tijd verschijnt en Saga bedienbaar blijft.
- [X] Schakel **Alleen exacte matches** opnieuw in en controleer dat de exacte lijst weer verschijnt.

## Samenvoegen en metadata

- [X] Start Samenvoegen opnieuw en kies bewust het doelboek.
- [X] Laat minimaal één afwijkend veld van de bron kopiëren of samenvoegen en bevestig de merge.
- [X] Controleer dat het bronboek uit het duplicatenoverzicht verdwijnt en het doelboek behouden blijft.
- [X] Sluit het duplicatenoverzicht en controleer dat de hoofdbibliotheek eenmaal wordt ververst en alleen het samengevoegde doelboek toont.
- [X] Controleer dat alle gekoppelde formaten bij het doelboek beschikbaar zijn en geopend kunnen worden.
- [X] Controleer dat de gekozen samengevoegde metadata in het detailpaneel zichtbaar is.

## Sidecars op schijf

- [X] Open via de formaatregels de mappen van minstens twee gekoppelde bestanden die vóór de merge bij verschillende boeken hoorden.
- [X] Controleer dat in iedere map een `metadata.json` staat.
- [X] Open beide sidecars en controleer dat titel, auteurs en het bewust gewijzigde veld overeenkomen met het samengevoegde doelboek.
- [X] Sluit Saga, open de bibliotheek opnieuw en controleer dat het samengevoegde boek en alle formaten behouden blijven.

## Foutpad en regressie

- [X] Controleer dat normale merges geen waarschuwing tonen. Het zeldzame sidecarfoutpad wordt geautomatiseerd getest en hoeft niet handmatig met maprechten of bestandssabotage te worden afgedwongen.
- [X] Controleer kort dat `Geen duplicaat`, verwijderen uit het duplicatenoverzicht en de opgeslagen standaardkeuzes voor mergevelden nog normaal werken.
