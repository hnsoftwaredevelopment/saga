# Metadata Cleanup

## Purpose

Saga should help users clean imported metadata after a large migration, especially from Calibre libraries where author names, series, tags, and language values can be inconsistent.

The cleanup workflow should start close to where users notice the problem: the filter list. When a wrong value appears in a filter, the user can rename or remove that value for all affected books.

## Milestone 19

Milestone 19 starts metadata cleanup hardening.

- Existing filter context-menu actions remain the primary workflow for now.
- Users can rename or remove author, series, tag, and language values from the left filter list.
- Series and language cleanup use direct scalar bulk updates.
- Author and tag cleanup now use a dedicated bulk list-metadata update, so large libraries avoid slow book-by-book persistence in the common case.
- Author cleanup updates each affected book's duplicate key, because duplicate detection depends on title plus authors.
- If a bulk author cleanup would create a duplicate-key conflict, Saga falls back to per-book updates and skips only the conflicting book.
- The UI refreshes the in-memory book list, visible rows, details pane, and filters after successful cleanup.

## Deferred Follow-Up Candidates

- Add bulk edit for selected books from the library grid, excluding title by default but allowing fields such as authors, series, tags, language, status, and selected custom metadata.
- Add a title/author swap action inside the bulk edit window for selected suspicious books, for imports where source metadata had those fields reversed. This should use a clear icon button and a preview before saving.
- Add batch tag tools such as add tag, remove tag, replace tag, split tag, and merge tags.
- Add batch normalization tools for authors, language values, reading status, and common typo/casing differences.
- Add clear read-only context menu feedback for filters that cannot be renamed or removed, such as format/type and device-derived e-reader state.
- Add cleanup actions for custom metadata filter values.
- Add a metadata quality dashboard for missing authors, unknown language values, empty series, duplicate tags, and suspicious author spellings.
- Add preview screens for large cleanup actions before applying changes.
- Add undo/history for metadata cleanup batches.
- Add richer author management with aliases and author profiles.
