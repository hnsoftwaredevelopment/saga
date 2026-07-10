# Saga Standard Metadata Inventory

## Purpose

This document compares useful Calibre metadata with Saga's current and proposed standard metadata model. Calibre is used as a reference, not as a blueprint. Saga should implement fields only when they make the application more useful for importing, browsing, filtering, searching, duplicate handling, or editing.

Custom columns are intentionally excluded from this inventory. They belong to a separate future milestone because they require a dedicated data model, settings design, import strategy, and UI behavior.

## Decision Status Values

- **Keep**: already supported and remains part of Saga standard metadata.
- **Improve**: already supported, but display, cleanup, search, filtering, or editing needs refinement.
- **Candidate**: useful, but not yet accepted for implementation.
- **Later**: explicitly useful later, but out of scope for Milestone 4 implementation.
- **No**: not planned as standard Saga metadata.

## Inventory

| Field | Calibre Equivalent | Current Saga Support | Source Candidates | Proposed Status | UI Surface | Editable | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Title | `title` | Supported | EPUB/KEPUB OPF, Calibre `metadata.opf`, Saga `metadata.json`, filename fallback | Keep | Bookshelf, grid, list, details, search, duplicate detection, duplicate merge | Yes | Core identity field. |
| Authors | `authors` | Supported | EPUB/KEPUB OPF, Calibre `metadata.opf`, Saga `metadata.json`, filename fallback | Keep | Grid, list, details, author filter, search, duplicate detection, duplicate merge | Yes | Multi-value. Needs careful cleanup and bulk rename/remove. |
| Author sort strategy | `author_sort`, author sort values | Not supported | Derived from authors, optional settings rule | Improve | Sorting, author filter ordering, settings | No per book | Implement as a library/application sorting strategy instead of storing an author-sort value on every book. |
| Series | `series` | Supported | Calibre `metadata.opf`, Saga `metadata.json`, EPUB Calibre-style metadata, bracketed title cleanup | Keep | Details, series filter, search, duplicate merge | Yes | Important for real libraries. |
| Series number | `series_index` | Supported | Calibre `metadata.opf`, Saga `metadata.json`, EPUB Calibre-style metadata, bracketed title cleanup | Keep | Details, sorting later, duplicate merge | Yes | Must remain numeric. Ignore non-numeric source values. |
| Tags | `tags` | Supported | Calibre `metadata.opf` subjects, EPUB subjects, Saga `metadata.json` | Improve | Details, tags filter, search, duplicate merge | Yes | Needs better bulk cleanup and consistent display. |
| Language | `languages` | Supported | EPUB/KEPUB OPF, Calibre `metadata.opf`, Saga `metadata.json` | Improve | Details, language filter, search, duplicate merge | Yes | Display should show friendly names such as Nederlands/Engels while preserving safe stored values. |
| Publisher | `publisher` | Supported internally | EPUB/KEPUB OPF, Calibre `metadata.opf`, Saga `metadata.json` | Improve | Details, search, duplicate merge | Yes | Already in domain/persistence; make sure UI behavior is consistent. |
| Publication date | `pubdate`, `date` | Supported internally | EPUB/KEPUB OPF, Calibre `metadata.opf`, Saga `metadata.json` | Improve | Details, search/sort later, duplicate merge | Yes | Date parsing must remain conservative. |
| ISBN | `identifiers` with ISBN scheme | Supported as single field | EPUB/KEPUB OPF identifiers, Calibre `metadata.opf`, Saga `metadata.json` | Improve | Details, search, duplicate merge | Yes | Keep one primary ISBN for now. Full identifiers list is separate. |
| Identifiers | `identifiers` | Not supported as list | Calibre DB, OPF identifiers, future Saga `metadata.json` | Candidate | Details later, external lookup later | Later | Needs scheme/value model, e.g. ISBN, DOI, Amazon, Goodreads. |
| Rating | `rating` | Not supported | Calibre DB, future Saga `metadata.json` | Candidate | Details, filter, sort later | Later | Useful, but not essential for the first metadata foundation. |
| Description | `comments` | Supported | EPUB/KEPUB OPF, Calibre `metadata.opf`, Saga `metadata.json` | Improve | Details, search, duplicate merge | Yes | HTML/RTF-like cleanup should improve display without damaging content. |
| Cover | `cover` | Supported | EPUB/KEPUB embedded cover, Calibre `cover.jpg`, CBZ first image, Saga managed cover | Improve | Bookshelf, grid, details, duplicate details, duplicate merge | Replace later | CBR/CBZ cover picker is a future feature. |
| Formats | `formats` | Supported | Managed library files | Keep | Grid/list optional, details, format filter, duplicate merge | No direct edit | Managed through import, link format, duplicate merge, delete format later. |
| File path | format path | Supported internally | Managed library files | Improve | Details later, diagnostics, open folder action later | No | Useful as system metadata, not normal editable metadata. |
| File size | format size | Import diagnostics only | Managed library files, source files | Candidate | Details/diagnostics later, duplicate comparison later | No | Useful for duplicate decisions and storage overview. |
| Date added | `timestamp` | Partly supported as `CreatedAt` | Saga import timestamp, Calibre DB if imported later | Candidate | Details/system info later, sort later | No | Should be system-owned. |
| Last modified | `last_modified` | Partly supported as `UpdatedAt` | Saga updates, Calibre DB if imported later | Candidate | Details/system info later, sort later | No | Should update when Saga changes metadata. |
| Reading status | Calibre custom/status-like workflows vary | Supported | Saga user edits | Keep | Grid, list, details, status filter | Yes | Saga-owned field. Not a direct Calibre standard field. |
| E-reader state | Device view/state | Basic column/filter concept exists | Future device scan | Later | Grid, list, filter | No direct edit | Belongs to e-reader milestone. |
| Author links | author link map | Not supported | Calibre DB | Later | Author management later | Later | Nice but not needed yet. |
| Publisher sort | none/direct sorting | Not supported | Derived | No | Not planned | No | Can use publisher text if needed. |
| Title sort | `sort` | Not supported | Calibre DB, derived from title | Candidate | Sorting later | Later | Useful for sorting articles such as The/A/De; not urgent. |
| Series sort | `series_sort` | Not supported | Calibre DB, derived from series | Later | Sorting/filter display later | Later | Depends on broader sort model. |
| Original publication date | not always standard | Not supported | External metadata, custom Calibre fields | Later | Details later | Later | Useful for some users, but out of first standard pass. |
| Edition | not always standard | Not supported | External metadata, identifiers, manual entry | Later | Details later | Later | Avoid unless clear user need emerges. |
| Rights | `rights` | Not supported | OPF | No | Not planned | No | Rarely useful for personal library management. |
| Contributors | `contributors` | Not supported | OPF | Later | Details later | Later | Translators/editors could matter, but not first pass. |
| Subjects | `subject` | Mapped to tags | EPUB/OPF subjects | Keep as Tags | Tags filter/details | Yes | Saga should treat subjects as tags for now. |
| Comments/source attribution | comments/source text | Partly in description | OPF description text | Improve as Description | Details | Yes | Avoid adding separate source field now. |
| Custom columns | `#column_name` | Not supported | Calibre DB/custom metadata | Later | Separate milestone | Later | Explicitly excluded from Milestone 4. |
| Virtual libraries | saved searches | Not supported | Calibre DB | Later | Separate feature | Later | Related to advanced filters/views, not metadata foundation. |
| User categories | grouped tag browser categories | Not supported | Calibre DB | Later | Separate feature | Later | Depends on custom organization model. |
| Data files | Calibre data files | Not supported | Calibre library folders | Later | File management later | Later | Not needed for core ebook library use. |

