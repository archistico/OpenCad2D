# Tools

Tools own user workflow state. Geometry calculations should live in Core services or focused Tools services. Document mutations must be executed through commands.

---

## Tool pipeline

A typical tool:

1. exposes a prompt state;
2. receives pointer input and/or command input;
3. updates preview state;
4. creates undoable commands when confirmed;
5. resets or advances its workflow state.

Command-driven tools implement `ICommandDrivenTool`.

---


## HUD and snap rules for interactive tools

The dynamic HUD is the primary visible command-input surface. A tool phase should expose only fields that are meaningful for that phase:

- point phases may expose `X/Y` or `Distance/Angle` when a base point exists;
- scalar phases may expose one scalar field such as radius, sides, distance, factor or height;
- entity-selection phases should not expose editable numeric point fields;
- dialog-owned values such as block name, selected block definition, block scale and block rotation stay in the dialog, not in the HUD.

Selection phases in modify tools must request `SnapKind.EntityOnly`. Point snaps such as endpoint, midpoint and intersection should not be active while the user is being asked to pick an entity rather than a geometric point.

## Drawing tools

Implemented:

- `PointTool`
- `DivideTool`
- `TextTool`
- `MultilineTextTool`
- `LineTool`
- `RectangleTool`
- `RectangleBySidesTool`
- `CircleTool`
- `EllipseTool`
- `ArcTool`
- `ArcThreePointsTool`
- `PolylineTool`
- `SplineTool`

`DIVIDE` follows AutoCAD naming/semantics: it selects one line, arc, circle or polyline, asks for an integer segment count from 2 to 1000, and creates persistent `PointEntity` markers at equal divisions without splitting the source entity. Open entities create `N - 1` internal points; closed entities create `N` points from the conventional start point. Points are created on the current layer and committed as one undoable operation. The segment count is edited through the dynamic HUD `Segments` field; like `Polygon` sides, this whole-number scalar is validated only on confirmation so intermediate typed values are not clamped back while the user is still editing.

`MTEXT` inserts multiline annotation text through the text dialog. `LINE` creates a single segment and then ends. `POLYLINE` supports `Close`, `Undo` and Enter/right-click to finish an open polyline. `SPLINE` creates an open or closed Bezier spline from control points, with `Undo`, `Close` and Enter/right-click-to-finish command flow.

---

## Symbols, library and parametric helpers

Implemented first pass:

- `NorthSymbolTool`
- `ScaleBarTool`
- Library Browser for `.opencad2d.json` snippets

`NorthSymbolTool` inserts a simple north arrow made of ordinary geometry: three lines, one circle and one `TextEntity` with label `N`. The first version uses the picked point as the `(0,0)` symbol base point, honors active snaps for the insertion point, uses the current layer/current text format, and commits insertion as one undoable command. Aliases: `NORTH`, `NORTHSYMBOL`, `NS`.

`ScaleBarTool` inserts the requested 0–1000 graphic scale bar as ordinary closed polylines, vertical tick lines and text labels. The picked point is the local `(0,0)` base point. Aliases: `SCALEBAR`, `SBAR`, `GRAPHICSCALE`.

The Library Browser scans `library/**/*.opencad2d.json`, groups items by the first folder below `library/`, shows a vector preview and inserts the selected item as a reusable block reference. The source file origin `(0,0)` is the insertion base point. Repeated insertions reuse the deterministic library block definition. See `docs/library-browser.md`.

Fixed reusable content should be stored as `.opencad2d.json` files under `library/` rather than becoming separate toolbar buttons. Direct symbol/tool buttons should be kept for parametric generators such as doors, windows, stairs, configurable section/elevation markers and title blocks.

## Dimension tools

Implemented:

- Horizontal Dimension;
- Vertical Dimension;
- Aligned Dimension;
- Radius Dimension;
- Diameter Dimension;
- Angular Dimension.

Dimensions are currently non-associative.

---

## Selection and navigation

Selection supports point/window/crossing workflows, hidden/locked layer rules and overlapping entity cycling.

Navigation tools:

- Zoom Window;
- Zoom Extents;
- pan;
- reset view.

---

## Edit, transform and modify tools

Implemented:

| Tool | Notes |
|---|---|
| Delete | in Edit group |
| Move | command-driven, supports typed points/distances |
| Copy | command-driven, supports typed points/distances |
| Rotate | command-driven, typed angle support |
| Scale | command-driven, typed factor support |
| Align | point-based transform align, optional scale confirmation |
| Break Point | line/arc/ellipse/polyline/open-spline target support |
| Break Segment | line/arc/circle/ellipse/polyline/open-spline target support |
| Extend | line/arc/open-polyline target support |
| Trim | cutting edges including ellipses, `All`, in-command `Undo` |
| Offset | line/circle/arc/polyline with preview |
| Boundary Fill | click inside a closed linear boundary to create a filled closed polyline; v2 core support for sampled curve boundaries is in progress behind service options |
| Fillet | line-line plus adjacent linear-polyline segments, Radius and Trim/NoTrim options, radius 0 sharp join for lines |
| Mirror | two-point mirror axis, keeps source by default, optional source deletion |
| Explode | selected polylines become individual lines/arcs; block references become world-space entities |
| Join | selected connected lines/arcs/open polylines become one or more polylines |

---

## Explode and Join

Workflow:

```text
EXPLODE: Select polylines or blocks to explode:
JOIN: Select connected lines, arcs and open polylines to join:
```

Both tools are selection-first, support multi-pick selection, use EntityOnly snap while selecting entities and accept Enter/right-click to confirm. Explode supports open and closed polylines, including mixed bulge segments: straight segments become `LineEntity` objects and curved bulge segments become `ArcEntity` objects. Block references still explode into transformed world-space child entities. Join supports connected line, arc and open-polyline chains and can create either open or closed polylines; disconnected connected groups become separate polylines.

---

## Offset

Workflow:

```text
OFFSET: Specify offset distance or first distance point <last>:
OFFSET: Specify second distance point or type distance:
OFFSET: Select object to offset:
OFFSET: Specify side to offset:
```

Supported targets:

- Line;
- Circle;
- Arc;
- straight-segment open/closed Polyline.

Polyline offset uses miter joins with a conservative miter limit. Very sharp joins fall back to a bevel-style corner instead of producing long miter spikes. Ellipse, elliptical arc and Bezier spline offset are intentionally deferred because true offsets are not the same native curve type. Rounded joins, configurable join styles, bulge/arc polyline segments and advanced self-intersection cleanup are future work.

The side preview must use the same geometry method as final creation.

---

## Boundary Fill

Workflow:

```text
BFILL: Pick inside a closed linear boundary:
```

`BoundaryFillTool` scans visible linear boundaries, splits them at intersections, builds planar faces and creates a new closed `PolylineEntity` for the face containing the picked point. The generated polyline is `IsFilled = true`, uses the current layer and is added through `AddEntityCommand`, so undo removes the fill boundary.

Current first-pass boundary support:

- standalone `LineEntity` segments;
- straight `PolylineEntity` segments, including rectangles and polygons;
- intersecting linear segments, where the clicked face can be selected from the resulting planar subdivision.

Curved boundaries, blocks, hatch patterns, holes and associative boundary updates are deferred. Aliases: `BFILL`, `BF`, `BOUNDARYFILL`, `FILL`, `RIEMPIMENTO`.

Planned v2 sequence:

1. Preview the detected boundary under the cursor before creation.
2. Add sampled curve boundaries for arcs and circles, keeping the generated result as a filled `PolylineEntity`.
3. Surface the implemented gap tolerance in the tool workflow, with clear diagnostics when the gap cannot be closed safely.
4. Revisit holes/islands through a real `HatchEntity`, because a single `PolylineEntity` cannot represent subtractive inner loops.

---

## Fillet

Workflow:

```text
FILLET: Select first line or [Radius/Trim] <r> (Trim):
FILLET: Specify fillet radius:
FILLET: Specify trim mode <Trim>:
FILLET: Select second line:
```

