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

## Pre-v1.0 focus

Before the first stable release, OpenCad2D should prioritize user trust and predictable workflows over new advanced geometry features:

- [~] persist application/session settings;
- [x] start with a clean drawing loaded from `Templates/default.opencad2d.json`;
- [x] open the main window maximized by default;
- [ ] implement draw order / Z-order independent from layers;
- [ ] validate DXF import/export in external CAD viewers;
- [ ] add end-to-end workflow tests for save/reopen and import/modify/export;
- [ ] keep current limitations visible in the README and user documentation.

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
- [x] editable Property Panel v2 for supported primary entity properties;
- [x] hidden layer behavior;
- [x] locked layer behavior;
- [x] spatial index abstraction;
- [x] viewport culling;
- [x] commands;
- [x] composite commands;
- [x] undo/redo;
- [x] persistence;
- [x] startup default template file;
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
- [x] application icon and logo assets;
- [x] modify tools for lines/arcs/circles/polylines where supported;
- [x] grip editing;
- [x] custom Avalonia canvas;
- [x] CAD-style crosshair;
- [x] basic command line with aliases and coordinate input for supported tools;
- [ ] CAD-style guided command input with prompt phases, options, visible history, relative and polar input for v0.8;
- [x] Ortho mode;
- [x] Polar Tracking with Off/90°/45°/30°/15°.

---

## Current stabilization phase

### Startup and default template

- [x] normal startup no longer seeds the sample drawing;
- [x] the main window opens maximized by default;
- [x] startup defaults are loaded from `src/OpenCad2D.App/Templates/default.opencad2d.json`;
- [x] the template contains the default line formats, text formats, dimension style and layers;
- [x] if the template is missing or invalid, the app falls back to a safe internal empty document with the built-in CAD layers.

Pending in this phase:

- [x] fix arc 3-point grip behavior so moving one construction grip keeps the other two construction points fixed;
- [x] update About with `info@opencad2d.org` and `www.opencad2d.org`;
- [x] center all secondary modal windows on their owner;
- [x] improve the save changes dialog styling;
- [x] add Select All and Select Last;
- [x] update Select Last to restore the last cleared selection instead of the newest created entity;
- [x] add Zoom Window;
- [x] expose Zoom Extents in the left Navigate tool panel;
- [ ] add document recovery options for partially invalid native files.

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

### Property Panel v1/v2

- [x] right-side panel;
- [x] no-selection state;
- [x] single entity details;
- [x] multiple selection summary;
- [x] Point details;
- [x] Text details;
- [x] editable Property Panel v2 for supported entity properties;
- [x] undoable Property Panel edits through document commands;
- [ ] numeric polyline vertex table editing.

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
- [ ] document tested external viewer versions and results.
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
- [ ] DXF compatibility validated externally with documented viewer versions.

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

Status: completed.

Release notes:

```text
docs/release-v0.6.md
```

Detailed planning and implementation notes:

```text
docs/v0.6-command-line-property-panel-plan.md
```

### Architectural decisions

- [x] mouse input and command-line input feed the same tool implementations;
- [x] command activation uses `CommandAliasRegistry` instead of hardcoded UI-only logic;
- [x] command parsing stays independent from Avalonia controls;
- [x] Property Panel v2 edits are committed through undoable commands;
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

- [x] keep `x,y` parsing centralized in `CommandInputParser`;
- [x] support whitespace around the comma;
- [x] support decimal point values;
- [x] reject invalid coordinate text clearly;
- [x] submit parsed points to the active tool;
- [x] command-line points bypass snap/ortho/polar so typed coordinates remain exact;
- [x] verify Line/Circle/Point workflows with command-line coordinates;
- [x] tests for valid absolute coordinates;
- [x] tests for invalid coordinates;
- [x] tests for command history not storing coordinate point input;
- [x] tests for culture-invariant decimal point parsing.

### Phase 3 - Relative coordinates and direct distance

- [x] parse `@x,y` relative coordinates;
- [x] compute relative point from the current tool base point;
- [x] preserve direct distance entry;
- [x] use current pointer/constrained direction for direct distance;
- [x] clear error when no base point exists;
- [x] clear error when distance direction is unavailable;
- [x] tests for relative input;
- [x] tests for direct distance input;
- [x] tests for invalid relative input.

### Phase 4 - Distance plus angle

