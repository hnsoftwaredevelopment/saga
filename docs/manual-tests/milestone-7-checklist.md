# Milestone 7 Manual Test Checklist

## Per-Format Export

- [ ] Select a book with one available format and confirm the format is shown as a compact file-type button.
- [ ] Right-click the file-type button and confirm the context menu shows Open folder, Save, and Save to.
- [ ] Use Save and confirm the file is copied to the default Downloads folder.
- [ ] Export the same format twice and confirm Saga creates a unique filename instead of overwriting the first export.
- [ ] Use Save to and confirm the file is copied to a user-selected folder.
- [ ] Confirm exported filenames are based on book metadata and do not contain invalid Windows filename characters.
- [ ] Confirm exporting a missing managed file shows a clear failure message.

## Regression Checks

- [ ] Confirm Open folder still opens Explorer for the selected format.
- [ ] Confirm details-pane format rows still show file size per format.
- [ ] Confirm books with multiple formats still show one book record in the library.
- [ ] Confirm the startup splashscreen still shows while the library loads.
