# Saga

Native Windows desktop ebook library manager built with .NET 10, WPF, SQLite, CommunityToolkit.Mvvm, EF Core, and Syncfusion WPF DataGrid.

## Current Status

Milestone 52 adds **Manage tags** next to Authors and Series in the main toolbar. Search used tags with book counts and browse linked books. **Change tag** and **Remove tag** apply only to explicitly selected books; **Rename tag everywhere** is a separate global action, including combining with an existing target without book selection. Replacing with an existing target reuses that tag; when the book already has it, only the old tag is removed. Other tags/order are preserved, and selection actions leave unselected books untouched. Source/target lists and counts refresh immediately. Transactions and existing file synchronization provide safe results and separate warnings, without schema changes. **1054 tests and 20 interactive WPF scenarios pass**; clean Debug build **2026.10.29.51**, 29 October **16:05:02 Europe/Amsterdam**, is ready for the [targeted manual checks](docs/manual-tests/milestone-52-checklist.md). The user accepted the complete slice, including the selection actions and global existing-target merge, on 29 October 2026. PR #54 awaits merge by the user. The [author-variant merge workflow](docs/feature-requests/author-variants.md) remains a later feature. Milestone 51 is accepted and merged in PR #53; its history follows.

**Milestone 51 verification (historical, 29 October 2026):** when a book belongs to multiple tag groups, vertical navigation preserves the intended group as well as the book ID. Focus and scrolling are scoped to that group's tile list. A failing WPF regression reproduced the wrong-occurrence focus; after correction **993 tests and all 15 WPF scenarios pass**, including verification against the normal Debug build. Historical build **2026.10.29.9**, placed at **08:59 Europe/Amsterdam**, had 0 warnings/errors and was the only Saga.exe under Builds at that point. Earlier build/test figures below are historical context. The user has now tested and accepted all remaining checklist items, including duplicate-tag focus. Own review found no additional issues; CodeQL and GitGuardian are green. CodeRabbit reached its review allowance, so the latest fix relies on the own review, regression tests and user acceptance.

**Milestone 51 review history, 29 October 2026:** grouped Bookshelf Up/Down navigation now calculates tile rows within each group, preventing partial rows from skipping the first books of the next group. Shift uses the corrected target and the existing unique global range order. **993 tests and 14 interactive WPF scenarios pass**; clean Debug build **2026.10.29.5** was placed at **08:15 Europe/Amsterdam**. The targeted grouped-keyboard recheck was subsequently accepted in the Milestone 51 checklist. The previously accepted selection/metadata workflow remains recorded below.

**Milestone 51 acceptance history, 27 October 2026:** multi-selection and metadata editing now work as expected in all three views, including the grouped-row correction in build 2026.10.27.34. Detailed checks were not individually confirmed at that point; all remaining checks were accepted on 29 October and PR #53 is merged. The implementation history follows.

Milestone 51 implementation history: added multi-selection to the **Bookshelf** cover tiles: click/Ctrl/Shift/Ctrl+Shift, visible selection and keyboard focus, a selected-book count and use of the existing metadata action in the main toolbar. The Bookshelf has no duplicate edit button. The user confirmed cross-group Bookshelf selection and reported single-selection in grouped List/Detailed views. That omission is fixed in build **2026.10.27.34**: grouped rows now share the same unique book selection, mouse modifiers and keyboard navigation, with visible selection/focus borders. Each view retains its own grouping while selection follows book IDs. Filtering removes hidden results without restoring them when search is cleared. **993 tests** and **thirteen interactive WPF scenarios** pass. Manual acceptance was partially open at this intermediate build; the later navigation corrections and all remaining checks are now accepted. See the [bookshelf selection design](docs/superpowers/specs/2026-10-27-saga-milestone-51-bookshelf-selection-design.md) and [accepted Milestone 51 checklist](docs/manual-tests/milestone-51-checklist.md).

