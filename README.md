# OpenCad2D

![OpenCad2D logo](screenshot/logo_opencad2d.svg)

**OpenCad2D** is an experimental open-source 2D CAD application built with **C#**, **.NET 8** and **Avalonia UI**.

The project explores how to build a small but serious 2D CAD system from the ground up, with a clean separation between geometry, document modeling, interaction logic, tools, persistence and the graphical user interface.

The long-term goal is not only to create a usable CAD application, but also to keep the codebase understandable, testable and extensible.

![OpenCad2D screenshot](screenshot/screenshot_UI_1.png)

---

## Project status

OpenCad2D is currently an early prototype. It is **not** intended to replace mature CAD software yet.

The current focus is to build strong foundations: geometry, entities, layers, commands, undo/redo, snapping, selection, tools, coordinate systems, spatial queries, CAD-style numeric input, persistence and a first cross-platform UI.

The application already supports a functional CAD workflow:

- drawing points;
- drawing single-line text annotations;
- drawing lines;
- drawing rectangles by opposite corners;
- drawing rectangles by first side and second side (`Rect Sides`);
- drawing circles;
- drawing arcs by center/start/end;
- drawing arcs through three points (`Arc 3P`);
- drawing open and closed polylines;
- creating horizontal, vertical, aligned, radius, diameter and angular dimensions;
- selecting entities by point, window and crossing selection;
- moving, copying, rotating, scaling, aligning and deleting selected entities;
- break point for lines, arcs and polylines, with clear non-applicable feedback for circles;
- break segment for lines, arcs, circles and polylines;
- trim with one cutting edge and two-cutting-edge Trim for line targets;
- extend for lines, arcs and open polylines where supported by the current geometry services;
- grip editing for supported entities;
- undo and redo;
- internal JSON save/load using `.opencad2d.json`;
- New, Open, Save and Save As file commands;
- SVG export using `.svg`, including points, single-line text and basic dimensions;
- DXF export using AutoCAD 2000 ASCII `.dxf`, including `POINT`, `TEXT` and basic dimensions as graphical primitives;
- dirty-state tracking and “Save changes?” confirmation;
- object snapping, including geometric snaps and entity snap for selection-oriented tools;
- configurable rectangular and isometric grid display and grid snapping;
- Grid Settings dialog for visibility, spacing, origin, screen spacing thresholds and isometric angle;
- layer visibility and locked layer behavior;
- assigning selected entities to the current layer from the top CAD bar;
- reusable line formats for layer stroke color, weight and style;
- reusable text formats for single-line annotation appearance;
- Line Format Manager;
- Text Format Manager;
- Layer Manager with line format selection;
- editable Property Panel v2 with undoable edits for supported entity properties;
- real command line with tool aliases, command history, absolute/relative coordinates, direct distance and distance-angle input;
- direct distance entry;
- non-mutating measure tools for distance, entity properties, angles and closed-polyline areas;
- Ortho mode and Polar Tracking with selectable angle steps (`Off`, `90°`, `45°`, `30°`, `15°`);
- zoom, pan, view reset and Zoom Extents;
- viewport culling for rendering only visible entities;
- CAD-style crosshair cursor;
- visual feedback for active command, current layer, snap type, rendered entity count and temporary measurements;
- application icon and in-app logo assets based on `screenshot/logo_opencad2d.*`;
- two-column left tool panel grouping Select/Draw/Dimension/Measure separately from Edit tools;
- v0.5 advanced modify-tool behavior for Break, Trim and Extend with locked/hidden-layer regression coverage.

---

## User interface

The desktop application is built with **Avalonia UI**.

The current UI layout is intentionally divided into stable areas:

```text
File command bar     New / Open / Save / Save As / current file name / dirty marker
Top CAD bar          layer selector, layer state, Layers..., Line formats..., Text formats..., Grid..., Polar selector, Undo/Redo, Extents, Props, active command
Left tool panel      two columns: Select / Draw / Dimension / Measure and Edit
Center canvas        drawing area
Right panel          optional editable Property Panel v2
Bottom snap bar      object snapping and legacy Ortho controls
Command line         typed point, distance and command input
Status bar           coordinates, snap state, measurements, rendered count and messages
```

The file command bar is deliberately kept in the highest, separate row so it is not accidentally removed when toolbars or CAD controls evolve.

The standard mouse cursor is hidden over the canvas and replaced by a large crosshair. A small rectangle around the intersection identifies the exact picking point. Entity snapping uses a simple rectangular marker so selection-oriented snapping remains visually distinct from geometric point snapping.

While tools are active, the UI can show temporary construction feedback:

- accepted base point;
- vector line from base point to cursor or snap point;
- preview geometry;
- `L`, `DX` and `DY` measurement values;
- transform previews;
- grip markers and grip edit previews.

---

## File commands and persistence

OpenCad2D saves drawings in an internal JSON format using the extension:

```text
.opencad2d.json
```

The format is intended for reliable save/reopen inside OpenCad2D. It is not a DXF or DWG interoperability format.

The UI supports:

| Command | Shortcut |
|---|---|
| New | `Ctrl+N` |
| Open | `Ctrl+O` |
| Save | `Ctrl+S` |
| Save As | `Ctrl+Shift+S` |
| Export SVG | button in file bar |
| Export DXF | button in file bar |

The application tracks dirty state using command history generation. When the drawing has unsaved changes, the title/file bar shows `*`.

Before New, Open or window close, if the drawing is dirty, the application asks whether to save changes:

```text
Save       -> save, then continue
Don't Save -> discard changes, then continue
Cancel     -> abort the operation
```

Viewport state is saved and restored with the drawing. This includes pan and zoom.

---


## Command line

OpenCad2D includes a CAD-style command line for tool activation and precise point input.

Supported command-line behavior:

- tool activation by command name or alias, for example `LINE`, `L`, `CIRCLE`, `C`, `TRIM`, `TR`, `HDIM` and `ANG`;
- case-insensitive alias resolution through `CommandAliasRegistry`;
- absolute point input, for example `100,50`;
- relative point input from the current tool base point, for example `@100,0`;
- direct distance input in the current cursor/constrained direction, for example `50`;
- distance-angle input in CAD model coordinates, for example `100<45`;
- command history for valid tool activations;
- `Enter` on an empty command line repeats the last valid command;
- right-click on the canvas repeats the last valid command when the workspace is idle;
- `Esc` cancels the active tool when the command input is empty.

The decimal separator is always `.`. The comma is reserved for separating X/Y coordinates.

Distance-angle input uses CAD orientation:

```text
0°   = right
90°  = up
180° = left
270° = down
```

Typed coordinate input is submitted to the same tools used by mouse input. It is also protected from accidental snap/ortho/polar alteration so exact numeric values remain exact.

---

## Property Panel v2

The right Property Panel is now editable for the supported properties of selected entities.

Supported edit groups include:

- point position;
- line start/end coordinates;
- circle center/radius;
- arc center/radius/start/end angles;
- text value, insertion point, rotation and text format;
- common polyline state such as `Closed`;
- layer assignment;
- dimension style and dimension text override.

Detailed polyline vertex editing remains intentionally handled by grip editing instead of a vertex table in the Property Panel.

All Property Panel edits are committed through undoable commands, primarily `ReplaceEntitiesCommand`, so undo/redo, dirty-state tracking and spatial-index consistency remain intact.

---

## SVG export

OpenCad2D can export the current visible drawing to SVG.

Current SVG export behavior:

- exports visible `PointEntity`, `TextEntity`, geometric entities and basic dimensions;
- ignores entities on hidden layers;
- includes entities on locked layers when their layer is visible;
- uses stroke color, line weight and dash style from the line format assigned to the entity layer;
- uses text format font, height, color, bold and italic for single-line text;
- computes an automatic `viewBox` from visible drawing bounds;
- exports a dark background rectangle matching the OpenCad2D canvas;
- preserves the same visual Y orientation as the canvas;
- writes standard `.svg` files that can be opened in a browser or vector editor.

Exporting SVG is not the same as saving the drawing:

```text
Export SVG does not change CurrentFilePath.
Export SVG does not call MarkSaved().
Export SVG does not clear the dirty marker.
```

The SVG exporter lives in `OpenCad2D.Export` and does not depend on Avalonia, App or Tools.



## DXF export

OpenCad2D can export the visible model-space drawing to AutoCAD 2000 ASCII DXF (`AC1015`).

Current DXF export behavior:

- writes `HEADER`, `TABLES`, `LTYPE`, `LAYER`, `ENTITIES` and `EOF` sections;
- exports `PointEntity` as `POINT`;
- exports `TextEntity` as `TEXT`;
- exports `LineEntity` as `LINE`;
- exports `CircleEntity` as `CIRCLE`;
- exports `ArcEntity` as `ARC`;
- exports `PolylineEntity` as `LWPOLYLINE`;
- exports dimensions as graphical primitives (`LINE`, `ARC` and `TEXT`), not native DXF `DIMENSION` records;
- writes all document layers to the DXF `LAYER` table;
- writes `CONTINUOUS`, `DASHED`, `DASHDOT` and `DASHDOTDOT` to the DXF `LTYPE` table;
- derives layer color, true color, lineweight and linetype from the layer's `LineFormat`;
- writes entity appearance as `BYLAYER`;
- ignores entities on hidden layers by default;
- exports visible locked-layer entities;
- mirrors Y using the exported content bounds so the exported drawing keeps the same visual top/bottom orientation seen in OpenCad2D viewers.