- [x] parse `distance<angle`;
- [x] support spaces around `<`;
- [x] support decimal distance and angle values;
- [x] support negative angles;
- [x] support angles over 360°;
- [x] use CAD model orientation, not screen orientation;
- [x] tests for `100<0`;
- [x] tests for `100<90`;
- [x] tests for `100<180`;
- [x] tests for `100<-90`;
- [x] tests for invalid polar input.

### Phase 5 - Repeat last command

- [x] track last valid tool activation;
- [x] do not treat coordinate input as the last command;
- [x] `Enter` on an empty command line repeats the last command when appropriate;
- [x] right-click repeats the last command from the canvas when the workspace is idle;
- [x] right-click does not interrupt active point-based commands;
- [x] invalid commands do not become repeatable commands;
- [x] tests for repeat by empty command-line submission;
- [x] tests for repeat after coordinate input;
- [x] tests for no repeat after invalid/no command.

### Phase 6 - Property Panel v2 base

- [x] introduce editable property row models;
- [x] edit `PointEntity` position;
- [x] edit `LineEntity` start/end;
- [x] edit `CircleEntity` center/radius;
- [x] edit `TextEntity` content, insertion point and rotation;
- [x] validate and parse numeric input with invariant decimal point;
- [x] apply edits through command history using `ReplaceEntitiesCommand`;
- [x] refresh the panel after a successful edit;
- [x] tests for undo/redo for the first edited entity types;
- [x] tests for invalid values.

### Phase 7 - Property Panel v2 complete

- [x] edit `TextEntity` text format from the panel;
- [x] edit `ArcEntity` properties;
- [x] edit common `PolylineEntity` properties;
- [x] keep detailed polyline vertex editing primarily in grips;
- [x] edit common dimension properties;
- [x] edit dimension style id;
- [x] edit dimension text override where available;
- [x] edit layer assignment;
- [x] edit style/format references where appropriate;
- [x] test undo/redo coverage;
- [x] test locked-layer edit rejection.

### Phase 8 - Final polish and release documentation

- [x] command-line behavior documented;
- [x] coordinate syntax documented;
- [x] repeat-last-command documented;
- [x] Property Panel v2 behavior documented;
- [x] update `README.md`;
- [x] update `docs/tools.md`;
- [x] update `docs/commands.md`;
- [x] update `docs/ai-handoff.md`;
- [x] create `docs/release-v0.6.md`;
- [x] mark v0.6 completed in this roadmap.

---

## v0.7 - Interoperability: DXF import and PDF export

Detailed implementation plan: [`v0.7-interoperability-plan.md`](v0.7-interoperability-plan.md).

### Phase progress

- [x] Phase 0 started: scope and architecture documented;
- [x] Phase 1 started: DXF reader infrastructure added;
- [x] Phase 2: DXF base entity import;
- [x] Phase 3: DXF layers and formats;
- [x] Phase 4: DXF import UI;
- [x] Phase 5: DXF round-trip validation;
- [x] Phase 6: PDF export core;
- [x] Phase 7: PDF export UI;
- [x] Phase 8: SVG export options;
- [x] Phase 9: documentation and release notes.

### Feature & UX

- [x] import DXF entities;
- [x] import DXF layers;
- [x] import DXF linetypes;
- [x] import DXF colors;
- [x] import DXF lineweights;
- [x] import DXF text where supported;
- [x] skip unsupported DXF entities with readable log;
- [x] export PDF;
- [x] PDF page format;
- [x] PDF fit-to-page scale;
- [x] PDF margins;
- [x] SVG layer grouping;
- [x] optional transparent SVG background.

### Stability & Test

- [x] DXF import error handling;
- [x] DXF export -> import round-trip tests;
- [x] PDF export tests;
- [x] unsupported DXF entity tests.

---

## v0.8 - CAD-style command input and guided tool workflow

### Main goal

The v0.8 milestone introduces a guided command input system. The command line should no longer be only a command launcher: it should show the active command, the current command phase, the expected input and the available options.

Core decisions for v0.8:

- [ ] mouse clicks and typed command input must feed the same tool state machine;
- [ ] whenever a command asks for a point, the user can either click on the canvas or type a coordinate;
- [ ] `LINE` remains a single-segment command: first point, second point, then command ends;
- [ ] absolute coordinate input is supported, for example `100,100`;
- [ ] relative cartesian input is supported, for example `@100,0`;
- [ ] relative polar input is supported, for example `@100<45`;
- [ ] empty Enter while idle repeats the last valid command;
- [ ] empty Enter inside an active command confirms the current phase only when that phase allows it;
- [ ] a compact visible command history is added near the command input;
- [ ] Trim is planned as an advanced base workflow, not only a minimal trim command.