Milestone 50 adds **Manage series** to the main toolbar: search series with book counts, view books in numeric series order and explicitly rename an entire series. Existing destination names are blocked; series numbers and other book metadata are preserved. Renaming uses one database transaction, followed by the existing book-file synchronization with separate warnings. No schema migration or folder moves are required. The user has accepted the complete workflow, including the keyboard-focus correction in Debug build **2026.10.24.61**. All 980 automated tests and five interactive WPF focus scenarios pass. PR #51 is merged; Milestone 50 is complete. See the [series design](docs/superpowers/specs/2026-10-24-saga-milestone-50-series-management-design.md).

Milestone 49 is fully user-accepted and merged in PR #50: multi-book metadata editing provides concise success feedback, localized per-book failure and file-warning details, preservation of completed updates, and selection of failed/partially updated visible books for a deliberate next attempt. Authors, series, tags and language are selected when text is entered and deselected when cleared; manually selecting an empty field still requests clearing. Tags is directly editable. Review corrections preserve feedback when cancelling, update command availability during other cleanups and distinguish refresh failures from uncertain storage. The final cancellation check is accepted; title/author swaps are never automatically replayed.

Milestone 48 adds library-local author management: searchable authors and their books, manually maintained biography and website, and portable author photos. Explicit author renaming updates all linked books while preserving the author identity and profile. An existing destination name is blocked, never merged; book details still assign authors per book. Profiles survive removal of the last linked book. The user accepted profiles, renaming, the supplied author icon and window resizing, including an author with 318 books. Review follow-ups retain the renamed author in search results and refresh affected books in bounded batches while preserving loaded covers.

Milestone 47's final quality reliability improvements are manually checked and accepted, with the unreproduced conflict scenario covered by automated tests: safe restoration errors, stable selection while saving and removal of old repair feedback after This is correct. Milestone 46 is manually accepted and merged: all six quality repair routes provide localized per-book error details without a noticeable slowdown reported by the user. Milestone 45's bulk title/author swap and conservative detection, and Milestone 44's per-book tag cleanup, are also accepted and merged.