Exporting DXF does not change the current `.opencad2d.json` file path and does not clear dirty state.

---

## Drawing tools

Implemented drawing tools:

- `PointTool` — one picked position;
- `TextTool` — insertion point plus single-line text dialog;
- `LineTool` — two points;
- `RectangleTool` — opposite corners, stored as closed polyline;
- `RectangleBySidesTool` / `Rect Sides` — first point, first side, second side;
- `CircleTool` — center and radius point or direct distance radius;
- `ArcTool` — center, start point, end direction;
- `ArcThreePointsTool` / `Arc 3P` — start point, point on arc, end point;
- `PolylineTool` — multi-point open or closed polyline.

Implemented dimension tools:

- `HorizontalDimensionTool` — horizontal distance between two measured points;
- `VerticalDimensionTool` — vertical distance between two measured points;
- `AlignedDimensionTool` — true distance aligned to the two measured points;
- `RadiusDimensionTool` — radius label using `R`;
- `DiameterDimensionTool` — diameter label using `Ø`;
- `AngularDimensionTool` — angular label in degrees, including reflex angles greater than 180°.

`PolylineTool` supports:

```text
click / coordinates / relative coordinates / direct distance / snap / Ortho / Polar Tracking
Enter -> finish open polyline
C     -> close polyline
Esc   -> cancel
```

Example:

```text
Polyline
100,100
@100,0
@0,50
@-100,0
C
```

This creates a closed rectangular polyline.

---


## Measure tools

Implemented non-mutating measure tools:

- `MeasureDistanceTool` / `Distance` — two points; reports distance, ΔX, ΔY and angle;
- `MeasureEntityTool` / `Entity` — click an entity; reports length, radius, diameter, circumference, sweep, area or vertex information depending on entity type;
- `MeasureAngleTool` / `Angle` — three points; reports angle and supplementary angle;
- `MeasureAreaTool` / `Area` — click a closed polyline; reports area, perimeter/length and vertex count.

Measure tools do not create geometry, do not execute undoable commands and do not mark the drawing dirty. They use snap and Polar Tracking where appropriate. Entity-based measure tools use entity snap and support `Ctrl+click` cycling through overlapping entities.

---

## Editing and transform tools

Implemented editing tools:

- `MoveTool`;
- `CopyTool`;
- `DeleteTool`;
- `RotateTool`;
- `ScaleTool`;
- `AlignTool`.

`RotateTool` uses base point, reference point and destination point. With Ortho enabled, interactive rotation is constrained to 90-degree directions. Polar Tracking is currently applied to point-placement tools such as line, polyline and move, not to explicit rotate-angle computation.

`ScaleTool` uses base point, reference point and destination point. The scale factor is calculated as:

```text
factor = distance(base, destination) / distance(base, reference)
```

`AlignTool` maps two source points to two destination points. After the fourth point, the tool asks whether scaling should be applied:

```text
Enter or N -> align without scale
Y          -> align with uniform scale
```

All edit and transform tools create undoable commands and modify the document through `CadDocument`.

`MoveTool` can now start even when no entity is selected. In that case it first enters an entity-selection phase, then moves to the usual base-point/destination-point workflow. During this first phase only entity snap is active; after confirmation, the tool returns to the normal geometric snaps.

### Modify tools

Implemented modify tools:

- `Break Point` — splits a `LineEntity` into two lines at one picked point;
- `Break Segment` — removes the segment between two picked break points on a `LineEntity`;
- `Extend` — extends supported open entities to a picked boundary;
- `Trim` — trims supported entities against a picked cutting edge.

Current `Trim`/`Extend` geometry support includes lines, arcs, circles and polylines where the operation is well-defined. Circles can be trimmed into arcs, but they are not extended because they are closed entities. These tools use geometric services in Core and commit through `ModifyEntitiesCommand`, so undo/redo and locked-layer protection remain consistent.

---

## Grip editing

Grip editing allows direct geometry modification through visible grip points.

Current behavior:

- `Tab` enters grip edit mode;
- if one entity is selected, that entity is edited;
- if multiple entities are selected, the last selected entity is edited;
- grips show cold, hot and active visual states;
- moving a grip shows a preview;
- committing a grip edit uses an undoable replacement command;
- `Esc` cancels the active grip or exits grip edit mode.

