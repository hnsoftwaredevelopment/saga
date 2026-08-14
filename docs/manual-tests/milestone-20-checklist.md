# Milestone 20 Manual Test Checklist

Use this checklist for the first metadata multi-edit slice.

## Selection

- [ ] Open Detailed view and select multiple books with Ctrl-click.
- [ ] Confirm the Multi-Edit toolbar button becomes enabled.
- [ ] Right-click a selected row in Detailed view and open Multi-Edit from the context menu.
- [ ] Open List view and select multiple books with Ctrl-click.
- [ ] Right-click a selected row in List view and open Multi-Edit from the context menu.
- [ ] Confirm selecting a single row still updates the details pane.

## Multi-Edit

- [ ] Open Multi-Edit and confirm the selected book count is shown.
- [ ] Set a shared series value and apply it.
- [ ] Confirm all selected books show the new series.
- [ ] Confirm a brand-new series appears in the Series filter immediately after applying.
- [ ] Set shared tags and apply them.
- [ ] Confirm filters refresh immediately with the new tags.
- [ ] Set a shared language and status and confirm visible rows update.
- [ ] Leave an unchecked field empty and confirm it is not changed.

## Custom Metadata

- [ ] Confirm all configured custom metadata fields are visible in Multi-Edit.
- [ ] Confirm text fields use a text box.
- [ ] Confirm number fields use a right-aligned numeric text box.
- [ ] Confirm date fields use a date picker.
- [ ] Confirm yes/no fields use a yes/no selector.
- [ ] Confirm single-choice fields use a drop-down with the configured options.
- [ ] Confirm multiple-choice fields show the configured options as checkboxes.
- [ ] Apply a shared custom metadata value and confirm details, filters, search, and custom columns update immediately.
- [ ] Apply an empty checked custom metadata field and confirm that value is cleared for the selected books.

## Safety

- [ ] Cancel the dialog and confirm no metadata changes are saved.
- [ ] Confirm the busy overlay appears while a larger batch is saved.
- [ ] Confirm search and filters use the updated values immediately.
