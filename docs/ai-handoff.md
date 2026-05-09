# OpenCad2D - AI Handoff Document

This document explains the current architecture, implementation rules and development status of OpenCad2D. Its purpose is to help future AI sessions, contributors and maintainers understand the project quickly and continue development without re-discovering architectural decisions from scratch.

This file should be updated after every important development phase.

---

## 1. Project purpose

OpenCad2D is an experimental open-source 2D CAD application built with C#, .NET 8 and Avalonia UI.

The goal is to build a small but serious 2D CAD system with:

- clean architecture;
- strong testability;
- UI-independent CAD logic;
- incremental development;
- clear separation between geometry, document model, interaction logic, tools and UI.

OpenCad2D is not only a graphical desktop application. It is also a CAD architecture experiment designed to keep the core understandable and extensible.

---

## 2. Current development status

The project currently supports:

- drawing lines;
- drawing rectangles;
- selecting entities;
- moving selected entities;
- copying selected entities;
- deleting selected entities;
- undo and redo;
- composite commands;
- current layer selection;
- hidden layer behavior;
- locked layer behavior;
- object snapping;
- grid snapping;
- command line numeric input;
- absolute coordinate input;
- relative coordinate input;
- direct distance entry;
- snap markers;
- CAD-like crosshair cursor;
- viewport zoom and pan;
- UCS/WCS coordinate distinction;
- geometry tolerance strategy;
- spatial index abstraction;
- document mutation through `CadDocument`;
- ViewModel property notifications through `INotifyPropertyChanged`;
- UI feedback for the active command/tool, current layer, snap type and temporary measurements.

The locked layer behavior and the first command line input workflow have been implemented. The next major areas are Ortho mode, zoom extents, richer layer management, additional drawing tools and persistence.

---

## 3. Solution structure

```text
src/
  OpenCad2D.Geometry/
  OpenCad2D.Core/
  OpenCad2D.Interaction/
  OpenCad2D.Tools/
  OpenCad2D.App/

tests/
  OpenCad2D.Geometry.Tests/
  OpenCad2D.Core.Tests/
  OpenCad2D.Interaction.Tests/
  OpenCad2D.Tools.Tests/
```

Dependency direction:

```text
OpenCad2D.App -> OpenCad2D.Tools -> OpenCad2D.Interaction -> OpenCad2D.Core -> OpenCad2D.Geometry
```

Reverse dependencies should be avoided.

Important rules:

- `Geometry` must not depend on `Core`, `Interaction`, `Tools` or `App`.
- `Core` must not depend on `Interaction`, `Tools` or `App`.
- `Interaction` must not depend on `App`.
- `Tools` must remain UI-independent.
- `App` should stay thin and should forward input to the tool/workspace system.

---

## 4. Architectural rules

The following rules are important and should be preserved.

- UI code must not contain CAD business logic.
- Geometry must describe mathematical shapes and operations, not CAD behavior.
- Core owns the document model, entities, layers and commands.
- Interaction owns hit testing, selection and snapping.
- Tools turn user intent into CAD operations.
- App renders and forwards input.
- Document mutations must go through `CadDocument`.
- User-facing document changes should go through commands.
- Tools should not know about Avalonia.
- Viewport conversion belongs to the UI layer.
- Tools receive model/user coordinates through `PointerInfo`.
- Snapping and selection work in model coordinates.
- Hidden layer entities must not be drawn, selected or snapped to.
- Locked layer entities must remain visible.
- Locked layer entities must not be selectable.
- Locked layer entities may still be used for snapping.
- Locked layer entities must not be modified, removed or transformed.
- Spatial queries should be preferred over full document scans when interaction is cursor-area based.
- Floating-point comparisons should use `GeometryTolerance`.
- Command line input must not create entities directly. It should resolve numeric text to model points and forward them to the active tool.

---

## 5. Coordinate systems

OpenCad2D distinguishes three coordinate spaces:

```text
Screen coordinates
WCS / model coordinates
UCS / user coordinates
```

### Screen coordinates

Screen coordinates are Avalonia coordinates measured relative to the canvas. They are used only by the UI layer.

### WCS / model coordinates

WCS is the internal world/model coordinate system. Entities are stored in WCS. Rendering, hit testing, selection and snapping ultimately operate on WCS geometry.

### UCS / user coordinates

UCS is the current user coordinate system. It is represented by `CoordinateSystem2D`.

`PointerInfo` contains both:

- `ModelPoint`;
- `UserPoint`.

The UI converts screen coordinates to WCS through the viewport, then derives UCS coordinates through the current coordinate system.

Important rule:

```text
Entities are stored in WCS.
Tools may display or reason with UCS, but persistent geometry remains WCS-based.
```

