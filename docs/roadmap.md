# Roadmap checklist

This roadmap describes the planned development direction for OpenCad2D.

The project grows in small testable steps. Each phase should compile, pass tests and update documentation before the next feature begins.

Legend:

```text
[x] done
[~] partially implemented
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
- [x] DimensionStyle and basic dimension entities/tools;
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
- [x] automated DXF structure and compatibility tests;
- [x] hit testing;
- [x] selection;
- [x] entity snap and overlapping selection cycling;
- [x] snapping;
- [x] grid configuration;
- [x] drawing tools;
- [x] editing/transform tools;
- [x] two-column left tool panel organization;
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
- [x] file command bar integration through `Export DXF`;
- [x] automated structure tests for representative DXF entity records;
- [x] automated layer compatibility tests for colors, linetypes and lineweights;
- [x] automated check that DXF code/value pairs are balanced.

### PolylineTool v1

- [x] multi-point polyline creation;
- [x] Enter to finish open;
- [x] C to close;
- [x] command line, snap, Ortho and Polar Tracking support.

### Polyline grip editing

- [x] move generic polyline vertices;
- [x] insert vertices from segment midpoint grips;
- [x] delete vertices with `Delete` while a vertex grip is hot or warm;
- [x] protect open polylines from going below two vertices;
- [x] protect closed polylines from going below three vertices;
- [x] keep rectangle-like closed polylines on rectangle-specific resize grips;
- [x] undo/redo support through `ReplaceEntitiesCommand`;
- [x] unit tests for provider behavior and grip tool workflow.

---

## v0.3 - Points, text and DXF validation

### Feature & UX

- [x] `PointEntity`;
- [x] `PointTool`;
- [x] `TextEntity` for single-line text;
- [x] `TextTool`;
- [x] reusable `TextFormat` configuration;
- [x] Text Format Manager;
- [x] DimensionStyle and basic dimension entities/tools;
- [x] grip editing for polylines: move vertices;
- [x] grip editing for polylines: insert vertices;
- [x] grip editing for polylines: delete vertices.

### Stability & Test

- [ ] validate DXF export in LibreCAD;
- [ ] validate DXF export in QCAD;
- [ ] validate DXF export in Autodesk DWG TrueView;
- [x] create automated compatibility checks for colors;
- [x] create automated compatibility checks for linetypes;
- [x] create automated compatibility checks for lineweights;
- [x] add systematic tests for `PointEntity`;
- [x] add systematic tests for `TextEntity`;
- [x] add systematic tests for text formats;
- [x] add systematic tests for point/text persistence;
- [x] add systematic tests for point/text SVG export;
- [x] add systematic tests for point/text DXF export;
- [x] add more edge-case geometric tests for tangents;
- [x] add more edge-case geometric tests for overlaps;
- [x] add more edge-case geometric tests for near-tangents;
- [x] add more edge-case geometric tests for near-collinear and shared-vertex intersections.

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

Status: **feature-complete for the planned v0.4 scope**.

### Architectural decisions

- [x] dimensions are non-associative in v0.4;
- [x] DXF export writes dimensions as graphical primitives, not native `DIMENSION` records;
- [x] horizontal and vertical dimensions use separate tools;
- [x] angular dimensions support reflex angles greater than 180°;
- [x] dimension rendering, SVG export and DXF export share `DimensionGeometryBuilder`.

### Feature & UX

- [x] `DimensionStyleId`;
- [x] `DimensionStyle`;
- [x] `DimensionStyleCollection`;
- [x] document-level `DimensionStyles`;
- [x] `LinearDimensionEntity`;
- [x] `AlignedDimensionEntity`;
- [x] `RadiusDimensionEntity`;
- [x] `DiameterDimensionEntity`;
- [x] `AngularDimensionEntity`;
- [x] `HorizontalDimensionTool`;
- [x] `VerticalDimensionTool`;
- [x] `AlignedDimensionTool`;
- [x] `RadiusDimensionTool`;
- [x] `DiameterDimensionTool`;
- [x] `AngularDimensionTool`;
- [x] dimension text format/style integration through `DimensionStyle.TextFormatId`;
- [x] preview while placing horizontal, vertical, aligned, radius, diameter and angular dimensions;
- [x] Property Panel support for dimension entities;
- [x] two-column left tool panel cleanup for Select/Draw/Dimension/Measure and Edit groups;
- [x] status bar visual polish;
- [x] horizontal, vertical and aligned dimension text placement polish.

### Persistence and export

- [x] JSON persistence for `DimensionStyle`;
- [x] JSON persistence for linear and aligned dimensions;
- [x] JSON persistence for radius and diameter dimensions;
- [x] JSON persistence for angular dimensions;
- [x] SVG export for horizontal, vertical and aligned dimensions;
- [x] SVG export for radius and diameter dimensions;
- [x] SVG export for angular dimensions;
- [x] DXF export for horizontal, vertical and aligned dimensions as graphical primitives;
- [x] DXF export for radius and diameter dimensions as graphical primitives;
- [x] DXF export for angular dimensions as graphical primitives.

### Stability & Test

- [x] tests for horizontal linear dimension;
- [x] tests for vertical linear dimension;
- [x] tests for aligned dimension;
- [x] tests for angular dimension;
- [x] tests for radius dimension;
- [x] tests for diameter dimension;
- [x] persistence tests for dimension styles and dimension entities;
- [x] SVG export tests for dimensions;
- [x] DXF export tests for dimensions;
- [x] undo/redo tests for dimension tools;
- [x] systematic dimension edge-case tests;
- [x] transform robustness tests for dimensions;
- [x] Trim/Extend preview tests.

### Editing polish

- [x] Trim/Extend preview with highlighted portion for line targets;
- [x] clearer status messages for non-applicable Trim/Extend operations during preview;
- [x] documentation of current Trim/Extend preview scope and limitations.

### Completion criteria

- [x] all planned basic dimension types are implemented;
- [x] all planned basic dimension tools are implemented;
- [x] preview is available for all planned dimension tools;
- [x] dimensions are saved and loaded in `.opencad2d.json`;
- [x] dimensions are rendered on canvas;
- [x] dimensions appear in SVG export;
- [x] dimensions appear in DXF export as graphical primitives;
- [x] tests pass after the v0.4 implementation phases;
- [x] documentation has been updated for v0.4.

---

## v0.5 - Advanced editing and refinement

Status: completed as a stability-first modify-tools milestone.

This milestone consolidates advanced editing after the v0.4 Basic Dimensions milestone. It deliberately avoids command-line redesign and Property Panel editing, which remain planned for v0.6.

Detailed audit and implementation notes:

```text
docs/v0.5-modify-tools-audit.md
docs/release-v0.5.md
```

### Phase 0 - Modify tools audit

- [x] map current Break Point support;
- [x] map current Break Segment support;
- [x] map current Trim support;
- [x] map current Extend support;
- [x] document current Core editing services;
- [x] define v0.5 layer rules for hidden/locked entities;
- [x] define Break Point behavior for circles;
- [x] define Break Segment behavior for circles;
- [x] define two-cutting-edge Trim workflow;
- [x] define recommended v0.5 implementation phases.

### Phase 1 - Break Point advanced

- [x] `CadBreakService` supports line, arc and polyline break-at-point operations;
- [x] `BreakAtPointTool` accepts `LineEntity`, `ArcEntity` and `PolylineEntity`;
- [x] `Break Point` on `CircleEntity` returns a clear not-applicable message;
- [x] open polylines can be split into two open polylines;
- [x] closed polylines can be opened at the break point;
- [x] arc break preserves clockwise/counter-clockwise direction;
- [x] Break Point preview works with the new supported entity types;
- [x] undo/redo coverage for polyline break-at-point.

### Phase 2 - Break Segment advanced

- [x] `CadBreakService` supports line, arc, circle and polyline break-between-points operations;
- [x] `BreakBetweenPointsTool` accepts `LineEntity`, `ArcEntity`, `CircleEntity` and `PolylineEntity`;
- [x] `Break Segment` on `CircleEntity` removes the minor arc and keeps the remaining major arc;
- [x] open polylines can remove an interval and return zero, one or two open polylines;
- [x] closed polylines remove the shortest path between the two picked points and return one open polyline;
- [x] arc break segment preserves clockwise/counter-clockwise direction;
- [x] Break Segment preview works with the new supported entity types;
- [x] undo/redo coverage remains in place for break segment operations.

### Phase 3 - Trim with two cutting edges

- [x] `CadTrimService.TrimByBoundaries` supports one or more cutting edges for line targets;
- [x] single-boundary Trim behavior remains backward compatible;
- [x] second cutting edge can be selected with Ctrl-click after the first cutting edge;
- [x] line targets can remove the middle interval between two cutting edges;
- [x] line targets can remove an external interval outside the two cutting edges;
- [x] remaining adjacent line intervals are merged when the removed portion is external;
- [x] preview shows the remaining fragments for two-cutting-edge Trim;
- [x] highlighted preview shows the removed line segment;
- [x] undo/redo coverage for two-cutting-edge line Trim.

### Phase 4 - Extend consolidation

- [x] systematic Extend tests for `LineEntity` regression;
- [x] systematic Extend tests for `ArcEntity` start/end extension;
- [x] systematic Extend tests for open `PolylineEntity` endpoint extension;
- [x] `CircleEntity`, closed `PolylineEntity`, point/text/dimension targets remain not applicable;
- [x] clear unsupported-target message for non-extendable entities;
- [x] Extend preview highlights the added segment for line targets;
- [x] Extend preview highlights the added arc for arc targets;
- [x] Extend preview highlights the added segment for open polyline endpoint targets;
- [x] undo/redo coverage for open polyline Extend.

### Phase 5 - Layer rules and modify-tool regression

- [x] Break Point ignores hidden targets;
- [x] Break Point ignores locked targets;
- [x] Break Segment ignores hidden targets;
- [x] Break Segment ignores locked targets;
- [x] Trim can use locked visible cutting edges;
- [x] Trim ignores hidden cutting edges;
- [x] Trim does not modify locked targets;
- [x] Extend can use locked visible boundaries;
- [x] Extend ignores hidden boundaries;
- [x] Extend does not modify locked targets;
- [x] locked visible entities are references only, never editable targets;
- [x] hidden entities do not participate as references or targets.

### Feature & UX

- [x] Break Point on arcs;
- [x] Break Point on open and closed polylines;
- [x] Break Point on circles as a clear not-applicable operation;
- [x] Break Segment on arcs;
- [x] Break Segment on circles;
- [x] Break Segment on open and closed polylines;
- [x] Trim with two cutting edges for line targets;
- [x] improved Extend on supported entity types;
- [x] clearer status messages for unsupported modify operations;
- [x] highlighted preview for removed/added portions where currently supported.

### Stability & Test

- [x] systematic Trim tests;
- [x] systematic Extend tests;
- [x] systematic Break Point tests;
- [x] systematic Break Segment tests;
- [x] locked-layer behavior in modify tools;
- [x] hidden-layer behavior in modify tools;
- [x] undo/redo regression coverage for Break/Trim/Extend workflows covered by v0.5;
- [x] documented remaining geometric refinement scope for future versions.

### v0.5 decisions

- [x] `Break Point` on circles does not create an artificial gap; it returns a clear not-applicable message;
- [x] `Break Segment` on circles removes the minor arc between the two picked points;
- [x] locked visible entities are valid references/boundaries, but not editable targets;
- [x] hidden entities do not participate as references or targets;
- [x] two-cutting-edge Trim keeps single-boundary Trim compatible by using: cutting edge 1 -> Ctrl-click cutting edge 2 -> target portion;
- [x] multi-boundary Trim beyond line targets is deferred until the underlying geometry model is further stabilized.

### Completion criteria

- [x] all planned v0.5 implementation phases completed;
- [x] build and automated tests passed after each implementation phase;
- [x] documentation updated;
- [x] v0.6 scope is clearly separated from v0.5.

---

## v0.6 - Real command line and Property Panel v2

Status: in progress. Phase 0 audit/design and Phase 1 command activation completed.

Detailed planning document:

```text
docs/v0.6-command-line-property-panel-plan.md
```

### Architectural decisions

- [x] mouse input and command-line input should feed the same tool implementations;
- [x] command activation should use a dedicated alias registry/service instead of hardcoded UI logic;
- [x] command parsing should stay independent from Avalonia controls;
- [x] Property Panel v2 edits must be committed through undoable commands;
- [x] decimal point `.` is the only decimal separator for command-line numeric input;
- [x] comma `,` separates X/Y coordinates;
- [x] distance-angle syntax uses CAD model orientation: `0°` right, `90°` up.

### Phase 0 - Command line and Property Panel audit/design

- [x] define v0.6 scope;
- [x] define command-line architecture rules;
- [x] define coordinate syntax;
- [x] define decimal separator rule;
- [x] define initial alias table;
- [x] define repeat-last-command behavior;
- [x] define Property Panel v2 undo rule;
- [x] define implementation phases;
- [x] document out-of-scope items.

### Phase 1 - Command activation by alias

- [x] add `CommandAliasRegistry`;
- [x] connect command-line submission to the UI;
- [x] activate tools by command name;
- [x] activate tools by alias;
- [x] make matching case-insensitive;
- [x] unknown command produces clear feedback;
- [x] valid command activation is added to command history;
- [x] existing typed coordinate behavior remains intact;
- [x] `Esc` on an empty command line cancels the active tool;
- [x] tests for alias resolution;
- [x] tests for unknown commands;
- [x] tests for empty input;
- [x] tests for command history.

### Phase 2 - Absolute coordinate pipeline

- [ ] centralize `x,y` parsing;
- [ ] support whitespace around the comma;
- [ ] support decimal point values;
- [ ] reject invalid coordinate text clearly;
- [ ] submit parsed points to the active tool;
- [ ] verify Line/Polyline/Rectangle/Circle/Point workflows;
- [ ] tests for valid absolute coordinates;
- [ ] tests for invalid coordinates;
- [ ] tests for culture-invariant parsing.

### Phase 3 - Relative coordinates and direct distance

- [ ] parse `@x,y` relative coordinates;
- [ ] compute relative point from the current tool base point;
- [ ] preserve direct distance entry;
- [ ] use current pointer/constrained direction for direct distance;
- [ ] clear error when no base point exists;
- [ ] clear error when distance direction is unavailable;
- [ ] tests for relative input;
- [ ] tests for direct distance input;
- [ ] tests for invalid relative input.

### Phase 4 - Distance plus angle

- [ ] parse `distance<angle`;
- [ ] support negative angles;
- [ ] support angles over 360°;
- [ ] use CAD model orientation, not screen orientation;
- [ ] tests for `100<0`;
- [ ] tests for `100<90`;
- [ ] tests for `100<180`;
- [ ] tests for `100<-90`;
- [ ] tests for invalid polar input.

### Phase 5 - Repeat last command

- [ ] track last valid tool activation;
- [ ] do not treat coordinate input as the last command;
- [ ] `Enter` on an empty command line repeats the last command when appropriate;
- [ ] right-click repeats the last command when the workspace is idle;
- [ ] right-click does not interrupt active multi-step tools unexpectedly;
- [ ] tests for repeat by Enter;
- [ ] tests for repeat by right-click;
- [ ] tests for no repeat after invalid command.

### Phase 6 - Property Panel v2 base

- [ ] introduce editable property models;
- [ ] edit `PointEntity` position;
- [ ] edit `LineEntity` start/end;
- [ ] edit `CircleEntity` center/radius;
- [ ] edit `TextEntity` content, insertion point, rotation and text format;
- [ ] validate and parse numbers/coordinates;
- [ ] apply edits through command history;
- [ ] tests for undo/redo for each edited entity type;
- [ ] tests for invalid values.

### Phase 7 - Property Panel v2 complete

- [ ] edit `ArcEntity` properties;
- [ ] edit common `PolylineEntity` properties;
- [ ] keep detailed polyline vertex editing primarily in grips;
- [ ] edit common dimension properties;
- [ ] edit dimension style id;
- [ ] edit dimension text override where available;
- [ ] edit layer assignment;
- [ ] edit style/format references where appropriate;
- [ ] test undo/redo coverage;
- [ ] test locked-layer edit rejection.

### Phase 8 - Final polish and release documentation

- [ ] command-line UI polish;
- [ ] clearer prompts and status feedback;
- [ ] update `README.md`;
- [ ] update `docs/tools.md`;
- [ ] update `docs/commands.md`;
- [ ] update `docs/ai-handoff.md`;
- [ ] create `docs/release-v0.6.md`;
- [ ] mark v0.6 completed in this roadmap.

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
- [x] initial two-column left tool panel organization;
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
