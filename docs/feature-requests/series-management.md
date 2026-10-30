# Series beheren

## Status

Milestone 55 voegt **Duplicaten zoeken** toe aan Series beheren: mogelijke schrijfvarianten vergelijken en bewust samenbrengen onder één bestaande naam. Boeken, nummers en overige gegevens blijven behouden. **1131 tests en 28 echte WPF-scenario's slagen**; Debug-build **2026.10.31.6**, 31 oktober 2026 om **00:08:58 Europe/Amsterdam**, heeft 0 waarschuwingen/fouten en is de enige Saga.exe onder Builds. Eigen review en licht/donkercontrole uitgevoerd. De gebruiker heeft de gehele slice op 31 oktober 2026 [geaccepteerd](../manual-tests/milestone-55-checklist.md). PR #57 wacht op merge. De vervolgbranch is gebaseerd op de nog openstaande PR #56.

## Historie — Milestone 50 geaccepteerd en gemerged

Milestone 50 is gebouwd op de goedgekeurde [specificatie](../superpowers/specs/2026-10-24-saga-milestone-50-series-management-design.md), vanaf geaccepteerde en gemergede PR #50. Beschikbaar via **Series beheren** in de hoofdwerkbalk. De gebruiker heeft de hoofdroute afgevinkt. Debug-build 2026.10.24.61 herstelt de resterende toetsenbordfocus bij laden en hernoemen, zonder een nieuwere focuskeuze of het hernoemvenster te verstoren. 980 geautomatiseerde tests en vijf interactieve WPF-focusscenario's slagen. Ook de gerichte toetsenbordhercontrole in de [handmatige checklist](../manual-tests/milestone-50-checklist.md) is door de gebruiker afgevinkt en geaccepteerd. PR #51 is gemerged; deze slice is afgerond.

## Eerste slice

- Herkenbaar beheervenster zoals Auteurs beheren, met zoeken en aantallen.
- Groeperen zoals het huidige seriefilter: namen trimmen en hoofdletterongevoelig vergelijken; geen gelijkenisherkenning.
- Boeken per serie met serienummer, titel en auteur(s), numeriek gesorteerd en gepagineerd; nummers ontbreken onderaan.
- Volledige serie expliciet hernoemen met bevestiging en behoud van serienummers en alle overige boekgegevens.
- Een bestaande andere doelserie blokkeren; geen samenvoegen.
- Transactionele databasewijziging, bestaande bestandsverwerking met aparte waarschuwingen en directe actualisatie van hoofdscherm/filter.
- Geen serietabel of bibliotheekconversie; geen foto-/biografieprofiel voor series in deze slice.

De lijst groepeert alleen bestaande namen; lezen wijzigt de opgeslagen schrijfwijzen niet. Na bevestigd hernoemen hebben alle boeken in de groep dezelfde getrimde naam. Een bestandswaarschuwing noemt de betrokken boeken; de wijziging in de bibliotheek is dan wel opgeslagen. Na sluiten worden hoofdscherm, details en seriefilter via de bestaande route bijgewerkt met behoud van geladen omslagen.

## Vervolg — Milestone 55

De voorstellen herkennen dezelfde letter-/cijferreeks met andere spaties, leestekens of accenten. Getallen zoals 1 en 2, 01 en 1 blijven verschillend. Al gegroepeerde hoofdletter-/trimvarianten worden niet opnieuw voorgesteld. Een lichte volledige naamindex wordt gecachet; zoeken loopt buiten de UI-thread en toont maximaal 1000 voorstellen met een verfijningsmelding. Er is geen automatische samenvoeging of online zoekactie.

Een geselecteerd paar opent een boekenvergelijking: huidige serie, nummer, titel en auteurs, numeriek gesorteerd, eerst 200 rijen met Meer laden. Gelijke nummers en boeken zonder nummer zijn herkenbaar, maar blokkeren niet. Kies één bestaande naam uit het paar en bevestig. Alle boeken uit het bevestigde paar worden verwerkt, ook nog niet geladen rijen. Bestaande exact passende doelboeken worden niet opnieuw geschreven. De oude schrijfvariant verdwijnt doordat haar boeken de doelnaam krijgen; er is geen aparte serietabel.

De gehele preview wordt opnieuw gecontroleerd vóór één transactie die alleen Series en UpdatedUtc van gewijzigde boeken aanpast. De bestaande metadata.json-/ebookroute volgt na commit; fouten blijven afzonderlijke bestandswaarschuwingen. Na succes verversen voorstellen en seriesbeheer en verdwijnt de zoektekst, met behoud van gekozen serie en boeken. Bij annuleren blijven gegevens en oorspronkelijke zoektekst staan. De gewone hernoemactie blijft een bestaande andere doelnaam weigeren.

## Nog buiten scope

Serienummers wijzigen/opnieuw toekennen, series splitsen, vrij gekozen willekeurige series samenvoegen, een vrije nieuwe doelnaam tijdens samenvoegen, lege series aanmaken, online seriegegevens, ontbrekende delen herkennen, leesvolgorde over meerdere series en nieuwe ebookmetadatawriters. Losse boekdetails en bulkbewerking behouden hun bestaande betekenis. Algemene UI-/icoonharmonisatie met boekvergelijking blijft een apart vervolgpunt.
