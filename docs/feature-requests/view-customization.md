# View Customization

## Status

Implemented as Milestone 11, extended in Milestone 13.

## Context

Saga now has strong built-in views with sorting and grouping. The next step is allowing users to shape those views without creating custom metadata columns yet.

The first milestone slice adds a shared column model and persists visible columns per grid view. UI controls can then use the same column identifiers instead of each view inventing its own column list.

Saga views should remember the choices a user makes inside each view, without forcing those choices onto the other views.

## Goals

- Remember visible columns separately for Detailed and List views.
- Keep Bookshelf cover-first and out of normal column customization.
- Use stable column identifiers so future user-defined views can reuse the same model.
- Preserve existing default columns when no column settings exist.
- Avoid custom metadata columns in this milestone.

## Implemented Slices

- Added `LibraryColumnOption` as the standard column identifier set.
- Added `LibraryColumnSettings` to app settings for Detailed and List views.
- Added `LibraryViewModel` APIs to read and update visible columns per view.
- Bound the standard Detailed and List grids to the persisted visible-column model.
- Added a live column chooser in the library side panel for Detailed and List views.
- Grouped Detailed and List templates now collapse hidden columns as well.
- Added settings round-trip and viewmodel persistence tests.
- Detailed and List can remember column widths.
- Duplicate candidates can remember column widths.
- Bookshelf, Detailed, and List can each have their own grouping.

## Milestone 13

Milestone 13 finishes the current view-customization foundation before custom user-created views are introduced.

### Slice 1: Per-view Sort Persistence

Status: implemented.

- Changing Sort by updates only the active view.
- Switching views restores the Sort by option last used in that view.
- Saved sort choices are stored in app settings as `LibrarySorts`.
- Invalid or missing stored values fall back to no sorting.

## Current Findings

- With a very large library, the Bookshelf can briefly render empty during startup or immediately after removing grouping while the virtualized view rebuilds. This is acceptable for now as long as the UI remains responsive and the view repopulates within a few seconds.
- Grouped Detailed and List views use Saga's custom grouped layout. It now supports visible columns, fixed headers, resizing, and saved column widths.

### Later

- User-created views with their own column, grouping, sort, and layout presets.
- Optional view names and duplicate/copy view actions.
- A richer view-management screen when the number of view settings grows.
- User-editable column order.
