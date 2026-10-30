# Auteursvarianten herkennen en samenvoegen

## Status

Gebruikerswens van 29 oktober 2026. Milestone 53 is op 30 oktober goedgekeurd en gebouwd: handmatig geselecteerde auteurs samenvoegen naar één bestaande geselecteerde auteur, met expliciete foto-/website-/biografiekeuzes en veilige boekcontrole. Automatisch voorstellen van vermoedelijk gelijke namen volgt later. Zie de [specificatie](../superpowers/specs/2026-10-30-saga-milestone-53-author-merge-design.md) en [testchecklist](../manual-tests/milestone-53-checklist.md). 1074 tests en 22 WPF-scenario's slagen; build 2026.10.30.20, 30 oktober 19:25:45 Europe/Amsterdam. Handmatige acceptatie staat open.

## Gewenste werkwijze

- Toon vermoedelijk gelijke auteurs, bijvoorbeeld J. Nowee, J.Nowee, Nowee, J. en Johannes Nowee.
- De gebruiker bepaalt welke vermeldingen daadwerkelijk dezelfde persoon zijn; gelijkenis is alleen een suggestie.
- Laat één bestaande of nieuwe naam kiezen voor de boeken van de bevestigde auteurs.
- Sluit aan op Auteurs beheren: afzonderlijke auteurrecords worden bewust samengevoegd, niet alleen de boekteksten vervangen.
- Behoud de gekoppelde boeken zonder dubbele auteurvermelding op hetzelfde boek. Maak duidelijk welke auteurgegevens overblijven.

## Eerste slice gebouwd

Het venster toont de geselecteerde auteurs, hun aantallen en het unieke totaal. De bestemming is één geselecteerde auteur. Bij verschillende foto's, biografieën of websites kiest de gebruiker expliciet een bron of Leeg laten. Foto's worden veilig onder de doelidentiteit opgeslagen. Overige mede-auteurs en hun relatieve volgorde blijven behouden. Dubbelboeken blokkeren de hele transactie. Bestaande bestandssynchronisatie, filter-/groeperingsverversing en waarschuwingen worden hergebruikt. Nieuwe zoekopdrachten wissen de selectie; Meer laden bewaart haar. Geen automatisch samenvoegen op naam en geen aanname dat twee gelijknamige personen dezelfde auteur zijn.

Het huidige hernoemen in Auteurs beheren blijft een andere actie en blokkeert een reeds bestaande doelnaam. Het herkennen van vermoedelijk gelijke auteurs en een nieuwe doelnaam zijn vervolgstappen; de veilige handmatige samenvoegroute vormt daarvoor de basis.

## Gebruikersfeedback na testen van Milestone 53

De gebruiker meldt dat de huidige functionaliteit werkt zoals beschreven en heeft de normale checklistcontroles positief aangevinkt. Twee verbeteringen zijn gewenst:

- Een actie Duplicaten zoeken voor auteurs toont vermoedelijk gelijke naamvarianten, zodat de gebruiker ze niet zelf hoeft te vermoeden of op te zoeken. Toon de oorspronkelijke namen en waarom ze overeenkomen. Begin voorzichtig met spaties/punten, omgekeerde naamvolgorde en initialen tegenover uitgeschreven voornamen bij een overeenkomende achternaam. Alleen dezelfde achternaam is onvoldoende; automatisch samenvoegen blijft uitgesloten.
- Een vergelijkingsvenster zoals bij boekduplicaten toont vooraf per auteur de foto, volledige biografie, website en het aantal gekoppelde boeken naast elkaar. Per onderdeel is zichtbaar wat wordt behouden en hoe het resultaat eruitziet; geen keuze uitsluitend op basis van een naam in een keuzelijst.

Voorgestelde uitvoering: eerst de visuele vergelijking op de bestaande veilige samenvoegroute, daarna de zoekactie met conservatieve overeenkomsten en expliciete gebruikersselectie. Dit is een vervolgvoorstel, geen reeds gebouwde uitbreiding van PR #55. Een nieuwe doelnaam en automatisch herkennen van pseudoniemen blijven afzonderlijke wensen.
