# OpenCad2D - AI Handoff Document

This document describes the current state, architecture and development rules of OpenCad2D. It is intended for future AI-assisted development sessions and contributors.

Update this file after every meaningful development phase.

---

## Project purpose

OpenCad2D is an experimental 2D CAD application built with C#, .NET 8 and Avalonia UI.

The goal is to build a small but serious CAD system with:

- clean architecture;
- strong testability;
- UI-independent CAD behavior;
- incremental development;
- clear separation between geometry, document model, interaction logic, tools, persistence and UI.

---

## Current implemented status

The project currently supports:

### Drawing

- v0.4 dimension work has started: dimension styles, non-associative horizontal/vertical/aligned/radius/diameter dimension entities, rendering, tools, preview and SVG/DXF graphical export exist;
- `PointTool`;
- `TextTool` for single-line text;
- `LineTool`;
- `RectangleTool`;
- `RectangleBySidesTool`;
- `CircleTool`;
- `ArcTool`;
- `ArcThreePointsTool`;
- `PolylineTool` v1;
- command line coordinate input;
- relative coordinate input;
- direct distance entry;
- snap support;
- Ortho and Polar Tracking support;
- preview geometry.

### Editing and transforms

- select by point/window/crossing;
- entity snap during selection;
- Ctrl-click cycling for overlapping entities;
- move, including select-first workflow when no entities were preselected;
- copy;
- delete;
- rotate;
- scale;
- align with optional scaling confirmation;
- line-based Break Point and Break Segment;
- Trim and Extend for lines, arcs, circles and polylines where supported;
- grip editing for supported entities;
- undo and redo.

### Layers

- current layer selection;
- hidden layer behavior;
- locked layer behavior;
- Layer Manager with line format selection;
- Line Format Manager;
- Text Format Manager;
- current layer must remain visible and unlocked;
- layer `0` protected;
- reusable line formats control layer stroke color, weight and style;
- reusable text formats control single-line text font, height, color, bold and italic;
- selected entities can be assigned to the current layer with the `Assegna` top-bar button.

### UI

- stable top file command bar;
- CAD top bar;
- vertical left tool panel;
- canvas with crosshair;
- optional right Property Panel v1;
- bottom snap/Ortho bar plus top-bar Polar Tracking selector;
- fixed command line input;
- status bar;
- grid configuration with rectangular/isometric layouts;
- line format management from the top CAD bar;
- text format management from the top CAD bar;
- viewport culling;
- rendered entity count.

### Persistence and export

- internal JSON format `.opencad2d.json`;
- document-level `DimensionStyleCollection` for dimension settings;
- first dimension entities persisted in JSON as `LinearDimension` and `AlignedDimension`;
- `OpenCad2D.Persistence` project;
- New/Open/Save/Save As;
- current file path;
- dirty state with `*` marker;
- “Save changes?” dialog before New/Open/Close;
- viewport state save/restore;
- `OpenCad2D.Export` project;
- SVG export from the file command bar;
- DXF export from the file command bar;
- SVG background rectangle matching the canvas;
- SVG export preserves the same visual Y orientation as the canvas;
- DXF export writes AutoCAD 2000 ASCII DXF with POINT, TEXT, LINE, CIRCLE, ARC and LWPOLYLINE; v0.4 dimensions currently export as LINE + TEXT graphical primitives, not native DIMENSION records;
- DXF export writes LTYPE/LAYER tables and uses LineFormat-derived layer appearance with BYLAYER entities;
- SVG/DXF export include points, single-line text and horizontal/vertical/aligned dimensions;
- DXF export mirrors Y by exported content bounds to preserve the visual top/bottom orientation in external viewers;
- automated DXF tests cover balanced code/value pairs, representative entity records, BYLAYER entity properties, built-in line format mapping, TEXT records and LWPOLYLINE flags;
- SVG export does not save the drawing and does not clear dirty state.

---

## Stable UI layout rule

The file commands must stay in their own highest row:

```text
New / Open / Save / Save As / Current file name / Dirty marker
```

Do not merge file commands into the CAD toolbars. Earlier iterations accidentally lost persistence controls when toolbars changed. The file command bar is now a protected UI region.

---

## Dependency rules

Allowed high-level dependencies:

```text
OpenCad2D.App
  -> OpenCad2D.Persistence
      -> OpenCad2D.Core
          -> OpenCad2D.Geometry

OpenCad2D.App
  -> OpenCad2D.Export
      -> OpenCad2D.Core
          -> OpenCad2D.Geometry

OpenCad2D.App
  -> OpenCad2D.Tools
      -> OpenCad2D.Interaction
          -> OpenCad2D.Core
              -> OpenCad2D.Geometry
```

