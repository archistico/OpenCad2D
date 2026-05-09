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
- drawing circles;
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
- snap markers;
- CAD-like crosshair cursor;
- command line point input;
- absolute coordinate input;
- relative coordinate input;
- direct distance entry;
- temporary base-point and vector feedback;
- temporary `L`, `DX` and `DY` measurement feedback;
- Ortho mode;
- viewport zoom and pan;
- Zoom Extents;
- UCS/WCS coordinate distinction;
- geometry tolerance strategy;
- spatial index abstraction;
- document mutation through `CadDocument`;
- ViewModel property notifications through `INotifyPropertyChanged`;
- UI feedback for the active command/tool, current layer, snap type and measurement state.

Recently completed areas include command line input, Ortho mode, `CircleTool` and Zoom Extents.

The next major areas are property panel, richer layer management, additional drawing tools, persistence and more advanced modify commands.

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
- Command line input must resolve to point input and be forwarded to the active tool; it must not create entities directly.
- Hidden layer entities must not be drawn, selected or snapped to.
- Locked layer entities must remain visible.
- Locked layer entities must not be selectable.
- Locked layer entities may still be used for snapping.
- Locked layer entities must not be modified, removed or transformed.
- Spatial queries should be preferred over full document scans when interaction is cursor-area based.
- Floating-point comparisons should use `GeometryTolerance`.

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

Typed coordinate input is interpreted as UCS input:

```text
100,50 -> UCS point
@50,0  -> UCS relative offset
```

The input is converted to WCS before being sent to the active tool.

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

Prefer tolerance-aware checks.

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

This is important for future tools such as trim, extend, fillet, chamfer, offset, explode, break and join.

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
- `ToolInputConstraintService`.

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

## 12. ToolContext boundary

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

`ToolContext` also carries shared tool runtime state:

- `CurrentBasePoint`;
- `IsOrthoEnabled`.

`CurrentBasePoint` is used by two-point tools, command line relative input, direct distance entry, contextual snaps and temporary measurements.

`IsOrthoEnabled` is used by input constraint services and two-point tools to constrain the second point.

`ToolContext` must not contain:

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

## 13. Command line input

The command line is part of the Avalonia application, but its behavior must remain aligned with the tool pipeline.

Supported input formats:

```text
100,50  absolute UCS point
@50,0   UCS offset from CurrentBasePoint
5       direct distance from CurrentBasePoint along cursor direction
```

The parser lives in `OpenCad2D.Tools.Input`.

Important types:

- `CommandInputParser`;
- `CommandInputParseResult`;
- `CommandInputKind`.

The command line should not create entities. It should:

```text
parse input
resolve final point
convert UCS to WCS when needed
create synthetic point/pointer input
forward the point to the active tool
```

This means tools do not need separate code paths for mouse input and typed input.

### Direct distance entry

Direct distance entry requires:

- `CurrentBasePoint`;
- current cursor or snap point;
- a non-zero direction vector.

The destination point is:

```text
basePoint + normalizedDirection * distance
```

When Ortho mode is enabled, the direction must be constrained before the distance is applied.

---

## 14. Ortho mode

Ortho mode constrains second-point input to the nearest horizontal or vertical direction from the current base point.

Rule:

```text
if |DX| >= |DY| -> horizontal
if |DY| >  |DX| -> vertical
```

Examples:

```text
base: 100,100
cursor: 180,120
DX = 80
DY = 20
result: 180,100
```

```text
base: 100,100
cursor: 120,180
DX = 20
DY = 80
result: 100,180
```

Ortho mode should affect:

- preview geometry;
- vector feedback;
- `L`, `DX`, `DY` measurements;
- direct distance entry;
- LineTool;
- RectangleTool;
- CircleTool radius preview;
- MoveTool;
- CopyTool.

Explicit coordinate input remains exact.

---

