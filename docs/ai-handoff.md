# Latest handoff note

## v0.8.x final documentation and release consolidation

The v0.8.x baseline is now ready for final local validation and GitHub release preparation. Polygon, Ellipse, MTEXT and Bezier Spline are complete in the current baseline, including command aliases, rendering/preview, persistence, export coverage and focused tests.

Important implementation notes for future work:

- regular polygons are stored as closed `PolylineEntity` instances;
- ellipse partial edit results currently become open `PolylineEntity` approximations because there is no `EllipseArcEntity`;
- Bezier spline Trim/Break/Offset workflows use sampled polyline approximation, so edited fragments currently become `PolylineEntity` results;
- DXF import supports `MTEXT`, but native DXF `ELLIPSE` and `SPLINE` import is still deferred;
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
- selected entities can be assigned to the current layer with the `Assegna` top-bar button.

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

Export/persistence status: JSON round-trip includes `BezierSplineEntityDto`; SVG/PDF export use sampled polyline/polygon approximations; DXF export writes a `SPLINE` entity with control points. DXF import is still deferred.

Modify support: Trim, Break Point, Break Segment and Offset accept splines by converting them to sampled `PolylineEntity` geometry. Result fragments are currently polylines, not partial spline entities. This is intentional for phase 1 and avoids introducing a partial spline model before the core curve behavior stabilizes.
