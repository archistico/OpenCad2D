# Tools

The tool system is the layer that turns user intent into CAD operations.

A tool represents something the user can do in the drawing area: select entities, draw a line, draw a rectangle, move selected entities, copy them or delete them.

The most important architectural decision is that tools are not part of the UI. They do not depend on Avalonia and they do not handle Avalonia events directly. They work with model-space data and with the CAD runtime context.

This makes tools easy to test and keeps the UI thin.

---

## Main idea

The UI receives mouse and keyboard input.

The canvas converts screen coordinates into model coordinates.

Then the input is forwarded to the active CAD tool.

The tool decides what to do.

For example, when the `LineTool` receives the first pointer press, it stores the first point. When it receives the second pointer press, it creates a `LineEntity` and executes an `AddEntityCommand`.

The UI does not create the line directly. It only forwards input.

---

## ToolContext

`ToolContext` is the shared runtime context used by tools.

It gives tools access to the objects they need in order to work:

```text
CadDocument
CommandHistory
SelectionSet
SelectionService
SnapService
GridSettings
CurrentLayerId
EnabledSnaps
SnapTolerance
SelectionTolerance
SelectionDragThreshold
```

A tool should not create its own document, command history or selection set. It should use the ones provided by the context.

This keeps all tools working on the same drawing state.

---

## ToolContext boundary

`ToolContext` provides only model-side services required by CAD tools.

It may contain:
- the active document;
- undoable command execution;
- selection state and selection services;
- snapping services and snapping settings;
- current entity creation defaults;
- current coordinate system and geometry tolerance.

It must not contain:
- UI controls;
- viewport or screen-to-model conversion logic;
- dialogs, message boxes or status bar services;
- file system, persistence or export services;
- rendering services;
- application-level configuration unrelated to tool execution.

Pointer coordinates must be converted before entering tools.
Tools receive model/user coordinates through `PointerInfo`.

--- 

## ICadTool

`ICadTool` is the common interface implemented by all tools.

It exposes the basic pointer lifecycle:

```text
OnPointerPressed
OnPointerMoved
OnPointerReleased
Cancel
Deactivate
```

`OnPointerPressed`, `OnPointerMoved` and `OnPointerReleased` receive model-space pointer information through `PointerInfo`.

`Cancel` is used when the user explicitly cancels the current operation, usually with `Esc`.

`Deactivate` is used when the current tool is replaced by another tool.

The distinction between `Cancel` and `Deactivate` is important.

---

## Cancel vs Deactivate

`Cancel` and `Deactivate` are intentionally different.

Cancel means that the user wants to cancel the current tool operation.

Deactivate means that the application is switching from one tool to another.

For example, `SelectionTool.Cancel()` clears the current selection because pressing `Esc` while selecting should cancel the selection state.

However, `SelectionTool.Deactivate()` does not clear the selection. This is important because a common workflow is:

```text
Select an entity
Switch to Move
Move the selected entity
```

If changing from `SelectionTool` to `MoveTool` cleared the selection, the move operation would have nothing to work on.

This is why `ToolController.SetActiveTool(...)` calls `Deactivate`, not `Cancel`.

---

## PointerInfo

`PointerInfo` represents input in model coordinates.

It contains the current model-space point and keyboard modifiers such as Shift, Control and Alt.

The UI is responsible for converting screen coordinates to model coordinates before creating `PointerInfo`.

This keeps tools independent from the viewport and from the UI framework.

---

## ToolResult

Tools return a `ToolResult`.

A result describes what happened after an input event.

Current result kinds include:

```text
None
Started
Updated
Completed
Cancelled
```

The result can also contain a message.

The UI can use this message in the status bar. For example, after creating a line, `LineTool` can return:

```text
Line created.
```

This gives the user feedback without coupling tools to the UI.

---

## TwoPointToolBase

Many CAD tools follow the same pattern:

```text
first point
preview while moving
second point
execute operation
```

Examples are line, rectangle, move and copy.

`TwoPointToolBase` implements this shared behavior.

It stores the first point, updates the current point while the pointer moves, applies snapping and resets the tool after completion.

Derived tools only need to define what happens when the two points are known.

For example, `LineTool` creates a line. `MoveTool` computes a displacement. `CopyTool` computes the same displacement but creates copied entities instead of replacing the originals.

This avoids duplicating the same state machine in every two-point tool.

---

## LineTool

`LineTool` creates a `LineEntity`.

The first point is stored after the first click.

The second point creates a line from the first point to the second point.

The created line uses the current layer from `ToolContext.CurrentLayerId`.

The operation is executed through `AddEntityCommand`, so it is undoable.

---

## RectangleTool

`RectangleTool` creates a closed `PolylineEntity`.

The first point is one corner of the rectangle.

The second point is the opposite corner.

The rectangle is represented by four vertices and `IsClosed = true`.

