# Milestone 40 handmatige checklist

Gebruik uitsluitend de actuele Debug-build uit `Builds\Debug\Saga.exe`. Maak voor deze controle bij voorkeur een kleine testbibliotheek met twee boeken die Saga als duplicaten herkent en die ieder minstens één eigen boekbestand hebben.

## Lokale schermaanpassingen

- [ ] Selecteer een boek en controleer dat de acties Opslaan, Ongedaan maken en Verwijderen als duidelijke icoonknoppen worden getoond.
- [ ] Open Instellingen en controleer dat de toelichting bij de boekenplankinstelling leesbaar blijft in het actieve thema.

## Annuleren

- [ ] Open het duplicatenoverzicht en start Samenvoegen voor een duplicaatpaar.
- [ ] Annuleer het voorbeeldvenster en controleer dat beide boeken, hun metadata en hun bestanden ongewijzigd blijven.

## Samenvoegen en metadata

- [ ] Start Samenvoegen opnieuw en kies bewust het doelboek.
- [ ] Laat minimaal één afwijkend veld van de bron kopiëren of samenvoegen en bevestig de merge.
- [ ] Controleer dat het bronboek uit het duplicatenoverzicht verdwijnt en het doelboek behouden blijft.
- [ ] Sluit het duplicatenoverzicht en controleer dat de hoofdbibliotheek eenmaal wordt ververst en alleen het samengevoegde doelboek toont.
- [ ] Controleer dat alle gekoppelde formaten bij het doelboek beschikbaar zijn en geopend kunnen worden.
- [ ] Controleer dat de gekozen samengevoegde metadata in het detailpaneel zichtbaar is.

## Sidecars op schijf

- [ ] Open via de formaatregels de mappen van minstens twee gekoppelde bestanden die vóór de merge bij verschillende boeken hoorden.
- [ ] Controleer dat in iedere map een `metadata.json` staat.
- [ ] Open beide sidecars en controleer dat titel, auteurs en het bewust gewijzigde veld overeenkomen met het samengevoegde doelboek.
- [ ] Sluit Saga, open de bibliotheek opnieuw en controleer dat het samengevoegde boek en alle formaten behouden blijven.

## Foutpad en regressie

- [ ] Controleer dat normale merges geen waarschuwing tonen. Het zeldzame sidecarfoutpad wordt geautomatiseerd getest en hoeft niet handmatig met maprechten of bestandssabotage te worden afgedwongen.
- [ ] Controleer kort dat `Geen duplicaat`, verwijderen uit het duplicatenoverzicht en de opgeslagen standaardkeuzes voor mergevelden nog normaal werken.
