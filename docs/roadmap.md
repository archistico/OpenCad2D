# Roadmap

This roadmap describes the planned development direction for OpenCad2D.

The goal is to grow the project step by step, without turning the codebase into an over-engineered system too early.

OpenCad2D should become a usable 2D CAD application, but the first priority is to keep the architecture clean, testable and understandable.

---

## Current status

OpenCad2D currently has a working prototype with a basic Avalonia UI and a tested CAD core.

The project already includes geometry primitives, CAD entities, document collections, layers, commands, undo/redo, hit testing, selection, snapping, drawing tools, editing tools and a custom canvas.

The current UI supports drawing lines and rectangles, selecting entities, moving, copying, deleting, undo/redo, zoom, pan, grid display, snap markers and snap toggles.

The next phase is to make the prototype more CAD-like by improving layers, viewport behavior, entity styling, document persistence and core drawing tools.

---

## Phase 1 - Consolidate the current prototype

The current prototype works, but some concepts need to be connected more completely.

The most important one is layer support.

The core already contains `Layer`, `LayerId` and `LayerCollection`. The tools also have access to `CurrentLayerId`. The next step is to expose the current layer in the UI and make layer properties visible in the canvas.

### Current layer in the UI

Add a layer selector to the toolbar.

The user should be able to choose the current layer before drawing.

New entities created by drawing tools should use the selected layer.

Initial demo layers can be simple:

```text
0
Walls
Furniture
Annotations
```

This does not need a full layer manager yet. A simple ComboBox is enough for the first version.

### Layer colors

Entities whose color is set to `ByLayer` should be rendered using the color of their layer.

Entities with an explicit color should use their own color.

This will make the drawing visually clearer and will give immediate value to the layer system.

### Layer visibility

Hidden layers should not be drawn.

Entities on hidden layers should also be ignored by hit testing, selection and snapping.

This avoids selecting or snapping to geometry that the user cannot see.

### Locked layers

Locked layers should be visible but not editable.

Entities on locked layers should not be selectable or modifiable.

The exact behavior can be refined later, but the first rule should be simple: visible but protected.

---

## Phase 2 - Improve viewport and canvas behavior

The canvas is already functional, but CAD usability depends heavily on viewport behavior.

### Zoom extents

Add a `Zoom Extents` action.

This should compute the bounding box of all visible entities and adapt the viewport so the whole drawing fits inside the canvas.

This is a very useful command even in an early CAD prototype.

### Better grid behavior

The current grid works, but it can become more useful.

Future improvements should include a configurable grid step, clearer major and minor grid lines, and better alignment between the visual grid and grid snapping.

### Screen-based pick tolerance

Some interactions currently use tolerance values in model units.

This can feel inconsistent when zooming in or out.

A future improvement should define pick and snap tolerance in screen pixels, then convert them to model units using the viewport transform.

This is closer to typical CAD behavior.

### Better snap markers

The current snap marker is functional.

Later, different snap kinds should have different visual markers.

For example, endpoint, midpoint, center, intersection, perpendicular, tangent and grid snaps could each have a distinct marker shape.

---

## Phase 3 - Add essential drawing tools

Once layers and viewport behavior are more stable, the next step is to add more CAD entities and tools.

### Circle tool

Add a tool to draw circles by center and radius.

This should use the existing `CircleEntity` and should support snapping for both the center point and radius point.

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

The first version can support position, text content, height and rotation.

Text is important for technical drawings and annotations.

---

## Phase 4 - Add property inspection and editing

A CAD application needs a way to inspect selected entities.

The first version can be simple and mostly read-only.

### Property panel

Add a side panel that displays information about the current selection.

For one selected line, it can show type, layer, start point, end point and length.

For one selected circle, it can show type, layer, center and radius.

For multiple selected entities, it can show the number of selected objects and common properties.

### Editable properties

After the read-only panel works, selected entity properties can become editable.

For example, the user may change the layer of selected entities or edit a circle radius directly.

These modifications should go through commands so they can be undone.

---

## Phase 5 - Add document persistence

The current document exists only in memory.

A real application needs save and open support.

### Internal JSON format

Before implementing DXF, add a simple internal JSON format.

A possible extension is:

```text
.opencad2d.json
```

The format should contain layers, entities, styles and document metadata.

The goal is not to create a universal CAD exchange format. The goal is to save and reopen OpenCad2D drawings reliably.

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

For example:

```text
OpenCad2D - drawing.opencad2d *
```

---

## Phase 6 - Add modify tools

After the basic drawing tools and persistence are in place, the project can grow toward more practical CAD editing.

### Offset

Offset is important for architectural and technical drawings.

A first version can support line segments and polylines.

The user should select an entity, specify an offset distance and choose the side.

### Trim

Trim is more complex because it requires detecting intersections and cutting entities.

It should be added only after the intersection system is strong enough.

### Extend

Extend is closely related to trim.

It should probably be developed after or together with trim.

### Fillet and chamfer

Fillet and chamfer are useful modify tools, but they are more advanced.

They should come after trim and extend.

---

## Phase 7 - Add dimensions

Dimensions are essential for technical drawings, but they should not be rushed.

A dimension is not just a few lines and text. It is a semantic entity with measurement rules, text placement, extension lines and styling.

### Linear dimension

Add basic horizontal, vertical or aligned dimensions.

### Radius and diameter dimensions

Add dimensions for circles and arcs.

### Dimension styles

Later, add dimension styles for text height, arrow size, offsets and formatting.

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

## Phase 9 - Project quality and automation

Some project-quality tasks should be added early and maintained continuously.

### GitHub Actions

Add a CI workflow that runs restore, build and tests on every push and pull request.

A first workflow can simply run:

```text
dotnet restore
dotnet build
dotnet test
```

### Issue templates

Add issue templates for bugs, features and tasks.

This will make the repository more organized as the project grows.

### Documentation

Keep technical documentation in the `docs/` folder.

The first documentation set should include:

```text
architecture.md
tools.md
snapping.md
commands.md
roadmap.md
```

More documents can be added later for persistence, rendering, layers and import/export.

---

## Recommended next steps

The next concrete development steps should be:

```text
1. Add the current layer selector to the UI.
2. Render entities using layer colors.
3. Implement hidden layer behavior.
4. Implement locked layer behavior.
5. Add Zoom Extents.
6. Add CircleTool.
7. Add PolylineTool.
8. Add internal JSON save/load.
9. Add GitHub Actions.
```

This order improves the existing prototype before adding too many new entities and tools.

It also keeps the architecture aligned with the current design: core behavior remains testable, while the Avalonia UI stays mostly responsible for presentation and input forwarding.

---

## Long-term direction

The long-term goal is to build a small but serious 2D CAD application.

OpenCad2D should remain open source, understandable and extensible.

It should not try to implement every feature at once.

The preferred development style is incremental: one concept, one set of tests, one working UI improvement at a time.