## Recommended Milestone 4 Decisions

Milestone 4 should implement or refine only the low-risk standard metadata decisions:

1. Keep current core fields: title, authors, series, series number, tags, language, publisher, publication date, ISBN, description, cover, formats, reading status.
2. Improve language display and filtering without destroying the original imported value.
3. Improve description cleanup for common HTML wrappers and excessive whitespace.
4. Make existing publisher, publication date, and ISBN behavior visible and consistent in details, search, duplicate merge, and sidecar persistence.
5. Keep identifiers as a single ISBN field for now.
6. Add an author sort strategy setting, but do not store author-sort values per book in Milestone 4.
7. Keep rating, title sort, file size, date added, and last modified as candidates unless explicitly selected after review.
8. Keep custom columns out of Milestone 4.

## Author Sort Strategy

Saga should support author sorting as a preference instead of requiring each book to store a separate author-sort value. This keeps the metadata model lighter and avoids forcing users to maintain an extra field for every book.

Initial strategy options:

- **Display name**: sort authors as entered, for example `Karin Slaughter`.
- **Last name first**: sort by a derived value like `Slaughter, Karin`.
- **Last name, first name, prefixes**: sort names with Dutch-style prefixes more deliberately, for example `Gogh, Vincent van` or `Velde, Peter van de`, once the rule is designed carefully.

The first implementation should be conservative:

- keep the stored author value unchanged;
- calculate the sort key at display/filter/sort time;
- use the selected strategy for author filter ordering and author-based sorting;
- avoid changing author text during import unless the existing cleanup rule is unambiguous.

This gives users useful sorting behavior without making AuthorSort a per-book editing burden.

## Open Review Questions

These questions should be answered before the Milestone 4 implementation plan is written:

1. Which author sort strategy should be the default for Dutch users?
2. Should `Rating` be included in Milestone 4, or remain later until the details pane and filters are more mature?
3. Should `Date added` and `Last modified` be shown in the details pane as read-only system metadata?
4. Should `File size` be shown per available format in the details pane?
5. Should language normalization change only display/filter behavior, or should Saga also offer a bulk action to rewrite stored language values?
