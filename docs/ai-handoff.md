## Preview UX: Trim removed interval is dashed

`TrimTool` now previews the exact native interval that will be removed, not only the kept replacement geometry. The removed interval is obtained through `CadTrimService.GetRemovedIntervalByBoundaries`, which delegates to `CadCurveSplitService.GetPickedInterval` and therefore uses the same cut collection, native curve adapters and interval selection rules as the final Trim operation.

`ToolPreviewDescriptor` now carries `HighlightedEntityKind`. `TrimTool` marks highlighted entities as `ToolPreviewHighlightKind.Removal`, while existing modify previews such as Extend keep the default emphasis style. `CadToolPreviewRenderer` renders removal highlights with a red dashed pen, making the part to be cut visually distinct from added/modified preview geometry.

Validation note: this environment still does not provide the `dotnet` CLI. Run locally:

```bash
dotnet build
dotnet test
```

---

## Preview UX: Trim uses entity-only snaps

`TrimTool` now implements `ISnapModeProvider` and returns `SnapKind.EntityOnly` for all Trim phases. Trim is an entity/side selection workflow, so geometric snaps such as endpoint, midpoint, center, quadrant, intersection, nearest, perpendicular, tangent and grid are disabled while the command is active. This keeps the UI from showing vertex/point snap markers during Trim and makes the active selection intent clearer.

`TrimTool` also recognizes `EllipticalArcEntity` as a supported cutting edge/target in the tool-level support check, matching the native curve-editing services.

Validation note: this environment still does not provide the `dotnet` CLI. Run locally:

```bash
dotnet build
dotnet test
```

---

## Curve editing: EXTEND with native elliptical boundaries

`CadEntityIntersectionService.IntersectInfiniteLineWithEntity` now supports `EllipseEntity` and `EllipticalArcEntity` directly. It computes infinite-line/ellipse intersections analytically and filters points against the elliptical-arc sweep when needed. `IntersectCircleWithEntity` also routes `EllipseEntity` and `EllipticalArcEntity` through the native circle/ellipse intersection helpers.

This improves `CadExtendService` for these supported scenarios:

- `LineEntity` extended to an `EllipseEntity` boundary;
- open `PolylineEntity` endpoint extended to an `EllipticalArcEntity` boundary;
- `ArcEntity` extended to an `EllipseEntity` boundary.

The added tests assert that the resulting endpoints remain on the native ellipse/elliptical arc and do not depend on sampled fallback geometry.

Validation note: this environment still does not provide the `dotnet` CLI. Run locally:

```bash
dotnet build
dotnet test
```

---

## Curve editing: native Bezier spline Trim/Break

Open `BezierSplineEntity` is now connected to `CadCurveSplitService` at command level. `CadTrimService` and `CadBreakService` no longer convert supported open spline Trim/Break results to `PolylineEntity`; they return native `BezierSplineEntity` fragments through `BezierSplineCurveAdapter` and `BezierSplineSplitService`. Closed spline editing remains intentionally deferred/no-op. Intersection discovery can still use the existing approximation path, but the cut is projected back to the Bezier parameter before fragment creation.

# Latest handoff note

## Curve editing: BezierSplineSplitService foundation

`BezierSplineSplitService` has been introduced as the first native spline-preservation step. It uses De Casteljau subdivision on open `BezierSplineEntity` control polygons so spline editing can eventually return native `BezierSplineEntity` fragments instead of permanent `PolylineEntity` approximations.

Current scope:

- `SplitAt(spline, t)` returns two native open Bezier spline fragments sharing the exact De Casteljau break point;
- `ExtractInterval(spline, t0, t1)` returns the native Bezier interval between two parameters;
- `RemoveInterval(spline, t0, t1)` returns the two native outer fragments around a removed interval;
- closed spline splitting is intentionally deferred and currently returns no fragments.

Added tests in `BezierSplineSplitServiceTests` verify native output, shared split points, endpoint correctness for extracted intervals, outer fragment creation, metadata preservation, endpoint no-op behavior and closed-spline deferral.

This phase does not yet connect splines to `ICurveAdapter`, TRIM or BREAK. The next planned phase is `BezierSplineCurveAdapter`, followed by native spline Trim/Break.

Validation note: this handoff environment still does not provide the `dotnet` CLI. Run locally:

```bash
dotnet build
dotnet test
```

---

# Latest handoff note

## Curve editing: native ellipse/polyline intersection consolidation

`CadEntityIntersectionService` now handles `PolylineEntity` against `EllipseEntity` and `EllipticalArcEntity` with analytic line-segment/ellipse intersections per polyline segment. This complements the existing native `LineEntity` support and avoids relying on sampled ellipse segments for Trim boundaries made from polylines.

Added precision tests covering direct polyline/ellipse intersections, polyline/elliptical-arc sweep filtering, and Trim results that remain native `EllipticalArcEntity` fragments with geometric endpoints on the source ellipse.

Remaining curve-editing priorities:
1. BezierSplineCurveAdapter.
2. Native spline Trim/Break.
3. Richer `CadIntersectionPoint` records.
4. EXTEND on the same native curve model.
5. Cleanup of remaining permanent polyline fallbacks.
6. Preview UX.

---

# Latest handoff note

## Curve editing: native ellipse/elliptical arc Trim and Break foundation

`CadCurveSplitService` now has adapters for `EllipseEntity` and `EllipticalArcEntity`. Trim on full ellipses and existing elliptical arcs can now return native `EllipticalArcEntity` fragments instead of permanent `PolylineEntity` approximations. Break Between Points on full ellipses and Break on existing elliptical arcs also route through the shared split pipeline.

One-point Break on a full closed ellipse remains a deliberate no-op, matching the current full-circle behavior, until a safe full-sweep/open-closed conic arc convention is introduced. Intersections may still be discovered through sampled segments, but the edited geometry is rebuilt from native ellipse parameters.

Added focused tests to verify that Trim/Break on ellipse workflows do not create `PolylineEntity` results.

---

﻿# Latest handoff note

## v0.8.5 stabilization: delete marks dimensions stale

Deleting model geometry now marks remaining non-associative dimensions as stale, matching the existing behavior used by modify, replace and transform commands. `DeleteEntitiesCommand` captures the previous dimension stale state before deletion, marks dimensions stale only when model geometry is removed, and restores the captured state on undo. Deleting dimension annotations alone does not mark other dimensions stale.

Added focused core tests for delete-driven stale marking, undo restoration and the dimension-only deletion case.

---

# Latest handoff note

## v0.8.5 DXF SPLINE import

Implemented first-pass DXF `SPLINE` import. Readable control-point splines are imported as editable `BezierSplineEntity` instances, preserving the OpenCad2D Bezier workflow and enabling round-trips for OpenCad2D-exported SPLINE entities. Closed spline flags are respected. Fit-point-only SPLINE entities are imported as `PolylineEntity` approximations with an informational diagnostic, because OpenCad2D does not yet evaluate external NURBS knot vectors or rational weights. Added focused importer tests for open control-point splines, closed splines, fit-point-only fallback and malformed point data.

Remaining DXF spline limitation: full external NURBS fidelity is still future work.

---

# Latest handoff note

## v0.8.x final documentation and release consolidation

The v0.8.x baseline is now ready for final local validation and GitHub release preparation. Polygon, Ellipse, MTEXT and Bezier Spline are complete in the current baseline, including command aliases, rendering/preview, persistence, export coverage and focused tests.

Important implementation notes for future work:

- regular polygons are stored as closed `PolylineEntity` instances;
- ellipse partial edit results currently become open `PolylineEntity` approximations because there is no `EllipseArcEntity`;
- Bezier spline Trim/Break/Offset workflows use sampled polyline approximation, so edited fragments currently become `PolylineEntity` results;
- DXF import supports `MTEXT`, full `ELLIPSE` entities and first-pass `SPLINE` control-point import;
- release notes are consolidated in `docs/release-v0.8.md`, with a GitHub-ready draft in `docs/release-v0.8-final.md`.

Recommended final validation before publishing:

```bash
dotnet build OpenCad2D.sln
dotnet test OpenCad2D.sln --no-build
```

---

# Latest handoff note

## Mirror tool

Implemented `MirrorTool` as a command-driven modify tool before the v0.9 roadmap. The workflow is:

```text
MIRROR: Select objects to mirror:
MIRROR: Specify first point of mirror line:
MIRROR: Specify second point of mirror line:
MIRROR: Delete source objects? [Yes/No] <No>:
```

The tool supports preselection or select-first workflow, typed coordinates for the two mirror-axis points, a live mirrored preview while choosing the second axis point, and the final `Yes`/`No` option. Empty Enter defaults to `No`, so the source entities are kept and mirrored copies are added. `Yes` mirrors the selected source entities in place through `MirrorEntitiesCommand`. The UI has a `Mirror` button in the Modify/Edit group and command aliases are `MIRROR` and `MI`.

Roadmap status: dimension export, Mirror, Polygon, Ellipse, multiline text, Spline and final documentation/release cleanup are complete for the v0.8.x baseline.

---

# Latest handoff note

## Dimension export stabilization

PDF export now supports all current dimension entities as graphical primitives plus text:

- horizontal and vertical linear dimensions;
- aligned dimensions;
- radius and diameter dimensions;
- angular dimensions with segmented arc output.

SVG and DXF dimension coverage already existed and remains based on graphical primitives. PDF export now mirrors that approach and includes tests for each dimension type. PDF text escaping now writes WinAnsi octal escapes for non-ASCII dimension symbols such as degree (`°`) and diameter (`Ø`), and the PDF font resource declares `/Encoding /WinAnsiEncoding`.

Roadmap status before v0.9: Mirror, Polygon, Ellipse, multiline text, Spline and final v0.8.x documentation/release cleanup are complete.

---

# Latest handoff note

## Startup template stabilization

The app now starts from a clean native template instead of seeding a sample drawing in `MainWindowViewModel`.

Implemented changes:

- `MainWindow.axaml` opens maximized by default through `WindowState="Maximized"`.
- `MainWindowViewModel` calls `LoadDefaultTemplate()` during construction and in `NewDocument()`.
- The template is `src/OpenCad2D.App/Templates/default.opencad2d.json`.
- `OpenCad2D.App.csproj` copies `Templates/**` to the output directory.
- The default template contains line formats, text formats, one dimension style and the default CAD layers, with no entities.
- If the template cannot be loaded, the view-model falls back to an internal empty document with the built-in layers.
- `MainWindowViewModelDefaultDrawingTests` now verifies that startup is empty instead of expecting the old sample drawing.

Validation note: the current environment used for this handoff does not provide the `dotnet` command, so build/tests must be run locally in Visual Studio or with `dotnet test OpenCad2D.sln`.

---

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

- v0.4 basic dimensions are implemented: horizontal, vertical, aligned, radius, diameter and angular dimensions with non-associative entities, tools, preview, rendering, persistence and SVG/DXF graphical export;
- `PointTool`;
- `TextTool` for single-line text;
- `MultilineTextTool` / `MTEXT` for multiline annotation text;
- `LineTool`;
- `RectangleTool`;
- `RectangleBySidesTool`;
- `CircleTool`;
- `EllipseTool`;
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
- Trim and Extend for lines, arcs, circles and polylines where supported; Trim supports optional two-cutting-edge line trimming through Ctrl-click on the second cutting edge;
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
- selected entities can be assigned to the current layer with the `Assign` top-bar button.

### UI

- stable top file command bar;
- CAD top bar;
- two-column left tool panel;
- canvas with crosshair;
- optional right editable Property Panel v2;
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
- dimensions persisted in JSON as `LinearDimension`, `AlignedDimension`, `RadiusDimension`, `DiameterDimension` and `AngularDimension`;
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
- DXF export writes AutoCAD 2000 ASCII DXF with POINT, TEXT, LINE, CIRCLE, ARC and LWPOLYLINE; v0.4 dimensions export as LINE/ARC/TEXT graphical primitives, not native DIMENSION records;
- DXF export writes LTYPE/LAYER tables and uses LineFormat-derived layer appearance with BYLAYER entities;
- SVG/DXF export include points, single-line text and all v0.4 basic dimensions;
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

Property Panel v2 is implemented and editable for supported single-selection properties.

It displays:

- no-selection document state;
- single line properties;
- single circle properties;
- single polyline properties;
- multiple-selection summary.

Property edits must continue to be routed through undoable commands, usually entity replacement via command history. Do not mutate entities directly from the UI.

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

Default layers used by the application template:

```text
0                    Continuous   white   1.0
Annotations          Continuous   gray    0.8
Walls                Continuous   white   2.0
Axis                 DashDot      red     0.75
Construction lines   Dashed       yellow  0.75
```

Default line formats:

```text
Continua           white       1     Continuous
Annotations        gray        0.8   Continuous
Walls              white       2     Continuous
Asse               red         0.75  DashDot
Tratteggiata       yellow      0.75  Dashed
Tratto due punti   light blue  0.5   DashDotDot
Tratto e punto     green       0.75  DashDot
```

The dash-dot and dash-dot-dot patterns were lengthened before closing v0.7 so they remain readable at normal CAD zoom levels.

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

