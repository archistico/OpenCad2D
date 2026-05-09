# Architecture

OpenCad2D is organized around a simple principle: CAD logic must remain independent from the graphical user interface.

The Avalonia application is the presentation layer. It draws the document, receives mouse and keyboard input, converts screen coordinates into CAD coordinates and forwards input to the tool system.

Geometry, entities, snapping, selection, commands, layers and editing behavior live in dedicated libraries and can be tested without launching the desktop application.

This separation is the most important design rule in the project.

---

## Solution structure

The solution is divided into five main projects.

```text
src/
  OpenCad2D.Geometry/
  OpenCad2D.Core/
  OpenCad2D.Interaction/
  OpenCad2D.Tools/
  OpenCad2D.App/
```

Each project has a specific responsibility.

`OpenCad2D.Geometry` contains low-level geometric primitives, coordinate systems, geometric operations, transformations and numeric tolerance rules.

`OpenCad2D.Core` contains the CAD document model, entities, layers, styles, spatial indexing, commands and undo/redo infrastructure.

`OpenCad2D.Interaction` contains UI-independent interaction services such as hit testing, selection and object snapping.

`OpenCad2D.Tools` contains UI-independent CAD tools, controllers, tool contexts, command line parsing and the runtime workspace.

`OpenCad2D.App` is the Avalonia desktop application.

The direction of dependencies should remain clear.

```text
OpenCad2D.App
  -> OpenCad2D.Tools
      -> OpenCad2D.Interaction
          -> OpenCad2D.Core
              -> OpenCad2D.Geometry
```

No project should depend on a project above it.

---

## Main architectural rule

The UI must not own CAD behavior.

Avalonia may:

- draw the document;
- draw previews;
- draw snap markers;
- draw the crosshair;
- host tool buttons;
- host layer controls;
- host the command line;
- convert screen coordinates to model coordinates;
- forward input to the workspace/tool system;
- manage viewport operations such as pan, zoom and Zoom Extents.

Avalonia should not:

- create CAD entities directly;
- decide document mutation rules;
- implement geometric algorithms;
- implement selection rules;
- implement locked-layer rules;
- implement command undo/redo behavior.

---

## Geometry layer

`OpenCad2D.Geometry` is the lowest layer.

It contains pure geometric concepts such as:

- `Point2D`;
- `Vector2D`;
- `LineSegment2D`;
- `Circle2D`;
- `Arc2D`;
- `Polyline2D`;
- `BoundingBox2D`;
- transformations;
- coordinate systems;
- geometric tolerance.

This project should not know what a CAD entity, layer, command, tool or UI is.

Geometry should remain reusable and independent.

---

## Core layer

`OpenCad2D.Core` owns the CAD document model.

It contains:

- CAD entities;
- entity identifiers;
- layers;
- styles;
- `CadDocument`;
- entity collections;
- spatial indexing;
- commands;
- command history.

### CadDocument mutation boundary

`CadDocument` is the public boundary for modifying the drawing.

Commands and tools should not mutate `EntityCollection` directly.

Preferred:

```csharp
document.AddEntity(entity);
document.ReplaceEntity(entity);
document.RemoveEntity(id);
```

Avoid:

```csharp
document.Entities.RemoveMany(ids);
```

This rule exists because document-level validation belongs to `CadDocument`.

For example, locked layer behavior is enforced by `CadDocument`: replacement and removal of entities that belong to locked layers are rejected at the document mutation boundary.

---

## Interaction layer

`OpenCad2D.Interaction` contains UI-independent interaction services.

Current responsibilities:

- hit testing;
- click selection;
- window selection;
- crossing selection;
- object snapping;
- grid snapping;
- snap prioritization.

Interaction services work in model coordinates. They should not know about Avalonia, screen controls or drawing styles.

### Selection and snapping are different

Selection and snapping intentionally use different document queries.

```text
Selection -> selectable entities
Snapping  -> visible entities
```

This matters for locked layers:

```text
locked layer entity -> visible
locked layer entity -> not selectable
locked layer entity -> snappable
```

---

## Tools layer

`OpenCad2D.Tools` turns user intent into document operations.

It contains:

- `ICadTool`;
- `ToolController`;
- `ToolRegistry`;
- `ToolContext`;
- `CadWorkspace`;
- `CadActionController`;
- drawing tools;
- modify tools;
- command line input parser;
- input constraint services such as Ortho.

Tools receive `PointerInfo`, not Avalonia events.

`PointerInfo` contains:

```text
ModelPoint  WCS/model point
UserPoint   UCS/user point
Modifiers   keyboard modifiers
```

Tools should create commands and execute them through `CommandHistory`.

---

## ToolContext and tool runtime state

`ToolContext` gives tools access to the services and state they need.

It is organized into focused sub-contexts:

- commands;
- selection;
- snapping;
- coordinates;
- creation defaults.

It also contains shared tool runtime state needed by the command line and input constraints:

- `CurrentBasePoint`;
- `IsOrthoEnabled`.

`CurrentBasePoint` is set by tools that have accepted a first point and are waiting for a second point.

This is used for:

- direct distance entry;
- relative command line input;
- contextual snaps;
- temporary measurement feedback;
- vector preview.

`IsOrthoEnabled` is used to constrain second-point workflows to horizontal or vertical movement.

---

## Command line input architecture

The command line is hosted by `OpenCad2D.App`, but the parsing model lives in `OpenCad2D.Tools.Input`.

Supported input formats:

```text
100,50  absolute UCS point
@50,0   UCS offset from CurrentBasePoint
5       direct distance along cursor direction
```

The command line does not create entities directly.

The correct flow is:

```text
TextBox input
parse input
resolve final point
convert UCS to WCS when needed
submit synthetic point input to CadWorkspace
tool receives point as normal input
```

This keeps mouse clicks, typed coordinates and direct distance entry on the same tool pipeline.

### Direct distance entry

Direct distance entry uses:

- the current base point;
- the current cursor/snap point;
- a numeric distance.

Conceptually:

```text
direction = normalize(currentPoint - basePoint)
result = basePoint + direction * distance
```

When Ortho mode is enabled, the current point is first constrained to the closest horizontal or vertical direction before the distance direction is computed.

---

## Ortho mode architecture

Ortho mode is a tool/input constraint, not a rendering trick.

The rule is:

```text
if |DX| >= |DY| -> constrain horizontally
if |DY| >  |DX| -> constrain vertically
```

The constraint should be applied before:

- preview geometry is produced;
- measurement feedback is calculated;
- direct distance entry calculates the final point;
- the final second point is accepted by a two-point tool.

This ensures that what the user sees is what the tool will create or modify.

Explicit typed coordinates remain exact and should not be altered by Ortho.

---

## Two-point tool pattern

Many CAD tools are based on two points:

```text
first point  -> base/anchor point
second point -> final point or vector destination
```

Current tools that follow this model include:

- `LineTool`;
- `RectangleTool`;
- `CircleTool`;
- `MoveTool`;
- `CopyTool`.

`TwoPointToolBase` centralizes common behavior:

- first point acquisition;
- second point acquisition;
- current base point state;
- snap context;
- Ortho constraint application;
- preview support;
- cancellation behavior.

This avoids duplicating command line and Ortho logic in every tool.

---

## App layer

`OpenCad2D.App` is the Avalonia application.

Current responsibilities:

- render visible entities;
- render selected entities;
- render preview entities;
- render CircleTool preview;
- render temporary base-point/vector feedback;
- render the grid;
- render snap markers;
- render the crosshair;
- host the top bar;
- host the left tool panel;
- host the snap/Ortho bar;
- host the command line;
- host the status bar;
- forward pointer input to the workspace;
- forward typed command-line input to the workspace;
- manage pan, zoom and Zoom Extents.

### Viewport

Viewport logic converts between:

```text
screen coordinates <-> WCS/model coordinates
```

This belongs to the UI layer because it depends on the visible canvas area.

### Zoom Extents

Zoom Extents fits visible entities inside the canvas.

Rules:

```text
visible entities are included
hidden layer entities are ignored
locked layer entities are included
empty documents should not crash
```

Zoom Extents does not modify the document. It only changes the viewport transform.

---

## Layers

Layer visibility and locked state are part of document-level rules.

Hidden layer behavior:

```text
hidden layer entities are not drawn
hidden layer entities are not selected
hidden layer entities are not used by snapping
```

Locked layer behavior:

```text
locked layer entities are drawn
locked layer entities are not selectable
locked layer entities can still be used as references for snapping
locked layer entities cannot be modified, removed or transformed
```

The UI exposes the current layer, a visibility toggle and a locked toggle.

The distinction between visible, selectable and snappable entities is important:

```text
Visible entities    = entities whose own visibility is true and whose layer is visible
Selectable entities = visible entities that are not on locked layers
Snappable entities  = visible entities, including locked-layer entities
```

Rendering, snapping and Zoom Extents use visible entities.

Selection and hit testing use selectable entities.

Locked-layer enforcement is implemented at the `CadDocument` mutation boundary, so invalid replacement/removal is blocked even if a future tool accidentally tries to modify an entity directly.

---

## Spatial indexing

The spatial index is an implementation detail of entity lookup.

It should answer:

```text
which entities have bounds intersecting this search area?
```

It should not decide visibility, selection, snapping or editability.

For this reason, spatial queries are usually followed by document-level filters such as visible-entity or selectable-entity filtering.

---

## Command architecture

User-facing document changes should go through commands.

Examples:

```text
LineTool      -> AddEntityCommand
RectangleTool -> AddEntityCommand
CircleTool    -> AddEntityCommand
MoveTool      -> MoveEntitiesCommand / TransformEntitiesCommand
CopyTool      -> CopyEntitiesCommand
DeleteTool    -> DeleteEntitiesCommand
```

This gives consistent undo/redo behavior.

---

## Current UI layout

The current UI layout is designed to scale better than a flat toolbar.

It uses:

- top bar for session/global controls;
- left vertical tool panel grouped by SELECT, DRAW and EDIT;
- central canvas;
- bottom snap/Ortho bar;
- fixed command line input;
- status bar.

This keeps tools, layer controls, snap modes and status feedback visually separate.

---

## Design implications for future work

When adding new tools:

- keep the tool in `OpenCad2D.Tools`;
- use commands for document edits;
- derive from `TwoPointToolBase` when appropriate;
- reuse command line point input automatically where possible;
- reuse Ortho constraints where appropriate;
- expose preview data without coupling to Avalonia;
- render previews in `CadCanvas`.

When adding new viewport features:

- keep document geometry unchanged;
- operate on `ViewportTransform`;
- use visible document entities when fitting or deriving view bounds.

When adding persistence:

- serialize document model concepts, not Avalonia UI state;
- avoid serializing transient tool state such as `CurrentBasePoint`.