- portable local ebook libraries with `library.db`
- import pipeline with duplicate detection
- multi-format import that attaches different file formats to the same book when title and author match
- import result suggestions for title-only matches when author metadata is unreliable
- details pane shows the available formats for the selected book
- EPUB metadata and cover extraction
- EPUB subject/tag and Calibre-style series metadata extraction
- Calibre `metadata.opf` sidecar import
- Calibre `cover.jpg` sidecar import
- background import progress for large scans
- live library refresh during imports
- cancel and close-warning behavior for active imports
- conservative title, author, and series cleanup
- CBZ cover extraction
- safe fallback import for recognized formats
- drag-and-drop import
- faceted sorting and filters in the left action list
- book type filtering for EPUB, PDF, CBR, CBZ, MOBI, AZW, AZW3, and KFX
- search-term highlighting
- refined import result summaries
- import result diagnostics with per-item duration, file size, and format
- searchable and outcome-filtered import result details with sortable diagnostic columns
- retry action for failed import items whose original source files are still available
- portable `metadata.json` sidecar metadata
- searchable library viewmodels
- responsive large-library search with reusable search values and delayed filtering of rapid input
- asynchronous, bounded cover-thumbnail loading in bookshelf, detailed, and list views
- editable metadata details with save, undo, and delete services
- structured settings foundation for metadata preferences
- settings-driven author sorting without per-book author-sort metadata
- reusable language display normalization
- explicit language-code normalization for supported Saga languages
- localized language filter and details display while preserving stable stored language codes
- read-only details display for date added and last modified system metadata
- per-format details rows with managed file sizes
- per-format open-file action from the details pane
- per-format open-folder action from the details pane
- per-format save actions from the details pane with localized feedback
- per-format remove action that never deletes the last remaining book format
- iconized per-format context menu with warning color for destructive actions
- description cleanup for common HTML-like metadata
- standard metadata search across language display names, dates, series numbers, formats, and system dates
- sectioned settings foundation for duplicates and diagnostics preferences
- duplicate merge default actions prepared in Settings and applied to merge previews
- duplicate finder with compare, delete, field-by-field merge, and a separate `Geen duplicaat` exclusion workflow for possible matches
- duplicate merge keeps SQLite and every relevant portable `metadata.json` sidecar synchronized, deduplicates writes per book folder, and reports sidecar warnings without misrepresenting the completed merge
- custom metadata fields with Calibre custom-column import
- customizable column visibility, saved grid layouts, and user-defined views
- multi-book metadata editing and cleanup actions for facets such as authors and tags
- metadata quality dashboard with issue counts, affected-book lists, direct navigation, and reversible `This is correct` decisions for one or multiple selected books within one signal
- management of ignored quality issues from a dedicated Quality tab in Settings, without changing the book metadata itself
- direct missing-author repair from the quality dashboard with known-author suggestions and free entry for a new author
- bulk missing-author repair that applies one chosen author to multiple selected books while preserving per-book failures and file warnings
- direct missing- or unknown-language repair from the quality dashboard with a searchable list of valid languages
- bulk missing- or unknown-language repair that applies one chosen language to multiple selected books and refreshes the library and language filters together
- direct missing-series repair with live suggestions while preserving the existing series number
- bulk missing-series repair that applies one chosen series name while preserving each selected book's own series number
- confirmed title-and-author swap repair with a clear before-and-after preview
- bulk title-and-author swap using the quality-list selection, one compact confirmation and protection against changed title/author values
- detail-pane title-and-author swap for any editable book, with staged Save and Undo behavior and immediate library refresh after saving
- editable messy-tag repair with a one-tag-per-line proposal, deduplication, whitespace cleanup, and support for an empty final tag list
- bulk messy-tag cleanup with a per-book before/after preview, preservation of each book's own tags, and immediate library and category-filter refresh
- explicit cover search with up to twelve validated Google Books and Open Library choices, a locally generated fallback, safe managed storage, and replacement from book details
- managed ebooks and covers distributed over 256 ID-based storage shards, with automatic database backup, visible first-start migration progress, and safe resume after interruption
- delete actions continue removing library records when managed file cleanup reports a warning
- filter context-menu cleanup for authors, series, tags, and languages
- WPF workspace with bookshelf, detailed grid, and list views
- light, dark, sepia, blue, and red themes
- English, Dutch, German, French, Spanish, and Italian selectable UI localization
- toolbar and details action icons with localized tooltips
- Syncfusion-based compact List view with real columns, sorting, and grouping surface
- Detailed view exposes the standard details-pane metadata fields as grid columns
- import diagnostics with phase timings for hashing, metadata, duplicate checks, copying, database save, and cleanup
- aggregate import phase summaries in import result details

The current manual acceptance steps are in the [Milestone 52 checklist](docs/manual-tests/milestone-52-checklist.md). Accepted multi-selection checks remain in the [Milestone 51 checklist](docs/manual-tests/milestone-51-checklist.md). Accepted series-management checks remain in the [Milestone 50 checklist](docs/manual-tests/milestone-50-checklist.md). Accepted bulk-edit checks remain in the [Milestone 49 checklist](docs/manual-tests/milestone-49-checklist.md); accepted author-management checks remain in the [author rename checklist](docs/manual-tests/author-rename-checklist.md) and [Milestone 48 checklist](docs/manual-tests/milestone-48-checklist.md).

## Prerequisites

- Windows
- .NET SDK 10
- Visual Studio 2026 or another IDE that supports .NET 10 WPF
- Syncfusion WPF license

Set the Syncfusion key in your user environment before running the app:

```powershell
[Environment]::SetEnvironmentVariable("SYNCFUSION_LICENSE_KEY", "<your-key>", "User")
```

Alternatively, place the key in `docs/SynfusionLicense.txt` or `docs/SyncfusionLicense.txt` for local development. These files are ignored by Git. Do not commit a Syncfusion license key.

## Build And Test

```powershell
dotnet restore EbookManager.sln
dotnet test EbookManager.sln
dotnet build EbookManager.sln
```