Supported targets:

- Line-Line;
- two adjacent straight segments of the same linear `PolylineEntity`;
- terminal segments of separate open linear `PolylineEntity` objects.

Rules:

- Radius `0` creates a sharp-corner join in Trim mode for Line-Line;
- polyline segment fillet requires a radius greater than zero;
- radius greater than zero creates a tangent arc;
- Line-Line in `Trim` mode replaces the source lines with trimmed line segments plus the fillet arc;
- Line-Line in `NoTrim` mode keeps the source lines and adds only the fillet arc;
- same-polyline segment fillet requires `Trim` mode and keeps the result as one `PolylineEntity` with one bulge segment;
- separate polyline fillet trims the selected terminal segment on each source polyline and adds an `ArcEntity` fillet, mirroring Line-Line behavior;
- multi-segment separate polyline fillet is intentionally deferred so the tool does not accidentally replace unrelated vertices;
- polyline segment fillet is limited to straight segments; existing curved/bulged polyline segments are rejected with a clear message;
- Enter/right-click accepts the current radius and trim-mode defaults;
- a live preview is shown while choosing the second object;
- Line-Arc, Arc-Arc and curved-polyline fillet are future work.

---

## Mirror

Workflow:

```text
MIRROR: Select objects to mirror:
MIRROR: Specify first point of mirror line:
MIRROR: Specify second point of mirror line:
MIRROR: Delete source objects? [Yes/No] <No>:
```

Rules:

- works on the current selection, or enters a selection phase when no entities are selected;
- the mirror axis is an infinite line defined by two points;
- the default final Enter/right-click keeps the source entities and creates mirrored copies;
- `Yes` deletes/replaces the source entities by mirroring them in place;
- preview is shown while choosing the second axis point.


## Draw order

Order tools:

- To Front;
- To Back;
- Forward;
- Backward.

Draw order is independent from layers. Higher draw order renders above lower draw order.

---

## Align and distribute object tools

Align tools use the bounding box of the whole selection:

- Align Left;
- Align Right;
- Align Top;
- Align Bottom.

Top/Bottom are defined visually on the canvas.

Distribution tools use entity centers:

- Distribute Horizontally;
- Distribute Vertically.

Distribution requires at least three selected entities and keeps first/last by sorted center position fixed.

---

## Measure tools

Implemented:

- Measure Distance;
- Measure Entity;
- Measure Angle;
- Measure Area.

Measure tools do not mutate the document.

---

## Grip editing

Grip editing is available for supported entities. Arc 3-point grip behavior is intentionally based on the three construction points:

- moving start keeps point-on-arc and end fixed;
- moving end keeps start and point-on-arc fixed;
- moving point-on-arc keeps start/end fixed and recomputes center/radius.

### Mirror axis preview update

The Mirror tool now draws the mirror axis while the user is choosing the second axis point. The preview also keeps showing the mirrored entities so the user can verify the axis direction before confirming whether source objects should be deleted.



## Polygon tool

The `Polygon` tool draws regular polygons as closed polylines. It is command-driven and supports `POLYGON` / `PG`. The first step asks for the side count, Enter accepts the default of 6, then the user specifies the center point and a vertex point or radius. The generated entity is a closed `PolylineEntity`.

## Preview provider protocols

Tools with previews made of normal CAD entities can implement `IToolPreviewEntityProvider`. Tools that also need guide lines, point markers, highlighted entities or other semantic overlays can implement `IToolPreviewDescriptorProvider`.

The app renderer asks the active tool for a descriptor first, then for simple preview entities. The former active-tool concrete fallback dispatch has been removed, so new preview-capable tools should expose their preview through one of these protocols instead of requiring a renderer `case SomeNewTool`.

The drawing tools now migrated to entity previews are `LineTool`, `RectangleTool`, `RectangleBySidesTool`, `CircleTool`, `ArcTool`, `ArcThreePointsTool`, `EllipseTool`, `PolylineTool`, `PolygonTool` and `SplineTool`. Dimension previews plus `MoveTool`, `CopyTool`, `RotateTool`, `ScaleTool`, `AlignTool`, `BreakAtPointTool`, `BreakBetweenPointsTool`, `FilletTool`, `OffsetTool` and `MeasureDistanceTool` are also migrated.

