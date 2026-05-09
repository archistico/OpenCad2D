# Tools

The tool system is the layer that turns user intent into CAD operations.

A tool represents something the user can do in the drawing area: select entities, draw a line, draw a rectangle, move selected entities, copy them or delete them.

Tools are not part of the UI. They do not depend on Avalonia and do not handle Avalonia events directly. They work with model/user coordinates and with the CAD runtime context.

This keeps tools testable and keeps the UI thin.

---

## Main idea

The UI receives mouse and keyboard input. The canvas converts screen coordinates into model coordinates and user coordinates. Then the input is forwarded to the active CAD tool.

The tool decides what to do.

For example, when `LineTool` receives the first pointer press, it stores the first point. When it receives the second pointer press, it creates a `LineEntity` and executes an `AddEntityCommand`.

The UI does not create the line directly. It only forwards input and renders the result.

---

## PointerInfo

`PointerInfo` represents pointer input after the UI has converted it into CAD coordinates.

It contains:

```text
ModelPoint  point in WCS/model coordinates
UserPoint   point in the current UCS
Modifiers   Shift, Control, Alt
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
Set            SelectionSet
Service        SelectionService
Tolerance      point selection tolerance
DragThreshold  threshold before window selection starts
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

Selection must be based on selectable entities, not merely visible entities.

```text
Hidden layer entity       -> not selectable
Locked layer entity       -> not selectable
Visible unlocked entity   -> selectable
```

---

## ToolSnapContext

`ToolSnapContext` provides snapping services and settings.

It contains:

```text
Service       SnapService
EnabledSnaps  enabled snap flags
Tolerance     snap tolerance
GridSettings  grid snapping configuration
```

Two-point tools use snapping through their shared base class.

Snapping uses visible entities. This intentionally includes entities on locked layers, because locked geometry may still be used as a reference.

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
first Esc cancels the active tool operation if one is in progress
second Esc clears selection if no tool operation is active
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

The result can contain a message. The UI can use this message in the status bar. This gives feedback without coupling tools to the UI.

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

`TwoPointToolBase` implements this shared behavior. It stores the first point, updates `ToolContext.CurrentBasePoint`, updates the current point while the pointer moves, applies snapping and resets the tool after completion.

`TwoPointToolBase` should not know the specific operation. Derived classes decide what happens when the second point is chosen.

Because it maintains `CurrentBasePoint`, all derived tools can benefit from command line relative input, direct distance entry and temporary measurement feedback.

---

## Command line input for tools

Command line input is a tool input mechanism, not a separate entity creation system.

Supported formats:

```text
100,50   absolute UCS point
@50,0    relative UCS offset from CurrentBasePoint
5        direct distance from CurrentBasePoint along the current cursor direction
```

The parser produces a structured result. The ViewModel resolves that result into a WCS `Point2D`, then calls `CadWorkspace.SubmitPointFromCommandLine(...)`.

The active tool should receive that point exactly like a mouse click. This is important because the same workflow must work for:

```text
LineTool
RectangleTool
MoveTool
CopyTool
future CircleTool
future ArcTool
future PolylineTool
```

Important rule:

```text
The command line supplies points.
The active tool decides what those points mean.
Commands still perform document mutation.
```

Direct distance entry requires a base point and a direction. The base point comes from `ToolContext.CurrentBasePoint`; the direction comes from the current mouse/snap position.

---

## SelectionTool

`SelectionTool` handles point, window and crossing selection.

Selection is stored as entity ids in `SelectionSet`.

Supported behavior:

```text
click                select by point
Shift + click        toggle selection
left-to-right drag   window selection
right-to-left drag   crossing selection
```

Hidden layer entities are ignored by selection through document visibility rules.

Locked layer entities are also ignored by selection. This is handled through selectable-entity queries, not by checking Avalonia UI state.

```text
Hidden layer entity       -> not selectable
Locked layer entity       -> not selectable
Visible unlocked entity   -> selectable
```

The selection tool must not modify or delete selected entities. It only manages `SelectionSet`.

---

## LineTool

`LineTool` creates a `LineEntity` using two points.

Expected flow:

```text
first click   stores start point
mouse move    updates preview and measurement
second click  creates LineEntity and executes AddEntityCommand
```

The new entity should use the current creation context, especially `CurrentLayerId`.

---

## RectangleTool

`RectangleTool` creates a rectangular closed polyline or equivalent rectangle entity, depending on the current implementation.

Expected flow:

```text
first click   stores first corner
mouse move    updates preview and measurement
second click  creates rectangle geometry and executes AddEntityCommand
```

The tool should create geometry in model coordinates and should not depend on screen-space values.

---

## MoveTool

`MoveTool` transforms selected entities by displacement.

Expected flow:

```text
requires selection
first click   base point
mouse move    preview displacement and vector measurement
second click  execute MoveEntitiesCommand
```

The command must replace entities through `CadDocument`, not mutate geometry in place.

Entities on locked layers should normally never reach this workflow because they are not selectable. `CadDocument` still blocks replacement if a future workflow accidentally attempts it.

---

## CopyTool

`CopyTool` creates copied entities by displacement.

Expected flow:

```text
requires selection
first click   base point
mouse move    preview displacement and vector measurement
second click  execute CopyEntitiesCommand
```

Copying does not modify the source entities. However, locked-layer entities are not selectable, so the normal UI workflow cannot copy them through selection.

A future explicit copy-from-reference workflow would need a clear rule for whether locked-layer source entities are allowed.

---

## DeleteTool

`DeleteTool` or delete actions remove selected entities through `DeleteEntitiesCommand`.

Expected behavior:

```text
read selected ids
create DeleteEntitiesCommand
execute through command context
clear or update selection as needed
```

The command must remove entities through `CadDocument.RemoveEntities(...)`.

Locked-layer entities are not selectable, so normal deletion should not target them. `CadDocument` still protects itself and rejects removal of locked-layer entities.

---

## Layer locking from the workspace

Layer locking is coordinated by `CadWorkspace`.

The UI can request that the current layer is locked or unlocked, but the UI should not decide which selected entities remain valid. That logic belongs to the workspace and document model.

When the current layer is locked:

```text
CadWorkspace.SetCurrentLayerLocked(true)
LayerCollection.SetLocked(...)
CadWorkspace.ClearSelectionOfNonSelectableEntities()
```

This ensures that any selected entity that has become non-selectable is immediately removed from `SelectionSet`.

Important rule:

```text
The UI toggles layer state.
The workspace cleans selection state.
The document enforces mutation rules.
```

Tools should not assume that selection is always valid forever. If layer state changes, selection must be revalidated against `CadDocument.IsEntitySelectable(...)`.

---

## Preview behavior

Tools may expose preview geometry so that the UI can render it. Two-point tools also expose enough state through `CurrentBasePoint` and current pointer/snap position for the UI to draw temporary measurement feedback.

The preview belongs conceptually to the tool state, but rendering belongs to `OpenCad2D.App`.

A tool may say “this is the preview entity” or “this is the current rectangle preview”, but it should not draw it directly. Temporary base-point markers and vector lines are also rendered by the App layer, not by the tool.

---

## Adding a new tool

When adding a new tool, follow this checklist:

1. Add the tool class in `OpenCad2D.Tools`.
2. Keep it independent from Avalonia.
3. Use `PointerInfo` for input.
4. Use `ToolContext` sub-contexts.
5. Use snapping when appropriate.
6. Create commands for document modifications.
7. Add focused tests in `OpenCad2D.Tools.Tests`.
8. Register the tool in `ToolRegistry`.
9. Add UI buttons or shortcuts in `OpenCad2D.App` only after the model-side tool works.

---

## Tool design rules

Preserve these rules:

```text
Tools do not depend on Avalonia.
Tools do not render.
Tools do not open dialogs.
Tools receive model/user coordinates.
Tools use commands for undoable mutations.
Tools do not mutate EntityCollection directly.
Tools should preserve selection on Deactivate.
Tools should cancel in-progress operations on Cancel.
```
