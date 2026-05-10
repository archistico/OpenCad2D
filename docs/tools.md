# Tools

The tool system is the layer that turns user intent into CAD operations.

A tool represents something the user can do in the drawing area: select entities, draw a line, draw a rectangle, draw a circle, edit grips, move selected entities, copy them or delete them.

Tools are not part of the UI. They do not depend on Avalonia and do not handle Avalonia events directly. They work with model/user coordinates and with the CAD runtime context.

This keeps tools testable and keeps the UI thin.

---

## Main idea

The UI receives mouse and keyboard input. The canvas converts screen coordinates into model coordinates and user coordinates. Then the input is forwarded to the active CAD tool.

The tool decides what to do.

For example, when `LineTool` receives the first point, it stores the first point. When it receives the second point, it creates a `LineEntity` and executes an `AddEntityCommand`.

The UI does not create the line directly. It only forwards input and renders the result.

The same rule applies to command line input: the UI resolves typed input to a CAD point and forwards that point to the active tool. It does not create entities directly.

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

---

## Tool lifecycle

A typical tool supports some or all of these operations:

```text
Activate
PointerPressed
PointerMoved
Cancel
Deactivate
```

Tools return `ToolResult` values to communicate what happened.

A result may say that:

- nothing changed;
- the document changed;
- the preview changed;
- the active operation completed;
- a user-facing message should be displayed.

---

## ToolContext

`ToolContext` gives tools access to the CAD runtime state.

It contains access to:

- document;
- command history;
- selection;
- snapping;
- current layer;
- current UCS;
- geometry tolerance;
- grid settings;
- input-related runtime state.

The important rule is that `ToolContext` must stay UI-independent.

It must not contain Avalonia controls, windows, dialogs, message boxes or viewport objects.

### CurrentBasePoint

`CurrentBasePoint` is a nullable model point representing the first accepted point of a two-point operation.

It is used by:

- command line relative input;
- direct distance entry;
- contextual snaps;
- temporary base-point marker;
- temporary vector feedback;
- temporary `L`, `DX`, `DY` measurements.

A two-point tool should set `CurrentBasePoint` after accepting its first point and clear it after completing or cancelling the operation.

### IsOrthoEnabled

`IsOrthoEnabled` stores whether Ortho mode is active.

When enabled, second-point workflows are constrained to the closest horizontal or vertical direction from `CurrentBasePoint`.

---

## TwoPointToolBase

`TwoPointToolBase` is the common base for tools that need two points.

The pattern is:

```text
first point  -> store base point
second point -> complete operation
```

Current tools using or following this model include:

- `LineTool`;
- `RectangleTool`;
- `CircleTool`;
- `GripEditTool`;
- `MoveTool`;
- `CopyTool`.

The base class should centralize common behavior:

- first point handling;
- second point handling;
- snapping;
- base point state;
- Ortho constraint application;
- preview point calculation;
- cancellation;
- cleanup of `CurrentBasePoint`.

This is important because command line input, direct distance entry and measurement feedback should not be reimplemented separately in every tool.

---

## Command line input

The command line supports the first CAD-style numeric input workflow.

Supported formats:

| Input | Meaning |
|---|---|
| `100,50` | absolute UCS point |
| `@50,0` | relative UCS offset from `CurrentBasePoint` |
| `5` | direct distance from `CurrentBasePoint` along the cursor direction |

The parser lives in `OpenCad2D.Tools.Input`.

Important types:

- `CommandInputParser`;
- `CommandInputParseResult`;
- `CommandInputKind`.

The command line should only resolve typed input to point input.

Correct flow:

```text
parse text
resolve point or distance
convert UCS to WCS when needed
submit point to CadWorkspace
active tool receives point as normal input
```

Wrong flow:

```text
parse text
create entity directly in UI
```

### Absolute point input

Input:

```text
100,50
```

Meaning:

```text
UCS point X=100, Y=50
```

The point is converted to WCS before it is sent to the active tool.

### Relative point input

Input:

```text
@50,0
```

Meaning:

```text
CurrentBasePoint + UCS offset 50,0
```

This requires a current base point.

### Direct distance entry

Input:

```text
5
```

Meaning:

```text
from CurrentBasePoint, move 5 units along the current cursor/snap direction
```

This requires:

- a current base point;
- a current cursor or snap point;
- a valid non-zero direction.

When Ortho mode is active, the direction is constrained before the final point is calculated.

---

## Ortho mode

Ortho mode is an input constraint.

