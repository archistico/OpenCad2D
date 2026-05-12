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

Planned coordinate syntax:

```text
100,50      absolute point
@100,50     relative point
100         direct distance in current pointer/constrained direction
100<45      distance and CAD-model angle
```

Numeric parsing should be culture-invariant. The decimal separator is `.` and the coordinate separator is `,`.

Planned alias examples:

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

Ambiguous one-letter aliases such as `R` and `D` should be avoided until a clear shortcut policy exists.

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