---

## 6. Numeric precision strategy

OpenCad2D uses `GeometryTolerance` to avoid direct floating-point equality checks.

Tolerance categories include:

- distance tolerance;
- angle tolerance;
- parameter tolerance;
- vector length tolerance.

New geometric algorithms should use `GeometryTolerance`.

Avoid hardcoded comparisons such as:

```csharp
if (distance == 0)
```

or:

```csharp
if (Math.Abs(value) < 1e-9)
```

Prefer:

```csharp
context.Coordinates.GeometryTolerance.IsDistanceZero(distance)
```

or:

```csharp
GeometryTolerance.Default.ArePointsEqual(first, second)
```

Interaction tolerances are different from geometric tolerances.

Examples:

- `SnapTolerance` is an interaction tolerance.
- `SelectionTolerance` is an interaction tolerance.
- `GeometryTolerance.Distance` is a mathematical/geometric tolerance.

Do not mix these concepts casually.

---

## 7. Document model

`CadDocument` is the central drawing object.

It owns:

- `LayerCollection`;
- `EntityCollection`.

Document mutation should go through these methods:

- `AddEntity`;
- `AddEntities`;
- `ReplaceEntity`;
- `ReplaceEntities`;
- `RemoveEntity`;
- `RemoveEntities`.

External code should not call mutating methods on `document.Entities` directly.

Allowed external usage of `document.Entities` should be mostly query-oriented:

- read all entities;
- get entity by id;
- get entities by ids;
- spatial query.

This rule is important because `CadDocument` is the correct place for:

- layer validation;
- locked layer validation;
- selectable entity queries;
- visible entity queries;
- spatial index consistency;
- future document events;
- future dirty-state tracking.

Bad pattern:

```csharp
document.Entities.RemoveMany(ids);
```

Preferred pattern:

```csharp
document.RemoveEntities(ids);
```

---

## 8. Entity model

CAD entities are immutable or treated as immutable. Transforming an entity returns a new entity instance. Commands replace old entities with new versions instead of mutating geometry in place.

Entity identifiers must remain stable when an operation modifies an existing entity. Copy operations create new identifiers.

Examples:

```text
Move    same entity id, replaced geometry
Rotate  same entity id, replaced geometry
Copy    new entity id, original entity unchanged
```

This rule is important for undo/redo, selection and spatial index updates.

---

## 9. Layer model

Layers are represented by:

- `Layer`;
- `LayerId`;
- `LayerCollection`.

A layer has:

- id;
- name;
- color;
- line weight;
- visibility;
- locked state.

### Hidden layer behavior

```text
hidden layer entities are not drawn
hidden layer entities are not selected
hidden layer entities are not used by snapping
```

Hidden layer filtering should be performed through document-level visibility methods, such as:

```csharp
document.IsEntityVisible(entity)
document.GetVisibleEntities()
document.GetVisibleEntities(searchArea)
```

### Locked layer behavior

Locked layer behavior is implemented.

```text
locked layer entities are drawn
locked layer entities are not selectable
locked layer entities can still be used by snaps
locked layer entities cannot be modified, deleted or transformed
```

The distinction between selection and snapping is intentional:

```text
Selectable entities = visible entities that are not on locked layers
Snappable entities  = visible entities, including entities on locked layers
Editable entities   = entities that are not on locked layers
```

Locked layer validation is enforced in `CadDocument` mutation methods, not only in UI code.

Important methods:

```csharp
document.IsEntitySelectable(entity)
document.GetSelectableEntities()
document.GetSelectableEntities(searchArea)
document.RemoveEntity(id)
document.RemoveEntities(ids)
document.ReplaceEntity(entity)
document.ReplaceEntities(entities)
```

When a layer is locked from the workspace/UI, the current selection is cleaned so that entities that are no longer selectable are removed from `SelectionSet`.

---

## 10. Command system

All undoable document modifications are represented by commands.

Main command types include:

- `AddEntityCommand`;
- `DeleteEntitiesCommand`;
- `ReplaceEntitiesCommand`;
- `TransformEntitiesCommand`;
- `MoveEntitiesCommand`;
- `CopyEntitiesCommand`;
- `RotateEntitiesCommand`;
- `ScaleEntitiesCommand`;
- `MirrorEntitiesCommand`;
- `CompositeCommand`.

Commands modify the document through `CadDocument`, not by mutating `EntityCollection` directly.

### CommandHistory

`CommandHistory` executes commands and stores undo/redo stacks.

When a command is executed:

```text
execute command
push to undo stack
clear redo stack
```

When undo is requested:

```text
pop from undo stack
undo command
push to redo stack
```

When redo is requested:

```text
pop from redo stack
execute command
push to undo stack
```

