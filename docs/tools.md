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

## Drawing tools

Implemented:

- `PointTool`
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

`MTEXT` inserts multiline annotation text through the text dialog. `LINE` creates a single segment and then ends. `POLYLINE` supports `Close`, `Undo` and Enter/right-click to finish an open polyline. `SPLINE` creates an open or closed Bezier spline from control points, with `Undo`, `Close` and Enter/right-click-to-finish command flow.

---

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
| Fillet | line-line, Radius and Trim/NoTrim options, radius 0 sharp join |
| Mirror | two-point mirror axis, keeps source by default, optional source deletion |

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

## Fillet

Workflow:

```text
FILLET: Select first line or [Radius/Trim] <r> (Trim):
FILLET: Specify fillet radius:
FILLET: Specify trim mode <Trim>:
FILLET: Select second line:
```

Supported targets:

- Line-Line.

Rules:

- Radius `0` creates a sharp-corner join in Trim mode;
- radius greater than zero creates a tangent arc;
- `Trim` replaces the source lines with trimmed line segments plus the fillet arc;
- `NoTrim` keeps the source lines and adds only the fillet arc;
- Enter/right-click accepts the current radius and trim-mode defaults;
- a live preview is shown while choosing the second line;
- Line-Arc, Arc-Arc and polyline fillet are future work.

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
