# Persistence


> Current implementation status: persistence is implemented through `OpenCad2D.Persistence` and integrated into the Avalonia App. The app supports New, Open, Save and Save As, keyboard shortcuts, viewport save/restore, current file path, dirty-state tracking and save-confirmation dialogs before New, Open and window close.

Persistence allows the user to save a drawing to disk and reopen it in a later session.

This document describes the design, architecture, file format, data model, dirty state tracking, file commands and testing expectations for persistence in OpenCad2D.

---

## Overview

OpenCad2D uses an internal JSON format with the extension `.opencad2d.json`.

The format is not intended for interoperability with other CAD applications. Its goal is reliable save and reopen for OpenCad2D drawings.

Serialization logic lives in a dedicated project `OpenCad2D.Persistence`. The App calls a serializer interface and does not know the file format details. The domain model in `OpenCad2D.Core` does not know about serialization.

A `version` field is included from the beginning so future format changes can be migrated incrementally.

---

## Project structure

A new project is added to the solution:

```text
src/
  OpenCad2D.Persistence/
    OpenCad2D.Persistence.csproj
    IDocumentSerializer.cs
    JsonDocumentSerializer.cs
    Dto/
      DocumentDto.cs
      DocumentSettingsDto.cs
      ViewportStateDto.cs
      LayerDto.cs
      EntityDto.cs
      LineEntityDto.cs
      CircleEntityDto.cs
      ArcEntityDto.cs
      PolylineEntityDto.cs
```

### Dependency rules

`OpenCad2D.Persistence` depends on `OpenCad2D.Core` and `OpenCad2D.Geometry`.

`OpenCad2D.App` depends on `OpenCad2D.Persistence`.

The full dependency graph becomes:

```text
OpenCad2D.App
  -> OpenCad2D.Persistence
      -> OpenCad2D.Core
          -> OpenCad2D.Geometry
  -> OpenCad2D.Tools
      -> OpenCad2D.Interaction
          -> OpenCad2D.Core
```

`OpenCad2D.Persistence` must not depend on `OpenCad2D.Tools`, `OpenCad2D.Interaction` or `OpenCad2D.App`.

---

## File format

Files use the `.opencad2d.json` extension.

Encoding is UTF-8. Line endings are platform-independent.

The top-level structure is:

```json
{
  "version": 1,
  "savedAt": "2025-05-09T14:32:00Z",
  "settings": {
    "currentLayerId": "layer-0"
  },
  "viewport": {
    "panX": 120.5,
    "panY": -40.0,
    "zoom": 1.5
  },
  "layers": [
    {
      "id": "layer-0",
      "name": "Layer 0",
      "color": "#FFFFFF",
      "lineWeight": 1.0,
      "isVisible": true,
      "isLocked": false
    }
  ],
  "entities": [
    {
      "type": "Line",
      "id": "e1a2b3c4",
      "layerId": "layer-0",
      "startX": 0.0,
      "startY": 0.0,
      "endX": 100.0,
      "endY": 50.0
    },
    {
      "type": "Circle",
      "id": "e5d6e7f8",
      "layerId": "layer-0",
      "centerX": 200.0,
      "centerY": 100.0,
      "radius": 30.0
    }
  ]
}
```

### version

`version` is an integer identifying the format schema.

The current version is `1`.

Future format changes that are not backward-compatible must increment this number.

When loading, the serializer checks `version` first and rejects files with unknown versions.

### savedAt

`savedAt` is an ISO 8601 UTC timestamp.

It is informational only and is not used during loading.

### settings

`settings` stores document-level settings that belong to the CAD model:

```text
currentLayerId -> the layer active at save time
```

### viewport

`viewport` stores the viewport state at save time:

```text
panX -> horizontal pan offset in model coordinates
panY -> vertical pan offset in model coordinates
zoom -> zoom scale factor
```

The App provides the viewport state before saving and applies it after loading.

Viewport state is not a document concept. It is provided and consumed by the App layer.

### layers

`layers` is an ordered array of layer objects.

Each layer contains:

```text
id          string identifier
name        display name
color       hex color string such as #FFFFFF
lineWeight  float
isVisible   bool
isLocked    bool
```

At least one layer must exist. The first layer in the file does not need to be the default layer; the current layer is identified by `settings.currentLayerId`.

### entities

`entities` is an array of entity objects.

Each entity has a `type` discriminator that identifies which entity class to deserialize into.

The serializer uses this field to dispatch to the correct DTO and reconstruct the correct domain entity.

Supported type values for version 1:

```text
"Line"
"Circle"
"Arc"
"Polyline"
```

Unknown `type` values should be skipped with a warning, not treated as fatal errors. This allows files created by a newer version to be partially loaded by an older version.

---

## DTO model

DTOs are plain C# records or classes in `OpenCad2D.Persistence.Dto`.

