# DataGrid Makeover

## Status

Future feature. Do not implement as part of Milestone 4.

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

## Candidate Scope

### Sorting

- Author column sorting should use the selected `AuthorSortStrategy`.
- Language column sorting should use stable language keys while showing localized labels.
- Format/type sorting should remain based on normalized format names.
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

### Views

The makeover should consider future user-defined views:

- remember sort and grouping per view;
- remember visible columns per view;
- allow default view selection;
- avoid locking this behavior to the detailed grid only if bookshelf/list views later gain compatible view definitions.

## Non-Goals

- Do not add custom metadata columns in this feature unless the custom metadata milestone has already defined the model.
- Do not store per-book `AuthorSort` values just to support grid sorting.
- Do not rewrite author names for sorting.

## Acceptance Ideas

- Clicking the Author column header gives the same author order as the Saga sort dropdown.
- Changing the author sort strategy in Settings immediately affects grid sorting behavior.
- Grouping by category/tag shows books under the expected group headers.
- Grouping and multi-column sorting can be combined without losing the selected library filter state.
- Large libraries remain responsive.