It is not a rendering-only feature.

Rule:

```text
if |DX| >= |DY| -> horizontal
if |DY| >  |DX| -> vertical
```

It affects:

- interactive preview;
- final second-point input;
- direct distance entry;
- vector feedback;
- status bar measurements.

It should not alter explicit coordinate input such as:

```text
100,50
@50,0
```

Ortho is coordinated through `ToolContext.IsOrthoEnabled` and the input constraint service.

---

## SelectionTool

`SelectionTool` handles entity selection.

Supported behavior:

- click selection;
- Shift-click toggle;
- window selection;
- crossing selection.

Hidden layer entities are ignored by selection through document visibility rules.

Selection order is preserved so that workflows such as grip editing can identify the last selected entity.

Locked layer entities are also ignored by selection. This is handled through selectable-entity queries, not by checking Avalonia UI state.

```text
Hidden layer entity     -> not selectable
Locked layer entity     -> not selectable
Visible unlocked entity -> selectable
```

---

## LineTool

`LineTool` creates a `LineEntity`.

Workflow:

```text
choose first point
choose second point
create line
execute AddEntityCommand
```

Supported input:

- mouse click;
- snap point;
- absolute command line point;
- relative command line point;
- direct distance entry.

When the first point is accepted, it becomes `CurrentBasePoint`.

While waiting for the second point, the UI can show:

- preview line;
- base point marker;
- vector feedback;
- length and delta values.

Ortho mode constrains the second point when active.

---

## RectangleTool

`RectangleTool` creates a rectangle from two opposite corners.

Rectangles are represented as closed `PolylineEntity` instances.

Workflow:

```text
choose first corner
choose opposite corner
create closed polyline
execute AddEntityCommand
```

It benefits from the same two-point infrastructure as `LineTool`.

---

## CircleTool

`CircleTool` creates a `CircleEntity`.

Workflow:

```text
choose center point
choose radius point
radius = distance(center, radius point)
create circle
execute AddEntityCommand
```

Supported input:

- center from mouse click;
- center from absolute command line coordinates;
- radius point from mouse click or snap;
- radius from direct distance entry.

Examples:

```text
Circle -> click center -> click radius point
Circle -> click center -> type 25
Circle -> 100,50 -> 25
```

The preview should show the temporary circle while the user moves the pointer after choosing the center.

Ortho mode can constrain the radius point direction, but the radius is still calculated as the distance from the center.

---


## GripEditTool

`GripEditTool` modifies existing entities by dragging characteristic control points called grips.

It is activated with `Tab` from selection state.

Activation rule:

```text
no selected entity       -> do nothing
one selected entity      -> edit that entity
multiple selected        -> edit the last selected entity
```

This keeps multi-selection available while still making grip editing unambiguous.

Current grip providers:

```text
LineEntity   -> start, midpoint, end
CircleEntity -> center, quadrant 0, quadrant 90, quadrant 180, quadrant 270
```

Grip states:

```text
Cold -> visible grip
Hot  -> cursor hovering near grip
Warm -> active grip waiting for destination
```

The tool exposes grip state and preview data for `CadCanvas`, but it does not depend on Avalonia.

Committed grip edits use `ReplaceEntitiesCommand`, preserving the entity id. Undo and redo therefore work through the normal command history.

`Esc` behavior:

```text
Grip active -> cancel active grip and return to idle grip edit
Grip idle   -> exit grip edit and return to SelectionTool
```

Grip editing must respect locked-layer protection. In normal usage locked-layer entities cannot enter grip editing because they are not selectable. `CadDocument` still rejects replacement as a final safeguard.

---

## MoveTool

`MoveTool` moves the current selection.

Workflow:

```text
select entities
activate Move
choose base point
choose destination point
vector = destination - base
execute MoveEntitiesCommand
```

Move uses the same two-point model as drawing tools.

This means it supports:

- snap-based base and destination points;
- command line destination points;
- direct distance entry;
- Ortho mode;
- vector preview;
- `L`, `DX`, `DY` measurement feedback.

---

## CopyTool

`CopyTool` copies the current selection.

Workflow:

```text
select entities
activate Copy
choose base point
choose destination point
vector = destination - base
execute CopyEntitiesCommand
```

The source entities are not modified. New copied entities receive new ids.

Like Move, Copy supports command line input, direct distance entry, Ortho mode, vector preview and measurement feedback.

---

## DeleteTool

`DeleteTool` deletes the current selection.

