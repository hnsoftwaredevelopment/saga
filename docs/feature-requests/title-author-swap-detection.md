# Titel/auteur herkennen: praktijkvoorbeelden en vervolgrichting

## Gebruikersbevinding

De gebruiker heeft de bulkherstelactie van Milestone 45 volledig getest en geaccepteerd. De oude detectie levert volgens de gebruiker ruim 95% onterechte meldingen op. De voorbeelden hieronder zijn aangeleverd als correct ingevulde boeken, met één bevestigde echte omwisseling. Op verzoek wordt de detectieverbetering alsnog in dezelfde slice meegenomen.

## Oorzaak in de oorspronkelijke code

De oorspronkelijke naamherkenning accepteerde uitsluitend twee tot vier woorden, ieder met een hoofdletter en minstens één kleine letter. Daardoor vielen initialen, tussenvoegsels, eenwoordige pseudoniemen en langere namen buiten de herkenning. Titels met hoofdletters werden juist vaak als naam gezien.

## Praktijkgevallen voor regressietests

| Titel | Auteur |
| --- | --- |
| De Wandelaar | Vreemdgaan |
| De Zes Zwijgers | Seamark |
| Dokter Deen | Edwin de Vries |
| Dokter Finley Assistent | A.J. Cronin |
| Dolfje Sneeuwwolfje | Paul van Loon |
| Dolfje Weerwolfje: Boze Drieling | Paul van Loon |
| Dolle Dinsdag | Nicolline van der Spek |
| Don Abbondio | A.L.G. Bosboom-Toussaint |
| Dood Henry Kissinger | Gérard de Villiers |
| Dood Joesjtsjenko! | Gérard de Villiers |
| Dood Van Een Kater | Arthur C. Clarke |
| Dossier Blaauw | J.A. Blaauw |
| Dossier Gifmoord | Ed van Eeden |
| Dossier Karadzic | Gérard de Villiers |
| Dossier Kinderroof | Ed van Eeden |
| Dossier Tobias | Annet de Jong |
| Dossier Vrouwenhandel | Ed van Eeden |
| Dossier Zwarte Lotto | Ed van Eeden |
| Dubbele Agenda | Saskia M.N. Oudshoorn |
| Duistere Wraak | Constance M. Burge |
| Ebony Eyes | Vorser |
| Echt Sanne | Marjan van den Berg |
| Edelman, Bedelman, Schutter, Spion | John le Carré |
| Een Demonische Grappenmaker | Constance M. Burge |
| Een Duistere Erfenis | Constance M. Burge |
| Een Festival Vol Verwikkelingen | Peter de Zwaan |
| Een Godsoordeel | Mr. J. van Lennep |
| Een Nachtmerrie | J.H. Carroll |
| Een Oase Van Verhalen | Adriaan van Garde |
| Een Oude Kennis | hertaald door Hans Keuning Hilbrand |
| Een Vermoeden Van Vrijheid | Henk de Velde |
| Een Wraakzuchtig Verleden | Constance M. Burge |

De gebruiker heeft bevestigd dat `Duenas, Maria` als titel en `De wereld vergeten` als auteur een juiste detectiemelding is. Gebruik dit als positieve regressietest: deze omwisseling moet ook na het aanscherpen van de controle in de lijst blijven staan.

## Aangescherpte controle binnen Milestone 45

- Alleen één bruikbare auteur kan een omwisseling opleveren. Ontbrekende auteurs blijven onder hun eigen kwaliteitsmelding vallen.
- Een naamachtige titel is niet voldoende. Het auteursveld moet zelfstandig op een boektitel wijzen: minstens drie woorden, beginnend met een herkend lidwoord uit de zes Saga-talen en minstens twee kleineletterwoorden die geen naam-tussenvoegsel zijn.
- Naamherkenning ondersteunt initialen, tussenvoegsels, samengestelde namen, accenten, één komma voor achternaam/voornaam en maximaal acht woorden. Een auteursveld dat als naam wordt herkend is geen omwisselsignaal.
- Eenwoordige auteurs, bewerkersvermeldingen en onvoldoende duidelijke gevallen worden niet meer op basis van alleen hoofdlettergebruik gemeld. De 32 aangeleverde correcte voorbeelden staan als negatieve regressietests vast; het bevestigde Duenas-geval en diverse naamvormen als positieve tests.
- Deze controle is bewust voorzichtig, niet een woordenboek of bewijs dat metadata correct is. Korte boektitels, titels zonder herkend beginlidwoord en titels volledig in titelhoofdletters kunnen worden gemist. Niet alle naamvormen of talen worden herkend.
- Geen netwerkverkeer, bibliotheekbrede zoekopdrachten of metadatawijzigingen bij het controleren. Bestaande uitsluitingen blijven geldig. De detailactie blijft beschikbaar voor handmatig gevonden gevallen.

Bibliotheekcontext en verdere taalherkenning zijn mogelijke vervolgstappen, maar herhaling is nooit zelfstandig bewijs: fout geïmporteerde waarden kunnen ook herhaald zijn. De gebruiker heeft de aanvullende handmatige tests uitgevoerd en geaccepteerd: de lijst is veel korter en realistischer. Een omwisseling naar een al bestaand boek werd terecht geblokkeerd. De melding noemt alleen `Mislukt: 1`; het begrijpelijk tonen van de oorzaak `Boek bestaat al` is een apart verbeterpunt.
