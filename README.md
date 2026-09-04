# Saga

Native Windows desktop ebook library manager built with .NET 10, WPF, SQLite, CommunityToolkit.Mvvm, EF Core, and Syncfusion WPF DataGrid.

## Current Status

Milestone 33 builds on version `0.1` with an explicit Open Library search and selection workflow for books without a cover, alongside the existing metadata quality repair actions.

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
- duplicate merge currently updates SQLite but not the portable `metadata.json` sidecar; this remaining consistency gap is tracked in [issue #1](https://github.com/hnsoftwaredevelopment/saga/issues/1)
- custom metadata fields with Calibre custom-column import
- customizable column visibility, saved grid layouts, and user-defined views
- multi-book metadata editing and cleanup actions for facets such as authors and tags
- metadata quality dashboard with issue counts, affected-book lists, direct navigation, and a reversible `This is correct` decision per book and signal
- management of ignored quality issues from Settings, without changing the book metadata itself
- direct missing-author repair from the quality dashboard with known-author suggestions and free entry for a new author
- direct missing- or unknown-language repair from the quality dashboard with a searchable list of valid languages
- direct missing-series repair with live suggestions while preserving the existing series number
- confirmed title-and-author swap repair with a clear before-and-after preview
- explicit missing-cover search with up to twelve validated Open Library choices and safe managed cover storage
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

SQLite is authoritative for metadata inside the application. Metadata edits are also written to a portable `metadata.json` sidecar file next to the managed book file, except duplicate merges: those currently update SQLite only, pending [issue #1](https://github.com/hnsoftwaredevelopment/saga/issues/1).

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

## Later-Version Candidates

The following remain later-version candidates:

- active e-reader detection and USB sync
- native metadata write-back into ebook files
- details-pane cover picker for CBR files that can extract the first image from the archive and use it as cover
- optional cloud-file hydration for OneDrive files that are not available locally
- direct repair for messy tags from the metadata quality dashboard
- ebook conversion
- full-text search inside book contents
- in-app bug reports and feature requests that can prepare or create GitHub issues after user review
