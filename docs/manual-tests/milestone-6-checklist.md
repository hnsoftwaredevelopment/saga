# Milestone 6 Manual Test Checklist

## Settings Structure

- [ ] Open Settings and confirm these sections are visible: General, Appearance, Import, Metadata, Confirmations, Duplicates, Diagnostics.
- [ ] Change the application language and confirm the new Duplicates and Diagnostics settings are translated.
- [ ] Save Settings, close Settings, reopen Settings, and confirm the selected values are preserved.

## Duplicate Preferences

- [ ] Open Settings > Duplicates and disable "Show exact duplicate matches by default".
- [ ] Open the duplicate overview and confirm title-only matches are visible immediately.
- [ ] Re-enable the setting and confirm the duplicate overview starts with stricter title and author matches.
- [ ] Confirm the in-window duplicate toggle can still be changed manually.

## Diagnostics Preferences

- [ ] Open Settings > Diagnostics and confirm the diagnostics option is visible.
- [ ] Save the diagnostics option in both enabled and disabled states and confirm it persists after reopening Settings.
- [ ] Confirm current import history and diagnostics remain available while this foundation setting is not yet used to hide details.

## Regression Checks

- [ ] Confirm General settings still save the selected app language.
- [ ] Confirm Appearance settings still save theme and default startup view.
- [ ] Confirm Import settings still save the scan-subdirectories preference.
- [ ] Confirm Metadata settings still save author sorting and language normalization still works.
- [ ] Confirm Confirmations settings still save delete confirmation behavior.
