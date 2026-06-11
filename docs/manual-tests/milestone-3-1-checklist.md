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

## Cancellation And Closing

- Start a large import and press Cancel.
- Confirm the app remains usable after cancellation.
- Start a large import and close the app.
- Confirm the close warning appears.
- Choose No and confirm the app stays open.
- Close again and choose Yes, then confirm the app closes.
