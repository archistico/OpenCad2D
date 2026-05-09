# Commands

Commands are the mechanism used by OpenCad2D to modify the document in a controlled and undoable way.

A CAD application needs reliable undo and redo from the beginning. User-facing operations should not directly mutate the document from the UI. Instead, they should be represented by command objects and executed through `CommandHistory`.

A command knows how to execute an operation and how to undo it.

---

## Main idea

When the user performs an operation, such as drawing a line, moving entities or deleting selected objects, the active tool creates a command.

The command is executed through `CommandHistory`. `CommandHistory` executes the command and stores it on the undo stack.

If the user runs undo, `CommandHistory` calls the command's `Undo` method.

If the user runs redo, `CommandHistory` executes the command again.

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

The command receives the document as a parameter instead of owning it. This keeps commands reusable and testable.

---

## CommandHistory

`CommandHistory` coordinates undo and redo. It stores executed commands on an undo stack.

When undo is requested, the latest command is removed from the undo stack, its `Undo` method is called, and the command is pushed onto the redo stack.

When redo is requested, the latest command from the redo stack is executed again and moved back to the undo stack.

When a new command is executed after an undo, the redo stack is cleared. This is the expected behavior in most editing applications.

---

## CadDocument as mutation boundary

Commands should modify entities through the `CadDocument` API.

Correct:

```csharp
document.AddEntity(entity);
document.AddEntities(entities);
document.ReplaceEntity(entity);
document.ReplaceEntities(entities);
document.RemoveEntity(id);
document.RemoveEntities(ids);
```

Avoid this in commands:

```csharp
document.Entities.Add(entity);
document.Entities.Replace(entity);
document.Entities.RemoveMany(ids);
```

`EntityCollection` may be queried by commands, but mutation should go through `CadDocument`.

This rule is important because document-level validation belongs in `CadDocument`.

Locked layer rules are enforced in:

```text
RemoveEntity
RemoveEntities
ReplaceEntity
ReplaceEntities
```

If commands bypass the document and mutate `EntityCollection` directly, locked-layer protection is skipped. This must not happen.

Command line input does not change this rule. Typed coordinates and distances are resolved to points and forwarded to the active tool; the tool still creates or executes the same commands used by mouse input.

---

## AddEntityCommand

`AddEntityCommand` adds one or more entities to the document.

It is used by drawing tools such as `LineTool` and `RectangleTool`.

When executed, it calls the document API to add entities.

When undone, it removes the same entities by id through the document API.

This command is simple but fundamental, because every drawing operation starts from adding new entities.

### Important rules

- Added entities must reference an existing layer.
- Undo must remove the same entity ids.
- Redo must re-add the same entity instances or equivalent entities with the same ids.
- The command must not mutate `EntityCollection` directly.

---

## DeleteEntitiesCommand

`DeleteEntitiesCommand` deletes existing entities from the document.

Before deleting, it stores the entities that will be removed. This is necessary because undo must restore the exact original entities.

When executed, the command removes the selected entities through `CadDocument.RemoveEntities(...)`.

If one of the target entities belongs to a locked layer, `CadDocument` rejects the removal. Selection should normally prevent this case earlier, because locked-layer entities are not selectable, but the document still protects itself.

When undone, it adds the previously deleted entities back through `CadDocument.AddEntities(...)`.

This command is used by `DeleteTool` and by keyboard delete actions.

### Important rules

- Store deleted entities before removing them.
- Remove through `CadDocument.RemoveEntities(...)`.
- Undo through `CadDocument.AddEntities(...)`.
- Never clear UI selection directly from the command.

---

## ReplaceEntitiesCommand

`ReplaceEntitiesCommand` replaces existing entities with new versions.

It is useful when an operation changes geometry or properties while preserving entity identifiers.

Before replacing, it stores the original entities.

When executed, it calls `CadDocument.ReplaceEntities(...)`.

If one of the target entities belongs to a locked layer, `CadDocument` rejects the replacement. This protects move, rotate, scale, mirror and future modify commands.

When undone, it restores the original entities through the same document API.

This command is useful as a general mechanism for entity modification and for future operations such as trim, extend, fillet, chamfer and property edits.

### Important rules

- Preserve entity ids.
- Store originals before replacement.
- Replace through `CadDocument.ReplaceEntities(...)`.
- Undo through `CadDocument.ReplaceEntities(...)`.
- Do not mutate entity geometry in place.

---

## TransformEntitiesCommand

`TransformEntitiesCommand` applies a transformation matrix to one or more entities.

It is the base command for operations such as move, rotate, scale and mirror.

When executed, it stores the original entities, creates transformed versions and replaces them through `CadDocument.ReplaceEntities(...)`.

Because replacement goes through `CadDocument`, transformations are blocked for entities on locked layers.

When undone, it restores the original entities.

The command itself does not decide what transformation means. It simply applies the provided `Matrix2D`.

### Important rules

- Use a transformation matrix.
- Preserve entity ids.
- Create transformed entity instances.
- Replace through the document API.
- Do not modify geometry in place.

---

## MoveEntitiesCommand

`MoveEntitiesCommand` moves selected entities by a displacement vector.

