# Saga

Native Windows desktop ebook library manager built with .NET 10, WPF, SQLite, CommunityToolkit.Mvvm, EF Core, and Syncfusion WPF DataGrid.

## Current Status

Milestone 5 builds on version `0.1` with standard metadata enrichment, system metadata visibility, and the completed metadata-settings foundation from Milestone 4.

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
- per-format open-folder action from the details pane
- description cleanup for common HTML-like metadata
- standard metadata search across language display names, dates, series numbers, formats, and system dates
- filter context-menu cleanup for authors, series, tags, and languages
- WPF workspace with bookshelf, detailed grid, and list views
- light, dark, sepia, blue, and red themes
- English, Dutch, German, French, Spanish, and Italian selectable UI localization
- toolbar and details action icons with localized tooltips

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

SQLite is authoritative for metadata inside the application. Metadata edits are also written to a portable `metadata.json` sidecar file next to the managed book file.

During import, metadata is resolved in this order:

1. Saga `metadata.json` sidecar next to the source file.
2. Calibre `metadata.opf` and `cover.jpg` sidecars next to the source file.
3. Embedded format metadata, strongest for EPUB and KEPUB.
4. Filename fallback.

Native write-back into ebook files remains a later opt-in feature per format, starting with EPUB only after representative safety tests.

## Manual Verification

Use these manual test checklists:

- [docs/manual-tests/milestone-2-checklist.md](docs/manual-tests/milestone-2-checklist.md)
- [docs/manual-tests/milestone-3-checklist.md](docs/manual-tests/milestone-3-checklist.md)
- [docs/manual-tests/milestone-3-1-checklist.md](docs/manual-tests/milestone-3-1-checklist.md)
- [docs/manual-tests/milestone-4-checklist.md](docs/manual-tests/milestone-4-checklist.md)
- [docs/manual-tests/milestone-5-checklist.md](docs/manual-tests/milestone-5-checklist.md)

## Later-Version Candidates

The following remain later-version candidates:

- active e-reader detection and USB sync
- native metadata write-back into ebook files
- custom metadata fields
- duplicate finder for possible duplicates, with options to delete one copy or merge metadata
- details-pane cover picker for CBR files that can extract the first image from the archive and use it as cover
- optional cloud-file hydration for OneDrive files that are not available locally
- per-format download/export action from the details pane
- richer metadata cleanup tools, including duplicate-aware merge flows and more bulk-edit diagnostics
- DataGrid makeover with Saga-aware column sorting, grouping, and view-specific grid behavior
- user-defined views
- ebook conversion
- full-text search inside book contents
- in-app bug reports and feature requests that can prepare or create GitHub issues after user review
