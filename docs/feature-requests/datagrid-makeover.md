# DataGrid Makeover

## Status

In progress as Milestone 9.

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

The UI may use a group panel above the grid, for example: "Drag a column here to group".

Multi-value grouping needs Saga-specific behavior instead of plain string grouping:

- grouping by author should create one group per individual author, not a combined heading such as `Author A, Author B`;
- grouping by tag should create one group per individual tag;
- when a book has multiple authors or tags, the same book may appear in multiple groups;
- this behavior should apply consistently to grid views and, later, to Bookshelf grouping.

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
- Bookshelf grouping supports two levels through `Group by` and `Then by`.
- Bookshelf multi-value groups are projected by Saga before they reach the view, so a multi-author, multi-tag, or multi-format book can appear under each individual group value while the visible book count remains unique.
- Bookshelf grouping shows cover sections with headers above the cover rows.
- Detailed and List views keep the native Syncfusion group drop area, so the user can interactively group by multiple columns per view.

## Remaining Slices

- User-defined view settings should eventually remember grouping, sorting, and visible columns per view.
- Detailed/List multi-value grouping, such as one book under each individual author when using the native group drop area, still needs a separate design because native grid grouping works on the current row value.