They have no business logic. They are pure data containers for JSON serialization.

DTOs must not reference domain entity types directly. Conversion between DTOs and domain types is the responsibility of `JsonDocumentSerializer`.

### DocumentDto

```csharp
public class DocumentDto
{
    public int Version { get; set; }
    public string SavedAt { get; set; }
    public DocumentSettingsDto Settings { get; set; }
    public ViewportStateDto Viewport { get; set; }
    public List<LayerDto> Layers { get; set; }
    public List<EntityDto> Entities { get; set; }
}
```

### DocumentSettingsDto

```csharp
public class DocumentSettingsDto
{
    public string CurrentLayerId { get; set; }
}
```

### ViewportStateDto

```csharp
public class ViewportStateDto
{
    public double PanX { get; set; }
    public double PanY { get; set; }
    public double Zoom { get; set; }
}
```

### LayerDto

```csharp
public class LayerDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Color { get; set; }
    public double LineWeight { get; set; }
    public bool IsVisible { get; set; }
    public bool IsLocked { get; set; }
}
```

### EntityDto

`EntityDto` is the base for all entity DTOs.

```csharp
public abstract class EntityDto
{
    public string Type { get; set; }
    public string Id { get; set; }
    public string LayerId { get; set; }
}
```

### LineEntityDto

```csharp
public class LineEntityDto : EntityDto
{
    public double StartX { get; set; }
    public double StartY { get; set; }
    public double EndX { get; set; }
    public double EndY { get; set; }
}
```

### CircleEntityDto

```csharp
public class CircleEntityDto : EntityDto
{
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Radius { get; set; }
}
```

### ArcEntityDto

```csharp
public class ArcEntityDto : EntityDto
{
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Radius { get; set; }
    public double StartAngleDegrees { get; set; }
    public double EndAngleDegrees { get; set; }
}
```

### PolylineEntityDto

```csharp
public class PolylineEntityDto : EntityDto
{
    public bool IsClosed { get; set; }
    public List<PointDto> Vertices { get; set; }
}

public class PointDto
{
    public double X { get; set; }
    public double Y { get; set; }
}
```

---

## IDocumentSerializer

`IDocumentSerializer` is the public interface exposed by `OpenCad2D.Persistence`.

```csharp
public interface IDocumentSerializer
{
    DocumentDto Serialize(CadDocument document, string currentLayerId, ViewportStateDto viewport);
    CadDocument Deserialize(DocumentDto dto, out string currentLayerId, out ViewportStateDto viewport);
    void SaveToFile(DocumentDto dto, string filePath);
    DocumentDto LoadFromFile(string filePath);
}
```

The App calls `Serialize` before saving, populating the `ViewportStateDto` from the current viewport.

The App calls `Deserialize` after loading, then applies the returned `currentLayerId` and `ViewportStateDto` to the workspace and viewport.

`SaveToFile` and `LoadFromFile` handle the JSON encoding and file I/O.

---

## JsonDocumentSerializer

`JsonDocumentSerializer` implements `IDocumentSerializer` using `System.Text.Json`.

### Serialization

Convert `CadDocument` layers to `LayerDto` list.

Convert each entity in `CadDocument` to the appropriate `EntityDto` subclass based on the entity type.

Set `DocumentDto.Version = 1`.

Set `DocumentDto.SavedAt` to the current UTC time in ISO 8601 format.

### Deserialization

Check `DocumentDto.Version`. If the version is unknown, throw a `UnsupportedDocumentVersionException` with a clear message.

Reconstruct layers from `LayerDto` list and add them to a new `CadDocument`.

For each entity in `DocumentDto.Entities`, dispatch on `EntityDto.Type` and reconstruct the domain entity. Unknown types are skipped. A skipped entity count can be returned or logged.

Return the reconstructed `CadDocument`, the `currentLayerId` string and the `ViewportStateDto`.

### Polymorphic entity serialization

`System.Text.Json` does not serialize polymorphic types automatically without configuration.

Use one of these approaches:

**Option A: Custom JsonConverter**

Write a `EntityDtoJsonConverter : JsonConverter<EntityDto>` that reads the `type` field first and dispatches to the correct subclass.

This is the most explicit and reliable approach.

**Option B: JsonDerivedType attributes (.NET 7+)**

