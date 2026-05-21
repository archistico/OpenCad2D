# Curve editing architecture

This document defines the precision and topology rules for OpenCad2D curve-editing operations. It is the binding reference for Trim, Break, Extend and future curve-editing work.

The goal is to keep OpenCad2D precise enough for CAD work while still allowing pragmatic internal algorithms for preview, hit testing and coarse search.

---

## Core principle

CAD editing operations modify native entities using native geometric parameters.

Sampling is allowed only as temporary support. Sampled points must not become the permanent source of edited geometry unless no native representation exists yet and the limitation is explicitly documented.

When the same intersection is used to modify more than one explicit-vertex entity, the intersection point must be calculated once and reused as the same logical `Point2D` value for all resulting endpoints or vertices. This avoids micro-gaps and nearly coincident vertices after reciprocal Trim, Extend, Break or future Join-style operations.

---

## Tool snap policy for modify workflows

Modify tools that select entities or entity sides must not automatically inherit geometric point snaps.

`TRIM` is an entity/side selection workflow:

```text
Select cutting edge
Select entity side to trim
```

Therefore the active snap set for `TrimTool` is `SnapKind.EntityOnly` in every phase. Endpoint, midpoint, center, quadrant, intersection, nearest, perpendicular, tangent and grid snaps are intentionally disabled while Trim is active. This avoids misleading markers and prevents vertex snaps from suggesting that the command is asking for a geometric point when it is actually asking for an entity portion.

Trim preview must use the same native interval-selection pipeline as the final operation. The kept replacement geometry is exposed as normal preview geometry, while the interval that will be removed is exposed as a removal highlight and rendered dashed. This avoids a mismatch where the UI shows an approximate or line-only removal preview but the command commits a different native curve fragment.

Tools that explicitly ask for construction points, such as drawing tools or Break point selection, may continue to use the normal geometric snap modes.


---

## Why this matters

A CAD operation is not stable enough if two entities only appear to meet visually.

Bad result:

```text
Line A endpoint = (10.000000000001, 5.000000000000)
Line B endpoint = (9.999999999999, 5.000000000002)
```

Even if the distance is below tolerance, this creates downstream risk for snaps, dimensions, DXF export, offset, trim chaining and later editing.

Expected result for explicit-vertex entities:

```text
Line A endpoint = P
Line B endpoint = P
```

where `P` is the one shared intersection point produced by the intersection pipeline.

Tolerance is used to classify and validate geometry. It must not be used as an excuse to leave intended shared endpoints as different points.

---

## Shared intersection data

Intersection code should move toward returning rich intersection records, not just raw points.

Recommended model:

```csharp
public readonly record struct CadIntersectionPoint(
    Point2D Point,
    double FirstParameter,
    double SecondParameter,
    IntersectionKind Kind);
```

`Point` is the shared geometric point. `FirstParameter` and `SecondParameter` are native curve parameters on the two intersecting entities. `Kind` should distinguish at least crossing, tangent, overlap and endpoint-style cases when the implementation needs that information.

For operations acting on a single target curve, use a target-local cut model:

```csharp
public readonly record struct CurveCut(
    double Parameter,
    Point2D Point);
```

The parameter is the authoritative location on the target curve. The point is the shared coordinate to reuse for explicit-vertex output and to validate parametric output.

---

## Native parameter rule

Every editable curve type must be represented internally by an adapter exposing a stable curve parameter.

Recommended adapter shape:

```csharp
public interface ICurveAdapter
{
    CadEntity Source { get; }

    bool IsClosed { get; }

    double StartParameter { get; }

    double EndParameter { get; }

    double Period { get; }

    Point2D PointAt(double parameter);

    double ProjectPointToParameter(Point2D point, double tolerance);

    IReadOnlyList<CadEntity> BuildFragments(
        IReadOnlyList<CurveInterval> intervalsToKeep);
}
```

Recommended adapters:

```text
LineEntity              -> LineCurveAdapter
CircleEntity            -> CircleCurveAdapter
ArcEntity               -> ArcCurveAdapter
PolylineEntity          -> PolylineCurveAdapter
EllipseEntity           -> EllipseCurveAdapter
EllipticalArcEntity     -> EllipticalArcCurveAdapter
BezierSplineEntity      -> BezierSplineCurveAdapter
```

