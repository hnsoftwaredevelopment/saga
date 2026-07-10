# Saga Milestone 4 Metadata Settings Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first Milestone 4 foundation slice: structured settings, settings-driven author sorting, friendlier language display, and consistent standard metadata surfaces.

**Architecture:** Keep SQLite and existing `BookMetadata` authoritative. Add user preferences to `AppSettings`, derive author sort keys at display/sort time instead of storing per-book author-sort metadata, and keep metadata normalization conservative. The first implementation pass should avoid schema migrations unless a selected field cannot be represented with the existing model.

**Tech Stack:** .NET 10, WPF, CommunityToolkit.Mvvm, EF Core SQLite, xUnit, FluentAssertions.

---

## Scope

This plan implements the first Milestone 4 slice only.

Included:

- settings model extension for author sort strategy;
- settings window structure prepared for General, Appearance, Import, Metadata, Duplicates, Confirmations, Diagnostics;
- author sorting as a strategy, not a stored book field;
- author filter and author sort option using the selected strategy;
- language display helper reuse and tests;
- details pane standard metadata consistency review;
- documentation and manual checklist updates.

Excluded:

- custom columns;
- stored per-book `AuthorSort`;
- rating;
- identifiers as scheme/value pairs;
- native ebook metadata write-back;
- e-reader sync;
- user-defined views.

## File Structure

- `src/EbookManager.Domain/Settings/AuthorSortStrategy.cs`  
  Defines selectable author sort strategies.
- `src/EbookManager.Domain/Abstractions/IAppSettingsStore.cs`  
  Extends `AppSettings` with `AuthorSortStrategy`.
- `src/EbookManager.Application/Metadata/AuthorSortKeyBuilder.cs`  
  Pure helper for deriving sort keys from display author names.
- `src/EbookManager.Application/Metadata/LanguageDisplayService.cs`  
  Pure helper for language filter keys and localized display names.
- `src/EbookManager.Presentation/ViewModels/SettingsViewModel.cs`  
  Exposes settings sections and author sort strategy selection.
- `src/EbookManager.Presentation/ViewModels/LibraryViewModel.cs`  
  Applies author sort strategy to author sorting and author filter ordering.
- `src/EbookManager.App/Views/SettingsWindow.xaml`  
  Replaces flat settings layout with sectioned tabs or grouped sections.
- `src/EbookManager.App/Resources/Strings/AppResources*.resx`  
  Adds localized labels for new settings sections and author sort options.
- `tests/EbookManager.Tests/Metadata/AuthorSortKeyBuilderTests.cs`  
  Covers author sort strategy behavior.
- `tests/EbookManager.Tests/Metadata/LanguageDisplayServiceTests.cs`  
  Covers language normalization and display.
- `tests/EbookManager.Tests/App/ViewModels/SettingsViewModelTests.cs`  
  Covers settings round-trip and selectable author sort strategies.
- `tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs`  
  Covers author filter ordering and author sort behavior.
- `tests/EbookManager.Tests/Settings/*SettingsStoreTests.cs`  
  Covers default settings and backward-compatible settings round-trip.
- `docs/manual-tests/milestone-4-checklist.md`  
  Manual verification checklist.
- `README.md`  
  Updates current status and manual checklist links.

## Task 1: Add Author Sort Strategy To Settings

**Files:**
- Create: `src/EbookManager.Domain/Settings/AuthorSortStrategy.cs`
- Modify: `src/EbookManager.Domain/Abstractions/IAppSettingsStore.cs`
- Modify: `src/EbookManager.Infrastructure/Settings/JsonAppSettingsStore.cs`
- Modify: `tests/EbookManager.Tests/TestSupport/InMemoryAppSettingsStore.cs`
- Modify: `tests/EbookManager.Tests/Settings/JsonAppSettingsStoreTests.cs`
- Modify: `tests/EbookManager.Tests/Settings/InMemoryAppSettingsStoreTests.cs`
- Modify: `tests/EbookManager.Tests/App/ViewModels/SettingsViewModelTests.cs`

- [ ] **Step 1: Write failing settings tests**

Add to `tests/EbookManager.Tests/App/ViewModels/SettingsViewModelTests.cs`:

```csharp
[Fact]
public void SelectableAuthorSortStrategies_include_display_and_last_name_options()
{
    var viewModel = new SettingsViewModel(new InMemoryAppSettingsStore());

    viewModel.SelectableAuthorSortStrategies
        .Select(option => option.Value)
        .Should()
        .Equal(AuthorSortStrategy.DisplayName, AuthorSortStrategy.LastNameFirst, AuthorSortStrategy.LastNameFirstDutchPrefixes);
}

[Fact]
public async Task Save_preserves_last_library_path_while_updating_author_sort_strategy()
{
    var store = new InMemoryAppSettingsStore();
    await store.SaveAsync(new AppSettings(
        "C:\\ELibrary",
        "en-US",
        "Light",
        "Detailed",
        true,
        true,
        AuthorSortStrategy.DisplayName), default);
    var viewModel = new SettingsViewModel(store);
    await viewModel.LoadAsync();

    viewModel.AuthorSortStrategy = AuthorSortStrategy.LastNameFirst;

    await viewModel.SaveAsync();

    var settings = await store.LoadAsync(default);
    settings.AuthorSortStrategy.Should().Be(AuthorSortStrategy.LastNameFirst);
    settings.LastLibraryPath.Should().Be("C:\\ELibrary");
}
```

Add `using EbookManager.Domain.Settings;` to the test file.

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet test "tests\EbookManager.Tests\EbookManager.Tests.csproj" --filter "FullyQualifiedName~SettingsViewModelTests" -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903
```

Expected: compile failure because `AuthorSortStrategy`, `SelectableAuthorSortStrategies`, and `SettingsViewModel.AuthorSortStrategy` do not exist.

- [ ] **Step 3: Add the enum**

Create `src/EbookManager.Domain/Settings/AuthorSortStrategy.cs`:

```csharp
namespace EbookManager.Domain.Settings;

public enum AuthorSortStrategy
{
    DisplayName,
    LastNameFirst,
    LastNameFirstDutchPrefixes
}
```

- [ ] **Step 4: Extend `AppSettings` with a backward-compatible default**

Modify `src/EbookManager.Domain/Abstractions/IAppSettingsStore.cs`:

```csharp
using EbookManager.Domain.Libraries;
using EbookManager.Domain.Settings;

namespace EbookManager.Domain.Abstractions;

public sealed record AppSettings(
    string? LastLibraryPath,
    string Culture,
    string Theme,
    string DefaultView,
    bool ConfirmDelete,
    bool IncludeScanSubdirectories = true,
    AuthorSortStrategy AuthorSortStrategy = AuthorSortStrategy.DisplayName);
```

Keep the `IAppSettingsStore` interface unchanged.

- [ ] **Step 5: Update settings defaults and in-memory store**

Modify `JsonAppSettingsStore.DefaultSettings`:

```csharp
private static readonly AppSettings DefaultSettings = new(
    null,
    "en-US",
    "Light",
    "Detailed",
    true,
    true,
    AuthorSortStrategy.DisplayName);
```

Add `using EbookManager.Domain.Settings;`.

Modify `tests/EbookManager.Tests/TestSupport/InMemoryAppSettingsStore.cs` default:

```csharp
public AppSettings Settings { get; private set; } = new(
    null,
    "en-US",
    "Light",
    "Detailed",
    true,
    true,
    AuthorSortStrategy.DisplayName);
```

Add `using EbookManager.Domain.Settings;`.

- [ ] **Step 6: Update `SettingsViewModel`**

Add an option record and property in `src/EbookManager.Presentation/ViewModels/SettingsViewModel.cs`:

```csharp
using EbookManager.Domain.Settings;

public sealed record AuthorSortStrategyOption(AuthorSortStrategy Value, string ResourceKey);
```

Inside `SettingsViewModel`:

```csharp
public IReadOnlyList<AuthorSortStrategyOption> SelectableAuthorSortStrategies { get; } =
[
    new(AuthorSortStrategy.DisplayName, "AuthorSortDisplayName"),
    new(AuthorSortStrategy.LastNameFirst, "AuthorSortLastNameFirst"),
    new(AuthorSortStrategy.LastNameFirstDutchPrefixes, "AuthorSortLastNameFirstDutchPrefixes")
];

