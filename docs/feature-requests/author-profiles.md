# Author Profiles

## Status

First slice in specification/preparation for Milestone 48; not implemented yet. See [the proposed design](../superpowers/specs/2026-10-22-saga-milestone-48-author-profiles-design.md).

## Context

Saga already stores authors and book-author links as library database entities. They do not yet have user-maintained profiles. Reuse those identities to add richer writer information without changing book metadata. Existing orphan-author cleanup must preserve authors with profile content.

## Goals

- Add an author profile screen.
- First slice: show the existing name and store an optional photo, website URL and short biography.
- First slice: searchable authors and their books in the active library; library-local, portable photos and explicit save/cancel behavior.
- Later: editable names, birth date and optional death date.
- Later: support author aliases, pseudonyms, and alternate spellings.
- Show all books by the author in the active library.
- Later: include books stored under linked aliases when showing an author's books.
- Later: help users clean up metadata by linking, renaming, or merging author variants.

## Initial Ideas

- Author profiles are library-local at first.
- Keep original book metadata unchanged until the user explicitly chooses to normalize or link authors.
- Store aliases as separate records linked to a canonical author profile.
- Use existing author filter cleanup behavior as inspiration, but move richer author edits to a dedicated author-management UI.
- Later Calibre imports may help seed author names or links, but biography and photos should start as user-managed fields.

## Out Of Scope For First Implementation

- Online author lookup.
- Automatic alias detection beyond suggestions.
- Cross-library profile sharing or a dedicated cloud-sync service; existing library-folder synchronization is unchanged.
- Writing author profile data back into ebook files.

## Candidate Milestone

Milestone 48 follows the accepted Quality reliability slice. Implementation starts after confirmation of the concrete proposed scope and storage choices. Online lookup, aliases and author normalization remain future work.