The command layer should not contain per-entity splitting algorithms. It should ask the adapter-backed split service for the fragments to keep.

---

## Curve intervals

Use curve intervals to describe which parts of a curve survive an edit.

```csharp
public readonly record struct CurveInterval(
    double StartParameter,
    double EndParameter);
```

For open curves, intervals stay between `StartParameter` and `EndParameter`.

For closed curves, wrap-around can be represented by allowing `EndParameter` to exceed the period-normalized range.

Example on a circle:

```text
p1 = 300 degrees
p2 = 40 degrees
interval from p1 to p2 in positive curve direction = 300 degrees -> 400 degrees
```

This avoids a separate wrap flag in the first implementation and keeps interval ordering deterministic.

---

## CadCurveSplitService

`CadCurveSplitService` should be the shared geometric engine for Trim, Break and later related modify tools.

Recommended responsibilities:

```text
- create a curve adapter for the target entity;
- normalize, sort and deduplicate curve cuts;
- reject cuts too close to endpoints when they only create degenerate fragments;
- build open or closed curve intervals;
- select the interval to remove when a pick point is provided;
- ask the adapter to build native fragments for the intervals that remain.
```

Recommended API direction:

```csharp
public sealed class CadCurveSplitService
{
    public IReadOnlyList<CadEntity> SplitAt(
        CadEntity entity,
        CurveCut cut,
        double tolerance);

    public IReadOnlyList<CadEntity> RemoveBetween(
        CadEntity entity,
        CurveCut firstCut,
        CurveCut secondCut,
        double tolerance);

    public IReadOnlyList<CadEntity> RemovePickedInterval(
        CadEntity entity,
        IReadOnlyList<CurveCut> cuts,
        Point2D pickPoint,
        double tolerance);
}
```

Command meaning:

```text
Break at point       = SplitAt
Break between points = RemoveBetween
Trim                 = RemovePickedInterval after boundary intersections
```

---

## Explicit-vertex vs parametric output

Different entities must use the shared cut point differently.

### Explicit-vertex entities

These should reuse `CurveCut.Point` directly as the resulting endpoint or inserted vertex:

```text
LineEntity
PolylineEntity
Rectangle output converted to open PolylineEntity when broken or trimmed
Polygon output converted to open PolylineEntity when broken or trimmed
```

This allows exact value equality for endpoints that should meet after reciprocal operations.

### Parametric entities

These should rebuild themselves from native parameters:

```text
CircleEntity -> ArcEntity fragments
ArcEntity -> ArcEntity fragments
EllipseEntity -> EllipticalArcEntity fragments
EllipticalArcEntity -> EllipticalArcEntity fragments
BezierSplineEntity -> BezierSplineEntity fragments where possible
```

For parametric entities, `CurveCut.Parameter` defines the new curve limit. `CurveCut.Point` is used to compute/refine the parameter and to validate that the rebuilt endpoint is within tolerance of the shared point.

Do not force an arbitrary point into a circle, arc, ellipse or spline if doing so would make the native curve mathematically inconsistent.

---



## Native ellipse/polyline intersection pass

The first native ellipse editing pass supports exact line-segment intersections for:

```text
LineEntity <-> EllipseEntity
LineEntity <-> EllipticalArcEntity
PolylineEntity <-> EllipseEntity
PolylineEntity <-> EllipticalArcEntity
CircleEntity <-> EllipseEntity
CircleEntity <-> EllipticalArcEntity
ArcEntity <-> EllipseEntity
ArcEntity <-> EllipticalArcEntity
```

For polyline boundaries, each polyline segment is intersected analytically against the ellipse equation in local ellipse coordinates. The resulting points are then filtered by segment range and, for `EllipticalArcEntity`, by the arc sweep.

For circle/arc boundaries, the intersection is found on the native ellipse parameter by solving the circle distance equation along the ellipse. The resulting point is produced from the ellipse parameter and validated against the circle radius. Circular arcs additionally filter by their own angular sweep. This prevents the ellipse side and the circle side from producing different sampled cut points.

This keeps Trim and Break results native while avoiding permanent sampled-polyline geometry. Sampling may still exist in other workflows such as preview, broad-phase search, snapping, or unsupported curve-pair intersections.

