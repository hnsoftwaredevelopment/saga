# Saga Milestone 27 Metadata Dashboard Navigation Implementation Plan

**Goal:** Open een geselecteerd kwaliteitsprobleemboek vanuit het metadata quality dashboard in de bestaande bibliotheekcontext.

**Architecture:** Laat het modale dashboard een boek-id teruggeven, laat `LibraryViewModel` minimale filter- en selectiewijzigingen uitvoeren, en laat de zichtbare WPF-view het platformafhankelijke scrollen verzorgen.

**Tech stack:** .NET 10, WPF, CommunityToolkit.Mvvm, Syncfusion WPF DataGrid, xUnit en FluentAssertions.

## Taak 1: Dashboardselectie En Resultaat

**Bestanden:**

- `src/EbookManager.Presentation/ViewModels/MetadataQualityDashboardViewModel.cs`
- `tests/EbookManager.Tests/App/ViewModels/MetadataQualityDashboardViewModelTests.cs`

- [ ] Schrijf eerst falende tests voor de eerste rijselectie, een lege categorie en de geselecteerde boek-id.
- [ ] Voeg `SelectedBook` en een afgeleide `SelectedBookId` toe.
- [ ] Zorg dat een categoriewijziging de eerste rij of `null` selecteert.
- [ ] Voer alleen de gerichte viewmodeltests uit en bevestig dat ze slagen.

## Taak 2: Modaal Dashboard Retourneert Een Boek-id

**Bestanden:**

- `src/EbookManager.Presentation/Abstractions/IUserInteractionService.cs`
- `src/EbookManager.App/Services/UserInteractionService.cs`
- `src/EbookManager.App/Views/MetadataQualityDashboardWindow.xaml`
- `src/EbookManager.App/Views/MetadataQualityDashboardWindow.xaml.cs`
- `src/EbookManager.App/Resources/Strings/AppResources*.resx`
- relevante test-fakes van `IUserInteractionService`

- [ ] Pas test-fakes eerst aan naar een `Task<Guid?>`-resultaat en voeg een falende integratietest voor het teruggegeven id toe.
- [ ] Bind de DataGrid-selectie aan `SelectedBook`.
- [ ] Voeg de gelokaliseerde knop `Openen in bibliotheek` toe en schakel die uit zonder selectie.
- [ ] Laat knop en dubbelklik hetzelfde resultaat instellen en de window bevestigend sluiten.
- [ ] Laat `UserInteractionService` het gekozen id of `null` retourneren.
- [ ] Bouw de WPF-app om XAML en alle interface-implementaties te controleren.

## Taak 3: Minimale Zoek- En Filteraanpassing

**Bestanden:**

- `src/EbookManager.Presentation/ViewModels/LibraryViewModel.cs`
- `tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs`

- [ ] Schrijf falende tests voor een zichtbaar boek, blokkerende algemene zoektekst, een blokkerende standaardfiltergroep en een blokkerende custom-metadata-filtergroep.
- [ ] Voeg een interne revealmethode toe die eerst controleert of het boek nog bestaat.
- [ ] Verwijder alleen zoektekst en geselecteerde filtergroepen die het doelboek uitsluiten.
- [ ] Onderdruk tussentijdse filterverversingen en pas de filters daarna één keer toe.
- [ ] Selecteer de zichtbare rij als enige selectie.
- [ ] Voeg een test toe die bewijst dat niet-blokkerende filters, view, sortering, layout en groepering behouden blijven.

## Taak 4: Gegroepeerde Weergaven En Reveal-Verzoek

**Bestanden:**

- `src/EbookManager.Presentation/ViewModels/LibraryGroupNodeViewModel.cs`
- `src/EbookManager.Presentation/ViewModels/LibraryViewModel.cs`
- `tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs`

- [ ] Schrijf een falende test voor een boek in twee groeperingsniveaus.
- [ ] Voeg een methode toe die het eerste groeppad naar een boek-id vindt en alleen dat pad uitklapt.
- [ ] Publiceer na selectie een eenmalig reveal-verzoek met boek-id en oplopend volgnummer.
- [ ] Test dat hetzelfde boek opnieuw geopend kan worden en opnieuw een reveal-verzoek veroorzaakt.

## Taak 5: Scrollen In De Actieve WPF-view

**Bestanden:**

- `src/EbookManager.App/Views/BookshelfView.xaml.cs`
- `src/EbookManager.App/Views/DetailedGridView.xaml.cs`
- `src/EbookManager.App/Views/LibraryListView.xaml.cs`
- eventueel één gedeelde helper onder `src/EbookManager.App/Views`

- [ ] Laat iedere view het reveal-verzoek observeren zolang die geladen is.
- [ ] Gebruik `ScrollIntoView` voor de bookshelf en de passende Syncfusion-scroll-API voor Detailed en List.
- [ ] Handel gegroepeerde en gevirtualiseerde inhoud na de layoutpass af via de dispatcher.
- [ ] Ontkoppel eventhandlers bij `Unloaded`.
- [ ] Bouw de app en voer een gerichte handmatige controle in alle drie views uit.

## Taak 6: Ontbrekend Boek, Documentatie En Volledige Verificatie

**Bestanden:**

- `src/EbookManager.App/Resources/Strings/AppResources*.resx`
- `tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs`
- `docs/feature-requests/metadata-quality-dashboard.md`
- `docs/manual-tests/milestone-27-checklist.md`
- `README.md`

- [ ] Schrijf eerst een falende test voor een boek dat na het openen van het dashboard is verwijderd.
- [ ] Toon een gelokaliseerde melding zonder zoektekst of filters te wijzigen.
- [ ] Werk de featurebeschrijving, README en handmatige checklist bij.
- [ ] Spiegel alle gewijzigde Markdown-bestanden naar Obsidian en verifieer de kopieën.
- [ ] Voer `dotnet test EbookManager.sln` en `dotnet build EbookManager.sln` uit.
- [ ] Controleer de volledige diff op onbedoelde wijzigingen en open daarna een draft-PR.
