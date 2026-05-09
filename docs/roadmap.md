# Roadmap

This roadmap describes the planned development direction for OpenCad2D.

The goal is to grow the project step by step, without turning the codebase into an over-engineered system too early.

OpenCad2D should become a usable 2D CAD application, but the first priority is to keep the architecture clean, testable and understandable.

---

## Current status

OpenCad2D currently has a working Avalonia prototype with a tested CAD core.

The project already includes:

```text
geometry primitives
coordinate systems / UCS foundation
numeric tolerance strategy
CAD entities
document collections
layers
hidden layer behavior
spatial index abstraction
commands
composite commands
undo/redo
hit testing
selection
snapping
drawing tools
editing tools
custom canvas
CAD-style crosshair
snap markers by snap type
basic ViewModel notifications
```

The UI supports drawing lines and rectangles, selecting entities, moving, copying, deleting, undo/redo, zoom, pan, grid display, snap toggles, active command feedback, layer selection and layer visibility toggling.

The current development direction is to finish the core editing foundations before adding many new entity types.

---

## Completed consolidation work

Several architectural risks have already been addressed.

### Hidden layer behavior

Hidden layers are not only hidden visually. Entities on hidden layers are also ignored by hit testing, selection and snapping.

This behavior is centralized through document visibility queries.

### UCS foundation

The project has a basic user coordinate system model.

Entities remain stored in WCS/model coordinates, while `PointerInfo` can carry both WCS and UCS points.

This prepares future support for relative coordinates, user-defined origins and rotated coordinate systems.

### Numeric tolerance strategy

`GeometryTolerance` separates tolerances for distances, angles, parameters and vector lengths.

New geometric algorithms should use this strategy instead of raw equality checks.

### Composite commands

`CompositeCommand` allows several commands to behave as one undoable operation.

This prepares the project for future operations such as trim, extend, fillet, chamfer and offset.

### ToolContext decomposition

`ToolContext` is organized into focused sub-contexts:

```text
Commands
Selection
Snapping
Coordinates
Creation
```

This avoids turning the context into a God Object.

### Document mutation boundary

Commands should mutate entities through `CadDocument`, not directly through `EntityCollection`.

This prepares future locked-layer and document-level validation rules.

### Spatial index abstraction

The entity collection owns an `ISpatialIndex` implementation.

The current implementation is linear, but hit testing, selection and snapping can query by area instead of being permanently coupled to full document scans.

### CAD-like UI feedback

The UI now has or is designed around:

```text
active command indicator
CAD-style crosshair
precise center pick box
snap markers by snap type
status updates through ViewModel notifications
```

---

## Phase 1 - Finish layer editing rules

Layer support is partially implemented.

### Current layer in the UI

The toolbar exposes the current layer.

New entities created by drawing tools should use the selected layer.

### Layer colors

Entities whose color is set to `ByLayer` should render using the color of their layer.

Entities with explicit color should use their own color.

The rendering path should continue using cached pens/brushes to avoid per-frame allocations.

### Layer visibility

Implemented behavior:

```text
hidden layer entities are not drawn
hidden layer entities are not selected
hidden layer entities are not used by snapping
```

### Locked layers

Next important layer task:

```text
locked layer entities remain visible
locked layer entities can be used as snap references
locked layer entities cannot be removed, replaced or transformed
```

Locked-layer enforcement should happen at the `CadDocument` mutation boundary.

---

## Phase 2 - Improve viewport and canvas behavior

The canvas is functional, but CAD usability depends heavily on viewport behavior.

### Zoom extents

Add a `Zoom Extents` action.

It should compute the bounding box of all visible entities and adapt the viewport so the whole drawing fits inside the canvas.

### Better grid behavior

Future grid improvements:

```text
configurable grid step
major/minor grid lines
better relation between visible grid and grid snapping
UCS-aware grid display
```

### Screen-based pick and snap tolerance

Some interactions currently use model-unit tolerances.

This can feel inconsistent when zooming.

A future improvement should define pick and snap tolerance in screen pixels and convert them to model units using the viewport transform.

### Crosshair refinement

The CAD crosshair should remain clear and non-intrusive.

Possible refinements:

```text
configurable color/opacity
crosshair constrained to drawing canvas
center pick box size setting
optional snap label near cursor
```

---

## Phase 3 - Add essential drawing tools

After layer locking and viewport improvements, add more core drawing tools.

### Circle tool

Add a tool to draw circles by center and radius.

It should use `CircleEntity` and support snapping for both center and radius point.

### Arc tool

Add a tool to draw arcs.

A first implementation can use center, start point and end point.

Later, another mode can support three-point arcs.

### Polyline tool

Add a polyline tool.

The user should be able to click multiple points and finish the polyline with a keyboard action.

A first version can support only straight segments.

Later, polyline editing and arc segments can be considered.

### Text tool

Add a basic text entity and a tool to place text.

The first version can support position, content, height and rotation.

---

## Phase 4 - Add property inspection and editing

A CAD application needs a way to inspect selected entities.

