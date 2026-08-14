# Milestone 19 Manual Test Checklist

Use this checklist for the first metadata cleanup hardening slice.

## Filter Cleanup

- [ ] Open a library with multiple books and visible author, series, tag, and language filters.
- [ ] Right-click an author filter value and choose Rename.
- [ ] Confirm all affected books now show the new author value.
- [ ] Confirm the old author filter disappears and the new author filter count is correct.
- [ ] Right-click a tag filter value and choose Delete.
- [ ] Confirm that tag is removed from all affected books and disappears from the tag filter.
- [ ] Rename a series filter value and confirm affected books and filters refresh.
- [ ] Rename or remove a language filter value and confirm language filters refresh.

## Large Library Behavior

- [ ] On a large library, confirm author/tag rename or delete shows the busy overlay while Saga is working.
- [ ] Open long author, series, tag, language, and custom metadata filter lists and confirm the expander opens quickly instead of freezing while all rows are created.
- [ ] Confirm the application does not jump position or appear frozen after the cleanup finishes.
- [ ] Confirm the selected book details refresh when the selected book was affected.
- [ ] Edit title/author/series/tags in the Details pane, save, and confirm the library refresh is local and does not feel like a full library reload.

## Safety Checks

- [ ] If a rename would create a duplicate title/author combination, confirm Saga keeps the conflicting book unchanged and still applies safe changes where possible.
- [ ] Confirm search and filters use the updated values immediately after cleanup.
