# Ebook Manager Milestone 3.1: Background Import Agent Design

## 1. Purpose

Milestone 3.1 makes large imports usable for real Calibre-sized libraries. A scan of thousands of books must not feel like the app disappeared into a tunnel. The application should keep responding while import work continues, show live progress, refresh the visible library as books are added, and warn the user before closing while an import job is still active.

The immediate driver is a Calibre library with more than 15,000 books. The current app already performs scan/import asynchronously enough to avoid freezing in simple cases, but the UI only refreshes after the whole import completes. That means the database grows while the visible library stays stale. Milestone 3.1 fixes that by treating scan/import as a first-class background job.

## 2. User Outcomes

After this milestone, a user can start a large scan and continue using the library while the import runs.

The user can:

- see that an import job is active;
- see how many files were found and how many have been processed;
- see counts for added, exact duplicate, possible duplicate, and failed items;
- see a concise current status or current file name;
- search and filter the growing library while import continues;
- open the final import result summary after the job completes;
- cancel an active job;
- receive a warning when trying to close the app while a job is still running.

## 3. Scope

Milestone 3.1 includes:

- A single active background import job for the current library.
- Background scan/import orchestration in presentation/application-facing services.
- Progress updates for total files, processed files, outcome counts, current file/status, started/completed state, and cancellation state.
- Periodic library refresh during import, initially after every 25 processed files or at least once per short interval.
- A compact progress surface in the main window, near the status bar or top of the library area.
- A command or button to open the latest import result window.
- A cancel command for the active job.
- A close warning if the main window is closed while the import agent is active.
- Tests for progress state, job lifecycle, cancellation, and live-refresh trigger behavior.

Milestone 3.1 excludes:

- Pause and resume.
- Running multiple import jobs at the same time.
- Persisting unfinished import jobs across app restarts.
- A dedicated long-running service process outside the desktop app.
- A queue for different libraries.
- Rich per-file live log UI beyond the existing final import result summary.

## 4. Architecture

The current `ImportService.ImportAsync` returns one `ImportBatchResult` after all files have been processed. For live progress, this milestone adds an incremental import path without breaking existing callers.

The design introduces:

- `ImportProgress`: immutable progress snapshot.
- `ImportProgressItem`: per-file progress event for the latest processed item.
- `ImportJobViewModel`: observable UI-facing state for the active job.
- `ImportAgent`: orchestrates scan/import on a background task and exposes progress to `LibraryViewModel`.

`ImportService` should expose a progress-aware overload or callback:

```csharp
Task<ImportBatchResult> ImportAsync(
    IReadOnlyList<string> sourcePaths,
    IProgress<ImportProgress>? progress,
    CancellationToken cancellationToken = default)
```

The existing overload remains and delegates to the new overload with `progress: null`.

The progress callback is raised after each file result is recorded. This keeps the source of truth in the import pipeline and avoids guessing from SQLite.

## 5. UI Behavior

The main window shows a compact import status card when a job is active or recently completed.

Suggested content:

- Title: `Importing books...` or localized equivalent.
- Main text: `Processed 125 of 15000`.
- Counts: added, duplicates, possible duplicates, failed.
- Indeterminate state while the scanner is finding files and the total is not known.
- Determinate progress once the file list is known.
- Buttons:
  - `Details` opens the latest import result when available.
  - `Cancel` requests cancellation while active.

When the job completes, the card remains visible with final counts until dismissed or until another import starts. The existing import result window can still open automatically at completion if that does not interrupt work too aggressively. If it feels intrusive during testing, completion should instead show the card with a `Details` button.

## 6. Live Refresh

The library view currently keeps an in-memory `books` list and refreshes from SQLite only at startup or after a completed import. During background import, `LibraryViewModel` should refresh periodically.

Initial refresh policy:

- refresh after every 25 processed items;
- also refresh when the job completes;
- do not refresh after every single book, because that would make large imports expensive and visually noisy;
- keep current filters, search text, selected view, and selected sort option.

If a selected book still exists after refresh, selection should remain stable. If it no longer exists, existing selection fallback behavior is acceptable.

## 7. Cancellation And Closing

Cancellation is cooperative. The import agent owns a `CancellationTokenSource` for the active job. Pressing `Cancel` requests cancellation. The job transitions to a cancelled state after the current file-level operation responds to the token.

Closing behavior:

- if no job is active, close normally;
- if a job is active, show a confirmation dialog;
- if the user chooses not to close, keep the app open and the job running;
- if the user chooses to close, request cancellation and then close.

In this milestone, closing does not wait for a graceful checkpoint beyond requesting cancellation. A later version may add a more careful shutdown sequence.

## 8. Error Handling

Per-file import errors remain import item failures and do not stop the job. This includes source unreadable, metadata read failure, copy failure, duplicate race, and result persistence problems where recoverable.

Unexpected job-level errors should:

- mark the import job as failed;
- keep the app open;
- expose a short safe message in the progress card;
- avoid full source paths in user-facing messages.

The existing final import result window remains the place where item-level outcomes are inspected.

## 9. Localization

New user-facing strings go through resource files. Dutch and English should be complete for:

- import active title;
- scanning title;
- processed count;
- added count;
- duplicate count;
- failed count;
- cancel;
- details;
- close warning title/message.

German, French, Spanish, and Italian can keep English fallback values for now, matching the current localization depth.

## 10. Testing Strategy

Automated tests should cover:

- `ImportService` progress callback receives one update per processed item.
- Progress counts are correct for added, exact duplicate, possible duplicate, and failed items.
- Cancellation stops the import job and marks state as cancelled.
- `LibraryViewModel` starts a background scan/import without awaiting completion in the command path.
- `LibraryViewModel` triggers refresh after progress thresholds and on completion.
- Close-warning state is exposed when a job is active.

Manual tests should cover:

- Start a large folder scan and confirm the UI remains responsive.
- Confirm visible books appear while import is still running.
- Confirm search/filter works during import.
- Confirm cancel stops a long import.
- Confirm closing while import is active shows a warning.
- Confirm final import result counts match the progress card.

## 11. Completion Criteria

Milestone 3.1 is complete when:

- scan/import progress is visible in the main UI;
- the library refreshes during long imports;
- the app remains usable while importing;
- active imports can be cancelled;
- closing during active import warns the user;
- automated tests pass;
- release build succeeds;
- manual checklist is added under `docs/manual-tests/`;
- README documents the background import behavior.