Grip editing does not bypass locked-layer protection because final modifications still go through document mutation APIs.

---

## Layers

Layers control visibility, lock state and the line format used by rendering.

Current behavior:

- hidden layer entities are not rendered, selected or snapped to;
- locked layer entities remain visible;
- locked layer entities are not selectable;
- locked layer entities can still be used for snapping;
- locked layer entities cannot be removed, replaced, moved or transformed.

The current layer must always remain:

```text
visible
unlocked
```

This avoids ambiguous drawing behavior when creating new entities.

### Layer Manager

The Layer Manager is a dedicated window opened from the `Layers...` button. It avoids cluttering the main CAD screen.

Current Layer Manager features:

- create layer;
- delete layer if empty and not current;
- rename layer;
- set current layer;
- toggle visible/locked for non-current layers;
- choose the line format used by each layer;
- apply changes only with OK;
- discard changes with Cancel;
- apply layer changes through one undoable command.

Rules:

- layer `0` is protected;
- layer `0` cannot be deleted or renamed;
- the current layer cannot be hidden or locked;
- layer names must be non-empty and unique;
- layers with entities cannot be deleted.

### Line Format Manager

The Line Format Manager is opened from `Line formats...`.

It manages reusable stroke definitions used by layers:

- format name;
- color;
- line weight;
- line style;
- built-in formats, editable but not deletable;
- user-defined formats;
- undoable application through `UpdateLineFormatsCommand`.

Default formats are `Continua`, `Asse`, `Tratteggiata`, `Tratto due punti` and `Tratto e punto`.

### Text Format Manager

The Text Format Manager is opened from `Text formats...`.

It manages reusable appearance definitions for single-line text annotations:

- format name;
- font family;
- text height in model units;
- color;
- bold;
- italic;
- preview;
- built-in formats, editable but not deletable;
- user-defined formats;
- undoable application through `UpdateTextFormatsCommand`.

Default text formats are `Standard`, `Title`, `Annotation` and `Small`.

---

## Property Panel v1

The Property Panel is a right-side optional panel. It is read-only in v1.

It shows:

- document state when nothing is selected;
- line properties: start, end, length, `DX`, `DY`, angle and bounds;
- circle properties: center, radius, diameter, area, circumference and bounds;
- point properties: position and bounds;
- text properties: text content, insertion point, rotation, text format and bounds;
- polyline properties: vertices, closed/open state, length, area when closed and bounds;
- multiple-selection summary: count, entity types, layers and combined bounding box.

The panel does not modify the document, does not create commands and does not affect undo/redo.

---

## Command line input

OpenCad2D supports CAD-style command line point input.

While a tool is waiting for a point, the user can type directly without first focusing the command input box.

Supported formats:

| Input | Meaning |
|---|---|
| `100,50` | absolute UCS coordinates |
| `@50,0` | relative UCS offset from the current base point |
| `5` | direct distance entry from the current base point along the cursor direction |

The command line does not create entities directly. It resolves typed input to a CAD point and forwards it to the active tool exactly like a mouse click.

---

## Ortho mode and Polar Tracking

OpenCad2D supports two related input constraints:

```text
Ortho            legacy horizontal/vertical constraint
Polar Tracking   configurable angular step: Off, 90°, 45°, 30° or 15°
```

Polar Tracking constrains interactive point input to the nearest multiple of the selected angle from the current base point. For example, `45°` allows directions at `0°`, `45°`, `90°`, `135°` and so on.

Input resolution order is intentionally:

```text
raw cursor -> snap candidate -> Polar/Ortho angular constraint
```

This means a snapped point may then be projected onto the active polar direction. Explicit coordinate input remains exact. Direct distance input uses the effective constrained direction when Polar Tracking or Ortho is active.

---

## Grid and viewport performance

The grid supports:

- separate visual grid display and grid snapping;
- rectangular and isometric layouts;
- major and minor grid spacing;
- configurable origin;
- zoom-based minimum/maximum screen spacing thresholds;
- a `Grid...` settings window in the top CAD bar.

The isometric grid is rendered with vertical lines plus two diagonal families at `+angle` and `-angle`. With the default `30°` angle, vertical lines are spaced so that major/minor verticals pass through the vertices created by diagonal intersections.

Viewport culling is applied during rendering. The canvas renders only visible entities whose bounding boxes intersect the visible world area, with a small safety margin.

The status bar shows:

```text
Rendered: visible-on-screen / total-visible
```

Grid, preview, grips, snap marker and crosshair are not culled as normal entities.

