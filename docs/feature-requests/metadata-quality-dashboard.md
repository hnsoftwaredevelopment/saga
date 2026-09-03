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

## Follow-up ideas

- Add direct repair actions for title/author, tags, and missing covers.
- Let users select multiple missing-author books and apply one chosen author to all selected books.
- Add export or filtered worklists for large cleanup sessions.
- Make checks configurable in settings.
- Tune heuristics after testing on real-world Calibre libraries.

## Status

Milestones 26 through 31 are implemented. The dashboard detects the initial quality signals, lists affected books in a resizable issue pane, navigates to a selected book while preserving the library layout context, stores reversible quality decisions per active library, and repairs a missing author, missing or unknown language, or missing series name for one selected book. Further repair types and bulk repair remain follow-up work.
