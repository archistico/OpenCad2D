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

`MTEXT` inserts multiline annotation text through the text dialog. `LINE` creates a single segment and then ends. `POLYLINE` supports `Close`, `Undo` and Enter to finish an open polyline. `SPLINE` creates an open or closed Bezier spline from control points, with `Undo`, `Close` and Enter-to-finish command flow.

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
| Align | point-based transform align |
| Break Point | line/arc/ellipse/polyline target support |
| Break Segment | line/arc/circle/ellipse/polyline target support |
| Extend | line/arc/open-polyline target support |
| Trim | cutting edges including ellipses, `All`, in-command `Undo` |
| Offset | line/circle/arc/polyline with preview |
| Fillet | line-line, Radius and Trim/NoTrim options, radius 0 sharp join |
| Mirror | two-point mirror axis, keeps source by default, optional source deletion |

---

## Offset

Workflow:

```text
OFFSET: Specify offset distance:
OFFSET: Select object to offset:
OFFSET: Specify side to offset:
```

Supported targets:

- Line;
- Circle;
- Arc;
- straight-segment open/closed Polyline;
- Bezier Spline through sampled polyline approximation.

Polyline and sampled spline offset use miter joins with a conservative miter limit. Very sharp joins fall back to a bevel-style corner instead of producing long miter spikes. Rounded joins, configurable join styles, bulge/arc polyline segments and advanced self-intersection cleanup are future work.

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
- a live preview is shown while choosing the second line;
- trim mode is always on;
- Line-Arc, Arc-Arc, polyline fillet and NoTrim are future work.

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
- the default final Enter keeps the source entities and creates mirrored copies;
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
