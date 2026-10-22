# Vervolg auteursbeheer: auteur hernoemen zonder samenvoegen

## Status en gebruikersdoel

De gebruiker heeft de aanpak goedgekeurd; hernoemen is geïmplementeerd als uitbreiding van de nog open PR #49. De gebruiker heeft auteursbeheer, hernoemen en het eigen auteuricoon getest en akkoord bevonden. Een al bestaande doelnaam blokkeert de actie, zonder samenvoegoptie. Alleen de laatste kleine-vensterindeling vraagt nog een gerichte handmatige controle.

Voorbeeld: A.K. Straatsma wordt Anne Klaas Straatsma, mits die laatste naam niet al bij een andere auteursidentiteit voorkomt. Dezelfde auteur, foto, website en biografie blijven behouden. In het boekdetailscherm betekent een andere naam nog steeds een andere auteur toewijzen aan uitsluitend dat boek.

## Gedrag en acceptatie

- Aparte actie Auteur hernoemen bij de geselecteerde auteur; invulveld met huidige naam en bevestiging met oude naam, nieuwe naam en aantal gekoppelde boeken. Nog niet opgeslagen profielwijzigingen eerst volgens het bestaande opslaan/verwerpen/annuleren-patroon behandelen.
- Bij een vrije doelnaam wijzigt de bestaande auteursidentiteit van naam. Geen nieuwe auteur, geen achterblijvend oud profiel. Alle gekoppelde boeken krijgen de nieuwe naam, ook voorbij de eerste zichtbare pagina. Bij meerdere auteurs verandert alleen de gekozen auteur; overige namen en volgorde blijven behouden.
- Ook een bewaard profiel met nul boeken mag naar een vrije naam worden hernoemd. Fotobestanden houden hun identiteitsgebonden pad; boekdirectories hoeven niet te verhuizen.
- Vergelijk doelnaam volgens de bestaande unieke auteursnormalisatie: trimmen en invariant kleine letters. Hoofdletter-/spatievarianten van een andere auteur gelden dus ook als bezet. Een ongewijzigde naam is geen actie; alleen de schrijfwijze van dezelfde identiteit corrigeren is toegestaan.
- Een lege naam of onbekend-auteurplaatsaanduiding is ongeldig. Bij een bezette doelnaam verandert niets, ook niet wanneer die andere auteur nul boeken heeft. Toon: Deze auteursnaam bestaat al. Wijzig de auteur per boek via de boekdetails als je boeken aan deze bestaande auteur wilt koppelen.
- Geen twee personen met dezelfde naam modelleren, geen achtervoegsels verzinnen, geen auteurs samenvoegen en geen profielgegevens overdragen. Een uitbreiding van de algemene bulkmetadata-editor valt buiten deze slice.
- Annuleren vóór opslag verandert niets. Een conflict, tussentijdse auteurswijziging of databasefout laat geen gedeeltelijke hernoeming in de bibliotheek achter. Na succes blijven selectie en profiel bij dezelfde identiteit; boekenlijsten en auteursfilters worden bijgewerkt.
- Korte groene succesmelding met aantal boeken; begrijpelijke foutredenen en eventuele afzonderlijke waarschuwingen voor bestandssynchronisatie. Alle nieuwe teksten in de bestaande zes talen.

## Inventarisatie en technische aanpak

Authors/BookAuthors bevatten al stabiele identiteiten en een unieke NormalizedName. AuthorProfileRepository.SaveAsync verbiedt terecht naamswijzigingen bij gewone profielopslag. Behoud die scheiding en voeg een expliciete hernoemopdracht toe; alleen het profielveld Name vrij bewerkbaar maken is onvoldoende.

Books bevat een unieke DuplicateKey gebaseerd op titel en auteurs. Alleen Authors.Name wijzigen zou die sleutel verouderen. Voorgesteld: één database-transactie die de actuele bronidentiteit/naam en doelnaam valideert, alle gekoppelde boeken inventariseert, de voorgestelde duplicaatsleutels controleert en vervolgens Authors.Name/NormalizedName plus de bijbehorende Books.DuplicateKey/UpdatedUtc samen wijzigt. Gebruik de bestaande DuplicateKeyNormalizer, geen eigen benadering. Bij een fout wordt de gehele databasewijziging teruggedraaid. De unieke indexes blijven als laatste bewaking gelden. De bestaande generieke per-boek-auteurwijziging is geen vervanging voor deze identiteitsbehoudende opdracht.

Reëel stijlvoorbeeld: `DuplicateKeyNormalizer.BuildDuplicateKey(book.Metadata.Title, book.Metadata.Authors)`. Volg bestaande C#-records, async-methoden, CancellationToken, nullable conventies en getypeerde resultaatstatussen. Een Application-service orkestreert de opdracht; EF blijft in Infrastructure. Geen nieuwe dependency of voorziene schemawijziging.