## 15. Selection system

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
Hidden layer entity     -> not selectable
Locked layer entity     -> not selectable
Visible unlocked entity -> selectable
```

Point selection and window/crossing selection must both respect locked layer filtering.

Selection should persist when switching from Select to Move or Copy.

---

## 16. ESC behavior

ESC has a two-step behavior.

```text
first ESC cancels the active tool operation if one is in progress
second ESC clears selection if no tool operation is active
```

This behavior is implemented at workspace/action level, not hardcoded in the UI alone.

Important rule: a tool should return `ToolResult.None()` when there is nothing to cancel. This allows the workspace to decide whether ESC should continue and clear the selection.

---

## 17. Snapping system

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

## 18. Snap visualization

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

## 19. Spatial indexing

The project has a spatial index abstraction.

Important types:

- `ISpatialIndex`;
- `LinearSpatialIndex`.

`EntityCollection` updates the spatial index when entities are:

- added;
- removed;
- replaced;
- cleared.

Current implementation is linear. This is intentional: the abstraction exists so that `LinearSpatialIndex` can later be replaced by a quadtree, R-tree, uniform grid or another spatial acceleration structure.

Interaction services should prefer spatial queries over full document scans when the operation has a search area.

Examples:

```csharp
document.GetVisibleEntities(searchArea)
document.GetSelectableEntities(searchArea)
document.Entities.Query(area)
```

Important rule: the spatial index answers “which entities may be in this area?” Visibility, selectability and locked-layer rules are still document/domain concerns.

---

## 20. Rendering and UI

`OpenCad2D.App` is the Avalonia application.

Important UI responsibilities:

- draw the document;
- draw the grid;
- draw selection highlights;
- draw previews;
- draw snap markers;
- draw crosshair;
- draw base-point and vector feedback;
- draw CircleTool preview;
- convert screen coordinates to WCS;
- convert WCS to screen coordinates;
- host command line input;
- host layer, snap, Ortho and tool controls;
- handle zoom, pan and Zoom Extents.

UI must not own CAD business logic.

### Current layout

The UI uses:

- a top bar for layer state, undo/redo, Zoom Extents and active command;
- a left vertical tool panel grouped by category;
- the central `CadCanvas`;
- a bottom snap/Ortho bar;
- a command line input row;
- a status bar.

### Zoom Extents

Zoom Extents fits the visible drawing into the current canvas area.

Rules:

```text
visible entities are included
hidden layer entities are ignored
locked layer entities are included because they remain visible
empty document is handled gracefully
```

Zoom Extents belongs to the App/viewport layer. It should not modify the CAD document.

---

## 21. Current tools

Current registered tools include:

- `SelectionTool`;
- `LineTool`;
- `RectangleTool`;
- `CircleTool`;
- `MoveTool`;
- `CopyTool`;
- `DeleteTool`.

### LineTool

Two-point drawing tool.

```text
first point  -> line start
second point -> line end
```

Supports mouse input, command line point input, relative input, direct distance entry, snapping, Ortho, preview and measurement feedback.

### RectangleTool

Two-point drawing tool.

```text
first point  -> first corner
second point -> opposite corner
```

Rectangles are stored as closed polylines.

### CircleTool

Two-point drawing tool.

```text
first point  -> center
second point -> radius point
```

The radius is the distance from center to radius point. Direct distance entry can be used as radius input.

Examples:

```text
Circle -> 100,50 -> 25
Circle -> click center -> move cursor -> 25
```

### MoveTool

Two-point modify tool.

```text
first point  -> base point
second point -> destination point
vector       -> destination - base
```

Uses `MoveEntitiesCommand`.

### CopyTool

Two-point modify tool.

```text
first point  -> base point
second point -> destination point
vector       -> destination - base
```

Uses `CopyEntitiesCommand`.

### DeleteTool

Immediate tool/action for deleting the current selection.

---

## 22. Important invariants

Preserve these invariants:

- Document edits go through commands.
- Commands mutate the document through `CadDocument`.
- UI does not create CAD entities directly.
- Command line input forwards points to the active tool.
- Entities are stored in WCS.
- Typed coordinates are interpreted in UCS and then converted.
- Selection stores ids, not entity references.
- Transforming an entity preserves its id.
- Copying an entity creates a new id.
- Hidden layer entities are ignored by rendering, selection and snapping.
- Locked layer entities are rendered.
- Locked layer entities are ignored by selection.
- Locked layer entities remain available for snapping.
- Locked layer entities must not be removed, replaced, moved, transformed or deleted.
- Ortho applies to interactive second-point workflows, previews, measurements and direct distance entry.
- Explicit typed coordinates remain exact.
- Zoom Extents uses visible entities only.
- Tools must remain UI-independent.

---

## 23. Current limitations

Known limitations:

- no full layer manager yet;
- no persistent file format yet;
- no property panel yet;
- no polyline drawing tool yet;
- no arc drawing tool yet;
- no text entities or dimensions yet;
- no file menu yet;
- no DXF/SVG/PDF import/export yet;
- spatial index is still linear;
- command line does not yet support polar syntax such as `@10<45`;
- command line does not yet support expressions or units.

---

## 24. Recommended next steps

Suggested next development order:

1. Add/update docs after the latest tool and viewport changes.
2. Add a first property panel.
3. Add PolylineTool.
4. Add ArcTool.
5. Add JSON save/load.
6. Add richer layer management.
7. Add text entity and TextTool.
8. Add dimensions.
9. Replace `LinearSpatialIndex` with a real spatial structure when needed.
10. Add GitHub Actions.

---

## 25. Class handoff notes

### CadDocument

Responsibilities:

- own the layer collection;
- own the entity collection;
- validate entity layer references;
- provide document-level add, replace and remove methods;
- provide visible-entity queries;
- provide selectable-entity queries;
- enforce locked-layer mutation rules;
- act as the mutation boundary for the document.

Locked layer validation is implemented here and must remain here. UI and tools may prevent invalid operations earlier, but `CadDocument` is the final protection boundary.

### Layer

Responsibilities:

- represent a CAD layer;
- store id, name, color, line weight, visibility and locked state;
- provide immutable-style update helpers such as visibility and locked-state changes.

### LayerCollection

Responsibilities:

- store all layers;
- provide lookup by `LayerId`;
- set layer visibility;
- set layer locked state.

### CadWorkspace

Responsibilities:

- aggregate document, command history, tool context, tool controller and action controller;
- expose current layer and current UCS;
- expose workspace-level actions such as ESC behavior;
- lock or unlock the current layer;
- clear selections that are no longer valid after layer state changes;
- submit command-line points to the active tool;
- coordinate Ortho mode state.

### TwoPointToolBase

Responsibilities:

- handle tools that need a first point and a second point;
- set and clear `CurrentBasePoint`;
- apply snapping for pointer input;
- apply Ortho constraints when enabled;
- provide preview data for UI rendering;
- allow derived tools to create the final command/entity.

Current derived or conceptually compatible tools include Line, Rectangle, Circle, Move and Copy.

### ToolInputConstraintService

Responsibilities:

- apply input constraints such as Ortho;
- keep constraint logic outside UI and outside individual tool implementations;
- provide focused tests for horizontal and vertical constraint behavior.

### CommandInputParser

Responsibilities:

- parse command-line text;
- recognize absolute point input;
- recognize relative point input;
- recognize direct distance input;
- return invalid parse results with a message when needed.

It should not know about Avalonia or create entities.

### CadCanvas

Responsibilities:

- render entities;
- render selection and preview;
- render grid and crosshair;
- render snap markers;
- render base-point/vector feedback;
- render CircleTool preview;
- manage viewport pan/zoom;
- execute Zoom Extents against visible entities;
- convert screen input to model/user coordinates.

It should not implement CAD document mutation rules.
