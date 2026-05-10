# Modify Tools

Modify tools change existing geometry by splitting, shortening or extending entities.

The first implemented scope is intentionally limited to `LineEntity`. This keeps behavior predictable and heavily testable before extending the same concepts to polylines, arcs and circles.

---

## Implemented infrastructure

Core services:

```text
LineParameterService
LineIntersectionService
LineBreakService
LineExtendService
LineTrimService
```

Command:

```text
ModifyEntitiesCommand
```

`ModifyEntitiesCommand` can replace one or more original entities with zero, one or more resulting entities. This is necessary because modify operations may split one entity into two pieces or remove a piece entirely.

---

## Break Point

Scope:

```text
LineEntity only
```

Workflow:

```text
activate Break Point
pick target line
pick break point
project point onto line
replace original line with two line segments
```

The tool rejects break points too close to the line endpoints to avoid degenerate segments.

---

## Break Segment

Scope:

```text
LineEntity only
```

Workflow:

```text
activate Break Segment
pick target line
pick first break point
pick second break point
project both points onto line
remove segment between the two projected points
```

The two break points are ordered along the line internally, so click order does not matter. The result may be zero, one or two valid line segments.

---

## Extend

Scope:

```text
Boundary: LineEntity
Target:   LineEntity
```

Workflow:

```text
activate Extend
pick boundary line
pick target line near the endpoint to extend
extend that endpoint until it reaches the boundary
```

The tool remains active with the same boundary until `Esc`.

The operation is ignored if there is no valid extension intersection or if the intersection is already inside the original target segment.

---

## Trim

Scope:

```text
Cutting edge: LineEntity
Target:       LineEntity
```

Workflow:

```text
activate Trim
pick cutting edge
pick target line on the side to remove
trim target line to the cutting edge
```

The tool remains active with the same cutting edge until `Esc`.

The operation is ignored if the cutting edge does not intersect the target internally.

---

## Design rules

```text
Geometry calculation belongs in Core services.
Tools own interaction state only.
Document mutations go through ModifyEntitiesCommand.
CadDocument remains the final mutation boundary.
Locked-layer rules must not be bypassed.
Preview must not modify the document.
```

---

## Future work

Next improvements:

- support open `PolylineEntity` segments;
- support multi-boundary trim/extend;
- add clearer status messages for ignored operations;
- evaluate arc/circle support when arc editing becomes mature;
- add richer previews for removed/remaining segments.
