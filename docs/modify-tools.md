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
Target: LineEntity
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

Current scope:

```text
Target: LineEntity
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
- for line targets, the newly added extension segment is highlighted separately.

The operation is ignored if there is no valid extension intersection or if the target cannot be extended in a meaningful way. The status message explains that no valid endpoint-to-boundary extension was found from the picked side.

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

The tool remains active with the same cutting edge until `Esc`.

Preview behavior:

- the remaining geometry is shown with the normal preview style;
- for line targets, the portion that will be removed is highlighted separately.

The operation is ignored if the cutting edge does not produce a valid trim result. The status message explains that the picked side cannot be trimmed by the selected boundary.

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

- broaden highlighted previews beyond line targets for Trim/Extend on arcs, circles and polylines;
- broaden break operations beyond `LineEntity`;
- add additional degenerate-case tests for tangent, near-tangent and overlapping geometry;
- evaluate multi-boundary trim/extend workflows.