[ObservableProperty]
private AuthorSortStrategy authorSortStrategy = AuthorSortStrategy.DisplayName;
```

In `LoadAsync`:

```csharp
AuthorSortStrategy = settings.AuthorSortStrategy;
```

In `SaveAsync`:

```csharp
AuthorSortStrategy = AuthorSortStrategy
```

inside the `current with { ... }` expression.

- [ ] **Step 7: Update existing AppSettings constructor calls in tests**

Run:

```powershell
rg -n "new AppSettings" tests src
```

For any call that should assert complete equality, append `AuthorSortStrategy.DisplayName` or the expected strategy. For calls that do not assert the full record, the default optional value can remain omitted.

- [ ] **Step 8: Verify settings tests pass**

Run:

```powershell
dotnet test "tests\EbookManager.Tests\EbookManager.Tests.csproj" --filter "FullyQualifiedName~SettingsViewModelTests|FullyQualifiedName~JsonAppSettingsStoreTests|FullyQualifiedName~InMemoryAppSettingsStoreTests" -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903
```

Expected: tests pass.

- [ ] **Step 9: Commit Task 1**

```powershell
git add src/EbookManager.Domain/Settings/AuthorSortStrategy.cs src/EbookManager.Domain/Abstractions/IAppSettingsStore.cs src/EbookManager.Infrastructure/Settings/JsonAppSettingsStore.cs src/EbookManager.Presentation/ViewModels/SettingsViewModel.cs tests/EbookManager.Tests/TestSupport/InMemoryAppSettingsStore.cs tests/EbookManager.Tests/Settings tests/EbookManager.Tests/App/ViewModels/SettingsViewModelTests.cs
git commit -m "Add author sort strategy setting"
```

## Task 2: Derive Author Sort Keys Without Storing Per-Book Metadata

**Files:**
- Create: `src/EbookManager.Application/Metadata/AuthorSortKeyBuilder.cs`
- Test: `tests/EbookManager.Tests/Metadata/AuthorSortKeyBuilderTests.cs`

- [ ] **Step 1: Write failing author sort tests**

Create `tests/EbookManager.Tests/Metadata/AuthorSortKeyBuilderTests.cs`:

```csharp
using EbookManager.Application.Metadata;
using EbookManager.Domain.Settings;
using FluentAssertions;

namespace EbookManager.Tests.Metadata;

public sealed class AuthorSortKeyBuilderTests
{
    [Theory]
    [InlineData("Karin Slaughter", "Karin Slaughter")]
    [InlineData("Slaughter, Karin", "Slaughter, Karin")]
    public void BuildSortKey_keeps_display_name_when_strategy_is_display_name(string author, string expected)
    {
        AuthorSortKeyBuilder.BuildSortKey(author, AuthorSortStrategy.DisplayName).Should().Be(expected);
    }

    [Theory]
    [InlineData("Karin Slaughter", "Slaughter, Karin")]
    [InlineData("J.R.R. Tolkien", "Tolkien, J.R.R.")]
    [InlineData("Slaughter, Karin", "Slaughter, Karin")]
    [InlineData("Unknown", "Unknown")]
    public void BuildSortKey_moves_last_token_first_when_strategy_is_last_name_first(string author, string expected)
    {
        AuthorSortKeyBuilder.BuildSortKey(author, AuthorSortStrategy.LastNameFirst).Should().Be(expected);
    }

    [Theory]
    [InlineData("Vincent van Gogh", "van Gogh, Vincent")]
    [InlineData("Peter van de Velde", "van de Velde, Peter")]
    [InlineData("Karin Slaughter", "Slaughter, Karin")]
    public void BuildSortKey_keeps_dutch_prefixes_with_last_name_when_strategy_uses_dutch_prefixes(string author, string expected)
    {
        AuthorSortKeyBuilder.BuildSortKey(author, AuthorSortStrategy.LastNameFirstDutchPrefixes).Should().Be(expected);
    }