---

## EllipticalArcEntity native ellipse-arc foundation

`EllipticalArcEntity` is the native entity used for partial ellipse results. It represents an ellipse segment by preserving the same mathematical definition as `EllipseEntity` plus a directed parameter interval.

Native definition:

```text
Center
MajorAxis
MinorRadius
StartParameterRadians
EndParameterRadians
IsCounterClockwise
```

This model has replaced the former temporary editing fallback that converted ellipse trim/break results into `PolylineEntity` approximations. The current native behavior is:

```text
EllipseEntity -> EllipticalArcEntity fragments
EllipticalArcEntity -> EllipticalArcEntity fragments
```

Rendering, persistence and export support are available for `EllipticalArcEntity`, so TRIM/BREAK may safely create native ellipse-arc fragments without producing unsavable or undisplayable entities.

## Trim rule

Trim should be expressed as:

```text
1. collect supported boundary intersections against the target entity;
2. convert target-side intersections to CurveCut values;
3. find the interval containing the user's pick point;
4. remove that interval;
5. rebuild the remaining intervals as native entities.
```

This replaces per-entity Trim branches with one interval-based pipeline.

For multi-boundary Trim, all valid cuts are collected first. Circle and Arc targets must not exit early simply because more than one boundary was selected.

Closed entities such as circles, ellipses and closed polylines require deterministic interval handling. The pick point decides the interval removed by Trim.

---

## Break rule

Break should be expressed as:

```text
Break at point:
    project the picked point onto the target curve;
    create one CurveCut;
    split the curve at that cut.

Break between points:
    project both picked points onto the target curve;
    create two CurveCut values;
    remove the interval between those cuts.
```

For closed curves, the order of the two break points determines the interval removed. The preview should highlight the interval that will be removed before commit.

For rectangles and regular polygons, editing treats them as closed polylines. Once broken or trimmed open, the result is a `PolylineEntity`, not a semantic rectangle/polygon object.

---

## Current and target entity behavior

| Entity | Current practical behavior | Target behavior |
|---|---|---|
| Line | native fragments | native `LineEntity` fragments using shared cut points |
| Rectangle | closed polyline style editing | open `PolylineEntity` fragments after break/trim |
| Circle | native Trim/Break Segment support, including multi-boundary Trim | `ArcEntity` fragments from native angular parameters |
| Arc | native Trim/Break support, including multi-boundary Trim | `ArcEntity` fragments from native sweep parameters |
| Polyline | native polyline fragments | native `PolylineEntity` fragments using inserted shared cut vertices |
| Polygon | stored as closed `PolylineEntity` | open `PolylineEntity` fragments after break/trim |
| Ellipse | native Trim and Break Between Points are implemented | `EllipticalArcEntity` fragments from native ellipse parameters |
| Spline | open Bezier Trim/Break returns native fragments; closed splines deferred | `BezierSplineEntity` fragments where the current Bezier model allows native splitting |

---

## Sampling policy

Sampling may be used for:

```text
- rendering previews;
- hit testing;
- coarse nearest-point search;
- coarse intersection discovery for curves without exact solver support;
- temporary visual highlighting.
```

Sampling must not be the final source of edited geometry when a native representation exists or is being introduced.

Current remaining exceptions are limited and explicit:

```text
Closed BezierSplineEntity editing remains deferred/no-op.
One-point Break on full closed circles and full closed ellipses remains deferred until a full-sweep open-arc convention is defined.
Offset still needs a dedicated native-geometry preservation review.
```

---

## Precision tests required

The implementation should include tests that verify both entity type preservation and geometric/topological correctness.

Required examples:

```text
TrimTwoLinesMutually_ShouldShareExactEndpoint
TrimLineWithArc_ShouldUseSharedIntersectionPointForLineEndpoint
TrimArcWithLine_ShouldKeepArcEndpointOnCircleAndNearSharedPoint
BreakLineAtPoint_ShouldCreateTwoLinesSharingTheSameCutPoint
BreakPolylineAtIntersection_ShouldInsertSharedIntersectionVertex
TrimCircleWithMultipleBoundaries_ShouldCreateNativeArcFragments
TrimArcWithMultipleBoundaries_ShouldCreateNativeArcFragments
BreakClosedPolylineBetweenPoints_ShouldCreateOpenPolylineWithoutMicroGap
TrimEllipseWithLine_ShouldCreateEllipticalArcFragment
BreakBezierSplineAtPoint_ShouldCreateBezierSplineFragments
```

