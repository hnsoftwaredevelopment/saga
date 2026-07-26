# View Customization

## Status

In progress as Milestone 11.

## Context

Saga now has strong built-in views with sorting and grouping. The next step is allowing users to shape those views without creating custom metadata columns yet.

The first milestone slice adds a shared column model and persists visible columns per grid view. UI controls can then use the same column identifiers instead of each view inventing its own column list.

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

## Current Findings

- With a very large library, the Bookshelf can briefly render empty during startup or immediately after removing grouping while the virtualized view rebuilds. This is acceptable for now as long as the UI remains responsive and the view repopulates within a few seconds.

## Remaining Slices

- Decide whether column order should be user-editable in this milestone or deferred.