### Property panel

Add a side panel that displays information about the current selection.

Examples:

```text
selected line: type, layer, start point, end point, length
selected circle: type, layer, center, radius
multiple selection: object count and common properties
```

### Editable properties

After the read-only panel works, selected entity properties can become editable.

Examples:

```text
change entity layer
change color mode
edit line endpoints
edit circle radius
```

These modifications should go through commands so they can be undone.

---

## Phase 5 - Add document persistence

The current document exists only in memory.

A real application needs save and open support.

### Internal JSON format

Before implementing DXF, add a simple internal JSON format.

Possible extension:

```text
.opencad2d.json
```

The format should contain:

```text
layers
entities
styles
document metadata
current settings where appropriate
```

The goal is not universal CAD exchange. The goal is reliable save/reopen for OpenCad2D drawings.

### File actions

Add basic file commands:

```text
New
Open
Save
Save As
```

The UI should track the current file path.

### Unsaved changes

Add a document dirty state.

The window title can show an asterisk when the document has unsaved changes.

Example:

```text
OpenCad2D - drawing.opencad2d *
```

---

## Phase 6 - Add modify tools

After basic drawing tools and persistence, the project can grow toward practical CAD editing.

### Offset

Offset is important for architectural and technical drawings.

A first version can support line segments and polylines.

The user should select an entity, specify an offset distance and choose the side.

### Trim

Trim requires detecting intersections and cutting entities.

It should be added only after intersection handling is robust.

Trim will likely use `ReplaceEntitiesCommand` or `CompositeCommand`.

### Extend

Extend is closely related to trim.

It should probably be developed after or together with trim.

### Fillet and chamfer

Fillet and chamfer are useful modify tools but more advanced.

They should come after trim and extend.

They will likely require `CompositeCommand`, because a single user operation may replace multiple entities and add or remove geometry.

---

## Phase 7 - Add dimensions

Dimensions are essential for technical drawings, but they should not be rushed.

A dimension is not just a few lines and text. It is a semantic entity with measurement rules, text placement, extension lines and styling.

### Linear dimension

Add basic horizontal, vertical or aligned dimensions.

### Radius and diameter dimensions

Add dimensions for circles and arcs.

### Dimension styles

Later, add dimension styles for:

```text
text height
arrow size
offsets
precision
unit formatting
```

---

## Phase 8 - Add import and export

Import/export should come after the internal model is stable enough.

### SVG export

SVG export is a good first target.

It is simpler than DXF and useful for visual output.

### PDF export

PDF export can follow SVG export.

It is useful for sharing and printing drawings.

### DXF export

DXF export is important for interoperability.

A first version can support only basic entities:

```text
LINE
CIRCLE
ARC
LWPOLYLINE
TEXT
```

### DXF import

DXF import is more complex than export.

It should be added after the export path is reliable.

The first version should support only a small subset of DXF entities.

---

## Phase 9 - Performance and scalability

Some performance foundations are already in place, but more work will be needed for large drawings.

### Spatial index implementation

The current `LinearSpatialIndex` is a baseline.

Future work should evaluate:

```text
Quadtree
R-Tree
Uniform grid
hybrid approaches
```

Important requirements:

```text
fast query by bounding box
safe updates after add/remove/replace
support for large entities crossing many regions
predictable behavior with 10k+ entities
```

### Rendering optimization

Continue avoiding per-frame allocations.

Current direction:

```text
cache pens by color/thickness
avoid allocating brushes per entity per frame
limit expensive geometry calculations during Render
```

Future improvements may include viewport culling and cached drawing primitives.

---

## Phase 10 - Project quality and automation

Project quality tasks should be added early and maintained continuously.

### GitHub Actions

Add a CI workflow that runs restore, build and tests on every push and pull request.

First workflow:

```text
dotnet restore
dotnet build
dotnet test
```

### Issue templates

Add issue templates for bugs, features and tasks.

### Documentation

Keep technical documentation in the `docs/` folder.

Core documents:

```text
architecture.md
tools.md
snapping.md
commands.md
roadmap.md
```

Future documents may include:

```text
layers.md
rendering.md
persistence.md
spatial-indexing.md
coordinate-systems.md
```

---

## Recommended next steps

Recommended next concrete steps:

```text
1. Implement locked layer behavior through CadDocument mutation validation.
2. Add layer lock UI checkbox.
3. Add Zoom Extents.
4. Add CircleTool.
5. Add ArcTool or PolylineTool.
6. Add property panel for selected entities.
7. Add internal JSON save/load.
8. Replace LinearSpatialIndex with a real spatial index when performance requires it.
9. Add GitHub Actions.
```

This order finishes the editing foundation before adding too many advanced tools.

---

## Long-term direction

The long-term goal is to build a small but serious 2D CAD application.

OpenCad2D should remain open source, understandable and extensible.

It should not try to implement every feature at once.

The preferred development style is incremental:

```text
one concept
one set of tests
one working UI improvement
one architectural rule preserved
```
