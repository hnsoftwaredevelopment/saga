# Milestone 4 Manual Test Checklist

## Settings

- [ ] Open Settings and confirm sections are understandable.
- [ ] Change application language and confirm Settings, toolbar, filters, sort options, and details labels update immediately.
- [ ] Change theme and confirm it still applies after Save.
- [ ] Change default view and confirm it still applies after restart.
- [ ] Change include-subdirectories setting and confirm scan uses it.
- [ ] Change author sort strategy and confirm it persists after reopening Settings.
- [ ] Use the metadata action "Normalize language codes" / "Taalcodes normaliseren" and confirm Saga asks for confirmation before rewriting stored language values.

## Author Sorting

- [ ] Select author sort "Zoals ingevoerd" and confirm author filter order follows visible names.
- [ ] Select author sort "Achternaam, voornaam" and confirm authors sort by last name.
- [ ] Select author sort with Dutch prefixes and confirm names like "Vincent van Gogh" sort under "van Gogh".
- [ ] Confirm author names themselves are not rewritten.

## Language Display

- [ ] Confirm language filter shows friendly display for `nl`, `nl-NL`, `eng`, and `en-US`.
- [ ] Confirm supported language names such as `Nederlands`, `Dutch`, `Deutsch`, `Français`, `Español`, and `Italiano` normalize to Saga language codes.
- [ ] Confirm unsupported language names such as `Latin` stay unchanged.
- [ ] Confirm language filter labels translate when switching UI language, for example `Nederlands` becomes `Dutch` in English.
- [ ] Confirm user tags are not translated when switching UI language.
- [ ] Confirm the details pane shows a localized friendly language name while preserving the editable stored value.
- [ ] Confirm selecting a normalized language filter still filters the expected books.
- [ ] Confirm unusual valid codes such as `lv` do not crash.

## Standard Metadata

- [ ] Select a book and verify publisher, publication date, ISBN, tags, series, formats, and description are visible where expected.
- [ ] Edit publisher, publication date, ISBN, and tags; save; confirm grid/filter/details refresh correctly.
- [ ] Merge duplicates and confirm the existing metadata merge screen still works.
