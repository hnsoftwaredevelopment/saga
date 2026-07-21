# Milestone 6 Manual Test Checklist

## Settings Structure

- [ ] Open Settings and confirm these sections are visible: General, Appearance, Import, Metadata, Confirmations, Duplicates, Diagnostics.
- [ ] Change the application language and confirm the new Duplicates and Diagnostics settings are translated.
- [ ] Save Settings, close Settings, reopen Settings, and confirm the selected values are preserved.
- [ ] Confirm settings that can be applied immediately do not require an app restart.
- [ ] For future settings that cannot be applied immediately, Saga should show a clear restart-required message.

## Appearance Preferences

- [ ] Open Settings > Appearance and change the theme. Confirm the app theme changes immediately before saving.
- [ ] Click Cancel after changing the theme and confirm the previous theme is restored.
- [ ] Change the default view and confirm the library view changes immediately before saving.
- [ ] Click Cancel after changing the default view and confirm the previous active view is restored.

## Bookshelf Layout

- [ ] Switch to Bookshelf view with enough books to fill the screen.
- [ ] Confirm books wrap across the available width instead of appearing in one horizontal row.
- [ ] Confirm vertical scrolling is available and horizontal scrolling is not needed.

## Duplicate Preferences

- [ ] Open Settings > Duplicates and disable "Show exact duplicate matches by default".
- [ ] Open the duplicate overview and confirm title-only matches are visible immediately.
- [ ] Re-enable the setting and confirm the duplicate overview starts with stricter title and author matches.
- [ ] Confirm the in-window duplicate toggle can still be changed manually.
- [ ] Change duplicate merge defaults for authors, tags, description, cover, publisher, and language.
- [ ] Open a duplicate merge preview and confirm the configured initial actions are selected where the field values differ.
- [ ] Confirm fields with equal values still start as "Do nothing" even when a default action is configured.

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
