# Custom Metadata

## Status

Milestone 16 started.

## Context

Saga already supports a strong standard metadata model and user-created views. The next step is custom metadata: user-defined fields that can be attached to books and later used in details, search, filters, views, and Calibre custom-column import.

Custom metadata should make Saga powerful without turning the standard `BookMetadata` model into a catch-all object. Standard metadata remains strongly modeled. Custom metadata lives in a separate definition/value model.

## Goals

- Let a library define its own custom metadata fields.
- Support useful first field types: text, number, date, boolean, single-select, and multi-select.
- Store definitions separately from book values.
- Keep field keys stable when display names are renamed, so views and imports can refer to a durable identifier.
- Keep SQLite authoritative and migration-friendly.
- Prepare for Details editing, custom view columns, search, filtering, and Calibre custom-column import.

## Out Of Scope For Initial Milestone

- Editing custom values in the Details pane.
- Showing custom metadata as grid columns.
- Importing Calibre custom columns.
- Native ebook metadata write-back.

## Milestone 16

Milestone 16 builds custom metadata in small, testable slices.

### Slice 1: Domain And SQLite Foundation

Status: implemented.

- Added `CustomMetadataFieldType`.
- Added `CustomMetadataFieldDefinition`.
- Added `CustomMetadataValue`.
- Added `ICustomMetadataRepository`.
- Added SQLite tables `CustomMetadataFields` and `CustomMetadataValues`.
- Added unique stable keys for custom fields.
- Added typed value storage for text, number, date, and boolean values. Select values initially use text storage.
- Added cascade cleanup from books and field definitions to custom values.
- Added repository tests for definitions, values, rename stability, deletion, and type validation.

### Slice 2: Settings Field Management

Status: implemented.

- Added a Metadata settings section for custom metadata fields.
- Added UI to create fields with a name and type.
- Added UI to rename and delete selected fields.
- Added localized field type names and status feedback.
- Added SettingsViewModel tests for field type options and create/rename/delete flow.

### Slice 3: Details Display And Editing

Status: implemented.

- Added custom metadata values to the Details pane.
- Loads field definitions and existing values for the selected book.
- Supports editing custom values and saving them through the typed repository.
- Added typed Details editors for number, date, and yes/no fields.
- Added localized validation feedback for custom metadata values.
- Blank custom values are removed from storage.
- Custom metadata edits participate in unsaved-changes detection.
- Added BookDetailsViewModel tests for loading, dirty-state detection, and saving custom metadata values.

### Next Slices

- Include custom metadata values in search.
- Make custom fields available as columns in custom views.
- Add filters for useful custom field types.
- Add Calibre custom-column import after Saga's own model has proven stable.
