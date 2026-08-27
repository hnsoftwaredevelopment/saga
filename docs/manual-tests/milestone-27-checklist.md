# Milestone 27 Manual Test Checklist

## Dashboard selection

- [x] Open the metadata quality dashboard and confirm the first non-empty issue selects its first book.
- [x] Select another issue and confirm its first affected book is selected.
- [x] Select an issue with zero books and confirm `Open in library` is disabled.
- [x] Close the dashboard and confirm the current library selection, search text, and filters do not change.

## Open in library

- [x] Select a book and use `Open in library`; confirm the dashboard closes and the book is selected in the main library.
- [x] Double-click a book row and confirm it performs the same action.
- [x] Double-click a column header or empty grid space and confirm it does not open a book.
- [x] Repeat the action in Bookshelf, Detailed, and List view and confirm the selected book scrolls into view.

## Search, filters, and layout

- [x] Open a book that is already visible and confirm active search text and filters remain unchanged.
- [x] Hide a book with general search text, open it from the dashboard, and confirm only the general search text is cleared.
- [x] Hide a book with a standard facet filter, open it, and confirm the blocking selection is cleared.
- [x] Hide a book with a custom metadata filter, open it, and confirm the blocking selection is cleared.
- [x] Confirm the active view, user-defined view, sort order, visible columns, and column widths remain unchanged.
- [x] Use one and two grouping levels; confirm only the first path to the selected book is expanded.

## Resizable issue pane

- [x] Drag the vertical divider and confirm the issue pane can be widened and narrowed while both panes remain usable.
- [x] Focus the divider with the keyboard and confirm the arrow keys adjust the issue pane width.

## Unavailable book

- [x] If a dashboard result becomes unavailable before navigation, confirm Saga shows a localized message and leaves the library context unchanged.
