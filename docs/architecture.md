# Architecture

OpenCad2D is organized around a strict separation of concerns.

---

## Projects

```text
OpenCad2D.Geometry
OpenCad2D.Core
OpenCad2D.Interaction
OpenCad2D.Tools
OpenCad2D.Persistence
OpenCad2D.Export
OpenCad2D.App
```

### OpenCad2D.Geometry

Pure geometry:

- points;
- vectors;
- lines;
- segments;
- circles;
- arcs;
- polylines;
- bounding boxes;
- matrices and transformations;
- intersections;
- distances;
- tolerance handling;
- coordinate systems.

No CAD document concepts and no UI dependencies belong here.

### OpenCad2D.Core

CAD model and document logic:

- entities;
- layers;
- styles;
- reusable line formats;
- document;
- spatial index abstraction;
- commands;
- command history;
- document mutation validation.

### OpenCad2D.Interaction

UI-independent user interaction services:

- hit testing;
- selection;
- snapping;
- grid settings and grid snapping.

### OpenCad2D.Tools

UI-independent CAD tools:

- drawing tools;
- edit tools;
- transform tools;
- grip editing;
- tool controller;
- workspace;
- command line point submission.

Tools receive points and contextual state. They must not depend on Avalonia.

### OpenCad2D.Persistence

Document serialization and deserialization.

It depends on `Core` and `Geometry`, not on `Tools`, `Interaction` or `App`.

### OpenCad2D.Export

Export services for non-native output formats such as SVG.

It depends on `Core` and `Geometry`, not on `App`, `Tools`, `Interaction` or `Persistence`.

The exporter reads the document and produces external output. It must not mutate the document and must not affect dirty state.

### OpenCad2D.App

Avalonia application:

- windows;
- file dialogs;
- XAML layout;
- rendering;
- viewport;
- keyboard/mouse forwarding;
- ViewModels;
- property panel;
- layer manager window;
- line format manager window.

---

## Dependency direction

Allowed graph:

```text
App -> Persistence -> Core -> Geometry
App -> Export -> Core -> Geometry
App -> Tools -> Interaction -> Core -> Geometry
```

The App may depend on `Tools`, `Persistence` and `Export`. `Persistence` and `Export` must remain independent of the App.

---

## Stable UI zones

The main window is intentionally split into stable zones:

```text
File command bar
CAD/session top bar
Left tool panel
Canvas
Right property panel
Snap/Ortho bar
Command line
Status bar
```

The file command bar is a protected region and contains New/Open/Save/Save As. It should not be mixed with drawing/editing toolbars.

---

## Tool-specific snap modes

The tool layer may expose phase-specific snap behavior through `ISnapModeProvider`. This keeps global snap settings stable while allowing a tool phase to request a narrower snap set.

Current examples:

```text
SelectionTool -> SnapKind.EntityOnly
MoveTool waiting for entity selection -> SnapKind.EntityOnly
MoveTool waiting for base/destination point -> ToolContext.EnabledSnaps
```

`SnapKind.Entity` is selection-oriented and is not included in `SnapKind.All`, which remains the set of geometric point snaps.

---

## Document mutation boundary

`CadDocument` is the public boundary for document mutation.

Commands and tools must use:

```csharp
AddEntity(...)
RemoveEntity(...)
RemoveEntities(...)
ReplaceEntity(...)
ReplaceEntities(...)
```

Direct mutation of `EntityCollection` should be avoided outside document internals.

This ensures:

- locked-layer protection;
- layer validation;
- spatial index updates;
- consistent future document events.

---

## Commands

User-facing document changes should go through commands.

Implemented command families include:

- add;
- delete;
- replace;
- move;
- copy;
- rotate;
- scale;
- transform;
- update layers;
- composite operations.

`CommandHistory` tracks undo/redo and command generation used by dirty-state tracking.

---

## Dirty state

Dirty state is based on command history generation.

Concept:

```text
CurrentGeneration != SavedGeneration -> dirty
CurrentGeneration == SavedGeneration -> clean
```

The App uses this to show `*` in the title/file bar and to trigger “Save changes?” before destructive file operations.

---

## Persistence architecture

The document file stores drawing content:

```text
.opencad2d.json
```

It includes:

- format version;
- layers;
- entities;
- current layer;
- viewport state.

The file does not store user-local application settings such as window position or user shortcuts.

---


## Export architecture

Export is separate from persistence.

```text
Persistence -> native OpenCad2D document save/reopen
Export      -> external derived output such as SVG
```

SVG export currently lives in `OpenCad2D.Export` and is intentionally UI-independent. The App owns only file dialogs and user-facing error messages.

Export must not:

- change `CurrentFilePath`;
- call `MarkSaved()`;
- clear the dirty marker;
- mutate the document.

SVG export uses visible document bounds to build the `viewBox`, writes a background rectangle matching the dark canvas and keeps the same visual Y orientation as the OpenCad2D canvas.

## Coordinate systems and command line

Entities are stored in WCS/model coordinates.

Typed command input is interpreted as UCS input and converted before reaching the active tool.

Supported command line point input:

```text
absolute point
relative point
direct distance
```

The command line is an input mechanism. It does not own CAD behavior and does not create entities directly.

---

## Layers

Layer responsibilities:

- identity;
- name;
- `LineFormatId`;
- visibility;
- locked state;
- future fill color;
- future draw order.

Stroke appearance is resolved through `Document.LineFormats`, not directly from layer color/weight fields.

Current layer rule:

```text
current layer must be visible and unlocked
```

Layer Manager applies batch layer changes through an undoable command. Line Format Manager applies reusable stroke-format changes through `UpdateLineFormatsCommand`.

---

## Line formats

Line formats are reusable stroke definitions stored in `CadDocument.LineFormats`.

Resolution path:

```text
Entity -> LayerId -> Layer -> LineFormatId -> LineFormat
```

A line format contains:

```text
Id
Name
Color
LineWeight
LineStyle
```

Rendering and SVG export must resolve appearance through this path. They must not use per-entity style overrides for the current layer-based appearance model.

Dash patterns are defined by `LineStyleDashPattern` in model units and are scaled by the viewport when rendered on screen.

---

## Selection, snapping and rendering queries

Different systems intentionally use different document queries:

```text
Rendering  -> visible entities intersecting viewport
Selection  -> selectable entities
Snapping   -> visible/snappable candidates near cursor
Editing    -> selected entities, then CadDocument mutation checks
```

Do not reuse rendering filters as mutation rules.

---

## Viewport culling

Viewport culling is a rendering optimization only.

It must not:

- remove entities;
- deselect entities;
- affect snapping globally;
- modify the document.

It should only reduce the set of entities drawn in the current frame.

---

## Managers

Manager windows, such as Layer Manager and Line Format Manager, should be separate windows/dialogs instead of filling the main CAD screen.

General manager pattern:

```text
open dialog
edit copy of data
Cancel -> discard
OK -> apply through command/service
```

This keeps the main screen operational and prevents accidental document mutation while the user experiments in a manager window.
