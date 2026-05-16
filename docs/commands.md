# Commands

Commands are undoable document mutations.

The command system keeps document changes predictable, reversible and testable.

---

## Command rule

Any user-facing operation that changes the drawing should be represented by a command.

Commands should not know about Avalonia, windows, controls, rendering or file dialogs.

---

## ICadCommand

Commands implement:

```text
Name
Execute(CadDocument document)
Undo(CadDocument document)
```

`Execute` applies the operation. `Undo` reverses it.

---

## CommandHistory

Command history owns undo and redo stacks.

Execution:

```text
execute command
push to undo stack
clear redo stack
increment generation
```

Undo/redo also affect generation. The workspace uses command generation to determine dirty state.

---

## Document mutation boundary

Commands must mutate the document through `CadDocument`.

Correct:

```csharp
document.AddEntity(entity);
document.RemoveEntities(ids);
document.ReplaceEntities(replacements);
```

Incorrect:

```csharp
document.Entities.Replace(entity);
document.Entities.RemoveMany(ids);
```

This ensures layer validation, locked-layer protection and spatial-index consistency.

---

## AddEntityCommand

Used by drawing tools:

- Line;
- Rectangle;
- Rect Sides;
- Circle;
- Arc;
- Arc 3P;
- Polyline;
- future Text/Dimension tools.

Execute adds created entities. Undo removes them.

---

## DeleteEntitiesCommand

Deletes selected entities and stores originals for undo.

Locked-layer entities should normally never reach this command because they are not selectable. If they do, `CadDocument` still rejects deletion.

---

## ReplaceEntitiesCommand

Replaces existing entities with modified versions while preserving entity identity.

Used by:

- grip editing;
- geometry edits;
- assigning selected entities to the current layer;
- future property edits.

---

## MoveEntitiesCommand

Moves selected entities by a vector.

---

## CopyEntitiesCommand

Creates copied entities from selected entities.

Copying does not modify the source entities. Locked-layer entities are not normally copyable through selection because they are not selectable.

---

## RotateEntitiesCommand

Rotates selected entities around a base point by an angle.

Used by `RotateTool`.

---

## ScaleEntitiesCommand

Scales selected entities around a base point by a positive uniform factor.

Used by `ScaleTool`.

---

## TransformEntitiesCommand

Applies a precomputed geometric transformation to selected entities.

Useful for composed transformations such as Align.

`AlignTool` can use a transformation computed by an alignment service and then commit through a transform/replacement command.

---

## UpdateLayersCommand

Applies batch layer changes from the Layer Manager.

It stores:

```text
old layer collection snapshot
new layer collection snapshot
old current layer
new current layer
```

Execute applies the new layer state. Undo restores the old state.

Layer Manager changes include the `LineFormatId` assigned to each layer. Color, line weight and line style are not edited directly on the layer.

After execution or undo, selection should be revalidated because hidden or locked layers can make selected entities invalid.

---

## UpdateLineFormatsCommand

Applies batch line format changes from the Line Format Manager.

It stores:

```text
old line format collection snapshot
new line format collection snapshot
```

Execute applies the new line format collection. Undo restores the old collection.

Line format changes affect rendering and SVG export for every layer that references the edited format. This is intentional: line formats are reusable stroke definitions.

Built-in line formats are editable but not deletable. User-defined line formats can be deleted only when allowed by the manager validation rules.

---

## ModifyEntitiesCommand

Replaces one or more existing entities with zero, one or more new entities.

This command is used by modify tools where the number of resulting entities may differ from the number of original entities.

Used by:

- Break Point;
- Break Segment;
- Extend;
- Trim.

Execute:

```text
remove original entities
add resulting entities
```

Undo:

```text
remove resulting entities
restore original entities
```

All removals/additions go through `CadDocument`, preserving locked-layer validation and spatial index consistency.

---

## CompositeCommand

Groups several commands into one user-facing undo step.

This is especially important for future modify tools such as:

```text
Trim
Extend
Break
Fillet
Chamfer
Offset
```

For example, a future trim operation may replace one entity and remove another, but the user should undo it as one action.

---

## Break/Trim/Extend commands

The implemented modify tools do not mutate entities directly.

Current command approach:

### Break

Split one entity into two or more entities.

Potential command composition:

```text
Remove original entity
Add resulting entity pieces
```

