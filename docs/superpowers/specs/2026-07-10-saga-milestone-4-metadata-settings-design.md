# Saga Milestone 4: Metadata And Settings Foundation Design

## 1. Purpose

Milestone 4 gives Saga a stronger metadata and settings foundation. Calibre is used as a reference for useful ebook-library metadata and workflows, but Saga will not clone Calibre. The goal is to keep the app focused, friendly, and predictable while still learning from mature features that have proven useful in real libraries.

This milestone follows the duplicate merge work: Saga can now detect, compare, delete, and merge duplicates with a clearer workflow than Calibre. Milestone 4 should bring the same principle to metadata management: preserve the power users need, but expose it in a calmer and more understandable way.

## 2. User Outcomes

After this milestone, a user should be able to:

- understand which standard metadata fields Saga supports;
- see richer metadata in the details pane where it is useful;
- trust that imported metadata is cleaned conservatively and consistently;
- normalize common metadata problems such as language codes and inconsistent author or tag values;
- understand the settings structure before advanced defaults such as merge defaults are added;
- keep working without custom columns being forced into the first metadata expansion.

## 3. Scope

Milestone 4 includes:

- Create a metadata inventory comparing Calibre-visible fields, Saga fields, import sources, and proposed Saga behavior.
- Define Saga standard metadata fields for the next implementation phase.
- Design a settings structure with stable sections for current and near-future behavior.
- Improve metadata cleanup and normalization rules where the behavior is low risk.
- Decide which extra standard metadata fields should be persisted, shown, searched, filtered, or edited.
- Add or refine UI visibility for selected standard metadata fields.
- Add focused automated tests and a manual verification checklist.

Milestone 4 excludes:

- Calibre custom columns.
- User-defined views.
- Dynamic grid columns based on custom metadata.
- Native metadata write-back into EPUB, PDF, MOBI, AZW, CBR, or CBZ files.
- Full Calibre database import as the primary import path.
- E-reader device sync.

## 4. Calibre As Reference, Not Blueprint

Calibre contains many mature metadata features that are useful as a reference:

- rich standard fields such as title, authors, author sort, series, series index, publisher, tags, identifiers, rating, dates, comments, languages, and formats;
- bulk cleanup actions from the tag browser;
- metadata merge defaults;
- custom columns;
- flexible search and sorting behavior.

Saga should not copy the Calibre UI or data model one-to-one. Saga should use Calibre to answer three questions:

1. Is this field genuinely useful in a large personal ebook library?
2. Can Saga represent it cleanly without making the app feel heavy?
3. Does it improve importing, searching, filtering, sorting, duplicate handling, or details editing?

If the answer is not clearly yes, the field remains a later-version candidate.

## 5. Metadata Inventory

Milestone 4 starts with a documentation artifact, not a schema migration. The inventory should be stored at:

`docs/metadata/saga-standard-metadata-inventory.md`

The inventory table should contain:

