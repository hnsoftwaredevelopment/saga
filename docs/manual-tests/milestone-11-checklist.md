# Milestone 11 Manual Test Checklist

## View Customization Foundation

- [ ] Open Saga and confirm Detailed view still shows the current default columns.
- [ ] Switch to List view and confirm it still shows the current default columns without the cover column.
- [ ] Open the Columns section in Detailed view, hide a column and confirm it disappears immediately.
- [ ] Open the Columns section in List view, hide a column and confirm it disappears immediately.
- [ ] Add grouping in Detailed view and confirm hidden columns remain hidden in the grouped rows and grouped header.
- [ ] Add grouping in List view and confirm hidden columns remain hidden in the grouped rows and grouped header.
- [ ] Switch to List view and confirm Detailed column choices do not affect List.
- [ ] Restart Saga and confirm saved column visibility is restored.
- [ ] Confirm Bookshelf remains cover-first and does not show normal grid column customization.
- [ ] With a large library, confirm Bookshelf may briefly render empty while rebuilding but repopulates without freezing.

## Regression Checks

- [ ] Sorting still works in Detailed and List views after columns are hidden.
- [ ] Grouping still works in Detailed and List views after columns are hidden.
- [ ] Search highlighting still works in visible text columns.
- [ ] Large libraries remain responsive when switching views.
