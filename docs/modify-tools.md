# Modify Tools

Modify tools change existing geometry. Tools own workflow state; geometry calculations should be delegated to focused services; mutations must be undoable.

---

## Break Point

Targets:

- Line;
- Arc;
- Polyline.

Circle is not applicable; use Break Segment for circles.

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
- Polyline.

Workflow:

```text
BREAK: Select entity:
BREAK: Specify first break point:
BREAK: Specify second break point:
```

For circles, the minor arc between the two projected points is removed. For closed polylines, the shortest path between points is removed.

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
- Polyline.

Target support:

- Line;
- Circle;
- Arc;
- Polyline.

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
- command remains active for repeated trims until cancelled or confirmed.

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
- straight-segment open/closed Polyline.

Rules:

- line offset creates a parallel line;
- circle/arc offset changes radius based on picked side;
- polyline offset uses miter joins;
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
FILLET: Select first line or [Radius] <r>:
FILLET: Specify fillet radius:
FILLET: Select second line:
```

Targets:

- Line-Line.

Rules:

- Radius option sets the active fillet radius;
- radius `0` creates a sharp-corner join;
- radius greater than `0` creates a tangent arc;
- trim mode is always on.

Future work:

- Line-Arc;
- Arc-Arc;
- polyline fillet;
- NoTrim option.