For explicit-vertex entities that should meet at the same intersection, tests should assert exact coordinate equality of the resulting values, not only distance within tolerance.

For parametric entities, tests should assert:

```text
- the result remains native;
- center/radius/axis/control data remain consistent where applicable;
- the rebuilt endpoint lies on the native curve;
- the rebuilt endpoint is within tolerance of the shared intersection point.
```

---

## Implementation status

The first native curve-editing stabilization pass is implemented for the current supported entity set.

Completed foundation:

- `CurveCut`, `CurveInterval`, `ICurveAdapter`, `ICurveAdapterFactory` and `CadCurveSplitService`;
- adapters for Line, Circle, Arc, Polyline, Ellipse, EllipticalArc and open BezierSpline;
- `CadIntersectionPoint` with shared point, native parameters, kind and ready-to-use `CurveCut` values;
- command-level Trim and Break delegation to the shared split service;
- native `EllipticalArcEntity` with rendering, persistence, snapping and export support;
- `BezierSplineSplitService` for open Bezier splitting with De Casteljau;
- cleanup of obsolete permanent-polyline fallbacks for supported native curves;
- Preview UX semantics for removal and addition intervals.

Current edit results:

```text
LineEntity                 -> LineEntity fragments
CircleEntity               -> ArcEntity fragments
ArcEntity                  -> ArcEntity fragments
PolylineEntity             -> PolylineEntity fragments
Rectangle/Polygon          -> PolylineEntity fragments because they are polyline-based contours
EllipseEntity              -> EllipticalArcEntity fragments
EllipticalArcEntity        -> EllipticalArcEntity fragments
Open BezierSplineEntity    -> BezierSplineEntity fragments
Closed BezierSplineEntity  -> deferred / no fragments
```

## Remaining non-goals / deferred work

The first pass should not attempt to implement all advanced CAD editing modes.

Deferred:

```text
- Trim Fence, Crossing, Project, Edge, Erase modes;
- full external NURBS knot/weight editing;
- associative dimensions;
- polyline arc-segment/bulge compound topology preservation;
- major spatial-index or rendering rewrites.
```

The immediate target is precise and predictable native editing for the current entity set.

---

## Implementation status

### Started: shared split pipeline for line, circle and arc

The first implementation phase introduces a shared native curve splitting pipeline under `OpenCad2D.Core.Editing.Curves`.

Implemented types:

- `CurveCut`
- `CurveInterval`
- `ICurveAdapter`
- `ICurveAdapterFactory`
- `DefaultCurveAdapterFactory`
- `CadCurveSplitService`

Initial adapters:

- `LineEntity`
- `CircleEntity`
- `ArcEntity`

Initial integration:

- `CadTrimService` uses `CadCurveSplitService` for `CircleEntity` and `ArcEntity` trim operations.
- Multi-boundary trim for circles and arcs no longer exits early before attempting the trim.

This initial status is retained as implementation history. Current behavior has since expanded to polyline, ellipse and elliptical-arc adapters; splines remain the next native-fragment target.

---

## Implemented foundation status

The first implementation pass introduced the shared curve editing namespace `OpenCad2D.Core.Editing.Curves` with:

```text
CurveCut
CurveInterval
ICurveAdapter
ICurveAdapterFactory
DefaultCurveAdapterFactory
CadCurveSplitService
```

Currently implemented adapters:

```text
LineEntity
CircleEntity
ArcEntity
PolylineEntity
```

`PolylineEntity` uses cumulative path length as its parameter. For closed polylines and polygons, the total length is also the period, allowing wrap-around intervals such as `25 -> 45` on a square with perimeter `40`.

Polyline fragments must preserve intermediate vertices. For example, trimming the last vertical segment of this polyline:

```text
(0,0) -> (10,0) -> (10,10)
```

at `(10,5)` should keep:

```text
(0,0) -> (10,0) -> (10,5)
```

not two disconnected fragments:

```text
(0,0) -> (10,0)
(10,0) -> (10,5)
```