---

## Architecture

OpenCad2D is split into focused projects:

```text
src/
  OpenCad2D.Geometry/
  OpenCad2D.Core/
  OpenCad2D.Interaction/
  OpenCad2D.Tools/
  OpenCad2D.Persistence/
  OpenCad2D.Export/
  OpenCad2D.App/

tests/
  OpenCad2D.Geometry.Tests/
  OpenCad2D.Core.Tests/
  OpenCad2D.Interaction.Tests/
  OpenCad2D.Tools.Tests/
  OpenCad2D.Persistence.Tests/
```

The dependency direction is intentional:

```text
App -> Persistence -> Core -> Geometry
App -> Export -> Core -> Geometry
App -> Tools -> Interaction -> Core -> Geometry
```

`OpenCad2D.Geometry` contains geometric primitives, operations, transformations, tolerance handling and coordinate systems.

`OpenCad2D.Core` contains CAD entities, layers, styles, document model, spatial indexing, commands and command history.

`OpenCad2D.Interaction` contains hit testing, selection and snapping.

`OpenCad2D.Tools` contains UI-independent CAD tools, controllers and workspace logic.

`OpenCad2D.Persistence` contains the internal document serializer.

`OpenCad2D.Export` contains export services such as SVG export. It is independent from Avalonia and Tools.

`OpenCad2D.App` is the Avalonia desktop application. It handles presentation, input, file dialogs, viewport navigation and rendering.

The main architectural rule is that CAD behavior should remain outside the UI.

---

## Documentation

Technical documentation lives in the [`docs`](docs/) folder.

Recommended reading:

- [Architecture](docs/architecture.md)
- [Commands](docs/commands.md)
- [Tools](docs/tools.md)
- [Snapping](docs/snapping.md)
- [Polar Tracking](docs/polar-tracking.md)
- [Roadmap](docs/roadmap.md)
- [Transform Tools](docs/transform-tools.md)
- [Modify Tools](docs/modify-tools.md)
- [Export](docs/export.md)
- [Layer Appearance](docs/layer-appearance.md)
- [Line Formats](docs/line-formats.md)
- [Text Formats](docs/text-formats.md)
- [Text and Dimensions](docs/text-and-dimensions.md)
- [AI Handoff Document](docs/ai-handoff.md)

---

## Build and run

OpenCad2D targets **.NET 8**.

Build:

```bash
dotnet build
```

Run:

```bash
dotnet run --project src/OpenCad2D.App
```

Run tests:

```bash
dotnet test
```

If you use the included `Makefile`:

```bash
make run
make build
make test
make check
make clean
```

---

## Development principles

OpenCad2D follows a few practical rules:

- CAD logic should remain independent from Avalonia.
- Geometry should not depend on the document model or the UI.
- User-facing document changes should go through commands.
- Commands should modify the document through the `CadDocument` API.
- Tools should work in model/user coordinates, not screen pixels.
- The UI should convert input and render output, not own CAD behavior.
- The command line should resolve input to points and forward them to the active tool, not create entities directly.
- Geometric snapping should query visible spatial candidates.
- Entity snapping should query selectable candidates, meaning visible entities that are not on locked layers.
- Selection should query selectable entities, meaning visible entities that are not on locked layers.
- Rendering should use viewport culling for normal entities.
- Numeric comparisons in geometry should use `GeometryTolerance`.
- New tools should be testable without launching the desktop application.
- Stable UI areas such as the file command bar should not be mixed into tool-specific layout regions.

---

## Roadmap summary

Recently completed:

1. persistence and stable file command bar;
2. grid configuration with `Grid...` dialog and rectangular/isometric layouts;
3. viewport culling;
4. property panel v1;
5. layer manager v1;
6. rotate, scale and align tools;
7. polyline tool v1;
8. arc tools, rectangle by sides and broader Trim/Extend geometry support;
9. SVG export;
10. entity snap for selection-oriented tools and Ctrl-click cycling for overlapping entities;
11. selected-entity assignment to the current layer.

Next planned areas:

1. update grip editing for polyline vertices;
2. refine Trim/Extend previews and edge cases for arcs/circles/polylines;
3. distance and area measure tools;
4. text and dimensions;
5. application/session settings;
6. richer layer appearance: fill color and draw order;
7. DXF/PDF import/export experiments and richer SVG options.

---

## License

OpenCad2D is released under the **GNU General Public License v3.0 or later**.

See the [`LICENSE`](LICENSE) file for details.

---

## Author

Created by **Emilie Rollandin**.

GitHub: [archistico](https://github.com/archistico)
