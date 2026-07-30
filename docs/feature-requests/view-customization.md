# View Customization

## Status

Implemented as Milestone 11, extended in Milestone 13 and Milestone 14.

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
- Visible column selection is managed from Settings per view.
- Bookshelf is shown in the view settings but explains that it uses covers only.

## Milestone 13

Milestone 13 finishes the current view-customization foundation before custom user-created views are introduced.

### Slice 1: Per-view Sort Persistence

Status: implemented.

- Changing Sort by updates only the active view.
- Switching views restores the Sort by option last used in that view.
- Saved sort choices are stored in app settings as `LibrarySorts`.
- Invalid or missing stored values fall back to no sorting.

## Current Findings

- With a large library, the Bookshelf can briefly render empty during startup or immediately after removing grouping while the virtualized view rebuilds. This is acceptable for now as long as the UI remains responsive and the view repopulates within a few seconds.
- Grouped Detailed and List views use Saga's custom grouped layout. It now supports visible columns, fixed headers, resizing, and saved column widths.

### Later

- User-created views with their own column, grouping, sort, and layout presets.
- Optional view names and duplicate/copy view actions.
- A richer view-management screen when the number of view settings grows.
- User-editable column order.

### Slice 2: Reset Current View Layout

Status: implemented.

- The active view can be reset from Settings > Views.
- Sorting and grouping are cleared for the active view.
- Detailed and List return to their default columns and default column widths.
- Layouts saved for other views are left unchanged.

### Slice 3: Column Visibility In Settings

Status: implemented.

- Column visibility is no longer shown in the library side panel.
- Settings has a Views section with a view selector.
- Detailed and List show their available columns as checkboxes.
- Bookshelf shows a disabled explanation because it only uses covers.
- Reset view lives with these view settings, so restoring default columns is explicit.

## Milestone 14

Milestone 14 prepares Saga for user-created views without exposing that workflow yet. The goal is to move from separate per-feature settings toward a single layout model per view.

### Slice 1: Unified View Layout Settings

Status: implemented.

- Added `LibraryViewLayoutSettings` as the canonical storage shape for view layout data.
- Each built-in view can store grouping, sorting, columns, and column widths in one layout record.
- Saga still reads the older `LibraryGroupings`, `LibraryColumns`, `LibraryColumnWidths`, and `LibrarySorts` settings for migration.
- Saving view customization writes both the new unified layout settings and the older settings for now.
- This creates a stable path toward user-created views with names, copied layouts, and custom presets.

### Next Slices

- Add column order persistence for Detailed and List.
- Introduce a view-definition model that can represent built-in views and future user-created views.
- Add view copy/duplicate behavior before allowing fully custom view creation.

### Slice 2: Column Settings Tab And Column Order

Status: implemented.

- Renamed the Settings tab from Views to Column settings to avoid confusion with Appearance.
- Detailed and List columns can be reordered by dragging the column grip in Settings.
- Column order changes are applied live to the active grid.
- Standard Detailed and List grids physically reorder their columns to match the saved layout.
- Hidden columns can be used as drop targets, and dropping below the list moves a visible column to the end.
- Bookshelf keeps the covers-only explanation and does not expose column ordering.