Annotate `EntityDto` with `[JsonDerivedType]` attributes:

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(LineEntityDto), "Line")]
[JsonDerivedType(typeof(CircleEntityDto), "Circle")]
[JsonDerivedType(typeof(ArcEntityDto), "Arc")]
[JsonDerivedType(typeof(PolylineEntityDto), "Polyline")]
public abstract class EntityDto { ... }
```

Option B is simpler but requires that `type` appears first in the JSON or that the serializer is configured to read ahead. Test this carefully.

Option A is recommended if unknown type skipping is important for forward compatibility.

### JsonSerializerOptions

Use indented JSON for readability:

```csharp
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
```

---

## Dirty state tracking

`CadWorkspace` exposes an `IsDirty` property that indicates whether the document has unsaved changes.

### How dirty state is tracked

`CommandHistory` executes and undoes commands. Each time a command is executed or undone, the document changes.

The workspace compares a saved generation counter to the current command history state.

Suggested approach using a generation counter on `CommandHistory`:

```csharp
// CommandHistory exposes:
int CurrentGeneration { get; }  // increments on every Execute or Undo

// CadWorkspace tracks:
int _savedGeneration = 0;

bool IsDirty => _commandHistory.CurrentGeneration != _savedGeneration;

void MarkSaved() => _savedGeneration = _commandHistory.CurrentGeneration;
```

When a new document is created, `_savedGeneration` is reset to match the current generation.

When a file is saved, `MarkSaved()` is called.

When a file is loaded, `_savedGeneration` is reset to match the state after loading.

### Window title

The App updates the window title to reflect the current file name and dirty state.

```text
OpenCad2D - drawing.opencad2d        (clean)
OpenCad2D - drawing.opencad2d *      (dirty)
OpenCad2D - Untitled *               (new unsaved document)
```

The title is updated whenever `IsDirty` changes or the file path changes.

---

## File commands

Four file commands are added to the App:

```text
New
Open
Save
Save As
```

These commands live in the App layer. They use file dialogs provided by Avalonia's `StorageProvider` API.

### New

```text
if document is dirty:
    show "Save changes?" dialog (Yes / No / Cancel)
    Yes    -> Save (or Save As if no current path), then proceed
    No     -> discard changes, proceed
    Cancel -> abort New

create a new empty CadDocument
reset CadWorkspace to new document state
clear command history
clear current file path
reset dirty state
reset viewport to default
```

### Open

```text
if document is dirty:
    show "Save changes?" dialog (Yes / No / Cancel)
    Yes    -> Save (or Save As), then proceed
    No     -> proceed
    Cancel -> abort Open

show file open dialog (filter: *.opencad2d.json)
if user cancels: abort

call serializer.LoadFromFile(path)
call serializer.Deserialize(dto, out currentLayerId, out viewport)
load document into CadWorkspace
apply currentLayerId to workspace
apply viewport to canvas
set current file path
reset dirty state
```

### Save

```text
if current file path is not set:
    trigger Save As
    return

call serializer.Serialize(document, currentLayerId, viewportState)
call serializer.SaveToFile(dto, currentFilePath)
call workspace.MarkSaved()
```

### Save As

```text
show file save dialog (filter: *.opencad2d.json, default extension: .opencad2d.json)
if user cancels: abort

set current file path to chosen path
trigger Save
```

### Keyboard shortcuts

```text
Ctrl+N  -> New
Ctrl+O  -> Open
Ctrl+S  -> Save
Ctrl+Shift+S -> Save As
```

These should be registered in the App similarly to existing shortcuts.

---

## Viewport serialization

The viewport state (pan and zoom) belongs to the App, not to `CadDocument`.

The App provides the current viewport state before serialization and applies it after deserialization.

```text
Before saving:
  var viewport = new ViewportStateDto
  {
      PanX = canvas.PanX,
      PanY = canvas.PanY,
      Zoom = canvas.Zoom
  };
  var dto = serializer.Serialize(document, currentLayerId, viewport);

After loading:
  serializer.Deserialize(dto, out var currentLayerId, out var viewport);
  canvas.PanX = viewport.PanX;
  canvas.PanY = viewport.PanY;
  canvas.Zoom = viewport.Zoom;
  workspace.SetCurrentLayer(currentLayerId);
```

The viewport state should be applied before the canvas renders the first frame after loading.

---

## Error handling

File operations can fail. All errors should be caught and reported to the user through the UI, not by crashing.

### Load errors

```text
File not found           -> show error dialog
Invalid JSON             -> show error dialog
Unknown version          -> show specific error: "This file was created by a newer version of OpenCad2D."
Missing required fields  -> show error dialog
Unknown entity types     -> load partial document, show warning: "Some entities could not be loaded."
```

### Save errors

```text
Disk full          -> show error dialog
Permission denied  -> show error dialog
Path too long      -> show error dialog
```

Error dialogs should be shown by the App using Avalonia's dialog API. The serializer should throw typed exceptions that the App can catch and interpret.

Suggested exception types in `OpenCad2D.Persistence`:

```csharp
public class DocumentLoadException : Exception { ... }
public class UnsupportedDocumentVersionException : DocumentLoadException { ... }
public class DocumentSaveException : Exception { ... }
```

---

## UX for "Save changes?" dialog

The unsaved changes dialog appears before New and Open when `IsDirty` is true.

It should offer three choices:

```text
Save       -> save the file, then proceed with the original action
Don't Save -> discard changes, proceed with the original action
Cancel     -> abort the original action entirely
```

This is the standard behavior for CAD and document applications.

The dialog belongs to the App layer and uses Avalonia's dialog API.

---

## Implemented files

```text
src/
  OpenCad2D.Persistence/
    OpenCad2D.Persistence.csproj
    IDocumentSerializer.cs
    JsonDocumentSerializer.cs
    DocumentLoadException.cs
    UnsupportedDocumentVersionException.cs
    DocumentSaveException.cs
    Dto/
      DocumentDto.cs
      DocumentSettingsDto.cs
      ViewportStateDto.cs
      LayerDto.cs
      EntityDto.cs
      LineEntityDto.cs
      CircleEntityDto.cs
      ArcEntityDto.cs
      PolylineEntityDto.cs