    [Fact]
    public void BuildSortKey_uses_first_author_from_display_list()
    {
        AuthorSortKeyBuilder.BuildSortKey("Karin Slaughter; Lee Child", AuthorSortStrategy.LastNameFirst)
            .Should()
            .Be("Slaughter, Karin");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```powershell
dotnet test "tests\EbookManager.Tests\EbookManager.Tests.csproj" --filter "FullyQualifiedName~AuthorSortKeyBuilderTests" -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903
```

Expected: compile failure because `AuthorSortKeyBuilder` does not exist.

- [ ] **Step 3: Implement `AuthorSortKeyBuilder`**

Create `src/EbookManager.Application/Metadata/AuthorSortKeyBuilder.cs`:

```csharp
using EbookManager.Domain.Settings;

namespace EbookManager.Application.Metadata;

public static class AuthorSortKeyBuilder
{
    private static readonly string[] DutchPrefixes = ["van", "de", "den", "der", "van de", "van der", "van den"];

    public static string BuildSortKey(string? authorsText, AuthorSortStrategy strategy)
    {
        var author = FirstAuthor(authorsText);
        if (string.IsNullOrWhiteSpace(author) || strategy == AuthorSortStrategy.DisplayName)
        {
            return author;
        }

        if (author.Contains(',', StringComparison.Ordinal))
        {
            return author;
        }

        var parts = author.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return author;
        }

        return strategy == AuthorSortStrategy.LastNameFirstDutchPrefixes
            ? BuildDutchPrefixAwareKey(parts)
            : $"{parts[^1]}, {string.Join(' ', parts[..^1])}";
    }

    private static string FirstAuthor(string? authorsText) =>
        (authorsText ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? string.Empty;

    private static string BuildDutchPrefixAwareKey(string[] parts)
    {
        var lastNameStart = parts.Length - 1;
        for (var prefixLength = Math.Min(3, parts.Length - 1); prefixLength >= 1; prefixLength--)
        {
            var candidateStart = parts.Length - 1 - prefixLength;
            var candidate = string.Join(' ', parts[candidateStart..^1]);
            if (DutchPrefixes.Contains(candidate, StringComparer.OrdinalIgnoreCase))
            {
                lastNameStart = candidateStart;
                break;
            }
        }

        var lastName = string.Join(' ', parts[lastNameStart..]);
        var firstNames = string.Join(' ', parts[..lastNameStart]);
        return string.IsNullOrWhiteSpace(firstNames) ? lastName : $"{lastName}, {firstNames}";
    }
}
```

- [ ] **Step 4: Verify author sort tests pass**

```powershell
dotnet test "tests\EbookManager.Tests\EbookManager.Tests.csproj" --filter "FullyQualifiedName~AuthorSortKeyBuilderTests" -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903
```

Expected: tests pass.

- [ ] **Step 5: Commit Task 2**

```powershell
git add src/EbookManager.Application/Metadata/AuthorSortKeyBuilder.cs tests/EbookManager.Tests/Metadata/AuthorSortKeyBuilderTests.cs
git commit -m "Add derived author sort keys"
```

## Task 3: Apply Author Sort Strategy In Library Filters And Sorting

**Files:**
- Modify: `src/EbookManager.Presentation/ViewModels/LibraryViewModel.cs`
- Test: `tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs`

- [ ] **Step 1: Write failing library tests**

Add tests to `LibraryViewModelTests` near existing sort/filter tests:

```csharp
[Fact]
public async Task Author_filter_order_uses_author_sort_strategy()
{
    var settingsStore = new InMemoryAppSettingsStore();
    await settingsStore.SaveAsync(settingsStore.Settings with
    {
        AuthorSortStrategy = AuthorSortStrategy.LastNameFirst
    }, default);
    var viewModel = CreateLoadedViewModel(
        [
            CreateBook("Book A", ["Karin Slaughter"]),
            CreateBook("Book B", ["Lee Child"]),
            CreateBook("Book C", ["J.R.R. Tolkien"])
        ],
        settingsStore: settingsStore);

    viewModel.AuthorFilters.Select(filter => filter.Name)
        .Should()
        .Equal("Lee Child", "Karin Slaughter", "J.R.R. Tolkien");
}

[Fact]
public async Task Author_sort_option_uses_author_sort_strategy()
{
    var settingsStore = new InMemoryAppSettingsStore();
    await settingsStore.SaveAsync(settingsStore.Settings with
    {
        AuthorSortStrategy = AuthorSortStrategy.LastNameFirst
    }, default);
    var viewModel = CreateLoadedViewModel(
        [
            CreateBook("C", ["J.R.R. Tolkien"]),
            CreateBook("A", ["Karin Slaughter"]),
            CreateBook("B", ["Lee Child"])
        ],
        settingsStore: settingsStore);

    viewModel.SelectedSortOption = LibrarySortOption.Author;

    viewModel.VisibleBooks.Select(row => row.Title).Should().Equal("B", "A", "C");
}
```

Add `using EbookManager.Domain.Settings;`.

- [ ] **Step 2: Run tests to verify they fail**

```powershell
dotnet test "tests\EbookManager.Tests\EbookManager.Tests.csproj" --filter "FullyQualifiedName~Author_filter_order_uses_author_sort_strategy|FullyQualifiedName~Author_sort_option_uses_author_sort_strategy" -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903
```

Expected: tests fail because current sorting uses display author text.

- [ ] **Step 3: Add strategy state to `LibraryViewModel`**

In `LibraryViewModel`, add:

```csharp
private AuthorSortStrategy authorSortStrategy = AuthorSortStrategy.DisplayName;
```

Add `using EbookManager.Application.Metadata;` and `using EbookManager.Domain.Settings;`.

In the initialization flow where settings are loaded for default view, also assign:

```csharp
authorSortStrategy = settings.AuthorSortStrategy;
```

If the helper only runs once after library load, ensure it runs before `RefreshFacetFilters()` or call `ApplyFilter()` after loading the strategy.

- [ ] **Step 4: Apply strategy to author filter ordering**

Change `RefreshFilters` to accept an optional sort key selector:

```csharp
private void RefreshFilters(
    ObservableCollection<FacetFilterViewModel> filters,
    IEnumerable<string> values,
    Func<string, string>? displayNameSelector = null,
    Func<string, string>? sortKeySelector = null)
```

Change ordering:

```csharp
.OrderBy(value => sortKeySelector?.Invoke(value.Name) ?? value.Name, StringComparer.CurrentCultureIgnoreCase)
.ThenBy(value => value.Name, StringComparer.CurrentCultureIgnoreCase)
```

Call author filters as:

```csharp
RefreshFilters(
    AuthorFilters,
    books.SelectMany(book => book.Metadata.Authors),
    sortKeySelector: author => AuthorSortKeyBuilder.BuildSortKey(author, authorSortStrategy));
```

- [ ] **Step 5: Apply strategy to author sort option**

Change `ApplySort` to accept strategy:

```csharp
private static IEnumerable<BookRowViewModel> ApplySort(
    IEnumerable<BookRowViewModel> rows,
    LibrarySortOption sortOption,
    AuthorSortStrategy authorSortStrategy)
```

Use:

```csharp
LibrarySortOption.Author => rows
    .OrderBy(row => AuthorSortKeyBuilder.BuildSortKey(row.Authors, authorSortStrategy), StringComparer.CurrentCultureIgnoreCase)
    .ThenBy(row => row.Title, StringComparer.CurrentCultureIgnoreCase),
```

Update the call site:

```csharp
var rows = ApplySort(
        ApplyFacetFilters(filtered, SelectedActiveFilters),
        SelectedSortOption,
        authorSortStrategy)
    .ToList();
```

- [ ] **Step 6: Verify library tests pass**

```powershell
dotnet test "tests\EbookManager.Tests\EbookManager.Tests.csproj" --filter "FullyQualifiedName~LibraryViewModelTests" -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903
```

Expected: library tests pass.

- [ ] **Step 7: Commit Task 3**

```powershell
git add src/EbookManager.Presentation/ViewModels/LibraryViewModel.cs tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs
git commit -m "Apply author sort strategy in library views"
```

## Task 4: Extract Language Display Behavior Into A Reusable Service

**Files:**
- Create: `src/EbookManager.Application/Metadata/LanguageDisplayService.cs`
- Modify: `src/EbookManager.Presentation/ViewModels/LibraryViewModel.cs`
- Test: `tests/EbookManager.Tests/Metadata/LanguageDisplayServiceTests.cs`
- Test: `tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs`

- [ ] **Step 1: Write failing language tests**

Create `tests/EbookManager.Tests/Metadata/LanguageDisplayServiceTests.cs`:

```csharp
using EbookManager.Application.Metadata;
using FluentAssertions;
using System.Globalization;

namespace EbookManager.Tests.Metadata;

public sealed class LanguageDisplayServiceTests
{
    [Theory]
    [InlineData("eng", "en")]
    [InlineData("en-US", "en")]
    [InlineData("nl-NL", "nl")]
    [InlineData("nl", "nl")]
    [InlineData("lv", "lv")]
    public void FilterKey_normalizes_common_language_values(string value, string expected)
    {
        LanguageDisplayService.FilterKey(value).Should().Be(expected);
    }

    [Fact]
    public void DisplayName_uses_current_ui_culture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");

            LanguageDisplayService.DisplayName("eng").Should().Be("Engels");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void DisplayName_returns_original_value_when_language_is_unknown()
    {
        LanguageDisplayService.DisplayName("fictional-language").Should().Be("fictional-language");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```powershell
dotnet test "tests\EbookManager.Tests\EbookManager.Tests.csproj" --filter "FullyQualifiedName~LanguageDisplayServiceTests" -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903
```

Expected: compile failure because `LanguageDisplayService` does not exist.

- [ ] **Step 3: Implement `LanguageDisplayService`**

Create `src/EbookManager.Application/Metadata/LanguageDisplayService.cs`:

```csharp
using System.Globalization;

namespace EbookManager.Application.Metadata;

public static class LanguageDisplayService
{
    public static string? FilterKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Equals("eng", StringComparison.OrdinalIgnoreCase))
        {
            return "en";
        }

        try
        {
            return CultureInfo.GetCultureInfo(normalized).TwoLetterISOLanguageName;
        }
        catch (CultureNotFoundException)
        {
            return normalized;
        }
    }