### CompositeCommand

`CompositeCommand` groups multiple child commands into one undoable operation.

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

This is important for future tools such as:

- trim;
- extend;
- fillet;
- chamfer;
- offset;
- explode;
- break;
- join.

Example future fillet:

```text
CompositeCommand: Fillet
  Replace first line
  Replace second line
  Add fillet arc
```

The user should undo this as one operation.

---

## 11. Tool system

Tools live in `OpenCad2D.Tools`. They are UI-independent.

Important types:

- `ICadTool`;
- `ToolContext`;
- `ToolController`;
- `ToolRegistry`;
- `CadActionController`;
- `CadWorkspace`;
- `PointerInfo`;
- `ToolResult`;
- `TwoPointToolBase`;
- `CommandInputParser`;
- `CommandInputParseResult`.

A tool should:

- receive pointer information;
- interpret user intent;
- use snapping when appropriate;
- create commands when modifying the document;
- return `ToolResult` messages;
- remain independent from Avalonia.

A tool should not:

- show dialogs;
- know about buttons;
- know about the canvas;
- convert screen coordinates;
- render directly;
- modify the document without a command, unless there is a very specific reason.

---
## 12. Command line input

The application supports a first version of CAD-style command line input.

The command line accepts point and distance input while the active tool is waiting for a point. It does not create CAD entities directly. Instead, it resolves the typed value to a model point and forwards that point to the active tool as synthetic pointer input.

Supported input formats:

```text
100,50   absolute UCS coordinates
@50,0    relative UCS offset from CurrentBasePoint
5        direct distance from CurrentBasePoint along the current cursor direction
```

Important rules:

- typed coordinates are interpreted in UCS/user coordinates;
- entities are still stored in WCS/model coordinates;
- absolute and relative coordinate input is converted to WCS before reaching the tool;
- direct distance entry uses `CurrentBasePoint` and the current cursor or snap point as the direction reference;
- if no base point exists, distance and relative input are invalid;
- if the cursor is on the base point, direct distance cannot determine a direction.

The command line flow is:

```text
TextBox input
CommandInputParser
resolved Point2D in WCS
CadWorkspace.SubmitPointFromCommandLine(...)
ToolController / active tool
normal command execution
```

This keeps command input compatible with `LineTool`, `RectangleTool`, `MoveTool`, `CopyTool` and future tools that consume point input.

---

## 13. ToolContext boundary

`ToolContext` is intentionally split into sub-contexts to avoid becoming a God Object.

Current sub-contexts:

- `ToolCommandContext`;
- `ToolSelectionContext`;
- `ToolSnapContext`;
- `ToolCoordinateContext`;
- `ToolCreationContext`.

New code should prefer:

```csharp
context.Commands
context.Selection
context.Snapping
context.Coordinates
context.Creation
```

instead of older compatibility properties.

Examples:

```csharp
context.Commands.Execute(context.Document, command);
```

```csharp
context.Selection.SelectedIds
```

```csharp
context.Creation.CurrentLayerId
```

```csharp
context.Coordinates.GeometryTolerance
```

`ToolContext` may contain model-side services required by tools. It also exposes `CurrentBasePoint`, which represents the last accepted model point for tools that are waiting for a second point.

`CurrentBasePoint` is used by:

- contextual snaps;
- command line relative input such as `@50,0`;
- direct distance entry such as `5`;
- temporary measurement feedback.

It must not contain:

- UI controls;
- Avalonia types;
- viewport conversion logic;
- dialogs;
- message boxes;
- status bar services;
- file system services;
- rendering services;
- application configuration unrelated to tool execution.

---

## 14. Selection system

Selection state is stored in `SelectionSet`. It stores entity ids, not entity references. This avoids stale references when commands replace entities.

Selection behavior is implemented through:

- `SelectionTool`;
- `SelectionService`;
- `SelectionSet`.

Supported behavior:

- click selection;
- shift-click toggle;
- window selection;
- crossing selection.

Selection uses selectable entities, not merely visible entities.

```text
Hidden layer entity      -> not selectable
Locked layer entity      -> not selectable
Visible unlocked entity  -> selectable
```

Point selection and window/crossing selection must both respect locked layer filtering.

Selection should persist when switching from Select to Move or Copy. This enables a common CAD workflow:

```text
select entities
activate Move
choose base point
choose destination point
```

Tool switching should use `Deactivate`, not `Cancel`.

---

## 15. ESC behavior

ESC has a two-step behavior.

```text
first ESC cancels the active tool operation if one is in progress
second ESC clears selection if no tool operation is active
```

Example:

```text
select two entities
activate Move
click base point
press ESC
Move operation is cancelled
selection remains
press ESC again
selection is cleared
```

