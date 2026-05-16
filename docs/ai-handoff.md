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
Polyline open   -> <polyline>
Polyline closed -> <polygon>
ArcEntity       -> <path>
PointEntity     -> marker
TextEntity      -> <text>
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

The left SELECT tool group includes buttons for Select, Select All and Select Last. Both actions replace the current selection and do not modify the document dirty state.

Layer rules:

- hidden-layer entities are skipped;
- locked-layer entities are skipped;
- Select Last searches backwards through document insertion order and chooses the newest selectable entity.

Regression coverage lives in `CadActionControllerTests` and `MainWindowViewModelCommandLineTests`.


### Nullable warning fix after Select All / Select Last

- `MainWindowViewModel.TryExecuteActionCommand` now uses a non-null `ToolResult` out parameter.
- The default unmatched-command result is initialized with `ToolResult.None()` so `SubmitCommandInput` can return it without CS8603 nullable warnings.
- No behavior change was intended.
