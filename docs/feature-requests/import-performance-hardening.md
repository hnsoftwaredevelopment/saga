# Import Performance And Large Library Hardening

## Status

Ready for review as Milestone 10.

## Context

Large imports can expose performance problems that are invisible in small libraries. Saga should make slow imports diagnosable before we start changing deeper import behavior. The first focus is therefore reliable timing information per import phase.

## Goals

- Keep large directory scans and imports responsive and predictable.
- Identify whether slow items spend time in hashing, metadata extraction, duplicate checks, managed copy, database save, cleanup, or cloud-file checks.
- Preserve clear import history so slow or failed files can be investigated after a scan is cancelled or completed.
- Improve OneDrive/cloud-only detection without accidentally hydrating remote files.
- Use measurements to guide later batching or repository optimizations.

## Implemented Slices

- Import item diagnostics now store phase timings for availability check, size read, hashing, metadata read, duplicate check, managed copy, database save, and cleanup.
- Import result details show a compact phase timing column.
- Phase timing diagnostics are persisted in SQLite import history.
- Import result details show an aggregate phase summary across all imported files, ordered by total time spent per phase.
- Import phase labels and aggregate summaries are localized and use user-facing names instead of internal diagnostic codes.
- Import result details can handle large import histories without crashing by using bulk refreshes and DataGrid virtualization.
- File hashing uses a shared large-buffer implementation to reduce overhead during large imports.
- Large-library view updates were hardened so grouping, sorting, and bookshelf rendering stay responsive with thousands of books.

## Current Findings

- Exact duplicate detection remains intentionally hash-based. This keeps duplicate handling reliable, but large imports with many already-known books will still spend most time in file recognition.
- OneDrive/cloud-only or unreliable cloud files can still fail when the file provider reports a local file but cannot actually hydrate it. Saga currently treats those as safe failed imports instead of forcing cloud hydration.
- Slow individual files can be investigated by sorting the import details grid by duration.

## Remaining Slices

- Consider safe optional cloud-file hydration as a later opt-in behavior.
- Consider a help or information affordance for the import phase summary if users need more explanation inside the app.