The ready-to-run Debug build is stored under `Builds` in the repository root. Internal compiler and test output remains in the ignored `.build` folder:

- Debug app: `Builds\Debug\Saga.exe`
- Future installer input: `Builds\Publish`

Create the future publish output with:

```powershell
dotnet publish src/EbookManager.App/EbookManager.App.csproj -c Release
```

Run the desktop app:

```powershell
dotnet run --project src/EbookManager.App/EbookManager.App.csproj
```

## Supported Import Formats

Version `0.1` recognizes:

- EPUB
- KEPUB
- PDF
- CBR
- CBZ
- MOBI
- AZW
- AZW3
- KFX

Metadata extraction is intentionally conservative:

- EPUB and KEPUB: OPF metadata and embedded cover where available
- EPUB and KEPUB: `dc:subject` tags and Calibre-style series metadata where available
- CBZ: first supported image as cover, filename fallback for title/author
- PDF, CBR, MOBI, AZW, AZW3, and KFX: safe import with filename fallback

SQLite is authoritative for metadata inside the application. Metadata edits are also written to a portable `metadata.json` sidecar file next to the managed book file. Duplicate merges synchronize the definitive target metadata with the sidecars in every relevant book folder; a sidecar error is reported without disguising or rolling back the completed database merge.

During import, metadata is resolved in this order:

1. Saga `metadata.json` sidecar next to the source file.
2. Calibre `metadata.opf` and `cover.jpg` sidecars next to the source file.
3. Embedded format metadata, strongest for EPUB and KEPUB.
4. Filename fallback.

Native write-back into ebook files remains a later opt-in feature per format, starting with EPUB only after representative safety tests.

## Manual Verification

Use these manual test checklists:

- [docs/manual-tests/milestone-1-checklist.md](docs/manual-tests/milestone-1-checklist.md)
- [docs/manual-tests/milestone-2-checklist.md](docs/manual-tests/milestone-2-checklist.md)
- [docs/manual-tests/milestone-3-checklist.md](docs/manual-tests/milestone-3-checklist.md)
- [docs/manual-tests/milestone-3-1-checklist.md](docs/manual-tests/milestone-3-1-checklist.md)
- [docs/manual-tests/milestone-4-checklist.md](docs/manual-tests/milestone-4-checklist.md)
- [docs/manual-tests/milestone-5-checklist.md](docs/manual-tests/milestone-5-checklist.md)
- [docs/manual-tests/milestone-6-checklist.md](docs/manual-tests/milestone-6-checklist.md)
- [docs/manual-tests/milestone-7-checklist.md](docs/manual-tests/milestone-7-checklist.md)
- [docs/manual-tests/milestone-8-checklist.md](docs/manual-tests/milestone-8-checklist.md)
- [docs/manual-tests/milestone-9-checklist.md](docs/manual-tests/milestone-9-checklist.md)
- [docs/manual-tests/milestone-10-checklist.md](docs/manual-tests/milestone-10-checklist.md)
- [docs/manual-tests/milestone-11-checklist.md](docs/manual-tests/milestone-11-checklist.md)
- [docs/manual-tests/milestone-12-checklist.md](docs/manual-tests/milestone-12-checklist.md)
- [docs/manual-tests/milestone-13-checklist.md](docs/manual-tests/milestone-13-checklist.md)
- [docs/manual-tests/milestone-14-checklist.md](docs/manual-tests/milestone-14-checklist.md)
- [docs/manual-tests/milestone-15-checklist.md](docs/manual-tests/milestone-15-checklist.md)
- [docs/manual-tests/milestone-16-checklist.md](docs/manual-tests/milestone-16-checklist.md)
- [docs/manual-tests/milestone-18-checklist.md](docs/manual-tests/milestone-18-checklist.md)
- [docs/manual-tests/milestone-19-checklist.md](docs/manual-tests/milestone-19-checklist.md)
- [docs/manual-tests/milestone-20-checklist.md](docs/manual-tests/milestone-20-checklist.md)
- [docs/manual-tests/milestone-21-checklist.md](docs/manual-tests/milestone-21-checklist.md)
- [docs/manual-tests/milestone-22-checklist.md](docs/manual-tests/milestone-22-checklist.md)
- [docs/manual-tests/milestone-23-checklist.md](docs/manual-tests/milestone-23-checklist.md)
- [docs/manual-tests/milestone-24-checklist.md](docs/manual-tests/milestone-24-checklist.md)
- [docs/manual-tests/milestone-25-checklist.md](docs/manual-tests/milestone-25-checklist.md)
- [docs/manual-tests/milestone-26-checklist.md](docs/manual-tests/milestone-26-checklist.md)
- [docs/manual-tests/milestone-27-checklist.md](docs/manual-tests/milestone-27-checklist.md)
- [docs/manual-tests/milestone-28-checklist.md](docs/manual-tests/milestone-28-checklist.md)
- [docs/manual-tests/milestone-29-checklist.md](docs/manual-tests/milestone-29-checklist.md)
- [docs/manual-tests/milestone-30-checklist.md](docs/manual-tests/milestone-30-checklist.md)
- [docs/manual-tests/milestone-31-checklist.md](docs/manual-tests/milestone-31-checklist.md)
- [docs/manual-tests/milestone-32-checklist.md](docs/manual-tests/milestone-32-checklist.md)
- [docs/manual-tests/milestone-33-checklist.md](docs/manual-tests/milestone-33-checklist.md)
- [docs/manual-tests/milestone-34-checklist.md](docs/manual-tests/milestone-34-checklist.md)
- [docs/manual-tests/milestone-35-checklist.md](docs/manual-tests/milestone-35-checklist.md)
- [docs/manual-tests/milestone-36-checklist.md](docs/manual-tests/milestone-36-checklist.md)
- [docs/manual-tests/milestone-37-checklist.md](docs/manual-tests/milestone-37-checklist.md)
- [docs/manual-tests/milestone-38-checklist.md](docs/manual-tests/milestone-38-checklist.md)
- [docs/manual-tests/milestone-39-checklist.md](docs/manual-tests/milestone-39-checklist.md)
- [docs/manual-tests/milestone-40-checklist.md](docs/manual-tests/milestone-40-checklist.md)
- [docs/manual-tests/milestone-41-checklist.md](docs/manual-tests/milestone-41-checklist.md)
- [docs/manual-tests/milestone-42-checklist.md](docs/manual-tests/milestone-42-checklist.md)
- [docs/manual-tests/milestone-43-checklist.md](docs/manual-tests/milestone-43-checklist.md)
- [docs/manual-tests/milestone-44-checklist.md](docs/manual-tests/milestone-44-checklist.md)
- [docs/manual-tests/milestone-45-checklist.md](docs/manual-tests/milestone-45-checklist.md)
- [docs/manual-tests/milestone-46-checklist.md](docs/manual-tests/milestone-46-checklist.md)
- [docs/manual-tests/milestone-47-checklist.md](docs/manual-tests/milestone-47-checklist.md)
- [docs/manual-tests/milestone-48-checklist.md](docs/manual-tests/milestone-48-checklist.md)
- [docs/manual-tests/milestone-49-checklist.md](docs/manual-tests/milestone-49-checklist.md)
- [docs/manual-tests/milestone-50-checklist.md](docs/manual-tests/milestone-50-checklist.md)
- [docs/manual-tests/milestone-51-checklist.md](docs/manual-tests/milestone-51-checklist.md)
- [docs/manual-tests/milestone-52-checklist.md](docs/manual-tests/milestone-52-checklist.md)

## Later-Version Candidates

The following remain later-version candidates:

- active e-reader detection and USB sync
- native metadata write-back into ebook files
- details-pane cover picker for CBR files that can extract the first image from the archive and use it as cover
- optional cloud-file hydration for OneDrive files that are not available locally
- ebook conversion
- full-text search inside book contents
- in-app bug reports and feature requests that can prepare or create GitHub issues after user review
