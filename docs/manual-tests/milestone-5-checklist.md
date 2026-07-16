# Milestone 5 Manual Test Checklist

## Details System Metadata

- [ ] Select a book and confirm the details pane shows Date added / Toegevoegd.
- [ ] Select a book and confirm the details pane shows Last modified / Laatst gewijzigd.
- [ ] Confirm both fields are read-only.
- [ ] Change the application language and confirm the labels and date/time formatting update.
- [ ] Edit normal metadata, save, and confirm Last modified updates after the save.
- [ ] Confirm loading a book does not show unsaved changes.

## Format Details

- [ ] Select a book with one format and confirm the details pane shows that format.
- [ ] Select a book with multiple formats and confirm each format is shown on its own row.
- [ ] Confirm file sizes are shown per format when Saga has managed file information.
- [ ] Confirm the format list remains read-only for now.
- [ ] Confirm the layout leaves room for future per-format actions such as Download.

## Metadata Enrichment Baseline

- [ ] Import an EPUB with rich metadata and confirm publisher, publication date, ISBN, tags, series, language, description, cover, formats, date added, and last modified are visible in Details.
- [ ] Import from a Calibre folder with `metadata.opf` and confirm the existing standard fields still load correctly.
- [ ] Confirm unsupported or future metadata fields are not shown as editable fields yet.
