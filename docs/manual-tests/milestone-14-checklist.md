# Milestone 14 Manual Test Checklist

## Unified View Layout Foundation

- [ ] Open Saga with an existing settings file from Milestone 13 and confirm the saved view layouts still load.
- [ ] Change sorting, grouping, visible columns, and column widths for Detailed.
- [ ] Restart Saga and confirm the Detailed layout is restored.
- [ ] Change sorting and grouping for Bookshelf.
- [ ] Restart Saga and confirm the Bookshelf layout is restored.
- [ ] Confirm Settings > Column settings still changes visible columns live for Detailed and List.
- [ ] Confirm Settings > Column settings still shows the disabled covers-only explanation for Bookshelf.

## Column Settings

- [ ] Confirm the settings tab is named Column settings in English and Kolominstellingen in Dutch.
- [ ] Open Settings > Column settings and select Detailed.
- [ ] Drag a visible column by its grip and confirm the Detailed grid updates immediately behind the settings window.
- [ ] Hide a column, move another visible column, save, restart Saga, and confirm both visibility and order are restored.
- [ ] Select Bookshelf and confirm column movement is not available and the covers-only explanation is shown.

## Regression

- [ ] Existing duplicate-candidate column widths are still restored.
- [ ] Reset view still clears only the selected view.
- [ ] Switching between Bookshelf, Detailed, and List keeps each view's own grouping and sorting.