Internally, it uses a translation matrix and inherits undo behavior from `TransformEntitiesCommand`.

The selected entities keep their identifiers. Undo restores the original positions.

This command is used by `MoveTool`.

Locked-layer entities are not selectable, so normal move workflows should not target them. If they are passed anyway, `CadDocument.ReplaceEntities(...)` rejects the replacement.

---

## CopyEntitiesCommand

`CopyEntitiesCommand` creates translated copies of selected entities.

Unlike move, copy does not modify the original entities.

When first executed, it creates copied entities with new identifiers. Undo removes the copied entities through the document API.

Redo should re-add the same copied entities with the same generated identifiers, rather than generating new identifiers every time. This keeps redo deterministic and avoids unstable command behavior.

Copying does not modify the source entities. However, locked-layer entities are not selectable, so the normal UI workflow cannot copy them through selection.

A future explicit copy-from-reference workflow would need a clear rule for whether locked-layer source entities are allowed.

### Important rules

- Do not modify the source entities.
- Generate copied entity ids once.
- Reuse copied entity ids on redo.
- Remove copied entities through `CadDocument.RemoveEntities(...)` on undo.

---

## RotateEntitiesCommand

`RotateEntitiesCommand` rotates selected entities around a center point by an angle.

It is based on a rotation matrix and inherits the common transform behavior.

The command keeps the same entity identifiers and replaces entities with rotated versions.

Undo restores the original entities.

Because it ultimately replaces entities through `CadDocument`, locked-layer protection applies.

---

## ScaleEntitiesCommand

`ScaleEntitiesCommand` scales selected entities around a center point.

It validates that the scale factor is greater than zero.

The current transformation model supports uniform scale. Non-uniform scaling is not represented as a separate high-level command yet.

Undo restores the original entities.

Because it ultimately replaces entities through `CadDocument`, locked-layer protection applies.

---

## MirrorEntitiesCommand

`MirrorEntitiesCommand` mirrors selected entities across a line.

It uses a mirror transformation matrix.

The operation replaces selected entities with mirrored versions while keeping their identifiers.

Undo restores the original entities.

Because it ultimately replaces entities through `CadDocument`, locked-layer protection applies.

---

## CompositeCommand

`CompositeCommand` groups multiple commands into one undoable operation.

This is useful when a user action requires multiple document changes.

Examples:

```text
trim + add extension geometry
fillet two lines and add arc
explode polyline into line segments
offset and create multiple entities
```

Execution order:

```text
child 1
child 2
child 3
```

Undo order:

```text
child 3
child 2
child 1
```

If a child command fails during execution, already executed child commands should be rolled back where possible.

### Important rules

- A composite command should represent one user-facing action.
- Do not hide unrelated operations inside the same composite command.
- Preserve predictable undo behavior.

---

## Command line input and commands

Command line input is not a command execution shortcut. It only provides precise point input to the active tool.

Example with `LineTool`:

```text
user types 100,50
command line resolves a WCS point
active LineTool receives the point
user types 200,50
active LineTool receives the second point
LineTool creates AddEntityCommand
CommandHistory executes the command
```

Example with direct distance entry:

```text
user chooses a base point
user moves cursor to indicate direction
user types 5
command line resolves the second point
active tool executes the normal command workflow
```

Therefore undo/redo behavior is unchanged.

---

## Command and selection interaction

Commands should not directly own UI selection behavior.

Selection is stored in `SelectionSet`, outside the command system.

A tool or workspace action may update selection after a command executes, but commands should focus on document changes only.

This separation keeps commands reusable and testable.

Example:

```text
Delete key pressed
ActionController reads SelectionSet
ActionController creates DeleteEntitiesCommand
Command removes entities through CadDocument
Workspace/UI refreshes selection/status
```

---

## Locked layer behavior and commands

Locked layer protection must not rely only on the UI.

The UI and interaction services prevent locked-layer entities from being selected, but `CadDocument` is the final protection boundary.

This matters because future code may create commands programmatically, bypassing normal point or window selection.

Rules:

```text
AddEntity          allowed if the target layer exists
RemoveEntity       rejected if the entity is on a locked layer
RemoveEntities     rejected if a target entity is on a locked layer
ReplaceEntity      rejected if the existing entity is on a locked layer
ReplaceEntities    rejected if a target entity is on a locked layer
```

Transform-based commands are protected because they use replace operations.

Delete-based commands are protected because they use remove operations.

---

## Adding a new command

When adding a new command, follow this checklist:

1. Implement `ICadCommand`.
2. Give the command a clear `Name`.
3. Use `CadDocument` mutation methods.
4. Store enough state to undo exactly.
5. Preserve entity ids when modifying existing entities.
6. Generate new ids only for new entities.
7. Make redo deterministic.
8. Add tests for execute, undo and redo.
9. Add tests for locked-layer behavior if the command removes or replaces entities.

---

## Command design rules

Preserve these rules:

```text
Commands do not depend on Avalonia.
Commands do not read mouse input.
Commands do not render.
Commands do not show dialogs.
Commands mutate documents only through CadDocument.
Commands must support reliable undo.
Commands must support deterministic redo.
Commands should not directly manage UI selection.
```
