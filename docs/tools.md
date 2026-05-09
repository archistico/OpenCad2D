# Tools

The tool system is the layer that turns user intent into CAD operations.

A tool represents something the user can do in the drawing area: select entities, draw a line, draw a rectangle, move selected entities, copy them or delete them.

Tools are not part of the UI. They do not depend on Avalonia and do not handle Avalonia events directly. They work with model/user coordinates and with the CAD runtime context.

This keeps tools testable and keeps the UI thin.

---

## Main idea

The UI receives mouse and keyboard input.

The canvas converts screen coordinates into model coordinates and user coordinates.

Then the input is forwarded to the active CAD tool.

The tool decides what to do.

For example, when `LineTool` receives the first pointer press, it stores the first point. When it receives the second pointer press, it creates a `LineEntity` and executes an `AddEntityCommand`.

The UI does not create the line directly. It only forwards input and renders the result.

---

## PointerInfo

`PointerInfo` represents pointer input after the UI has converted it into CAD coordinates.

It contains:

```text
ModelPoint   point in WCS/model coordinates
UserPoint    point in the current UCS
Modifiers    Shift, Control, Alt
```

This is important because tools should not know about the viewport or Avalonia coordinates.

The canvas owns screen-to-model conversion. The current UCS owns model-to-user conversion.

Existing tools may continue to use `ModelPoint`. Future tools can use `UserPoint` for UCS-aware input and coordinate display.

---

## ToolContext

`ToolContext` is the shared runtime context used by tools.

It used to expose many unrelated properties directly. To avoid becoming a God Object, it is now organized into focused sub-contexts.

Current structure:

```text
ToolContext
  Document
  Commands
  Selection
  Snapping
  Coordinates
  Creation
```

New tool code should prefer these grouped contexts.

---

## ToolCommandContext

`ToolCommandContext` provides command execution services.

It wraps the command history and exposes operations such as:

```text
Execute
Undo
Redo
CanUndo
CanRedo
```

Typical usage:

```csharp
context.Commands.Execute(
    context.Document,
    new AddEntityCommand(entity));
```

Tools should use this instead of directly working with `CommandHistory`.

---

## ToolSelectionContext

`ToolSelectionContext` provides selection state, selection services and selection settings.

It contains:

```text
Set             SelectionSet
Service         SelectionService
Tolerance       point selection tolerance
DragThreshold   threshold before window selection starts
HasSelection
SelectedIds
```

Typical usage:

```csharp
if (!context.Selection.HasSelection)
{
    return ToolResult.None("No entities selected.");
}

IReadOnlyList<EntityId> ids = context.Selection.SelectedIds.ToList();
```

---

## ToolSnapContext

`ToolSnapContext` provides snapping services and settings.

It contains:

```text
Service        SnapService
EnabledSnaps   enabled snap flags
Tolerance      snap tolerance
GridSettings   grid snapping configuration
```

Two-point tools use snapping through their shared base class.

---

## ToolCoordinateContext

`ToolCoordinateContext` provides coordinate and precision settings.

It contains:

```text
CurrentUcs
GeometryTolerance
```

New geometric decisions inside tools should use `context.Coordinates.GeometryTolerance` rather than raw magic numbers.

---

## ToolCreationContext

`ToolCreationContext` contains defaults used when tools create new entities.

Currently it contains:

```text
CurrentLayerId
```

In the future it can grow to include:

```text
current color mode
current line weight
current line type
current text style
current dimension style
```

This avoids adding many unrelated `Current...` properties directly to `ToolContext`.

---

## ToolContext boundary

`ToolContext` provides only model-side services required by CAD tools.

It may contain:

```text
active document
undoable command execution
selection state and selection services
snapping services and snapping settings
entity creation defaults
current coordinate system
geometry tolerance
```

It must not contain:

```text
Avalonia controls
viewport or screen-to-model conversion logic
dialogs, message boxes or status bar services
file system, persistence or export services
rendering services
application-level configuration unrelated to tool execution
```

Pointer coordinates must be converted before entering tools.

---

## ICadTool

`ICadTool` is the common interface implemented by all tools.

It exposes the basic lifecycle:

```text
OnPointerPressed
OnPointerMoved
OnPointerReleased
Cancel
Deactivate
```

`OnPointerPressed`, `OnPointerMoved` and `OnPointerReleased` receive `PointerInfo`.

`Cancel` is used when the user explicitly cancels the current operation, usually with `Esc`.

`Deactivate` is used when the current tool is replaced by another tool.

The distinction between `Cancel` and `Deactivate` is important.

---

## Cancel, Deactivate and Escape

`Cancel` and `Deactivate` are intentionally different.

Cancel means that the user wants to cancel the current operation.

Deactivate means that the application is switching from one tool to another.

For example, changing from `SelectionTool` to `MoveTool` must not clear the current selection. Otherwise the move operation would have nothing to work on.

`Esc` behavior is layered:

```text
first Esc   cancels the active tool operation if one is in progress
second Esc  clears selection if no tool operation is active
```

This behavior belongs to `CadWorkspace.Escape()` or equivalent workspace-level logic, not to Avalonia event handlers.

---

## ToolResult

Tools return a `ToolResult`.

A result describes what happened after an input event.

Common result kinds include:

```text
None
Started
Updated
Completed
Cancelled
```

The result can contain a message.

The UI can use this message in the status bar. This gives feedback without coupling tools to the UI.

---

## TwoPointToolBase

Many CAD tools follow the same pattern:

```text
first point
preview while moving
second point
execute operation
```

Examples:

```text
LineTool
RectangleTool
MoveTool
CopyTool
```

`TwoPointToolBase` implements this shared behavior.

