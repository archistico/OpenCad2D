# Commands

Commands are the mechanism used by OpenCad2D to modify the document in a controlled and undoable way.

A CAD application needs reliable undo and redo from the beginning. User-facing operations should not directly mutate the document from the UI. Instead, they should be represented by command objects and executed through `CommandHistory`.

A command knows how to execute an operation and how to undo it.

---

## Main idea

When the user performs an operation, such as drawing a line, drawing a circle, moving entities or deleting selected objects, the active tool creates a command.

The command is executed through `CommandHistory`. `CommandHistory` executes the command and stores it on the undo stack.

If the user runs undo, `CommandHistory` calls the command's `Undo` method.

If the user runs redo, `CommandHistory` executes the command again.

`CommandHistory` also exposes a generation counter used by the workspace to track dirty state for persistence.

This keeps document changes predictable and centralized.

---

## ICadCommand

All commands implement `ICadCommand`.

A command exposes:

```text
Name
Execute(CadDocument document)
Undo(CadDocument document)
```

`Execute` applies the operation to a `CadDocument`.

`Undo` reverses the operation.

Commands should not know about Avalonia, controls, windows, viewports or rendering.

---

## CommandHistory

`CommandHistory` stores undo and redo stacks.

Normal execution:

```text
execute command
push command to undo stack
clear redo stack
```

Undo:

```text
pop command from undo stack
undo command
push command to redo stack
```

Redo:

```text
pop command from redo stack
execute command
push command to undo stack
```

This means command implementations must be deterministic and reversible.

### Generation counter and dirty state

Persistence uses command history generation to know whether the document has unsaved changes.

Conceptually:

```text
Execute command -> generation changes
Undo command    -> generation changes
Redo command    -> generation changes
Save/Open/New   -> workspace marks current generation as saved
```

`CadWorkspace.IsDirty` compares the current generation with the saved generation. This avoids scattering dirty flags across document mutation methods.

---

## CadDocument as mutation boundary

Commands must modify the drawing through `CadDocument`.

Correct examples:

```csharp
document.AddEntity(entity);
document.RemoveEntities(ids);
document.ReplaceEntities(entities);
```

Incorrect examples:

```csharp
document.Entities.RemoveMany(ids);
document.Entities.Replace(entity);
```

This rule is important because document-level validation belongs in `CadDocument`.

Locked layer rules are enforced in:

```text
RemoveEntity
RemoveEntities
ReplaceEntity
ReplaceEntities
```

If commands bypass the document and mutate `EntityCollection` directly, locked-layer protection is skipped. This must not happen.

---

## AddEntityCommand

`AddEntityCommand` adds one or more new entities to the document.

Used by:

- `LineTool`;
- `RectangleTool`;
- `CircleTool`;
- future drawing tools.

Execute:

```text
add created entity/entities
```

Undo:

```text
remove created entity/entities
```

The command should preserve the ids of the entities it adds, so undo/redo remains stable.

### CircleTool usage

`CircleTool` should create a `CircleEntity` and execute `AddEntityCommand`.

The radius is calculated by the tool before the command is created.

The command does not know whether the circle came from:

- mouse input;
- command line coordinates;
- direct distance entry;
- snap input.

That distinction belongs to the tool/input pipeline, not to the command.

---

## DeleteEntitiesCommand

`DeleteEntitiesCommand` removes entities from the document.

When created, it should capture enough information to restore the deleted entities during undo.

Execute:

```text
remove entities from document
```

Undo:

```text
add original entities back to document
```

When executed, the command removes the selected entities through `CadDocument.RemoveEntities(...)`.

If one of the target entities belongs to a locked layer, `CadDocument` rejects the removal. Selection should normally prevent this case earlier, because locked-layer entities are not selectable, but the document still protects itself.

---

## ReplaceEntitiesCommand

`ReplaceEntitiesCommand` replaces existing entities with new versions.

It is useful for operations where the same logical entity remains, but its geometry changes.

Examples:

- grip edit;
- move;
- rotate;
- scale;
- mirror;
- future stretch-like operations.

Execute:

```text
replace old entities with new entities
```

Undo:

```text
replace new entities with old entities
```

When executed, it calls `CadDocument.ReplaceEntities(...)`.

If one of the target entities belongs to a locked layer, `CadDocument` rejects the replacement. This protects grip editing, move, rotate, scale, mirror and future modify commands.

---

