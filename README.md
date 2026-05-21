# OpenCad2D

**OpenCad2D** is an experimental open-source 2D CAD application built with **C#**, **.NET 8** and **Avalonia UI**.

The project explores how to build a small but serious 2D CAD system from the ground up, with a clean separation between geometry, document modeling, interaction logic, tools, persistence, export and the graphical user interface.

OpenCad2D is not intended to replace mature CAD applications yet. The current goal is to create a precise, fast, testable and understandable 2D CAD foundation.

## A long-running personal project

OpenCad2D is not a disposable AI-generated experiment. Its roots go back more than twenty years, to when I first started writing C# and imagining how a personal 2D CAD system could be built from the ground up.

For a long time, the project existed as separate pieces: geometry experiments, drawing ideas, editing tools, file-format tests, interface prototypes and many small lessons accumulated over years of programming. What changed recently is that AI-assisted development finally made it possible to bring those fragments together, review them, refactor them and turn them into a coherent, tested and working application.

AI is part of the process, but it is not the author of the vision. OpenCad2D remains a human-led project: shaped by long-term interest in CAD, by practical software design choices, by tests, by manual validation and by a clear preference for a precise, understandable and open 2D drafting tool.

![OpenCad2D screenshot](screenshot/screenshot_UI_1.png)

---

## Current status

OpenCad2D currently supports a complete early CAD workflow:

- clean startup from `Templates/default.opencad2d.json`;
- maximized main window on startup;
- native save/load using `.opencad2d.json`;
- document-level settings persistence for grid, snap, ortho, polar tracking and current drawing settings;
- local application settings for last opened file metadata, recent files and last open/save/export folders;
- document recovery for partially invalid native files;
- New, Open, Save and Save As;
- SVG, DXF and PDF export, including solid fill for closed circles/polylines, DXF HATCH output, DXF SPLINE knot-vector output and MTEXT reference width;
- ASCII DXF import for core 2D entities, layer tables, MTEXT, LWPOLYLINE bulge arcs, ELLIPSE and readable SPLINE entities;
- layers with visibility and locking;
- reusable line formats with color, lineweight, line style and custom dash pattern values;
- reusable text formats;
- Layer Manager, Line Format Manager and Text Format Manager;
- compact ColorPicker support in line/text format managers;
- editable Property Panel for supported entities, including MTEXT value/reference-width editing and read-only draw order display;
- independent draw order / Z-order, separate from layers;
- CAD-style command input with contextual prompts, command aliases, coordinates, relative coordinates, polar input, direct distances, command history navigation and first-pass autocomplete;
- object snapping, grid snapping, Ortho mode and Polar Tracking;
- selection, Select All, Select Last and Deselect;
- drawing tools for points, single-line text, multiline text, lines, rectangles, circles, ellipses, arcs, polylines, polygons and Bezier splines;
- dimension tools for horizontal, vertical, aligned, radius, diameter and angular dimensions, with stale-state marking after geometry modifications;
- transform tools: move, copy, rotate, scale and point-based align;
- modify tools: delete, break point, break segment, trim, extend, offset, fillet, explode and join;
- trim and break support based on native curve parameters for lines, arcs, circles where applicable, ellipses, elliptical arcs, polylines, polygons and open Bezier splines;
- offset for lines, circles, arcs and straight-segment polylines;
- line-line fillet with radius option, live preview, Trim/NoTrim modes and radius `0` sharp-corner join in Trim mode;
- layer-based solid fill for closed circles and closed polylines, with `Fill: None/Solid` in the Property Panel and a layer Fill color picker;
- align object tools: left, right, top and bottom;
- distribute object tools: horizontal and vertical distribution by centers;
- measure tools for distance, entity properties, angles and closed-polyline areas;
- Zoom Window, Zoom Extents, pan and reset view;
- CAD-style crosshair cursor;
- undo/redo for document mutations;
- tool-provided preview descriptor/entity protocols that keep active tool preview logic out of the app renderer;
- minimal application logging for tool/UI exceptions.

See `docs/roadmap.md` and `docs/stabilization-v0.9-plan.md` for the active v0.9 stabilization plan.

---

## User interface

The UI is organized into stable zones:

```text
File command bar     New / Open / Save / Save As / export buttons / file name / dirty marker
Top CAD bar          layer selector, layer state, managers, grid, polar tracking, undo/redo, view commands
Left tool panel      Select, Draw, Dimension, Measure, Edit, Order, Align/Distribute and Navigate groups
Center canvas        drawing area with CAD crosshair and previews
Right panel          editable Property Panel
Bottom snap bar      snap toggles and drafting toggles
Command row          active tool, contextual prompt and command input
Status bar           coordinates, snap state, measurements, rendered count and messages
```

The command row is intentionally compact. The active tool and current prompt are shown next to the command input so the user knows which phase is active without losing drawing space.

---

## Command input