This is important because explicit-vertex geometry should preserve both topology and user-authored vertices unless an edit actually removes them.

---

## Implemented foundation status - Break service delegation

`CadBreakService` now uses `CadCurveSplitService` for the native base entity set:

```text
LineEntity
CircleEntity
ArcEntity
PolylineEntity
```

Current behavior:

- `BreakAtPoint` delegates to the shared split pipeline for `LineEntity`, `ArcEntity` and `PolylineEntity`.
- `BreakBetweenPoints` delegates to the shared split pipeline for `LineEntity`, `CircleEntity`, `ArcEntity` and `PolylineEntity`.
- one-point break on a full `CircleEntity` intentionally remains a no-op for now, because the stable circle workflow is two-point break producing a native `ArcEntity` without introducing a near-360-degree arc edge case.
- `EllipseEntity` uses `EllipticalArcEntity` for Break Between Points;
- open `BezierSplineEntity` uses native Bezier split fragments through `BezierSplineSplitService`;
- closed splines remain intentionally deferred rather than falling back to permanent polylines.

This keeps Break and Trim aligned on the same interval model for the entities that already have native adapters. It also ensures explicit-vertex outputs such as lines and polylines reuse the projected/shared cut point directly as the resulting endpoint or vertex.


---

## Current implementation status: native elliptical arcs

The shared curve editing pipeline now includes native adapters for:

```text
EllipseEntity
EllipticalArcEntity
```

This means that Trim and Break Between Points can produce `EllipticalArcEntity` fragments instead of permanently converting full ellipses into `PolylineEntity` approximations.

Current policy:

```text
Ellipse Trim                 -> EllipticalArcEntity fragments
Ellipse Break Between Points -> EllipticalArcEntity fragment
EllipticalArc Trim           -> EllipticalArcEntity fragments
EllipticalArc Break          -> EllipticalArcEntity fragments
```

One-point Break on a full closed ellipse remains intentionally deferred, like one-point Break on a full circle, because representing a true open 360-degree conic arc requires an explicit full-sweep convention. Until that convention is added, the command must not create a degraded polyline fallback.

Intersections involving ellipses and elliptical arcs may still use sampled segments for discovery. The permanent edit result, however, is now reconstructed from the native ellipse parameter and kept as `EllipticalArcEntity`.



## Elliptical arc consolidation tests

The native ellipse phase is considered consolidated only when tests verify both type preservation and geometric precision. Current focused tests cover:

```text
TrimEllipse_WithTwoLineBoundaries_ShouldKeepNativeEllipticalArcFragments
TrimEllipse_WithTwoLineBoundaries_ShouldUseGeometricCutEndpoints
TrimEllipticalArc_ByLineBoundary_ShouldKeepNativeEndpointOnBoundaryAndEllipse
BreakEllipticalArc_AtPoint_ShouldCreateTwoNativeFragmentsSharingBreakPoint
BreakEllipticalArc_BetweenPoints_ShouldRemoveMiddleSegmentAndKeepNativeFragments
```

These tests assert that edited ellipse results:

- remain `EllipticalArcEntity`;
- never silently fall back to `PolylineEntity`;
- preserve center, major axis and minor radius;
- produce endpoints on the original ellipse;
- place Trim endpoints on the intended boundary lines within the editing tolerance.


## Implemented foundation status - Bezier spline split service

`BezierSplineSplitService` is the first native spline-editing building block. It preserves the current `BezierSplineEntity` representation by splitting the control polygon with De Casteljau rather than converting the edited result to `PolylineEntity`.

Current behavior:

```text
SplitAt(open spline, t)        -> two BezierSplineEntity fragments
ExtractInterval(open spline)   -> one BezierSplineEntity interval
RemoveInterval(open spline)    -> zero, one or two BezierSplineEntity outer fragments
Closed spline split            -> intentionally deferred / no fragments
```

`BezierSplineCurveAdapter` now connects open `BezierSplineEntity` to `CadCurveSplitService` for native parametric splitting. Closed splines remain intentionally deferred.


## Implemented foundation status - Bezier spline curve adapter

`BezierSplineCurveAdapter` adapts open `BezierSplineEntity` to the shared curve split pipeline.

Current behavior:

```text
SplitAtPoint(open spline)        -> native BezierSplineEntity fragments
RemoveBetweenPoints(open spline) -> native BezierSplineEntity outer fragments
RemovePickedInterval(open spline)-> native BezierSplineEntity fragments
Closed spline                    -> intentionally deferred / no fragments
```

Projection from a point to a spline parameter uses a sampled search plus local numeric refinement. This projection is allowed as a parameter-discovery step, but the resulting fragments are rebuilt through `BezierSplineSplitService` and remain native Bezier geometry.

TRIM/BREAK command-level fallback removal for supported open splines has been completed. Remaining unsupported cases must return no result or a documented explicit fallback; they must not silently create permanent sampled polylines.

## Implemented command status - native Bezier spline Trim/Break

Open `BezierSplineEntity` is now connected to the shared curve-editing pipeline at command level.

Current behavior:

```text
TRIM open BezierSplineEntity          -> BezierSplineEntity fragments
BREAK AT POINT open BezierSplineEntity -> BezierSplineEntity fragments
BREAK BETWEEN open BezierSplineEntity  -> BezierSplineEntity outer fragments
Closed BezierSplineEntity              -> intentionally deferred / no fragments
```

The previous command-level fallback that converted supported open spline Trim/Break results to `PolylineEntity` has been removed. Spline intersections may still be discovered through existing approximate intersection paths, but the selected cut is projected back to the native Bezier parameter and fragments are rebuilt through `BezierSplineSplitService`.

This keeps the project aligned with the curve-editing rule: sampling may assist discovery/projection, but it must not become the permanent edited geometry when a native representation is available.

## Implemented foundation: rich intersection points

The project now has an additive rich-intersection layer:

```csharp
public readonly record struct CadIntersectionPoint(
    Point2D Point,
    double FirstParameter,
    double SecondParameter,
    CadIntersectionKind Kind)
{
    public CurveCut FirstCut => new(FirstParameter, Point);
    public CurveCut SecondCut => new(SecondParameter, Point);
}
```

The existing `CadEntityIntersectionService.Intersect(...)` remains available and returns only `Point2D` values. New code can use:

```csharp
CadEntityIntersectionService.IntersectDetailed(first, second, tolerance)
```

when it needs the shared point plus native parameters on both entities.

Initial implementation policy:

- `IntersectDetailed(...)` is additive and compatibility-preserving;
- it uses the existing intersection calculation to get the shared geometric point;
- it then projects that point through `ICurveAdapter` for both entities;
- explicit-vertex entities can reuse `CadIntersectionPoint.Point` directly;
- parametric entities should use the corresponding parameter and validate the rebuilt endpoint against `Point`.

The next implementation step should progressively feed `CadIntersectionPoint.FirstCut` and `SecondCut` into TRIM/BREAK/EXTEND when those commands operate on two known entities. This will reduce repeated point projection and further protect against micro-gaps.

## EXTEND native curve model phase

`CadExtendService` now begins moving onto the same native curve-editing model used by TRIM and BREAK.

Implemented in this phase:

```text
EllipticalArcEntity target -> EllipticalArcEntity result
```

A full temporary `EllipseEntity` is used only to discover candidate intersections with the selected boundary. The resulting extension point is then converted back to the native ellipse parameter of the `EllipticalArcEntity`, and the extended result is rebuilt as an `EllipticalArcEntity`.

This preserves the core editing rule:

```text
EXTEND must not convert native ellipse arcs into PolylineEntity fragments.
```

Current EXTEND target policy:

```text
LineEntity            supported
ArcEntity             supported
PolylineEntity open   supported
EllipticalArcEntity   supported
CircleEntity          not supported as target
EllipseEntity         not supported as target
Closed Polyline       not supported as target
BezierSplineEntity    deferred
```

For explicit-vertex entities such as lines and open polylines, the selected extension endpoint is replaced by the shared geometric intersection point. For parametric entities such as circular arcs and elliptical arcs, the extension endpoint is rebuilt from the native curve parameter.

## EXTEND and native elliptical boundaries

`EXTEND` follows the same precision policy as `TRIM` and `BREAK`: the candidate boundary intersection must come from the native geometry whenever the target or boundary supports it.

The extend pipeline now treats full ellipses and elliptical arcs as native boundary sources for line-like extension:

- a line extended to an ellipse uses an analytic infinite-line/ellipse intersection;
- an open polyline endpoint extended to an elliptical arc uses the same analytic infinite-line/ellipse intersection and filters the result on the elliptical-arc sweep;
- a circular arc extended to an ellipse uses the native circle/ellipse intersection path.

This prevents the extended endpoint from landing on a sampled polyline approximation of the ellipse. For explicit-vertex targets such as `LineEntity` and open `PolylineEntity`, the chosen intersection point becomes the actual endpoint/vertex. For parametric targets such as `ArcEntity`, the selected point is converted back into the native angular parameter and the resulting endpoint is validated against the boundary.



## Permanent Polyline fallback cleanup

The command-level legacy fallback code paths that created permanent `PolylineEntity` fragments for native curve editing have been removed from `CadTrimService` and `CadBreakService`. The services now delegate supported entities to `CadCurveSplitService` and the matching `ICurveAdapter`.

Current cleanup result:

```text
LineEntity                 -> native line fragments
CircleEntity               -> ArcEntity fragments
ArcEntity                  -> ArcEntity fragments
PolylineEntity             -> PolylineEntity fragments
EllipseEntity              -> EllipticalArcEntity fragments
EllipticalArcEntity        -> EllipticalArcEntity fragments
Open BezierSplineEntity    -> BezierSplineEntity fragments
Closed BezierSplineEntity  -> deferred / no fragments
```

`PolylineEntity` remains a correct result only when the source geometry is itself polyline-based, for example open polylines, rectangles and polygons represented as closed polylines. It is no longer the hidden permanent fallback for ellipse or supported open spline TRIM/BREAK operations.

Sampling is still allowed for preview, snapping, broad-phase discovery or unsupported import cases. When a native edited representation exists, sampled geometry must be projected/refined back to the native parameter before the command creates the final entity.

### BREAK preview policy

Break Segment previews must distinguish the geometry that will remain from the interval that will be removed. Remaining fragments may be shown as regular transient entities, but the removed interval must be exposed as `ToolPreviewHighlightKind.Removal` so UI renderers can draw it as a dashed removal preview.

The removed preview interval must be produced by the shared curve-splitting pipeline, not by a separate sampled approximation, so preview and final geometry stay consistent for lines, arcs, circles, polylines, ellipses, elliptical arcs and supported open Bezier splines.


## Preview consistency rule

Preview geometry for Trim, Break and Extend must be generated from the same native curve-editing pipeline used by the final command.

- Removed intervals are represented with `ToolPreviewHighlightKind.Removal`.
- Added extension intervals are represented with `ToolPreviewHighlightKind.Addition`.
- The full replacement entity may be previewed normally, but the operation-specific interval must be highlighted separately.

This prevents misleading previews where a sampled or visually approximate interval differs from the entity that will be committed to the document.


## Command-line UX for native curve editing

The command line must reinforce the same geometric rule used by the editing services: the preview is not decorative, it describes the interval that will be removed or added by the next click.

- TRIM messages should refer to the picked side and the dashed removed portion.
- BREAK Segment messages should refer to the first and second break points, with the dashed interval being the removed portion.
- EXTEND messages should refer to the endpoint side and the highlighted added portion.

This is especially important for circles, ellipses, closed polylines and polygons because two valid intervals may exist between two points. The user must be guided by both the preview and the command text.

---

## Implemented command status - granular failure messages

Curve-editing commands must not fail silently when the geometric service cannot produce an edit result.

The current command layer now distinguishes the most common failure categories for manual regression work:

- `TRIM`: no intersection with the active cutting edge versus an existing intersection that does not produce a removable interval from the picked side;
- `BREAKPOINT`: point outside the selected entity versus point too close to an endpoint, vertex or unstable split location;
- `BREAK`: coincident break points, second point outside the selected entity, closed-curve segment removal limitations and spline limitations;
- `EXTEND`: projected target does not intersect the boundary versus the boundary being reachable only from the opposite endpoint side.

These messages are produced at command level by `EditingStatusMessageBuilder`, leaving the low-level geometry services focused on returning edited entities or an empty result. This keeps the regression checklist easier to execute because a failed operation now indicates whether the problem is user input, unsupported topology or a real geometric bug.