`MirrorTool` and `MeasureAngleTool` use descriptor previews because they need normal preview entities plus overlay markers/guide lines. `SelectionTool` and `ZoomWindowTool` use descriptor previews for their filled model-space windows. `GripEditTool` uses descriptor previews for grip markers, replacement-entity preview and base-to-destination measurement guides. `ExtendTool` and `TrimTool` use descriptor previews for their normal preview entities plus highlighted added/removed fragments.

---

## Toolbar icon resources

The main toolbar and left tool panel now use vector icons loaded from `src/OpenCad2D.App/Resources/Icons.axaml`.

The icons are stored as `StreamGeometry` resources generated from the SVG source paths. Buttons keep their existing click handlers and tooltips, but their content is now a left-aligned grid with:

- a `Path` bound to a `StreamGeometry` resource;
- a small spacing column;
- the button text aligned to the left.

The shared styling lives in `MainWindow.axaml` through the `icon-button-content`, `icon-button-path` and `icon-button-text` classes. Since the supplied SVG icons are outline/stroke icons, they are rendered with `Path` and `Stroke` rather than `PathIcon` fill rendering. The toolbar icon stroke uses the same yellow accent as the snap markers (`#FFE650`) and a slightly thinner `StrokeThickness` of `1.5` for a lighter visual weight.


## Icon licensing

The toolbar icons are derived from Tabler Icons SVG assets and converted to Avalonia `StreamGeometry` resources. Tabler Icons is licensed under the MIT License.

When distributing OpenCad2D with these icons, keep the Tabler Icons copyright and MIT license notice in `LICENSES/Tabler-Icons-MIT.txt` and keep the attribution summary in `docs/third-party-notices.md`.


Polyline vertex editing in the Property Panel is capped for responsiveness: up to 25 vertices are shown with editable coordinate/action rows, followed by a `More vertices` note for larger polylines. This avoids creating hundreds or thousands of controls in the right panel.


Polyline Property Panel performance: for large polylines, coordinate rows are shown for up to 25 vertices, while insert/delete action rows are hidden when the polyline has more than 10 vertices. This keeps selection and panel refresh responsive.


Property Panel update: `Layer id` is exposed as a combo box populated from the document layers. Polyline vertices are displayed as a single editable `X, Y` value per vertex. To keep the panel responsive, only the first 4 vertices are shown, followed by a `More vertices` note when needed.


Property Panel vertex list simplification: the `Vertices` section now shows only the first 4 polyline vertices as compact editable `X, Y` rows, followed by `More vertices` when applicable. Insert/delete/reorder actions are intentionally kept out of this section for now to keep the UI responsive and readable.


Property Panel: polyline `Closed` is exposed as a `Yes`/`No` combo box instead of a free text field.


Property Panel final cleanup:

- `Layer id` uses a combo box populated from document layer ids.
- `Dimension style` uses a combo box populated from document dimension styles.
- Polyline `Closed` uses a Yes/No combo box.
- Polyline vertices are shown in a compact `Vertices` section as editable `X, Y` rows, capped to the first 4 vertices with a `More vertices` note.

---

## External raster image reference tools

OpenCad2D supports local PNG/JPG/JPEG files as external raster references. The drawing stores the image path and oriented rectangle geometry only; raster bytes are never embedded.

Implemented toolbar workflows:

- `Attach Image`: creates a new external image reference near the current viewport center.
- `Replace Image`: relinks the selected image reference to another local raster file while preserving its CAD geometry.
- `Relink Missing`: relinks the selected missing reference, or the first missing reference found in the drawing.
- `Reset Aspect`: restores the selected reference height from its stored pixel aspect ratio while preserving width, center and rotation.
- `Collect Refs`: copies linked raster files into an `images/` folder beside the drawing file and saves portable relative paths.
- `Manage Refs`: opens the Image References Manager, which lists status, path, pixel size, CAD size, rotation, transparency percentage and instance count and provides select/relink/replace/open-folder actions plus an undoable transparency update.