Forbidden dependencies:

- `Geometry` must not depend on anything else in the solution.
- `Core` must not depend on `Tools`, `Interaction`, `Persistence` or `App`.
- `Interaction` must not depend on `Tools` or `App`.
- `Tools` must not depend on `App` or Avalonia.
- `Persistence` must not depend on `Tools`, `Interaction` or `App`.
- `Export` must not depend on `Tools`, `Interaction`, `Persistence` or `App`.

---

## Document mutation rule

All document changes must go through commands and through `CadDocument` mutation APIs.

Correct:

```csharp
document.AddEntity(entity);
document.ReplaceEntities(replacements);
document.RemoveEntities(ids);
```

Incorrect:

```csharp
document.Entities.Replace(entity);
document.Entities.RemoveMany(ids);
```

This matters because `CadDocument` enforces layer validation, locked-layer validation and spatial index consistency.

---

## Hidden and locked layers

Hidden layer entities:

```text
not rendered
not selectable
not snappable
```

Locked layer entities:

```text
rendered if visible
not selectable
snappable
not editable/removable/transformable
```

Locked-layer protection is enforced at `CadDocument.ReplaceEntity`, `ReplaceEntities`, `RemoveEntity` and `RemoveEntities`.

---

## Current layer rule

The current layer must always be:

```text
visible
unlocked
```

Layer Manager and quick layer controls must preserve this rule.

---

## Command line and point input

Typed input is resolved to a point and then forwarded to the active tool. The command line must not create entities directly.

Supported point input:

```text
100,50   absolute UCS point
@50,0    relative UCS offset from current base point
5        direct distance from current base point along cursor direction
```

Explicit coordinates are not modified by Ortho or Polar Tracking. Direct distance uses the effective constrained direction when Polar Tracking or Ortho is enabled.

---

## ToolContext runtime state

`ToolContext` stores shared runtime state needed by tools:

- current layer;
- snap settings;
- grid settings;
- active tool snap-mode override through `ISnapModeProvider`;
- current UCS;
- current base point;
- Ortho mode;
- Polar Tracking through `AngleConstraintSettings`, `AngleConstraintService` and `ToolInputConstraintService`;
- selection set;
- command history.

The UI should not inspect private fields of tools. Shared information should be exposed through `ToolContext`, `CadWorkspace` or tool public properties.

---


## Polar Tracking status

Polar Tracking is implemented as a runtime input constraint in `OpenCad2D.Tools.Common`.

Main types:

```text
AngleConstraintSettings
AngleConstraintService
ToolInputConstraintService
PolarTrackingOptionViewModel
```

The App exposes a top-bar `Polar:` ComboBox with these values:

```text
Off
90°
45°
30°
15°
```

`AngleConstraintService` preserves the distance from the current base point and rounds the direction to the nearest multiple of the configured step. The current pipeline is:

```text
raw cursor point -> snapping -> Polar/Ortho angle constraint -> preview/command
```

Polar Tracking has priority over legacy Ortho when enabled. If Polar is `Off`, legacy Ortho can still constrain to horizontal/vertical directions.

Implemented integration points:

```text
TwoPointToolBase
MoveTool
PolylineTool
command-line direct distance direction
status text through MainWindowViewModel.PolarTrackingText
```

## Measure tools status

Baseline measure tools are implemented in `OpenCad2D.Tools.Measurements` and use pure measurement logic from `OpenCad2D.Core.Measurements`.

Implemented tools:

```text
MeasureDistanceTool  two points -> distance, DX, DY, angle
MeasureEntityTool    click entity -> entity-specific measurements
MeasureAngleTool     three points -> angle and supplementary angle
MeasureAreaTool      closed polyline -> area/perimeter/vertices
```

Important behavior:

- measure tools do not create geometry;
- measure tools do not execute undoable commands;
- measure tools do not mark the document dirty;
- point-based measure tools use snap plus Polar Tracking / Ortho through the normal point input pipeline;
- entity-based measure tools use `SnapKind.EntityOnly` and support `Ctrl+click` cycling through overlapping entities;
- formatted output intentionally has no physical unit suffix because model space has no fixed unit.

Future measure follow-ups: point coordinates, area by picked points, copy result to clipboard and configurable precision.

## Entity snap and overlapping selection status

Selection-oriented tools use `SnapKind.Entity` instead of geometric snaps. `SnapKind.Entity` is intentionally excluded from `SnapKind.All`; use `SnapKind.EntityOnly` when a tool phase is selecting objects.

Implemented behavior:

```text
SelectionTool -> entity snap only
MoveTool with no initial selection -> entity-selection phase, then base/destination phase
Ctrl+click -> cycle through overlapping selectable entities
Shift+click -> toggle selection
Ctrl+Shift+click -> cycle and toggle
```

The entity snap marker is a simple rectangle.

## Transform tools status

Implemented:

- `RotateTool` — base/reference/destination, preview, Ortho to 90-degree steps.
- `ScaleTool` — base/reference/destination, preview.
- `AlignTool` — source1/destination1/source2/destination2, preview, optional scaling confirmation.

Align confirmation:

```text
Enter or N -> apply without scale
Y          -> apply with uniform scale
```

Keyboard input is case-insensitive for confirmation keys at the tool level.

---

## Arc and rectangle variants status

Current additional drawing tools:

```text
RectangleBySidesTool  first corner -> first side endpoint -> second side point
ArcTool               center -> start point -> end direction
ArcThreePointsTool    start point -> point on arc -> end point
```

`RectangleBySidesTool` projects the third point perpendicular to the first side. `ArcThreePointsTool` uses `ArcCreationService` and rejects duplicate or collinear points.

---

## PolylineTool status

`PolylineTool` v1 is implemented.

Behavior:

```text
click or typed point -> add vertex
Enter                -> finish open polyline
C                    -> close polyline
Esc                  -> cancel
```

The tool supports command line input, snap, Ortho, Polar Tracking and direct distance entry.

Polyline and rectangle grip editing is implemented. Generic polylines support vertex movement, midpoint insertion grips, vertex deletion through `Delete`, and undoable updates. Rectangle-like closed four-vertex polylines keep their rectangle-specific corner/edge/center resize behavior.

---

## Property Panel status

Property Panel v1 is implemented and read-only.

It displays:

- no-selection document state;
- single line properties;
- single circle properties;
- single polyline properties;
- multiple-selection summary.

Do not add editing fields to the property panel until modifications can be routed through undoable commands.

---

## Layer Manager status

Layer Manager is implemented as a separate window.

It supports:

- New layer;
- Delete layer when allowed;
- Rename;
- Visible;
- Locked;
- Line format selection;
- Current layer selection;
- OK/Cancel workflow;
- one undoable update command.

Layer `0` is protected. Current layer cannot be hidden, locked or deleted. Layer stroke appearance is chosen by assigning a `LineFormatId`, not by editing color/weight directly in the Layer Manager.

---

## Line Format Manager status

Line Format Manager is implemented as a separate window opened from `Line formats...`.

It supports:

- built-in formats that are editable but not deletable;
- user-defined format creation;
- deletion of user-defined formats when allowed;
- format name editing;
- color editing;
- line weight editing;
- line style selection;
- OK/Cancel workflow;
- undoable application through `UpdateLineFormatsCommand`.

Default formats:

```text
Continua           white       1     Continuous
Asse               red         0.5   DashDot
Tratteggiata       yellow      1     Dashed
Tratto due punti   light blue  0.5   DashDotDot
Tratto e punto     green       0.75  DashDot
```

---

## Text Format Manager status

Text Format Manager is implemented as a separate window opened from `Text formats...`.

Implemented text model:

- `TextFormatId`;
- `TextFormat`;
- `TextFormatCollection`;
- built-in formats: `Standard`, `Title`, `Annotation`, `Small`;
- `CadDocument.TextFormats`;
- `TextEntity.TextFormatId`;
- `UpdateTextFormatsCommand`;
- JSON persistence for text formats;
- SVG and DXF text export;
- tests for format validation, manager behavior and undo/redo.

Text entities are intentionally single-line for now. They store content, insertion point, rotation and format id. They do not store font, height, color, bold or italic directly.


## Grid and viewport culling status

Grid display is configurable separately from grid snapping. The top CAD bar exposes `Grid...`, which opens the grid settings dialog. Rectangular and isometric grids are supported.

Viewport culling is implemented at rendering time. Only normal entities whose bounding boxes intersect the visible world area are rendered.

Do not use viewport culling to modify selection state or document state. It is only a rendering optimization.

---

## Escape behavior status

`Esc` follows the current CAD policy:

```text
non-selection tool -> cancel command, return to Selection, keep selection
Selection + selected entities -> clear selection
Selection + empty selection -> no operation
```

This allows a common two-step flow: first `Esc` exits a command, second `Esc` clears the selection.

---

## Persistence status

Persistence is implemented in `OpenCad2D.Persistence`.

The serializer handles:

- versioned JSON;
- layers;
- entities;
- current layer id;
- line formats and layer line format references;
- viewport state;
- unknown entity type tolerance.

The App handles:

- file dialogs;
- New/Open/Save/Save As;
- dirty-state title/file marker;
- Save changes dialog.

