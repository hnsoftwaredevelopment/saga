# Milestone 2 Manual Test Checklist

Use this checklist for the version `0.1` Windows desktop test release.

## Library And Import

- Create a new library and confirm it is selected immediately.
- Open an existing library and confirm the last used library reopens on restart.
- Add books through the toolbar.
- Drag and drop one or more supported files onto bookshelf, detailed, and list views.
- Scan a folder with the configured subdirectory setting.
- Confirm import results show added, skipped, possible duplicate, and failed items clearly.

## Views, Search, And Filters

- Switch between bookshelf, detailed, and list views.
- Set a default startup view in Settings and confirm it is used after restart.
- Search across metadata and confirm visible matches are highlighted.
- Confirm filters start inactive and all books are visible.
- Select multiple authors and confirm matching books are added to the visible list.
- Select series, tags, status, e-reader, and language filters and confirm selections expand the result set.
- Sort by title, author, e-reader, and tags.

## Metadata Editing

- Select a book and edit title, authors, tags, series, status, and description.
- Confirm unsaved changes are shown only after editing.
- Save changes and confirm the grid, bookshelf/list rows, and left filters update immediately.
- Confirm `metadata.json` is written next to the managed book file.
- Undo unsaved edits and confirm original values return.
- Delete a book and confirm the library view refreshes.

## Localization, Themes, And Icons

- Switch between English and Dutch and confirm toolbar, details, settings, view selector, sort selector, grid headers, and import result window update.
- Switch among light, dark, sepia, blue, and red themes.
- Confirm toolbar and details action icons are visible, aligned, themed, and still have localized tooltips.

## Known Deferrals

- Native metadata write-back into ebook files is deferred.
- Active e-reader detection and USB sync are deferred.
- Custom fields and user-defined views are deferred.
