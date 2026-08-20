# Milestone 26 manual test checklist

## Metadata quality dashboard

- Start Saga with an existing library.
- Click the metadata quality toolbar button.
- Verify that the dashboard opens without changing the current library view.
- Verify that the header shows the total book count and total detected issue count.
- Verify that the issue cards are shown with counts and short descriptions.
- Select each issue card and verify that the grid changes to the matching books.
- Check at least these categories when the library contains matching data:
  - missing author
  - unknown language
  - missing cover
  - series number without series
  - possible title/author swap
  - messy tags
- Close the dashboard and verify that the main library remains usable.

## Expected limitations

- The dashboard is read-only in this slice.
- Selecting a row does not yet navigate to the book in the main library.
- Repair actions are planned for later cleanup slices.
