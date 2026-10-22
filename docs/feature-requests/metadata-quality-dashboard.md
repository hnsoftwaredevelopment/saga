# Metadata quality dashboard

## Goal

Saga should give users a fast overview of metadata problems in the active library, especially after large Calibre imports or bulk cleanup work. The dashboard supports navigation, reversible quality decisions, and incremental direct repair actions.

## Milestone 26: First slice

- Add a toolbar action that opens a metadata quality dashboard.
- Show the total number of books and total number of detected quality signals.
- Show issue cards with counts and explanations.
- Let users select an issue card and inspect the affected books in a grid.
- Keep the dashboard read-only in this slice.

## Initial quality signals

- Missing author: no author or only `Unknown`.
- Unknown language: empty or invalid language value.
- Missing cover: no embedded or managed cover available.
- Series number without series: a numeric series position exists, but the series name is empty.
- Possible title/author swap: the title looks like a person name and the single author does not.
- Messy tags: empty tags, leading/trailing whitespace, double spaces, or comma-separated tag text.

## Milestone 27: Open in library

- Select an affected book row in the dashboard.
- Open the selected book with the `Open in library` button or by double-clicking its row.
- Resize the issue pane when a title or description needs more horizontal space.
- Keep the active library view, user-defined layout, sorting, columns, and grouping.
- Clear only search text or selected filter values that prevent the chosen book from being visible.
- Expand the first group path that contains the book and scroll it into view.
- Report a clear message if the selected book is no longer available.

## Milestone 28: Reversible quality decisions

- Mark one quality signal for one book as correct without changing that book's metadata.
- Hide only the exact book-and-signal combination; other signals for the same book remain visible.
- Keep the decision in the active library's SQLite database and apply it again when the dashboard reopens.
- Keep selection and issue counts current after a decision.
- Manage ignored quality issues from Settings, including restoring selected entries or all entries.
- Re-evaluate a restored signal the next time the dashboard opens.
- Remove stored decisions automatically when their book is deleted.
- Keep all dashboard and management actions keyboard accessible and localized in every supported language.

## Milestone 29: Repair a missing author

- Select one book under `Missing author` and choose `Change author`.
- Choose an existing author from live suggestions based on the active library or enter a completely new author.
- Reject empty values and `Unknown` without changing the book.
- Save through Saga's existing metadata, sidecar, and supported ebook write-back route.
- Re-read the saved book and immediately re-evaluate every quality signal for that book.
- Refresh the dashboard rows, counts, selection, main library rows, and author filters without restarting Saga.
- Keep valid existing authors protected from this repair path.
- Keep the workflow keyboard accessible and localized in all six supported languages.

## Milestone 30: Repair a missing or unknown language

- Show `Change language` only while the unknown-language issue and one affected book are selected.
- Hide the author action outside the missing-author issue so the available action always matches the selected problem.
- Choose from a searchable, localized list of valid languages.
- Store the selected language as a consistent language code.
- Save through Saga's existing metadata, sidecar, and supported ebook write-back route.
- Re-read the saved book and immediately refresh the dashboard, main library row, and language filters.
- Keep an existing valid language protected from this repair path.
- Keep the workflow keyboard accessible and localized in all six supported languages.

## Milestone 31: Repair a missing series name

- Show `Change series` only while the series-number-without-series issue and one affected book are selected.
- Show the existing series number as read-only context and leave it unchanged.
- Choose an existing series from live suggestions based on the active library or enter a completely new series name.
- Reject empty values without changing the book.
- Save through Saga's existing metadata, sidecar, and supported ebook write-back route.
- Re-read the saved book and immediately refresh the dashboard, main library row, and series filters.
- Keep valid existing series names protected from stale dashboard data.
- Keep the workflow keyboard accessible, usable with longer translated labels, and localized in all six supported languages.

## Milestone 32: Repair a swapped title and author

- Show `Swap title and author` only while the possible-title-author-swap issue and one repairable book are selected.
- Show the current and resulting title and author before the user confirms the change.
- Swap only the full title and the single usable author; preserve all other metadata and book state.
- Reject stale issues, empty or `Unknown` authors, and books with multiple authors without writing.
- Save through Saga's existing metadata and sidecar route and immediately refresh dashboard, main library rows, and author filters.
- Keep the workflow modal, keyboard accessible, usable with longer translated labels, and localized in all six supported languages.

## Milestone 33: Find and repair a missing cover

- Show `Search for cover` only while the missing-cover issue and one affected book are selected.
- Search Google Books and Open Library only after an explicit user action, using current title and author and adding an exact ISBN route only for a validated ISBN-10 or ISBN-13.
- Reject unrelated online candidates by checking exact ISBN or a strong title-and-author match; prefer the local Saga cover when confidence is insufficient.
- Show at most twelve validated, unique candidates with source and dimensions; require the user to select one explicitly.
- When neither online source returns a usable candidate, offer one locally generated Saga cover with title and author.
- Let the user choose with mouse, Enter, or double-click and cancel without changing the book.
- Store the chosen image safely as the managed `cover.jpg`, update cover bytes and relative path, and refresh every visible library surface immediately.
- Keep `Change cover` available in book details even when a cover already exists; stage the selected cover until the user chooses the normal `Save`, and restore it with `Undo`.
- Keep external responses bounded and untrusted, use fixed HTTPS hosts, and report no-results, network, validation, and storage failures without metadata loss.
- Keep the workflow localized in all six supported languages and do not add a package, API key, or database migration.

## Milestone 39: Repair missing authors in bulk

