# Roadmap checklist

This roadmap describes the planned development direction for OpenCad2D.

The project grows in small testable steps. Each phase should compile, pass tests and update documentation before the next feature begins.

Legend:

```text
[x] done
[ ] planned
```

---

## Current implemented foundations

OpenCad2D currently includes:

- [x] geometry primitives;
- [x] coordinate systems / UCS foundation;
- [x] numeric tolerance strategy;
- [x] CAD entities;
- [x] layers;
- [x] Layer Manager with line format selection;
- [x] Line Format Manager;
- [x] Text Format Manager;
- [x] Property Panel v1;
- [x] hidden layer behavior;
- [x] locked layer behavior;
- [x] spatial index abstraction;
- [x] viewport culling;
- [x] commands;
- [x] composite commands;
- [x] undo/redo;
- [x] persistence;
- [x] SVG export;
- [x] DXF export;
- [x] hit testing;
- [x] selection;
- [x] entity snap and overlapping selection cycling;
- [x] snapping;
- [x] grid configuration;
- [x] drawing tools;
- [x] editing/transform tools;
- [x] modify tools for lines/arcs/circles/polylines where supported;
- [x] grip editing;
- [x] custom Avalonia canvas;
- [x] CAD-style crosshair;
- [x] command line input;
- [x] Ortho mode;
- [x] Polar Tracking with Off/90°/45°/30°/15°.

---

## Recently completed work

### Persistence and file bar

- [x] `.opencad2d.json` serializer;
- [x] file commands;
- [x] stable top file command bar;
- [x] dirty state;
- [x] Save changes dialog;
- [x] viewport save/restore.

### Grid and viewport performance

- [x] configurable grid visibility through `Grid...`;
- [x] rectangular and isometric grid layouts;
- [x] major/minor grid spacing;
- [x] grid origin and screen spacing thresholds;
- [x] grid display separated from grid snap;
- [x] viewport culling;
- [x] rendered entity count.

### Line format system

- [x] reusable `LineFormat` model;
- [x] built-in formats: Continua, Asse, Tratteggiata, Tratto due punti, Tratto e punto;
- [x] `Layer.LineFormatId`;
- [x] canvas rendering from line formats;
- [x] SVG export from line formats, including dash arrays;
- [x] JSON persistence for line formats;
- [x] Layer Manager combo box for selecting a line format;
- [x] Line Format Manager for editing formats;
- [x] undoable updates through `UpdateLineFormatsCommand`.

### Text format and single-line text system

- [x] reusable `TextFormat` model;
- [x] `TextFormatId` built-ins: `Standard`, `Title`, `Annotation`, `Small`;
- [x] document-level `TextFormatCollection`;
- [x] default text formats with visible model-space heights;
- [x] Text Format Manager opened from the top CAD bar through `Text formats...`;
- [x] edit text format name, font family, height, color, bold and italic;
- [x] preview inside the Text Format Manager;
- [x] undoable updates through `UpdateTextFormatsCommand`;
- [x] protection against deleting built-in text formats;
- [x] protection against deleting text formats used by text entities;
- [x] JSON persistence for text formats.

### Point entity

- [x] `PointEntity`;
- [x] `PointTool`;
- [x] point rendering in the canvas;
- [x] point selection and hit testing;
- [x] point grip editing;
- [x] point snap support;
- [x] JSON persistence;
- [x] SVG export;
- [x] DXF export as native `POINT`;
- [x] Property Panel support;
- [x] unit tests.

### Single-line text entity

- [x] `TextEntity`;
- [x] `TextTool`;
- [x] async text input dialog;
- [x] current text format stored in tool creation settings;
- [x] insertion point, text, rotation and text format id;
- [x] estimated bounding box for hit testing and culling;
- [x] text rendering from `TextFormat`;
- [x] selection highlight rendering;
- [x] corrected screen rotation direction;
- [x] text grip editing through insertion point;
- [x] snap support through insertion point;
- [x] Property Panel support;
- [x] JSON persistence;
- [x] SVG export as `<text>`;
- [x] DXF export as native `TEXT`;
- [x] unit tests and UI-level transform tests.

### Polar Tracking

- [x] `AngleConstraintSettings`;
- [x] `AngleConstraintService`;
- [x] `ToolInputConstraintService`;
- [x] top-bar `Polar:` selector with `Off`, `90°`, `45°`, `30°`, `15°`;
- [x] integration in two-point-style tools, Move and Polyline;
- [x] preview and direct distance input using the constrained direction;
- [x] snap first, then angular constraint.

### Property Panel v1

- [x] right-side read-only panel;
- [x] no-selection state;
- [x] single entity details;
- [x] multiple selection summary;
- [x] Point details;
- [x] Text details.

### Layer Manager

- [x] separate manager window;
- [x] create/delete/rename layer;
- [x] visible/locked;
- [x] line format assignment;
- [x] current layer;
- [x] batch undoable update command;
- [x] current layer must be visible and unlocked;
- [x] top-bar `Assegna` action to move selected entities to the current layer.

### Transform tools

- [x] Rotate;
- [x] Scale;
- [x] Align with optional scale confirmation.

