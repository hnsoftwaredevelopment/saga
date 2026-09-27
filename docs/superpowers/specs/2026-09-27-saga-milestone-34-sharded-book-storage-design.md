# Saga Milestone 34: Gespreide en hervatbare boekenopslag

## Aanleiding

De actieve bibliotheek bevat ongeveer 34.483 boekmappen rechtstreeks onder `books`. OneDrive meldt daardoor dat deze ene map te veel onderdelen bevat. Een indeling per auteur verlaagt dit aantal, maar koppelt de fysieke opslag aan veranderlijke metadata en levert problemen op bij meerdere auteurs, ontbrekende auteurs, gelijke namen en ongeldige padtekens.

## Aannames

- Een opslag-ID is de stabiele sleutel voor fysieke opslag; normaal is dit het oorspronkelijke boek-ID. Na een duplicaatsamenvoeging kan een bestand bewust zijn oorspronkelijke opslag-ID behouden. Auteur, titel en andere metadata bepalen nooit het pad.
- De nieuwe indeling gebruikt de eerste twee hexadecimale tekens van de opslag-ID als vaste tussenmap.
- Nieuwe imports gebruiken de nieuwe indeling onmiddellijk.
- Bestaande bibliotheken worden bij de eerste opening na de update automatisch en hervatbaar gemigreerd voordat normaal gebruik mogelijk is.
- De migratie verplaatst bestanden op dezelfde schijf en maakt geen tweede kopie van ieder ebook.
- De database krijgt vóór de eerste verplaatsing één blijvende veiligheidskopie.
- De wijziging verlaagt het aantal directe onderdelen onder `books`, maar niet het totale aantal door OneDrive gesynchroniseerde bestanden en mappen.
- De eerder gemelde stroperigheid van het hoofdgrid en titelfilter wordt na deze opslagwijziging afzonderlijk gemeten en geoptimaliseerd; deze milestone schrijft geen onbewezen oorzaak toe aan de mapindeling.

De richting van gespreide opslag op basis van het ID is functioneel goedgekeurd op 27 september 2026. De precieze migratie- en foutafhandeling in dit document vormt het controlepunt vóór implementatie.

## Doel

Saga bewaart beheerde boekbestanden en omslagen in een stabiele, breed verdeelde mappenstructuur. De bestaande bibliotheek kan veilig worden omgezet zonder metadata te wijzigen, zonder verlies bij een onderbreking en zonder later bestanden te hoeven verplaatsen wanneer auteur of titel verandert.

## Nieuwe opslagindeling

Een opslag-ID wordt in kleine letters zonder streepjes geschreven. De eerste twee tekens vormen de shard:

```text
books/
  00/
    00a1.../
      boek.epub
      cover.jpg
  7f/
    7f3a.../
      boek.pdf
  ff/
    ffe2.../
      boek.epub
```

Het relatieve pad van een ebook wordt daarmee:

```text
books/<eerste-2-tekens>/<opslag-id>/<bestandsnaam>
```

Er zijn maximaal 256 tussenmappen (`00` tot en met `ff`). Bij de huidige omvang bevat een shard gemiddeld ongeveer 135 boekmappen. Lege shardmappen hoeven niet vooraf te worden aangemaakt en mogen na verwijdering blijven bestaan; het opruimen daarvan is niet nodig voor correctheid.

## Gebruikersverloop bij een bestaande bibliotheek

1. Saga opent de bibliotheekdatabase en maakt, als die nog niet bestaat, een veiligheidskopie vóór de opslagmigratie.
2. Saga herkent rechtstreeks onder `books` alleen mappen waarvan de volledige naam een geldige opslag-ID is die vanuit een boek, ebookpad of omslagpad wordt gerefereerd.
3. Wanneer zulke oude boekmappen aanwezig zijn, toont het startscherm `Bibliotheekopslag optimaliseren` met het aantal verwerkte en resterende opslagmappen.
4. Saga verwerkt steeds één opslagmap volledig. Tijdens deze fase kunnen importeren, bewerken en verwijderen niet parallel starten.
5. Na iedere geslaagde mapverplaatsing worden de relatieve ebook- en omslagpaden van dat boek in één databasetransactie bijgewerkt.
6. Als Saga of Windows tussentijds stopt, herkent de volgende start zowel de oude als de reeds verplaatste toestand en gaat verder waar dat veilig kan.
7. Na voltooiing opent de bibliotheek normaal. Latere starts doen alleen een snelle controle en voeren geen migratie meer uit.