| Field | Calibre Equivalent | Current Saga Support | Source Candidates | Proposed Saga Status | UI Surface | Editable | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Title | title | Supported | EPUB, OPF, metadata.json, filename | Keep | Grid, details, search, duplicates | Yes | Core identity field. |
| Authors | authors | Supported | EPUB, OPF, metadata.json, filename | Keep | Grid, details, filter, search, duplicates | Yes | Multi-value. |
| Author sort | author_sort | Not supported | Calibre OPF/DB, derived | Candidate | Details, sorting later | Later | Useful, but needs careful rules. |
| Series | series | Supported | EPUB, OPF, metadata.json, filename cleanup | Keep | Details, filter, search, duplicates | Yes | Multi-book organization. |
| Series number | series_index | Supported | OPF, metadata.json, title cleanup | Keep | Details, sorting later | Yes | Must remain numeric. |
| Tags | tags | Supported | OPF subjects, metadata.json | Keep | Details, filter, search | Yes | Multi-value cleanup needed. |
| Language | languages | Supported | EPUB, OPF, metadata.json | Improve | Details, filter, search | Yes | Normalize display names. |
| Publisher | publisher | Supported internally | EPUB, OPF, metadata.json | Expose/refine | Details, search | Yes | Good standard field. |
| Publication date | pubdate/date | Supported internally | EPUB, OPF, metadata.json | Expose/refine | Details, search/sort later | Yes | Date parsing must stay conservative. |
| ISBN | identifiers/isbn | Supported internally | EPUB, OPF, metadata.json | Expose/refine | Details, search | Yes | One primary ISBN for now. |
| Identifiers | identifiers | Not supported as list | OPF, Calibre DB, metadata.json later | Candidate | Details later | Later | Needs model for scheme/value pairs. |
| Rating | rating | Not supported | Calibre DB, metadata.json later | Candidate | Details, filters later | Yes later | Useful but not essential for first pass. |
| Date added | timestamp | Partly via Book.CreatedAt | Supported as system metadata | Details/status later | No | Should not be user-editable initially. |
| Last modified | last_modified | Partly via Book.UpdatedAt | Supported as system metadata | Details/status later | No | Useful diagnostics. |
| Formats | formats | Supported | Managed files | Keep/improve | Grid, details, filters, duplicates | No direct edit | Managed through import/merge/delete. |
| File size | size | Import diagnostics only | Managed files | Candidate | Details/diagnostics later | No | Useful for duplicates and storage. |
| Cover | cover | Supported | EPUB, OPF sidecar, CBZ, metadata.json refs | Keep/improve | Bookshelf, grid, details, duplicates | Replace later | CBR cover picker is later candidate. |
| Description | comments | Supported | EPUB, OPF, metadata.json | Keep/improve | Details, merge, search | Yes | HTML cleanup remains important. |

The inventory may include more Calibre fields during research, but implementation should remain limited to fields selected in the milestone plan.

## 6. Proposed Standard Saga Metadata Fields

Saga standard metadata should be split into three groups.

### 6.1 Core Editable Metadata

These are user-facing fields that belong in the details pane and can be edited:

- Title
- Authors
- Series
- Series number
- Tags
- Language
- Publisher
- Publication date
- ISBN
- Description

Most of these already exist in the domain model. Milestone 4 should focus on making them consistently visible, searchable, normalized, and documented.

### 6.2 System Metadata

These are useful to show, filter, or diagnose, but should not be edited directly in the normal details pane:

- Formats
- Date added
- Last modified
- Managed file paths
- File size
- Import source/run diagnostics

System metadata helps the user understand the library without turning metadata editing into file management.

### 6.3 Later Standard Candidates

These are useful but should be implemented only after the foundation is stable:

- Author sort
- Identifiers as scheme/value pairs
- Rating
- Reading dates
- Original publication date
- Data files or supplemental files

Author sort is especially useful, but it needs careful behavior around names such as `van Gogh`, `J.R.R. Tolkien`, organizations, and already comma-separated names.

## 7. Custom Columns Are A Separate Milestone

Custom columns are explicitly out of scope for Milestone 4. They are powerful enough to need their own design.

A later custom metadata milestone should define:

- custom field definitions per library;
- supported field types such as text, number, date, yes/no, list, rating, and URL;
- SQLite storage strategy;
- Calibre custom column import;
- search, filter, and sort behavior;
- optional grid visibility;
- interaction with user-defined views;
- export and sidecar behavior.

Milestone 4 may reserve a named future settings section for custom metadata, but it must not add the custom-column model.

## 8. Settings Structure

The settings structure should be designed before adding merge defaults or other advanced preferences. Saga should group settings by user intent, not by technical implementation.

Proposed top-level sections:

- General
- Appearance
- Libraries
- Import
- Metadata
- Duplicates
- Confirmations
- Diagnostics
- E-reader, later
- Custom metadata, later

### 8.1 General

General settings contain application-wide behavior:

- language;
- startup behavior;
- default library;
- application updates or about information later.

### 8.2 Appearance

Appearance settings contain:

- theme;
- default library view;
- possibly density or cover size later.

### 8.3 Libraries

Library settings contain:

- default library location;
- known libraries;
- missing-library behavior;
- backup/export settings later.

### 8.4 Import

Import settings contain:

- include subdirectories by default;
- recognized formats;
- duplicate handling defaults later;
- cloud-file hydration behavior later;
- import result behavior.

### 8.5 Metadata

