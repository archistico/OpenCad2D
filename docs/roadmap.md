# Roadmap

This roadmap describes the planned development direction for OpenCad2D.

The goal is to grow the project step by step, without turning the codebase into an over-engineered system too early. OpenCad2D should become a usable 2D CAD application, but the first priority is to keep the architecture clean, testable and understandable.

---

## Current status

OpenCad2D has moved beyond the initial prototype stage. The current application includes a tested CAD core, an Avalonia UI, command line point input, direct distance entry, Ortho mode, grip editing, JSON persistence, configurable grid display and viewport rendering culling.

Implemented foundations include:

```text
geometry primitives
coordinate systems / UCS foundation
numeric tolerance strategy
CAD entities: line, circle, arc, polyline
document collections
layers with visibility and lock state
spatial index abstraction
commands and command history
undo/redo
hit testing
selection
snapping
command line input
direct distance entry
Ortho mode
grip editing for lines and circles
internal JSON persistence
configurable grid
viewport culling
custom Avalonia canvas
CAD-style crosshair
snap markers by snap type
status feedback with measurements and rendered count
```

---

## Recently completed

1. Hidden layer behavior.
2. Locked layer behavior.
3. Selection filtering for locked layers.
4. Snap support on locked layers.
5. Vertical CAD UI layout.
6. Command line coordinate input.
7. Direct distance entry.
8. Temporary vector and measurement feedback.
9. Ortho mode.
10. CircleTool.
11. Zoom Extents.
12. Grip editing for line and circle entities.
13. Internal JSON persistence with New/Open/Save/Save As.
14. Dirty state and save confirmation dialogs.
15. Configurable grid display.
16. Viewport rendering culling.

---

## Recommended next phases

### Phase 1 — Property panel

Add a right-side property panel in read-only mode first.

Initial display:

```text
Line: type, layer, start, end, length, DX, DY
Circle: type, layer, center, radius, diameter
Multiple selection: count, common/different layers, bounding box
```

After the read-only version is stable, add numeric editing through commands.

### Phase 2 — Layer manager and layer appearance

Add a proper layer manager with create, rename, visible, locked, color, line weight, fill color and draw order.

Design rule: appearance belongs to layers, not entities.

### Phase 3 — PolylineTool

Implement a multi-point drawing tool with:

```text
click input
absolute coordinates
relative coordinates
direct distance entry
Ortho
snapping
preview of current segment
ESC to finish
C to close
```

### Phase 4 — Measure tools

Add non-mutating tools:

```text
DistanceTool
AreaTool
```

Measure tools must not execute commands or modify the document.

### Phase 5 — Transform tools

Add advanced modify tools:

```text
Rotate
Scale
Align
Match Properties
Polygon
```

All document changes must go through commands.

### Phase 6 — Text and dimensions

Add semantic annotation entities:

```text
TextEntity
LinearDimensionEntity
RadiusDimensionEntity
DiameterDimensionEntity
DimensionStyle
```

Dimensions should store definition points and compute display geometry at render time.

### Phase 7 — Performance and spatial index

After viewport culling, profile large drawings and decide whether to replace the linear spatial index with a quadtree, R-tree or uniform grid.

---

## Design documents

The following documents define future implementation rules:

- [Layer Appearance](layer-appearance.md)
- [Application Settings](application-settings.md)
- [Measure Tools](measure-tools.md)
- [Transform Tools](transform-tools.md)
- [Text and Dimensions](text-and-dimensions.md)
- [Development Options](development-options.md)
