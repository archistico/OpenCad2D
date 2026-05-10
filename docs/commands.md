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
- Circle;
- Polyline;
- future Arc/Text/Dimension tools.

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

It should store:

```text
old layer collection snapshot
new layer collection snapshot
old current layer
new current layer
```

Execute applies the new layer state. Undo restores the old state.

After execution or undo, selection should be revalidated because hidden or locked layers can make selected entities invalid.

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

## Future Break/Trim/Extend commands

The next modify tools should not mutate entities directly.

Recommended command approach:

### Break

Split one entity into two or more entities.

Potential command composition:

```text
Remove original entity
Add resulting entity pieces
```

or a dedicated command storing original and resulting pieces.

### Trim

Replace or remove parts of an entity based on cutting boundaries.

Should likely use `CompositeCommand` because a trim can remove a segment, replace an entity, or in some cases produce multiple resulting pieces.

### Extend

Replace an entity with an extended version that reaches a boundary.

Can usually be a `ReplaceEntitiesCommand` or dedicated extension command.

All three must respect locked-layer protection by using `CadDocument` mutation APIs.