This behavior is implemented at workspace/action level, not hardcoded in the UI alone.

Important rule: a tool should return `ToolResult.None()` when there is nothing to cancel. This allows the workspace to decide whether ESC should continue and clear the selection.

---

## 16. Snapping system

Snapping lives in `OpenCad2D.Interaction`.

Important types:

- `SnapKind`;
- `SnapRequest`;
- `SnapCandidate`;
- `ISnapProvider`;
- `SnapService`;
- `GridSettings`.

Supported snap kinds:

- endpoint;
- midpoint;
- center;
- quadrant;
- intersection;
- nearest;
- perpendicular;
- tangent;
- grid.

Snap providers should:

- work in model coordinates;
- not modify the document;
- respect hidden layer behavior;
- use visible entities only;
- keep snapping available on locked layers;
- return candidates within tolerance;
- return no candidates when contextual input is missing;
- have focused tests.

### Contextual snaps

Some snaps require a base point.

Current contextual snaps:

- perpendicular;
- tangent.

`TwoPointToolBase` passes the first point as `BasePoint` when choosing the second point.

---

## 17. Snap visualization

The Avalonia UI draws snap markers. Different snap kinds should have different marker shapes.

Current intended marker semantics:

```text
Endpoint       L shape
Midpoint       X shape
Center         circle
Quadrant       diamond
Intersection   plus
Nearest        square
Perpendicular  T shape
Tangent        circle with line
Grid           small grid/cross
```

This is UI feedback only. Snap logic remains in `OpenCad2D.Interaction`.

---

## 18. Spatial indexing

The project has a spatial index abstraction.

Important types:

- `ISpatialIndex`;
- `LinearSpatialIndex`.

`EntityCollection` updates the spatial index when entities are:

- added;
- removed;
- replaced;
- cleared.

Current implementation is linear. This is intentional: the abstraction exists so that `LinearSpatialIndex` can later be replaced by:

- Quadtree;
- R-Tree;
- uniform grid;
- another spatial acceleration structure.

Interaction services should prefer spatial queries over full document scans when the operation has a search area.

Examples:

```csharp
document.GetVisibleEntities(searchArea)
```

```csharp
document.GetSelectableEntities(searchArea)
```

```csharp
document.Entities.Query(area)
```

Important rule: the spatial index answers “which entities may be in this area?” Visibility, selectability and locked-layer rules are still document/domain concerns.

---

## 19. Rendering and UI

`OpenCad2D.App` is the Avalonia application.

`CadCanvas` renders:

- grid;
- entities;
- selection;
- previews;
- temporary base-point marker;
- temporary vector/measurement line for two-point tools;
- crosshair cursor;
- snap markers;
- UCS indicator.

The standard OS cursor is hidden over the CAD canvas. A CAD-like crosshair is drawn across the full canvas. A small central rectangle indicates the exact pick point.

Entity pens are cached to avoid allocating brushes and pens for every visible entity on every frame.

Rendering should not contain CAD business logic. If rendering code starts deciding document rules, the logic probably belongs in Core, Interaction or Tools.

---

## 20. ViewModel and UI state

`MainWindowViewModel` implements `INotifyPropertyChanged`.

This is used to keep bound UI elements updated without manually rewriting every label.

Important calculated properties include:

- `StatusText`;
- `EntityCount`;
- `SelectedCount`;
- `ActiveToolName`;
- `LayerNames`;
- `Layers`;
- `CurrentLayer`;
- `CurrentLayerIsVisible`;
- `CurrentLayerIsLocked`;
- `CurrentLayerText`;
- `MousePositionText`;
- `SnapText`;
- `LastMessage`;
- `CommandPromptText`;
- `MeasurementText`.

The code-behind may still manage UI-specific behavior, such as:

- active tool button classes;
- canvas invalidation;
- layer checkbox synchronization;
- cursor/canvas-specific visual state.

Do not move CAD business logic into the code-behind just because it is convenient.

---

## 21. Keyboard and mouse behavior

Keyboard shortcuts:

```text
Ctrl+Z  undo
Ctrl+Y  redo
Delete  delete selection
Esc     cancel active operation, then clear selection
Home    reset viewport
```

Mouse behavior:

```text
left click           tool input
mouse move           preview, snapping, crosshair
middle mouse button  pan
mouse wheel          zoom
```

The active command/tool should always be visible to the user.

The UI should show:

- active tool button highlight;
- active command text;
- status bar message.

---

## 22. Important invariants

The following invariants should be preserved.