It stores the first point, updates the current point while the pointer moves, applies snapping and resets the tool after completion.

Derived tools only define what happens when the two points are known.

`Cancel` should return `ToolResult.None()` if there is no active first point/current operation. This allows `Esc` to move on to selection clearing only when there is nothing left to cancel.

---

## LineTool

`LineTool` creates a `LineEntity`.

The first point is stored after the first click.

The second point creates a line from the first point to the second point.

The created line uses `context.Creation.CurrentLayerId`.

The operation is executed through `AddEntityCommand`, so it is undoable.

---

## RectangleTool

`RectangleTool` creates a closed `PolylineEntity`.

The first point is one corner of the rectangle.

The second point is the opposite corner.

The rectangle is represented by four vertices and `IsClosed = true`.

Rectangle validity should use `context.Coordinates.GeometryTolerance` so that nearly-zero width or height is rejected consistently.

Like `LineTool`, it creates the entity through `AddEntityCommand`.

---

## SelectionTool

`SelectionTool` modifies the `SelectionSet` through the tool selection context.

It supports:

```text
point selection
shift-click toggle
window selection
crossing selection
```

Point selection is applied on pointer release, not pointer press. This avoids accidentally selecting by click before a drag window is completed.

When the user drags from left to right, the selection mode is window selection. Entities must be fully inside the selection rectangle.

When the user drags from right to left, the selection mode is crossing selection. Entities only need to intersect the selection rectangle.

Hidden layer entities are ignored by selection through document visibility rules.

---

## MoveTool

`MoveTool` moves selected entities.

It uses the first point as the base point and the second point as the destination point.

The displacement is:

```text
secondPoint - firstPoint
```

Then the tool executes `MoveEntitiesCommand` through `context.Commands`.

The selected entities keep their identifiers, but their geometry is transformed.

Because the operation is command-based, it can be undone and redone.

---

## CopyTool

`CopyTool` copies selected entities.

It uses the same two-point displacement logic as `MoveTool`.

The difference is that copied entities receive new identifiers.

The original entities remain unchanged.

The operation is executed through `CopyEntitiesCommand`, so it can be undone.

Preview copies may use temporary ids because they are not inserted into the document.

---

## DeleteTool

`DeleteTool` deletes the current selection.

Unlike line, rectangle, move and copy, delete is not a two-point operation.

After deleting selected entities, it clears the selection.

The operation is executed through `DeleteEntitiesCommand`, so undo restores the deleted entities.

---

## ToolController

`ToolController` owns the active tool.

The UI should not call concrete tools directly in most cases. It should call the controller:

```text
OnPointerPressed
OnPointerMoved
OnPointerReleased
CancelActiveTool
SetActiveTool
```

When the active tool changes, the controller deactivates the previous tool.

This keeps tool switching behavior consistent.

---

## ToolRegistry

`ToolRegistry` is a small factory and catalog for tools.

It maps a `ToolId` to a concrete tool instance.

Example:

```text
ToolId.Selection   -> SelectionTool
ToolId.Line        -> LineTool
ToolId.Rectangle   -> RectangleTool
ToolId.Move        -> MoveTool
ToolId.Copy        -> CopyTool
```

This avoids creating tools directly from the UI.

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

These actions can be triggered from toolbar buttons, menu items or keyboard shortcuts.

---

## CadWorkspace

`CadWorkspace` aggregates runtime objects needed by the application.

It owns or exposes:

```text
CadDocument
CommandHistory
SelectionSet
SnapService
SelectionService
ToolRegistry
ToolContext
ToolController
CadActionController
current UCS
geometry tolerance
current layer
```

The Avalonia application can create one workspace and use it as the central runtime object.

Workspace-level behavior such as `Escape()` is useful when the behavior spans multiple concepts, such as tool cancellation followed by selection clearing.

---

## Snapping inside tools

Tools do not manually calculate every snap.

`TwoPointToolBase` applies snapping through `SnapService`.

When a tool already has a first point, that point is passed as `BasePoint` in the snap request.

This is important for contextual snaps such as perpendicular and tangent.

---

## Preview behavior

Some tools expose preview entities.

Examples:

```text
LineTool       preview line
RectangleTool  preview rectangle
MoveTool       transformed selected entities
CopyTool       copied selected entities
```

The UI is responsible for rendering previews. Tools only provide temporary geometry.

This keeps rendering separate from tool behavior.

---

## Active command feedback

The UI should always show which command/tool is active.

This is not part of the tool logic itself, but tools provide enough state through `ToolController` and `MainWindowViewModel` for the UI to display:

```text
active toolbar button
active command label
window title/status text
```

For modal tools such as line, rectangle, move and copy, this feedback is important because the next click depends on the active tool.

---

## Testing tools

Tools are designed to be tested without running Avalonia.

A test can create a document, command history, selection set and tool context.

Then it can send pointer events to a tool and verify document state.

Examples:

```text
LineTool creates a LineEntity after two clicks
RectangleTool rejects zero-size rectangles
MoveTool moves selected entities
CopyTool creates new entities
SelectionTool selects visible entities only
Esc cancels an active two-point operation
```

---

## Guidelines for new tools

A new tool should:

```text
stay UI-independent
work in model/user coordinates
use ToolContext sub-contexts
execute commands for document changes
use CadDocument mutation through commands
use GeometryTolerance for geometric validity checks
use snapping through the existing snapping flow
preserve selection unless explicitly cancelling selection
return meaningful ToolResult messages
have focused tests
```

If it follows the first-point / second-point pattern, it should probably derive from `TwoPointToolBase`.

If it performs a complex multi-entity operation, it should create a `CompositeCommand` or a focused command that internally uses document-level mutation APIs.
