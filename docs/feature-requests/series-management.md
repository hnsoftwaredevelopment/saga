# Series beheren

Afronding 31 oktober 2026: de gebruiker heeft het samenvoegen van series opnieuw getest zonder problemen en bevestigt dat PR #56 en PR #57 zijn gemerged. GitHub bevestigt beide merges; lokale main is bijgewerkt naar 373d45f. Milestones 54 en 55 zijn afgerond. Onderstaande uitvoeringsnotities en individuele checklistvakjes blijven als historie behouden. De UI-/icoonharmonisatie blijft een afzonderlijke wens voor later.

## Status

Bij de test van het tagbeheer stelde de gebruiker **Serie wijzigen** voor als mogelijke naam voor het verplaatsen van geselecteerde boeken. De huidige actie **Geselecteerde boeken afsplitsen** maakt een nieuwe serie aan. Een bestaande serie als bestemming kiezen is een mogelijke uitbreiding.

Milestone 60 voegt **Geselecteerde boeken afsplitsen** toe. Selecteer een deel van de boeken in één serie, controleer de titels, volgnummers en het aantal achterblijvers, en sla op onder een unieke nieuwe serienaam. Saga toont de nieuwe serie na het opslaan. Voor alle boeken samen blijft **Serie hernoemen** beschikbaar. Zie de [specificatie](../superpowers/specs/2026-11-10-saga-milestone-60-series-split-design.md) en [checklist 60](../manual-tests/milestone-60-checklist.md).

## Historie — Milestone 59 geaccepteerd en gemerged

Milestone 59 voegt **Geselecteerde series samenvoegen** toe. Kies zelf twee of meer series, vergelijk al hun boeken en kies één van hun namen of typ een nieuwe naam. Boeken, volgnummers en overige metadata blijven behouden. Een bestaande niet-geselecteerde doelserie wordt geweigerd; neem die expliciet in de selectie op of kies een andere naam. Meer laden bewaart de serie-selectie, een nieuwe zoekopdracht wist haar. Bij meerdere geselecteerde series is het boekpaneel niet bewerkbaar. Na succes zijn index, doelboeken en hoofdweergave bijgewerkt en is het zoekveld leeg. De bestaande duplicaatvoorstellen behouden hun eigen expliciete doelkeuze. Zie de [specificatie](../superpowers/specs/2026-11-03-saga-milestone-59-manual-series-merge-design.md) en [checklist 59](../manual-tests/milestone-59-checklist.md). Verificatie: **1237 geautomatiseerde tests en 32 echte WPF-proeven geslaagd**, 0 mislukt/overgeslagen. Schone enige Debug-build **2026.11.3.53**, **3 november 2026 16:41:41 Europe/Amsterdam**, 0 waarschuwingen/fouten. Eigen vijfassige review uitgevoerd; licht/donker op minimale venstergrootte gerenderd en bekeken. Nieuwe doelnaam en niet-geselecteerde bestaande doelnaam eerst falend getest; 503 boeken over drie series, behoud nummers/overige metadata, echte metadata.json, bestaande rollback/conflict/bestandsroute, paging/selectie/focus en oude duplicaatroute gecontroleerd. De gebruiker heeft op 3 november 2026 alle tests uit checklist 59 uitgevoerd en bevestigd dat alles werkt zoals beschreven. Milestone 59 is afgerond. PR #61 is op 10 november 2026 gemerged.

## Historie — Milestone 58 geaccepteerd en gemerged

Milestone 58 voegt **Volgnummers toekennen** toe voor meerdere geselecteerde boeken binnen één serie. De gebruiker bepaalt de volgorde en een startnummer; het voorstel toont huidige/nieuwe nummers. De reeks loopt telkens met één op, ook vanaf een decimaal, nul of negatief nummer. Alleen geselecteerde boeken veranderen. Alle bevestigde gegevens worden vóór één transactie opnieuw gecontroleerd, met behoud van andere metadata. De bestaande bestandsverwerking volgt met afzonderlijke waarschuwingen. Numerieke sortering en zichtbare selectie worden hersteld. De bestaande losse nummerwijziging blijft behouden. Zie de [specificatie](../superpowers/specs/2026-11-03-saga-milestone-58-series-numbers-design.md) en [checklist 58](../manual-tests/milestone-58-checklist.md). De gebruiker heeft Milestone 58 op 3 november 2026 geaccepteerd: de uitgevoerde handmatige tests werken goed. Twee handmatige controles zijn niet uitgevoerd: bestandswaarschuwingen en conflict/rollback. Deze scenario's zijn geautomatiseerd afgedekt; de twee checklistvakjes blijven bewust open. PR #60 is op 3 november 2026 gemerged; Milestone 58 is afgerond. Verificatie: **1219 geautomatiseerde tests en 31 echte WPF-proeven geslaagd**, 0 mislukt/overgeslagen. Schone enige Debug-build **2026.11.3.33**, **3 november 2026 11:57:01 Europe/Amsterdam**, 0 waarschuwingen/fouten. Eigen vijfassige review uitgevoerd; licht/donker op minimale venstergrootte gerenderd en bekeken. SQL-batches boven 500 IDs, rollback, annulering, behoud overige metadata, echte metadata.json, bestandswaarschuwingen, paging/selectie/focus en bestaande één-boekroute gecontroleerd. De gebruiker heeft Milestone 58 op 3 november 2026 geaccepteerd: de uitgevoerde handmatige tests werken goed. Twee handmatige controles zijn niet uitgevoerd: bestandswaarschuwingen en conflict/rollback. Deze scenario's zijn geautomatiseerd afgedekt; de twee checklistvakjes blijven bewust open. PR #60 is op 3 november 2026 gemerged; Milestone 58 is afgerond.

