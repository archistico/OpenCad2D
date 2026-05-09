# Commands

Commands are the mechanism used by OpenCad2D to modify the document in a controlled and undoable way.

A CAD application needs reliable undo and redo from the beginning. User-facing operations should not directly mutate the document from the UI. Instead, they should be represented by command objects and executed through `CommandHistory`.

A command knows how to execute an operation and how to undo it.

---

## Main idea

When the user performs an operation, such as drawing a line, moving entities or deleting selected objects, the active tool creates a command.

The command is executed through `CommandHistory`.

`CommandHistory` executes the command and stores it on the undo stack.

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

`CommandHistory` coordinates undo and redo.

It stores executed commands on an undo stack.

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

This rule is important because document-level validation belongs in `CadDocument`. For example, locked layer rules should be enforced in `RemoveEntity`, `RemoveEntities`, `ReplaceEntity` and `ReplaceEntities`. If commands bypass the document, those future rules would be skipped.

---

## AddEntityCommand

`AddEntityCommand` adds one or more entities to the document.

It is used by drawing tools such as `LineTool` and `RectangleTool`.

When executed, it calls the document API to add entities.

When undone, it removes the same entities by id through the document API.

This command is simple but fundamental, because every drawing operation starts from adding new entities.

---

## DeleteEntitiesCommand

`DeleteEntitiesCommand` deletes existing entities from the document.

Before deleting, it stores the entities that will be removed.

This is necessary because undo must restore the exact original entities.

When executed, the command removes the selected entities through `CadDocument.RemoveEntities(...)`.

When undone, it adds the previously deleted entities back through `CadDocument.AddEntities(...)`.

This command is used by `DeleteTool` and by keyboard delete actions.

---

## ReplaceEntitiesCommand

`ReplaceEntitiesCommand` replaces existing entities with new versions.

It is useful when an operation changes geometry or properties while preserving entity identifiers.

Before replacing, it stores the original entities.

When executed, it calls `CadDocument.ReplaceEntities(...)`.

When undone, it restores the original entities through the same document API.

This command is useful as a general mechanism for entity modification and for future operations such as trim, extend, fillet, chamfer and property edits.

---

## TransformEntitiesCommand

`TransformEntitiesCommand` applies a transformation matrix to one or more entities.

It is the base command for operations such as move, rotate, scale and mirror.

When executed, it stores the original entities, creates transformed versions and replaces them through `CadDocument.ReplaceEntities(...)`.

When undone, it restores the original entities.

The command itself does not decide what transformation means. It simply applies the provided `Matrix2D`.

---

## MoveEntitiesCommand

`MoveEntitiesCommand` moves selected entities by a displacement vector.

Internally, it uses a translation matrix and inherits undo behavior from `TransformEntitiesCommand`.

The selected entities keep their identifiers.

Undo restores the original positions.

This command is used by `MoveTool`.

---

## CopyEntitiesCommand

`CopyEntitiesCommand` creates translated copies of selected entities.

Unlike move, copy does not modify the original entities.

When first executed, it creates copied entities with new identifiers.

Undo removes the copied entities through the document API.

Redo should re-add the same copied entities with the same generated identifiers, rather than generating new identifiers every time. This keeps redo deterministic and avoids unstable command behavior.

Copying from a locked layer may be allowed in the future because it does not modify the source entities. The copied entities should normally be created on the appropriate target/current layer according to the tool's creation rules.

---

## RotateEntitiesCommand

`RotateEntitiesCommand` rotates selected entities around a center point by an angle.

It is based on a rotation matrix and inherits the common transform behavior.

The command keeps the same entity identifiers and replaces entities with rotated versions.

Undo restores the original entities.

---

## ScaleEntitiesCommand

`ScaleEntitiesCommand` scales selected entities around a center point.

It validates that the scale factor is greater than zero.

The current transformation model supports uniform scale. Non-uniform scaling is not represented as a separate high-level command yet.

Undo restores the original entities.

---

## MirrorEntitiesCommand

`MirrorEntitiesCommand` mirrors selected entities across a line.

It uses a mirror transformation matrix.

The operation replaces selected entities with mirrored versions while keeping their identifiers.

Undo restores the original entities.

---

## CompositeCommand

`CompositeCommand` represents a single undoable command composed of several child commands.