Metadata settings contain:

- language normalization behavior;
- author cleanup behavior;
- title cleanup behavior;
- sidecar behavior;
- metadata write-back behavior later.

### 8.6 Duplicates

Duplicate settings contain:

- default duplicate match mode;
- merge defaults later;
- possible duplicate suggestion behavior;
- duplicate cleanup behavior.

### 8.7 Confirmations

Confirmation settings contain:

- delete confirmation reset;
- duplicate delete confirmation behavior;
- destructive metadata cleanup confirmation behavior.

### 8.8 Diagnostics

Diagnostics settings contain:

- import history retention later;
- logging detail later;
- export diagnostic bundle later.

## 9. Metadata Cleanup And Normalization

Cleanup must stay conservative. Saga should avoid silently changing metadata in ways that users cannot predict.

Milestone 4 should focus on low-risk cleanup:

- display language codes as localized language names in filters and details where appropriate;
- preserve the original stored language code unless the user explicitly renames or edits it;
- normalize empty strings to null;
- clean obvious HTML wrappers in descriptions for display while preserving meaningful line breaks;
- keep numeric series numbers numeric;
- keep author comma inversion conservative;
- avoid merging or splitting author names unless the pattern is unambiguous.

Language behavior should distinguish:

- stored value, such as `nl`, `nl-NL`, `eng`;
- normalized filter key, such as `nl` or `en`;
- display value, such as `Nederlands` or `Engels`.

This prevents data loss while making filters friendlier.

## 10. UI Impact

Milestone 4 should not redesign the whole app. It should improve existing surfaces:

- details pane shows selected standard metadata fields clearly;
- filters show normalized display names where useful;
- duplicate merge preview uses the same standard field list where relevant;
- import result diagnostics remain focused on import outcomes;
- settings window gains a clearer section structure before advanced settings are added.

If a field is added to the model but not yet useful in the main grid, it can remain details-only until user-defined views or column customization arrive later.

## 11. Persistence And Compatibility

SQLite remains authoritative inside Saga. `metadata.json` sidecars remain the portable Saga correction format.

Schema changes should be minimal:

- prefer documenting and exposing existing fields before adding new columns;
- add new columns only when a field is selected as a standard Saga field and cannot be derived;
- keep migrations backward compatible for existing libraries;
- avoid storing custom metadata in Milestone 4.

If author sort, identifiers, or rating are selected later, they should get explicit schema design before implementation.

## 12. Testing Strategy

Automated tests should cover:

- metadata inventory-driven decisions where they affect code;
- language normalization display and filter behavior;
- metadata cleanup rules;
- settings round-trip compatibility;
- details pane dirty detection after adding or exposing fields;
- import preservation of selected metadata;
- duplicate merge behavior when selected metadata fields are present.

Manual tests should cover:

- opening a large Calibre-imported library;
- checking language filter display for values such as `nl`, `nl-NL`, `eng`, and `de`;
- editing publisher, publication date, ISBN, tags, and description;
- verifying metadata changes refresh filters and visible rows;
- verifying settings sections are understandable;
- confirming duplicate merge still behaves correctly after field additions.

## 13. Implementation Order

The implementation plan should proceed in this order:

1. Create and commit the metadata inventory document.
2. Refine settings structure in documentation and viewmodel shape.
3. Improve language normalization display behavior.
4. Review and expose existing standard metadata fields in details/search/filter surfaces.
5. Add only agreed standard fields that require schema changes.
6. Update duplicate merge field list if new standard fields are included.
7. Add manual test checklist and README status updates.

Merge defaults should be implemented only after the settings structure has landed.

## 14. Open Decisions For Implementation Planning

Before code work starts, decide:

- whether `AuthorSort` enters Milestone 4 or remains later;
- whether `Rating` enters Milestone 4 or remains later;
- whether identifiers remain a single ISBN field for now;
- whether system metadata such as date added and file size appears in the details pane;
- how much of the settings window should be restructured in the first implementation pass.

My recommendation is to keep Milestone 4 implementation conservative:

- include language normalization;
- expose and polish existing fields;
- document author sort, rating, and identifiers as candidates;
- postpone schema-heavy changes until after testing the refined metadata workflow.