OpenCad2D includes a CAD-style guided command input.

Examples:

```text
L
100,100
@100,0
```

```text
PL
0,0
@100,0
@100<90
C
```

Supported input forms:

| Input | Meaning |
|---|---|
| `100,50` | absolute point |
| `@50,0` | relative cartesian point |
| `@100<45` | relative polar point |
| `25` | distance, angle or factor when the active command expects it |
| `C`, `Close`, `U`, `Undo`, `All`, `Radius` | command options when exposed by the active prompt |
| empty Enter while idle | repeat the last valid command |
| empty Enter/right-click inside a command | confirm only if the current phase accepts confirmation |
| `↑` / `↓` | navigate command history while the command input is focused |
| `Tab` | accept the first autocomplete suggestion for known commands/aliases |

Mouse input and typed input feed the same tool state machine. When a tool asks for a point, the user can either click on the canvas or type coordinates.

---

## Native file format

OpenCad2D saves native drawings as:

```text
.opencad2d.json
```

The native file stores:

- entities, including fill state for supported closed entities;
- layers, including layer fill color;
- line formats;
- text formats;
- dimension styles;
- viewport state;
- document-level settings such as grid, snap, ortho and polar tracking.

The native format is for OpenCad2D save/reopen reliability. Use DXF/SVG/PDF for interchange/export.

---

## Build and test

```bash
dotnet build OpenCad2D.sln
dotnet test OpenCad2D.sln --no-build
```

Or use the repository workflow:

```bash
make check
```

---

## Documentation

Important documents:

| Document | Purpose |
|---|---|
| `docs/architecture.md` | project structure and dependency rules |
| `docs/roadmap.md` | current roadmap, completed stabilization work and next milestones |
| `docs/commands.md` | commands, aliases and undoable command rules |
| `docs/curve-editing.md` | Trim/Break curve-editing architecture, native precision and shared cut-point rules |
| `docs/command-input.md` | command input syntax and tool workflow |
| `docs/tools.md` | tool behavior, workflow rules and preview-provider conventions |
| `docs/line-formats.md` | line format and line style pattern rules |
| `docs/application-settings.md` | document settings and local settings separation |
| `docs/draw-order.md` | Z-order behavior |
| `docs/persistence.md` | native file format and recovery rules |
| `docs/stabilization-v0.9-plan.md` | v0.9 release-candidate stabilization plan |
| `docs/ai-handoff.md` | current handoff for future development |

Historical milestone details are intentionally kept out of the active roadmap; use Git history and release notes for old implementation logs.

---

## License

OpenCad2D is released under the GPL-3.0-or-later license. See `LICENSE`.

---

## Credits

Created with love by Emilie Rollandin.

- Dimension Styles manager for configurable dimension text, units, classic terminator symbols, offsets, live preview and left-readable vertical dimension text.


Dimension styles include built-in Standard, Architectural and Mechanical presets with configurable text, symbols, units and orientation.


Dimension text fit rules keep labels readable on short dimensions by moving text outside the measured span when needed.


Dimension terminator fit rules can keep arrows/ticks inside or move them outside when a dimension is too short.


## Current limitations and planned improvements

OpenCad2D is intentionally transparent about what is already solid and what still needs refinement.

- **Polyline numeric editing**: polylines can be edited visually through canvas grips, but the Property Panel does not yet provide a tabular editor for individual vertices. A future vertex table will allow precise X/Y correction for each point.
- **Non-associative dimensions**: dimensions store their own measured points and dimension-line geometry. When the measured entity is later moved, scaled or edited, the dimension value does not update automatically. OpenCad2D can mark dimensions as potentially stale after geometry changes, but users must update or recreate them manually.
- **Local user settings**: basic local settings exist, but the next step is a more complete startup configuration flow for default grid/snap preferences, recent paths and last-file behavior.


## Current stabilization checkpoint

OpenCad2D is currently in the v0.9 stabilization cycle. Recent completed work includes:

- Dimension Styles with built-in `Standard`, `Architectural` and `Mechanical` presets;
- a Dimension Style Manager with live preview based on the same `DimensionGeometryBuilder` used by real dimensions;
- property-panel dimension style selection through a combo box;
- configurable dimension text and terminator fit rules for short dimensions;
- classic dimension terminators including arrows, triangles, dots, architectural ticks and slash symbols;
- left-readable vertical dimension text according to the current OpenCad2D convention;
- bugfixes for Break Point on full ellipses, TRIM open-polyline endpoint filtering and Offset polyline side detection;
- cleanup of obsolete TRIM highlight helper code and clearer Fillet branch-parameter documentation;
- layer-based solid fill for closed circles and closed polylines, including Property Panel control, Layer Manager fill color picker and SVG/PDF/DXF export.

The next planning step is v0.9 scope consolidation: decide which remaining polish items are required before the next public release and which are deferred to v1.0.
