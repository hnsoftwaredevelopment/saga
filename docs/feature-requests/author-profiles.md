# Author Profiles

## Status

Deferred future feature.

## Context

Saga currently stores authors as book metadata. A future author-management milestone should make authors first-class library entities so users can keep richer information about writers and clean up aliases or spelling variants more comfortably.

## Goals

- Add an author profile screen.
- Store display name, photo, description or biography, birth date, and optional death date.
- Support author aliases, pseudonyms, and alternate spellings.
- Show all books by the author in the active library.
- Include books stored under linked aliases when showing an author's books.
- Help users clean up metadata by linking, renaming, or merging author variants.

## Initial Ideas

- Author profiles are library-local at first.
- Keep original book metadata unchanged until the user explicitly chooses to normalize or link authors.
- Store aliases as separate records linked to a canonical author profile.
- Use existing author filter cleanup behavior as inspiration, but move richer author edits to a dedicated author-management UI.
- Later Calibre imports may help seed author names or links, but biography and photos should start as user-managed fields.

## Out Of Scope For First Implementation

- Online author lookup.
- Automatic alias detection beyond suggestions.
- Cross-library or cloud-synced author profiles.
- Writing author profile data back into ebook files.

## Candidate Milestone

After custom metadata, Calibre custom-column import, and the first metadata-management workflows are stable.