or the implemented `ModifyEntitiesCommand`, which stores original and resulting pieces.

### Trim

Replace or remove parts of an entity based on cutting boundaries.

Uses `ModifyEntitiesCommand` for trimming lines, arcs, circles and polylines where supported. Future multi-boundary workflows may use `CompositeCommand` if several operations must be grouped into one undo step.

### Extend

Replace an entity with an extended version that reaches a boundary.

Uses `ModifyEntitiesCommand` for consistency with the other modify tools. Current targets include lines, arcs and open polylines; circles are not extended because they are closed.

All three must respect locked-layer protection by using `CadDocument` mutation APIs.

---

## v0.6 command-line planning notes

v0.6 will expand the existing typed point input into a real command line.

The command line should support command activation through command names and aliases without duplicating tool logic. Typed commands should activate existing tools; typed coordinates should be submitted to the active tool through the same input path used by mouse picks.

Supported coordinate syntax:

```text
100,50      absolute point
@100,50     relative point
100         direct distance in current pointer/constrained direction
100<45      distance and CAD-model angle
```

Numeric parsing should be culture-invariant. The decimal separator is `.` and the coordinate separator is `,`.

Supported alias examples:

```text
L      -> LINE
C      -> CIRCLE
PL     -> POLYLINE
TR     -> TRIM
EX     -> EXTEND
HDIM   -> Horizontal Dimension
VDIM   -> Vertical Dimension
ADIM   -> Aligned Dimension
RAD    -> Radius Dimension
DIA    -> Diameter Dimension
ANG    -> Angular Dimension
```

Ambiguous one-letter aliases such as `R` and `D` are deliberately avoided for now.

See:

```text
docs/v0.6-command-line-property-panel-plan.md
```

---


## v0.6 distance plus angle input

Distance-angle input is supported with CAD model orientation:

```text
100<0       100 units to the right
100<90      100 units upward
100<180     100 units to the left
100<270     100 units downward
100<-90     normalized to 270 degrees
100<450     normalized to 90 degrees
```

Rules:

- the format is `distance<angle`;
- spaces around `<` are allowed;
- distance must be greater than zero;
- angles are expressed in degrees;
- negative angles and angles over 360 degrees are normalized;
- the input requires a current base point accepted by the active tool;
- distance-angle input is not stored in command history.

Example:

```text
L
0,0
100<45
```

creates a line from `(0,0)` to the point 100 units away at 45 degrees in CAD coordinates.

## v0.6 Property Panel command rule

Property Panel v2 edits must be undoable.

A property edit should create or trigger a command, usually by replacing the selected entity with a modified copy.

Correct flow:

```text
Property editor value
-> validation/parsing
-> modified entity copy
-> ReplaceEntitiesCommand or dedicated command
-> CommandHistory
```

The UI must not directly mutate entity objects. This protects undo/redo, dirty-state tracking, layer validation and spatial-index consistency.


---

## Command-line tool activation

v0.6 introduces command-line activation for tools. This is separate from undoable document commands: typing `LINE` or `L` activates `LineTool`; it does not directly mutate the document.

Alias resolution is handled by `CommandAliasRegistry` in `OpenCad2D.Tools.Input` before the existing coordinate parser runs. This preserves existing typed coordinate behavior such as `100,50`, `@10,0` and direct distance input.

Examples:

```text
L       -> Line
C       -> Circle
TR      -> Trim
EX      -> Extend
HDIM    -> Horizontal Dimension
ANG     -> Angular Dimension
```

Unknown textual commands return a clear message and do not change the active tool.


## v0.6 relative coordinates and direct distance

Relative coordinates and direct distance entry are supported by the same command-line pipeline used for absolute coordinates.

Supported forms:

```text
@100,0      relative point from the current tool base point
@0,-50      relative point with negative Y offset
100         direct distance from the current tool base point
```

Rules:

- `@x,y` requires a current base point accepted by the active tool;
- direct distance also requires a current base point;
- direct distance uses the current cursor direction, after Ortho/Polar constraints when enabled;
- coordinate and distance input is not stored in command history;
- the decimal separator is always `.`.

Examples:

```text
L
0,0
@100,0
```

creates a 100-unit horizontal line.

```text
L
0,0
# move cursor to the right
50
```

creates a 50-unit line in the indicated direction.


