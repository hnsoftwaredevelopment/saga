# Import Performance And Large Library Hardening

## Status

In progress as Milestone 10.

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

## Remaining Slices

- Add aggregate phase summaries per import run.
- Use phase diagnostics to identify and optimize the slowest path in large scans.
- Improve user-facing import progress for files that take unusually long.
- Consider safe optional cloud-file hydration as a later opt-in behavior.