- Entities are stored in WCS.
- Tools receive model/user coordinates, not screen coordinates.
- Commands are UI-independent.
- Document mutations go through `CadDocument`.
- Hidden layer entities are ignored by rendering, selection and snapping.
- Locked layer entities are rendered.
- Locked layer entities are ignored by selection.
- Locked layer entities remain available for snapping.
- Locked layer entities must not be removed, replaced, moved, transformed or deleted.
- The spatial index must stay synchronized with entity add/remove/replace.
- Copy commands must reuse created entity ids on redo.
- Composite commands undo child commands in reverse order.
- Selection stores ids, not entity instances.
- Geometry algorithms should use `GeometryTolerance`.
- Viewport operations must not modify document geometry.
- Rendering should not mutate document state.
- Tools should not depend on Avalonia.
- Command line input must go through the active tool and the normal command system.
- `CurrentBasePoint` must be cleared when a two-point operation completes or is cancelled.

---

## 23. Current limitations

Known limitations:

- no full layer manager yet;
- no persistent file format yet;
- no circle drawing tool yet;
- no arc drawing tool yet;
- no polyline drawing tool yet;
- no text tool yet;
- no property panel yet;
- spatial index is still linear;
- no model space / paper space separation yet;
- no angular/polar command line input yet;
- no command history/autocomplete yet;
- no Ortho mode yet;
- no DXF/SVG/PDF export yet.

---

## 24. Recommended next steps

Recommended next implementation steps:

```text
1. Add Ortho mode.
2. Add Zoom Extents.
3. Add CircleTool.
4. Add ArcTool.
5. Add PolylineTool.
6. Add a richer Layer Manager.
7. Add JSON save/load.
8. Add property panel.
9. Add text entity and TextTool.
10. Replace LinearSpatialIndex with a real spatial structure when needed.
11. Add GitHub Actions.
```

The preferred development style is:

```text
one concept
one focused implementation
one set of tests
one UI improvement when needed
```

---

# 24. Class reference

This section should be expanded as the project grows. Each important class should be documented using the following format:

```markdown
## ClassName

**Project:** `ProjectName`
**Path:** `relative/path/ClassName.cs`

### Purpose

Short explanation of what the class does.

### Responsibilities

- responsibility 1;
- responsibility 2.

### Must not do

- forbidden responsibility 1;
- forbidden responsibility 2.

### Important collaborators

- collaborator 1;
- collaborator 2.

### Notes for future changes

Important rules, edge cases or invariants.
```

---

## CadDocument

**Project:** `OpenCad2D.Core`
**Path:** `src/OpenCad2D.Core/Documents/CadDocument.cs`

### Purpose

Represents the current CAD drawing.

### Responsibilities

- Own the layer collection.
- Own the entity collection.
- Validate entity layer references.
- Provide document-level add, replace and remove methods.
- Provide visible-entity queries.
- Provide selectable-entity queries.
- Enforce locked-layer mutation rules.
- Act as the mutation boundary for the document.

### Must not do

- Render entities.
- Handle UI input.
- Know about Avalonia.
- Implement tool state machines.
- Store viewport state.

### Important collaborators

- `LayerCollection`;
- `EntityCollection`;
- `CadEntity`;
- `Layer`.

### Notes for future changes

Locked layer validation is implemented here and must remain here. UI and tools may prevent invalid operations earlier, but `CadDocument` is the final protection boundary.

---

## EntityCollection

**Project:** `OpenCad2D.Core`
**Path:** `src/OpenCad2D.Core/Collections/EntityCollection.cs`

### Purpose

Stores entities and keeps the spatial index synchronized.

### Responsibilities

- Store entities by id.
- Provide entity lookup.
- Provide spatial queries.
- Update the spatial index on add, remove, replace and clear.

### Must not do

- Validate layer existence.
- Decide hidden/locked layer behavior.
- Execute commands.
- Know about UI.

### Important collaborators

- `CadEntity`;
- `EntityId`;
- `ISpatialIndex`;
- `LinearSpatialIndex`.

### Notes for future changes

External code should avoid direct mutation through this collection. Mutations should go through `CadDocument`.

---

## Layer

**Project:** `OpenCad2D.Core`
**Path:** `src/OpenCad2D.Core/Layers/Layer.cs`

### Purpose

Represents a CAD layer.

### Responsibilities

- Store layer identity and name.
- Store default color and line weight.
- Store visibility and locked state.
- Provide immutable-style update helpers such as visibility and locked-state changes.

### Must not do

- Store entities.
- Render itself.
- Decide selection behavior.

### Important collaborators

- `LayerId`;
- `CadColor`;
- `LineWeight`.

### Notes for future changes

Locked layer support must preserve the distinction between visibility, selectability, snapping and editability.

---

## LayerCollection

**Project:** `OpenCad2D.Core`
**Path:** `src/OpenCad2D.Core/Layers/LayerCollection.cs`

