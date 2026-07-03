# Feature Request: Samenvoegen Van Duplicate Boeken

## Samenvatting

Voeg een veilige workflow toe waarmee een gebruiker twee of meer duplicate boeken kan samenvoegen tot een enkel bibliotheekrecord.

De huidige duplicate finder helpt al bij het vinden, vergelijken en verwijderen van dubbele boeken. Samenvoegen is de volgende stap, maar moet bewust en controleerbaar gebeuren omdat metadata of boekbestanden anders onbedoeld verloren kunnen gaan.

## Gewenst Gedrag

- De gebruiker opent de duplicate finder.
- De gebruiker selecteert een groep duplicate boeken.
- De gebruiker kiest `Samenvoegen`.
- Saga toont een merge-scherm waarin de gebruiker:
  - een basisboek kiest waarvan het boekbestand en de bestaande metadata als uitgangspunt worden gebruikt;
  - een of meer aanvullende boeken kiest waarvan metadata kan worden overgenomen;
  - per veld ziet welke waarden verschillen;
  - per veld kan kiezen welke waarde behouden wordt;
  - kan aangeven welke velden nooit automatisch overschreven mogen worden.
- Na bevestiging wordt een enkel bibliotheekrecord bewaard en worden de gekozen metadatawaarden opgeslagen.
- Het resultaat wordt ook naar de portable `metadata.json` sidecar geschreven.

## Velden

De eerste versie moet in ieder geval deze velden tonen:

- titel
- auteurs
- serie
- serienummer
- tags
- taal
- uitgever
- publicatiedatum
- ISBN
- omschrijving
- boekformaat of gekoppelde bestanden

## Veiligheidsregels

- Samenvoegen mag nooit automatisch bronbestanden verwijderen zonder expliciete bevestiging.
- Het basisboek blijft standaard behouden.
- Als er meerdere bestandsformaten beschikbaar zijn, bijvoorbeeld EPUB en PDF, moet Saga deze bij voorkeur aan hetzelfde boekrecord kunnen koppelen in plaats van ze stilzwijgend te verwijderen.
- Bij twijfel moet Saga de gebruiker laten kiezen.
- De workflow moet annuleerbaar zijn zonder wijzigingen.
- Na samenvoegen moet de duplicate finder worden ververst.

## Standaardkeuzes Voor Later

De keuzes die de gebruiker maakt, kunnen later als standaardinstellingen worden opgeslagen. Voorbeelden:

- omschrijving nooit automatisch overschrijven;
- tags samenvoegen in plaats van vervangen;
- langste omschrijving voorstellen;
- hoogste resolutie cover voorstellen;
- EPUB als voorkeursformaat boven PDF gebruiken;
- metadata uit Saga `metadata.json` hoger waarderen dan metadata uit Calibre `metadata.opf`.

Deze instellingen hoeven niet in de eerste versie van de merge-workflow te zitten, maar het ontwerp moet er wel rekening mee houden.

## Acceptatiecriteria

- De gebruiker kan vanuit de duplicate finder een merge-workflow starten.
- Verschillende metadatawaarden zijn duidelijk zichtbaar voordat er iets wordt opgeslagen.
- De gebruiker kiest expliciet welk boek het basisboek is.
- De gebruiker kiest expliciet welke afwijkende metadatawaarden worden overgenomen.
- De merge kan worden geannuleerd zonder dat de bibliotheek wijzigt.
- Na bevestigen wordt SQLite bijgewerkt.
- Na bevestigen wordt `metadata.json` bijgewerkt.
- De duplicate finder toont de samengevoegde groep niet meer als duplicate wanneer er nog maar een relevant boekrecord over is.
- De hoofdweergave wordt pas ververst wanneer de duplicate finder wordt gesloten of wanneer de gebruiker expliciet vernieuwt.

## Status

Uitgesteld. Dit is bewust geen onderdeel van de eerste duplicate finder-iteratie, omdat verwijderen al destructief genoeg is en samenvoegen een aparte veilige ontwerpstap verdient.
