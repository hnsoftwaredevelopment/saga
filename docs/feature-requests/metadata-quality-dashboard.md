# Metadata quality dashboard

## Goal

Saga should give users a fast overview of metadata problems in the active library, especially after large Calibre imports or bulk cleanup work. The dashboard is intended as a navigation and insight tool first; repair actions can be added in later slices.

## First slice

- Add a toolbar action that opens a metadata quality dashboard.
- Show the total number of books and total number of detected quality signals.
- Show issue cards with counts and explanations.
- Let users select an issue card and inspect the affected books in a grid.
- Keep the dashboard read-only in this slice.

## Initial quality signals

- Missing author: no author or only `Unknown`.
- Unknown language: empty or invalid language value.
- Missing cover: no embedded or managed cover available.
- Series number without series: a numeric series position exists, but the series name is empty.
- Possible title/author swap: the title looks like a person name and the single author does not.
- Messy tags: empty tags, leading/trailing whitespace, double spaces, or comma-separated tag text.

## Follow-up ideas

- Select or open a book from the dashboard in the main library.
- Add direct repair actions for common issues.
- Add export or filtered worklists for large cleanup sessions.
- Make checks configurable in settings.
- Tune heuristics after testing on real-world Calibre libraries.