tests/
  OpenCad2D.Persistence.Tests/
    OpenCad2D.Persistence.Tests.csproj
    JsonDocumentSerializerTests.cs
    RoundTripTests.cs
```

---

## Implemented/modified files

```text
OpenCad2D.sln
  -> add OpenCad2D.Persistence project
  -> add OpenCad2D.Persistence.Tests project

OpenCad2D.App/
  OpenCad2D.App.csproj        -> add reference to OpenCad2D.Persistence
  MainViewModel.cs            -> add CurrentFilePath, IsDirty, window title logic
  MainWindow.axaml            -> add File menu (New, Open, Save, Save As) and keyboard shortcuts
  MainWindow.axaml.cs         -> file command handlers, Save/Open dialogs, unsaved changes dialog
  CadCanvas.cs or Viewport    -> expose PanX, PanY, Zoom for reading and writing

OpenCad2D.Core/
  CommandHistory.cs           -> add CurrentGeneration counter

OpenCad2D.Tools/
  CadWorkspace.cs             -> add IsDirty, MarkSaved(), LoadDocument(CadDocument, string layerId)
```

---

## Tests

### JsonDocumentSerializer — serialization

```text
Serialize a document with one layer and no entities -> valid DocumentDto
Serialize a document with a LineEntity -> EntityDto with type "Line" and correct coordinates
Serialize a document with a CircleEntity -> EntityDto with type "Circle" and correct values
Serialize a document with a PolylineEntity -> correct vertices and isClosed
Serialize preserves entity id and layer id
Serialize includes version = 1
Serialize includes currentLayerId in settings
Serialize includes viewport pan and zoom values
```

### JsonDocumentSerializer — deserialization

```text
Deserialize a valid v1 JSON string -> correct CadDocument with expected entities
Deserialize restores layer names, colors, visibility and locked state
Deserialize restores entity geometry precisely
Deserialize returns correct currentLayerId
Deserialize returns correct viewport pan and zoom
Deserialize with unknown entity type -> skips entity, returns partial document
Deserialize with unknown version -> throws UnsupportedDocumentVersionException
Deserialize with invalid JSON -> throws DocumentLoadException
```

### Round-trip tests

```text
Serialize then deserialize a document with multiple entity types -> document equals original
Round-trip preserves entity ids
Round-trip preserves layer ids
Round-trip preserves viewport state
Round-trip preserves current layer selection
```

### Dirty state

```text
New document -> IsDirty is false
Execute a command -> IsDirty is true
MarkSaved -> IsDirty is false
Execute another command after save -> IsDirty is true
Undo back to saved state -> IsDirty is false (if generation matches)
```

### File service (integration tests, optional)

```text
SaveToFile then LoadFromFile -> document survives file round-trip
SaveToFile to read-only path -> throws DocumentSaveException
LoadFromFile from nonexistent path -> throws DocumentLoadException
```

---

## Versioning strategy

Version 1 is the initial format.

When the format changes in a breaking way, increment the version number.

For each supported version, a migration path should eventually be added. The recommended approach is to implement `IDocumentMigrator` interfaces in the future:

```csharp
public interface IDocumentMigrator
{
    int FromVersion { get; }
    int ToVersion   { get; }
    DocumentDto Migrate(DocumentDto dto);
}
```

For now, the serializer reads version 1 only and rejects everything else with a clear message.

---

## Design rules to preserve

```text
OpenCad2D.Persistence must not depend on OpenCad2D.Tools, OpenCad2D.Interaction or OpenCad2D.App.
OpenCad2D.Core must not depend on OpenCad2D.Persistence.
Domain entities must not carry serialization attributes.
DTOs must not carry CAD business logic.
The App provides viewport state before saving and applies it after loading.
File dialogs belong to the App.
Error handling for load/save belongs to the App.
Dirty state is tracked through CommandHistory generation, not through document mutation flags.
The serializer does not know about Avalonia.
Unknown entity types in a file must not crash the loader.
```
