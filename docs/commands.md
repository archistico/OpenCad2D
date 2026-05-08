# Commands

Commands are the mechanism used by OpenCad2D to modify the document in a controlled and undoable way.

A CAD application needs reliable undo and redo from the beginning. For this reason, user-facing operations should not directly mutate the document from the UI. Instead, they should be represented by command objects.

A command knows how to execute an operation and how to undo it.

---

## Main idea

When the user performs an operation, such as drawing a line, moving entities or deleting selected objects, the tool creates a command.

The command is executed through `CommandHistory`.

`CommandHistory` executes the command and stores it on the undo stack.

If the user runs undo, `CommandHistory` calls the command's `Undo` method.

If the user runs redo, `CommandHistory` executes the command again.

This keeps document changes predictable and centralized.

---

## ICadCommand

All commands implement `ICadCommand`.

A command exposes a name and two operations:

```text
Execute
Undo
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

## AddEntityCommand

`AddEntityCommand` adds one or more entities to the document.

It is used by drawing tools such as `LineTool` and `RectangleTool`.

When executed, it adds the entities to the document.

When undone, it removes the same entities by id.

This command is simple but fundamental, because every drawing operation starts from adding new entities.

---

## DeleteEntitiesCommand

`DeleteEntitiesCommand` deletes existing entities from the document.

Before deleting, it stores the entities that will be removed.

This is necessary because undo must be able to restore the exact original entities.

When executed, the command removes the selected entities.

When undone, it adds the previously deleted entities back to the document.

This command is used by `DeleteTool`.

---

## ReplaceEntitiesCommand

`ReplaceEntitiesCommand` replaces existing entities with new versions.

It is useful when an operation changes the geometry or properties of existing entities while keeping their identifiers.

Before replacing, it stores the original entities.

When undone, the original entities are restored.

This command is useful as a general mechanism for entity modification.

---

## TransformEntitiesCommand

`TransformEntitiesCommand` applies a transformation matrix to one or more entities.

It is the base command for operations such as move, rotate, scale and mirror.

When executed, it stores the original entities and replaces them with transformed versions.

When undone, it restores the original entities.

The command itself does not decide what transformation means. It simply applies the provided `Matrix2D`.

---

## MoveEntitiesCommand

`MoveEntitiesCommand` moves selected entities by a displacement vector.

Internally, it uses a translation matrix.

The selected entities keep their identifiers.

Undo restores the original positions.

This command is used by `MoveTool`.

---

## CopyEntitiesCommand

`CopyEntitiesCommand` creates translated copies of selected entities.

Unlike move, copy does not modify the original entities.

When executed, it creates new entities with new identifiers.

When undone, it removes the copied entities.

This behavior is important: copied entities must not reuse the identifiers of the original entities.

---

## RotateEntitiesCommand

`RotateEntitiesCommand` rotates selected entities around a center point by an angle.

It is based on a rotation matrix.

The command keeps the same entity identifiers and replaces the entities with rotated versions.

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

The operation replaces the selected entities with mirrored versions while keeping their identifiers.

Undo restores the original entities.

---

## Commands and tools

Tools should generally not modify the document directly.

A tool interprets user input and creates the appropriate command.

For example, `LineTool` waits for two points. When the second point is selected, it creates a `LineEntity` and executes an `AddEntityCommand`.

`MoveTool` waits for a base point and a destination point. It calculates the displacement and executes a `MoveEntitiesCommand`.

`DeleteTool` reads the current selection and executes a `DeleteEntitiesCommand`.

This keeps tool logic focused on interaction and keeps document mutation inside commands.

---

## Commands and selection

Commands operate on entities in the document.

Selection is stored separately in `SelectionSet`.

For example, `MoveTool` reads selected ids from `SelectionSet`, then passes those ids to `MoveEntitiesCommand`.

The command itself does not own the selection.

This separation is useful because undoing a command should restore document geometry, but not necessarily restore UI selection state unless we explicitly decide to support that later.

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

This behavior should remain consistent as new commands are added.

---

## Direct document modification

Direct modification of `CadDocument` is acceptable in some cases, such as tests, initialization and demo seed data.

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

For example, commands that receive a list of entity ids should reject empty lists when an empty operation would be meaningless.

Commands should fail clearly when required entities are not found.

This makes bugs easier to diagnose during development.

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

Command tests should verify both normal behavior and edge cases.

For example, delete command tests should verify that deleted entities are restored by undo. Copy command tests should verify that copied entities have different identifiers from the originals.

---

## Guidelines for new commands

A new command should implement `ICadCommand`.

It should only do one clear operation.

It should validate constructor parameters.

It should store enough information to undo itself.

It should not depend on Avalonia or any UI concept.

It should not read mouse input or keyboard state.

It should modify the document only through the document API.

If it transforms entities, it should preserve identifiers unless the operation creates new entities.

If it creates new entities, it should assign new identifiers.

If it deletes entities, it should store the deleted entities for undo.

The command should have focused tests for execute, undo and redo behavior.

---

## Future improvements

The command system can be extended in several useful ways.

One improvement is command grouping. Some complex operations may need to execute several commands but appear as one undo step.

Another improvement is command descriptions for the UI, so the status bar or future command history panel can show more meaningful messages.

Selection state could also become undoable in the future, although this should be handled carefully because selection is UI state, while commands currently focus on document state.

A future save system may use command history to detect whether the document has unsaved changes.