## v0.6 repeat last command

The command line now remembers the last valid tool activation as the repeatable command. Coordinate inputs are intentionally excluded from repeat history.

Examples:

```text
L
0,0
100,0
Enter
```

The final `Enter` on an empty command input repeats the last command, so `Line` becomes active again.

Right-clicking inside the CAD canvas also requests repeat-last-command. This is handled at the canvas/UI boundary and calls `MainWindowViewModel.RepeatLastCommandFromCanvas()`. Canvas repeat is intentionally conservative: if a point-based command is already active and has a base point, right-click reports that the current command should be finished or cancelled first.

Rules:

```text
Valid command or alias: stored as repeatable command
Coordinate input: not stored as repeatable command
Relative input: not stored as repeatable command
Direct distance input: not stored as repeatable command
Distance-angle input: not stored as repeatable command
Invalid command: not stored as repeatable command
No previous command: reports "No command to repeat."
```


---

## v0.6 command-line final behavior

The v0.6 command line supports both tool activation and precise point input.

Tool activation examples:

```text
L / LINE                    -> Line
PL / POLYLINE               -> Polyline
C / CIRCLE                  -> Circle
A / ARC                     -> Arc
T / TEXT                    -> Text
PO / POINT                  -> Point
HDIM / H                    -> Horizontal Dimension
VDIM / V                    -> Vertical Dimension
ADIM / AL                   -> Aligned Dimension
RAD / RDIM                  -> Radius Dimension
DIA / DDIM                  -> Diameter Dimension
ANG / ANGDIM                -> Angular Dimension
TR / TRIM                   -> Trim
EX / EXTEND                 -> Extend
BP / BREAKPOINT             -> Break Point
BS / BREAKSEGMENT           -> Break Segment
DI / DISTANCE               -> Measure Distance
ME / MEASURE                -> Measure Entity
```

Point input forms:

```text
100,50      absolute model point
@100,0      relative point from the current tool base point
50          direct distance in current pointer/constrained direction
@100<45     relative polar point from the current tool base point
```

Older v0.6 notes used `100<45` for distance-angle input. The v0.8 guided command input documentation uses the clearer relative polar form `@distance<angle`.

Command-line coordinate input is routed through the active tool. It does not duplicate drawing logic.

Coordinate input is intentionally not stored as the repeatable command. The repeatable command is the last valid tool activation.

`Enter` with an empty command line repeats the last valid command. Right-click on the canvas does the same when the workspace is idle. If a multi-step command is already in progress, right-click does not interrupt it.

`Esc` with an empty command line cancels the active command.

---

## v0.6 Property Panel v2 final behavior

Property Panel edits are undoable document mutations.

Supported editable properties include:

- `PointEntity`: X/Y position;
- `LineEntity`: start/end coordinates;
- `CircleEntity`: center/radius;
- `ArcEntity`: center/radius/start angle/end angle;
- `TextEntity`: value, insertion point, rotation and text format;
- `PolylineEntity`: common state such as closed/open;
- dimensions: dimension style and text override;
- common layer assignment where applicable.

The panel validates input before applying edits. Invalid numeric values, invalid radii, empty text values and invalid geometry are rejected before a command is executed.

All successful edits are applied through command history, normally by replacing the selected entity with a modified copy. This keeps undo/redo, dirty-state tracking and spatial-index updates consistent.


## Navigation commands

OpenCad2D supports viewport navigation from the command line:

```text
ZOOMWINDOW / ZW
```

`Zoom Window` asks for two opposite corners and fits the viewport to the selected rectangular model area. Very small windows are ignored to avoid accidental extreme zooms. `Zoom Extents` is also available in the left Navigate panel and fits the viewport to all visible geometry.

## Selection commands

OpenCad2D supports direct selection actions from the command line:

```text
SELECTALL / SA / ALL
SELECTLAST / SL / LAST
```

`Select All` replaces the current selection with all selectable entities. Entities on hidden or locked layers are skipped.

`Select Last` restores the last effective selection that was explicitly cleared. It can restore either one entity or a multi-entity selection. Entities that are no longer selectable, for example because their layer is hidden or locked, are skipped.

---

## Command input refactor plan for v0.8

The v0.8 command input work is separate from document mutation commands such as `AddEntityCommand`, `MoveEntitiesCommand` and `ModifyEntitiesCommand`.