    public static string DisplayName(string value)
    {
        var normalized = FilterKey(value) ?? value.Trim();
        if (normalized.Length == 0)
        {
            return normalized;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(normalized);
            var languageOnly = CultureInfo.GetCultureInfo(culture.TwoLetterISOLanguageName);
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(languageOnly.DisplayName);
        }
        catch (CultureNotFoundException)
        {
            return value;
        }
    }
}
```

- [ ] **Step 4: Replace private language helpers in `LibraryViewModel`**

Remove private `LanguageDisplayName` and `LanguageFilterKey` implementations from `LibraryViewModel`.

Replace call sites:

```csharp
LanguageDisplayService.FilterKey(book.Metadata.Language)
LanguageDisplayService.DisplayName
```

For scalar comparisons:

```csharp
MetadataFilterKind.Language => ScalarValueMatches(metadata.Language, oldValue, LanguageDisplayService.FilterKey),
```

For replace:

```csharp
LanguageDisplayService.FilterKey
```

- [ ] **Step 5: Verify metadata and library tests pass**

```powershell
dotnet test "tests\EbookManager.Tests\EbookManager.Tests.csproj" --filter "FullyQualifiedName~LanguageDisplayServiceTests|FullyQualifiedName~LibraryViewModelTests" -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903
```

Expected: tests pass.

- [ ] **Step 6: Commit Task 4**

```powershell
git add src/EbookManager.Application/Metadata/LanguageDisplayService.cs src/EbookManager.Presentation/ViewModels/LibraryViewModel.cs tests/EbookManager.Tests/Metadata/LanguageDisplayServiceTests.cs tests/EbookManager.Tests/App/ViewModels/LibraryViewModelTests.cs
git commit -m "Extract language metadata display rules"
```

## Task 5: Restructure Settings Window Into Stable Sections

**Files:**
- Modify: `src/EbookManager.App/Views/SettingsWindow.xaml`
- Modify: `src/EbookManager.App/Resources/Strings/AppResources.resx`
- Modify: `src/EbookManager.App/Resources/Strings/AppResources.nl.resx`
- Modify: fallback resource files for German, French, Spanish, Italian

- [ ] **Step 1: Add resource keys**

Add the following keys to all `AppResources*.resx` files, with Dutch translations in `nl` and English fallback text in others if needed:

```xml
<data name="SettingsGeneralSection" xml:space="preserve"><value>General</value></data>
<data name="SettingsAppearanceSection" xml:space="preserve"><value>Appearance</value></data>
<data name="SettingsImportSection" xml:space="preserve"><value>Import</value></data>
<data name="SettingsMetadataSection" xml:space="preserve"><value>Metadata</value></data>
<data name="SettingsDuplicatesSection" xml:space="preserve"><value>Duplicates</value></data>
<data name="SettingsConfirmationsSection" xml:space="preserve"><value>Confirmations</value></data>
<data name="SettingsDiagnosticsSection" xml:space="preserve"><value>Diagnostics</value></data>
<data name="AuthorSortStrategy" xml:space="preserve"><value>Author sorting</value></data>
<data name="AuthorSortDisplayName" xml:space="preserve"><value>As entered</value></data>
<data name="AuthorSortLastNameFirst" xml:space="preserve"><value>Last name, first name</value></data>
<data name="AuthorSortLastNameFirstDutchPrefixes" xml:space="preserve"><value>Last name with prefixes, first name</value></data>
```

Dutch values:

```xml
<data name="SettingsGeneralSection" xml:space="preserve"><value>Algemeen</value></data>
<data name="SettingsAppearanceSection" xml:space="preserve"><value>Weergave</value></data>
<data name="SettingsImportSection" xml:space="preserve"><value>Import</value></data>
<data name="SettingsMetadataSection" xml:space="preserve"><value>Metadata</value></data>
<data name="SettingsDuplicatesSection" xml:space="preserve"><value>Duplicaten</value></data>
<data name="SettingsConfirmationsSection" xml:space="preserve"><value>Bevestigingen</value></data>
<data name="SettingsDiagnosticsSection" xml:space="preserve"><value>Diagnostiek</value></data>
<data name="AuthorSortStrategy" xml:space="preserve"><value>Auteurs sorteren</value></data>
<data name="AuthorSortDisplayName" xml:space="preserve"><value>Zoals ingevoerd</value></data>
<data name="AuthorSortLastNameFirst" xml:space="preserve"><value>Achternaam, voornaam</value></data>
<data name="AuthorSortLastNameFirstDutchPrefixes" xml:space="preserve"><value>Achternaam met tussenvoegsel, voornaam</value></data>
```

- [ ] **Step 2: Replace flat settings grid with sectioned layout**

Use a `TabControl` in `SettingsWindow.xaml` with tabs:

- General: culture.
- Appearance: theme, default view.
- Import: include subdirectories.
- Metadata: author sort strategy.
- Confirmations: confirm delete.
- Duplicates and Diagnostics: disabled text or empty section only if there is already a useful localized "future" label. If no such label exists, do not show empty tabs yet.

Use the existing save/cancel button row at the bottom.

For author sort:

```xml
<ComboBox ItemsSource="{Binding SelectableAuthorSortStrategies}"
          SelectedValue="{Binding AuthorSortStrategy}"
          SelectedValuePath="Value"
          Margin="0,0,0,12">
  <ComboBox.ItemTemplate>
    <DataTemplate>
      <TextBlock Text="{Binding ResourceKey, Converter={StaticResource ResourceKeyToStringConverter}}" />
    </DataTemplate>
  </ComboBox.ItemTemplate>
