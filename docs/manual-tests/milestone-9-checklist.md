# Milestone 9 Manual Test Checklist

## DataGrid Makeover

- [ ] Open the List view and confirm it uses visible columns with headers instead of one continuous text row.
- [ ] Open the Detailed view and confirm all standard details-pane metadata fields are available as columns.
- [ ] Sort Detailed view by Series number and confirm numeric order, for example 1, 2, 3, 10.
- [ ] Change the author sort strategy in Settings and confirm the Author column sort follows that strategy in Detailed and List views.
- [ ] Confirm List view selection still updates the details pane.
- [ ] Confirm search highlighting still works in the List view.
- [ ] Click column headers in List view and confirm sorting works.
- [ ] Confirm the group drop area is visible in Detailed view and List view.
- [ ] Drag a supported column header to the group area and confirm grouping works.
- [ ] Use the Group by dropdown in Detailed view and List view and confirm grouping is applied automatically.
- [ ] Group by Author and confirm a multi-author book appears under each individual author.
- [ ] Group by Tags and confirm a multi-tag book appears under each individual tag while the status book count remains unique.
- [ ] Group by Type and confirm books with multiple formats can appear under each available format.
- [ ] Switch between Bookshelf, Detailed, and List views without losing the active search and filters.

## Regression Checks

- [ ] Confirm Detailed view still shows cover, title, author, status, and e-reader columns.
- [ ] Confirm drag and drop import still works in Detailed view and List view.
- [ ] Confirm a large library remains responsive enough when switching between grid views.