Document commands remain responsible for undoable changes to `CadDocument`.

The command input system is responsible for:

- starting tools by command name or alias;
- displaying the active command prompt;
- parsing user text as command, point, distance, option or confirmation;
- routing parsed input to the active tool;
- maintaining compact visible command history;
- repeating the last valid command when the workspace is idle and Enter is pressed.

The detailed v0.8 design is in:

```text
docs/command-input.md
```

Important planned rules:

- mouse clicks and typed points should feed the same tool state machine;
- absolute point input uses `x,y`;
- relative cartesian input uses `@dx,dy`;
- relative polar input uses `@distance<angle`;
- `LINE` remains a single-segment command;
- `POLYLINE` should support `Close`/`C`, `Undo`/`U` and empty Enter to finish;
- Trim should use a picked-entity input that includes both entity id and pick point.


### v0.8 command input block 2

Completed UI plumbing for the CAD-style command input refactor:

- visible compact command history;
- contextual command prompt remains visible above the input box;
- contextual placeholder examples for absolute, relative and polar coordinates;
- empty Enter can repeat the last command from the command line/canvas flow;
- existing command alias history remains separate from the visible UI history.

### LINE command input in v0.8

`LINE` is the first command migrated to the guided command-input workflow.

Supported input while `LINE` is active:

```text
100,50
@50,0
@100<45
5
```

The plain distance form uses the current cursor direction and therefore requires a first/base point and a meaningful cursor direction.


## v0.8 command-driven input notes

`LINE` and `POLYLINE` now use the guided command input flow. Both tools can receive points from either the mouse/canvas or the command input.

### LINE

Aliases: `LINE`, `L`

```text
LINE: Specify first point:
LINE: Specify second point:
```

The second point supports `x,y`, `@dx,dy`, `@distance<angle` and direct distance input.

### POLYLINE

Aliases: `POLYLINE`, `PL`

```text
POLYLINE: Specify first point:
POLYLINE: Specify next point or [Close/Undo]:
```

Options while collecting vertices:

- `C` / `Close` closes the polyline;
- `U` / `Undo` removes the last vertex;
- empty Enter finishes an open polyline.


## v0.8 base drawing command input

The command input now guides the basic drawing tools with contextual prompts:

- `CIRCLE` / `C`: center point, then radius point or radius-style input.
- `RECTANGLE` / `REC`: first corner, then opposite corner.
- `ARC3P` / `A3P`: start point, point on arc, then end point.

For point prompts the user can either click in the canvas or type coordinates such as `100,50`, `@50,0` or `@100<45`.

## v0.8 command-driven edit aliases

Additional edit command aliases:

```text
MOVE / M
COPY / CP
BREAK / BR / BREAKSEGMENT / BS
```

`MOVE` and `COPY` support typed base/destination points, including `@x,y` and `@distance<angle`. `BREAK` supports typed first and second break points after the target entity has been selected from the canvas.

## v0.8 edit command input notes

The edit commands now expose CAD-style command prompts:

```text
ROTATE: Specify base point:
ROTATE: Specify reference point:
ROTATE: Specify destination point or type angle:

SCALE: Specify base point:
SCALE: Specify reference point:
SCALE: Specify destination point or type scale factor:

ALIGN: Specify first source point:
ALIGN: Specify first destination point:
ALIGN: Specify second source point:
ALIGN: Specify second destination point:
ALIGN: Apply scale or [Yes/No]:

BREAKPOINT: Select entity:
BREAKPOINT: Specify break point:

EXTEND: Select boundary entity:
EXTEND: Select entity to extend:

TRIM: Select cutting edge:
TRIM: Select entity side to trim:

DELETE: Press Enter to delete selected entities:
```

Rotate interprets a plain number in the final phase as an angle in degrees. Scale interprets a plain number in the final phase as a scale factor.


### Trim advanced base

The v0.8 command-input work adds a first advanced `TRIM` workflow with `All`, in-command `Undo`, repeated target trimming, Ctrl-click additional cutting edges and Enter-to-finish behavior. More advanced options such as Fence, Crossing, Edge, Project and Shift-to-Extend remain future work.

### Offset

Aliases: `OFFSET`, `O`.

Creates a constant-distance copy of a line, circle, arc or straight-segment polyline. The command asks for a distance, then an object, then the side point. After creating an offset, it stays active and asks for another object.

