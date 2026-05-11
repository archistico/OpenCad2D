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
Layer Manager with line format selection
Line Format Manager
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
DXF export
hit testing
selection
entity snap and overlapping selection cycling
snapping
grid configuration
drawing tools
editing/transform tools
modify tools for lines/arcs/circles/polylines where supported
grip editing
custom Avalonia canvas
CAD-style crosshair
command line input
Ortho mode
Polar Tracking with Off/90°/45°/30°/15°
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


### Line format system

Implemented:

- reusable `LineFormat` model;
- built-in formats: Continua, Asse, Tratteggiata, Tratto due punti, Tratto e punto;
- `Layer.LineFormatId`;
- canvas rendering from line formats;
- SVG export from line formats, including dash arrays;
- JSON persistence for line formats;
- Layer Manager combo box for selecting a line format;
- Line Format Manager for editing formats;
- undoable updates through `UpdateLineFormatsCommand`.


### Polar Tracking

Implemented:

- `AngleConstraintSettings`;
- `AngleConstraintService`;
- `ToolInputConstraintService`;
- top-bar `Polar:` selector with `Off`, `90°`, `45°`, `30°`, `15°`;
- integration in two-point-style tools, Move and Polyline;
- preview and direct distance input using the constrained direction;
- snap first, then angular constraint.

### Property Panel v1

Implemented:

- right-side read-only panel;
- no-selection state;
- single entity details;
- multiple selection summary.

### Layer Manager

Implemented:

- separate manager window;
- create/delete/rename layer;
- visible/locked;
- line format assignment;
- current layer;
- batch undoable update command;
- current layer must be visible and unlocked;
- top-bar `Assegna` action to move selected entities to the current layer.

### Transform tools

Implemented:

- Rotate;
- Scale;
- Align with optional scale confirmation.

### Drawing tool expansion

Implemented:

- `ArcTool`;
- `ArcThreePointsTool`;
- `RectangleBySidesTool`;
- preview and tests for these tools.

### Modify tools expansion

Implemented:

- Break Point for `LineEntity`;
- Break Segment for `LineEntity`;
- Extend with line/circle/arc/polyline boundaries and line/arc/open-polyline targets;
- Trim with line/circle/arc/polyline cutting edges and line/circle/arc/polyline targets;
- shared Core services for entity intersections, trim and extend;
- `ModifyEntitiesCommand`.

### SVG export
DXF export

Implemented:

- `OpenCad2D.Export` project;
- SVG string/file export;
- UI integration through Export SVG in the file command bar;
- visible entities only;
- hidden layers ignored;
- locked visible layers exported;
- line formats used for stroke color, stroke width and dash style;
- automatic viewBox;
- dark background rectangle;
- same visual Y orientation as the canvas.


### DXF export

Implemented:

- `OpenCad2D.Export.Dxf` infrastructure;
- AutoCAD 2000 ASCII DXF (`AC1015`);
- `HEADER`, `TABLES`, `LTYPE`, `LAYER`, `ENTITIES` and `EOF` sections;
- geometric entity export: `LINE`, `CIRCLE`, `ARC`, `LWPOLYLINE`;
- layer table export;
- linetype table export;
- layer appearance from `LineFormat`;
- entities written as `BYLAYER`;
- hidden layer handling;
- locked layer table flag;
- Y flip by exported content bounds to match visual orientation in tested viewers;
- file command bar integration through `Export DXF`.

### PolylineTool v1

Implemented:

- multi-point polyline creation;
- Enter to finish open;
- C to close;
- command line, snap, Ortho and Polar Tracking support.

---

## Next recommended phase: annotation and editing polish

DXF export and baseline measure tools are now implemented enough to be useful. The next step should be practical annotation support and editing UX refinement.

Recommended order:

1. validate exported DXF files in LibreCAD, QCAD, AutoCAD/DWG TrueView and at least one online viewer;
2. create a small compatibility checklist for layer colors, linetypes, lineweights, arcs and Y orientation;
3. add `TextEntity` and a simple `TextTool`;
4. use the measurement services as groundwork for basic dimension entities;
5. improve Trim/Extend previews so the exact removed/extended portion is highlighted;
6. add clearer status messages when an operation is ignored;
7. broaden Break Point and Break Segment beyond `LineEntity`;
8. add more tests for tangent, near-tangent, overlapping and multi-segment cases.

---

## Follow-up phases

### Export improvements

Future SVG/DXF export improvements:

- optional export settings dialog;
- export selected entities only;
- preserve layer grouping with SVG `<g>` elements;
- optional transparent SVG background;
- richer fill support when layer fill color is implemented;
- text, dimensions, hatches and blocks in DXF when the model supports them;
- physical units / print-oriented export options.

### Polyline grip editing

Add grip provider for `PolylineEntity`:

- vertex grips;
- optional midpoint insertion grips later;
- undoable vertex edits.

### Measure tools

Implemented baseline non-mutating tools:

- Distance;
- Entity;
- Angle;
- Area for closed polylines.

Follow-up improvements:

- Measure Point / Coordinates;
- Area by picked points;
- measurement history panel;
- copy result to clipboard;
- configurable display precision and optional unit labels when drawing settings are introduced.

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
