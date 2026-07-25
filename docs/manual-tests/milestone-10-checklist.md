# Milestone 10 Manual Test Checklist

## Import Performance Diagnostics

- [ ] Import a small EPUB or PDF and open the import result details.
- [ ] Confirm the details grid shows total duration and the new phase timing column.
- [ ] Confirm phase timings include useful parts such as `hash`, `meta`, `dup`, `copy`, or `db`.
- [ ] Open the same import from import history and confirm the phase timings are still available.
- [ ] Import or scan a folder with multiple formats and confirm phase timings appear per row.
- [ ] Search the import result details for a phase label such as `meta` or `copy` and confirm matching rows remain visible.

## Regression Checks

- [ ] Confirm added, duplicate, possible duplicate, and failed counts still look correct.
- [ ] Confirm retry failed imports still works for retryable failed source paths.
- [ ] Confirm title-match linking still works from import history.
- [ ] Confirm unsupported or unavailable cloud-only files fail without crashing.