Image references participate in selection, snapping, transform tools and grip editing like rectangular CAD entities. Endpoint snap exposes the four corners, midpoint snap exposes the four edge midpoints, center snap exposes the rectangle center and nearest snap uses the image border.


## Create Block

The Create Block workflow requires a non-empty selection. The dialog shows the current entity count and disables creation when the count is zero. From the dialog the user can return to the drawing to select entities; a normal single selection reopens the dialog immediately, while `Shift` selection keeps accumulating entities until `Enter` finishes the selection loop.

The base point can be typed in the dialog, entered through HUD `X/Y`, or picked from the drawing. Picking the base point returns to the dialog for review; the block is created only when the user presses OK. The visual geometry stays in place because the selected entities are translated into the block definition relative to the base point.


## Current fixed-symbol tools

### North Symbol

`NorthSymbol` inserts a fixed-size north arrow as ordinary CAD geometry on the current layer. Aliases: `NORTH`, `NORTHSYMBOL`, `NS`.

### Metric Scale Bar

`ScaleBar` inserts the 0–1000 metric graphic scale bar as ordinary polylines, lines and text geometry on the current layer. Aliases: `SCALEBAR`, `SBAR`, `GRAPHICSCALE`. The first version is inserted with one picked point used as local origin `(0,0)`.

## Library Browser

Fixed symbols and reusable snippets should be provided as library files rather than new toolbar buttons.

Workflow:

```text
Library button -> modal Library window -> category -> preview -> Insert -> pick insertion point
```

Expected library layout:

```text
library/arredo/*.opencad2d.json
library/simboli/*.opencad2d.json
library/sanitari/*.opencad2d.json
library/porte-finestre/*.opencad2d.json
library/annotazioni/*.opencad2d.json
```

Library items are inserted as block references by default. The browser creates or reuses a deterministic block definition for each item, then starts a canvas insertion-point workflow that honors active snaps. Explode Block is available when raw geometry is needed.

See `docs/library-browser.md` for item creation rules, folder layout and current limitations.

### Explode and Join stabilization

The Explode tool works on selected polylines and block references. Mixed polylines are decomposed segment-by-segment: `bulge == 0` creates a line, while non-zero bulge creates an arc with the same layer, style, visibility, lock state and draw order. Closed polylines include the closing segment, so a closing bulge becomes a closing arc.

The Join tool works on selected lines, arcs and open polylines. It converts every joinable input into an internal oriented segment, builds endpoint-connected chains, rejects ambiguous branching junctions and emits `PolylineEntity` objects. Reversed arc or polyline segments invert their bulge sign so the final curve direction remains geometrically correct.

Closed polylines and unsupported entity kinds are rejected with command-line feedback instead of failing silently.

### Fillet / Chamfer on linear polyline segments

`FILLET` and `CHAMFER` can now pick two adjacent linear segments from the same `PolylineEntity`.
The first picked segment is excluded when resolving the second pick on the same polyline, so clicking near the shared vertex can select the adjacent segment instead of repeatedly selecting the first one.
For this phase, existing bulged/curved polyline segments are rejected with explicit feedback.

### Fillet and Chamfer on polyline segments

`FILLET` and `CHAMFER` support linear polyline segments in addition to standalone lines. Supported cases include adjacent segments of the same linear polyline. `FILLET` also supports terminal segments of separate open linear polylines by trimming each source polyline and adding an arc. Curved/bulged polyline segments and internal trims on separate multi-segment polylines are rejected conservatively for now.

### Chamfer on separate simple polylines

`CHAMFER` also supports terminal segments of separate open linear polylines, plus mixed line/polyline pairs. The selected polyline source is trimmed and remains a `PolylineEntity`; the chamfer edge is created as a `LineEntity`.

Separate multi-segment polylines are still rejected conservatively. This avoids modifying only part of a larger polyline until segment-level replacement is implemented for separate entities.
