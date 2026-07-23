# Milestone 8 Manual Test Checklist

## Library Management Polish

- [x] Select a book with at least one managed format and confirm the format is shown as a compact file-type button.
- [x] Right-click the file-type button and confirm the context menu shows Open, Open folder, Save, and Save to.
- [x] Confirm the format context menu uses icons and a clear warning color for Remove format.
- [x] Use Open and confirm Windows opens the selected ebook file with the default app for that file type.
- [x] Confirm Open shows a short localized message when the managed file is missing.
- [x] Use Open folder and confirm Windows Explorer opens at the managed file location.
- [x] Confirm Open folder shows a short localized message when the managed folder is missing.
- [x] Select a book with multiple formats and confirm Remove format removes only the selected format after confirmation.
- [x] Select a book with one format and confirm Remove format does not delete the book and shows a clear localized message.
- [x] Change the UI language and confirm Open and the save-folder dialog title follow the selected language.

## Regression Checks

- [x] Confirm Save still copies the selected format to Downloads.
- [x] Confirm Save to still copies the selected format to a user-selected folder.
- [x] Confirm the short save confirmation still follows the selected UI language.
- [x] Confirm books with multiple formats still show one book record in the library.