### Purpose

Stores all layers in the document.

### Responsibilities

- Add layers.
- Retrieve layers by id.
- Replace layer definitions.
- Set layer visibility.
- Set layer locked state.

### Must not do

- Store entities.
- Render layers.
- Execute commands.

### Important collaborators

- `Layer`;
- `LayerId`.

---

## ICadCommand

**Project:** `OpenCad2D.Core`
**Path:** `src/OpenCad2D.Core/Commands/ICadCommand.cs`

### Purpose

Defines an undoable document operation.

### Responsibilities

- Expose command name.
- Execute operation.
- Undo operation.

### Must not do

- Depend on Avalonia.
- Read mouse input.
- Store UI controls.
- Mutate `EntityCollection` directly.

### Important collaborators

- `CadDocument`.

---

## CommandHistory

**Project:** `OpenCad2D.Core`
**Path:** `src/OpenCad2D.Core/Commands/CommandHistory.cs`

### Purpose

Coordinates undo and redo.

### Responsibilities

- Execute commands.
- Store undo stack.
- Store redo stack.
- Clear redo stack after new command execution.

### Must not do

- Know tool details.
- Know UI details.
- Modify documents without commands.

### Important collaborators

- `ICadCommand`;
- `CadDocument`.

---

## CompositeCommand

**Project:** `OpenCad2D.Core`
**Path:** `src/OpenCad2D.Core/Commands/CompositeCommand.cs`

### Purpose

Groups multiple commands into one undoable operation.

### Responsibilities

- Execute child commands in order.
- Undo child commands in reverse order.
- Roll back already executed child commands if execution fails.

### Must not do

- Hide unrelated operations in a misleading command.
- Depend on UI.

### Important collaborators

- `ICadCommand`;
- `CadDocument`.

### Notes for future changes

Use this for trim, extend, fillet, chamfer and offset when they require multiple document mutations.

---

## GeometryTolerance

**Project:** `OpenCad2D.Geometry`
**Path:** `src/OpenCad2D.Geometry/GeometryTolerance.cs`

### Purpose

Defines the numeric tolerance strategy for geometric algorithms.

### Responsibilities

- Compare distances.
- Compare angles.
- Compare normalized parameters.
- Detect near-zero vector lengths.
- Compare points.

### Must not do

- Represent user snap tolerance.
- Represent selection tolerance.
- Depend on CAD entities or UI.

### Important collaborators

- `Point2D`;
- `Vector2D`.

---

## CoordinateSystem2D

**Project:** `OpenCad2D.Geometry`
**Path:** `src/OpenCad2D.Geometry/Coordinates/CoordinateSystem2D.cs`

### Purpose

Represents a 2D user coordinate system mapped to world coordinates.

### Responsibilities

- Convert points from UCS to WCS.
- Convert points from WCS to UCS.
- Convert vectors between UCS and WCS.
- Store UCS origin and axes.

### Must not do

- Store document entities.
- Render the UCS indicator.
- Know about Avalonia.

### Important collaborators

- `Point2D`;
- `Vector2D`.

---

## ToolContext

**Project:** `OpenCad2D.Tools`
**Path:** `src/OpenCad2D.Tools/Common/ToolContext.cs`

### Purpose

Provides runtime model-side services required by tools.

### Responsibilities

- Expose the active document.
- Expose command execution context.
- Expose selection context.
- Expose snapping context.
- Expose coordinate/tolerance context.
- Expose entity creation defaults.
- Expose `CurrentBasePoint` for active two-point workflows.

### Must not do

- Store UI controls.
- Store viewport conversion logic.
- Show dialogs.
- Render anything.
- Become a dumping ground for unrelated services.

### Important collaborators

- `ToolCommandContext`;
- `ToolSelectionContext`;
- `ToolSnapContext`;
- `ToolCoordinateContext`;
- `ToolCreationContext`.

---

## CadWorkspace

**Project:** `OpenCad2D.Tools`
**Path:** `src/OpenCad2D.Tools/Common/CadWorkspace.cs`

### Purpose

Aggregates the runtime CAD objects used by the application.

### Responsibilities

- Own or expose document runtime state.
- Own command history.
- Own selection state.
- Own tool controller.
- Own action controller.
- Provide workspace-level actions such as ESC behavior.
- Lock or unlock the current layer.
- Clear selections that are no longer valid after layer state changes.
- Submit command line points to the active tool.

### Must not do

- Render.
- Depend on Avalonia.
- Open files or dialogs.

### Important collaborators

- `CadDocument`;
- `CommandHistory`;
- `SelectionSet`;
- `ToolContext`;
- `ToolController`;
- `CadActionController`.

---

## ToolController