---

## Modify tools status

Implemented modify tools:

```text
Break Point    LineEntity only
Break Segment  LineEntity only
Extend         boundary: Line/Circle/Arc/Polyline; target: Line/Arc/open Polyline
Trim           cutting edge: Line/Circle/Arc/Polyline; target: Line/Circle/Arc/Polyline
```

Design rule: modify tools use geometry services, produce preview when useful and commit changes through undoable commands that mutate the document through `CadDocument`.

Core services currently include entity intersection, trim and extend services plus line-specific break helpers. `ModifyEntitiesCommand` supports replacing one entity with zero, one or more entities.

Recommended follow-up: improve trim/extend previews, add clearer ignored-operation messages and broaden break operations beyond `LineEntity`.

---


## SVG export status

SVG export is implemented in `OpenCad2D.Export`.

Current behavior:

```text
LineEntity      -> <line>
CircleEntity    -> <circle>
Polyline open   -> <polyline>
Polyline closed -> <polygon>
ArcEntity       -> <path>
```

Export rules:

- hidden layers are ignored;
- locked but visible layers are exported;
- stroke color, stroke width and dash array come from the line format referenced by each entity layer;
- the SVG `viewBox` is computed from visible drawing bounds;
- a dark background rectangle is exported by default;
- Y orientation matches the OpenCad2D canvas;
- export does not change current file path, does not mark the document saved and does not clear dirty state.

## Development practice

Before adding or changing code:

1. start from the latest project zip/baseline;
2. keep each phase small;
3. add tests with every new service/tool;
4. run `dotnet build` and `dotnet test`;
5. update docs after each milestone;
6. avoid overwriting stable UI regions such as file commands.

## v0.4 dimensions phase 2 status

Implemented after the initial dimension core phase:

- `DimensionGeometryBuilder` creates renderer-agnostic primitives for `LinearDimensionEntity` and `AlignedDimensionEntity`.
- Horizontal and vertical dimensions use separate tools: `HorizontalDimensionTool` and `VerticalDimensionTool`.
- `AlignedDimensionTool` uses the same three-click placement flow.
- Tool flow is: first measured point, second measured point, dimension-line placement point.
- Canvas rendering currently draws dimension lines, extension lines, arrow wings and automatic text through the dimension style's `TextFormatId`.
- Dimension selection uses the same selected pen color as other entities.
- Dimension tools create entities through `AddEntityCommand`, so undo/redo is available.
- DXF/SVG native export for dimensions is still pending; the agreed v0.4 strategy is to export dimensions as graphical primitives rather than native DXF `DIMENSION` entities.


## v0.4 dimensions phase 4 status

Implemented radius and diameter dimensions as non-associative entities. Both store center, point on circle and text point. `RadiusDimensionTool` and `DiameterDimensionTool` use a three-click flow: center, point on circle, text placement. Rendering/export reuse `DimensionGeometryBuilder`; SVG/DXF export remains graphical (`LINE` + `TEXT`) rather than native DXF `DIMENSION`. Next planned dimension phase: angular dimension, including support for angles greater than 180°.

## v0.4 angular dimensions

Angular dimensions are implemented as non-associative entities through `AngularDimensionEntity`. The entity stores `Center`, `FirstRayPoint`, `SecondRayPoint`, `ArcPoint` and `IsCounterClockwise`. The chosen sweep can be minor or reflex; `AngularDimensionTool` derives the sweep direction from the fourth click using `AngularDimensionEntity.ShouldUseCounterClockwiseSweep`.

Rendering/export use `DimensionGeometryBuilder`, which now emits `DimensionArcPrimitive` in addition to line, arrow and text primitives. SVG/DXF export writes angular dimensions as graphical primitives, not native DXF `DIMENSION` records.


## Dimension edge cases

The v0.4 dimension system includes systematic tests for degenerate cases and transform robustness. Linear dimensions reject zero measured distance on their orientation axis. Aligned, radius, diameter and angular dimensions reject invalid zero-length definitions. Horizontal/vertical dimensions transformed by arbitrary rotation become aligned dimensions when they are no longer axis-aligned. Angular dimensions flip `IsCounterClockwise` when transformed by a matrix with negative determinant, such as mirror, so the visual/measured sweep remains stable.


## Recent v0.4 editing polish update

- Trim and Extend now expose highlighted preview entities in addition to the normal result preview.
- For line targets, Trim highlights the segment that will be removed.
- For line targets, Extend highlights the segment that will be added.
- `CadCanvas` draws these highlighted modify previews with a separate red pen.
- Current follow-up: extend highlighted previews to arcs, circles and polylines if needed.