## TransformEntitiesCommand

`TransformEntitiesCommand` is a higher-level replacement command for geometric transformations.

When executed, it stores the original entities, creates transformed versions and replaces them through `CadDocument.ReplaceEntities(...)`.

Because replacement goes through `CadDocument`, transformations are blocked for entities on locked layers.

This command is useful when several tools can share the same transformation pipeline.

---

## MoveEntitiesCommand

`MoveEntitiesCommand` moves existing entities by a vector.

Conceptual behavior:

```text
for each selected entity:
    create transformed entity using translation vector
    replace original with transformed version
```

The entity id should remain the same.

Move should be undoable by replacing the moved entities with the original entities.

Move can receive its vector from:

- mouse base/destination points;
- snap points;
- command line destination point;
- direct distance entry;
- Ortho-constrained input.

The command itself should only know the final vector, not how the vector was entered.

---

## CopyEntitiesCommand

`CopyEntitiesCommand` creates translated copies of existing entities.

Conceptual behavior:

```text
for each selected entity:
    create transformed copy using translation vector
    assign new entity id
    add copied entity to document
```

The original entities are not modified.

Undo removes the copied entities.

Copying does not modify the source entities. However, locked-layer entities are not selectable, so the normal UI workflow cannot copy them through selection. A future explicit copy-from-reference workflow would need a clear rule for whether locked-layer source entities are allowed.

Copy can receive its vector from mouse, snapping, command line input, direct distance entry or Ortho-constrained input. The command should only receive the resolved vector.

---

## CompositeCommand

`CompositeCommand` groups several commands into one undoable operation.

Execution order:

```text
command 1
command 2
command 3
```

Undo order:

```text
command 3
command 2
command 1
```

This is important for future CAD operations that are conceptually one user action but require several document changes.

Examples:

```text
Fillet:
  replace first line
  replace second line
  add arc
```

```text
Trim:
  replace or remove one or more entities
```

The user should be able to undo the whole operation with one undo command.

---

## Command line and commands

The command line does not create commands directly in the UI.

Correct flow:

```text
user types input
command input parser resolves point/distance
the workspace submits a point to the active tool
the active tool creates the appropriate command
command history executes the command
```

This keeps command creation inside the tool layer.

For example:

```text
Line -> user types 100,50 -> point sent to LineTool
Line -> user types 200,50 -> LineTool creates AddEntityCommand
```

```text
Circle -> user types 100,50 -> point sent to CircleTool as center
Circle -> user types 25 -> CircleTool creates CircleEntity and AddEntityCommand
```

Grip editing follows the same principle:

```text
TAB -> GripEditTool
click grip -> choose destination
GripEditTool creates replacement entity
ReplaceEntitiesCommand commits the edit
```

---

## Ortho mode and commands

Ortho mode should affect the point or vector before a command is created.

The command should not know whether Ortho was enabled.

Example:

```text
MoveTool receives base point and Ortho-constrained destination point
MoveTool calculates final vector
MoveTool creates MoveEntitiesCommand(vector)
```

This keeps commands simple and independent of UI/input modes.

---

## Preview and commands

Preview should not modify the document and should not create commands.

Preview belongs to the active tool/UI rendering pipeline.

Only final accepted input should create commands.

Examples:

```text
move mouse during LineTool -> preview only
click second point -> AddEntityCommand
```

```text
move mouse during MoveTool -> transformed preview only
click destination point -> MoveEntitiesCommand
```

```text
move mouse during CircleTool -> circle preview only
click radius point or type radius -> AddEntityCommand
```

---

## Undo/redo expectations

Expected behavior:

```text
Draw Line       -> undo removes line
Draw Rectangle  -> undo removes rectangle polyline
Draw Circle     -> undo removes circle
Grip edit       -> undo restores original geometry
Move entities   -> undo restores original positions
Copy entities   -> undo removes copied entities
Delete entities -> undo restores deleted entities
```

Redo should reapply the same operation.

---

## Testing commands

Command tests should verify:

- execute changes the document as expected;
- undo restores the previous state;
- redo through `CommandHistory` works;
- entity ids are preserved where appropriate;
- copied entities receive new ids;
- locked-layer mutation rules are respected;
- grip edits preserve entity ids;
- dirty-state generation changes when commands execute or undo;
- document mutation goes through `CadDocument`.

Tool tests should verify that tools create the correct commands, but command behavior itself should be tested separately where possible.