**Project:** `OpenCad2D.Tools`
**Path:** `src/OpenCad2D.Tools/Common/ToolController.cs`

### Purpose

Owns and coordinates the active tool.

### Responsibilities

- Forward pointer events to the active tool.
- Switch active tools.
- Deactivate previous tools.
- Cancel the active tool when requested.

### Must not do

- Clear selection automatically on tool switch.
- Know about Avalonia controls.
- Render previews.

### Important collaborators

- `ICadTool`;
- `ToolRegistry`;
- `ToolContext`.

---

## TwoPointToolBase

**Project:** `OpenCad2D.Tools`
**Path:** `src/OpenCad2D.Tools/Common/TwoPointToolBase.cs`

### Purpose

Base class for tools that use a first point and a second point.

### Responsibilities

- Store the first point.
- Track the current point for previews.
- Apply snapping.
- Call derived behavior when the second point is selected.
- Reset state after completion or cancellation.
- Update `ToolContext.CurrentBasePoint` after the first point is accepted.
- Clear `ToolContext.CurrentBasePoint` after completion or cancellation.

### Must not do

- Decide specific operation behavior.
- Render previews directly.
- Know about Avalonia.

### Important collaborators

- `ToolContext`;
- `PointerInfo`;
- `SnapService`;
- `ToolResult`.

---

## SelectionTool

**Project:** `OpenCad2D.Tools`
**Path:** `src/OpenCad2D.Tools/Selection/SelectionTool.cs`

### Purpose

Handles point, window and crossing selection.

### Responsibilities

- Select entity by point.
- Toggle selection with Shift.
- Select by window or crossing rectangle.
- Keep selection state in `SelectionSet`.

### Must not do

- Delete or modify selected entities.
- Render the selection rectangle.
- Clear selection when merely deactivated.

### Important collaborators

- `SelectionService`;
- `SelectionSet`;
- `ToolContext`.

---

## SnapService

**Project:** `OpenCad2D.Interaction`
**Path:** `src/OpenCad2D.Interaction/Snapping/SnapService.cs`

### Purpose

Coordinates snap providers and chooses the best snap candidate.

### Responsibilities

- Run enabled snap providers.
- Collect candidates.
- Order candidates by priority and distance.
- Return the best candidate.

### Must not do

- Render snap markers.
- Modify the document.
- Depend on Avalonia.

### Important collaborators

- `ISnapProvider`;
- `SnapRequest`;
- `SnapCandidate`;
- `SnapKind`.

---

## SnapRequest

**Project:** `OpenCad2D.Interaction`
**Path:** `src/OpenCad2D.Interaction/Snapping/SnapRequest.cs`

### Purpose

Describes a snap query.

### Responsibilities

- Store document.
- Store cursor point.
- Store optional base point.
- Store tolerance.
- Store enabled snap kinds.
- Store grid settings.
- Provide spatial search area.

### Must not do

- Choose the best candidate.
- Render anything.
- Modify the document.

### Important collaborators

- `CadDocument`;
- `Point2D`;
- `GridSettings`;
- `SnapKind`.

---

## ISpatialIndex

**Project:** `OpenCad2D.Core`
**Path:** `src/OpenCad2D.Core/Spatial/ISpatialIndex.cs`

### Purpose

Defines a spatial lookup abstraction for entities.

### Responsibilities

- Add entities.
- Remove entities.
- Replace entities.
- Clear index.
- Query entities by bounding box.

### Must not do

- Decide visibility.
- Decide locked layer behavior.
- Decide selectability.
- Execute commands.

### Important collaborators

- `CadEntity`;
- `BoundingBox2D`;
- `EntityId`.

---

## LinearSpatialIndex

**Project:** `OpenCad2D.Core`
**Path:** `src/OpenCad2D.Core/Spatial/LinearSpatialIndex.cs`

### Purpose

Simple baseline implementation of `ISpatialIndex`.

### Responsibilities

- Store entity bounding boxes.
- Return entities whose bounds intersect the query area.

### Must not do

- Optimize prematurely.
- Decide CAD behavior beyond spatial lookup.

### Important collaborators

- `ISpatialIndex`;
- `CadEntity`;
- `BoundingBox2D`.

### Notes for future changes

Can later be replaced by Quadtree, R-Tree or uniform grid without changing interaction services.

---

## CadCanvas

**Project:** `OpenCad2D.App`
**Path:** `src/OpenCad2D.App/Controls/CadCanvas.cs`

### Purpose

Avalonia canvas responsible for rendering and input forwarding.

### Responsibilities

- Render grid.
- Render visible entities.
- Render previews.
- Render selection visuals.
- Render crosshair.
- Render snap markers.
- Render base-point and vector feedback for two-point tools.
- Convert screen coordinates to model coordinates.
- Forward pointer and keyboard input to the workspace.