### Fillet

Aliases: `FILLET`, `F`.

Creates a line-line fillet. Use `R` or `Radius` to set the radius. Radius `0` joins the two selected lines at their theoretical intersection without creating an arc.

---

## v0.8 final command input summary

The v0.8 command system supports guided prompts, visible command history and exact typed input for the main drawing/editing workflows.

### Coordinate input

```text
100,50      absolute point
@100,0      relative cartesian point
@100<45     relative polar point
50          distance/number when the active prompt expects it
```

### Main aliases

| Command | Aliases | Notes |
|---|---|---|
| Line | `LINE`, `L` | Single segment: first point, second point, finish. |
| Polyline | `POLYLINE`, `PL` | Supports `Close` / `C`, `Undo` / `U`, Enter to finish open polyline. |
| Rectangle | `RECTANGLE`, `REC` | First corner, opposite corner. |
| Circle | `CIRCLE`, `C` | Center point, radius point or typed radius. |
| Arc 3P | `ARC3P` | Start, point on arc, end. |
| Move | `MOVE`, `M` | Selection, base point, destination point. |
| Copy | `COPY`, `CO` | Selection, base point, destination point. |
| Rotate | `ROTATE`, `RO` | Selection, base point, reference point, destination point or typed angle. |
| Scale | `SCALE`, `SC` | Selection, base point, reference point, destination point or typed factor. |
| Align | `ALIGN`, `AL` | Two source/destination point pairs and scale confirmation. |
| Break Point | `BREAKPOINT`, `BP` | Entity selection and break point. |
| Break Segment | `BREAK`, `BR`, `BREAKSEGMENT`, `BS` | Entity selection, first point, second point. |
| Extend | `EXTEND`, `EX` | Boundary entity, target entity. |
| Trim | `TRIM`, `TR` | Cutting edge or `All`, then repeated target side trimming with `Undo`. |
| Delete | `DELETE`, `DEL` | Enter confirms deletion of the current selection. |
| Offset | `OFFSET`, `O` | Distance, object, side point. Supports line/circle/arc/polyline. |
| Fillet | `FILLET`, `F` | Line-Line fillet. Supports `Radius` / `R`; radius `0` joins at a sharp corner. |
| Select All | `SELECTALL`, `SA`, `ALL` | Selects selectable visible/unlocked entities. |
| Select Last | `SELECTLAST`, `SL`, `LAST` | Restores the last real selection before deselection. |
| Zoom Window | `ZOOMWINDOW`, `ZW` | Fits the viewport to a picked rectangle. |
| Zoom Extents | `ZOOMEXTENTS`, `ZE` | Fits the viewport to visible drawing extents. |

### Enter behavior

- Empty Enter while idle repeats the last valid command.
- Empty Enter during an active command is routed to that command.
- If the active phase accepts confirmation, Enter confirms/completes the phase.
- If the active phase requires input, Enter reports that input is required and stays in the current phase.

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

## Align object action commands

These commands operate immediately on the current selection and are separate from the geometric `ALIGN` tool.

| Command | Alias | Behavior |
| --- | --- | --- |
| `ALIGNLEFT` | `ALEFT` | Align selected entities to the left edge of the selection bounds. |
| `ALIGNRIGHT` | `ARIGHT` | Align selected entities to the right edge of the selection bounds. |
| `ALIGNTOP` | `ATOP` | Align selected entities to the top edge of the selection bounds. |
| `ALIGNBOTTOM` | `ABOTTOM` | Align selected entities to the bottom edge of the selection bounds. |

## v0.8.x distribution commands

The distribution commands are undoable selection actions. They operate on the current selection and keep the first and last selected entities fixed according to their ordered center positions.

| Command | Aliases | Behavior |
| --- | --- | --- |
| `DISTRIBUTEHORIZONTAL` | `DISTRIBUTEHORIZONTALLY`, `DH` | Distributes selected entities horizontally by bounding-box center. Requires at least three selectable entities. |
| `DISTRIBUTEVERTICAL` | `DISTRIBUTEVERTICALLY`, `DV` | Distributes selected entities vertically by bounding-box center. Requires at least three selectable entities. |

Distribution currently uses center spacing, not equal gaps between bounding boxes. This is intentional for the first implementation because it is stable for mixed CAD entities.