It is needed because many real CAD operations are not single simple mutations.

For example, a future fillet operation may need to:

```text
shorten the first entity
shorten the second entity
add a fillet arc
```

The user should undo that as one operation.

`CompositeCommand.Execute` executes child commands in order.

`CompositeCommand.Undo` undoes child commands in reverse order.

If execution fails after some child commands have already run, the composite command should undo the executed children to roll the document back to its previous state.

This is not a full document transaction system, but it provides the first necessary layer for atomic multi-step CAD operations.

---

## Commands and tools

Tools should not modify the document directly.

A tool interprets user input and creates the appropriate command.

Examples:

```text
LineTool       -> AddEntityCommand
RectangleTool  -> AddEntityCommand
MoveTool       -> MoveEntitiesCommand
CopyTool       -> CopyEntitiesCommand
DeleteTool     -> DeleteEntitiesCommand
future Fillet  -> CompositeCommand
future Trim    -> ReplaceEntitiesCommand or CompositeCommand
```

This keeps tool logic focused on interaction and document mutation inside commands.

---

## Commands and selection

Commands operate on entities in the document.

Selection is stored separately in `SelectionSet`.

For example, `MoveTool` reads selected ids from the tool selection context and passes those ids to `MoveEntitiesCommand`.

The command itself does not own the selection.

This separation is useful because undoing a command should restore document geometry, but not necessarily restore UI selection state unless that is explicitly supported later.

Currently, commands focus on document state, not selection state.

---

## Commands and entity immutability

Entities are treated as immutable objects in most operations.

Instead of modifying a line in place, a transform command creates a transformed line entity and replaces the old one in the document.

This makes undo easier because the command can store the original entity and restore it later.

It also avoids accidental partial updates.

---

## Commands and identifiers

Entity identifiers are important.

Move, rotate, scale and mirror keep the same entity identifiers because they modify existing entities.

Copy creates new identifiers because it creates new entities.

Delete removes existing identifiers from the document, and undo restores the original entities with their original identifiers.

Redo should be deterministic. A command that generated new entities during first execution should normally reuse the same generated entities during redo.

---

## Direct document modification

Direct modification of `CadDocument` is acceptable in limited cases:

```text
tests
initialization
demo seed data
low-level setup
```

For user-facing editing operations, direct modification should be avoided.

A good rule is:

```text
If the user may expect undo, use a command.
```

---

## Undo and redo behavior

Undo should restore the document to the state before the command was executed.

Redo should reapply the same operation.

A command should not rely on UI state when undoing or redoing.

For example, `DeleteEntitiesCommand.Undo` should restore deleted entities even if the current selection has changed.

Commands should be self-contained enough to undo their own effects.

---

## Error handling

Commands should validate invalid input early.

Commands receiving entity ids should reject empty lists when an empty operation would be meaningless.

Commands should fail clearly when required entities are not found.

Commands should fail through document-level validation when the operation violates document rules, such as future locked-layer restrictions.

---

## Testing commands

Commands are easy to test because they do not depend on the UI.

A command test usually follows this pattern:

```text
create document
add initial entities
execute command
assert document state
undo command
assert original state
redo command if needed
assert modified state again
```

Command tests should verify normal behavior and edge cases.

Important cases include:

```text
deleted entities are restored by undo
copied entities have different ids from originals
copy redo reuses the same generated ids
transform undo restores original geometry
composite command is one undo step
commands mutate through CadDocument
```

---

## Guidelines for new commands

A new command should:

```text
implement ICadCommand
have a clear name
validate constructor parameters
store enough information to undo itself
avoid Avalonia and UI concepts
avoid mouse or keyboard state
modify entities only through CadDocument
preserve identifiers when modifying existing entities
create new identifiers when creating new entities
store deleted entities for undo
have focused tests for execute, undo and redo
```

For multi-step operations, prefer `CompositeCommand` over manually performing multiple unrelated document mutations.

---

## Future improvements

The command system can be extended in several useful ways.

Possible future improvements:

```text
command descriptions for UI/history panels
document dirty-state tracking
selection-state undo, if explicitly needed
full document transaction or ChangeSetCommand
command serialization for macros or scripting
```

A full document transaction system is not required yet. `CompositeCommand` is the current lightweight solution for grouping operations atomically.
