# Architecture

OpenCad2D is organized around a simple principle: CAD logic must remain independent from the graphical user interface.

The Avalonia application is the presentation layer. It draws the document, receives mouse and keyboard input, converts screen coordinates into CAD coordinates and forwards input to the tool system.

Geometry, entities, snapping, selection, commands, layers and editing behavior live in dedicated libraries and can be tested without launching the desktop application.

This separation is the most important design rule in the project.

---

## Solution structure

The solution is divided into six main projects.

```text
src/
  OpenCad2D.Geometry/
  OpenCad2D.Core/
  OpenCad2D.Interaction/
  OpenCad2D.Tools/
  OpenCad2D.Persistence/
  OpenCad2D.App/
```

Each project has a specific responsibility.

`OpenCad2D.Geometry` contains low-level geometric primitives, coordinate systems, geometric operations, transformations and numeric tolerance rules.

`OpenCad2D.Core` contains the CAD document model, entities, layers, styles, spatial indexing, commands and undo/redo infrastructure.

`OpenCad2D.Interaction` contains UI-independent interaction services such as hit testing, selection and object snapping.

`OpenCad2D.Tools` contains UI-independent CAD tools, controllers, tool contexts, command line parsing, grip editing and the runtime workspace.

`OpenCad2D.Persistence` contains the internal JSON persistence format, DTOs, serializer, file I/O helpers and persistence-specific exceptions.

`OpenCad2D.App` is the Avalonia desktop application.

The direction of dependencies should remain clear.

```text
OpenCad2D.App
  -> OpenCad2D.Persistence
      -> OpenCad2D.Core
          -> OpenCad2D.Geometry
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
- host file dialogs and save-confirmation dialogs;
- convert screen coordinates to model coordinates;
- forward input to the workspace/tool system;
- manage viewport operations such as pan, zoom and Zoom Extents.

Avalonia should not:

- create CAD entities directly;
- decide document mutation rules;
- implement geometric algorithms;
- implement selection rules;
- implement locked-layer rules;
- implement command undo/redo behavior;
- contain JSON serialization details.

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
- command history;
- command history generation for dirty-state tracking.

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
- input constraint services such as Ortho;
- grip providers and `GripEditTool`.

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


## Grip editing architecture

Grip editing belongs to `OpenCad2D.Tools`, not to Avalonia.

The App renders grip markers and forwards pointer input. The actual grip state machine, provider lookup, preview entity and final replacement command live in the tool layer.

Current grip model:

```text
LineEntity   -> start, midpoint, end
CircleEntity -> center, four quadrant grips
```

Activation rule:

```text
TAB with exactly one selected entity -> edit that entity
TAB with multiple selected entities  -> edit the last selected entity
TAB with no selection                -> do nothing
```

The grip tool uses `ReplaceEntitiesCommand` for committed edits. This preserves entity ids and keeps undo/redo behavior consistent. Preview does not modify the document.

`CadCanvas` may render cold, hot and warm grips, but it must not decide how a grip modifies an entity.

---

## Persistence layer

`OpenCad2D.Persistence` is responsible for saving and loading drawings.

It depends only on:

- `OpenCad2D.Core`;
- `OpenCad2D.Geometry`.

It must not depend on:

- `OpenCad2D.App`;
- `OpenCad2D.Tools`;
- `OpenCad2D.Interaction`;
- Avalonia.

The internal file format is JSON with the extension:

```text
.opencad2d.json
```

Version 1 serializes:

- format version;
- save timestamp;
- current layer id;
- viewport pan and zoom;
- layers, including color, line weight, visibility and locked state;
- line entities;
- circle entities;
- arc entities;
- polyline entities.

The serializer maps between domain objects and DTOs. Domain entities must not contain persistence attributes or JSON-specific logic.

Unknown entity types are skipped where possible so that older builds can partially load files created by newer builds. Unsupported document versions throw a specific exception.

---

## App layer

`OpenCad2D.App` is the Avalonia application.

Current responsibilities:

- render visible entities;
- render selected entities;
- render preview entities;
- render CircleTool preview;
- render grip markers and grip-edit preview;
- render temporary base-point/vector feedback;
- render the grid;
- render snap markers;
- render the crosshair;
- host the top bar;
- host the left tool panel;
- host the snap/Ortho bar;
- host the command line;
- host file command UI;
- host save/open dialogs;
- host save-confirmation dialogs;
- host the status bar;
- forward pointer input to the workspace;
- forward typed command-line input to the workspace;
- call `OpenCad2D.Persistence` for New/Open/Save/Save As;
- apply loaded viewport state;
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


### File commands and dirty state

The App owns document-level file commands:

```text
New
Open
Save
Save As
```

These commands use Avalonia storage dialogs and the persistence serializer. The serializer knows the file format; the App knows the user workflow.

Dirty state is exposed by `CadWorkspace.IsDirty`, which compares the current `CommandHistory.CurrentGeneration` with the last saved generation. After saving or loading, the workspace calls `MarkSaved()`.

Before New, Open or window close, the App shows a save-confirmation dialog when the document is dirty.

```text
Save       -> save and continue
Don't Save -> discard and continue
Cancel     -> abort
```

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

---

## Rendering and viewport culling

Rendering is an App-layer concern. `CadCanvas` may use the viewport transform to compute the visible world bounds and draw only entities whose bounding boxes intersect that area.

This optimization must remain purely visual:

- it must not remove entities from the document;
- it must not clear selection;
- it must not change snap state;
- it must not bypass layer visibility rules.

The rendering order should remain:

```text
background
grid and axes
visible entities culled by viewport
selection overlay
preview entities
grip markers
snap marker
crosshair
```

The status bar may expose a rendered/total count to help profile large drawings.

---

## Future layer appearance rule

The project direction is that entity appearance should be owned by layers. Future appearance work should avoid adding per-entity color, line weight or fill color. Entities should carry geometry and a layer reference; layers should own stroke color, line weight, optional fill color and draw order.

Layer appearance changes should go through commands so they are undoable and mark the document dirty. Persistence should serialize appearance on layers, not on entities.

---

## Settings separation

The project distinguishes three kinds of state:

```text
document content        -> saved in .opencad2d.json
document drawing setup  -> future DrawingSettings in the document file
user/session settings   -> future settings.json outside the document
```

Session settings such as window position, shortcut preferences and last opened file must not be stored in drawing files. Drawing settings such as units, precision and default text/dimension values should be stored in the document and changed through commands.
