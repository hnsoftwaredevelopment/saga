# Milestone 13 Manual Test Checklist

## Per-view Sorting

- [ ] Open Saga and select Detailed view.
- [ ] Set Sort by to Title and confirm the Detailed view is sorted by title.
- [ ] Switch to Bookshelf and confirm Sort by returns to that view's saved value, or None when no value was saved.
- [ ] Set Bookshelf Sort by to Author.
- [ ] Switch back to Detailed and confirm Sort by is still Title.
- [ ] Restart Saga and confirm Detailed still restores Title and Bookshelf still restores Author.
- [ ] Switch to List and confirm it can keep a different Sort by value from Detailed and Bookshelf.

## Regression

- [ ] Existing grouping per view is still restored after restart.
- [ ] Existing visible column choices for Detailed and List are still restored after restart.
- [ ] Existing column widths for Detailed, List, and duplicate candidates are still restored after restart.

## Reset Current View

- [ ] Customize Detailed with Sort by, grouping, hidden columns, and changed column widths.
- [ ] Open Settings > Views, select Detailed, click Reset view, and confirm Detailed returns to no sorting, no grouping, default columns, and default widths.
- [ ] Open Settings > Views, select Bookshelf, click Reset view, and confirm Bookshelf sorting and grouping are cleared.
- [ ] Confirm Bookshelf and List keep their own saved sorting and grouping.
- [ ] Restart Saga and confirm the reset Detailed layout is still restored.

## View Settings

- [ ] Confirm the library side panel no longer contains the Columns expander or Reset view action.
- [ ] Open Settings > Views and switch between Bookshelf, Detailed, and List.
- [ ] Confirm Bookshelf shows the disabled text "De boekenplank gebruikt alleen omslagen" in Dutch.
- [ ] Confirm Detailed and List show column checkboxes and changes apply to the selected view.
- [ ] Confirm changing Detailed columns does not change List columns, and the other way around.
