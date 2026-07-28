# DataGrid Makeover

## Status

In progress as Milestone 12.

## Context

Saga currently has two sorting paths:

- the Saga sort dropdown, which can use application-specific sort keys such as the selected author sort strategy;
- the detailed DataGrid column headers, which use the grid's native sorting behavior and therefore sort by the visible cell text.

This is acceptable for the current metadata-settings milestone, but it can feel inconsistent. For example, sorting by author through the dropdown can use `Achternaam, voornaam`, while clicking the Author column header sorts the visible value such as `Voornaam Achternaam`.

Because the detailed grid should eventually support richer desktop behavior, this should be solved as part of a broader DataGrid makeover instead of as a narrow Milestone 4 fix.

## Goals

- Make DataGrid column-header sorting use Saga-aware sort keys where needed.
- Preserve multi-column sorting.
- Add grouping support for useful metadata fields.
- Keep grid behavior consistent with user settings.
- Prepare for user-defined views and column customization later.
- Allow each view to define its own visible columns later.

## Candidate Scope

### Sorting

- Author column sorting uses the selected `AuthorSortStrategy` in the grid row sort key while still showing the normal author display text.
- Language column sorting should use stable language keys while showing localized labels.
- Format/type sorting should remain based on normalized format names.
- Series number sorting should use the numeric series index, not the localized display text.
- Multi-column sorting should remain available.

### Grouping

Initial grouping candidates:

- author;
- category/tag;
- series;
- language;
- status;
- book type/format;
- e-reader availability later.

The UI uses Saga's grouping builder in the filter pane. The user adds grouping chips from a localized dropdown and removes each grouping with the chip close button.

Multi-value grouping needs Saga-specific behavior instead of plain string grouping:

- grouping by author should create one group per individual author, not a combined heading such as `Author A, Author B`;
- grouping by tag should create one group per individual tag;
- when a book has multiple authors or tags, the same book may appear in multiple groups;
- this behavior should apply consistently to grid views and Bookshelf grouping.

Bookshelf grouping should preserve the visual nature of the bookshelf. Covers remain the primary content, but the source should be projected into grouped sections such as author, series, or tag. Horizontal-only cover flow should not return.

### Views

The makeover should consider future user-defined views:

- remember sort and grouping per view;
- remember visible columns per view;
- allow default view selection;
- avoid locking this behavior to the detailed grid only if bookshelf/list views later gain compatible view definitions.

Detailed view should expose all standard metadata fields available in the details pane. Later the user can decide per view which of these columns are visible.

## Non-Goals

- Do not add custom metadata columns in this feature unless the custom metadata milestone has already defined the model.
- Do not store per-book `AuthorSort` values just to support grid sorting.
- Do not rewrite author names for sorting.

## Acceptance Ideas

- Clicking the Author column header gives the same author order as the Saga sort dropdown.
- Changing the author sort strategy in Settings immediately affects grid sorting behavior.
- Grouping by category/tag shows books under the expected group headers.
- Grouping by author or tag treats individual authors/tags as separate group keys, even when one book belongs to multiple values.
- Bookshelf grouping shows cover sections without replacing the cover-first layout with a text grid.
- Grouping and multi-column sorting can be combined without losing the selected library filter state.
- Large libraries remain responsive.

## Implemented Slices

- List view now uses `SfDataGrid` columns instead of a continuous text row.
- Detailed view exposes the standard details-pane metadata fields as grid columns.
- Series number sorting uses the numeric series index.
- Author grid sorting uses the configured author sort strategy while keeping the display name unchanged.
- Bookshelf view has Saga-driven grouping by author, series, tag, language, status, or type.
- Saga-driven grouping is available for Bookshelf, Detailed, and List views.
- Grouping supports multiple levels through removable grouping chips.
- Bookshelf multi-value groups are projected by Saga before they reach the view, so a multi-author, multi-tag, or multi-format book can appear under each individual group value while the visible book count remains unique.
- Bookshelf grouping shows cover sections with headers above the cover rows.
- Detailed and List grouped views use the same Saga group tree as Bookshelf, so multi-author, multi-tag, and multi-format grouping behaves consistently across views.
- Detailed grouped view keeps all standard details-pane metadata columns visible.
- List view keeps the same standard metadata columns as Detailed view, minus the cover.
- Grouping updates reuse the existing visible book rows so adding or removing grouping does not rebuild the filtered list.
- Bookshelf layout refreshes itself after loading, view changes, grouping source changes, and group expansion so covers appear without requiring a manual window resize.

## Milestone 12 Scope

The current milestone focuses on finishing the grid experience created by Milestone 11 column visibility.

### Slice 1: Fixed Grouped Headers

- Detailed and List grouped views should keep a visible column header while grouped.
- The grouped header should use the same visible-column model as the rows.
- Hidden columns should disappear from both the header and row layout.
- The header should remain understandable even when the grouped tree is scrolled.

Status: implemented as a visually distinct fixed header row for grouped Detailed and List views.

### Slice 1b: Standard Header Styling

- Standard Detailed and List grid headers should visually match the grouped header style.
- Native Syncfusion header sorting must remain active when clicking a column header.
- Header sorting should support the normal three-state cycle: ascending, descending, and no sorting.
- Header labels should align with the column content, use theme-aware vertical separator lines, and stand out through a subtle theme-aware background.

Status: implemented by styling the native `SfDataGrid.HeaderStyle`, preserving Syncfusion sorting behavior. Standard headers are left aligned, use a dedicated theme-aware header background, use the theme accent color for vertical separator lines, and enable Syncfusion tri-state sorting.

### Slice 1c: Bookshelf Group Rendering Stability

- Bookshelf should show covers immediately after startup when it is the default view.
- Bookshelf grouped headers should render their books when expanded, including multi-level grouping such as tags and series.
- Fixes should not remove virtualization from the main ungrouped Bookshelf surface.

Status: implemented by refreshing Bookshelf layout after relevant view/source changes and using stable wrap layout for expanded grouped cover rows while preserving virtualization for the main Bookshelf.

### Slice 2: Column Width Foundation

- Capture current column widths per view.
- Persist width changes for Detailed and List views.
- Allow users to resize Detailed and List grid columns directly in the header.
- Keep the defaults identical to the current layout when no custom widths exist.
- Use the same settings foundation for the duplicate candidates view, where the default columns can be too narrow and user resizing should be remembered.

Status: implemented. Detailed and List views allow header-based column resizing, persist resized Syncfusion column widths, and grouped rows reuse the same width snapshot. Duplicate candidates now uses the same settings foundation for its WPF DataGrid column widths.

### Slice 3: Column Order Foundation

- Prepare the column model for user-defined order.
- Keep order per view.
- Defer drag/drop UI if it would make the milestone too large.

## Deferred

- User-defined views.
- Custom metadata columns.
- Full custom grid replacement.
- Import/cloud performance hardening.
