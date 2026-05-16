# Modify Tools

Modify tools change existing geometry. Tools own workflow state; geometry calculations should be delegated to focused services; mutations must be undoable.

---

## Break Point

Targets:

- Line;
- Arc;
- Ellipse;
- Polyline;
- Bezier Spline, converted to an open polyline approximation.

Circle is not applicable; use Break Segment for circles. Ellipse Break Point opens the ellipse into an approximated open polyline starting and ending at the break point.

Workflow:

```text
BREAKPOINT: Select entity:
BREAKPOINT: Specify break point:
```

The picked point is projected onto the target entity. Degenerate pieces are rejected.

---

## Break Segment

Targets:

- Line;
- Arc;
- Circle;
- Ellipse;
- Polyline;
- Bezier Spline, converted to an open polyline approximation.

Workflow:

```text
BREAK: Select entity:
BREAK: Specify first break point:
BREAK: Specify second break point:
```

For circles, the minor arc between the two projected points is removed. For ellipses, the removed portion is approximated and the remaining ellipse path is returned as an open polyline. Open polylines can be broken on internal segments. Closed polylines and regular polygons are opened and the shortest path between points is removed.

---

## Extend

Boundary support:

- Line;
- Circle;
- Arc;
- Polyline.

Target support:

- Line;
- Arc;
- open Polyline.

Workflow:

```text
EXTEND: Select boundary entity:
EXTEND: Select target entity:
```

The target endpoint nearest the picked side is extended to the boundary when a valid extension exists.

---

## Trim

Cutting-edge support:

- Line;
- Circle;
- Arc;
- Ellipse;
- Polyline.

Target support:

- Line;
- Circle;
- Arc;
- Ellipse;
- Polyline;
- Bezier Spline, converted to polyline approximation fragments.

Workflow:

```text
TRIM: Select cutting edge or [All]:
TRIM: Select entity side to trim or [All/Undo]:
```

Features:

- `All` uses all visible supported entities as cutting edges;
- target entity is excluded from its own cutting-edge set in All mode;
- additional cutting edges can be selected;
- `Undo` reverses the last trim inside the active Trim command;
- command remains active for repeated trims until cancelled or confirmed;
- open and closed polylines, including regular polygons stored as closed `PolylineEntity`, can be trimmed;
- trimmed polyline/polygon fragments are returned as open `PolylineEntity` fragments;
- trimmed ellipse fragments are returned as open polyline approximations because the model currently has a full `EllipseEntity` but no partial ellipse-arc entity;
- trimmed spline fragments are returned as open polyline approximations, preserving layer/style/draw-order metadata.

---

## Offset

Workflow:

```text
OFFSET: Specify offset distance:
OFFSET: Select object to offset:
OFFSET: Specify side to offset:
```

Targets:

- Line;
- Circle;
- Arc;
- straight-segment open/closed Polyline;
- Bezier Spline, offset through sampled polyline approximation.

Rules:

- line offset creates a parallel line;
- circle/arc offset changes radius based on picked side;
- polyline/spline approximation offset uses miter joins;
- invalid or degenerate results are rejected;
- live preview is shown while choosing the side.

Future work:

- rounded joins;
- advanced self-intersection cleanup;
- polyline bulge/arc segment support;
- Multiple/Through/Erase/Layer options.

---

## Fillet

Workflow:

```text
FILLET: Select first line or [Radius/Trim] <r> (Trim):
FILLET: Specify fillet radius:
FILLET: Specify trim mode <Trim>:
FILLET: Select second line:
```

Targets:

- Line-Line.

Rules:

- Radius option sets the active fillet radius;
- radius `0` creates a sharp-corner join;
- radius greater than `0` creates a tangent arc;
- while selecting the second line, Fillet shows a live preview of the final result;
- `Trim` mode trims/replaces the source lines and adds the tangent arc;
- `NoTrim` mode keeps the source lines unchanged and adds only the tangent arc;
- radius `0` requires `Trim` mode because `NoTrim` would not create new geometry.

Future work:

- Line-Arc;
- Arc-Arc;
- polyline fillet.