Detailed design document:

```text
docs/command-input.md
```

### Block 1 - Specification and parser infrastructure

- [ ] add `CommandPromptState`;
- [ ] add `CommandOption`;
- [ ] add `CommandInputKind`;
- [ ] add `CommandInputSubmission`;
- [ ] add `CommandInputSubmissionKind`;
- [ ] add centralized parser for command text input;
- [ ] parse absolute points: `x,y`;
- [ ] parse relative cartesian points: `@dx,dy`;
- [ ] parse relative polar points: `@distance<angle`;
- [ ] parse distances/numbers when the prompt expects them;
- [ ] parse options by keyword or shortcut;
- [ ] parse empty input as Confirm/Repeat depending on active state;
- [ ] add parser tests before changing tool behavior.

### Block 2 - ViewModel and UI integration

- [ ] add current command prompt text to the view-model;
- [ ] add a compact visible command history;
- [ ] keep existing command aliases working;
- [ ] keep existing action commands working: Select All, Select Last, Zoom Window, Zoom Extents;
- [ ] route active command input to command-driven tools;
- [ ] implement idle Enter repeat-last-command;
- [ ] ensure invalid commands do not become repeatable commands;
- [ ] keep Escape behavior: first Esc cancels active tool, second Esc clears selection.

### Block 3 - Convert `LINE`

- [ ] show `LINE: Specify first point:`;
- [ ] accept first point from mouse or typed coordinate;
- [ ] show `LINE: Specify second point:`;
- [ ] accept second point from mouse, absolute input, relative input or polar input;
- [ ] create a single line segment;
- [ ] finish the command after the second point;
- [ ] add tests for absolute, relative and polar input.

### Block 4 - Convert `POLYLINE`

- [ ] show `POLYLINE: Specify start point:`;
- [ ] show `POLYLINE: Specify next point or [Close/Undo]:`;
- [ ] support absolute, relative and polar point input;
- [ ] support `Close` / `C`;
- [ ] support `Undo` / `U`;
- [ ] use empty Enter to finish an open polyline;
- [ ] add tests for options and mixed mouse/text input.

### Block 5 - Convert base drawing tools

- [ ] Rectangle: first corner, opposite corner;
- [ ] Circle: center point, radius;
- [ ] Arc 3P: start point, point on arc, end point;
- [ ] add tests for typed point/distance input.

### Block 6 - Convert Move, Copy and Break

- [ ] Move: select objects, base point, destination point;
- [ ] Copy: select objects, base point, destination point;
- [ ] Break: select entity, first break point, second break point;
- [ ] support relative and polar destination input for Move/Copy;
- [ ] support mixed mouse/entity selection and typed point input for Break.

### Block 7 - Trim advanced base

- [ ] add `TRIM: Select cutting edges or [All]:`;
- [ ] support multiple cutting-edge selection;
- [ ] support `All` / `A`;
- [ ] empty Enter confirms cutting-edge selection;
- [ ] add `TRIM: Select object to trim or [Undo]:`;
- [ ] keep Trim active after each trim operation;
- [ ] support `Undo` / `U` inside the Trim session;
- [ ] introduce picked-entity input with entity id and pick point;
- [ ] defer Fence/Crossing/Edge/Project/Erase/Shift-Extend to later milestones.

### Block 8 - Documentation and release stabilization

- [ ] update `docs/commands.md`;
- [ ] update `docs/tools.md`;
- [ ] update `docs/ai-handoff.md`;
- [ ] update README user-facing command input section if needed;
- [ ] add release notes for v0.8;
- [ ] ensure full solution build and tests pass.

### Secondary v0.8 backlog

These remain useful but are lower priority than the command input refactor:

- [ ] color picker improvements for layer and formats;
- [ ] application settings;
- [ ] shortcuts persistence;
- [ ] last file persistence;
- [ ] default grid settings;
- [ ] favicon;
- [ ] final XAML theme;
- [ ] draw order / Z-order independent from layers before v1.0;
- [ ] snap icons: active / detected / disabled states;
- [ ] dark theme regression tests;
- [ ] settings persistence tests;
- [ ] draw order / Z-order tests.

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