Text annotations now include `TextEntity` for single-line text and `MultilineTextEntity` for MTEXT-style multiline notes. Both store content, insertion point, rotation and format id. They do not store font, height, color, bold or italic directly.


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
Break Point    LineEntity, ArcEntity and PolylineEntity; CircleEntity returns a clear not-applicable message
Break Segment  LineEntity, ArcEntity, CircleEntity and PolylineEntity
Extend         boundary: Line/Circle/Arc/Polyline; target: Line/Arc/open Polyline
Trim           cutting edge: Line/Circle/Arc/Polyline; target: Line/Circle/Arc/Polyline
```

Design rule: modify tools use geometry services, produce preview when useful and commit changes through undoable commands that mutate the document through `CadDocument`.

Core services currently include entity intersection, trim and extend services plus generic break helpers through `CadBreakService`. `ModifyEntitiesCommand` supports replacing one entity with zero, one or more entities.

Recommended follow-up: implement Trim with two cutting edges, improve broader trim/extend previews and add stronger layer-rule tests.

---


## SVG export status

SVG export is implemented in `OpenCad2D.Export`.

Current behavior:

```text
LineEntity      -> <line>
CircleEntity    -> <circle>
EllipseEntity   -> sampled <polygon> / DXF ELLIPSE / PDF polyline approximation
Polyline open   -> <polyline>
Polyline closed -> <polygon>
ArcEntity       -> <path>
PointEntity     -> marker
TextEntity      -> <text>
MultilineTextEntity -> <text> with <tspan> lines
Dimensions      -> graphical primitives
```

Export rules:

- hidden layers are ignored by default;
- locked but visible layers are exported;
- stroke color, stroke width and dash array come from the line format referenced by each entity layer;
- the SVG `viewBox` is computed from visible drawing bounds;
- background mode supports canvas dark, white and transparent;
- optional layer grouping writes `<g id="layer-..." data-layer-name="...">`;
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
- For line targets, Extend highlights the segment that will be added. For arc targets, Extend highlights the added arc portion. For open-polyline targets, Extend highlights the added endpoint segment.
- `CadCanvas` draws these highlighted modify previews with a separate red pen.
- Current follow-up: extend highlighted previews to arcs, circles and polylines if needed.


## v0.4 final status

The planned v0.4 Basic Dimensions scope is complete. Implemented dimension types:

- horizontal;
- vertical;
- aligned;
- radius;
- diameter;
- angular, including reflex angles greater than 180°.

Important decisions:

- dimensions are non-associative;
- `DimensionStyle` is document-level configuration;
- dimension text appearance is resolved through `DimensionStyle.TextFormatId`;
- rendering, preview and export share `DimensionGeometryBuilder`;
- SVG/DXF export dimensions as graphical primitives, not native editable CAD dimensions.

Recent UI polish:

- left tool panel is organized into two columns;
- first column contains Select, Draw, Dimension and Measure groups;
- second column contains Edit tools;
- status bar color is aligned with the rest of the dark UI;
- horizontal, vertical and aligned dimension text placement was adjusted.

Recommended next milestone: continue v0.5 Advanced editing and refinement. Break Point, Break Segment, Trim with two cutting edges, and Extend consolidation are complete for the current v0.5 scope; remaining gaps are stronger locked/hidden layer tests, near-tangent/overlap regressions and final v0.5 documentation.

## v0.5 modify tools audit

The v0.5 milestone has started with a planning/audit-only phase. No production code was changed in this phase.

New planning document:

```text
docs/v0.5-modify-tools-audit.md
```

Current modify-tool baseline after v0.5 phase 2:

```text
Break Point    LineEntity, ArcEntity and PolylineEntity; CircleEntity returns a clear not-applicable message
Break Segment  LineEntity, ArcEntity, CircleEntity and PolylineEntity
Trim           Line/Circle/Arc/Polyline targets with one cutting edge
Extend         Line/Arc/open Polyline targets to Line/Circle/Arc/Polyline boundaries
```

Key v0.5 decisions:

- Break Point on CircleEntity should be a clear not-applicable operation, not an artificial tiny-gap arc.
- Break Segment on CircleEntity should remove the minor arc between the two picked points.
- Trim with two cutting edges should use the simple three-click workflow: cutting edge 1, cutting edge 2, target portion to remove.
- Hidden entities should not participate as targets or references.
- Locked visible entities should be usable as references/boundaries/cutting edges, but not editable as targets.
- Current hit testing uses selectable entities, so implementing locked references may require a visible-reference hit-test path separate from target selection.

Recommended next implementation phase:

```text
v0.5 Phase 3 - Trim with two cutting edges
[ ] two cutting-edge selection workflow
[ ] target portion selection
[ ] line target split into zero, one or two remaining pieces
[ ] preview for remaining geometry and removed portion
[ ] tests for center/outside trim portions
[ ] undo/redo tests
```


## v0.5 phase 1 status - Break Point advanced

Completed implementation details:

- Added `CadBreakService` for entity-level break-at-point operations.
- `BreakAtPointTool` now supports `LineEntity`, `ArcEntity` and `PolylineEntity`.
- `CircleEntity` is intentionally not applicable for Break Point and returns a clear message recommending Break Segment.
- Arc break creates two arcs and preserves the original clockwise/counter-clockwise direction.
- Open polyline break creates two open polylines.
- Closed polyline break opens the polyline at the selected point.
- Added core and tool tests, including undo/redo coverage for polyline break-at-point.

This phase is complete. Phase 2 expanded Break Segment to arcs, circles and polylines.


## v0.5 phase 2 status - Break Segment advanced

Completed in v0.5 phase 2:

```text
Break Segment supports LineEntity, ArcEntity, CircleEntity and PolylineEntity.
CircleEntity removes the minor arc between the two picked points and keeps the remaining major ArcEntity.
Open PolylineEntity removes the path interval and returns zero, one or two open polylines.
Closed PolylineEntity removes the shortest path and returns one open polyline.
BreakBetweenPointsTool now stores a generic CadEntity target and uses CadBreakService.BreakBetweenPoints.
Preview uses the same generic break service and works for the newly supported target types.
Core and tool tests cover arcs, circles, open polylines, closed polylines and existing line regression.
```

Next v0.5 phase recommended: Trim with two cutting edges.

## v0.5 modify-tool layer rules

The v0.5 modify tools use this rule set:

```text
hidden entities:
    ignored as targets and references

locked visible entities:
    valid as Trim cutting edges / Extend boundaries
    not editable as Break/Trim/Extend targets
```

`TrimTool` and `ExtendTool` therefore use visible-entity picking for boundary/cutting-edge selection, while target selection remains based on selectable/editable entities. Regression coverage lives in `ModifyToolLayerRuleTests`.

## v0.5 final status

The v0.5 Advanced editing and refinement milestone is complete for the agreed scope.

Completed phases:

```text
Phase 0 - Modify tools audit                     done
Phase 1 - Break Point advanced                   done
Phase 2 - Break Segment advanced                 done
Phase 3 - Trim with two cutting edges            done
Phase 4 - Extend consolidation                   done
Phase 5 - Layer rules and regression tests       done
Phase 6 - Final documentation and release notes  done
```

Final modify-tool scope:

```text
Break Point:
    LineEntity, ArcEntity and PolylineEntity.
    CircleEntity is intentionally not applicable.

Break Segment:
    LineEntity, ArcEntity, CircleEntity and PolylineEntity.
    CircleEntity removes the minor arc and keeps the remaining major arc.

Trim:
    one-cutting-edge workflow remains available;
    two-cutting-edge workflow is available for LineEntity targets through Ctrl-click on the second cutting edge.

Extend:
    LineEntity, ArcEntity and open PolylineEntity are supported targets.
    CircleEntity, closed PolylineEntity, PointEntity, TextEntity and dimension entities are not extendable targets.
```

Layer rules are now explicit and tested:

```text
Hidden entities:
    ignored as targets and references.

Locked visible entities:
    valid as references, boundaries and cutting edges;
    invalid as editable targets.
```

Release notes:

```text
docs/release-v0.5.md
```

Next milestone:

```text
v0.6 - Real command line and Property Panel v2
```

Recommended starting point for v0.6: design the command input architecture before writing code, because it affects tool activation, aliases, coordinate parsing, command history, contextual prompts and right-click repeat-last-command behavior.

---

## v0.6 planning state

The next milestone is `v0.6 - Real command line and Property Panel v2`.

Phase 0 planning is complete and documented in:

```text
docs/v0.6-command-line-property-panel-plan.md
```

Key decisions:

- command-line input and mouse input should feed the same tool implementations;
- command activation should be handled through a dedicated alias registry/service;
- command parsing should remain independent from Avalonia controls;
- Property Panel v2 edits must be undoable commands;
- coordinate syntax is:

```text
100,50      absolute point
@100,50     relative point
100         direct distance
100<45      distance plus CAD-model angle
```

- decimal separator is always `.`;
- comma separates X/Y;
- avoid ambiguous aliases such as `R` and `D` at first;
- planned first implementation phase is command activation by alias, not coordinate redesign.

Recommended next implementation phase:

```text
v0.6 Phase 1 - Command activation by alias
```

Phase 1 should add the alias registry/service, connect command-line submission to tool activation, preserve existing coordinate input behavior and add tests for aliases, unknown commands, empty input and command history.


---

## Current state after v0.6 phase 5

Command-line activation by alias has been added. `CommandAliasRegistry` resolves tool names and aliases before coordinate parsing. The UI command input supports activating tools with aliases such as `L`, `C`, `TR`, `EX`, `HDIM`, `ANG`, etc. Unknown textual commands produce a clear message, and valid tool activations are stored in `MainWindowViewModel.CommandLineHistory`.

Phase 2 verified the absolute coordinate pipeline. Absolute coordinate inputs such as `100,50` are submitted to the active tool through the same workspace/tool pipeline used by mouse clicks. Coordinate inputs are intentionally not stored in command history.

Phase 3 verified relative coordinates and direct distance input. `@x,y` is resolved from `Workspace.Context.CurrentBasePoint`; direct distance uses the current mouse/snap direction and applies active Ortho/Polar constraints before computing the final point. Tests cover valid relative input, missing base point errors, direct distance line creation, missing direction errors, invalid relative input and command-history behavior.

Phase 4 added CAD-style distance-angle input such as `100<45`. The parser returns `CommandInputKind.DistanceAngle`; angles are normalized, use CAD orientation (`0°` right, `90°` up), support negative values and values over 360°, and are resolved from the active tool base point.

Phase 5 added repeat-last-command. `MainWindowViewModel` tracks the last valid tool activation separately from coordinate/history input. Empty command-line submission repeats that tool. Right-click on the canvas raises `RepeatLastCommandRequested`, handled by `MainWindow`, and calls `RepeatLastCommandFromCanvas()`. That path refuses to repeat when a point-based command already has `Workspace.Context.CurrentBasePoint`, so right-click does not interrupt an active multi-step command. Coordinate inputs, relative inputs, direct distance and distance-angle inputs are not repeatable commands.

Next recommended phase: v0.6 phase 6, Property Panel v2 base editing.


## v0.6 Phase 6 Property Panel v2 base completed

The Property Panel now supports a first editable set of entities. Editable rows show a text box and an `Apply` button. Successful edits are committed through `ReplaceEntitiesCommand`, so undo/redo works through the normal command history.

Supported in this phase:

- `PointEntity`: X/Y position;
- `LineEntity`: start/end X/Y coordinates;
- `CircleEntity`: center X/Y and radius;
- `TextEntity`: value, insertion X/Y and rotation.

Validation rules:

- numeric values use invariant parsing and the `.` decimal separator;
- `10,5` is rejected as a number because comma is reserved for coordinate input;
- circle radius must be greater than zero;
- text value cannot be empty;
- line start and end points cannot become equal;
- hidden or locked selected entities cannot be edited from the panel.

Deferred to the next v0.6 phase:

- text format selection from the panel;
- layer and format references;
- arc, polyline and dimension property editing.


---

## v0.6 completed state

The v0.6 milestone is complete.

Command line:

- `CommandAliasRegistry` resolves command names and aliases before coordinate parsing;
- command activation is case-insensitive;
- supported point input includes absolute `x,y`, relative `@x,y`, direct distance and distance-angle `distance<angle`;
- distance-angle uses CAD model orientation: `0°` right, `90°` up;
- typed coordinate input bypasses snap/ortho/polar side effects so numeric values stay exact;
- command history stores valid tool activations, not point/coordinate input;
- empty `Enter` repeats the last valid command;
- right-click on the canvas repeats the last valid command when the workspace is idle;
- `Esc` cancels the active tool when the command input is empty.

Property Panel v2:

- editable rows use text input plus Apply;
- edits are validated before application;
- successful edits are committed through undoable command history, normally through `ReplaceEntitiesCommand`;
- supported properties include Point position, Line start/end, Circle center/radius, Arc center/radius/start/end angles, Text value/insertion/rotation/text format, common Polyline state, layer assignment, DimensionStyle and dimension text override;
- detailed polyline vertex editing remains handled by grip editing.

Release notes are in:

```text
docs/release-v0.7.md
```

Current milestone status:

```text
v0.7 - Interoperability: DXF import, PDF export and SVG options is implemented.
```

Key v0.7 implementation areas:

1. DXF import for `LINE`, `CIRCLE`, `ARC`, `POINT`, `LWPOLYLINE` and `TEXT`;
2. DXF layer table import and diagnostics;
3. DXF import report UI;
4. DXF export/import round-trip validation;
5. PDF export core and settings UI;
6. SVG export options for background mode and grouping by layer.

Recommended next milestone:

```text
v0.8 - UI, colors and settings
```

Likely v0.8 starting points:

1. persist application settings;
2. remember last file and default export options;
3. improve color-picker workflows;
4. polish final visual theme;
5. design and implement draw order / Z-order independent from layers before v1.0.

## Branding and application icon

Logo source files are kept in `screenshot/`:

```text
screenshot/logo_opencad2d.svg
screenshot/logo_opencad2d_16.png
screenshot/logo_opencad2d_32.png
screenshot/logo_opencad2d_64.png
screenshot/logo_opencad2d_128.png
screenshot/logo_opencad2d_256.png
screenshot/logo_opencad2d_512.png
```

Runtime application assets are copied under `src/OpenCad2D.App/Assets/`. The app icon is `Assets/app-icon.ico`, generated from the logo PNG resolutions, and is configured in `OpenCad2D.App.csproj` through `ApplicationIcon`. Avalonia windows use `Icon="/Assets/app-icon.ico"`. The About window displays `Assets/logo_opencad2d_128.png`.

Documentation should use the SVG logo where appropriate. The root `README.md` uses `screenshot/logo_opencad2d.svg`; documentation under `docs/` should reference `../screenshot/logo_opencad2d.svg`. More details are in `docs/branding.md`.


## DXF import status

DXF import is implemented in `OpenCad2D.Export.Dxf.Import`.

Supported imported DXF entities:

```text
LINE
CIRCLE
ARC
POINT
LWPOLYLINE
TEXT
```

The importer reads `TABLES/LAYER`, maps basic ACI color, linetype, lineweight, hidden/frozen and locked state, and creates missing layers when an entity references an undeclared layer.

Unsupported records are skipped with diagnostics. Malformed input handled through `DxfDocumentImporter` returns an error `DxfImportResult` instead of crashing the UI flow.

UI integration:

```text
Import DXF -> dirty-document protection -> replace current document on successful import -> show report for warnings/errors
```

Detailed docs:

```text
docs/dxf-import.md
docs/v0.7-interoperability-plan.md
```

## PDF export status

PDF export is implemented in `OpenCad2D.Export.Pdf`.

Current behavior:

```text
single-page vector PDF
A4/A3/A2/A1/A0
portrait/landscape
margin in millimeters
fit-to-page
visible layers by default
optional hidden-layer inclusion
print-friendly color mode by default
```

The PDF settings UI is implemented in `PdfExportSettingsWindow` and `PdfExportSettingsWindowViewModel`.

Detailed docs:

```text
docs/pdf-export.md
```

### DXF import dirty-state fix

DXF import now loads the imported document as an unsaved native OpenCad2D drawing. `CadWorkspace.LoadDocument` supports an explicit `markAsSaved` flag; normal native/template loads remain clean, while DXF import uses `markAsSaved: false` so `IsDirty` is immediately true after a successful import.



## 2026-05-16 - DXF import dirty-state stabilization

After introducing startup template loading, DXF import must still create an unsaved native drawing.
`MainWindowViewModel.ImportDxfFromFile` now explicitly registers the imported document as changed after replacing the current document, so `IsDirty` remains true even though the native file path is cleared.

Expected behavior:

- opening/loading a native `.opencad2d.json` can mark the workspace as saved;
- loading the default startup template is clean;
- importing a DXF replaces the document, clears `CurrentFilePath`, shows `Untitled`, and marks the document dirty because it still needs to be saved as a native OpenCad2D document.

### 2026-05-16 - DXF import dirty-state stabilization

Follow-up fix after the startup template phase:

- DXF import now leaves the native OpenCad2D document explicitly dirty.
- `CadWorkspace` now tracks external unsaved changes separately from command-history generation.
- `MarkSaved()` clears the external dirty flag.
- `LoadDocument(..., markAsSaved: false)` marks the loaded document as unsaved without relying on generation arithmetic.
- The DXF import view-model test now asserts the new clean startup state before import, then verifies that import marks the document dirty.

This keeps startup/template/native file loading clean while ensuring imported DXF files still require a native save.

### 2026-05-16 - Modal polish and About contact update

The second stabilization pass updated the user-facing modal behavior:

- `AboutWindow` now shows `info@opencad2d.org` as the contact email.
- `AboutWindow` now shows `www.opencad2d.org` as the project website.
- Secondary modal windows are configured with `WindowStartupLocation="CenterOwner"` where applicable.
- The inline save-changes confirmation built in `MainWindow.axaml.cs` was replaced by a dedicated `SaveChangesWindow`.
- `SaveChangesChoice` is now a small app-level enum used by the save confirmation window and `MainWindow`.

Files involved:

```text
src/OpenCad2D.App/AboutWindow.axaml
src/OpenCad2D.App/DxfImportReportWindow.axaml
src/OpenCad2D.App/LayerManagerWindow.axaml
src/OpenCad2D.App/LineFormatManagerWindow.axaml
src/OpenCad2D.App/PdfExportSettingsWindow.axaml
src/OpenCad2D.App/SaveChangesChoice.cs
src/OpenCad2D.App/SaveChangesWindow.axaml
src/OpenCad2D.App/SaveChangesWindow.axaml.cs
src/OpenCad2D.App/SvgExportSettingsWindow.axaml
src/OpenCad2D.App/TextFormatManagerWindow.axaml
src/OpenCad2D.App/MainWindow.axaml.cs
```

Next recommended implementation step: fix arc endpoint grip behavior so moving start/end grips preserves the opposite endpoint and keeps the radius unchanged.


### 2026-05-16 - Arc endpoint grip behavior

Arc endpoint grip editing was corrected so endpoint grips only change the corresponding angle:

- Moving the start grip updates `StartAngle` only.
- Moving the end grip updates `EndAngle` only.
- `Center` remains unchanged.
- `Radius` remains unchanged.
- The opposite endpoint remains fixed.
- The midpoint radius grip still changes the radius.

The key implementation point is that `ArcGripProvider.MoveEndpoint()` must keep `arc.Radius` and use the destination only to compute the new polar angle around the existing center.


### 2026-05-16 - Arc 3-point grip behavior

Arc grip editing now follows a 3-point construction rule:

- Moving the start grip rebuilds the arc through the new start point, the current point-on-arc grip and the current end point.
- Moving the point-on-arc grip rebuilds the arc through the current start point, the new point-on-arc and the current end point.
- Moving the end grip rebuilds the arc through the current start point, the current point-on-arc grip and the new end point.
- Moving the center grip still translates the whole arc rigidly.

The implementation is in `ArcGripProvider` and uses `ArcCreationService.TryCreateFromThreePoints`. If the three resulting points are duplicate or collinear, the original arc is preserved.


## Select All / Select Last status

Implemented selection actions:

```text
SELECTALL / SA / ALL
SELECTLAST / SL / LAST
```

The left SELECT tool group includes buttons for Select, Select All and Select Last. Select All replaces the current selection and does not modify the document dirty state. Select Last restores the last effective selection that was explicitly cleared.

Layer rules:

- hidden-layer entities are skipped;
- locked-layer entities are skipped;
- Select Last restores the previous cleared selection, including multi-entity selections, and skips any remembered entity that is no longer selectable.

Regression coverage lives in `CadActionControllerTests` and `MainWindowViewModelCommandLineTests`.


### Nullable warning fix after Select All / Select Last

- `MainWindowViewModel.TryExecuteActionCommand` now uses a non-null `ToolResult` out parameter.
- The default unmatched-command result is initialized with `ToolResult.None()` so `SubmitCommandInput` can return it without CS8603 nullable warnings.
- No behavior change was intended.


### 2026-05-16 - Zoom Window navigation tool

Implemented `Zoom Window` as an interactive viewport navigation tool.

Command aliases:

```text
ZOOMWINDOW / ZW
```

The tool collects two opposite model-space corners, shows a blue preview rectangle and asks `CadCanvas` to fit the viewport to the selected rectangle. Degenerate windows smaller than a few screen pixels are ignored. The command changes only the viewport and does not dirty the document.

Implementation notes:

- `ToolId.ZoomWindow` is registered in `ToolRegistry` under the `Navigation` category.
- `ZoomWindowTool` lives in `OpenCad2D.Tools.Navigation`.
- The left tool panel has a `NAVIGATE` section with a `Zoom Window` button.
- `CadCanvas.ZoomToWindow` applies the viewport fit because viewport state belongs to the UI layer, not to `OpenCad2D.Tools`.

### 2026-05-16 - Navigate panel Zoom Extents and Select Last semantics

Updated the left tool panel `NAVIGATE` section so it now exposes both `Zoom Window` and the already existing `Zoom Extents` command. The top-bar `Extents` button remains available and both UI buttons share the same click handler.

Changed `Select Last` semantics. It no longer selects the newest entity in document insertion order. It now restores the most recent effective selection that was explicitly cleared, preserving multi-entity selections. `SelectionSet` stores the last non-empty cleared selection and `CadActionController.SelectLast()` filters that snapshot against the current document so deleted, hidden-layer or locked-layer entities are not restored. `CadWorkspace.LoadDocument()` resets both current and remembered selection state when replacing a document.


### 2026-05-16 - Select Last nullable warning cleanup

- Removed the nullable warning in `CadActionController.SelectLast()` by adding an explicit `entity is not null` guard after `Entities.TryGet(...)`.
- No behavior change: Select Last still restores the last deselected selection and filters out entities that are no longer selectable.


## Latest handoff update

Added the first document recovery layer for `.opencad2d.json` loading. The application now uses the tolerant recovery path when opening native files: valid entities are preserved, invalid entities are skipped, entities on missing layers are moved to `Layer 0`, and a missing current layer is reset to `Layer 0`. Malformed JSON and unsupported versions still fail explicitly.


### 2026-05-16 - v0.8 command input planning decision

The next major milestone is v0.8 and its primary focus is a CAD-style guided command input refactor.

Important user decisions:

- Every time a tool asks for a point, the user must be able to either click with the mouse or type coordinates in the command input.
- Mouse point input and text point input should feed the same tool state machine.
- `LINE` remains a single-segment command: first point, second point, then finish command.
- Relative cartesian coordinate input is required in v0.8, for example `@100,0`.
- Relative polar coordinate input is required in v0.8, for example `@100<45`.
- User-facing polar angles are in degrees: 0 right, 90 up, 180 left, 270 down.
- Empty Enter while no command is active should repeat the last valid command.
- Empty Enter while a command is active confirms the current phase only if that phase explicitly allows confirmation.
- A compact visible command history should be added near the command input.
- Trim should be planned as an advanced base workflow, not merely a minimal one-shot command.

New planning document:

```text
docs/command-input.md
```

Planned v0.8 implementation sequence:

1. Add command input specification and parser infrastructure without changing existing tool behavior.
2. Add prompt state and visible command history to the UI/view-model.
3. Convert `LINE` first as the reference implementation.
4. Convert `POLYLINE` with `Close`/`Undo` options and empty Enter to finish.
5. Convert Rectangle, Circle and Arc 3P.
6. Convert Move, Copy and Break.
7. Implement advanced-base Trim with cutting-edge selection, `All`, trim-object phase, `Undo`, and picked-entity input carrying both `EntityId` and pick point.
8. Stabilize docs/tests and prepare v0.8 release notes.

Architectural direction:

- Introduce `CommandPromptState`, `CommandOption`, `CommandInputKind`, `CommandInputSubmission` and `CommandInputSubmissionKind`.
- Introduce a centralized `CommandInputParser` for absolute points, relative cartesian points, relative polar points, distances, options and empty confirmation.
- Introduce an interface similar to `ICommandDrivenTool` so tools can expose their current prompt and handle typed command submissions.
- Avoid splitting tool behavior into separate mouse-only and text-only paths.
- For Trim and future tools, plan a richer picked-entity input model with entity id and pick point; a simple `EntityId` is not sufficient to decide which side of an entity should be trimmed.


### 2026-05-16 - v0.8 command input parser infrastructure started

Implemented the first technical block of the v0.8 CAD-style command input refactor without converting existing tools yet.

Added the new command input infrastructure in `OpenCad2D.Tools.Input`:

- `CommandPromptState` for the active command name, user-facing prompt, expected input kind, options and empty-Enter behavior.
- `CommandOption` for keyword/shortcut options such as `Close/C`, `Undo/U` and `All/A`.
- `CommandInputKind` for prompt expectations such as `CommandName`, `Point`, `Distance`, `PointOrOption` and `SelectionOrOption`.
- `CommandInputSubmissionKind` and `CommandInputSubmission` for typed contextual submissions.
- `ICommandDrivenTool` as the future contract for tools that expose a prompt and accept parsed command input.

The existing low-level coordinate parser remains available through `CommandInputParser.Parse(string?)`, but its result enum has been renamed to `CommandInputParseKind` to free `CommandInputKind` for v0.8 prompt expectations. Existing behavior is preserved and `@distance<angle` is now also recognized by the legacy parser.

The new contextual parser overload is:

```csharp
CommandInputSubmission Parse(
    string? input,
    CommandPromptState promptState,
    Point2D? referencePoint = null)