### Drawing tool expansion

- [x] `PointTool`;
- [x] `TextTool`;
- [x] `ArcTool`;
- [x] `ArcThreePointsTool`;
- [x] `RectangleBySidesTool`;
- [x] preview and tests for these tools.

### Modify tools expansion

- [x] Break Point for `LineEntity`;
- [x] Break Segment for `LineEntity`;
- [x] Extend with line/circle/arc/polyline boundaries and line/arc/open-polyline targets;
- [x] Trim with line/circle/arc/polyline cutting edges and line/circle/arc/polyline targets;
- [x] shared Core services for entity intersections, trim and extend;
- [x] `ModifyEntitiesCommand`.

### SVG export

- [x] `OpenCad2D.Export` project;
- [x] SVG string/file export;
- [x] UI integration through Export SVG in the file command bar;
- [x] visible entities only;
- [x] hidden layers ignored;
- [x] locked visible layers exported;
- [x] line formats used for stroke color, stroke width and dash style;
- [x] point export;
- [x] text export;
- [x] automatic viewBox;
- [x] dark background rectangle;
- [x] same visual Y orientation as the canvas.

### DXF export

- [x] `OpenCad2D.Export.Dxf` infrastructure;
- [x] AutoCAD 2000 ASCII DXF (`AC1015`);
- [x] `HEADER`, `TABLES`, `LTYPE`, `LAYER`, `ENTITIES` and `EOF` sections;
- [x] geometric entity export: `LINE`, `CIRCLE`, `ARC`, `LWPOLYLINE`;
- [x] point export as `POINT`;
- [x] text export as `TEXT`;
- [x] layer table export;
- [x] linetype table export;
- [x] layer appearance from `LineFormat`;
- [x] entities written as `BYLAYER` where appropriate;
- [x] hidden layer handling;
- [x] locked layer table flag;
- [x] Y flip by exported content bounds to match visual orientation in tested viewers;
- [x] file command bar integration through `Export DXF`.

### PolylineTool v1

- [x] multi-point polyline creation;
- [x] Enter to finish open;
- [x] C to close;
- [x] command line, snap, Ortho and Polar Tracking support.

---

## v0.3 - Points, text and DXF validation

### Feature & UX

- [x] `PointEntity`;
- [x] `PointTool`;
- [x] `TextEntity` for single-line text;
- [x] `TextTool`;
- [x] reusable `TextFormat` configuration;
- [x] Text Format Manager;
- [ ] grip editing for polylines: move vertices;
- [ ] grip editing for polylines: insert vertices;
- [ ] grip editing for polylines: delete vertices.

### Stability & Test

- [ ] validate DXF export in LibreCAD;
- [ ] validate DXF export in QCAD;
- [ ] validate DXF export in Autodesk DWG TrueView;
- [ ] create compatibility checklist for colors;
- [ ] create compatibility checklist for linetypes;
- [ ] create compatibility checklist for lineweights;
- [x] add systematic tests for `PointEntity`;
- [x] add systematic tests for `TextEntity`;
- [x] add systematic tests for text formats;
- [x] add systematic tests for point/text persistence;
- [x] add systematic tests for point/text SVG export;
- [x] add systematic tests for point/text DXF export;
- [ ] add more edge-case geometric tests for tangents;
- [ ] add more edge-case geometric tests for overlaps;
- [ ] add more edge-case geometric tests for near-tangents.

### Documentation

- [x] update README for points and text;
- [x] document text formats;
- [x] update text and dimensions documentation;
- [x] update persistence documentation;
- [x] update export documentation;
- [x] add roadmap checklist;
- [ ] add external DXF validation notes after manual testing.

### Completion criteria

- [x] points are usable in normal drawing workflow;
- [x] single-line text is usable in normal drawing workflow;
- [x] text formats are document-level reusable configuration;
- [x] save/load works for points, text and text formats;
- [x] SVG/DXF export includes points and text;
- [x] tests pass after point and text implementation;
- [ ] DXF compatibility validated externally.

---

## v0.4 - Basic dimensions

### Feature & UX

- [ ] linear dimension;
- [ ] aligned dimension;
- [ ] angular dimension;
- [ ] radius dimension;
- [ ] diameter dimension;
- [ ] base dimension style;
- [ ] dimension text format/style integration;
- [ ] preview while placing dimensions.

### Stability & Test

- [ ] tests for horizontal linear dimension;
- [ ] tests for vertical linear dimension;
- [ ] tests for aligned dimension;
- [ ] tests for angular dimension;
- [ ] tests for radius dimension;
- [ ] tests for diameter dimension;
- [ ] persistence tests for dimensions;
- [ ] SVG export tests for dimensions;
- [ ] DXF export tests for dimensions;
- [ ] undo/redo tests for dimension tools.

### Editing polish

- [ ] Trim/Extend preview with highlighted portion;
- [ ] clearer status messages for non-applicable operations;
- [ ] systematic tests for dimension edge cases.

---

## v0.5 - Advanced editing and refinement

### Feature & UX

