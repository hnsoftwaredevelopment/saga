# Saga Milestone 27: Navigatie Vanuit Metadata Quality Dashboard

## Doel

Maak van het metadata quality dashboard een bruikbaar startpunt voor opschoonwerk. De gebruiker kan een gevonden boek openen in de bestaande bibliotheekweergave, zonder handmatig opnieuw te zoeken.

## Gebruikersresultaat

- De gebruiker selecteert een boekregel in het dashboard.
- De actie `Openen in bibliotheek` wordt beschikbaar.
- Dubbelklikken op een boekregel voert dezelfde actie uit.
- Het dashboard sluit en Saga selecteert het gekozen boek in de hoofdweergave.
- De bestaande weergave, sortering, kolomindeling en groepering blijven behouden.
- Als zoektekst of actieve filters het boek verbergen, verwijdert Saga alleen de beperkende zoektekst of filtergroepen die nodig zijn om dit boek zichtbaar te maken.
- Bij een gegroepeerde weergave worden alleen de groepen op het pad naar het boek uitgeklapt.
- De actieve weergave scrollt het geselecteerde boek in beeld en de bestaande detailweergave toont het boek.

## Interactieontwerp

Het dashboard blijft modaal en read-only. Het krijgt één geselecteerde boekregel en een primaire knop onderaan:

- `Openen in bibliotheek`: sluit het dashboard met het geselecteerde boek-id als resultaat;
- `Sluiten`: sluit zonder navigatie;
- dubbelklik op een regel: gelijk aan `Openen in bibliotheek`;
- een verticale splitter maakt het linkerpaneel breder of smaller, met minimumruimte voor zowel de issuelijst als de boekentabel;
- geen selectie: de primaire actie is uitgeschakeld;
- wisselen van kwaliteitscategorie selecteert standaard de eerste boekregel van die categorie, of niets wanneer de categorie leeg is.

De knoptekst en tooltip worden toegevoegd aan alle ondersteunde talen. Voor niet-Nederlandse vertalingen mag de bestaande Engelse terugvaltekst worden gebruikt als er geen betrouwbare vertaling beschikbaar is.

## Navigatieregels

Saga zoekt het boek op basis van het stabiele boek-id, nooit op titel of auteur.

1. Als het boek al zichtbaar is, blijven zoektekst en filters ongewijzigd.
2. Als de algemene zoektekst het boek uitsluit, wordt alleen de algemene zoektekst geleegd. Zoektekst binnen filterlijsten beïnvloedt de bibliotheekresultaten niet en blijft daarom staan.
3. Voor iedere actieve standaard- of custom-metadata-filtergroep:
   - blijft de groep ongewijzigd als het boek aan minstens één geselecteerde waarde voldoet;
   - worden alleen de selecties uit die groep verwijderd als de groep het boek uitsluit.
4. De filters worden na het aanpassen één keer opnieuw toegepast, zodat grote bibliotheken niet herhaaldelijk worden opgebouwd.
5. De huidige `LibraryView`, geselecteerde gebruikersweergave, sortering, kolomindeling en groeperingskeuzes wijzigen niet.
6. In een gegroepeerde weergave worden de voorouders van het boek uitgeklapt; andere groepen behouden hun bestaande toestand.
7. Het boek wordt de enige actieve selectie en er wordt een eenmalig verzoek aan de zichtbare UI-weergave gestuurd om het in beeld te scrollen.

Als het boek sinds het openen van het dashboard is verwijderd, sluit het dashboard normaal en toont Saga een gelokaliseerde melding dat het boek niet meer bestaat. Zoek- en filterinstellingen blijven dan ongewijzigd.

## Architectuur

De dashboard-viewmodel bevat de selectie en levert alleen een boek-id op. De WPF-window bepaalt via `DialogResult` of de gebruiker daadwerkelijk wil navigeren. `IUserInteractionService` retourneert daarom `Guid?` voor het dashboard.

`LibraryViewModel` blijft verantwoordelijk voor bibliotheekstatus:

- het bepaalt welke zoektekst en filtergroepen het boek verbergen;
- het past de minimaal benodigde wijzigingen toe;
- het selecteert de zichtbare `BookRowViewModel`;
- het klapt het benodigde groeppad uit;
- het publiceert een eenmalig, niet-opgeslagen reveal-verzoek voor de actieve view.

De WPF views blijven verantwoordelijk voor platformafhankelijk scrollgedrag. De Bookshelf-, Detailed- en List-view reageren alleen wanneer zij zichtbaar zijn. De viewmodel hoeft daardoor geen WPF- of Syncfusion-typen te kennen.

## Fout- En Randgevallen

- Een lege kwaliteitscategorie heeft geen geselecteerd boek en geen actieve openactie.
- Annuleren of sluiten verandert de bibliotheekselectie, zoektekst en filters niet.
- Een boek dat in meerdere kwaliteitscategorieën voorkomt, navigeert steeds via hetzelfde id.
- Een actief filter dat het boek al toelaat blijft geselecteerd.
- Een gegroepeerd boek kan meerdere paden hebben, bijvoorbeeld door meerdere auteurs of tags. Saga klapt het eerste zichtbare pad volgens de bestaande groepsvolgorde uit.
- De reveal-notificatie is tijdelijk en wordt niet in instellingen opgeslagen.

## Buiten Scope

- metadata rechtstreeks in het dashboard aanpassen;
- bulkselectie of bulkreparaties vanuit het dashboard;
- een permanente kwaliteitsfilter in de hoofdbibliotheek;
- wijzigingen aan de kwaliteitssignalen of heuristieken;
- het resterende `metadata.json`-probleem van duplicate merge uit issue #1.

## Acceptatiecriteria

- Een geselecteerde dashboardregel kan via knop en dubbelklik in de bibliotheek worden geopend.
- Sluiten zonder openactie verandert niets aan de bibliotheekstatus.
- Een al zichtbaar boek wordt geselecteerd zonder zoektekst of filters te wijzigen.
- Een verborgen boek wordt zichtbaar door uitsluitend de blokkerende zoektekst en filtergroepen te verwijderen.
- Actieve view, gebruikersweergave, sortering, kolommen en groepering blijven behouden.
- Benodigde groepen worden uitgeklapt en de zichtbare view scrollt het boek in beeld.
- Een verwijderd boek veroorzaakt geen crash en geeft een begrijpelijke melding.
- Viewmodeltests dekken selectie, minimale filteraanpassing, groepering en het ontbrekende-boekscenario.
- Een handmatige checklist dekt knop, dubbelklik en alle drie bibliotheekweergaven.
- De breedte van het linkerpaneel kan met muis en toetsenbord worden aangepast zonder dat een van beide panelen onbruikbaar wordt.