```

It currently supports:

- idle command-name parsing;
- empty Enter confirmation when the prompt allows it;
- option keyword/shortcut matching;
- absolute points such as `100,50`;
- relative points such as `@25,-10` resolved from a supplied reference point;
- relative polar points such as `@100<45` resolved from a supplied reference point;
- distance, angle, number and text submissions.

No existing tool has been converted yet. The next block should integrate prompt/current history state into `MainWindowViewModel` and the UI while keeping old command aliases and tools working.

### 2026-05-16 - v0.8 command input infrastructure compile fix

Fixed the first compile issue found after the command input parser infrastructure patch.

`CommandInputSubmission` exposed properties named `Point`, `Distance`, `Number` and `Text`, and also had static factory methods with the same names. C# does not allow a type member method and property to share the same identifier, so the factory methods were renamed to avoid the conflict:

- `Point(...)` -> `FromPoint(...)`
- `Distance(...)` -> `FromDistance(...)`
- `Number(...)` -> `FromNumber(...)`
- `Text(...)` -> `FromText(...)`

`CommandInputParser` was updated to call the renamed factory methods. This is a compile-only fix and does not change parser behavior.

## v0.8 command input block 2 - prompt and visible history

Implemented the second command input refactor block:

- Added a small visible command history surface in the command line UI.
- Kept the existing `CommandLineHistory` as the logical command alias history used by tests and repeat logic.
- Added `VisibleCommandHistory` in `MainWindowViewModel`, capped to the latest 8 entries.
- Added `CommandInputPlaceholderText` so the input box can show contextual examples such as absolute, relative, and polar point input.
- Updated command submission to append user input, command activation, prompts, results, and errors to the visible history.
- Empty Enter is now submitted even when focus is on the canvas, so it can repeat the last command consistently.
- No drawing tools have been converted to `ICommandDrivenTool` yet; this block only improves feedback and UI plumbing.

Next recommended block: convert `LINE` to the new command-driven input flow while preserving mouse-click behavior.

## 2026-05-16 - v0.8 command input block 3: LINE command-driven

Implemented the first command-driven drawing tool for the v0.8 command input refactor.

- `LineTool` now implements `ICommandDrivenTool`.
- `LINE` exposes a contextual prompt state:
  - `LINE: Specify first point:`
  - `LINE: Specify second point:`
- The second point accepts absolute coordinates, relative coordinates, polar coordinates, and direct distance input.
- Text input and mouse input still share the same underlying two-point workflow.
- `TwoPointToolBase` now has `SubmitResolvedPoint(...)` so command-line points can be submitted without re-snapping or re-applying pointer constraints.
- `MainWindowViewModel.SubmitCommandInput(...)` now routes text to an active command-driven tool when appropriate.
- While a two-point command is in progress, command text is interpreted as input for that active command before being treated as a new alias.
- Existing legacy command input remains available for tools not yet migrated to `ICommandDrivenTool`.

Next planned block: migrate `PolylineTool` to the command-driven model with `Close` and `Undo` options.


### v0.8 command input block 3 compile/test refinement

- Fixed contextual parsing for prompts that accept both points and direct distances.
- Invalid point-like text now keeps the point parser's clear message instead of falling through to a generic distance error.
- Numeric input can still be accepted as a direct distance when the active prompt supports `PointOrDistance`.

## 2026-05-16 - v0.8 command input block 4: POLYLINE command-driven

Migrated `PolylineTool` to the new command-driven input model.

- `PolylineTool` now implements `ICommandDrivenTool`.
- Initial prompt: `POLYLINE: Specify first point:`.
- Collection prompt: `POLYLINE: Specify next point or [Close/Undo]:`.
- Point input supports mouse clicks, absolute coordinates, relative coordinates, relative polar coordinates and direct distance input through the existing parser/view-model routing.
- Empty Enter while a polyline is collecting vertices completes an open polyline.
- `C` / `Close` completes a closed polyline when at least three vertices are available.
- `U` / `Undo` removes the last collected vertex and keeps the command active.
- Text-submitted points are treated as resolved points and are not re-snapped. Mouse input continues to use the existing snap and angle constraint path.

Next planned block: migrate the remaining basic draw tools (`Rectangle`, `Circle`, `Arc 3P`) to the command-driven model.


### 2026-05-16 - v0.8 command input block 4 compile fix

Fixed a C# local-variable shadowing issue in `MainWindowViewModel.SubmitCommandInput` introduced while routing empty Enter to command-driven tools. The non-empty active command result variable was renamed so the polyline command-driven patch compiles without changing behavior.


## 2026-05-16 - v0.8 command input block 5: base draw tools command-driven

Migrated the remaining basic draw tools to the command-driven input model.

- `RectangleTool` now implements `ICommandDrivenTool`.
  - Prompt 1: `RECTANGLE: Specify first corner:`.
  - Prompt 2: `RECTANGLE: Specify opposite corner:`.
- `CircleTool` now implements `ICommandDrivenTool`.
  - Prompt 1: `CIRCLE: Specify center point:`.
  - Prompt 2: `CIRCLE: Specify radius point or type radius:`.
- `ArcThreePointsTool` now implements `ICommandDrivenTool`.
  - Prompt 1: `ARC3P: Specify start point:`.
  - Prompt 2: `ARC3P: Specify point on arc:`.
  - Prompt 3: `ARC3P: Specify end point:`.
- Mouse workflows continue to use the existing snapping and angle-constraint logic.
- Typed coordinates are submitted as resolved points and share the same command phases as mouse clicks.
- Added tests for direct tool command input and ViewModel command-line creation for Circle, Rectangle and Arc 3P.

Next planned block: migrate `Move`, `Copy` and `Break` to guided selection/base-point/destination-point workflows.

## 2026-05-16 - v0.8 command input block 6: Move, Copy and Break command-driven

Migrated the first edit tools to the command-driven workflow.

- `MoveTool` now implements `ICommandDrivenTool`.
  - Selection phase: `MOVE: Select objects to move:`.
  - Base point phase: `MOVE: Specify base point:`.
  - Destination phase: `MOVE: Specify destination point:`.
  - The destination point accepts absolute, relative, relative polar and direct-distance input.
- `CopyTool` now implements `ICommandDrivenTool` and has a guided selection/base/destination workflow matching `MoveTool`.
  - If nothing is selected, mouse clicks can select entities before pressing Enter.
  - If entities are already selected, the command starts from the base point phase.
- `BreakBetweenPointsTool` now implements `ICommandDrivenTool`.
  - Target entity is still selected from the drawing canvas.
  - First and second break points can be provided by mouse or command input.
  - The second break point supports absolute, relative, relative polar and direct-distance input.
- Added `BREAK` and `BR` aliases for `BreakBetweenPoints` while keeping existing `BREAKSEGMENT` and `BS` aliases.
- `MainWindowViewModel` now routes Enter/text to active command-driven selection phases, not only to commands that already have a base point.

Next planned block: evaluate and implement a first advanced `TRIM` command workflow with cutting-edge selection, `All`, trim-object picking and local `Undo`.

## 2026-05-16 - v0.8 command input block 6b

Completed a command-input coverage pass before the advanced Trim redesign.

Updated command-driven coverage:

- `RotateTool` implements `ICommandDrivenTool`.
  - Prompts: select objects before rotating, base point, reference point, destination point or typed angle.
  - Destination phase accepts either a point or an angle in degrees.
- `ScaleTool` implements `ICommandDrivenTool`.
  - Prompts: select objects before scaling, base point, reference point, destination point or typed scale factor.
  - Destination phase accepts either a point or a numeric scale factor.
- `AlignTool` implements `ICommandDrivenTool`.
  - Prompts: first source point, first destination point, second source point, second destination point, scale confirmation.
  - Scale confirmation accepts Enter/N for no scale and Y for scale.
- `BreakAtPointTool` implements `ICommandDrivenTool`.
  - Entity selection still comes from the canvas.
  - Break point can now be typed with absolute/relative/polar coordinate input after the entity is selected.
- `ExtendTool` and `TrimTool` now expose command-driven prompt states for their canvas-selection phases.
  - This prepares Trim for the upcoming advanced redesign with richer picked-entity input.
- `DeleteTool` implements `ICommandDrivenTool`.
  - Press Enter to delete the current selection.
  - If there is no selection, the prompt explains that objects must be selected first.

Parser additions:

- Added `PointOrAngle` / `PointOrAngleOrOption` prompt kinds.
- Added `PointOrNumber` / `PointOrNumberOrOption` prompt kinds.
- Updated command input routing so active command-driven tools receive text while they are waiting for any non-idle command input phase, not only when a base point exists.

### v0.8 command input note - active required steps and Enter

During the v0.8 command-input migration, empty Enter is intentionally context-sensitive:

- when no command-driven tool is active, empty Enter repeats the last repeatable command;
- when a command-driven tool is active and the current prompt accepts empty Enter, empty Enter confirms/completes the current step;
- when a command-driven tool is active and the current prompt requires input, empty Enter stays inside the active command and reports `Input is required for the current command step.`.

This means aliases typed while a command is actively waiting for a point or selection are treated as input for that active command, not as new commands. To start another command, the current command should be completed or cancelled first.

### v0.8 command input - Trim advanced base

Implemented a first advanced Trim workflow while preserving existing pointer behavior:

- `TRIM` now exposes command prompt options.
- Initial prompt supports `All` / `A` to use all visible supported entities as cutting edges.
- Target prompt supports `Undo` / `U` to undo the last trim operation performed inside the current Trim command.
- Target prompt accepts empty Enter to finish/reset the current Trim command.
- Ctrl-click can add more than one cutting edge while trimming.
- The existing single-boundary and two-boundary pointer workflow remains compatible.
- In `All` mode, the picked target entity is excluded from the effective cutting-edge list so an entity can still be trimmed even though all visible entities were selected as cutting edges.

## v0.8 Block 7b - Offset and Fillet planning/implementation notes

Decisions fixed before implementation:

- Offset is a v0.8 modify tool and must be command-driven.
- Initial Offset support started with `LineEntity`, `CircleEntity` and `ArcEntity`; it was later extended to straight-segment `PolylineEntity`.
- Offset flow:
  - `OFFSET`
  - `Specify offset distance:`
  - `Select object to offset:`
  - `Specify side to offset:`
- Offset keeps the distance active after each offset and returns to object selection, allowing repeated offsets until Escape.
- Fillet is a v0.8 modify tool and must be command-driven.
- Initial Fillet support is intentionally limited to Line-Line.
- Fillet supports the `Radius` / `R` option.
- Default fillet radius is `0`, so the command can also join two lines into a sharp corner.
- Fillet uses trim mode only; NoTrim is deferred.
- A picked entity must carry both entity id and pick point. This is now represented by `ToolPickedEntityInput` and is important for Offset, Fillet, Trim, Extend, Break and future Chamfer.

Implemented in Block 7b:

- Added `ToolId.Offset` and `ToolId.Fillet`.
- Added default aliases `OFFSET` / `O` and `FILLET` / `F`.
- Added left-panel buttons for Offset and Fillet.
- Added `OffsetTool` with command-driven prompts and support for lines, circles, arcs and straight-segment polylines.
- Added `FilletTool` with command-driven prompts, Radius option and Line-Line support.
- Updated command parser support for mixed point/scalar prompt kinds so commands can accept point-or-angle and point-or-number inputs correctly.

### v0.8 Offset/Fillet follow-up

Fixed a command-input routing issue introduced by the Offset/Fillet phase: pure distance prompts such as `OFFSET: Specify offset distance` must receive a `Distance` submission, while mixed point-or-distance prompts such as `LINE` second point or `CIRCLE` radius point may resolve a numeric distance into a point using the current cursor direction. Added an explicit `ShouldResolveDistanceAsPoint(...)` gate in `MainWindowViewModel` so Offset can move from distance input to entity selection correctly.

Also tightened nullable guards in `OffsetTool` and `FilletTool` before constructing `AddEntityCommand` / `ModifyEntitiesCommand`.

## 2026-05-16 - v0.8 Block 8: documentation and release stabilization

Completed the documentation stabilization pass for v0.8.

Updated documentation:

- `docs/roadmap.md` now marks the v0.8 command-input blocks as completed and records the remaining deferred backlog.
- `docs/command-input.md` now includes the final v0.8 command-driven coverage summary and the distance-routing rule.
- `docs/commands.md` now includes a v0.8 command/alias summary table.
- `docs/tools.md` now includes a v0.8 tool workflow summary for guided input, Offset and Fillet.
- `docs/modify-tools.md` now documents Offset, Fillet and `ToolPickedEntityInput`.
- `README.md` now reflects guided command input, advanced Trim, Offset and Fillet.
- Added `docs/release-v0.8.md`.

Final v0.8 implemented scope to remember:

- guided command input with prompt phases and visible history;
- absolute coordinates: `100,50`;
- relative cartesian coordinates: `@100,0`;
- relative polar coordinates: `@100<45`;
- idle Enter repeats the last valid command;
- active Enter is routed to the active command;
- command-driven Line, Polyline, Rectangle, Circle, Arc 3P;
- command-driven Move, Copy, Rotate, Scale, Align, Delete;
- command-driven Break Point, Break Segment, Extend, Trim, Offset, Fillet;
- Trim advanced base with `All`, Ctrl-click additional cutting edges, local `Undo` and repeated trimming;
- Offset supports Line, Circle and Arc;
- Fillet supports Line-Line with `Radius` / `R` and radius `0` sharp-corner join;
- `ToolPickedEntityInput` is the side-sensitive selection foundation.

Validation still to run locally before tagging the release:

```bash
dotnet build OpenCad2D.sln
dotnet test OpenCad2D.sln --no-build
```

Suggested manual smoke checks:

```text
LINE -> 100,100 -> @100<45
POLYLINE -> 0,0 -> @100,0 -> @50<90 -> C
OFFSET -> distance -> line/circle/arc -> side point
FILLET -> R -> 10 -> first line -> second line
TRIM -> All -> target side -> Undo
MOVE/COPY -> selection -> base point -> @50,0
BREAK -> entity -> first point -> second point
```

## 2026-05-16 - v0.8.x Block A color picker improvements

Implemented the first post-v0.8 usability stabilization block.

Decisions confirmed:

- use compact `ColorPicker` controls directly inside manager rows;
- store project settings later in `.opencad2d.json`;
- future Z-order hit testing must select the topmost entity;
- future align tools use the global selection bounding box;
- future distribute tools use center-based distribution.

Changes in this block:

- added `Avalonia.Controls.ColorPicker` to `OpenCad2D.App` using the same version as the other Avalonia packages;
- added the Fluent ColorPicker style include to `App.axaml`;
- Line Format Manager rows now expose a compact `ColorPicker` bound to the line format color;
- Text Format Manager rows now expose a compact `ColorPicker` bound to the text format color;
- existing `#RRGGBB` fields remain as precise/manual input and remain the serialization/validation source;
- layer appearance remains line-format driven, so the Layer Manager still assigns line formats rather than direct layer colors.

