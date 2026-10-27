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

## Milestone 24

Milestone 24 expands tag maintenance in the same multi-edit workflow:

- Tags can be replaced, which is the original behavior.
- Tags can be added while preserving existing tags.
- Tags can be removed while preserving all other tags.
- Multiple tag values are still separated with semicolons.
- Tag changes refresh the visible rows, tag filters, selected book details, and search results immediately.

## Deferred Follow-Up Candidates

### Milestone 49: reliable outcomes

Implemented after scope approval; the original workflow, input refinement and final cancellation correction are user-accepted and merged in PR #50. Per-book outcomes distinguish success, unchanged data, failure, partial storage, unavailable books and file warnings. Successfully stored values are preserved and reconciled once. Failed/partially saved visible books remain selected unless the user made a newer selection. Generic failures no longer abort subsequent books. Custom fields retain independent storage with honest partial results and field names. Database updates use the existing BookService stored-metadata synchronization route; no new format writer or save-result contract.

Feedback is a short green summary or a compact summary with expandable, bounded, virtualized attention details, localized in six languages. Existing input fields and custom metadata support are preserved. No automatic replay of title/author swaps, new retry button, history or undo. See [the design](../superpowers/specs/2026-10-24-saga-milestone-49-multi-edit-results-design.md) and [manual checklist](../manual-tests/milestone-49-checklist.md).

The user accepted the original workflow and the input refinement: authors, series, tags and language can be typed directly, editing nonblank text selects the field and clearing it deselects it. Manual selection of an empty field still requests clearing; manually deselecting a populated field preserves its draft without applying it. Tags no longer requires selection before editing. Swap protection, tag modes, reading status and custom-field controls are unchanged.

All five CodeRabbit review findings were assessed and corrected: cleanup availability notifications, preserving previous results when cancelling the dialog, phase-aware refresh/uncertain-storage messages, bounded asynchronous test waits and explicit assertions against exposing raw technical errors. The final manual cancellation check is accepted; difficult failure paths are covered automatically.

- Implemented in Milestone 51: the same multi-selection workflow is available from Bookshelf tiles, including groups, keyboard navigation, shared selection across views and the existing bulk result behavior. [Manual acceptance](../manual-tests/milestone-51-checklist.md) includes a brief recheck for build 2026.10.27.25 after removing the duplicate Bookshelf edit button; the main toolbar remains the single entry point.
- Add advanced tag modes: split tags and merge tags.
- Add a preview screen for title/author swaps before applying large batches.
- Add a preview screen for large batches before applying changes.
- Add undo/history for batch metadata edits.
- Conflict reporting for skipped books is implemented in Milestone 49.
- Add richer validation feedback inside the dialog instead of using only the busy/status feedback path.