Bewaar foto, biografie, website, BookAuthors-koppelingen, overige auteurs en overige boekvelden. Valideer opnieuw bij uitvoeren; bij gewijzigde bron of veranderde bevestigde selectie/aantallen vraag opnieuw te bevestigen in plaats van ongemerkt extra boeken mee te nemen. Annulering vóór commit rolt terug; na commit moet het resultaat eerlijk aangeven dat de bibliotheekwijziging is opgeslagen.

Na databasecommit moeten sidecars en ondersteunde ebook-metadata via de bestaande metadata-schrijfroute worden gesynchroniseerd, zonder de inmiddels opgeslagen auteursidentiteit opnieuw aan te maken of nieuwere boekgegevens te overschrijven. Inventariseer daarvoor de gedeelde bestandsschrijfverwerking in BookService/MetadataSidecarWriter; stel eventuele noodzakelijke kleine extractie apart testgestuurd veilig. Database en meerdere bestanden zijn niet één atomaire transactie: een mislukte bestandssynchronisatie geeft een aparte waarschuwing per boek en draait niet stil een succesvolle bibliotheekwijziging terug. Deze slice maakt geen nieuwe native EPUB-write-backimplementatie.

Het modale auteursvenster blijft aan de actieve bibliotheek gebonden. Geef gewijzigde boekidentiteiten door aan de bestaande hoofdschermverversing/reconciliatie; voorkom een aparte volledige boekvoorraad in het auteursviewmodel. UI blijft responsief en de hernoemactie is tijdens uitvoeren geblokkeerd.

## Structuur en verificatie

Contract/resultaat onder src/EbookManager.Domain/Authors en Abstractions; orkestratie onder src/EbookManager.Application/Authors; transacties onder src/EbookManager.Infrastructure/Persistence/Repositories. Viewmodel onder src/EbookManager.Presentation/ViewModels, WPF-interactie en vertalingen onder src/EbookManager.App. Tests onder tests/EbookManager.Tests, volgens bestaande xUnit/FluentAssertions-conventies.

Eerst falende tests voor behoud van identiteit/profiel en het weigeren van een bezette doelnaam. Vervolgens dekking voor alle boeken over paginagrenzen, mede-auteurs/volgorde, nul boeken, schrijfwijzecorrectie, duplicaatsleutels en daaropvolgende import, rollback, verouderde bron, annulering en bestandsschrijffouten. UI-tests bewaken bevestiging, conceptwijzigingen, tweede opdracht tijdens opslag, selectie en verversing. Nieuwe handmatige checklist bij implementatie; de reeds door de gebruiker afgevinkte Milestone 48-checklist blijft behouden.

- Gericht: `dotnet test tests/EbookManager.Tests/EbookManager.Tests.csproj -c Debug --no-restore --filter FullyQualifiedName~Author`.
- Volledig: `dotnet test EbookManager.sln -c Debug --no-restore`.
- Build: `dotnet build src/EbookManager.App/EbookManager.App.csproj -c Debug --no-restore`.
- Diff: `git diff --check`.

Definitieve verificatie: 933 tests geslaagd, 0 mislukt of overgeslagen. Debug-build 2026.10.22.109, 22 oktober 2026 om 18:09 (Europe/Amsterdam), 0 waarschuwingen en fouten. Eigen review op correctheid, eenvoud, architectuur, veiligheid en prestaties uitgevoerd. De bestaande titelgesorteerde boekpagina's blijven bij hernoemen behouden; een regressietest met 205 boeken bewaakt Meer laden zonder doublures. Bij auteurswissel verdwijnen oude bestandswaarschuwingen. De twee relevante CodeRabbit-opmerkingen zijn verwerkt: passend werkbalkicoon en een scrollbaar profielpaneel met bereikbare onderknoppen op kleine vensters. Zie de afzonderlijke handmatige checklist voor de nog open layoutcontrole.

## Grenzen en beslispunt

Altijd gegevensbehoud, bestaande duplicaatbescherming, eerst falende tests, eigen review, project-DoD en Markdown-spiegel toepassen. Eerst afstemmen bij noodzakelijke schemawijziging, nieuwe dependencies of uitbreiding naar samenvoegen/bulkbewerken. Nooit echte gebruikersgegevens voor fouttests veranderen, auteurs op naam automatisch samenvoegen, technische exceptions als gebruikersmelding tonen of zelf een PR mergen.

De functionele en technische keuzes zijn bevestigd en uitgevoerd op codex/milestone-48-author-profiles, vanaf main 1de190f. PR #49 is nog open; geen merge verondersteld. De databasewijziging is atomair, bestandssynchronisatie volgt apart met eerlijke waarschuwingen per boek. Bestaande auteuridentiteit en profiel blijven behouden; geen schemawijziging of nieuwe dependency nodig.
