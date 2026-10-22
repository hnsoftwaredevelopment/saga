# Author Profiles

## Status

Milestone 48 profiles and explicit author renaming are implemented and user-tested in PR #49. The supplied author icon is accepted. Only the final small-window layout follow-up remains. See [the profile design](../superpowers/specs/2026-10-22-saga-milestone-48-author-profiles-design.md), [rename design](../superpowers/specs/2026-10-22-saga-author-rename-design.md) and [current checklist](../manual-tests/author-rename-checklist.md).

## Context

Saga stores authors and book-author links as library database entities. Milestone 48 reuses those identities for user-maintained profiles without changing book metadata. Orphan-author cleanup preserves authors with profile content.

## Goals

- Add an author profile screen.
- First slice: show the existing name and store an optional photo, website URL and short biography.
- First slice: searchable authors and their books in the active library; library-local, portable photos and explicit save/cancel behavior.
- Implemented: explicitly rename the same author and all linked books, preserving the profile. Block an existing destination name without merging. Book details retain per-book author assignment.
- Later: birth date and optional death date.
- Later: support author aliases, pseudonyms, and alternate spellings.
- Show all books by the author in the active library.
- Later: include books stored under linked aliases when showing an author's books.
- Later: consider linking author variants as a separately specified feature. Implicit author merging is explicitly excluded.

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

Milestone 48 follows the accepted Quality reliability slice. Scope, storage and transactional rename choices were confirmed before implementation. Online lookup, aliases and broader author normalization remain future work.
