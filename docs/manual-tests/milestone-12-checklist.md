# Milestone 12 Manual Test Checklist

## Grouped Headers

- [ ] Open Detailed view without grouping and confirm the normal header is visible.
- [ ] Click a Detailed view column header and confirm sorting cycles through ascending, descending, and no sorting.
- [ ] Confirm Detailed view headers are left aligned, use a subtle theme-aware background, and only show vertical separator lines.
- [ ] Add grouping in Detailed view and confirm the grouped layout still shows a visible column header.
- [ ] Hide a column in Detailed view and confirm the grouped header and grouped rows both hide it.
- [ ] Open List view without grouping and confirm the normal header is visible.
- [ ] Click a List view column header and confirm sorting cycles through ascending, descending, and no sorting.
- [ ] Confirm List view headers are left aligned, use a subtle theme-aware background, and only show vertical separator lines.
- [ ] Add grouping in List view and confirm the grouped layout still shows a visible column header.
- [ ] Hide a column in List view and confirm the grouped header and grouped rows both hide it.

## Regression Checks

- [ ] Grouping by Author and Series still works.
- [ ] Multi-value grouping, such as Authors or Tags, still shows books under individual values.
- [ ] Start Saga with Bookshelf as default view and confirm covers appear without resizing the window.
- [ ] In Bookshelf, sort by Author, group by Tags and Series, then expand groups and confirm counted books are visible.
- [ ] Resize columns in Detailed view by dragging the header edge, restart Saga, and confirm the widths are restored.
- [ ] Resize columns in List view by dragging the header edge, restart Saga, and confirm the widths are restored.
- [ ] Resize columns, enable grouping, and confirm grouped headers and rows use the same widths.
- [ ] Resize columns in the duplicate candidates window, close and reopen the window, and confirm the widths are restored.
- [ ] Confirm duplicate candidate row actions use compact icons with tooltips for merge, delete, and details.
- [ ] Column visibility remains separate between Detailed and List.
- [ ] Large libraries remain responsive when grouping is added or removed.