### Must not do

- Implement CAD business rules.
- Modify document entities directly.
- Implement snapping algorithms.
- Implement command logic.

### Important collaborators

- `CadWorkspace`;
- `ViewportTransform`;
- `MainWindowViewModel`;
- `ToolController`;
- `SnapService`.

---

## MainWindowViewModel

**Project:** `OpenCad2D.App`
**Path:** `src/OpenCad2D.App/ViewModels/MainWindowViewModel.cs`

### Purpose

Exposes UI state derived from the CAD workspace.

### Responsibilities

- Expose status text.
- Expose active tool name.
- Expose entity and selection counts.
- Expose mouse coordinate text.
- Expose snap text.
- Expose layer-related state.
- Expose command prompt text.
- Expose temporary measurement text.
- Resolve command line input into tool points.
- Notify UI changes with `INotifyPropertyChanged`.

### Must not do

- Implement geometry algorithms.
- Render entities.
- Mutate document entities directly.
- Replace command/tool logic.

### Important collaborators

- `CadWorkspace`;
- `ToolResult`;
- `SnapCandidate`;
- `Layer`.

---

## CommandInputParser

**Project:** `OpenCad2D.Tools`
**Path:** `src/OpenCad2D.Tools/Input/CommandInputParser.cs`

### Purpose

Parses command line numeric input into a structured input kind.

### Responsibilities

- Parse absolute point input such as `100,50`.
- Parse relative point input such as `@50,0`.
- Parse direct distance input such as `5`.
- Return invalid results with user-facing error messages when input cannot be parsed.

### Must not do

- Create CAD entities.
- Execute commands.
- Know about Avalonia.
- Convert screen coordinates.

### Important collaborators

- `CommandInputParseResult`;
- `CommandInputKind`;
- `Point2D`;
- `Vector2D`.

---

## AddEntityCommand

**Project:** `OpenCad2D.Core`
**Path:** `src/OpenCad2D.Core/Commands/AddEntityCommand.cs`

### Purpose

Adds one or more entities to the document.

### Responsibilities

- Add entities on execute.
- Remove the same entities on undo.
- Use `CadDocument` mutation methods.

### Must not do

- Mutate `EntityCollection` directly.
- Depend on UI.

### Important collaborators

- `CadDocument`;
- `CadEntity`.

---

## DeleteEntitiesCommand

**Project:** `OpenCad2D.Core`
**Path:** `src/OpenCad2D.Core/Commands/DeleteEntitiesCommand.cs`

### Purpose

Deletes existing entities and restores them on undo.

### Responsibilities

- Store deleted entities.
- Remove entities through `CadDocument`.
- Restore deleted entities on undo.

### Must not do

- Clear UI selection directly.
- Mutate `EntityCollection` directly.

### Important collaborators

- `CadDocument`;
- `EntityId`;
- `CadEntity`.

---

## TransformEntitiesCommand

**Project:** `OpenCad2D.Core`
**Path:** `src/OpenCad2D.Core/Commands/TransformEntitiesCommand.cs`

### Purpose

Transforms existing entities using a transformation matrix.

### Responsibilities

- Store original entities.
- Create transformed entities.
- Replace transformed entities through `CadDocument`.
- Restore original entities on undo.

### Must not do

- Create new identifiers for transformed existing entities.
- Mutate entities in place.
- Depend on UI.

### Important collaborators

- `CadDocument`;
- `Matrix2D`;
- `CadEntity`.

---

## CopyEntitiesCommand

**Project:** `OpenCad2D.Core`
**Path:** `src/OpenCad2D.Core/Commands/CopyEntitiesCommand.cs`

### Purpose

Creates copied entities using a displacement vector.

### Responsibilities

- Read source entities.
- Create copied entities with new ids.
- Reuse the same created ids on redo.
- Remove copied entities on undo.

### Must not do

- Modify original entities.
- Recreate different ids on redo.
- Mutate `EntityCollection` directly.

### Important collaborators

- `CadDocument`;
- `EntityId`;
- `Vector2D`.

---

# 25. Maintenance rules for this document

Update this file whenever one of these things changes:

- architecture boundaries;
- document mutation rules;
- command behavior;
- layer behavior;
- coordinate system behavior;
- snapping behavior;
- tool behavior;
- important class responsibilities;
- recommended next steps.

When starting a new AI-assisted session, provide this file together with:

- `docs/architecture.md`;
- `docs/commands.md`;
- `docs/tools.md`;
- `docs/snapping.md`;
- `docs/roadmap.md`.

This gives the AI a stable architectural contract and reduces the risk of receiving suggestions that violate current project decisions.