- Select one or multiple books within `Missing author` and choose `Change author`.
- Choose one known author from live suggestions or enter a completely new author for every selected book.
- Show the selected-book count in the repair window and make the change action an explicit confirmation.
- Re-read every book immediately before saving and never overwrite a valid author that appeared after the dashboard opened.
- Process results per book through the existing SQLite, sidecar and supported ebook write-back route.
- Reconcile successful, partially saved, stale, removed and failed books separately so the dashboard always reflects stored reality.
- Keep fully failed rows visible and selected, while summarizing all result counts in the current language.
- Refresh the main library and author filters once for the complete batch.
- Keep every other repair action limited to exactly one selected book.

## Milestone 41: Repair missing or unknown languages in bulk

- Select one or multiple books within `Missing or unknown language` and choose `Change language`.
- Choose one valid language from the existing searchable, localized language list for every selected book.
- Show the selected-book count in the repair window and make the bulk change explicit on the confirmation button.
- Re-read every book immediately before saving and never overwrite a valid language that appeared after the dashboard opened.
- Process results per book through the existing SQLite, sidecar and supported ebook write-back route.
- Reconcile successful, partially saved, stale, removed and failed books separately so the dashboard reflects stored reality.
- Keep fully failed rows visible and selected, while summarizing all result counts in the current language.
- Refresh the main library and language filters once for the complete batch.
- Keep other single-book repair actions unchanged; bulk missing-author repair and bulk `This is correct` remain available in their own contexts.

## Milestone 42: Verify title and author swap from book details

- Keep the existing title-and-author swap action available from book details for any editable book, including books that the quality heuristic does not flag.
- Stage the swapped title and authors in the detail pane without writing immediately.
- Let the normal `Save` action persist the change and let `Undo` restore the original values before saving.
- Treat the former complete title as one author, even when that title contains a semicolon.
- Refresh the visible library row, active search results, selection and author filters after saving.
- Keep the existing arrow icon keyboard accessible and provide its localized tooltip and accessible name in all six supported languages.
- Preserve every other metadata field and do not change the possible-title-author-swap heuristic.

## Milestone 43: Repair missing series names in bulk

- Select one or multiple books within `Series number without series` and choose `Change series`.
- Choose one known series from live suggestions or enter a completely new series name for every selected book.
- Show the selected-book count and explain that every book keeps its own series number.
- Re-read every book immediately before saving and never overwrite a series name that appeared after the dashboard opened.
- Process results per book through the existing SQLite, sidecar and supported ebook write-back route.
- Reconcile successful, partially saved, stale, removed and failed books separately so the dashboard reflects stored reality.
- Keep fully failed rows visible and selected, while summarizing all result counts in the current language.
- Refresh the main library and series filters once for the complete batch.
- Keep single-book series repair, other quality repairs and existing bulk actions unchanged.

## Milestone 44: Clean messy tags in bulk

- Select multiple books under Messy tags and review an individual before/after proposal for every book.
- Use the existing normalizer independently for each book; an empty result explicitly says No tags.
- Keep the bulk preview read-only; selecting one book retains the existing editable proposal.
- Apply all proposals only after explicit confirmation; cancellation before confirmation writes nothing.
- Compare original tags with current storage before saving; never overwrite tags changed since the preview.
- Handle results per book, continue after individual errors, refresh the library and category filters once, and retain failed rows for retry.
- Reuse existing metadata persistence and localized result summaries in all six languages.

## Milestone 45: Swap title and author for selected books

- Use the existing quality-list selection directly and show one compact count confirmation, without a second list or checkboxes.
- Confirming processes all suitable selected books; cancelling writes nothing.
- Unsuitable books are skipped and counted in the confirmation. Single-book before/after confirmation remains available.
- Compare the original full title and author list with current storage before swapping; skip changed or unavailable books safely.
- Reuse the existing persistence service and batch reconciliation, continue after individual failures and refresh the library and author filters together.
- Keep unselected books unchanged and failed rows selected; show localized result counts in all six languages.
- Preserve detail-pane Save/Undo. At the user's request, also tighten swap detection within this slice: require independent title-like evidence in the author field and recognize initials, particles and surname-first notation. Preserve the confirmed Duenas swap while suppressing the 32 supplied correct examples; prefer fewer supported signals over ambiguous guesses.

## Follow-up ideas

- Add a broader metadata lookup that proposes ISBN, authors, description, publisher and other fields separately, so the user decides which values to adopt. Evaluate BoekenBase as a Dutch source before implementation, including API access, terms and stability.
- Improve large-library scrolling and filtering in a separate performance slice with measurable checks on a representative library.
- Add export or filtered worklists for large cleanup sessions.
- Make checks configurable in settings.
- Continue tuning heuristics after real-world use. The conservative title/author rules, supplied regression examples and deliberate limitations are recorded in [detection examples](title-author-swap-detection.md).

## Status

Milestones 26 through 32 and 36 through 44 are implemented and accepted, including individual repairs, bulk author/language/series/tag repair and title/author swapping from details. Milestone 44 was merged on 22 October 2026. Milestone 45's bulk action is manually tested and accepted: users select books in the quality list and confirm the title/author swap once. The additional conservative title/author detection is automatically tested and awaits manual recheck. Cover search uses Google Books and Open Library with a generated fallback; its expanded real-world acceptance remains open. The title/author heuristic now requires stronger evidence and intentionally omits ambiguous cases; any remaining false positives can still be dismissed with `This is correct`. Online metadata lookup, other bulk repairs, export, configurable checks and heuristic tuning remain follow-up work.