## Historie — Milestone 56 geaccepteerd en gemerged

Milestone 56 voegt **Volgnummer wijzigen** toe voor één geselecteerd boek. Gehele en decimale nummers aanpassen of wissen, met invoercontrole en informatieve doubluremelding. Alleen het nummer/tijdstip worden opgeslagen, waarna de bestaande bestandsroute en directe sortering volgen. **1166 tests en 29 WPF-proeven groen**. Debug **2026.11.3.1** van **3 november 08:52:17 Europe/Amsterdam**, 0 waarschuwingen/fouten en één Saga.exe onder Builds. Eigen review en beide thema's op minimale grootte uitgevoerd. De gebruiker heeft de gehele slice op 3 november 2026 [getest en geaccepteerd](../manual-tests/milestone-56-checklist.md). PR #58 is op 3 november 2026 gemerged.

## Historie — Milestone 55 geaccepteerd en gemerged

Milestone 55 voegt **Duplicaten zoeken** toe aan Series beheren: mogelijke schrijfvarianten vergelijken en bewust samenbrengen onder één bestaande naam. Boeken, nummers en overige gegevens blijven behouden. **1131 tests en 28 echte WPF-scenario's slagen**; Debug-build **2026.10.31.6**, 31 oktober 2026 om **00:08:58 Europe/Amsterdam**, heeft 0 waarschuwingen/fouten en is de enige Saga.exe onder Builds. Eigen review en licht/donkercontrole uitgevoerd. De gebruiker heeft de gehele slice op 31 oktober 2026 [geaccepteerd](../manual-tests/milestone-55-checklist.md). PR #57 is gemerged. Deze slice begon als vervolg op PR #56; beide pull requests zijn inmiddels gemerged.

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

Automatisch serienummers toekennen, lege series aanmaken, online seriegegevens, ontbrekende delen herkennen, leesvolgorde over meerdere series en nieuwe ebookmetadatawriters. Handmatig meerdere geselecteerde boeken nummeren hoort bij Milestone 58, vrij gekozen series samenvoegen bij Milestone 59 en een selectie afsplitsen bij Milestone 60. Algemene UI- en icoonharmonisatie met boekvergelijking blijft een apart vervolgpunt.

## Volgnummer wijzigen — Milestone 56

Selecteer één geladen boek en open de nummerdialoog. Titel, auteurs en serie geven context; het invoerveld heeft focus. Leeg betekent wissen; ongewijzigde en ongeldige invoer kan niet worden opgeslagen. Het lokale decimaalteken wordt gebruikt zonder duizendtalscheiding. Een nummer dat bij andere boeken uit dezelfde serie voorkomt blijft toegestaan. Nul/negatieve decimalen blijven net als in boekdetails mogelijk.

Voor opslag worden identiteit, exacte serienaam, nummer, titel en auteurs opnieuw gecontroleerd. Nieuwe wijzigingen aan andere velden worden behouden: uitsluitend SeriesNumber/UpdatedUtc worden in één transactie aangepast. Na commit volgt bestaande metadata.json-/ebookverwerking. Bestandsproblemen worden apart gemeld; bij mislukte verversing is duidelijk dat het nummer wel opgeslagen is.

De reeds geladen lijst wordt in pagina's van 200 opnieuw numeriek opgebouwd. De selectie wordt op ID hersteld als het boek nog in die lijst valt. Anders verdwijnt de selectie met een korte uitleg en blijft Meer laden beschikbaar; er wordt geen gehele bibliotheek geladen om een verschoven boek zichtbaar te houden. Serienaam, zoektekst en tellingen blijven gelijk. Bij sluiten volgt de bestaande metadatareconciliatie met behoud van geladen omslagen.

Reviewcorrectie PR #61, 3 november 2026: opmerkingen 4455095425 en 4455095443 zijn verwerkt. De bestemming wordt ook na de eerste 200 series in de index opgenomen, geselecteerd en bij handmatig samenvoegen in beeld gebracht. Meer laden gebruikt het aantal werkelijk gelezen repositoryrijen, zodat de extra doelrij geen series laat overslaan of verdubbelen. Eerst falende regressie met 205 series toegevoegd; de WPF-proef gebruikt nu een bestemming achter de eerste pagina. De sidecartest gebruikt verschillende nummers per boek en leest iedere sidecar via het betreffende boek-ID. Opnieuw 1237 tests en 32 WPF-proeven geslaagd; Debug 2026.11.3.53 van 3 november 2026 16:41:41 Europe/Amsterdam, 0 waarschuwingen/fouten. De gebruikersacceptatie betreft build 2026.11.3.48; deze reviewcorrecties zijn geautomatiseerd gecontroleerd in de nieuwe build.
