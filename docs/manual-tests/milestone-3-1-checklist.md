# Milestone 3.1 Manual Test Checklist

Use this checklist for background import testing.

## Large Import

- Start the app with an existing library.
- Scan a folder with many ebook files.
- Confirm the import progress card appears.
- Confirm the progress card shows processed and total counts.
- Confirm added, duplicate, possible duplicate, and failed counts update.
- Confirm the library view refreshes while import is still running.
- Confirm search and filters remain usable during import.

## Completion And Details

- Let an import complete.
- Confirm final counts remain visible.
- Open the details/import result view.
- Confirm result counts match the progress card.
- If failed items exist and the source files are still available, confirm Retry failed starts a new import.
- Confirm Retry failed is disabled when failed items only have unavailable or display-only source paths.
- Import the same book in two different formats, for example PDF and MOBI, with matching title and author.
- Confirm Saga keeps one book in the library and the Type filter contains both formats for that book.
- Import the same book again in an already existing format and confirm it is still treated as a possible duplicate or exact duplicate.

## Cancellation And Closing

- Start a large import and press Cancel.
- Confirm the app remains usable after cancellation.
- Start a large import and close the app.
- Confirm the close warning appears.
- Choose No and confirm the app stays open.
- Close again and choose Yes, then confirm the app closes.
