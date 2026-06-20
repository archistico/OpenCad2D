# Geometry intersections and overlap policy

This document records the current intersection contract used by OpenCad2D editing, snapping and curve operations. It is intentionally conservative: CAD commands may use rich boundary information when it has a finite and testable meaning, while visual snapping must not invent arbitrary points on coincident geometry.

## Public intersection paths

`CadEntityIntersectionService.Intersect(...)` is the compatibility path. It returns only finite point intersections as `Point2D` values. This is the right API for snapping and for callers that only need visible point intersections. It must not synthesize points for coincident full circles or other infinite intersection sets.

`CadEntityIntersectionService.IntersectDetailed(...)` is the editing path. It returns `CadIntersectionPoint` records with a shared point, target-side parameters and a `CadIntersectionKind`. The shared point is the coordinate that explicit-vertex entities should reuse directly. The parameters are the authoritative location for native curve rebuilds.

The detailed path is additive. Ordinary point intersections still come from `Intersect(...)`; finite overlap boundaries are added before ordinary points so that a boundary point that appears through both paths keeps the `Overlap` classification after de-duplication.

## Intersection kinds

The current editing model uses these meanings:

| Kind | Meaning | Current use |
|---|---|---|
| `Crossing` | A finite interior point shared by both editable curves. | Standard trim, break, extend and shared endpoint creation. |
| `Endpoint` | The shared point lies on at least one open-curve endpoint. | Used to preserve endpoint-only no-op rules where a command should not create degenerate fragments. |
| `Overlap` | A finite boundary point of a continuous shared interval. | Used by Trim as an additional cut source for supported overlap cases. |
| `Tangent` | Reserved classification for true tangential contact. | The enum exists, but the general detailed classifier does not yet populate this kind for all curve pairs. Do not depend on it until explicit tangent classification tests are added. |

The distinction between `Crossing` and `Overlap` is important. A crossing is a point where curves meet. An overlap describes an interval where curves coincide, but the editing pipeline still receives only the finite boundary points of that interval.

## Overlap and coincidence rules

OpenCad2D deliberately separates finite overlap boundaries from infinite coincidence.

Finite overlap boundaries are produced only where there is a stable native meaning:

```text
LineEntity <-> LineEntity                 overlapping segment endpoints
CircleEntity <-> ArcEntity                arc start/end on the same circular support
ArcEntity <-> ArcEntity                   start/end of each finite overlapping angular interval
```

Arc overlap detection is normalized to a counterclockwise angular representation internally. This allows clockwise arcs and arcs crossing 0°/360° to be split into finite intervals without treating negative raw angle values as errors.

Full coincident circles have infinitely many shared points and no finite boundary. `CircleIntersectionService.IntersectCircleCircle(...)` therefore returns an empty point list for coincident full circles, and `IntersectDetailed(...)` does not create synthetic cut points for them. This is intentional, especially for intersection snapping: snapping to an arbitrary point on a coincident circle would be misleading.

The same principle applies to future full-coincident or same-support curve pairs. A command may add explicit handling only if it can derive a finite endpoint, boundary, or user-selected parameter with predictable semantics.

## Command consumption rules

Visual intersection snapping should use `Intersect(...)` or an equivalent point-only path. It should not consume `Overlap` boundary cuts unless a future UX explicitly asks for overlap-boundary snap markers. This prevents coincident or partially overlapping curves from creating surprising snap points.

Trim may consume finite `Overlap` boundary points from `IntersectDetailed(...)` because Trim removes an interval from a target curve and needs valid cut locations. This is already implemented in `CadTrimService` by augmenting ordinary point intersections with detailed overlap entries.

Break Point and Break Segment do not consume boundary entities. They operate on user-selected points projected onto the target curve. Their robustness comes from `Arc2D.ContainsAngle(...)` and the curve adapters, not from overlap-boundary injection.

Extend must remain direction-aware. For same-support boundaries, it should not blindly use `Overlap` as a target. The current implementation instead augments candidates with finite boundary endpoints when the boundary is collinear or cocircular and then lets the existing direction filter choose the valid candidate.

## Consolidation notes

A small amount of duplicated point-on-segment logic still exists between low-level geometry helpers and CAD-level overlap collection. This is acceptable for the current stabilization because the implementations are private, small and covered by focused tests. A future cleanup may extract a shared primitive predicate, but it should not change command behavior.

Remaining incremental work should be explicit and tested:

```text
- full tangent classification in IntersectDetailed(...);
- richer overlap/coincidence records if a command needs more than boundary points;
- ellipse/elliptical-arc overlap semantics only when there is a native, non-sampled representation;
- spline overlap semantics only after a deliberate spline-intersection design.
```
