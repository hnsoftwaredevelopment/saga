# Milestone 15 Manual Test Checklist

## Custom View Definitions

- [ ] Open Saga with existing settings from Milestone 14 and confirm the built-in views still load.
- [ ] Confirm custom view definitions in settings do not break startup.
- [ ] Confirm a custom view can keep its own layout key without changing the built-in Detailed or List layouts.

## Copy View Workflow

- [ ] Copy the current Detailed view into a new named view.
- [ ] Confirm the new view appears in the view switcher.
- [ ] Change sorting, grouping, visible columns, column order, and column widths in the copied view.
- [ ] Switch back to Detailed and confirm the original Detailed layout is unchanged.
- [ ] Restart Saga and confirm the copied view and its layout are restored.

## Regression

- [ ] Built-in Bookshelf, Detailed, and List still switch correctly.
- [ ] Reset view resets only the selected view.
- [ ] Settings > Column settings still edits the selected view.
- [ ] Grouped Detailed and grouped List still follow each selected view's column layout.
