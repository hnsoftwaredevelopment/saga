# Milestone 16 Manual Test Checklist

## Slice 1: Foundation

- [ ] Open an existing Saga library and confirm it migrates without startup errors.
- [ ] Create or open a new library and confirm it still starts empty without errors.
- [ ] Import or browse existing books and confirm standard metadata still appears unchanged.

## Later Slices

- [ ] Create a custom metadata field from Settings.
- [ ] Rename a custom metadata field and confirm existing values remain linked.
- [ ] Delete a custom metadata field and confirm it disappears from the field list.
- [ ] Try to create a custom metadata field with the same name twice and confirm Saga shows a clear message.
- [ ] Switch the app language and confirm the custom metadata field type labels are translated.
- [ ] Select a book and confirm the custom metadata fields appear in Details.
- [ ] Edit custom metadata values in Details and confirm Save works.
- [ ] Confirm yes/no custom fields use a yes/no selector.
- [ ] Confirm number custom fields are right-aligned and reject obvious non-numeric typing.
- [ ] Enter an invalid custom metadata value and confirm the validation message is shown in the active UI language.
- [ ] Clear a custom metadata value, save, reselect the book, and confirm the value stays empty.
- [ ] Edit a custom metadata value and confirm Undo restores the original value.
- [ ] Restart Saga and confirm custom fields and values are restored.
- [ ] Add a custom metadata field as a column in a custom view.
- [ ] Search for a value stored in custom metadata.
