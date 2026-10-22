# Milestone 45: titel en auteur voor meerdere boeken omwisselen

Testbuild: `Builds\Debug\Saga.exe`, versie `2026.10.22.24`, gebouwd op 22 oktober 2026 om 11:26.

## Voorbereiding en overzicht

- [ ] Maak zo nodig een bibliotheekback-up; noteer titel, auteur en overige metadata van enkele testboeken.
- [ ] Open Quality, kies Mogelijk omgewisselde auteur/titel en selecteer twee of meer boeken met Ctrl of Shift.
- [ ] Titel en auteur omwisselen opent één overzicht met het juiste aantal te controleren boeken.
- [ ] Per boek zijn huidige en voorgestelde titel en auteur duidelijk zichtbaar. Lange waarden lopen om; scrollen werkt met muis en toetsenbord.
- [ ] Alle vinkjes beginnen uit. Bevestigen is uitgeschakeld; Enter zonder vinkjes wijzigt niets.

## Expliciete keuze en annuleren

- [ ] Vink één boek aan. Alleen dat boek telt mee op de bevestigingsknop.
- [ ] Vink het weer uit; bevestigen is opnieuw uitgeschakeld.
- [ ] Gebruik Tab en spatie om een boek aan en uit te vinken.
- [ ] Annuleer via de knop en via Escape. Er is niets gewijzigd.
- [ ] Open het overzicht opnieuw; de vinkjes beginnen weer uit.

## Opslaan en verversen

- [ ] Kies minstens twee boeken maar vink in het overzicht slechts één ervan aan; bevestig met Enter of de knop.
- [ ] Alleen het aangevinkte boek is omgewisseld. Het andere blijft ongewijzigd in de kwaliteitslijst staan.
- [ ] De melding is kort en groen en noemt het werkelijk gewijzigde aantal.
- [ ] Herhaal met meerdere aangevinkte boeken; ieder krijgt zijn eigen omgewisselde waarden.
- [ ] Hoofdlijst, details en auteursfilters tonen de actuele titel en auteur zonder herstart.
- [ ] Overige metadata, tags, serie, taal, omslag, formaten en leesstatus blijven behouden.
- [ ] Na herstart zijn de wijzigingen nog aanwezig.

## Regressies en veiligheid

- [ ] Eén geselecteerd boek toont de bestaande enkelvoudige bevestiging met vóór en na.
- [ ] De detailactie voor titel/auteur omwisselen en de overige bulkherstelacties werken nog zoals eerder.
- [ ] Controleer zo mogelijk een andere interfacetaal en een kleiner venster.
- [ ] Als er een ongeschikte rij is: die heeft een toelichting en kan niet worden aangevinkt.
- [ ] Indien veilig reproduceerbaar: een na openen gewijzigd of verdwenen boek wordt niet vanuit de oude preview overschreven.
- [ ] Indien veilig reproduceerbaar: bij gemengde resultaten worden aantallen gemeld, overige boeken verwerkt en mislukte boeken geselecteerd voor opnieuw proberen.

Ongeschikte boeken, gewijzigde waarden, verdwenen boeken, opslagfouten en annulering tijdens verwerking zijn automatisch getest. Niet veilig reproduceerbare foutcontroles mogen als niet uitgevoerd worden genoteerd.

## Acceptatie

- [ ] Alle toepasselijke controles zijn geslaagd; eventuele afwijkingen zijn beschreven.
