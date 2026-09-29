# Milestone 34 Implementatieplan: Gespreide boekenopslag

## Overzicht

Milestone 34 verdeelt boekmappen over 256 ID-shards, laat alle opslagroutes één centrale padindeling gebruiken en migreert de bestaande bibliotheek veilig en hervatbaar tijdens de eerste start na de update.

## Afhankelijkheden

```text
Centrale opslagindeling
        │
        ├── nieuwe imports en omslagen
        │
        └── inventarisatie en veilige mapverplaatsing
                    │
                    └── transactionele databasepad-update
                                │
                                └── opstartvoortgang en foutafhandeling
                                            │
                                            └── regressie, checklist en Debug-build
```

## Taak 1: Centrale ID-shardindeling

**Beschrijving:** Introduceer één veilige component die oude en nieuwe boekmappen en relatieve paden uit een boek-ID afleidt.

**Acceptatiecriteria:**

- [x] Een ID wordt als 32 kleine hexadecimale tekens geschreven en de eerste twee vormen de shard.
- [x] Alle berekende paden blijven binnen de actieve bibliotheek.
- [x] Ongeldige ID's, ontsnappende paden en reparse points worden geweigerd.

**Verificatie:** Eerst falende tests voor shardgrenzen en padbeveiliging; daarna gerichte tests groen.

## Taak 2: Nieuwe opslagroutes omschakelen

**Beschrijving:** Laat ebookimport, omslagopslag en verwijderen dezelfde centrale indeling gebruiken.

**Acceptatiecriteria:**

- [x] Nieuwe ebooks en `cover.jpg` komen samen onder `books/<shard>/<boek-id>`.
- [x] Tijdelijke importbestanden staan niet als duizenden directe onderdelen onder `books`.
- [x] Openen en exporteren blijven werken via de opgeslagen relatieve paden.
- [x] Verwijderen raakt uitsluitend de exacte oude of nieuwe map van het gevraagde boek.

**Verificatie:** Testgedreven opslag- en regressietests.

## Checkpoint 1

Controleer na taken 1 en 2 de architectuur, padbeveiliging en regressies. Nieuwe imports moeten de nieuwe indeling gebruiken voordat migratiecode wordt toegevoegd.

Afgerond: 678 tests en de volledige Debug-build slagen zonder waarschuwingen.

## Taak 3: Hervatbare mapmigratie en databaseback-up

**Beschrijving:** Inventariseer oude GUID-mappen, maak eenmaal een consistente SQLite-back-up en verplaats elk boek afzonderlijk met herstel van een onderbroken tussenstand.

**Acceptatiecriteria:**

- [x] Normale, reeds voltooide en na verplaatsing onderbroken toestanden worden correct afgehandeld.
- [x] Bestaande bron én bestemming, ontbrekende opslag en afwijkende databasepaden stoppen zonder wijziging.
- [x] Ebook- en omslagpaden worden per opslag-ID samen in één databasetransactie bijgewerkt.
- [x] Onbekende mappen, bestanden en reparse points blijven onaangeroerd.

**Verificatie:** Eerst falende scenariotests voor iedere toestand, inclusief geforceerde onderbreking; daarna integratietests groen.

## Taak 4: Opstartintegratie en voortgang

**Beschrijving:** Voer de migratie vóór normaal bibliotheekgebruik uit en toon gelokaliseerde voortgang en herstelbare fouten op het bestaande startscherm.

**Acceptatiecriteria:**

- [x] De gebruiker ziet `Bibliotheekopslag optimaliseren` en verwerkte/totale aantallen.
- [x] Importeren, bewerken en verwijderen kunnen niet gelijktijdig starten.
- [x] Afsluiten wordt tussen opslagmappen verwerkt en de volgende start hervat veilig.
- [x] Een fout noemt het pad en een volgende start kan opnieuw proberen zonder technische stacktrace.

**Verificatie:** Viewmodeltests plus een kleine tijdelijke testbibliotheek.

## Checkpoint 2

Controleer de volledige migratieketen op databehoud, hervatbaarheid, OneDrive-fouten en begrijpelijke gebruikersfeedback.

Afgerond: alle geautomatiseerde scenario's zijn groen en een leesbare preflight op de echte bibliotheek vond geen ontbrekende gerefereerde mappen of bestanden. Samengevoegde boeken met een afwijkende oorspronkelijke opslag-ID worden ondersteund; onverwezen mappen blijven staan.

## Taak 5: Regressie, documentatie en testbuild

**Beschrijving:** Rond regressietests, zes vertalingen, handmatige checklist, documentatie, zelfreview en de actuele Debug-build af.

**Acceptatiecriteria:**

- [x] Importeren, openen, exporteren, omslag wijzigen, auteur wijzigen en verwijderen zijn geautomatiseerd getest; praktijktest staat in de checklist.
- [x] De checklist bevat veilige voorbereiding, de echte bibliotheek en hervatbaarheid die geautomatiseerd met tijdelijke bibliotheken is bewezen.
- [x] Alle gewijzigde Markdown is exact naar Obsidian gespiegeld.
- [x] Alleen de actuele applicatiebuild staat in `Builds/Debug`.
- [x] De volledige testsuite met 697 tests en build slagen zonder waarschuwingen.

**Verificatie:** Volledige geautomatiseerde suite, Debug-build, zelfreview en handmatige checklist.

Praktijkacceptatie: op 29 september 2026 is de echte bibliotheek succesvol gemigreerd van 34.483 directe boekmappen naar 256 shardmappen. De eenmalige migratie duurde enkele uren, maar alle checklistcontroles slaagden. De 23 onverwezen lege mappen zijn handmatig verwijderd en OneDrive synchroniseert weer normaal.

## Daarna

Na deze milestone meten we afzonderlijk het scrollen, Page Up/Page Down en filteren op titel. Daarna kan de Quality Page-slice `Rommelige tags` worden hervat.