Like `LineTool`, it uses the current layer and creates the entity through `AddEntityCommand`.

---

## SelectionTool

`SelectionTool` modifies the `SelectionSet`.

It supports point selection, shift-click toggle, window selection and crossing selection.

The tool uses `SelectionService` to determine which entities are selected.

Point selection is applied on pointer release, not on pointer press. This avoids a common issue where a drag operation could accidentally trigger a click selection before the selection window is completed.

When the user drags from left to right, the selection mode is window selection. Entities must be fully inside the selection rectangle.

When the user drags from right to left, the selection mode is crossing selection. Entities only need to intersect the selection rectangle.

---

## MoveTool

`MoveTool` moves the currently selected entities.

It uses the first point as the base point and the second point as the destination point.

The displacement is calculated as:

```text
secondPoint - firstPoint
```

Then the tool executes a `MoveEntitiesCommand`.

The selected entities keep their identifiers, but their geometry is transformed.

Because the operation is command-based, it can be undone and redone.

---

## CopyTool

`CopyTool` copies the currently selected entities.

It uses the same two-point displacement logic as `MoveTool`.

The difference is that copied entities receive new identifiers.

The original entities remain unchanged.

The operation is executed through `CopyEntitiesCommand`, so it can be undone.

---

## DeleteTool

`DeleteTool` deletes the current selection.

Unlike line, rectangle, move and copy, delete is not really a two-point operation.

It can be executed directly through its `Execute` method, or through `OnPointerPressed` when used as a normal tool.

After deleting the selected entities, it clears the selection.

The operation is executed through `DeleteEntitiesCommand`, so undo restores the deleted entities.

---

## ToolController

`ToolController` owns the active tool.

The UI should not call tools directly in most cases. It should call the controller:

```text
OnPointerPressed
OnPointerMoved
OnPointerReleased
CancelActiveTool
SetActiveTool
```

The controller forwards events to the active tool.

When the active tool changes, the controller deactivates the previous tool.

This keeps tool switching behavior consistent.

---

## ToolRegistry

`ToolRegistry` is a small factory and catalog for tools.

It maps a `ToolId` to a concrete tool instance.

For example:

```text
ToolId.Line       -> LineTool
ToolId.Rectangle  -> RectangleTool
ToolId.Move       -> MoveTool
```

This avoids creating tools directly from the UI.

Instead of writing:

```csharp
new LineTool()
```

the application can use:

```csharp
registry.Create(ToolId.Line)
```

This keeps the UI less dependent on concrete tool classes.

---

## CadActionController

`CadActionController` centralizes global actions.

It handles operations such as:

```text
Undo
Redo
DeleteSelection
CancelActiveTool
```

This is useful because these actions are usually triggered from different UI places: toolbar buttons, menu items or keyboard shortcuts.

For example, the UI can map `Ctrl+Z` to `ActionController.Undo()` and `Delete` to `ActionController.DeleteSelection()`.

---

## CadWorkspace

`CadWorkspace` aggregates the runtime objects needed by the application.

It owns or exposes the document, command history, selection set, snap service, selection service, tool registry, tool context, tool controller and action controller.

The Avalonia application can create one workspace and use it as the central runtime object.

This keeps application startup simple.

---

## Snapping inside tools

Tools do not manually calculate every snap.

`TwoPointToolBase` applies snapping through `SnapService`.

When a tool already has a first point, that point is passed as `BasePoint` in the snap request.

This is important for contextual snaps such as perpendicular and tangent.

For example, after the first click of `LineTool`, the first point becomes the base point. The second point can then snap to a perpendicular or tangent point.

---

## Preview behavior

Some tools expose preview entities.

`LineTool` can return a preview line.

`RectangleTool` can return a preview rectangle.

`MoveTool` and `CopyTool` can return preview entities transformed by the current displacement.

The UI is responsible for rendering previews. The tools only provide the temporary geometry.

This keeps rendering separate from tool behavior.

---

## Testing tools

Tools are designed to be tested without running Avalonia.

A test can create a `CadDocument`, a `CommandHistory`, a `SelectionSet` and a `ToolContext`.

Then it can send pointer events to a tool and verify the resulting document state.

For example, a `LineTool` test can send two pointer presses and assert that a `LineEntity` was created.

A `MoveTool` test can select an entity, send two pointer presses and assert that the entity moved.

This testability is one of the main reasons why tools are UI-independent.

---

## Guidelines for new tools

A new tool should stay UI-independent.

It should work in model coordinates.

It should use `ToolContext` instead of creating its own services.

If it changes the document, it should usually execute a command.

If it follows the first-point / second-point pattern, it should probably derive from `TwoPointToolBase`.

If it needs selection, it should read selected identifiers from `SelectionSet`.

If it needs snapping, it should rely on the existing snapping flow.

The UI should not contain the tool logic. It should only activate the tool, forward input and render the result.

