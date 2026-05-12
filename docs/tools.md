# Tools

Tools are UI-independent CAD interaction objects. They live in `OpenCad2D.Tools` and receive input through the tool pipeline.

A tool should not depend on Avalonia controls, windows or drawing APIs.

---

## Tool pipeline

Typical flow:

```text
Avalonia input
-> CadCanvas / MainWindow
-> CadWorkspace
-> ToolController
-> active ICadTool
-> command creation/execution when needed
```

The App converts UI events into CAD input and forwards them. The tool owns command-specific interaction state.

---

## ToolContext

`ToolContext` provides shared runtime services and state:

- document;
- command history;
- selection set;
- selection service;
- snap service;
- grid settings;
- current layer;
- UCS/WCS conversion;
- current base point;
- Ortho state;
- Polar Tracking settings;
- tolerances.

Tools should use `ToolContext` instead of reaching into UI code.

---

## Command line input

The command line supports point input:

```text
100,50   absolute UCS point
@50,0    relative UCS offset
5        direct distance entry
```

The command line resolves typed input to a point and submits it to the active tool as if it came from the mouse.

Special command keys may be interpreted by active tools. Examples:

```text
PolylineTool: Enter finishes, C closes
AlignTool:    Enter/N confirms without scale, Y confirms with scale
MoveTool:     Enter confirms the entity-selection phase and asks for base point
```

---

## Drawing tools

### PointTool

Creates a `PointEntity` from one picked point.

Supports:

- mouse point;
- snap;
- current layer;
- undo/redo through `AddEntityCommand`.

### TextTool

Creates a single-line `TextEntity`.

Workflow:

```text
activate Text
pick insertion point
enter text in the async text input dialog
choose text format
set optional rotation
OK -> AddEntityCommand
Cancel -> no entity is created
```

`TextTool` stays UI-independent. It depends on `ITextInputProvider`; the Avalonia app provides the actual dialog implementation.

The created entity stores only `Text`, `InsertionPoint`, `RotationDegrees` and `TextFormatId`. Font, height, color, bold and italic come from the document-level `TextFormat`.

### LineTool

Creates a line from two points.

Supports:

- mouse points;
- typed absolute/relative coordinates;
- direct distance;
- snap;
- Ortho;
- Polar Tracking;
- preview;
- undo/redo through `AddEntityCommand`.

### RectangleTool

Creates a closed `PolylineEntity` from two opposite corners.

### RectangleBySidesTool

Creates a closed rectangular `PolylineEntity` from:

```text
first corner
endpoint of first side
point defining the second side
```

The third point is projected perpendicular to the first side so the result remains a true rectangle. Polar Tracking applies to the first side.

### CircleTool

Creates a `CircleEntity` from center and radius point.

The radius can come from:

- a second mouse point;
- snap;
- command line direct distance.

### ArcTool

Creates an `ArcEntity` from:

```text
center point
start point / radius
end point / direction
```

The tool shows construction feedback from the center and commits through `AddEntityCommand`.

### ArcThreePointsTool

Creates an `ArcEntity` through three points:

```text
start point
point on arc
end point
```

The geometric calculation is performed by `ArcCreationService`. Duplicate or collinear points are rejected. The preview appears after the second point.

### PolylineTool

Creates an open or closed `PolylineEntity` from a sequence of points.

State model:

```text
WaitingForFirstPoint
CollectingVertices
```

Behavior:

```text
point input -> add vertex
Enter       -> finish open polyline, requires at least 2 vertices
C           -> close polyline, requires at least 3 vertices
Esc         -> cancel
```

The current base point is updated to the last accepted vertex so relative input and direct distance continue from the last segment.

Ortho or Polar Tracking applies to the current segment only.

---

## Dimension tools

Dimension tools create non-associative annotation entities. They use the current layer, the default dimension style and undoable `AddEntityCommand` commits.

All dimension tools show preview geometry before the final click. Rendering and preview are based on the shared `DimensionGeometryBuilder`, so the interactive view matches SVG/DXF export geometry.

### HorizontalDimensionTool

Workflow:

```text
activate Horizontal Dim
pick first measured point
pick second measured point
pick dimension-line placement point
```

The measured value is the horizontal delta between the first and second points.

### VerticalDimensionTool

Workflow:

```text
activate Vertical Dim
pick first measured point
pick second measured point
pick dimension-line placement point
```

The measured value is the vertical delta between the first and second points.

### AlignedDimensionTool

Workflow:

```text
activate Aligned Dim
pick first measured point
pick second measured point
pick dimension-line placement point
```

The measured value is the true distance between the two measured points.