</ComboBox>
```

If no resource-key converter exists, use explicit `ComboBoxItem` values instead:

```xml
<ComboBox SelectedValue="{Binding AuthorSortStrategy}" SelectedValuePath="Tag">
  <ComboBoxItem Content="{loc:Loc AuthorSortDisplayName}" Tag="{x:Static settings:AuthorSortStrategy.DisplayName}" />
  <ComboBoxItem Content="{loc:Loc AuthorSortLastNameFirst}" Tag="{x:Static settings:AuthorSortStrategy.LastNameFirst}" />
  <ComboBoxItem Content="{loc:Loc AuthorSortLastNameFirstDutchPrefixes}" Tag="{x:Static settings:AuthorSortStrategy.LastNameFirstDutchPrefixes}" />
</ComboBox>
```

Add namespace:

```xml
xmlns:settings="clr-namespace:EbookManager.Domain.Settings;assembly=EbookManager.Domain"
```

- [ ] **Step 3: Build app to verify XAML**

```powershell
dotnet build "src\EbookManager.App\EbookManager.App.csproj" -c Release -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903
```

Expected: build succeeds.

- [ ] **Step 4: Commit Task 5**

```powershell
git add src/EbookManager.App/Views/SettingsWindow.xaml src/EbookManager.App/Resources/Strings/AppResources*.resx
git commit -m "Organize settings into metadata sections"
```

## Task 6: Standard Metadata Surface Review

**Files:**
- Inspect: `src/EbookManager.App/Views/MainWindow.xaml`
- Inspect: `src/EbookManager.Presentation/ViewModels/BookDetailsViewModel.cs`
- Modify only if fields are missing from UI bindings.
- Test: `tests/EbookManager.Tests/App/ViewModels/BookDetailsViewModelTests.cs`

- [ ] **Step 1: Confirm existing standard fields in details pane**

Check that the details pane shows and binds:

- title;
- authors;
- formats;
- description;
- language;
- publisher;
- publication date;
- tags;
- series;
- series number;
- ISBN;
- reading status.

If a field exists in `BookDetailsViewModel` but not in XAML, add it to the details pane under a clear label.

- [ ] **Step 2: Add or update a details test**

If a field binding requires a ViewModel change, add a test to `BookDetailsViewModelTests`:

```csharp
[Fact]
public void Load_exposes_standard_metadata_fields()
{
    var book = CreateBook(new BookMetadata(
        "Title",
        ["Author"],
        "Description",
        "nl",
        "Publisher",
        new DateOnly(2020, 1, 2),
        ["Tag"],
        "Series",
        1,
        "9780000000000"));
    var viewModel = new BookDetailsViewModel(new FakeBookService());

    viewModel.Load(book);

    viewModel.Publisher.Should().Be("Publisher");
    viewModel.PublicationDate.Should().Be(new DateOnly(2020, 1, 2));
    viewModel.Isbn.Should().Be("9780000000000");
    viewModel.FormatsText.Should().NotBeNull();
}
```

Use the existing helpers in `BookDetailsViewModelTests` rather than introducing a new fake if one already exists.

- [ ] **Step 3: Verify details tests**

```powershell
dotnet test "tests\EbookManager.Tests\EbookManager.Tests.csproj" --filter "FullyQualifiedName~BookDetailsViewModelTests" -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903
```

Expected: tests pass.

- [ ] **Step 4: Commit Task 6 if changes were needed**

```powershell
git add src/EbookManager.App/Views/MainWindow.xaml src/EbookManager.Presentation/ViewModels/BookDetailsViewModel.cs tests/EbookManager.Tests/App/ViewModels/BookDetailsViewModelTests.cs
git commit -m "Expose standard metadata details consistently"
```

If no changes were needed, do not create an empty commit.

## Task 7: Documentation And Manual Verification

**Files:**
- Create: `docs/manual-tests/milestone-4-checklist.md`
- Modify: `README.md`
- Mirror Markdown files to `C:\Data\Obsidian\markdown\Development\HNSoftwareDevelopment\Ebook Manager`

- [ ] **Step 1: Create manual checklist**

Create `docs/manual-tests/milestone-4-checklist.md`:

```markdown
# Milestone 4 Manual Test Checklist

