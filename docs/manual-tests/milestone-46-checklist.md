# Milestone 46: resultaatdetails bij kwaliteitsherstel

Testbuild: `Builds\Debug\Saga.exe`, versie `2026.10.22.56`, gebouwd op 22 oktober 2026 om 13:25 (Europe/Amsterdam). De implementatie is gereed voor handmatige controle; onderstaande controles zijn nog niet door de gebruiker uitgevoerd. Automatisch: 882 tests geslaagd, 0 mislukt of overgeslagen; Debug-build met 0 waarschuwingen en fouten.

## Voorbereiding

- [ ] Noteer versie en bouwtijd van de nieuwe Builds/Debug/Saga.exe; test niet met de build van Milestone 45.
- [ ] Gebruik een testbibliotheek of back-up voor conflict- en foutscenario's. Forceer geen riskante bestands- of databasefouten in de echte bibliotheek.

## Herstelroutes

Voer een toepasselijke herstelactie uit voor auteur, taal, serie, titel/auteur, tags en omslag. Eén boek en bestaande meervoudige selectie vallen beide onder de controle; omslag blijft enkelvoudig.

- [ ] Volledig geslaagde acties behouden de huidige rustige terugkoppeling; bulk toont een korte groene succesmelding zonder 0 fouten.
- [ ] Fouten hebben naast de samenvatting bekijkbare details met het betrokken boek en een begrijpelijke reden. Geldige successen worden niet als fout opgesomd.
- [ ] Open/sluit details met toetsenbord en muis; geen extra opslag of wijziging. Langere teksten blijven leesbaar en actiebalk/boekenlijst blijven bruikbaar.

## Conflict en gemengde resultaten

- [ ] Maak een correct boek en een echt omgewisseld exemplaar dat na herstel hetzelfde boek zou opleveren. De omwisseling wordt geblokkeerd en vermeldt Boek bestaat al met deze titel en auteur.
- [ ] Bij een bulkselectie met één conflict worden andere geldige boeken wel gewijzigd. Aantallen kloppen en de mislukte, nog bestaande regel blijft geselecteerd.
- [ ] Het correct aanwezige boek blijft ongewijzigd. Overige metadata, bestanden, bibliotheek en filters blijven correct.
- [ ] Indien veilig te reproduceren: bij gedeeltelijke bestandsbijwerking staat duidelijk dat de bibliotheekwijziging wel is opgeslagen.
- [ ] Indien veilig te reproduceren: een verdwenen of inmiddels gewijzigd boek krijgt een passende reden; geen overschrijving van nieuwere metadata.

## Levensduur en regressies

- [ ] Een volgende bevestigde actie vervangt eerdere details; na categoriewissel worden oude fouten niet aan de nieuwe categorie toegeschreven.
- [ ] Annuleren vóór bevestiging schrijft niets en presenteert geen nieuwe fout.
- [ ] Herhaal een controle in een andere taal: geen onvertaalde sleutels, ruwe exceptionteksten, SQL of volledige lokale paden.
- [ ] De bestaande bevestigingen, directe verversing en bescherming tegen dubbele boeken blijven werken.

Niet veilig reproduceerbare foutpaden worden automatisch getest en hier expliciet als niet handmatig uitgevoerd genoteerd. Noteer afwijkingen en accepteer de slice pas na het testen van de actuele build.