- [ ] Trim with two cutting edges;
- [ ] Break Point on arcs;
- [ ] Break Point on circles;
- [ ] Break Point on polylines;
- [ ] Break Segment on arcs;
- [ ] Break Segment on circles;
- [ ] Break Segment on polylines;
- [ ] improved Extend on all supported entity types.

### Stability & Test

- [ ] systematic Trim tests;
- [ ] systematic Extend tests;
- [ ] systematic Break tests;
- [ ] locked-layer behavior in all modify tools;
- [ ] hidden-layer behavior in all modify tools;
- [ ] robust intersections for multi-segment entities;
- [ ] robust intersections for near-tangent cases;
- [ ] no regressions in undo/redo for existing tools.

---

## v0.6 - Real command line and Property Panel v2

### Feature & UX

- [ ] real command aliases;
- [ ] absolute coordinates through command input;
- [ ] relative coordinates through command input;
- [ ] command history;
- [ ] contextual feedback;
- [ ] clear command errors;
- [ ] right-click to repeat last command;
- [ ] editable Property Panel v2;
- [ ] edit properties for all core entities;
- [ ] edit text content, rotation and text format;
- [ ] edit layer and common entity properties.

### Stability & Test

- [ ] all Property Panel edits must be undoable commands;
- [ ] tests for empty input;
- [ ] tests for invalid input;
- [ ] tests for `Esc`;
- [ ] tests for `Enter`;
- [ ] tests for command repetition;
- [ ] tests for text property editing.

---

## v0.7 - Interoperability: DXF import and PDF export

### Feature & UX

- [ ] import DXF entities;
- [ ] import DXF layers;
- [ ] import DXF linetypes;
- [ ] import DXF colors;
- [ ] import DXF lineweights;
- [ ] import DXF text where supported;
- [ ] skip unsupported DXF entities with readable log;
- [ ] export PDF;
- [ ] PDF page format;
- [ ] PDF scale;
- [ ] PDF margins;
- [ ] SVG layer grouping;
- [ ] optional transparent SVG background.

### Stability & Test

- [ ] DXF import error handling;
- [ ] DXF export -> import round-trip tests;
- [ ] PDF export tests;
- [ ] unsupported DXF entity tests.

---

## v0.8 - UI, colors and settings

### Feature & UX

- [ ] color picker improvements for layer and formats;
- [ ] lateral toolbar in two columns: Draw / Edit;
- [ ] application settings;
- [ ] shortcuts persistence;
- [ ] last file persistence;
- [ ] default grid settings;
- [ ] finalized visual identity;
- [ ] application icon;
- [ ] favicon;
- [ ] final XAML theme;
- [ ] draw order / Z-order independent from layers.

### Stability & Test

- [ ] snap icons: active / detected / disabled states;
- [ ] dark theme regression tests;
- [ ] settings persistence tests;
- [ ] draw order tests.

---

## v0.9 - Release candidate

### Rule

- [ ] no new features;
- [ ] no avoidable architecture churn;
- [ ] bug fixing only;
- [ ] test expansion;
- [ ] documentation completion;
- [ ] safe performance work only.

### Stability & Test

- [ ] end-to-end workflow: draw -> save -> reopen;
- [ ] end-to-end workflow: draw -> annotate -> export DXF;
- [ ] end-to-end workflow: draw -> annotate -> export SVG/PDF;
- [ ] end-to-end workflow: import DXF -> modify -> export;
- [ ] performance review for rendering;
- [ ] performance review for large files;
- [ ] performance review for snap and hit testing;
- [ ] bug fixing from v0.8 feedback.

### Documentation

- [ ] complete user documentation;
- [ ] complete developer documentation;
- [ ] installation guide;
- [ ] first-use guide;
- [ ] import/export guide;
- [ ] shortcuts guide;
- [ ] full changelog;
- [ ] release candidate notes.

---

## v1.0 - First stable release

### Criteria

- [ ] text and basic dimensions operational;
- [ ] DXF import and export verified;
- [ ] PDF export working;
- [ ] editable Property Panel for all primary entities;
- [ ] real command line with aliases and coordinates;
- [ ] stable undo/redo on all primary tools;
- [ ] reliable `.opencad2d.json` persistence;
- [ ] no known crash in common operations;
- [ ] complete user documentation;
- [ ] updated developer documentation;
- [ ] systematic regression tests;
- [ ] GitHub release package ready.

---

## Post v1.0 advanced features

- [ ] blocks;
- [ ] groups;
- [ ] ellipses;
- [ ] splines;
- [ ] background/reference raster images;
- [ ] SVG import;
- [ ] associative dimensions;
- [ ] layer appearance v2: fill and advanced draw order;
- [ ] advanced style system for line/text/dimension/fill/entity;
- [ ] technical I/T profiles;
- [ ] Blender export;
- [ ] advanced UI customization.

---

## Development rules

- Keep phases small.
- Prefer testable services before UI integration.
- Avoid direct document mutation.
- Use commands for user-facing changes.
- Keep CAD logic out of Avalonia.
- Update docs after each milestone.
- A phase is not complete until it compiles, tests pass and documentation is updated.
