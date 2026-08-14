# Milestone 18 Manual Test Checklist

Use this checklist for the first Calibre custom-column import slice.

## Calibre Custom Columns

- [ ] Create a new Saga test library.
- [ ] Scan a small folder from an existing Calibre library that contains `metadata.db` at the library root and ebook files in Calibre book folders.
- [ ] Confirm standard metadata from `metadata.opf` still imports correctly.
- [ ] Open Settings > Metadata and confirm supported Calibre custom columns were created as Saga custom metadata fields.
- [ ] Confirm Calibre enumeration columns appear as single-choice fields with their option lists.
- [ ] Confirm Calibre text, number, date, and yes/no values appear in the Details pane for imported books when values exist in Calibre.
- [ ] Add imported custom metadata fields to a custom view and confirm the values appear in columns.
- [ ] Search for an imported custom metadata value and confirm matching books are found.
- [ ] Confirm imported custom metadata fields appear in the left filter list when populated.
- [ ] Import a Calibre folder that contains books already present in Saga and confirm exact duplicates stay skipped while their Calibre custom metadata is added to the existing Saga book.

## Expected Limitations

- [ ] Confirm Calibre composite/template columns are not imported as editable Saga fields.
- [ ] Confirm possible duplicates are still not automatically backfilled, because Saga cannot safely choose the target book yet.
- [ ] Confirm importing from a normal non-Calibre folder still works without custom metadata.
- [ ] Confirm a failed custom-column import is shown as a metadata warning and does not block importing the book.