De gebruiker hoeft geen doelmap of naamgevingskeuze te maken. Een fout toont een begrijpelijke melding met het betreffende boek of pad en een mogelijkheid om na herstel opnieuw te proberen. Saga gaat bij twijfel niet verder met de conflicterende map.

## Hervatbaarheid en consistente toestanden

De migratie behandelt de bestandssysteemverplaatsing en databasewijziging bewust als twee controleerbare stappen:

- **Oud aanwezig, nieuw afwezig, oude databasepaden:** normale beginsituatie; map verplaatsen en daarna databasepaden bijwerken.
- **Oud afwezig, nieuw aanwezig, oude databasepaden:** vermoedelijke onderbreking na de mapverplaatsing; inhoud en bestemming controleren en alleen de databasepaden herstellen.
- **Oud afwezig, nieuw aanwezig, nieuwe databasepaden:** boek is gereed en wordt overgeslagen.
- **Oud en nieuw beide aanwezig:** conflict; niets samenvoegen, overschrijven of verwijderen en de migratie gecontroleerd stoppen.
- **Beide afwezig terwijl de database bestanden verwacht:** ontbrekende opslag; niets wijzigen en een gerichte fout tonen.

Annulering of afsluiten wordt alleen tussen twee boeken verwerkt, niet midden in een mapverplaatsing of databasetransactie. Onbekende mappen en bestanden direct onder `books` worden nooit automatisch verwijderd of hernoemd.

## Architectuur

### Centrale padindeling

Eén infrastructuurcomponent wordt de enige bron voor:

- het normaliseren van een opslag-ID;
- het bepalen van de shard;
- oude en nieuwe boekmappen;
- relatieve ebook- en omslagpaden;
- controle dat ieder pad binnen de actieve bibliotheek blijft.

Zowel de bestaande bestandsopslag als omslagopslag gebruikt deze component. Daarmee kunnen import en omslagwijziging niet ongemerkt verschillende mappenstructuren gebruiken.

### Migratieservice

Een afzonderlijke applicatieservice coördineert de eenmalige opslagmigratie. De infrastructuur levert het gecontroleerd inventariseren en verplaatsen; een repository werkt uitsluitend de paden van het bijbehorende boek bij. De UI ontvangt voortgang en een expliciet resultaat: niets nodig, voltooid, hervatbaar onderbroken of geblokkeerd door een conflict.

De bestaande relatieve paden in SQLite blijven leidend voor het openen van bestanden. Er is geen wijziging aan ebookinhoud, auteurs, titels, sidecars of leesstatus nodig.

### Databaseback-up

Vóór de eerste verplaatsing maakt Saga via SQLite een consistente back-up in een herkenbare map onder de bibliotheekroot. Een bestaande back-up voor deze migratie wordt behouden en niet telkens overschreven. Een afgebroken tijdelijke back-up wordt bij een volgende start gecontroleerd vervangen. De back-up is een extra herstelmiddel; de normale hervatting is gebaseerd op de feitelijke oude/nieuwe map en de opgeslagen relatieve paden. Bij de huidige bibliotheek is `library.db` ongeveer 5,98 GB; de eerste onbepaalde voortgangsfase en benodigde vrije ruimte worden daarom expliciet in de checklist genoemd.

## Veiligheidsgrenzen

- Alleen canonieke paden binnen de actieve bibliotheek worden geaccepteerd.
- Reparse points, symbolische koppelingen en junctions in een te verplaatsen boekpad worden geweigerd.
- Alleen een exacte mapnaam die als niet-lege opslag-ID kan worden gelezen en door de database wordt gerefereerd, komt voor automatische migratie in aanmerking.
- Een bestaande doelmap wordt nooit blind overschreven of samengevoegd.
- Bronmappen worden nooit verwijderd als losse opruimstap; de verplaatsing zelf is de enige normale bronwijziging.
- Databasepaden worden alleen vervangen wanneer ze exact onder de verwachte oude boekmap vallen.
- Bestandsnamen blijven ongewijzigd.
- Fouten van OneDrive, vergrendelde bestanden, ontbrekende rechten en te lange paden worden opgevangen en zichtbaar gemaakt.

## Teststrategie