Next planned blocks before v0.9:

1. persist document/editor settings in `.opencad2d.json`;
2. implement Draw order / Z-order independent from layers;
3. add Align Top/Right/Left/Bottom tools;
4. add Distribute Horizontal/Vertical tools based on entity centers.

## v0.8.x stabilization - document settings persistence

The native `.opencad2d.json` format now persists document-level drafting settings under `settings`:

- `currentLayerId`;
- `currentTextFormatId`;
- grid settings: kind, visibility, minor/major step, origin, screen spacing thresholds and isometric angle;
- snapping settings: enabled state, active snap modes and snap tolerance;
- drafting settings: Ortho and Polar Tracking.

Important rule: settings that affect continuing the drawing are document data and mark the document dirty when changed. Local UI state such as window size, theme, recent files or panel widths must remain in user-local settings, not in `.opencad2d.json`.

Compatibility: old files without `settings` or with partial settings must still load using defaults. App-level loading applies settings after the document is loaded so the serializer stays independent from `OpenCad2D.App`, `OpenCad2D.Tools` and `OpenCad2D.Interaction`.

### 2026-05-16 - Document settings persistence round-trip fix

Fixed `JsonDocumentSerializer.NormalizeSettings` so legacy `Serialize(document, currentLayerId, viewport)` calls preserve the provided current layer when no explicit `DocumentSettingsDto` is supplied. The new settings DTO defaulted `CurrentLayerId` to `"0"`, which caused older round-trip tests to restore layer `0` instead of the caller-provided current layer such as `Details`. Explicit settings passed by the app are still preserved.

## Draw order / Z-order stabilization

Implemented in the pre-v0.9 stabilization phase:

- Draw order is independent from layers.
- Higher `DrawOrder` entities render above lower `DrawOrder` entities.
- Point hit-testing uses draw order as the topmost tie-breaker when overlapping entities are equally close.
- The left tool panel includes an `ORDER` group with:
  - `To Front`
  - `To Back`
  - `Forward`
  - `Backward`
- Command input action aliases:
  - `BRINGTOFRONT`, `BTF`, `FRONT`
  - `SENDTOBACK`, `STB`, `BACK`
  - `BRINGFORWARD`, `BF`, `FORWARD`
  - `SENDBACKWARD`, `SB`, `BACKWARD`
- Draw-order changes are undoable and keep the current selection.

### v0.8.x draw order one-step fix

The draw-order service now handles `BringForward` and `SendBackward` as true one-step reorder operations in the ordered entity list. The selected entities swap across the nearest unselected neighbor, then the draw orders are normalized into a dense sequence. This avoids duplicate draw-order values and keeps `Forward`/`Backward` between adjacent entities instead of jumping over the next entity.

## 2026-05-16 - UI cleanup before align/distribute phase

Applied a small UI consistency cleanup after the draw-order/Z-order work:

- Moved the `Delete` button from the `ORDER` group into the `EDIT` group, immediately after `Fillet`.
- Added `ToolTip.Tip` text to all buttons in `MainWindow.axaml`, including file actions, global actions, left tool panel tools, draw-order actions, property panel buttons and command-related actions.
- The `ORDER` group now contains only draw-order controls: `To Front`, `To Back`, `Forward`, `Backward`.

Next planned blocks remain:

1. Align tools: Align Left/Right/Top/Bottom using the global selection bounding box.
2. Distribute tools: distribute selected entities horizontally/vertically by center positions.


## 2026-05-16 - Property panel draw order value

- Added a read-only `Draw order` row to the single-selection property panel.
- This helps verify Z-order changes after `To Front`, `To Back`, `Forward`, and `Backward`.
- Confirmed intended behavior: `To Front` moves the selected entity to the highest draw order range, visible entities are rendered in increasing draw order, so the entity is drawn last and appears above overlapping entities. Hit testing also prefers the higher draw-order entity.

## v0.8.x - Align object tools

Added a pre-v0.9 CAD usability block for object alignment actions.

Implemented/expected behavior:

- Align Left, Align Right, Align Top and Align Bottom operate on the current selection.
- The reference is the combined bounding box of the selected entities.
- Align Left moves each entity so its `Bounds.MinX` matches the selection `MinX`.
- Align Right moves each entity so its `Bounds.MaxX` matches the selection `MaxX`.
- Align Top follows the visual canvas convention and moves each entity so its `Bounds.MinY` matches the selection `MinY`.
- Align Bottom follows the visual canvas convention and moves each entity so its `Bounds.MaxY` matches the selection `MaxY`.
- The operation preserves the other axis.
- At least two selectable entities are required.
- Operations are undoable via `ReplaceEntitiesCommand`.
- The current selection is preserved after the operation.
- UI buttons live in the left tool panel under `ALIGN OBJECTS`.
- Command aliases are action commands, not modal tools:
  - `ALIGNLEFT`, `ALEFT`
  - `ALIGNRIGHT`, `ARIGHT`
  - `ALIGNTOP`, `ATOP`
  - `ALIGNBOTTOM`, `ABOTTOM`

Next planned block: distribute selected entities horizontally/vertically by center points.

---

## v0.8.x distribute tools

Implemented object distribution tools after the Align tools:

- `DistributionOperation` with `Horizontal` and `Vertical`.
- `DistributionService` creates transformed replacements for selected entities.
- Distribution requires at least three selectable entities.
- Horizontal distribution sorts entities by bounding-box center X, keeps the first and last fixed, and spaces intermediate center X values evenly.
- Vertical distribution sorts entities by bounding-box center Y, keeps the first and last fixed, and spaces intermediate center Y values evenly.
- Operations are undoable through `ReplaceEntitiesCommand` and preserve selection.
- UI buttons added to the `ALIGN OBJECTS` group: `Distribute H` and `Distribute V`.
- Command aliases:
  - `DISTRIBUTEHORIZONTAL`, `DISTRIBUTEHORIZONTALLY`, `DH`;
  - `DISTRIBUTEVERTICAL`, `DISTRIBUTEVERTICALLY`, `DV`.

Design decision: distribution is by centers for now, not by equal gaps between bounding boxes.


### v0.8.x Polyline Offset follow-up

Extended `OffsetTool` to support `PolylineEntity` made of straight segments. The command flow remains unchanged: specify distance, select object, specify side. For polylines, the side is determined by the nearest original segment to the side point; each segment is offset by the same signed normal and adjacent offset segments are joined with mitered infinite-line intersections. Open polylines keep translated start/end caps; closed polylines compute every corner from adjacent offset segment intersections.

Current limitations are intentional: no rounded joins, no bulge/arc polyline offset, and no advanced self-intersection cleanup. Degenerate zero-length segments and duplicate resulting consecutive vertices are rejected with a clear warning.

### v0.8.x Offset safety and preview

- Offset now exposes a live preview while the command is in the side-selection phase.
- Preview uses the same `TryCreateOffsetEntity` geometry path as the final click, so the displayed result and committed result stay aligned.
- Offset tests were expanded for invalid distances, zero-length lines, circle/arc inner offsets that would collapse the radius, collinear polyline joins, too-few-vertex polylines, and preview behavior.
- Polyline offset remains miter-only for straight-segment polylines; rounded joins and bulge/arc polyline offsets remain future work.

### v0.8.x command input layout cleanup

The command input area was simplified after user testing:

- The active tool / command indicator was moved from the top toolbar to the command input row.
- The command prompt now sits inline immediately before the input box.
- The visible multi-line command history panel above the input was removed from the default layout because it consumed vertical space without adding enough value during normal drafting.
- The underlying command history data can remain in the view model for future use, but the default UI should stay compact.

Current command row layout:

```text
[Active tool] [Prompt text] [Command input]
```


### Command input compact layout compile fix

Removed the stale `ActiveCommandTextBlock` code-behind update from `MainWindow.axaml.cs`. The compact command input layout now binds the active tool name directly in XAML next to the command prompt, so no named `ActiveCommandTextBlock` control exists anymore.


### v0.8.x Custom line style pattern foundation

User clarified the naming model: `LineStyle` is the stroke style/pattern family; `LineFormat` remains the full reusable format with color, weight and style. Implemented the model/persistence foundation for custom line style patterns:

