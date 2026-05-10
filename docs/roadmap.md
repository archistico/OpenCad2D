# Roadmap

This roadmap describes the planned development direction for OpenCad2D.

The project grows in small testable steps. Each phase should compile, pass tests and update documentation before the next feature begins.

---

## Current implemented foundations

OpenCad2D currently includes:

```text
geometry primitives
coordinate systems / UCS foundation
numeric tolerance strategy
CAD entities
layers
Layer Manager v1
Property Panel v1
hidden layer behavior
locked layer behavior
spatial index abstraction
viewport culling
commands
composite commands
undo/redo
persistence
hit testing
selection
snapping
grid configuration
drawing tools
editing/transform tools
grip editing
custom Avalonia canvas
CAD-style crosshair
command line input
Ortho mode
```

---

## Recently completed phases

### Persistence and file bar

Implemented:

- `.opencad2d.json` serializer;
- file commands;
- stable top file command bar;
- dirty state;
- Save changes dialog;
- viewport save/restore.

### Grid and viewport performance

Implemented:

- configurable grid visibility;
- major/minor grid spacing;
- grid display separated from grid snap;
- viewport culling;
- rendered entity count.

### Property Panel v1

Implemented:

- right-side read-only panel;
- no-selection state;
- single entity details;
- multiple selection summary.

### Layer Manager v1

Implemented:

- separate manager window;
- create/delete/rename layer;
- visible/locked;
- color/lineweight;
- current layer;
- batch undoable update command;
- current layer must be visible and unlocked.

### Transform tools

Implemented:

- Rotate;
- Scale;
- Align with optional scale confirmation.

### PolylineTool v1

Implemented:

- multi-point polyline creation;
- Enter to finish open;
- C to close;
- command line, snap and Ortho support.

---

## Next recommended phase: modify tools

The next serious CAD editing area is:

```text
Break
Trim
Extend
```

Recommended order:

1. **BreakTool** — lowest complexity because it splits one entity.
2. **ExtendTool** — extends one entity to a boundary.
3. **TrimTool** — trims one entity against boundaries and may produce more cases.

### BreakTool v1 scope

Recommended first scope:

```text
LineEntity only
pick entity
pick break point
split line into two line entities
undoable operation
```

Then extend to:

```text
CircleEntity
PolylineEntity
ArcEntity when mature
```

### ExtendTool v1 scope

Recommended first scope:

```text
LineEntity to LineEntity boundary
select/pick entity to extend
pick boundary
replace extended entity
```

### TrimTool v1 scope

Recommended first scope:

```text
LineEntity trimmed by LineEntity boundary
pick cutting edge
pick side/part to remove
replace or remove resulting geometry
```

---

## Follow-up phases

### Polyline grip editing

Add grip provider for `PolylineEntity`:

- vertex grips;
- optional midpoint insertion grips later;
- undoable vertex edits.

### Measure tools

Implement non-mutating tools:

- Distance;
- Area.

These tools should not create commands and should not change the document.

### Text and dimensions

Future annotation features:

- `TextEntity`;
- dimension entities;
- dimension styles;
- semantic dimensions, not groups of primitive lines/text.

### Layer appearance v2

Future layer model expansions:

- fill color;
- draw order;
- layer reorder command;
- fill rendering for closed entities.

### Application settings

Future user-local settings:

- window state;
- shortcuts;
- last opened file;
- grid visibility defaults.

These are user-local and must stay separate from drawing persistence.

---

## Development rules

- Keep phases small.
- Prefer testable services before UI integration.
- Avoid direct document mutation.
- Use commands for user-facing changes.
- Keep CAD logic out of Avalonia.
- Update docs after each milestone.
