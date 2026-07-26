# Milestone 10 Manual Test Checklist

## Import Performance Diagnostics

- [ ] Import a small EPUB or PDF and open the import result details.
- [ ] Confirm the details grid shows total duration and the new phase timing column.
- [ ] Confirm phase timings include useful parts such as file recognition, metadata, duplicate check, copy to library, or save data.
- [ ] Confirm the import result details show an aggregate phase summary ordered by the phases that took the most total time.
- [ ] Open the same import from import history and confirm the phase timings are still available.
- [ ] Import or scan a folder with multiple formats and confirm phase timings appear per row.
- [ ] Search the import result details for the currently displayed phase label, such as `metadata` or `copy` in English, and confirm matching rows remain visible.
- [ ] Confirm the import result details grid shows a horizontal scrollbar when columns exceed the window width.
- [ ] With a library of at least 6000 books, group by Author, sort by Author, add Series grouping, and confirm Saga returns without a long UI freeze.

## Import Phase Meaning

- File availability: Saga checks whether the source file is locally readable before it tries to import it.
- File size: Saga reads the source file size for diagnostics and managed file metadata.
- File recognition: Saga calculates a file fingerprint so exact duplicates can be recognized safely.
- Metadata: Saga reads title, author, description, cover, series, tags, language, and other available book metadata.
- Duplicate check: Saga compares the imported book with the current library by fingerprint, title, author, and possible title match.
- Copy to library: Saga copies the source file into the managed Saga library folder.
- Save data: Saga saves book data, file data, and import history to the SQLite database.
- Cleanup: Saga removes partial files or database records when an import cannot be completed.

## Regression Checks

- [ ] Confirm added, duplicate, possible duplicate, and failed counts still look correct.
- [ ] Confirm retry failed imports still works for retryable failed source paths.
- [ ] Confirm title-match linking still works from import history.
- [ ] Confirm unsupported or unavailable cloud-only files fail without crashing.