- `LineStyle.Custom` added.
- `LineFormat` now stores `DashPattern` as dash/gap values in drawing units.
- Default style patterns are now `Dashed = [8,4]`, `DashDot = [12,4,1,4]`, `DashDotDot = [12,4,1,4,1,4]`, `Continuous = []`.
- `.opencad2d.json` line format DTOs now include `dashPattern`.
- Legacy files with missing `dashPattern` are rebuilt from `lineStyle`.
- Invalid persisted patterns fall back to style defaults on load.
- `default.opencad2d.json` includes explicit `dashPattern` values.

Next planned line-format blocks: Line Format Manager pattern editor + preview, then canvas/SVG rendering from `LineFormat.DashPattern` instead of only `LineStyle`. DXF custom LTYPE generation can remain a later refinement.


### v0.8.x line style dash pattern SVG export fix

- SVG export now uses the effective `LineFormat.DashPattern` instead of recomputing the pattern only from `LineStyle`.
- The dashed SVG export test now expects the new default dashed pattern `8 4` expressed in drawing units.
- This keeps the distinction clear: `LineStyle` is the style category/preset, while `LineFormat` stores the actual color, weight, style and dash pattern.


## 2026-05-16 - SVG dashed pattern test alignment

Aligned `SvgExporterTests.Export_ShouldWriteStrokeDashArray_ForDashedLineFormat` with the new line style pattern foundation. The default `Dashed` pattern is now `8,4` drawing units, so SVG export should contain `stroke-dasharray="8 4"` instead of the old legacy `6 3`.

## v0.8.x line style pattern editor

The Line Format Manager now exposes the effective dash pattern of each line format.
The distinction is:

- `LineStyle` describes the stroke style/pattern category, including `Custom`.
- `LineFormat` remains the complete reusable appearance: color, line weight, style and effective dash pattern.

Dash patterns are edited as comma-separated dash/gap pairs in drawing units, for example `8,4` or `12,4,1,4`.
Changing a preset style applies its default pattern. Editing the pattern manually marks the style as `Custom`.
The manager also shows a compact textual preview of the resulting pattern.


## 2026-05-16 - Line format pattern editor warning fix

- Replaced obsolete Avalonia `TextBox.Watermark` usage in `LineFormatManagerWindow.axaml` with `PlaceholderText`.
- No functional changes to line format pattern editing, validation, preview, or persistence.

### Mirror axis preview update

The Mirror tool now draws the mirror axis while the user is choosing the second axis point. The preview also keeps showing the mirrored entities so the user can verify the axis direction before confirming whether source objects should be deleted.



## Latest update - Polygon tool

Added the command-driven regular polygon tool before v0.9. `POLYGON` / `PG` asks for side count, center point, then vertex point or radius. It creates a closed `PolylineEntity`, includes preview support, button integration, aliases and tests. Next planned drawing tools: multiline text, spline.

## Polygon tool test alignment

The Polygon tool is part of the Draw category. ToolRegistry draw-category tests must expect 11 draw tools and include `ToolId.Polygon` in both normal and case-insensitive category checks.


## Latest update - Ellipse tool

Added Block 4 Ellipse support. The draw category now includes `EllipseTool` with `ELLIPSE` / `EL` aliases. The workflow asks for center, major axis endpoint and minor radius point or typed radius. The implementation adds `EllipseEntity`, rendering preview/final drawing, SVG/PDF/DXF export, JSON persistence, center/quadrant snaps, grip editing, property panel readout, button integration and focused tests. ToolRegistry draw-category tests now expect 11 draw tools.

Follow-up for Block 4: Trim and Break now accept ellipses. `CadEntityIntersectionService` approximates ellipse intersections with sampled line segments. `CadTrimService` can trim ellipse targets and use ellipses as cutting edges; resulting partial ellipse fragments are returned as open `PolylineEntity` approximations. `CadBreakService` supports Break Point and Break Segment on ellipses, also returning open polyline approximations because there is no partial ellipse-arc entity yet.



## Latest update - Trim/Break polyline and polygon verification

Verified and strengthened modification coverage for polylines and polygon-like closed polylines after the Ellipse block.

- `CadBreakService` has focused tests for open polylines, closed polylines and regular-polygon-style closed `PolylineEntity` cases.
- `Break Point` opens closed polylines at the picked point.
- `Break Segment` removes the segment between two projected points on open polylines, and removes the shortest path on closed polylines/polygons.
- `CadTrimService` now routes polyline targets through the same multi-boundary path used by lines and ellipses, so `TrimByBoundaries` supports polylines as targets too.
- Added trim regression tests for open polylines, closed polygon-like polylines, and two-boundary trimming on a polyline segment.

Operational note: regular polygons are stored as closed `PolylineEntity`, so the same Trim/Break rules apply to both manually drawn closed polylines and polygons created by the Polygon tool.

Next planned block after this historical note was completed: multiline text (`MTEXT`) with insertion point, multiline dialog, persistence and export.


### Block 6 — SPLINE phase 1

Added initial Bezier spline support. The new `BezierSplineEntity` stores control points and an open/closed flag, evaluates the curve with De Casteljau sampling, and exposes `ToPolylineApproximation()` for modify/export workflows that need segment geometry.

Implemented `SplineTool` with `SPLINE` / `SPL` aliases. Workflow: specify control points, `Undo` removes the last control point, `Close` creates a closed spline, Enter creates an open spline. Rendering and preview draw the sampled curve. Grip editing exposes each control point plus a move-entity grip.

Export/persistence status: JSON round-trip includes `BezierSplineEntityDto`; SVG/PDF export use sampled polyline/polygon approximations; DXF export writes a `SPLINE` entity with control points. DXF import now supports readable control-point SPLINE entities; full external NURBS fidelity remains deferred.

Modify support: Trim, Break Point, Break Segment and Offset accept splines by converting them to sampled `PolylineEntity` geometry. Result fragments are currently polylines, not partial spline entities. This is intentional for phase 1 and avoids introducing a partial spline model before the core curve behavior stabilizes.

## v0.8.1 stabilization kickoff

Started the v0.9 stabilization track after the post-v0.8 critical review.

Scope for this first stabilization pass:

- Added `docs/stabilization-v0.9-plan.md` to triage the critical review into actionable milestones.
- Protected `CadCanvas.OnPointerPressed` with a top-level exception boundary by moving the awaited logic into `OnPointerPressedAsync` and reporting failures via `ToolResult.Cancelled` instead of letting `async void` exceptions escape silently.
- Replaced the old `_isTextInputDialogOpen` boolean gate with `AsyncReentrancyGuard`, a small non-blocking semaphore-based guard used while async tools such as TEXT/MTEXT open modal input dialogs.
- Added an end-to-end persistence workflow test covering draw/annotate/save/reopen for the current primary entity set: line, circle, arc, closed polyline/polygon, ellipse, Bezier spline, text, MTEXT and horizontal dimension.

This pass intentionally avoids the large `CadCanvas` renderer/preview refactor. That should be handled next as a separate low-risk milestone after runtime safety and workflow tests are green.
---

## Latest update — v0.8.2 export/interop stabilization kickoff

Implemented the next stabilization step after the v0.8.1 runtime-safety pass.

Added:

- `EndToEndExportWorkflowTests` covering draw -> annotate -> export to SVG/PDF/DXF.
- A first import DXF -> trim -> export regression test using simple imported LINE/TEXT/MTEXT entities.
- `docs/dxf-compatibility.md` to track manual viewer validation.
- `samples/dxf/compatibility/` with initial ASCII DXF smoke-test files.

Important current state:

- Automated DXF structure tests exist, and the v0.8 compatibility samples have been opened successfully during release validation.
- Dimensions are intentionally exported as graphic primitives, not native DXF `DIMENSION` entities.
- `ELLIPSE` and first-pass `SPLINE` import now exist; external NURBS fidelity remains future work.
- `LWPOLYLINE` bulge import has since been implemented by converting bulge segments to native line/arc geometry.

Recommended next step:

1. Run the full test suite.
2. Open the initial DXF samples in LibreCAD and QCAD.
3. Record results in `docs/dxf-compatibility.md`.
4. Then continue with v0.8.3: small `MainWindow.axaml.cs` cleanup before the larger `CadCanvas` renderer extraction.

## Latest update — v0.8.3 MainWindow refresh cleanup

Started the architecture-cleanup part of the v0.9 stabilization track with a deliberately small `MainWindow.axaml.cs` change.

Implemented:

- Added `RefreshAllUiAfterDocumentChange(bool clearSnapMarker = true, bool focusCanvas = true)`.
- Replaced duplicated document-replacement refresh blocks after New, Open and Import DXF with the helper.
- Kept layer-manager refresh paths separate because they update layer controls without replacing the whole document.
- Did not start the larger `CadCanvas` renderer/preview extraction in this pass.

Recommended next step:

1. Run the full test suite.
2. Manually check New, Open and Import DXF to confirm layer combo, polar tracking combo, status bar, snap marker and canvas focus still refresh correctly.
3. Continue v0.8.3 by extracting entity rendering from `CadCanvas` into a renderer class, without changing tool behavior.

## Latest update — v0.8.3 CadCanvas entity renderer extraction

Continued the architecture-cleanup track by extracting CAD entity drawing from `CadCanvas` into `OpenCad2D.App.Rendering.CadEntityRenderer`.

Implemented:

- Added `CadEntityRenderer` in the App rendering layer.
- Moved entity drawing for points, lines, circles, arcs, polylines, ellipses, Bezier splines, single-line text, MTEXT and dimension entities into the renderer.
- Kept `CadCanvas` responsible for viewport state, visible-entity selection, pen/style resolution, grid/UCS overlays, snap markers, active-tool previews, crosshair and input handling.
- Left active tool preview rendering in `CadCanvas` for now to avoid mixing preview abstraction with entity-render extraction.

Important design note:

This is intentionally an extraction-only refactor. It should not change CAD behavior, entity geometry, selection, snapping, export or persistence. Tool-specific preview switch removal remains the next `CadCanvas` refactor target.

Recommended validation:

1. Run the full test suite.
2. Manually open a drawing containing line, circle, arc, polyline/polygon, ellipse, spline, TEXT, MTEXT and dimensions.
3. Check selected-entity highlight color and lineweights.
4. Check that dimension text, rotated text and multiline text still render correctly.
5. Then continue with preview extraction using a geometry-preview abstraction rather than another large canvas rewrite.

## Latest update — v0.8.3 CadCanvas active-tool preview renderer extraction

Continued the architecture-cleanup track by extracting transient active-tool preview drawing from `CadCanvas` into `OpenCad2D.App.Rendering.CadToolPreviewRenderer`.

Implemented:

- Added `CadToolPreviewRenderer` in the App rendering layer.
- Moved active-tool preview drawing for draw tools, dimension tools, modify tools, measure tools, selection window, zoom window and grip-edit overlays out of `CadCanvas`.
- Reused the existing `CadEntityRenderer` for preview entities so entity rendering remains centralized.
- Kept `CadCanvas` responsible for viewport state, grid/UCS overlays, snap markers, crosshair, workspace events and pointer/keyboard input.
- Did not change tool behavior, command handling, geometry, export, import or persistence.

Important design note:

This is still an extraction-only refactor. `CadToolPreviewRenderer` intentionally preserves the current concrete-tool dispatch so this pass stays low-risk. The next architecture step should not move more drawing by copy/paste; it should introduce a small preview descriptor/protocol so tools can provide preview geometry without `CadToolPreviewRenderer` knowing every concrete tool type.

Recommended validation:

1. Run the full test suite.
2. Manually verify preview rendering for Line, Rectangle, Circle, Arc, Polyline, Polygon, Ellipse, Spline, Move, Copy, Rotate, Scale, Align, Trim, Extend, Break, Offset, Mirror, Measure Distance, Measure Angle, Selection Window, Zoom Window and Grip Edit.
3. Check that base-point markers, measurement vectors, highlighted trim/extend entities and grip hot/warm/cold markers still render correctly.
4. Continue with keyboard/input delegation only after preview rendering is stable.


## Latest update — v0.8.3 active-tool keyboard delegation

Continued the architecture-cleanup track by removing the remaining tool-specific keyboard branches from `CadCanvas.OnKeyDown`.

Implemented:

- Added `CadToolKey`, a UI-framework-independent key enum in `OpenCad2D.Tools.Common`.
- Added `IKeyboardAwareTool` for tools that need active keyboard handling.
- Implemented `IKeyboardAwareTool` in `AlignTool`, `MoveTool`, `CopyTool`, `PolylineTool` and `GripEditTool`.
- Replaced concrete `is AlignTool`, `is MoveTool`, `is CopyTool`, `is PolylineTool` and `is GripEditTool` branches in `CadCanvas.OnKeyDown` with a single delegation path.
- Kept global canvas shortcuts in `CadCanvas`: Escape, Delete selection fallback, Ctrl+Z, Ctrl+Y, Tab grip edit and Home zoom extents.

Behavior preserved:

- ALIGN Enter confirms without scale; S confirms with scale.
- MOVE/COPY Enter confirms selection when the tool is in entity-selection mode.
- POLYLINE Enter completes open; C completes closed.
- Grip Edit Delete removes the current polyline vertex when supported.

Recommended validation:

1. Run the full test suite.
2. Manually verify ALIGN Enter/S, MOVE Enter after selecting entities, COPY Enter after selecting entities, POLYLINE Enter/C and Grip Edit Delete.
3. Continue v0.8.3 with preview descriptors so `CadToolPreviewRenderer` can stop switching over concrete tool types.

## 2026-05-16 - v0.8.4 command history navigation

Implemented CAD-style Up/Down navigation for the command input. `MainWindowViewModel` now exposes `NavigateCommandHistoryPrevious()`, `NavigateCommandHistoryNext()` and `ResetCommandHistoryNavigation()` so the UI can recall stored command/action submissions without exposing coordinate/point input as reusable commands. `MainWindow.axaml.cs` handles `Key.Up` and `Key.Down` for the command input and canvas-focused workflow, places recalled text in the command box and keeps the caret at the end.

Added App tests covering most-recent-first navigation, next navigation back to an empty input, reset after command submission and empty-history behavior. Updated `docs/command-input.md`, `docs/stabilization-v0.9-plan.md` and `docs/roadmap.md`.



## 2026-05-16 - v0.8.4 conservative dimension stale marker

Implemented the first non-associative dimension safety pass. `DimensionEntity` now has an `IsStale` flag and all dimension subclasses preserve it through id/layer/transform/recreate flows. Transform and topology-modify commands conservatively mark dimensions as stale after geometry changes, while undo restores the previous dimension stale states. `ReplaceEntitiesCommand` gained an opt-in `markDimensionsStale` mode used by grip edits, alignment and distribution, while layer/style/property-only replacement paths remain unchanged.

The stale flag is serialized through the JSON DTOs and is shown in the Property Panel as `Status: Checked` or `Status: Potentially stale`. Canvas rendering now draws stale dimensions with a distinct dashed amber pen when they are not selected.

Added tests for move/modify/replace stale marking and undo restoration, and extended the persistence end-to-end workflow to preserve a stale dimension. The next v0.8.4 task should add a user-facing command/action to mark stale dimensions as checked, then continue with first-pass command autocomplete.

## 2026-05-16 - v0.8.4 first-pass command autocomplete

Implemented a lightweight command autocomplete workflow for the command input. `MainWindowViewModel.GetCommandAutocompleteSuggestion()` now completes known command/action prefixes using registered aliases plus action commands, while intentionally ignoring coordinate/distance syntax. `MainWindow.axaml.cs` handles Tab in the command input and in canvas-focused workflows only when there is non-empty command text and a valid suggestion; empty Tab remains available for canvas grip edit behavior.

