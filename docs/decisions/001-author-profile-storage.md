# ADR 001: bibliotheekgebonden auteursprofielen

## Status en datum

Geaccepteerd door gebruiker als onderdeel van Milestone 48, 22 oktober 2026.

## Context

Authors en BookAuthors bestaan al met stabiele identiteiten. De gebruiker wil foto, website en biografie onderhouden, zonder boekmetadata te wijzigen. Ongebruikte auteurs worden momenteel automatisch verwijderd. Een platte opslagmap met zeer veel directories veroorzaakte eerder OneDrive-problemen.

## Besluit

Voeg drie optionele velden aan Authors toe: Biography, WebsiteUrl en PhotoRelativePath. Behoud auteurs met profielinhoud, ook zonder boeken; hergebruik hun identiteit bij opnieuw koppelen. De bestaande auteursnaam is in deze eerste slice alleen-lezen. Bewaar gekopieerde, verwerkte foto's onder authors met twee hextekens als mapverdeling en unieke bestandsnamen. Bewaar alleen bibliotheekrelatieve verwijzingen.

## Alternatieven

Een tweede auteursregister zou identiteiten en naamkoppelingen dupliceren. Een aparte één-op-één-profieltabel is mogelijk, maar voor drie optionele velden zonder aparte levenscyclus onnodig. Foto's buiten de bibliotheek zijn eenvoudiger, maar niet overdraagbaar. Een vaste fotobestandsnaam maakt veilig behoud bij een mislukte databaseopslag lastiger.

## Gevolgen

Additieve migratie met behoud van boeken en auteurskoppelingen. Beide orphan-opruimroutes moeten verrijkte auteurs ontzien. Schrijf een nieuwe foto vóór databaseopslag en ruim de oude pas daarna op. Een procesonderbreking kan een ongebruikt bestand achterlaten; bestaande gegevens gaan hierdoor niet verloren. Aliassen, hernoemen en samenvoegen worden later afzonderlijk ontworpen.
