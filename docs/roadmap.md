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
SVG export
hit testing
selection
entity snap and overlapping selection cycling
snapping
grid configuration
drawing tools
editing/transform tools
line-based modify tools
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

- configurable grid visibility through `Grid...`;
- rectangular and isometric grid layouts;
- major/minor grid spacing;
- grid origin and screen spacing thresholds;
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

### Modify tools v1

Implemented:

- Break Point for `LineEntity`;
- Break Segment for `LineEntity`;
- Extend `LineEntity` to `LineEntity` boundary;
- Trim `LineEntity` by `LineEntity` cutting edge;
- shared Core services for line parameter/intersection/break/extend/trim;
- `ModifyEntitiesCommand`.

### SVG export

Implemented:

- `OpenCad2D.Export` project;
- SVG string/file export;
- UI integration through Export SVG in the file command bar;
- visible entities only;
- hidden layers ignored;
- locked visible layers exported;
- layer color and line weight used for stroke;
- automatic viewBox;
- dark background rectangle;
- same visual Y orientation as the canvas.

### PolylineTool v1

Implemented:

- multi-point polyline creation;
- Enter to finish open;
- C to close;
- command line, snap and Ortho support.

---

## Next recommended phase: broaden modify tools

The first line-based modify tools are implemented. The next step is to broaden their geometry support.

Recommended order:

1. add Break/Trim/Extend support for open `PolylineEntity` segments;
2. add preview refinements and clearer status messages;
3. evaluate arc/circle support once arc editing/rendering is mature;
4. add tests for multi-segment cases and degenerate cases.

---

## Follow-up phases

### SVG export improvements

Future SVG export improvements:

- optional export settings dialog;
- export selected entities only;
- preserve layer grouping with SVG `<g>` elements;
- optional transparent background;
- richer fill support when layer fill color is implemented;
- physical units / print-oriented export options.

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