- [x] text and basic dimensions operational;
- [x] PDF export working;
- [x] basic command line with aliases and coordinate input;
- [ ] guided CAD-style command input completed for v0.8;
- [~] editable Property Panel for supported primary entity properties;
- [~] DXF import/export covered by automated structural and round-trip tests;
- [ ] DXF import/export externally validated in LibreCAD, QCAD and Autodesk DWG TrueView;
- [ ] application/session settings persistence;
- [ ] draw order / Z-order independent from layers;
- [ ] end-to-end workflow: draw -> save -> reopen;
- [ ] end-to-end workflow: import DXF -> modify -> export;
- [ ] stable undo/redo on all primary tools;
- [ ] reliable `.opencad2d.json` persistence under end-to-end workflow tests;
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
- [ ] layer appearance v2: fill and advanced appearance rules;
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


## v0.7 completion update

Completed in the v0.7 development cycle:

- DXF import for base entities;
- DXF layer table import;
- DXF import diagnostics and report window;
- DXF import UI;
- DXF round-trip validation;
- PDF export core;
- PDF export UI basic command;
- PDF export settings dialog;
- SVG export options for layer grouping and transparent, white or canvas-dark background;
- final v0.7 documentation and release notes.

Release notes:

```text
docs/release-v0.7.md
```

### Completed stabilization: arc endpoint grips

Implemented:

- Arc start grip updates only the start angle.
- Arc end grip updates only the end angle.
- Endpoint grip moves preserve center, radius and the opposite endpoint.
- Tests cover start and end endpoint-grip behavior.


### Completed: arc 3-point grip behavior

Arc grip editing now uses 3-point reconstruction for start, point-on-arc and end grips. Moving one construction grip keeps the other two construction points fixed and recalculates the arc center/radius/sweep.


### Document loading robustness

Implemented first recovery layer:

- tolerant recovery for valid JSON documents with partially invalid entity data;
- invalid entities are skipped while valid entities are preserved;
- missing entity layers and current layer references are repaired to `Layer 0`;
- recovery counts and issues are reported internally and surfaced in the status message.

Remaining future work:

- dedicated recovery dialog with issue list;
- optional backup copy before repair/export;
- broader automatic repair rules for style and format references.

### v0.8 implementation note - Command input block 1

Started the CAD-style command input refactor by adding the neutral parser/model infrastructure before changing tool behavior. The project now has prompt states, command options, contextual submissions and a parser capable of absolute, relative and polar point input. Existing tools still use the current command path; conversion starts in the next blocks.


### v0.8 command input block 2

Completed UI plumbing for the CAD-style command input refactor:

- visible compact command history;
- contextual command prompt remains visible above the input box;
- contextual placeholder examples for absolute, relative and polar coordinates;
- empty Enter can repeat the last command from the command line/canvas flow;
- existing command alias history remains separate from the visible UI history.

### v0.8 command input progress - block 3

Completed first command-driven tool migration:

- [x] `LINE` exposes `CommandPromptState`.
- [x] `LINE` accepts absolute coordinate input.
- [x] `LINE` accepts relative coordinate input.
- [x] `LINE` accepts relative polar coordinate input.
- [x] `LINE` keeps mouse input working through the existing two-point workflow.
- [x] Legacy command input remains available for tools not yet migrated.

Next:

- [ ] Migrate `POLYLINE` with `Close` and `Undo` options.


### v0.8 progress - Command input refactor

Completed in the current v0.8 path:

- [x] Command input specification and parser infrastructure.
- [x] Visible command history and contextual prompt text.
- [x] `LINE` as a command-driven tool with absolute, relative, polar and direct-distance input.
- [x] `POLYLINE` as a command-driven tool with `Close`, `Undo` and empty-Enter completion.

Next planned steps:

- [x] Convert `Rectangle`, `Circle` and `Arc 3P` to the command-driven model.
- [ ] Convert `Move`, `Copy` and `Break`.
- [ ] Design and implement the advanced Trim workflow with picked-entity input.


### v0.8 command input progress - block 5

Completed command-driven migration for the remaining basic drawing tools:

- [x] `Rectangle` supports guided first-corner and opposite-corner prompts.
- [x] `Circle` supports guided center and radius-point/radius prompts.
- [x] `Arc 3P` supports guided start, point-on-arc and end-point prompts.
- [x] Mouse input and typed coordinate input now share the same tool phases for these tools.

Next:

- [ ] Convert `Move`, `Copy` and `Break` to guided command input.