### RadiusDimensionTool

Workflow:

```text
activate Radius Dim
pick center point
pick point on circle
pick text placement point
```

The generated label uses the `R` prefix.

### DiameterDimensionTool

Workflow:

```text
activate Diameter Dim
pick center point
pick point on circle
pick text placement point
```

The generated label uses the `Ø` prefix.

### AngularDimensionTool

Workflow:

```text
activate Angular Dim
pick angle center
pick point on first ray
pick point on second ray
pick arc placement point
```

The fourth click chooses the angular sector. This supports both minor and reflex angles greater than 180°.

Dimension limitations in v0.4:

- dimensions are not associative;
- there is no dimension grip editing yet;
- dimension styles are persisted but do not yet have a dedicated manager window;
- Property Panel support is read-only.

---

## SelectionTool

Selection supports:

- click selection;
- Shift-click toggle;
- Ctrl-click cycle through overlapping entities;
- Ctrl+Shift-click cycle and toggle;
- window selection;
- crossing selection.

Selection uses selectable entities:

```text
visible and unlocked entities
```

Hidden entities and locked-layer entities are not selectable.

While `SelectionTool` is active, only `SnapKind.Entity` is enabled. This keeps the snap marker focused on selectable entities and avoids showing endpoint/midpoint/grid snaps while the user is trying to pick objects.

---

## Basic edit tools

### MoveTool

Moves entities by base point and destination point.

If the selection set is already populated when Move starts, the workflow is the classic two-point flow:

```text
activate Move
pick base point
pick destination point
commit move
```

If no entity is selected when Move starts, the tool first enters an entity-selection phase:

```text
activate Move with no selection
click entity to move
optionally Shift-click to toggle more entities
optionally Ctrl-click to cycle overlapping entities
Enter -> confirm selected entities
pick base point
pick destination point
commit move
```

During the first phase only entity snap is active. During the base/destination phase the tool uses the ordinary geometric snaps from `ToolContext.EnabledSnaps`; the resulting point can then be constrained by Polar Tracking or Ortho.

### CopyTool

Copies selected entities by base point and destination point.

### DeleteTool

Deletes selected entities.

All edit tools use document commands and remain protected by locked-layer rules.

---

## Snap mode per tool phase

Tools may implement `ISnapModeProvider` to control which snap kinds are active in the current interaction phase.

Examples:

```text
SelectionTool -> EntityOnly
MoveTool waiting for entity selection -> EntityOnly
MoveTool waiting for base/destination -> enabled geometric snaps
```

This avoids changing the user's global snap settings just because a specific tool temporarily needs entity picking.

---

## Transform tools

### RotateTool

Workflow:

```text
select entities
activate Rotate
pick base point
pick reference point
pick destination point
commit rotation
```

The angle is:

```text
angle(base -> destination) - angle(base -> reference)
```

Ortho constrains interactive rotation to multiples of 90 degrees. Polar Tracking is currently focused on point placement, so explicit rotate-angle behavior remains separate.

### ScaleTool

Workflow:

```text
select entities
activate Scale
pick base point
pick reference point
pick destination point
commit scale
```

Factor:

```text
factor = distance(base, destination) / distance(base, reference)
```

Scale factors must be positive.

### AlignTool

Workflow:

```text
select entities
activate Align
pick source point 1
pick destination point 1
pick source point 2
pick destination point 2
confirm scale option
```

After the fourth point:

```text
Enter or N -> apply translation + rotation only
Y          -> apply translation + rotation + uniform scale
```

Align owns a multi-step state machine and does not derive from `TwoPointToolBase`.

---


## Modify tools

Modify tools change existing geometry topologically or by extending/trimming parts.

Break tools are currently line-based. Trim and Extend now support multiple entity types where the operation is geometrically meaningful.

### BreakAtPointTool

Workflow:

```text
activate Break Point
pick target LineEntity
pick break point
project break point onto line
replace original line with two line entities
```

Uses `LineBreakService` and commits through `ModifyEntitiesCommand`.

### BreakBetweenPointsTool

Workflow:

```text
activate Break Segment
pick target LineEntity
pick first break point
pick second break point
remove the segment between the projected break points
```

The two break points are ordered along the line. The result may be zero, one or two remaining line segments depending on where the break points fall.

### ExtendTool

Workflow:

```text
activate Extend
pick boundary entity
pick target entity near the endpoint to extend
extend the nearest valid endpoint until it reaches the boundary
```

Boundaries may be lines, circles, arcs or polylines. Targets currently include lines, arcs and open polylines. Circles are not extended because they are closed.

