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
- document mutation validation;
- pure measurement services in `OpenCad2D.Core.Measurements`.

### OpenCad2D.Interaction

UI-independent user interaction services:

- hit testing;
- selection;
- snapping;
- grid settings and grid snapping.

### OpenCad2D.Tools

UI-independent CAD tools:

- drawing tools, including arc variants and rectangle variants;
- edit tools;
- transform tools;
- grip editing;
- non-mutating measure tools;
- tool controller;
- workspace;
- command line point submission;
- shared point-input constraints such as Ortho and Polar Tracking.

Tools receive points and contextual state. They must not depend on Avalonia.

### OpenCad2D.Persistence

Document serialization and deserialization.

It depends on `Core` and `Geometry`, not on `Tools`, `Interaction` or `App`.

### OpenCad2D.Export

Export services for non-native output formats such as SVG and DXF.

It depends on `Core` and `Geometry`, not on `App`, `Tools`, `Interaction` or `Persistence`.

The exporter reads the document and produces external output. It must not mutate the document and must not affect dirty state. SVG and DXF exporters share this rule.

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
Snap/Ortho bar plus Polar selector in the CAD top bar
Command line
Status bar
```

The file command bar is a protected region and contains New/Open/Save/Save As. It should not be mixed with drawing/editing toolbars.

---


## Point input constraints

Interactive point input can be constrained after the raw cursor point has been resolved.

The current cross-tool constraint layer lives in `OpenCad2D.Tools.Common`:

```text
AngleConstraintSettings      runtime Polar Tracking configuration
AngleConstraintService       pure angular projection service
ToolInputConstraintService   shared entry point used by tools
```

The input order is:

```text
raw pointer/model point
-> snapping
-> Polar Tracking or legacy Ortho
-> preview and command commit
```

Polar Tracking has priority when enabled. Otherwise `ToolInputConstraintService` falls back to legacy Ortho when `ToolContext.IsOrthoEnabled` is true.

This logic belongs to `Tools`, not `App`, so it can be tested without Avalonia and reused by drawing/edit tools.

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


## Curve editing precision boundary

Trim, Break and related modify operations must preserve native geometry and shared topology.

The project-level rule is:

```text
CAD editing operations modify native entities using native geometric parameters.
Sampling is allowed only as temporary support and must not become the permanent source of edited geometry when a native representation exists.
```

Trim and Break target fragmentation is routed through the shared curve-splitting pipeline:

```text
CadTrimService / CadBreakService
-> CadCurveSplitService
-> ICurveAdapter
-> native entity fragments
```

`CadExtendService` follows the same native-geometry direction for supported targets and boundaries. Command services should not duplicate per-entity splitting logic. They should collect intersections or break points, convert them into target-side `CurveCut` values, and ask the split service or target-specific native extension logic to build the final entity.

When one intersection modifies multiple explicit-vertex entities, the intersection point must be computed once and reused as the same logical `Point2D` value for all resulting endpoints/vertices. This avoids micro-gaps after reciprocal Trim/Extend-style operations. `CadIntersectionPoint` carries the shared point plus native parameters for both participating entities.

See `docs/curve-editing.md` for the detailed rules, current supported entity set and deferred work.

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

The App must make this distinction visible after export. A successful export message should explicitly state that export did not save the editable OpenCad2D project and should tell the user whether Save/Save As is still needed for the native `.opencad2d.json` drawing.

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

---

## Entity style and line formats

Rendering and SVG export resolve stroke appearance through this path:

```text
Entity -> LayerId -> Layer -> LineFormatId -> LineFormat
```

The layer is the only source of visual stroke appearance for entities in the current phase. Entity-level style overrides are intentionally out of scope.

The `Assegna` top-bar action changes the `LayerId` of selected entities to the current layer through an undoable replacement command. It must preserve entity geometry and ids.

---

## Escape behavior

The workspace owns the high-level `Esc` policy:

```text
non-selection tool -> return to Selection and keep selection
Selection with selected entities -> clear selection
Selection without selected entities -> no operation
```

Tool-specific cleanup remains inside the active tool, but the fallback policy belongs to `CadWorkspace`, not to Avalonia controls.

## Tool-provided previews

The active-tool preview renderer supports two UI-agnostic protocols from the tools layer.

`IToolPreviewEntityProvider` is for simple previews that can be represented as ordinary transient `CadEntity` instances. Drawing tools, dimension previews and many modify tools use this path.

`IToolPreviewDescriptorProvider` is the richer protocol for previews that also need semantic overlays, such as guide lines, axis lines, point markers, highlighted fragments or selection/zoom windows. The descriptor is still defined in the tools layer and uses only model-space geometry plus semantic preview items; the app layer decides how those items are rendered.

Currently migrated examples include:

- `LineTool` for a simple two-point drawing preview;
- `RectangleTool` and `RectangleBySidesTool` for rectangle previews;
- `CircleTool`, `ArcTool`, `ArcThreePointsTool` and `EllipseTool` for basic curve previews;
- `PolylineTool`, `PolygonTool` and `SplineTool` for multi-point drawing previews;
- `MoveTool`, `CopyTool`, `RotateTool`, `ScaleTool` and `AlignTool` for context-aware modify previews;
- `BreakAtPointTool`, `BreakBetweenPointsTool`, `FilletTool` and `OffsetTool` for entity-based edit previews;
- `MeasureDistanceTool` for its transient distance segment preview;
- `MirrorTool` through `IToolPreviewDescriptorProvider`, including mirrored entities, axis line and endpoint markers;
- `MeasureAngleTool` through `IToolPreviewDescriptorProvider`, including measurement rays and point markers;
- `SelectionTool` and `ZoomWindowTool` through `IToolPreviewDescriptorProvider`, including semantic filled preview windows;
- `GripEditTool` through `IToolPreviewDescriptorProvider`, including preview replacement entities, measurement guides and hot/warm/cold grip markers;
- `ExtendTool` and `TrimTool` through `IToolPreviewDescriptorProvider`, including normal preview entities plus highlighted added/removed fragments;
- `ThreePointDimensionToolBase`, which covers the linear/aligned/radius/diameter/angular dimension tools that expose entity previews.

`CadToolPreviewRenderer` now tries the descriptor provider first and then the entity provider. The previous active-tool concrete fallback dispatch has been removed, so tool-specific preview knowledge stays in the tools layer rather than in the app renderer.

---

## Application diagnostics

`OpenCad2D.App.Diagnostics` contains the minimal application logging abstraction used by the UI layer.

The current logging contract is intentionally small:

- `IApplicationLogger` exposes `Info`, `Warning` and `Error` methods;
- `ApplicationLogEntry` stores timestamp, severity, category, message and optional exception;
- `TraceApplicationLogger` writes entries to `System.Diagnostics.Trace`;
- `InMemoryApplicationLogger` supports focused tests and future diagnostic UI scenarios.

The first consumer is `CadCanvas.HandlePointerPressedException`. Pointer/tool failures are still converted into a recoverable user-facing `ToolResult.Cancelled(...)`, but the full exception is now logged before the canvas is invalidated. This avoids losing stack traces when asynchronous tool input fails.
