# Metadata Multi-Edit

## Purpose

Saga should let users maintain metadata for multiple selected books in one action. This is essential after large imports where books share the same series, tags, language, status, or author corrections.

## Milestone 20

Milestone 20 introduces the first testable multi-edit workflow:

- Users can select multiple books in the Detailed and List views.
- The library toolbar exposes a Multi-Edit action when one or more books are selected.
- Detailed and List views also expose Multi-Edit through the row/grid context menu.
- A Multi-Edit dialog shows the selected book count and safe editable standard fields.
- The first editable fields are authors, series, tags, language, and reading status.
- Each field has an explicit checkbox, so blank values are not accidentally applied.
- Applying changes refreshes the visible rows, filters, selected book details, and search results.
- Existing bulk persistence paths are reused where possible.
- Custom metadata fields are also available in the same dialog.
- Custom metadata editors respect the configured field type: text, number, date, yes/no, single choice, and multiple choice.
- Checked empty custom metadata fields clear that value for all selected books.
- Custom metadata changes refresh custom filters, visible custom columns, selected book details, and search results immediately.
- Users can swap title and authors for selected books when imported metadata has those fields reversed.

## Deferred Follow-Up Candidates

- Add the same multi-selection workflow to Bookshelf tiles.
- Add tag modes: replace tags, add tags, remove tags, split tags, and merge tags.
- Add a preview screen for title/author swaps before applying large batches.
- Add a preview screen for large batches before applying changes.
- Add undo/history for batch metadata edits.
- Add conflict reporting that lists skipped books when duplicate title/author rules prevent an update.
- Add richer validation feedback inside the dialog instead of using only the busy/status feedback path.