Added App tests for command completions such as `li -> LINE`, `mt -> MTEXT`, `m -> MOVE`, action completion such as `selecta -> SELECTALL`, and no-suggestion cases for empty input, coordinates, unknown commands and already-complete commands. Updated `docs/command-input.md`, `docs/stabilization-v0.9-plan.md` and `docs/roadmap.md`.

Next recommended v0.8.4 task: add a user-facing command/action to mark stale dimensions as checked, then move to Fillet/Offset refinement.

## 2026-05-16 - v0.8.5 Fillet live preview kickoff

Implemented the first Fillet refinement pass. `FilletTool` now keeps transient preview entities while the command is waiting for the second line; `OnPointerMoved` evaluates the same Line-Line fillet geometry used by final creation and exposes the result through `GetPreviewEntities()`. `CadToolPreviewRenderer` renders those preview entities with the standard preview pen, so users can see the trimmed line result and tangent arc before committing the second pick.

The preview is cleared when the tool is cancelled, deactivated, reset, completed or when the hovered second entity is invalid. The Line-Line fillet geometry also gained an explicit degenerate-bisector guard before normalizing the angle bisector, making near-opposite branch cases safer.

Added Tool tests covering live preview generation without changing the document and preview cleanup after committing the fillet. Subsequent v0.8.5 work added Fillet Trim/NoTrim, near-collinear safeguards, Offset miter-limit fallback, and DXF bulge/ELLIPSE/SPLINE import.


## 2026-05-16 - v0.8.5 Fillet Trim/NoTrim refinement

Implemented the next Fillet refinement pass. `FilletTool` now supports a command-driven `Trim` option with `Trim` and `NoTrim` modes. Trim remains the default and preserves the existing behavior: the two source lines are replaced by trimmed line segments plus the fillet arc. NoTrim keeps both source lines unchanged and adds only the tangent arc for positive-radius fillets.

Radius `0` intentionally remains Trim-only because NoTrim with zero radius would not create any new geometry. The tool returns a non-mutating result in that case. The live preview uses the same trim mode as final creation, so NoTrim previews only the future arc while Trim previews the trimmed source lines plus the arc.

Added tests for trim-mode command input, NoTrim creation, NoTrim preview, zero-radius NoTrim rejection, and near-parallel line safety.

## v0.8.5 Offset miter-limit refinement

Offset polyline/sampled-spline joins now keep the existing miter behavior for normal corners, but apply a conservative miter limit (`distance * 4.0`). If the infinite-line miter intersection would create a spike beyond that limit, the join falls back to two bevel-style vertices (`previous.End` and `current.Start`). This prevents acute polyline corners from generating very distant offset vertices while preserving the existing rectangle/L-shape behavior.

Updated files:

- `src/OpenCad2D.Tools/Editing/OffsetTool.cs`;
- `tests/OpenCad2D.Tools.Tests/OffsetToolTests.cs`;
- `docs/modify-tools.md`;
- `docs/tools.md`;
- `docs/stabilization-v0.9-plan.md`;
- `docs/roadmap.md`;
- `docs/ai-handoff.md`.

Remaining Offset work: configurable join styles (`Miter`, `Bevel`, `Round`), true round joins, better curve/bulge offset, and self-intersection cleanup for complex polylines.

## 2026-05-16 — v0.8.5 DXF LWPOLYLINE bulge import

Implemented the next DXF import compatibility improvement after Fillet/Offset refinement: `LWPOLYLINE` bulge segments are now converted to native `ArcEntity` geometry instead of being flattened to straight segments.

Behavior:

- straight `LWPOLYLINE` entities still import as `PolylineEntity`;
- when any vertex has a non-zero bulge, the lightweight polyline is imported as separate `LineEntity` and `ArcEntity` segments;
- open polylines convert segments from vertex `i` to `i+1`;
- closed polylines also convert the closing segment from the last vertex back to the first vertex, using the last vertex bulge;
- the import log records an informational message explaining that bulge geometry was imported as separate line/arc entities.

This preserves curved geometry from external DXF files without introducing a curved-polyline model yet. Future work: preserve original LWPOLYLINE topology as a compound entity if/when OpenCad2D gains polyline arc segments/bulge support.


## Latest update - v0.8.5 DXF ELLIPSE import

Implemented DXF `ELLIPSE` import. Full ellipse parameter ranges are mapped to native `EllipseEntity`; partial elliptical arcs are approximated as open `PolylineEntity` instances. Added importer tests for full ellipses, omitted parameters, partial ranges, invalid major axis and invalid ratio. Next DXF target: external NURBS spline fidelity and compatibility samples.


## 2026-05-17 — v0.8 final documentation cleanup

Aligned the active documentation set with the implemented v0.8.x state after Ellipse, MTEXT, Bezier Spline, Fillet Trim/NoTrim, Offset miter-limit fallback, command history/autocomplete, dimension stale markers, and DXF bulge/ELLIPSE/SPLINE import.

Important documentation corrections:

- `known-limitations.md` now treats command history/autocomplete, Fillet NoTrim, full DXF ELLIPSE import and readable DXF SPLINE import as implemented.
- `release-v0.8-final.md` now reflects DXF bulge, ELLIPSE and SPLINE import support instead of listing them as future work.
- `README.md` now mentions command history/autocomplete, dimension stale markers, Fillet Trim/NoTrim, Offset miter-limit fallback and the expanded DXF import subset.

Remaining pre-release tasks after this documentation cleanup were moved to the DXF compatibility block and final release gate.


## 2026-05-17 — v0.8 final DXF compatibility samples

Prepared the v0.8 manual DXF compatibility sample set under `samples/dxf/compatibility/`. The set now includes seven ASCII DXF files covering lines/layers, TEXT/MTEXT, arcs/circles/ellipses, polylines/polygons with bulge arcs, dimensions as graphical primitives, SPLINE control points and a mixed v0.8 smoke drawing.

Updated `docs/dxf-compatibility.md` with the sample matrix and manual validation checklist. The seven v0.8 sample files were manually opened successfully by Emilie before the final release gate. Viewer versions were not recorded in this pass and should be added in a future compatibility audit when available.

Remaining pre-release tasks:

- run the final clean/build/test release gate;
- verify the working tree is clean;
- tag `v0.8.0`;
- publish the GitHub release text from `docs/release-v0.8-final.md`.


## 2026-05-17 — v0.8 final release gate cleanup

Consolidated the final release documents after the DXF compatibility samples were manually validated successfully. Updated the roadmap/stabilization plan to mark DXF sample validation as complete for v0.8, while keeping a note that exact external viewer versions should be recorded in a future audit.

Final pre-tag gate:

- `dotnet clean`;
- `dotnet restore`;
- `dotnet build`;
- `dotnet test`;
- `git status`;
- commit release docs/samples if needed;
- tag and push `v0.8.0`.

## 2026-05-17 — v0.8.5 stabilization S2 DXF SPLINE knot vector

Completed the second stabilization pass after the dimension-stale delete fix. `DxfExporter.WriteBezierSpline` now writes a structurally complete DXF `SPLINE` header for OpenCad2D Bezier splines: degree, knot count, control point count, fit point count, open-uniform knot values and control points. Closed splines now use the closed + planar DXF flags without the periodic flag, because the exported knot vector is open-uniform rather than periodic.

Added DXF export tests for quadratic and cubic spline knot vectors and for the closed-spline flag. Updated `docs/dxf-export.md` to document the SPLINE group codes and the current NURBS limitations.

Validation note: tests were prepared but not executed in the assistant sandbox because `dotnet` is not installed there. Run `dotnet build OpenCad2D.sln` and `dotnet test OpenCad2D.sln --no-build` locally.


## 2026-05-17 — v0.8.5 stabilization S3 DXF external validation preparation

Prepared the third stabilization pass after the SPLINE knot-vector export fix. The project now includes an explicit manual DXF compatibility sample folder under `samples/dxf/compatibility/` with seven small ASCII DXF files:

- `01_basic_lines_layers.dxf`;
- `02_text_mtext.dxf`;
- `03_arcs_circles_ellipses.dxf`;
- `04_polylines_polygons.dxf`;
- `05_dimensions_as_geometry.dxf`;
- `06_spline_bezier.dxf`;
- `07_mixed_drawing.dxf`.

Updated `docs/dxf-compatibility.md` so the compatibility status distinguishes clearly between automated/internal checks and external viewer validation. The previous v0.8 note remains documented as a historical pass, but the new rule is stricter: a manual audit is complete only when the application name, exact version/build, operating system and date are recorded.

Next validation step: open all seven sample files in LibreCAD and QCAD at minimum, record pass/partial/fail results in `docs/dxf-compatibility.md`, and only then mark the external compatibility audit as complete.

## 2026-05-17 — v0.8.5 stabilization S4 curve intersection snaps

Completed the fourth stabilization pass for intersection snapping. `IntersectionSnapProvider` now includes a first-pass curve-intersection path for `EllipseEntity` and `BezierSplineEntity` by converting supported curves to high-resolution polyline approximations during snap candidate evaluation.

New covered combinations include line/ellipse, polyline/ellipse, circle/ellipse, ellipse/ellipse, line/spline, polyline/spline, circle/spline, ellipse/spline and spline/spline. Existing exact analytic cases for lines, polylines, circles and arcs remain unchanged.

Added focused interaction tests for line/ellipse, polyline/ellipse, line/spline and ellipse/spline intersection snaps. Updated snapping and limitation docs to make clear that ellipse/spline intersections are currently approximate for interactive snapping, not exact analytic/NURBS solving.

Validation note: tests were prepared but not executed in the assistant sandbox because `dotnet` is not installed there. Run `dotnet build OpenCad2D.sln` and `dotnet test OpenCad2D.sln --no-build` locally.


## 2026-05-17 - S5 preview provider first migration

Started the first low-risk refactor of `CadToolPreviewRenderer` after the v0.8.5 stabilization passes.

Added `IToolPreviewEntityProvider` in `OpenCad2D.Tools.Common`. The interface lets tools expose transient preview geometry as `CadEntity` instances, so the App renderer can draw these previews without switching on each concrete tool type.

Migrated representative tools only:

- `LineTool` for a simple two-point drawing preview;
- `MoveTool` for a context-aware modify preview based on the current selection and tool context;
- `ThreePointDimensionToolBase`, so horizontal, vertical, aligned, radius, diameter and angular dimensions can provide their preview entities through the shared protocol.

`CadToolPreviewRenderer` now tries the provider first and keeps the legacy concrete-tool dispatch as fallback for tools not yet migrated or for previews that require custom overlay drawing. Measurement vectors, base-point markers and other custom overlays remain unchanged.

Added `ToolPreviewEntityProviderTests` to lock the protocol for line, move and dimension previews. Future cleanup can migrate Rectangle/Circle/Arc/Polyline/Spline/Polygon, then modify tools, and finally remove dead fallback cases once manual preview validation is complete.

## 2026-05-17 - S6 application logging foundation

Added the first minimal application logging abstraction after the preview-provider stabilization pass.

New `OpenCad2D.App.Diagnostics` types:

- `ApplicationLogLevel`;
- `ApplicationLogEntry`;
- `IApplicationLogger`;
- `TraceApplicationLogger`;
- `InMemoryApplicationLogger`.

`CadCanvas` now owns a configurable `IApplicationLogger` instance, defaulting to `TraceApplicationLogger.Instance`. `HandlePointerPressedException` logs the full exception, including stack trace through `Exception.ToString()`, before showing the existing user-facing status message. This keeps the UI recoverable while preserving enough diagnostic detail for debugging tool/input failures.

Added `ApplicationLoggerTests` to verify in-memory logging, exception preservation and basic entry validation. Future passes can route additional application-level exception handlers through the same abstraction and optionally replace the trace logger with a file-backed logger.


## 2026-05-17 - S8 MTEXT DXF reference width

Added `MultilineTextEntity.ReferenceWidth` as the native holder for DXF MTEXT reference rectangle width. The value defaults to `0`, preserving the previous unconstrained-wrapping behavior. Positive values are persisted through JSON document serialization and used for DXF import/export; on-canvas wrapping remains a future UI/rendering step.

DXF export now writes group code `41` for `MTEXT`, and DXF import reads group code `41` when available. Missing or non-positive imported widths are treated as `0`. Added focused Core, Export and Persistence tests for reference width validation, DXF group `41` output/import and native round-trip preservation.


## 2026-05-17 - S7 preview provider drawing-tool migration

Continued the `CadToolPreviewRenderer` refactor by migrating the remaining entity-based drawing previews to `IToolPreviewEntityProvider`.

Newly migrated drawing tools:

- `RectangleTool`;
- `RectangleBySidesTool`;
- `CircleTool`;
- `ArcTool`;
- `ArcThreePointsTool`;
- `EllipseTool`;
- `PolylineTool`;
- `PolygonTool`;
- `SplineTool`.

`CadToolPreviewRenderer` no longer needs switch cases for these basic drawing previews. It still keeps fallback cases for modify tools with highlighted previews, measurement overlays, selection/zoom windows, mirror-axis overlays and grip editing. Existing measurement helper overlays for two-point tools and arc tools remain in the renderer so dynamic distance/radius guides are unchanged.

Expanded `ToolPreviewEntityProviderTests` to cover the migrated drawing tools and verify that representative previews are still exposed as normal `CadEntity` instances. Future cleanup can migrate context-aware modify tools (`CopyTool`, `RotateTool`, `ScaleTool`, `MirrorTool`, `OffsetTool`) and then remove now-unused private drawing-preview methods from `CadToolPreviewRenderer` after manual UI validation.


## 2026-05-17 - S7.2 preview provider modify-tool migration

Continued the `CadToolPreviewRenderer` refactor after the drawing-tool migration by moving additional entity-based modify previews to `IToolPreviewEntityProvider`.

Newly migrated modify tools:

- `CopyTool`;
- `RotateTool`;
- `ScaleTool`;
- `AlignTool`;
- `BreakAtPointTool`;
- `BreakBetweenPointsTool`;
- `FilletTool`.

`CadToolPreviewRenderer` now delegates these previews through the shared provider protocol before falling back to custom drawing. The fallback switch is reduced to tools that still need additional overlays or non-entity drawing: `ExtendTool`, `TrimTool`, `OffsetTool`, `MirrorTool`, measurement tools, selection/zoom windows and grip editing.

Expanded `ToolPreviewEntityProviderTests` with a modify-tool protocol check. Future cleanup can either introduce a richer preview descriptor for highlighted/custom overlays or keep those tools in the fallback until their drawing requirements are generalized.

## 2026-05-17 - S7.3 preview provider offset and distance migration

Continued the incremental `CadToolPreviewRenderer` cleanup by migrating two more entity-only previews to `IToolPreviewEntityProvider`:

- `OffsetTool`, which now exposes its current offset preview entity through the shared provider protocol;
- `MeasureDistanceTool`, which now exposes its transient measured segment as a preview entity.

Removed the corresponding fallback switch cases and private drawing methods from `CadToolPreviewRenderer`. The renderer still keeps concrete fallback cases for tools that need extra custom drawing beyond plain preview entities: `ExtendTool` and `TrimTool` need highlighted preview fragments, `MirrorTool` needs the mirror-axis overlay, `MeasureAngleTool` needs angle-point markers, selection/zoom tools need filled screen windows, and `GripEditTool` needs grip-marker overlays.

Expanded `ToolPreviewEntityProviderTests` so the protocol check includes `OffsetTool` and `MeasureDistanceTool`, with a focused distance-preview assertion. The next architectural step should not force these remaining custom-overlay tools into the current entity-only interface; instead, introduce a richer preview descriptor/model if we want to remove the final fallback dispatch cleanly.

