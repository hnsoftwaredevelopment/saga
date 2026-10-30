# Author Profiles

## Status

Milestone 48 profiles and explicit author renaming are implemented and user-tested in PR #49. The supplied author icon, 318-book author and window resizing are accepted. Review follow-ups preserve search selection and use bounded metadata refreshes without discarding loaded covers. See [the profile design](../superpowers/specs/2026-10-22-saga-milestone-48-author-profiles-design.md), [rename design](../superpowers/specs/2026-10-22-saga-author-rename-design.md) and [accepted rename checklist](../manual-tests/author-rename-checklist.md).

## Context

Saga stores authors and book-author links as library database entities. Milestone 48 reuses those identities for user-maintained profiles without changing book metadata. Orphan-author cleanup preserves authors with profile content.

## Goals

- [Recognize and deliberately merge author-name variants](author-variants.md): manual merging is implemented in Milestone 53, visual comparison and conservative suggestions in Milestone 54. Explicit rename remains a separate action; new target names and pseudonym/alias registration remain later work.
- Add an author profile screen.
- First slice: show the existing name and store an optional photo, website URL and short biography.
- First slice: searchable authors and their books in the active library; library-local, portable photos and explicit save/cancel behavior.
- Implemented: explicitly rename the same author and all linked books, preserving the profile. Block an existing destination name without merging. Book details retain per-book author assignment.
- Milestone 53 implemented: explicitly select multiple authors and merge into one selected existing author, with per-field biography/website/photo choices, duplicate-book protection and transactional metadata synchronization. This is separate from rename; suggestions are added in Milestone 54. User-test record: [Milestone 53 checklist](../manual-tests/milestone-53-checklist.md).
- Milestone 54 implemented: side-by-side source photos, complete biographies and websites with visible result choices, plus conservative author-name duplicate suggestions and explicit comparison. The existing merge transaction is reused; online identity lookup, pseudonyms and broader alias registration remain future work. [Milestone 54 checklist](../manual-tests/milestone-54-checklist.md).
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
