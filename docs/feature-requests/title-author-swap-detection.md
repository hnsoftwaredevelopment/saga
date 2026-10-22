# Titel/auteur herkennen: praktijkvoorbeelden en vervolgrichting

## Gebruikersbevinding

De gebruiker heeft Milestone 45 volledig getest en geaccepteerd. De herstelactie werkt; de detectie levert volgens de gebruiker ruim 95% onterechte meldingen op. De voorbeelden hieronder zijn aangeleverd als correct ingevulde boeken, met één afzonderlijk te beoordelen geval.

## Oorzaak in de huidige code

De huidige naamherkenning accepteert uitsluitend twee tot vier woorden, ieder met een hoofdletter en minstens één kleine letter. Daardoor vallen initialen, tussenvoegsels, eenwoordige pseudoniemen en langere namen buiten de herkenning. Titels met hoofdletters worden juist vaak als naam gezien.

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

## Voorgestelde vervolgslice

- Herken initialen, tussenvoegsels, achternaam-voornaamnotatie en afwijkend hoofdlettergebruik beter.
- Een titel die op een naam lijkt is op zichzelf onvoldoende aanwijzing.
- Een eenwoordige auteur of bewerkersvermelding is niet automatisch fout; geef bij onvoldoende aanwijzingen liever geen melding.
- Gebruik eventueel aanvullende bibliotheekcontext, maar behandel herhaling nooit als bewijs op zichzelf: fout geïmporteerde waarden kunnen ook herhaald zijn.
- Bewaak de aangeleverde correcte voorbeelden én echte omwisselingen. Meer precisie kan minder gevonden omwisselingen betekenen.
- De detailactie blijft beschikbaar voor handmatig gevonden gevallen.

De detectie is in Milestone 45 niet aangepast. Dit document bewaart de voorbeelden en voorgestelde richting voor een afzonderlijke slice.
