# Milestone 33 Handmatige Testchecklist

Gebruik uitsluitend de actuele Debug-build uit `Builds\Debug\Saga.exe`. Voor de zoekacties is een internetverbinding nodig.

## Hoofdroute met ISBN

- [ ] Open een bibliotheek met een boek zonder omslag dat een geldig ISBN, een titel en een auteur heeft.
- [ ] Open de Quality Page, kies `Geen omslag` en selecteer het boek.
- [ ] Controleer dat `Omslag zoeken` zichtbaar en actief is en dat de andere contextgebonden herstelknoppen verborgen zijn.
- [ ] Kies `Omslag zoeken` en controleer dat eerst een duidelijke zoekstatus en daarna maximaal twaalf omslagen verschijnen.
- [ ] Controleer dat geen omslag vooraf geselecteerd is en dat `Deze omslag gebruiken` uitgeschakeld blijft totdat je zelf een omslag kiest.
- [ ] Controleer dat iedere keuze een voorbeeld, bron en resolutie toont.
- [ ] Selecteer een passende omslag en kies `Deze omslag gebruiken`.
- [ ] Controleer dat het boek direct uit `Geen omslag` verdwijnt en dat telling en selectie logisch worden bijgewerkt.
- [ ] Controleer in het hoofdscherm, de boekenplank en het detailpaneel dat de nieuwe omslag direct zichtbaar is.

## Zoeken zonder ISBN en selectie

- [ ] Herhaal de route met een boek zonder ISBN maar met een herkenbare titel en auteur; controleer dat Saga ook hiervoor resultaten kan tonen.
- [ ] Selecteer een omslag met de muis en bevestig met dubbelklik.
- [ ] Herhaal indien mogelijk met een ander boek en selecteer uitsluitend met `Tab` en de pijltjestoetsen; bevestig met `Enter`.
- [ ] Maak het venster smaller en groter en controleer dat de galerij omloopt, scrolbaar blijft en de knoppen bereikbaar blijven.

## Annuleren en geen resultaten

- [ ] Open de zoekactie, selecteer eventueel een omslag en kies `Annuleren`; controleer dat het boek ongewijzigd blijft.
- [ ] Open de zoekactie opnieuw en annuleer met `Escape`; controleer opnieuw dat er niets wordt opgeslagen.
- [ ] Test indien beschikbaar een boek waarvoor Open Library geen omslag vindt; controleer dat Saga een begrijpelijke melding toont en niets wijzigt.
- [ ] Controleer dat `Dit is correct` beschikbaar blijft voor een boek waarvoor bewust geen omslag gewenst is.

## Netwerk- en gegevensveiligheid

- [ ] Verbreek tijdelijk de internetverbinding of blokkeer Open Library, start een zoekactie en controleer dat binnen redelijke tijd een begrijpelijke foutmelding verschijnt.
- [ ] Controleer na de netwerkfout dat het boek onder `Geen omslag` blijft staan en dat metadata en bestanden niet zijn gewijzigd.
- [ ] Herstel de verbinding en controleer dat een nieuwe zoekactie normaal kan slagen.
- [ ] Controleer na een geslaagde keuze dat `books\<boek-id>\cover.jpg` in de actieve bibliotheek bestaat.
- [ ] Sluit Saga, open dezelfde bibliotheek opnieuw en controleer dat de gekozen omslag behouden en zichtbaar is.
- [ ] Controleer dat titel, auteurs, beschrijving, taal, uitgever, publicatiedatum, tags, serie, serienummer, ISBN, leesstatus en beschikbare formaten gelijk zijn gebleven.

## Regressie

- [ ] Controleer dat `Auteur wijzigen`, `Taal wijzigen`, `Serie wijzigen`, `Titel en auteur omwisselen`, `Dit is correct` en `Openen in bibliotheek` nog steeds in hun eigen context werken.
- [ ] Controleer dat een boek dat al een omslag heeft niet onder `Geen omslag` verschijnt.
- [ ] Controleer dat alleen de actuele Debug-build in `Builds\Debug` staat en gestart wordt.