It should use `CadDocument.RemoveEntities(...)` through a command, not mutate the entity collection directly.

Locked-layer entities normally cannot be selected, so they should not appear in the selection set. `CadDocument` still protects itself if a command tries to remove a locked-layer entity.

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

Important rule:

```text
The UI toggles layer state.
The workspace cleans selection state.
The document enforces mutation rules.
```

Tools should not assume that selection is always valid forever. If layer state changes, selection must be revalidated against `CadDocument.IsEntitySelectable(...)`.

---

## ToolRegistry

`ToolRegistry` maps tool identifiers to tool descriptors and tool creation logic.

Current registered tools:

```text
Selection
Line
Rectangle
Circle
GripEdit
Move
Copy
Delete
```

Current drawing tools:

```text
Line
Rectangle
Circle
```

When a new tool is added, update:

- `ToolId`;
- `ToolRegistry`;
- UI tool button;
- tool registry tests;
- docs.

---

## Preview and measurement feedback

Tools should expose enough state for the UI to render preview feedback without moving CAD logic into Avalonia.

Current temporary feedback includes:

- grip markers and grip-edit preview entities;
- preview entities;
- base-point marker;
- vector line from base point to current point;
- length `L`;
- delta X `DX`;
- delta Y `DY`.

The measurement values should be calculated consistently with the active UCS and the constrained preview point.

If Ortho mode is active, measurements should reflect the Ortho-constrained point, not the unconstrained raw mouse position.

---

## Cancellation

A tool operation should be cancellable.

For two-point tools:

```text
before first point -> no active operation
first point accepted -> operation in progress
ESC -> clear first point and CurrentBasePoint
```

After cancelling a tool operation, the current selection should remain. A second ESC can clear the selection at workspace level.

---

## Adding a new tool

When adding a new tool:

1. Add or reuse an entity type in `OpenCad2D.Core`.
2. Add the tool in `OpenCad2D.Tools`.
3. Use commands for document changes.
4. Derive from `TwoPointToolBase` if the tool is naturally point-based.
5. Register the tool in `ToolRegistry`.
6. Add UI button and handler in `OpenCad2D.App`.
7. Add tests.
8. Update docs.

Prefer reusing command line, Ortho and preview infrastructure instead of adding custom UI-specific logic.

---

## Future tool families

The following tool families are specified in dedicated design documents and should be implemented incrementally with tests:

- Measure tools (`DistanceTool`, `AreaTool`) query geometry only. They must not execute commands or mutate the document.
- Transform tools (`RotateTool`, `ScaleTool`, `AlignTool`) transform selected entities through undoable commands and `CadDocument.ReplaceEntities(...)`.
- Utility tools such as `MatchPropertiesTool` should copy layer assignment rather than per-entity appearance, because appearance is intended to be layer-owned.
- Annotation tools (`TextTool`, dimension tools) will introduce semantic entities rather than decomposing annotations into unrelated lines and text.

These tools should reuse the existing input pipeline where possible: snapping, command line point input, direct distance entry, Ortho where appropriate, preview rendering and command history.

---

## Layer Manager

The Layer Manager is an App-layer dialog, not a CAD tool. It is opened from the `Layers...` button in the top bar.

The dialog edits a copy of the document layer list. Pressing `Cancel` discards the copy. Pressing `OK` validates the copy and applies it to the workspace.

Layer Manager v1 supports:

```text
create layer
rename non-default layer
delete empty non-current layer
set current layer
set visibility
set locked state
set color hex
set line weight
```

Validation rules:

```text
layer 0 cannot be deleted
layer 0 cannot be renamed
current layer cannot be deleted
layers containing entities cannot be deleted
layer names are required
layer names must be unique
current layer must be visible and unlocked
```

Applying Layer Manager changes must go through `UpdateLayersCommand`, so the operation is undoable and marks the document dirty.

The Layer Manager is intentionally separate from the main canvas. The main window keeps only quick layer controls and the current-layer selector.

---

## Property panel

The Property Panel is an App-layer read-only inspection panel. It is not a tool and does not modify the document.

It is updated from the current selection and document state. It can show:

```text
no selection -> document summary
single line -> start, end, length, DX, DY, angle, bounds
single circle -> center, radius, diameter, area, circumference, bounds
single polyline -> vertices, closed state, length, area when closed, bounds
multiple selection -> count, type summary, layer summary, aggregate bounds
```

Future editable properties must use commands. Do not add direct `CadDocument` mutations from the property panel.