- Eenheidstests voor alle shardgrenzen, GUID-notatie, relatieve paden en padinsluiting.
- Opslagtests bewijzen dat nieuwe ebooks en omslagen dezelfde nieuwe boekmap gebruiken.
- Migratietests dekken alle vijf toestanden uit de hervatbaarheidstabel.
- Tests simuleren een onderbreking na verplaatsing maar vóór database-update en bewijzen succesvol hervatten.
- Tests bewijzen dat conflicten, onbekende mappen, reparse points en paden buiten de bibliotheek niets wijzigen.
- Repositorytests controleren dat ebook- en omslagpaden samen worden bijgewerkt en overige metadata gelijk blijft.
- Viewmodeltests controleren voortgang, blokkering van normaal gebruik en begrijpelijke foutmeldingen.
- Regressietests controleren importeren, omslag wijzigen, openen, exporteren en verwijderen.
- De volledige testsuite en Debug-build moeten zonder waarschuwingen slagen.

## Handmatige controle

De handmatige checklist gebruikt eerst een kleine kopiebibliotheek met oude mappen en daarna, na een extra back-upcontrole, de echte bibliotheek:

- voortgang en herstart tijdens de migratie;
- openen van meerdere formaten en omslagen na migratie;
- import van een nieuw boek in de juiste shard;
- auteur wijzigen zonder bestandsverplaatsing;
- omslag wijzigen en boek verwijderen;
- directe aantallen onder `books` vergelijken voor en na migratie;
- OneDrive laten uit synchroniseren en controleren op meldingen.

De echte bibliotheek wordt niet door geautomatiseerde tests aangepast.

## Leesbare preflight op de echte bibliotheek

Op 27 september 2026 is de gesloten bibliotheek uitsluitend leesbaar gecontroleerd:

- 34.447 boeken en 34.643 ebookbestanden in SQLite;
- 34.460 unieke, door ebook- of omslagpaden gerefereerde opslag-ID's;
- geen gerefereerde opslagmap en geen gerefereerd bestand ontbreekt;
- 13 ebookpaden en 2 omslagpaden gebruiken na eerdere duplicaatsamenvoegingen nog een oorspronkelijke opslag-ID; deze worden op opslag-ID gemigreerd;
- 23 GUID-mappen worden niet door de database gerefereerd en blijven daarom bewust onaangeroerd;
- geen oude en nieuwe opslaglocatie bestaat tegelijk.

## Acceptatiecriteria

- Nieuwe boekmappen staan onder `books/<shard>/<opslag-id>` en ebook en omslag blijven bijeen.
- Auteur- en titelwijzigingen veranderen het opslagpad niet.
- Een bestaande bibliotheek migreert met zichtbare voortgang en blokkeert gelijktijdige mutaties.
- Een onderbreking kan zonder dupliceren, overschrijven of dataverlies worden hervat.
- Conflicten stoppen veilig en noemen het betreffende pad.
- De databaseback-up bestaat vóór de eerste verplaatsing en wordt niet automatisch verwijderd.
- Oude relatieve ebook- en omslagpaden worden correct bijgewerkt; overige boekgegevens blijven gelijk.
- Importeren, openen, exporteren, omslag wijzigen en verwijderen werken na de migratie.
- Onder `books` blijven na succesvolle migratie hoofdzakelijk maximaal 256 shardmappen over in plaats van tienduizenden boekmappen.
- Alle geautomatiseerde tests en de Debug-build slagen zonder waarschuwingen.

## Buiten scope en vervolg

- Een auteursgebaseerde mappenstructuur.
- Het totale aantal door OneDrive gesynchroniseerde onderdelen verlagen door bestanden te verwijderen of extern te archiveren.
- Bestanden hernoemen op basis van titel of auteur.
- De inhoud van EPUB-, PDF- of andere ebookbestanden wijzigen.
- Automatisch onbekende of verweesde mappen verwijderen.
- De eerder gemelde schokkerige boekenlijst en trage titelfilter oplossen; hiervoor volgt na deze opslagmigratie een afzonderlijke, gemeten performance-slice.
- De Quality Page-slice `Rommelige tags`; die kan na de veilige opslagbasis worden hervat.

## Open vragen

Geen blokkerende vragen. De voorgestelde automatische migratie bij de eerste start, met zichtbare voortgang en hervatting na een onderbreking, wordt samen met deze specificatie ter goedkeuring voorgelegd.
