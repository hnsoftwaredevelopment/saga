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
- Added settings round-trip and viewmodel persistence tests.

## Remaining Slices

- Bind Detailed and List grid column visibility to the persisted column model.
- Add a user-facing column chooser for Detailed and List views.
- Decide whether column order should be user-editable in this milestone or deferred.
- Add manual test coverage once the UI is available.