## 2026-05-17 - S7.4 ToolPreviewDescriptor foundation

Introduced the richer tool preview descriptor protocol so previews are no longer limited to plain transient `CadEntity` lists.

New tools-layer preview model types:

- `IToolPreviewDescriptorProvider`;
- `ToolPreviewDescriptor`;
- `ToolPreviewLine` / `ToolPreviewLineKind`;
- `ToolPreviewMarker` / `ToolPreviewMarkerKind`;
- `ToolPreviewWindow` / `ToolPreviewWindowKind`.

The descriptor model is UI-agnostic and stores model-space geometry plus semantic overlay items. `CadToolPreviewRenderer` now tries `IToolPreviewDescriptorProvider` first, then `IToolPreviewEntityProvider`, then the remaining legacy fallback switch.

Migrated `MirrorTool` to descriptor previews so mirrored entities, mirror-axis line and axis endpoint markers are provided by the tool instead of drawn by a concrete renderer case. Migrated `MeasureAngleTool` to descriptor previews so measurement rays and angle point markers are also tool-provided. Removed the old concrete renderer cases and private drawing methods for those two tools.

Expanded `ToolPreviewEntityProviderTests` with descriptor-specific coverage for mirror-axis overlays, angle markers and descriptor collection storage. Remaining fallback tools can now be migrated incrementally by expressing their overlays as descriptor items instead of adding new renderer-specific branches.



## 2026-05-17 - S7.5 ToolPreviewDescriptor window migration

Continued the `ToolPreviewDescriptor` migration by moving filled window overlays out of the concrete renderer fallback.

Migrated tools:

- `SelectionTool`, which now implements `IToolPreviewDescriptorProvider` and emits a `ToolPreviewWindow` with `ToolPreviewWindowKind.Selection` while a drag window is active;
- `ZoomWindowTool`, which now implements `IToolPreviewDescriptorProvider` and emits a `ToolPreviewWindow` with `ToolPreviewWindowKind.Zoom` while the zoom rectangle is active.

`CadToolPreviewRenderer` already knew how to render descriptor windows, so the concrete `SelectionTool` and `ZoomWindowTool` fallback cases and their private drawing methods were removed. The remaining fallback switch is now limited to `ExtendTool`, `TrimTool` and `GripEditTool`.

Expanded `ToolPreviewEntityProviderTests` with descriptor checks for selection and zoom windows. This keeps the migration incremental while reducing concrete tool knowledge in the app renderer.

## 2026-05-17 - S7.6 ToolPreviewDescriptor grip-edit migration

Continued the `ToolPreviewDescriptor` migration by moving `GripEditTool` out of the concrete `CadToolPreviewRenderer` fallback.

Changes made:

- `GripEditTool` now implements `IToolPreviewDescriptorProvider`.
- The descriptor emitted by `GripEditTool` contains:
  - the transient replacement entity while a grip is being dragged;
  - the base-to-destination measurement guide line;
  - primary/secondary point markers for the active grip move;
  - cold/hot/warm grip markers for all active grips.
- `ToolPreviewMarker` now has a `Shape` field through `ToolPreviewMarkerShape` so app rendering can distinguish square grip markers from circular insert-vertex markers without the app depending on `GripKind`.
- `ToolPreviewMarkerKind` now includes `GripCold`, `GripHot` and `GripWarm` semantic states.
- `CadToolPreviewRenderer` renders grip markers from descriptors and no longer switches on `GripEditTool`.

The remaining concrete fallback switch in `CadToolPreviewRenderer` is now limited to `ExtendTool` and `TrimTool`, because they still expose two preview collections: normal preview entities and highlighted fragments. A next cleanup pass can migrate them by returning normal entities through `ToolPreviewDescriptor.Entities` and highlighted fragments through `ToolPreviewDescriptor.HighlightedEntities`.


## 2026-05-17 - S7.7 ToolPreviewDescriptor highlighted-fragment migration

Completed the remaining concrete fallback cleanup in `CadToolPreviewRenderer` by migrating the highlighted-fragment modify tools to `IToolPreviewDescriptorProvider`.

Migrated tools:

- `ExtendTool`, which now emits its extended replacement entity through `ToolPreviewDescriptor.Entities` and the newly added extension segment through `ToolPreviewDescriptor.HighlightedEntities`;
- `TrimTool`, which now emits the kept trim result through `ToolPreviewDescriptor.Entities` and the removed fragment through `ToolPreviewDescriptor.HighlightedEntities`.

`CadToolPreviewRenderer` now resolves active tool previews through the tool-provided protocols only: descriptor provider first, entity provider second. The old concrete fallback switch for `ExtendTool` and `TrimTool` was removed. This means adding a new preview-capable tool should no longer require adding a concrete tool case to the app renderer; the tool should implement either `IToolPreviewEntityProvider` or `IToolPreviewDescriptorProvider`.

Expanded `ToolPreviewEntityProviderTests` with focused descriptor tests for `ExtendTool` and `TrimTool`, including highlighted fragments. The app renderer still contains some measurement-overlay helpers for shared base classes/special tool states, but the former active-tool fallback dispatch has been eliminated.

## 2026-05-17 - S10 multiline text property editing

Added basic property-panel editing support for `MultilineTextEntity` so MTEXT annotations are no longer read-only after insertion.

Editable MTEXT properties now include:

- text value;
- insertion X/Y;
- rotation;
- text format id;
- reference width.

The update path uses the same `ReplaceEntitiesCommand` workflow as other property-panel edits, so edits participate in undo/redo and keep the selected entity id active after replacement. Empty multiline text values are rejected, invalid numeric values use the shared numeric validation message, and negative reference widths are rejected because `ReferenceWidth = 0` is the supported unconstrained-wrapping value.

Expanded `PropertyPanelEditingTests` with multiline text value, text format and reference width editing coverage.

## 2026-05-17 - Documentation sync after S10

Updated the active documentation set after the S1-S10 stabilization passes. The docs now reflect that:

- deleting model geometry marks dimensions stale;
- DXF `SPLINE` export writes knot-vector data;
- the representative DXF compatibility samples were manually checked, while exact viewer/version recording remains a future audit task;
- ellipse/spline intersection snaps use sampled approximations;
- active tool previews are provided through `IToolPreviewEntityProvider` and `IToolPreviewDescriptorProvider`, with the old concrete active-tool fallback dispatch removed from `CadToolPreviewRenderer`;
- application logging captures tool/UI exceptions;
- MTEXT reference width is persisted and imported/exported through DXF group `41`;
- MTEXT value, insertion, rotation, text format and reference width are editable from the property panel.

Touched docs include `README.md`, `docs/roadmap.md`, `docs/stabilization-v0.9-plan.md`, `docs/dxf-compatibility.md`, `docs/known-limitations.md` and this handoff.



## 2026-05-18 - v0.9 planning kickoff / Phase 0 documentation alignment

Opened the v0.9 release-candidate planning track.

The active direction is stabilization rather than another feature-heavy release. v0.9 should focus on local application/session settings, undo/redo audit, export workflow hardening, exact DXF compatibility audit records, safe performance review, documentation completion and release packaging.

Documentation changes made in this pass:

- `docs/roadmap.md` now separates completed v0.8.x work from the active v0.9 release-candidate checklist.
- The old secondary v0.8 backlog was re-triaged so already-completed items such as Z-order, draw-order tests, document-level drafting settings, polyline offset, favicon/logo support and Line-Line Fillet improvements are not planned twice.
- The post-v1.0 backlog no longer lists ellipses and splines as future features because native entity/tool support already exists.
- `docs/stabilization-v0.9-plan.md` was rewritten as the working v0.9 plan with phases 0-7.
- `docs/known-limitations.md` now distinguishes persisted document-level drafting settings from the still-planned local application/session settings layer.
- `README.md` links the v0.9 stabilization plan in the documentation table.

Recommended next implementation step:

1. Start v0.9 Phase 1 with a small local settings service.
2. Keep it separate from `.opencad2d.json` drawing persistence.
3. Cover missing/partial/corrupt settings fallback with tests before wiring it into the UI.

Suggested first files for Phase 1:

- `src/OpenCad2D.App/Settings/LocalApplicationSettings.cs`
- `src/OpenCad2D.App/Settings/ILocalApplicationSettingsStore.cs`
- `src/OpenCad2D.App/Settings/JsonLocalApplicationSettingsStore.cs`
- `tests/OpenCad2D.App.Tests/LocalApplicationSettingsStoreTests.cs`
- `docs/application-settings.md`
- `docs/ai-handoff.md`

## 2026-05-18 - v0.9 Phase 1 local settings layer

Implemented the first local application/session settings layer in `OpenCad2D.App.Settings`. The new model is intentionally small and separate from `.opencad2d.json`:

- `ApplicationSettings` stores schema version, last opened file path, last open directory, last save directory, last export directory and recent native drawing files;
- `IApplicationSettingsStore` abstracts loading and saving;
- `JsonApplicationSettingsStore` persists JSON to `%APPDATA%/OpenCad2D/settings.json` on Windows, with AppContext fallback if the application data path is unavailable;
- missing, empty, partial, invalid, unreadable or unauthorized settings load as safe defaults;
- `MainWindowViewModel` updates the local settings after native open/save and after SVG/DXF/PDF export;
- local settings save failures are swallowed so they never block drawing workflows.

Tests added:

- `ApplicationSettingsTests`;
- `JsonApplicationSettingsStoreTests`.

Important boundary: local settings are app/user preferences and metadata only. Drawing state continues to live in `.opencad2d.json`. Window size/position, panel widths, theme and shortcut persistence remain deferred.

Validation note: this environment did not have the `dotnet` command available, so build/tests must be run locally with `dotnet build` and `dotnet test`.


## 2026-05-19 - Toolbar StreamGeometry icons

Added vector icons to the main OpenCad2D command buttons.

Implemented files:

- `src/OpenCad2D.App/Resources/Icons.axaml`: dedicated resource dictionary containing `StreamGeometry` entries generated from the supplied SVG icon paths;
- `src/OpenCad2D.App/App.axaml`: loads the icon resource dictionary through application resources;
- `src/OpenCad2D.App/MainWindow.axaml`: replaces plain text-only button content with left-aligned icon+text grids for file commands, top-bar commands and the left tool panel.

Important implementation notes:

- the icons are outline SVG paths, so buttons render them with `Path` and `Stroke` instead of `PathIcon` fill rendering;
- the existing commands, names, click handlers and tooltips were preserved;
- icon/text alignment is controlled by shared XAML classes: `icon-button-content`, `icon-button-path` and `icon-button-text`;
- toolbar icons use the same yellow accent as the snap marker (`#FFE650`) with a `StrokeThickness` of `1.5` for a lighter outline;
- active tool buttons keep the yellow icon stroke so the symbol language remains consistent with snap markers.

Validation note: this environment did not have the `dotnet` command available, so build/tests must be run locally with `dotnet build` and `dotnet test`.


### Third-party icon licensing

Toolbar icons are derived from Tabler Icons SVG assets and converted to Avalonia `StreamGeometry` resources. Tabler Icons is licensed under the MIT License. Keep `LICENSES/Tabler-Icons-MIT.txt` and `docs/third-party-notices.md` with the repository/distribution when these icon resources are included.


## 2026-05-20 - Curve editing precision design lock

Prepared the documentation foundation for the next Trim/Break stabilization pass before continuing the broader roadmap.

New document:

- `docs/curve-editing.md`

The new architecture rule is now explicit:

```text
CAD editing operations modify native entities using native geometric parameters.
Sampling is allowed only as temporary support and must not become the permanent source of edited geometry when a native representation exists.
Shared intersections used by multiple explicit-vertex entities must reuse the same cut point to avoid micro-gaps and nearly coincident vertices.
```

Documentation updates:

- `README.md` links the new curve-editing architecture document.
- `docs/architecture.md` now includes the curve-editing precision boundary.
- `docs/modify-tools.md` distinguishes current Trim/Break behavior from the target native-parameter pipeline.
- `docs/known-limitations.md` now calls out current ellipse/spline approximation limits and the target native preservation policy.
- `docs/roadmap.md` adds a curve-editing precision gate before broader v0.9/v1.0 work.
- `docs/stabilization-v0.9-plan.md` records that the Trim/Break precision work may temporarily interrupt the original v0.9 sequence because it protects CAD correctness.

Planned implementation sequence:

1. Introduce `CurveCut`, `CurveInterval`, `ICurveAdapter`, `ICurveAdapterFactory` and `CadCurveSplitService`.
2. Implement adapters for `LineEntity`, `CircleEntity`, `ArcEntity` and `PolylineEntity`.
3. Rebuild Break Point and Break Segment on top of `CadCurveSplitService`.
4. Rebuild Trim target fragmentation on the same pipeline and stabilize multi-boundary Circle/Arc Trim.
5. Add precision tests such as `TrimTwoLinesMutually_ShouldShareExactEndpoint`, `TrimLineWithArc_ShouldUseSharedIntersectionPointForLineEndpoint`, `TrimArcWithLine_ShouldKeepArcEndpointOnCircleAndNearSharedPoint` and `BreakPolylineAtIntersection_ShouldInsertSharedIntersectionVertex`.
6. Add `EllipticalArcEntity` so ellipse Trim/Break can return native elliptical arc fragments.
7. Add native Bezier splitting so supported spline Trim/Break operations can preserve spline fragments instead of returning polyline approximations.

Important implementation constraint:

- For explicit-vertex output (`LineEntity`, `PolylineEntity`, rectangle/polygon converted to open polylines), use `CurveCut.Point` directly as the endpoint or inserted vertex.
- For parametric output (`CircleEntity` -> `ArcEntity`, `ArcEntity`, future `EllipticalArcEntity`, future native spline fragments), use `CurveCut.Parameter` to rebuild the native curve and validate the rebuilt endpoint against `CurveCut.Point` within tolerance.

---

## Curve editing stabilization phase started

Initial implementation work has started for the shared curve editing architecture described in `docs/curve-editing.md`.

Added the first native curve splitting pipeline in `OpenCad2D.Core.Editing.Curves`:

- `CurveCut` stores a native curve parameter plus the shared geometric point for a cut.
- `CurveInterval` stores kept intervals between two cuts.
- `ICurveAdapter` defines the native parametric interface used by editing operations.
- `ICurveAdapterFactory` and `DefaultCurveAdapterFactory` currently support `LineEntity`, `CircleEntity` and `ArcEntity`.
- `CadCurveSplitService` provides the first shared operations for splitting at a point, removing between two points and removing the picked interval between cuts.

`CadTrimService` now routes `CircleEntity` and `ArcEntity` trimming through `CadCurveSplitService`, removing the previous early exit for multiple boundaries on those entity types. This is the first concrete step toward shared TRIM/BREAK behavior.

New tests were added for:

- direct `CadCurveSplitService` behavior on line, circle and arc;
- circle TRIM with two line boundaries;
- arc TRIM with two line boundaries.

Current scope notes:

- The new curve split pipeline is deliberately limited to line, circle and arc in this phase.
- Existing polyline, ellipse and spline behavior remains unchanged.
- Ellipse and spline still require the planned native follow-up work (`EllipticalArcEntity` and native Bezier split) before their permanent polyline fallback can be removed.
- The local environment used for this handoff did not provide the `dotnet` CLI, so compilation and tests must be run by the developer locally.

---

## Curve editing stabilization - Polyline adapter phase

Extended the shared curve editing pipeline with `PolylineEntity` support in `DefaultCurveAdapterFactory`.

New behavior in the adapter-backed pipeline:

- open polylines use cumulative path length as their native curve parameter;
- closed polylines/polygons use the same cumulative path length with `Period = TotalLength`;
- inserted cut points are reused directly as explicit vertices, preserving shared `Point2D` values;
- fragments keep intermediate native polyline vertices instead of degrading into one two-point polyline per segment;
- closed polyline/polygon edits return open `PolylineEntity` fragments.

`CadTrimService.TrimPolylineByBoundaries` now delegates polyline target fragmentation to `CadCurveSplitService`. This means rectangle/polygon-like closed polylines start using the same interval pipeline already introduced for circle and arc trimming.

Tests added/updated:

- `CadCurveSplitServiceTests.SplitAtPoint_WithOpenPolyline_ShouldCreateTwoPolylineFragmentsSharingProjectedVertex`
- `CadCurveSplitServiceTests.RemoveBetweenPoints_WithOpenPolyline_ShouldRemoveMiddlePathAndPreserveNativeVertices`
- `CadCurveSplitServiceTests.RemoveBetweenPoints_WithClosedPolylinePolygon_ShouldReturnOpenPolylineAroundRemainingPath`
- `CadTrimServiceTests.TrimOpenPolyline_ByLineBoundaryOnSecondSegment_ShouldCreatePolylineFragments` now expects a single continuous polyline fragment that preserves the corner vertex, instead of two disconnected two-point fragments.

Validation note: this environment still does not provide the `dotnet` CLI, so run the full local validation with:

```bash
dotnet build
dotnet test
```

---

## Curve editing stabilization - Break service delegation phase

`CadBreakService` now delegates base native entities to `CadCurveSplitService` instead of maintaining separate break fragmentation logic for the same cases.

Delegated cases:

- `BreakAtPoint`: `LineEntity`, `ArcEntity`, `PolylineEntity`
- `BreakBetweenPoints`: `LineEntity`, `CircleEntity`, `ArcEntity`, `PolylineEntity`

The intentional exception remains one-point break on a full `CircleEntity`, which still returns no fragments. The supported circle workflow is `BreakBetweenPoints`, returning a native `ArcEntity` complement. This avoids introducing a near-360-degree arc representation before that behavior is deliberately designed.

Temporary fallback cases kept unchanged:

- `EllipseEntity` still returns open `PolylineEntity` approximations.
- `BezierSplineEntity` still breaks its polyline approximation.

New tests added to lock the shared point/projection behavior through `CadBreakService`:

- `CadBreakServiceTests.BreakAtPoint_WithLine_ShouldCreateTwoLinesSharingProjectedPoint`
- `CadBreakServiceTests.BreakBetweenPoints_WithLine_ShouldRemoveMiddleSegmentUsingProjectedPoints`

Validation note: this handoff environment still does not provide the `dotnet` CLI. Run locally:

```bash
dotnet build
dotnet test
```

---

## Curve editing stabilization - Line trim delegation phase

`CadTrimService.TrimLineByBoundaries` now delegates line target fragmentation to `CadCurveSplitService`.

This removes the remaining separate line-specific trim fragmentation path for the native base entities and aligns `LineEntity` with the same `CurveCut` / `CurveInterval` pipeline already used by `CircleEntity`, `ArcEntity`, `PolylineEntity`, and `CadBreakService`.

Important precision rule preserved by this phase:

- line fragments use the projected/shared `CurveCut.Point` directly as their resulting endpoint;
- mutual line trims therefore produce exactly matching endpoint coordinates when they come from the same geometric intersection;
- tolerances are still used to classify cuts, filter endpoints, and remove degenerate fragments, but not to justify keeping intended coincident vertices as different coordinates.

New tests added:

- `CadTrimServiceTests.TrimLine_ByBoundary_ShouldReuseSharedIntersectionPointAsEndpoint`
- `CadTrimServiceTests.TrimTwoLinesMutually_ShouldCreateExactlyMatchingSharedEndpoint`

Validation note: this handoff environment still does not provide the `dotnet` CLI. Run locally:

```bash
dotnet build
dotnet test
```

---

## Curve editing stabilization - EllipticalArcEntity foundation phase

Added the first native model object required to remove permanent ellipse degradation during future Trim/Break operations:

- `EllipticalArcEntity`
- `EntityKind.EllipticalArc`

The entity uses the same native ellipse definition as `EllipseEntity`:

- `Center`
- `MajorAxis`
- `MinorRadius`
- `StartParameterRadians`
- `EndParameterRadians`
- `IsCounterClockwise`

Superseded status: this phase was foundational only at the time it was written. The later phases have since added rendering, persistence, export support, `EllipseCurveAdapter`, `EllipticalArcCurveAdapter`, and TRIM/BREAK wiring for native ellipse fragments. The former `EllipseEntity -> PolylineEntity` editing fallback has been removed for supported ellipse edits.

Core tests added:

- `EllipticalArcEntityTests.Constructor_ShouldPreserveNativeEllipseDefinitionAndParameters`
- `EllipticalArcEntityTests.GetSamplePoints_ShouldFollowDirectedSweepAndIncludeEndpoints`
- `EllipticalArcEntityTests.WithLayer_ShouldPreserveGeometry`

Validation note: this handoff environment still does not provide the `dotnet` CLI. Run locally:

```bash
dotnet build
dotnet test
```

---

## Curve editing stabilization - EllipticalArc infrastructure phase

`EllipticalArcEntity` is now wired into the application infrastructure so future native ellipse Trim/Break results can be displayed, saved and exported before the command services start producing them.

Added support:

- screen rendering in `CadEntityRenderer` using the entity's directed sample points;
- JSON persistence with `EllipticalArcEntityDto` and the `EllipticalArc` type discriminator;
- serializer/deserializer mapping in `JsonDocumentSerializer`;
- SVG export as a native `<path>` elliptical arc command;
- DXF export as a partial `ELLIPSE` entity using group codes `41` and `42` for the start/end parameters;
- PDF export using the current sampled-line strategy, matching the existing ellipse/spline export approach.

New tests added:

- `EllipticalArcRoundTripTests.SerializeDeserialize_ShouldPreserveEllipticalArcEntity`
- `EllipticalArcRoundTripTests.JsonRoundTrip_ShouldPreserveEllipticalArcDtoType`
- `SvgExporterTests.Export_WhenDocumentContainsEllipticalArc_ShouldWritePathElement`
- `DxfExporterTests.Export_WhenDocumentContainsEllipticalArc_ShouldWritePartialEllipseEntity`

Important limitation that still remains by design:

- Superseded: `CadTrimService` and `CadBreakService` now return `EllipticalArcEntity` for supported ellipse editing. The former `EllipseEntity -> PolylineEntity` editing fallback has been removed.

Validation note: this handoff environment still does not provide the `dotnet` CLI. Run locally:

```bash
dotnet build
dotnet test
```


---

## Curve editing stabilization - EllipticalArc consolidation tests

Added focused precision tests for native ellipse editing results. The new tests verify that Trim and Break on `EllipseEntity` / `EllipticalArcEntity` keep native geometry rather than returning permanent `PolylineEntity` approximations.

New test file:

- `tests/OpenCad2D.Core.Tests/EllipticalArcEditingPrecisionTests.cs`

Covered scenarios:

- full ellipse Trim with two line boundaries returns native `EllipticalArcEntity` fragments;
- full ellipse Trim endpoints lie on both the source ellipse and the vertical line boundaries;
- `EllipticalArcEntity` Trim by line keeps native endpoint geometry;
- `EllipticalArcEntity` Break At Point creates two native fragments sharing the break point within tolerance;
- `EllipticalArcEntity` Break Between Points removes the middle segment while preserving center, major axis and minor radius.

This phase does not add new spline behavior. The next planned phase remains improving native/non-degrading intersections for `EllipseEntity` and `EllipticalArcEntity` with `LineEntity` and `PolylineEntity`, followed by `BezierSplineSplitService`.

Validation note: this handoff environment still does not provide the `dotnet` CLI. Run locally:

```bash
dotnet build
dotnet test
```

---

## Curve editing stabilization - Ellipse/Circle shared intersections

Improved native intersection handling for ellipse-based entities against circular entities. The goal is to avoid the manual issue where trimming an ellipse with a circle produced endpoints that were visibly separated from the circle by a large amount because the operation fell back to independent sampled approximations.

Updated behavior:

- `CircleEntity <-> EllipseEntity` now uses a native ellipse-parameter root search against the circle equation;
- `CircleEntity <-> EllipticalArcEntity` uses the same native calculation and filters results by the elliptical arc sweep;
- `ArcEntity <-> EllipseEntity` filters native circle/ellipse intersections by the circular arc sweep;
- `ArcEntity <-> EllipticalArcEntity` filters by both the circular arc sweep and the elliptical arc sweep.

The returned point is produced from the ellipse parameter and validated against the circle radius. This gives both the ellipse adapter and the circle/arc adapter the same shared geometric cut point, preventing large mismatches after Trim.

New precision tests were added to `EllipticalArcEditingPrecisionTests` for:

- `IntersectCircleEllipse_ShouldReturnPointsOnBothNativeCurves`;
- `TrimEllipse_ByCircleBoundary_ShouldKeepEndpointsOnCircleAndEllipse`;
- `TrimCircle_ByEllipseBoundary_ShouldKeepEndpointsOnCircleAndEllipse`.

Validation note: this handoff environment still does not provide the `dotnet` CLI. Run locally:

```bash
dotnet build
dotnet test
```


## 2026-05-20 - BezierSplineCurveAdapter foundation

The shared curve editing pipeline now has a `BezierSplineCurveAdapter` for open `BezierSplineEntity`.

Implemented behavior:

- `DefaultCurveAdapterFactory` creates a spline adapter for `BezierSplineEntity`;
- open spline split/remove operations use native Bezier parameters `0..1`;
- fragments are rebuilt through `BezierSplineSplitService`, preserving `BezierSplineEntity`;
- closed splines remain deliberately deferred and return no fragments;
- new `CadCurveSplitServiceTests` verify native split, remove-between and remove-picked behavior for splines.

Important limitation:

- Superseded: TRIM/BREAK services now route supported open spline editing through `CadCurveSplitService` and `BezierSplineSplitService`; the command-level permanent `PolylineEntity` fallback for supported open splines has been removed.

## 2026-05-20 - CadIntersectionPoint rich intersection foundation

Added the first implementation layer for richer CAD intersections, without replacing the existing `Intersect(...)` API yet.

New types:

- `CadIntersectionPoint` in `OpenCad2D.Core.Editing`;
- `CadIntersectionKind` in `OpenCad2D.Core.Editing`.

`CadIntersectionPoint` stores:

- one shared `Point2D` that explicit-vertex entities can reuse directly;
- `FirstParameter` on the first entity;
- `SecondParameter` on the second entity;
- an intersection kind classification;
- convenience `FirstCut` and `SecondCut` values for the shared split pipeline.

`CadEntityIntersectionService.IntersectDetailed(...)` now wraps the existing intersection points and projects them onto both entities through `DefaultCurveAdapterFactory`. This preserves compatibility while giving future TRIM/BREAK/EXTEND work access to native curve parameters and a single shared point.

New tests in `CadEntityIntersectionDetailedTests` verify:

- line/line intersections return a single shared point plus native parameters;
- endpoint intersections are classified as `Endpoint`;
- line/circle intersections expose both line parameters and circle angular parameters;
- circle/ellipse intersections reuse the same point for both `CurveCut` values and keep points on both native curves.

This phase is intentionally additive. The existing `CadTrimService` and `CadBreakService` still call their current APIs. The next refactor step can progressively replace target-side point projection with `CadIntersectionPoint.FirstCut` / `SecondCut` where the command already knows which entity is the target.

Validation note: this handoff environment still does not provide the `dotnet` CLI. Run locally:

```bash
dotnet build
dotnet test
```

## Curve editing stabilization - EXTEND native elliptical arc phase

Extended `CadExtendService` to support `EllipticalArcEntity` targets.

Behavior added:

```text
EllipticalArcEntity + boundary -> EllipticalArcEntity
```

The service discovers candidate intersections using the full ellipse definition, chooses the nearest valid intersection outside the current elliptical-arc sweep in the picked extension direction, converts that point to the native ellipse parameter, and rebuilds the result as an `EllipticalArcEntity`.

`ExtendTool` now accepts `EllipticalArcEntity` as a target and creates highlighted preview fragments for the newly added elliptical-arc portion.

Regression tests added:

```text
ExtendEllipticalArc_ToLineBoundary_ShouldExtendPickedEndWithNativeGeometry
ExtendEllipticalArc_ToLineBoundary_ShouldExtendPickedStartWithNativeGeometry
ExtendEllipse_ShouldReturnNull
```

Full `EllipseEntity` targets remain unsupported for EXTEND because closed curves have no natural extension endpoint. `BezierSplineEntity` EXTEND remains deferred.


## 2026-05-20 - Permanent Polyline fallback cleanup for curve editing

Cleaned up the old command-level TRIM/BREAK fallback implementations that permanently converted native curve edits into `PolylineEntity` results.

Changed files:

```text
src/OpenCad2D.Core/Editing/CadTrimService.cs
src/OpenCad2D.Core/Editing/CadBreakService.cs
docs/curve-editing.md
docs/ai-handoff.md
```

`CadTrimService` is now a thin dispatcher over the shared curve-editing pipeline for supported targets: line, circle, arc, ellipse, elliptical arc, polyline and open Bezier spline. It collects boundary intersections, filters endpoints where needed, and delegates final interval removal to `CadCurveSplitService`. The obsolete private fragment builders for line, circle, arc, ellipse-as-polyline and polyline-as-two-point-fragments were removed.

`CadBreakService` is now similarly reduced to the public Break At Point / Break Between Points dispatch. It delegates supported open/native entities to `CadCurveSplitService`; full `CircleEntity` and `EllipseEntity` still intentionally return no result for one-point break until a full-sweep-open-arc policy is defined.

Current policy after cleanup:

- source polylines, rectangles and polygons may legitimately produce `PolylineEntity` fragments;
- full ellipses and elliptical arcs produce `EllipticalArcEntity` fragments;
- supported open Bezier splines produce `BezierSplineEntity` fragments;
- unsupported closed splines are deferred/no-op rather than silently degraded;
- sampled geometry remains allowed only for preview/discovery/projection, not as the permanent edited result when a native representation exists.

Validation note: this handoff environment still does not provide the `dotnet` CLI. Run locally:

```bash
dotnet build
dotnet test
```

## 2026-05-20 - Save versus Export UX clarity

Clarified the UX distinction between native Save and external Export.

Changed files:

```text
src/OpenCad2D.App/ViewModels/MainWindowViewModel.cs
tests/OpenCad2D.App.Tests/MainWindowViewModelExportSaveSemanticsTests.cs
docs/export.md
docs/architecture.md
docs/ai-handoff.md
```

`ExportSvgToFile`, `ExportDxfToFile` and `ExportPdfToFile` still do not update `CurrentFilePath`, do not call `MarkSaved()` and do not clear the dirty state. This remains the correct data-integrity policy because SVG/PDF/DXF are derived outputs, not the editable native OpenCad2D project.

The post-export status message now explicitly says that the export did not save the editable OpenCad2D project. It distinguishes three cases:

- no native file path yet: tell the user to use Save As;
- native file exists but the drawing is dirty: tell the user unsaved project changes remain and to use Save;
- native file exists and the drawing is clean: tell the user the native drawing is already saved.

Added App tests proving SVG/DXF/PDF export do not change `CurrentFilePath` and do not clear `IsDirty`, plus coverage for the never-saved and already-saved message variants.

Validation note: this handoff environment still does not provide the `dotnet` CLI. Run locally:

```bash
dotnet build
dotnet test
```
