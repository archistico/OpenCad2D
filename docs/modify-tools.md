# Modify Tools

Modify tools change existing geometry by splitting, shortening or extending entities.

The current implementation keeps the interaction simple while moving the geometric logic into Core services. Tools own only the workflow state; Core owns the calculations; document mutations go through undoable commands.

---

## Implemented infrastructure

Core services include:

```text
CadEntityIntersectionService
CadTrimService
CadExtendService
LineParameterService
LineIntersectionService
LineBreakService
```

Command:

```text
ModifyEntitiesCommand
```

`ModifyEntitiesCommand` can replace one or more original entities with zero, one or more resulting entities. This is necessary because modify operations may split one entity into pieces, remove a piece entirely or replace a closed entity with one or more open pieces.

---

## Break Point

Current scope:

```text
Targets: LineEntity, ArcEntity, PolylineEntity
CircleEntity: not applicable, with a clear message
```

Workflow:

```text
activate Break Point
pick target entity
pick break point
project point onto the selected entity
replace original entity with the resulting pieces
```

Supported behavior:

```text
LineEntity       -> two line segments
ArcEntity        -> two arcs preserving the original direction
Open Polyline    -> two open polylines
Closed Polyline  -> one open polyline cut at the break point
CircleEntity     -> not applicable; use Break Segment instead
```

The tool rejects break points too close to non-useful endpoints to avoid degenerate pieces.

---

## Break Segment

Current scope:

```text
Targets: LineEntity, ArcEntity, CircleEntity, PolylineEntity
```

Workflow:

```text
activate Break Segment
pick target entity
pick first break point
pick second break point
project both points onto the selected entity
remove the segment between the two projected points
replace the original entity with the remaining valid pieces
```

Supported behavior:

```text
LineEntity       -> zero, one or two remaining line segments
ArcEntity        -> zero, one or two remaining arcs preserving the original direction
CircleEntity     -> one remaining major arc after removing the minor arc between the picked points
Open Polyline    -> zero, one or two open polylines
Closed Polyline  -> one open polyline after removing the shortest path between the picked points
```

The two break points are internally projected onto the target entity. For lines, arcs and open polylines, the path order is normalized so click order does not matter. For circles, the minor arc between the two points is removed. For closed polylines, the shortest path between the two points is removed. Degenerate results are discarded.

---

## Extend

Current boundary support:

```text
LineEntity
CircleEntity
ArcEntity
PolylineEntity
```

Current target support:

```text
LineEntity
ArcEntity
open PolylineEntity
```

`CircleEntity` is intentionally not extended because it is already closed.

Workflow:

```text
activate Extend
pick boundary entity
pick target entity near the endpoint to extend
extend the nearest valid endpoint until it reaches the boundary
```

The tool remains active with the same boundary until `Esc`.

Preview behavior:

- the full resulting entity is shown with the normal preview style;
- for line targets, the newly added extension segment is highlighted separately;
- for arc targets, the newly added arc portion is highlighted separately;
- for open polyline targets, the newly added endpoint segment is highlighted separately.

The operation is ignored if there is no valid extension intersection or if the target cannot be extended in a meaningful way. The status message explains that no valid endpoint-to-boundary extension was found from the picked side. Closed polylines, circles, points, text and dimensions are not valid Extend targets.

---

## Trim

Current cutting-edge support:

```text
LineEntity
CircleEntity
ArcEntity
PolylineEntity
```

Current target support:

```text
LineEntity
CircleEntity
ArcEntity
PolylineEntity
```

Important behavior:

- trimming a line may shorten it or split it depending on the picked side and intersections;
- trimming a circle can replace it with one or more arcs;
- trimming an arc keeps the remaining valid arc portion;
- trimming a polyline works on its component segments where supported by the current service.

Workflow:

```text
activate Trim
pick cutting edge
pick target entity on the side/portion to remove
replace the target with the remaining geometry
```

Optional two-cutting-edge workflow for line targets:

```text
activate Trim
pick first cutting edge
Ctrl-click second cutting edge
pick target line portion to remove
replace the target with the remaining line fragment(s)
```

The Ctrl-click step keeps the existing single-boundary workflow compatible. With two cutting edges, a line target can remove the middle interval or one of the external intervals depending on where the target is picked. Adjacent remaining intervals are merged when the removed portion is external.

The tool remains active with the selected cutting edge(s) until `Esc`.

Preview behavior:

- the remaining geometry is shown with the normal preview style;
- for line targets, the portion that will be removed is highlighted separately;
- with two cutting edges, the highlighted preview shows the interval selected by the target pick.

The operation is ignored if the cutting edge(s) do not produce a valid trim result. The status message explains that the picked side cannot be trimmed by the selected boundary or cutting edges.

---

## Design rules

```text
Geometry calculation belongs in Core services.
Tools own interaction state only.
Document mutations go through ModifyEntitiesCommand.
CadDocument remains the final mutation boundary.
Locked-layer rules must not be bypassed.
Preview must not modify the document.
Closed entities such as circles are trimmable but not extendable.
```

---

## Current limitations and follow-up work

Recommended next refinements:

- broaden highlighted previews for Trim on arcs, circles and polylines;
- continue systematic regression tests for Break Point and Break Segment;
- add additional degenerate-case tests for tangent, near-tangent and overlapping geometry;
- broaden multi-boundary Trim beyond line targets only if the core geometry remains stable.

---

## v0.5 audit summary

The v0.5 milestone will focus on advanced editing and refinement. The detailed planning document is:

```text
docs/v0.5-modify-tools-audit.md
```

Current state before v0.5 implementation:

```text
Break Point    LineEntity, ArcEntity and PolylineEntity; CircleEntity returns a not-applicable message
Break Segment  LineEntity, ArcEntity, CircleEntity and PolylineEntity
Trim           Line/Circle/Arc/Polyline targets with one cutting edge
Extend         Line/Arc/open Polyline targets to Line/Circle/Arc/Polyline boundaries
```

Main v0.5 implementation decisions:

```text
Break Point on CircleEntity: not applicable with clear message
Break Segment on CircleEntity: remove minor arc between two picked points
Trim with two cutting edges: 3-click workflow, edge 1 -> Ctrl-click edge 2 -> target portion
Locked visible references: usable as boundary/cutting edge, not editable as targets
Hidden entities: ignored entirely by modify tools
```

Layer behavior is now covered by regression tests:

```text
Break Point / Break Segment:
    hidden or locked targets are ignored.

Trim:
    hidden cutting edges are ignored;
    locked visible cutting edges can be used as references;
    locked targets are not modified.

Extend:
    hidden boundaries are ignored;
    locked visible boundaries can be used as references;
    locked targets are not modified.
```

Recommended order:

```text
1. Break Point advanced
2. Break Segment advanced
3. Trim with two cutting edges
4. Extend consolidation
5. Layer rules
6. systematic tests and v0.5 closure
```
