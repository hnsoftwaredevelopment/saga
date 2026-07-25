# Milestone 9 Manual Test Checklist

## DataGrid Makeover

- [ ] Open the List view and confirm it uses visible columns with headers instead of one continuous text row.
- [ ] Open the Detailed view and confirm all standard details-pane metadata fields are available as columns.
- [ ] Sort Detailed view by Series number and confirm numeric order, for example 1, 2, 3, 10.
- [ ] Change the author sort strategy in Settings and confirm the Author column sort follows that strategy in Detailed and List views.
- [ ] Confirm List view selection still updates the details pane.
- [ ] Confirm search highlighting still works in the List view.
- [ ] Click column headers in List view and confirm sorting works.
- [ ] Add Author as grouping chip and confirm grouping is applied automatically.
- [ ] Add Series after Author and confirm multi-level grouping works.
- [ ] Remove the Series chip and confirm only that chip disappears while Author remains active.
- [ ] Group by Author and Series and confirm a multi-author book appears under each individual author instead of one combined author header.
- [ ] Group by Tags and confirm a multi-tag book appears under each individual tag while the status book count remains unique.
- [ ] Group by Type and confirm books with multiple formats can appear under each available format.
- [ ] In Detailed grouped view, confirm all standard details-pane metadata columns remain visible.
- [ ] In List grouped view, confirm the standard metadata columns remain visible without the cover column.
- [ ] Confirm grouping can differ between Bookshelf, Detailed, and List views.
- [ ] In Bookshelf view, confirm grouped sections show headers above the cover rows and keep vertical scrolling without horizontal cover flow.
- [ ] Switch between Bookshelf, Detailed, and List views without losing the active search and filters.

## Regression Checks

- [ ] Confirm Detailed view still shows cover, title, author, status, and e-reader columns.
- [ ] Confirm drag-and-drop import still works in Detailed view and List view.
- [ ] Confirm a large library remains responsive enough when switching between grid views.