The tool stays active with the same boundary until `Esc`.

### TrimTool

Workflow:

```text
activate Trim
pick cutting edge entity
pick target entity on the portion to remove
replace the target with the remaining geometry
```

Cutting edges and targets may be lines, circles, arcs or polylines. A trimmed circle can become one or more arcs.

The tool stays active with the same cutting edge until `Esc`.

All modify tools create undoable changes and use Core geometry services.

---

## GripEditTool

Grip editing modifies an existing selected entity through grip points.

Activation:

```text
Tab
```

Selection rule:

```text
one selected entity       -> edit that entity
multiple selected entities -> edit the last selected entity
no selection              -> no operation
```

Grip edit state:

```text
Idle       -> show grips, hover detection
GripActive -> one grip is active, preview modified entity
```

Grip edits commit through replacement commands and remain undoable.

---

## Measure tools

Measure tools are non-mutating tools. They report information about existing geometry or picked points without changing the document.

Implemented tools:

```text
MeasureDistanceTool
MeasureEntityTool
MeasureAngleTool
MeasureAreaTool
```

### MeasureDistanceTool

Workflow:

```text
activate Distance
pick first point
pick second point
report distance, DX, DY and angle
```

The tool derives from the shared two-point workflow, shows a temporary preview line and supports snap, typed coordinates, relative coordinates, direct distance and Polar Tracking / Ortho. It creates no entity and executes no command.

### MeasureEntityTool

Workflow:

```text
activate Entity
click entity
report entity-specific measurements
```

The tool uses `SnapKind.EntityOnly`, does not modify selection, and supports `Ctrl+click` to cycle overlapping entities.

Reported values depend on entity type:

```text
LineEntity      length, angle
CircleEntity    radius, diameter, circumference, area
ArcEntity       radius, diameter, sweep, arc length
PolylineEntity  length/perimeter, area when closed, vertex count, closed flag
```

### MeasureAngleTool

Workflow:

```text
activate Angle
pick first ray point
pick vertex point
pick second ray point
report angle and supplementary angle
```

The tool shows temporary ray previews and supports the same point input constraints as other point tools.

### MeasureAreaTool

Workflow:

```text
activate Area
click closed polyline
report area, length/perimeter and vertex count
```

The tool currently accepts only closed `PolylineEntity` instances. It uses entity snap and supports `Ctrl+click` cycling.

### Measurement rules

All measure tools follow these rules:

- do not create entities;
- do not call `CommandHistory.Execute`;
- do not mark the document dirty;
- do not change undo/redo history;
- use `MeasurementService` and `MeasurementFormatter` from `OpenCad2D.Core.Measurements`;
- display values in model units without physical unit suffixes.

---

## Ortho and Polar Tracking

Ortho is the legacy horizontal/vertical point constraint. Polar Tracking generalizes the same idea with a configurable angular step.

```text
Polar Off -> no polar constraint
Polar 90° -> 0°, 90°, 180°, 270°
Polar 45° -> 0°, 45°, 90°, 135°, ...
Polar 30° -> 0°, 30°, 60°, 90°, ...
Polar 15° -> 0°, 15°, 30°, 45°, ...
```

The shared implementation is `ToolInputConstraintService`.

Resolution order:

```text
raw point -> snapping -> Polar Tracking / Ortho -> preview and commit
```

Tools that use point constraints should apply them before preview, measurements and direct distance results so “what you see is what you get”.

Polar Tracking has priority when enabled. If Polar is `Off`, legacy Ortho can still constrain to horizontal/vertical directions.

---

## Future modify tools

Next planned modify tools:

```text
Break
Trim
Extend
```

They should follow these rules:

- work on selected or picked entities;
- use hit testing/snapping where appropriate;
- calculate geometry in service classes;
- preview before committing when useful;
- commit through undoable commands;
- modify documents only through `CadDocument` APIs;
- respect locked-layer protection.

---

## Export is not a tool

SVG export is triggered from the file command bar and lives in `OpenCad2D.Export`, not in `OpenCad2D.Tools`.

Export does not participate in the tool pipeline and does not modify the document.

---

## Escape behavior

`Esc` follows a progressive CAD-style rule:

```text
inside a non-selection tool -> cancel the active command and return to Selection
Selection with selected entities -> clear the selection
Selection with no selected entities -> no operation
```

The first `Esc` inside a drawing or modify command keeps the current selection. A second `Esc`, now in `SelectionTool`, clears that selection.

`Enter` is reserved for confirming or advancing multi-step command phases, such as finishing a polyline or confirming the entity-selection phase of Move.