## Settings

- [ ] Open Settings and confirm sections are understandable.
- [ ] Change theme and confirm it still applies after Save.
- [ ] Change default view and confirm it still applies after restart.
- [ ] Change include-subdirectories setting and confirm scan uses it.
- [ ] Change author sort strategy and confirm it persists after reopening Settings.

## Author Sorting

- [ ] Select author sort "Zoals ingevoerd" and confirm author filter order follows visible names.
- [ ] Select author sort "Achternaam, voornaam" and confirm authors sort by last name.
- [ ] Select author sort with Dutch prefixes and confirm names like "Vincent van Gogh" sort under "van Gogh".
- [ ] Confirm author names themselves are not rewritten.

## Language Display

- [ ] Confirm language filter shows friendly display for `nl`, `nl-NL`, `eng`, and `en-US`.
- [ ] Confirm selecting a normalized language filter still filters the expected books.
- [ ] Confirm unusual valid codes such as `lv` do not crash.

## Standard Metadata

- [ ] Select a book and verify publisher, publication date, ISBN, tags, series, formats, and description are visible where expected.
- [ ] Edit publisher, publication date, ISBN, and tags; save; confirm grid/filter/details refresh correctly.
- [ ] Merge duplicates and confirm the existing metadata merge screen still works.
```

- [ ] **Step 2: Update README manual verification links**

Add:

```markdown
- [docs/manual-tests/milestone-4-checklist.md](docs/manual-tests/milestone-4-checklist.md)
```

under Manual Verification.

Update Current Status with:

```markdown
- structured settings foundation for metadata preferences
- settings-driven author sorting without per-book author-sort metadata
- reusable language display normalization
```

- [ ] **Step 3: Mirror Markdown files to Obsidian**

```powershell
$files = @(
  "README.md",
  "docs\manual-tests\milestone-4-checklist.md",
  "docs\superpowers\plans\2026-07-10-saga-milestone-4-metadata-settings-foundation.md"
)
foreach ($source in $files) {
  $target = Join-Path "C:\Data\Obsidian\markdown\Development\HNSoftwareDevelopment\Ebook Manager" $source
  New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
  Copy-Item -LiteralPath $source -Destination $target -Force
}
```

- [ ] **Step 4: Verify full suite and publish test build**

```powershell
dotnet test "EbookManager.sln" -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903
dotnet publish "src\EbookManager.App\EbookManager.App.csproj" -c Release -o "artifacts\publish-milestone-4-foundation" -p:TreatWarningsAsErrors=false -p:WarningsNotAsErrors=NU1903
```

Expected: tests pass and publish succeeds with only the known `SQLitePCLRaw` NU1903 warning.

- [ ] **Step 5: Commit Task 7**

```powershell
git add README.md docs/manual-tests/milestone-4-checklist.md docs/superpowers/plans/2026-07-10-saga-milestone-4-metadata-settings-foundation.md
git commit -m "Document milestone 4 foundation testing"
```

## Self-Review

Spec coverage:

- Metadata inventory is already created and referenced by this plan.
- Settings structure is implemented through sectioned settings UI and settings model changes.
- Author sort is implemented as a strategy, not stored metadata.
- Language normalization is extracted and tested.
- Standard metadata visibility is reviewed without forcing unnecessary schema work.
- Custom columns remain out of scope.

Completeness scan:

- No unfinished markers should remain in this plan.
- Later-version items are explicitly marked as excluded or candidate decisions.

Type consistency:

- `AuthorSortStrategy` is defined in `EbookManager.Domain.Settings`.
- `AppSettings.AuthorSortStrategy` uses that enum.
- `AuthorSortKeyBuilder.BuildSortKey(string?, AuthorSortStrategy)` is used by `LibraryViewModel`.
- `LanguageDisplayService.FilterKey` and `DisplayName` replace the private language helper methods.
