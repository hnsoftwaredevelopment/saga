# Milestone 7 Manual Test Checklist

## Per-Format Export

- [ ] Select a book with one available format and confirm the format row shows an export action.
- [ ] Export the format to the default Downloads folder and confirm the file is copied there.
- [ ] Export the same format twice and confirm Saga creates a unique filename instead of overwriting the first export.
- [ ] Export a format to a user-selected folder and confirm the file is copied there.
- [ ] Confirm exported filenames are based on book metadata and do not contain invalid Windows filename characters.
- [ ] Confirm exporting a missing managed file shows a clear failure message.

## Regression Checks

- [ ] Confirm Open folder still opens Explorer for the selected format.
- [ ] Confirm details-pane format rows still show file size per format.
- [ ] Confirm books with multiple formats still show one book record in the library.
- [ ] Confirm the startup splashscreen still shows while the library loads.
