# Latest handoff note

## v0.9 curve editing and preview UX checkpoint

The curve-editing stabilization block is now substantially complete for the current supported native entity set. `CadCurveSplitService`, `ICurveAdapter`, `CurveCut`, `CurveInterval` and the richer `CadIntersectionPoint` model are in place. TRIM and BREAK now preserve native geometry for lines, circles, arcs, polylines, full ellipses, elliptical arcs and open Bezier splines. Permanent command-level fallback from ellipses or supported open splines to `PolylineEntity` has been removed.

Current native edit behavior:

- `LineEntity` -> native line fragments, with shared cut points reused as explicit endpoints;
- `CircleEntity` -> native `ArcEntity` fragments for Trim and Break Segment;
- `ArcEntity` -> native `ArcEntity` fragments;
- `PolylineEntity`, including rectangles/polygons represented as closed polylines -> `PolylineEntity` fragments;
- `EllipseEntity` -> native `EllipticalArcEntity` fragments for Trim and Break Segment;
- `EllipticalArcEntity` -> native `EllipticalArcEntity` fragments;
- open `BezierSplineEntity` -> native `BezierSplineEntity` fragments through De Casteljau splitting;
- closed Bezier splines remain intentionally deferred/no-op for edit splitting.

EXTEND now participates in the same native-geometry direction for supported targets and boundaries, including elliptical arc targets and native ellipse/elliptical-arc boundary intersections. Full closed curves such as full circles, full ellipses and closed polylines are not extend targets.

Preview UX is aligned with the edit semantics:

- TRIM uses entity-only snapping and previews the removed interval as a dashed removal highlight;
- BREAK Segment previews the removed interval as a dashed removal highlight;
- EXTEND previews the added interval as an addition highlight;
- command messages explain removed/added highlighted intervals and closed-curve constraints.

Save/Export UX has also been clarified: export creates derived files and does not update `CurrentFilePath` or clear dirty state. Status messages now tell the user that the editable native OpenCad2D project still needs to be saved when appropriate.

Remaining curve-editing work is now focused rather than foundational: richer intersection adoption inside more command paths, closed spline policy, Offset cleanup, additional preview polish and broader release-candidate validation.

---

## Preview UX: Extend added interval is highlighted as addition

`ExtendTool` now marks the highlighted extension segment with `ToolPreviewHighlightKind.Addition` instead of the generic emphasis highlight. This keeps preview semantics explicit:

- `Addition` = geometry that will be added by Extend.
- `Removal` = geometry that will be removed by Trim/Break.
- `Emphasis` = generic highlighted transient geometry.

`CadToolPreviewRenderer` renders addition highlights with a dedicated solid green pen, while removal highlights remain red and dashed. The full extended replacement entity is still drawn as a normal preview entity; the added portion only is highlighted with the addition style.


---

## Preview UX: Trim removed interval is dashed

`TrimTool` now previews the exact native interval that will be removed, not only the kept replacement geometry. The removed interval is obtained through `CadTrimService.GetRemovedIntervalByBoundaries`, which delegates to `CadCurveSplitService.GetPickedInterval` and therefore uses the same cut collection, native curve adapters and interval selection rules as the final Trim operation.

`ToolPreviewDescriptor` now carries `HighlightedEntityKind`. `TrimTool` marks highlighted entities as `ToolPreviewHighlightKind.Removal`, while existing modify previews such as Extend keep the default emphasis style. `CadToolPreviewRenderer` renders removal highlights with a red dashed pen, making the part to be cut visually distinct from added/modified preview geometry.


---

## Preview UX: Trim uses entity-only snaps

`TrimTool` now implements `ISnapModeProvider` and returns `SnapKind.EntityOnly` for all Trim phases. Trim is an entity/side selection workflow, so geometric snaps such as endpoint, midpoint, center, quadrant, intersection, nearest, perpendicular, tangent and grid are disabled while the command is active. This keeps the UI from showing vertex/point snap markers during Trim and makes the active selection intent clearer.

`TrimTool` also recognizes `EllipticalArcEntity` as a supported cutting edge/target in the tool-level support check, matching the native curve-editing services.


---

## Curve editing: EXTEND with native elliptical boundaries

`CadEntityIntersectionService.IntersectInfiniteLineWithEntity` now supports `EllipseEntity` and `EllipticalArcEntity` directly. It computes infinite-line/ellipse intersections analytically and filters points against the elliptical-arc sweep when needed. `IntersectCircleWithEntity` also routes `EllipseEntity` and `EllipticalArcEntity` through the native circle/ellipse intersection helpers.

This improves `CadExtendService` for these supported scenarios:

- `LineEntity` extended to an `EllipseEntity` boundary;
- open `PolylineEntity` endpoint extended to an `EllipticalArcEntity` boundary;
- `ArcEntity` extended to an `EllipseEntity` boundary.

The added tests assert that the resulting endpoints remain on the native ellipse/elliptical arc and do not depend on sampled fallback geometry.


---

## Curve editing: native Bezier spline Trim/Break

Open `BezierSplineEntity` is now connected to `CadCurveSplitService` at command level. `CadTrimService` and `CadBreakService` no longer convert supported open spline Trim/Break results to `PolylineEntity`; they return native `BezierSplineEntity` fragments through `BezierSplineCurveAdapter` and `BezierSplineSplitService`. Closed spline editing remains intentionally deferred/no-op. Intersection discovery can still use the existing approximation path, but the cut is projected back to the Bezier parameter before fragment creation.

# Latest handoff note

## Curve editing: BezierSplineSplitService foundation

`BezierSplineSplitService` has been introduced as the first native spline-preservation step. It uses De Casteljau subdivision on open `BezierSplineEntity` control polygons so spline editing can eventually return native `BezierSplineEntity` fragments instead of permanent `PolylineEntity` approximations.

Current scope:

- `SplitAt(spline, t)` returns two native open Bezier spline fragments sharing the exact De Casteljau break point;
- `ExtractInterval(spline, t0, t1)` returns the native Bezier interval between two parameters;
- `RemoveInterval(spline, t0, t1)` returns the two native outer fragments around a removed interval;
- closed spline splitting is intentionally deferred and currently returns no fragments.

Added tests in `BezierSplineSplitServiceTests` verify native output, shared split points, endpoint correctness for extracted intervals, outer fragment creation, metadata preservation, endpoint no-op behavior and closed-spline deferral.

This phase does not yet connect splines to `ICurveAdapter`, TRIM or BREAK. The next planned phase is `BezierSplineCurveAdapter`, followed by native spline Trim/Break.


---

# Latest handoff note

## Curve editing: native ellipse/polyline intersection consolidation

`CadEntityIntersectionService` now handles `PolylineEntity` against `EllipseEntity` and `EllipticalArcEntity` with analytic line-segment/ellipse intersections per polyline segment. This complements the existing native `LineEntity` support and avoids relying on sampled ellipse segments for Trim boundaries made from polylines.

Added precision tests covering direct polyline/ellipse intersections, polyline/elliptical-arc sweep filtering, and Trim results that remain native `EllipticalArcEntity` fragments with geometric endpoints on the source ellipse.

Remaining curve-editing priorities:
1. BezierSplineCurveAdapter.
2. Native spline Trim/Break.
3. Richer `CadIntersectionPoint` records.
4. EXTEND on the same native curve model.
5. Cleanup of remaining permanent polyline fallbacks.
6. Preview UX.

---

# Latest handoff note

## Curve editing: native ellipse/elliptical arc Trim and Break foundation

`CadCurveSplitService` now has adapters for `EllipseEntity` and `EllipticalArcEntity`. Trim on full ellipses and existing elliptical arcs can now return native `EllipticalArcEntity` fragments instead of permanent `PolylineEntity` approximations. Break Between Points on full ellipses and Break on existing elliptical arcs also route through the shared split pipeline.

One-point Break on a full closed ellipse remains a deliberate no-op, matching the current full-circle behavior, until a safe full-sweep/open-closed conic arc convention is introduced. Intersections may still be discovered through sampled segments, but the edited geometry is rebuilt from native ellipse parameters.

Added focused tests to verify that Trim/Break on ellipse workflows do not create `PolylineEntity` results.

---

﻿# Latest handoff note

## v0.8.5 stabilization: delete marks dimensions stale

Deleting model geometry now marks remaining non-associative dimensions as stale, matching the existing behavior used by modify, replace and transform commands. `DeleteEntitiesCommand` captures the previous dimension stale state before deletion, marks dimensions stale only when model geometry is removed, and restores the captured state on undo. Deleting dimension annotations alone does not mark other dimensions stale.

Added focused core tests for delete-driven stale marking, undo restoration and the dimension-only deletion case.

---

# Latest handoff note

## v0.8.5 DXF SPLINE import

Implemented first-pass DXF `SPLINE` import. Readable control-point splines are imported as editable `BezierSplineEntity` instances, preserving the OpenCad2D Bezier workflow and enabling round-trips for OpenCad2D-exported SPLINE entities. Closed spline flags are respected. Fit-point-only SPLINE entities are imported as `PolylineEntity` approximations with an informational diagnostic, because OpenCad2D does not yet evaluate external NURBS knot vectors or rational weights. Added focused importer tests for open control-point splines, closed splines, fit-point-only fallback and malformed point data.

Remaining DXF spline limitation: full external NURBS fidelity is still future work.

---

# Latest handoff note

## v0.8.x final documentation and release consolidation

The v0.8.x baseline is now ready for final local validation and GitHub release preparation. Polygon, Ellipse, MTEXT and Bezier Spline are complete in the current baseline, including command aliases, rendering/preview, persistence, export coverage and focused tests.

Important implementation notes for future work:

- regular polygons are stored as closed `PolylineEntity` instances;
- ellipse partial edit results currently become open `PolylineEntity` approximations because there is no `EllipseArcEntity`;
- Bezier spline Trim/Break/Offset workflows use sampled polyline approximation, so edited fragments currently become `PolylineEntity` results;
- DXF import supports `MTEXT`, full `ELLIPSE` entities and first-pass `SPLINE` control-point import;
- release notes are consolidated in `docs/release-v0.8.md`, with a GitHub-ready draft in `docs/release-v0.8-final.md`.

Recommended final validation before publishing:

```bash
dotnet build OpenCad2D.sln
dotnet test OpenCad2D.sln --no-build
```

---

# Latest handoff note

## Mirror tool

Implemented `MirrorTool` as a command-driven modify tool before the v0.9 roadmap. The workflow is:

```text
MIRROR: Select objects to mirror:
MIRROR: Specify first point of mirror line:
MIRROR: Specify second point of mirror line:
MIRROR: Delete source objects? [Yes/No] <No>:
```

The tool supports preselection or select-first workflow, typed coordinates for the two mirror-axis points, a live mirrored preview while choosing the second axis point, and the final `Yes`/`No` option. Empty Enter defaults to `No`, so the source entities are kept and mirrored copies are added. `Yes` mirrors the selected source entities in place through `MirrorEntitiesCommand`. The UI has a `Mirror` button in the Modify/Edit group and command aliases are `MIRROR` and `MI`.

Roadmap status: dimension export, Mirror, Polygon, Ellipse, multiline text, Spline and final documentation/release cleanup are complete for the v0.8.x baseline.

---

# Latest handoff note

## Dimension export stabilization

PDF export now supports all current dimension entities as graphical primitives plus text:

- horizontal and vertical linear dimensions;
- aligned dimensions;
- radius and diameter dimensions;
- angular dimensions with segmented arc output.

SVG and DXF dimension coverage already existed and remains based on graphical primitives. PDF export now mirrors that approach and includes tests for each dimension type. PDF text escaping now writes WinAnsi octal escapes for non-ASCII dimension symbols such as degree (`°`) and diameter (`Ø`), and the PDF font resource declares `/Encoding /WinAnsiEncoding`.

Roadmap status before v0.9: Mirror, Polygon, Ellipse, multiline text, Spline and final v0.8.x documentation/release cleanup are complete.

---

# Latest handoff note

## Startup template stabilization

The app now starts from a clean native template instead of seeding a sample drawing in `MainWindowViewModel`.

Implemented changes:

- `MainWindow.axaml` opens maximized by default through `WindowState="Maximized"`.
- `MainWindowViewModel` calls `LoadDefaultTemplate()` during construction and in `NewDocument()`.
- The template is `src/OpenCad2D.App/Templates/default.opencad2d.json`.
- `OpenCad2D.App.csproj` copies `Templates/**` to the output directory.
- The default template contains line formats, text formats, one dimension style and the default CAD layers, with no entities.
- If the template cannot be loaded, the view-model falls back to an internal empty document with the built-in layers.
- `MainWindowViewModelDefaultDrawingTests` now verifies that startup is empty instead of expecting the old sample drawing.


---

## Curve editing stabilization - Break service delegation phase

`CadBreakService` now delegates base native entities to `CadCurveSplitService` instead of maintaining separate break fragmentation logic for the same cases.

Delegated cases:

- `BreakAtPoint`: `LineEntity`, `ArcEntity`, `PolylineEntity`
- `BreakBetweenPoints`: `LineEntity`, `CircleEntity`, `ArcEntity`, `PolylineEntity`

The intentional exception remains one-point break on a full `CircleEntity`, which still returns no fragments. The supported circle workflow is `BreakBetweenPoints`, returning a native `ArcEntity` complement. This avoids introducing a near-360-degree arc representation before that behavior is deliberately designed.

Temporary fallback cases kept unchanged:

- `EllipseEntity` still returns open `PolylineEntity` approximations.
- `BezierSplineEntity` still breaks its polyline approximation.

New tests added to lock the shared point/projection behavior through `CadBreakService`:

- `CadBreakServiceTests.BreakAtPoint_WithLine_ShouldCreateTwoLinesSharingProjectedPoint`
- `CadBreakServiceTests.BreakBetweenPoints_WithLine_ShouldRemoveMiddleSegmentUsingProjectedPoints`


---

## Curve editing stabilization - Line trim delegation phase

`CadTrimService.TrimLineByBoundaries` now delegates line target fragmentation to `CadCurveSplitService`.

This removes the remaining separate line-specific trim fragmentation path for the native base entities and aligns `LineEntity` with the same `CurveCut` / `CurveInterval` pipeline already used by `CircleEntity`, `ArcEntity`, `PolylineEntity`, and `CadBreakService`.

Important precision rule preserved by this phase:

- line fragments use the projected/shared `CurveCut.Point` directly as their resulting endpoint;
- mutual line trims therefore produce exactly matching endpoint coordinates when they come from the same geometric intersection;
- tolerances are still used to classify cuts, filter endpoints, and remove degenerate fragments, but not to justify keeping intended coincident vertices as different coordinates.

New tests added:

- `CadTrimServiceTests.TrimLine_ByBoundary_ShouldReuseSharedIntersectionPointAsEndpoint`
- `CadTrimServiceTests.TrimTwoLinesMutually_ShouldCreateExactlyMatchingSharedEndpoint`


---

## Curve editing stabilization - EllipticalArcEntity foundation phase

Added the first native model object required to remove permanent ellipse degradation during future Trim/Break operations:

- `EllipticalArcEntity`
- `EntityKind.EllipticalArc`

The entity uses the same native ellipse definition as `EllipseEntity`:

- `Center`
- `MajorAxis`
- `MinorRadius`
- `StartParameterRadians`
- `EndParameterRadians`
- `IsCounterClockwise`

Superseded status: this phase was foundational only at the time it was written. The later phases have since added rendering, persistence, export support, `EllipseCurveAdapter`, `EllipticalArcCurveAdapter`, and TRIM/BREAK wiring for native ellipse fragments. The former `EllipseEntity -> PolylineEntity` editing fallback has been removed for supported ellipse edits.

Core tests added:

- `EllipticalArcEntityTests.Constructor_ShouldPreserveNativeEllipseDefinitionAndParameters`
- `EllipticalArcEntityTests.GetSamplePoints_ShouldFollowDirectedSweepAndIncludeEndpoints`
- `EllipticalArcEntityTests.WithLayer_ShouldPreserveGeometry`


---

## Curve editing stabilization - EllipticalArc infrastructure phase

`EllipticalArcEntity` is now wired into the application infrastructure so future native ellipse Trim/Break results can be displayed, saved and exported before the command services start producing them.

Added support:

- screen rendering in `CadEntityRenderer` using the entity's directed sample points;
- JSON persistence with `EllipticalArcEntityDto` and the `EllipticalArc` type discriminator;
- serializer/deserializer mapping in `JsonDocumentSerializer`;
- SVG export as a native `<path>` elliptical arc command;
- DXF export as a partial `ELLIPSE` entity using group codes `41` and `42` for the start/end parameters;
- PDF export using the current sampled-line strategy, matching the existing ellipse/spline export approach.

New tests added:

- `EllipticalArcRoundTripTests.SerializeDeserialize_ShouldPreserveEllipticalArcEntity`
- `EllipticalArcRoundTripTests.JsonRoundTrip_ShouldPreserveEllipticalArcDtoType`
- `SvgExporterTests.Export_WhenDocumentContainsEllipticalArc_ShouldWritePathElement`
- `DxfExporterTests.Export_WhenDocumentContainsEllipticalArc_ShouldWritePartialEllipseEntity`

Important limitation that still remains by design:

- Superseded: `CadTrimService` and `CadBreakService` now return `EllipticalArcEntity` for supported ellipse editing. The former `EllipseEntity -> PolylineEntity` editing fallback has been removed.



---

## Curve editing stabilization - EllipticalArc consolidation tests

Added focused precision tests for native ellipse editing results. The new tests verify that Trim and Break on `EllipseEntity` / `EllipticalArcEntity` keep native geometry rather than returning permanent `PolylineEntity` approximations.

New test file:

- `tests/OpenCad2D.Core.Tests/EllipticalArcEditingPrecisionTests.cs`

Covered scenarios:

- full ellipse Trim with two line boundaries returns native `EllipticalArcEntity` fragments;
- full ellipse Trim endpoints lie on both the source ellipse and the vertical line boundaries;
- `EllipticalArcEntity` Trim by line keeps native endpoint geometry;
- `EllipticalArcEntity` Break At Point creates two native fragments sharing the break point within tolerance;
- `EllipticalArcEntity` Break Between Points removes the middle segment while preserving center, major axis and minor radius.

This phase does not add new spline behavior. The next planned phase remains improving native/non-degrading intersections for `EllipseEntity` and `EllipticalArcEntity` with `LineEntity` and `PolylineEntity`, followed by `BezierSplineSplitService`.


---

## Curve editing stabilization - Ellipse/Circle shared intersections

Improved native intersection handling for ellipse-based entities against circular entities. The goal is to avoid the manual issue where trimming an ellipse with a circle produced endpoints that were visibly separated from the circle by a large amount because the operation fell back to independent sampled approximations.

Updated behavior:

- `CircleEntity <-> EllipseEntity` now uses a native ellipse-parameter root search against the circle equation;
- `CircleEntity <-> EllipticalArcEntity` uses the same native calculation and filters results by the elliptical arc sweep;
- `ArcEntity <-> EllipseEntity` filters native circle/ellipse intersections by the circular arc sweep;
- `ArcEntity <-> EllipticalArcEntity` filters by both the circular arc sweep and the elliptical arc sweep.

The returned point is produced from the ellipse parameter and validated against the circle radius. This gives both the ellipse adapter and the circle/arc adapter the same shared geometric cut point, preventing large mismatches after Trim.

New precision tests were added to `EllipticalArcEditingPrecisionTests` for:

- `IntersectCircleEllipse_ShouldReturnPointsOnBothNativeCurves`;
- `TrimEllipse_ByCircleBoundary_ShouldKeepEndpointsOnCircleAndEllipse`;
- `TrimCircle_ByEllipseBoundary_ShouldKeepEndpointsOnCircleAndEllipse`.



## 2026-05-20 - BezierSplineCurveAdapter foundation

The shared curve editing pipeline now has a `BezierSplineCurveAdapter` for open `BezierSplineEntity`.

Implemented behavior:

- `DefaultCurveAdapterFactory` creates a spline adapter for `BezierSplineEntity`;
- open spline split/remove operations use native Bezier parameters `0..1`;
- fragments are rebuilt through `BezierSplineSplitService`, preserving `BezierSplineEntity`;
- closed splines remain deliberately deferred and return no fragments;
- new `CadCurveSplitServiceTests` verify native split, remove-between and remove-picked behavior for splines.

Important limitation:

- Superseded: TRIM/BREAK services now route supported open spline editing through `CadCurveSplitService` and `BezierSplineSplitService`; the command-level permanent `PolylineEntity` fallback for supported open splines has been removed.

## 2026-05-20 - CadIntersectionPoint rich intersection foundation

Added the first implementation layer for richer CAD intersections, without replacing the existing `Intersect(...)` API yet.

New types:

- `CadIntersectionPoint` in `OpenCad2D.Core.Editing`;
- `CadIntersectionKind` in `OpenCad2D.Core.Editing`.

`CadIntersectionPoint` stores:

- one shared `Point2D` that explicit-vertex entities can reuse directly;
- `FirstParameter` on the first entity;
- `SecondParameter` on the second entity;
- an intersection kind classification;
- convenience `FirstCut` and `SecondCut` values for the shared split pipeline.

`CadEntityIntersectionService.IntersectDetailed(...)` now wraps the existing intersection points and projects them onto both entities through `DefaultCurveAdapterFactory`. This preserves compatibility while giving future TRIM/BREAK/EXTEND work access to native curve parameters and a single shared point.

New tests in `CadEntityIntersectionDetailedTests` verify:

- line/line intersections return a single shared point plus native parameters;
- endpoint intersections are classified as `Endpoint`;
- line/circle intersections expose both line parameters and circle angular parameters;
- circle/ellipse intersections reuse the same point for both `CurveCut` values and keep points on both native curves.

This phase is intentionally additive. The existing `CadTrimService` and `CadBreakService` still call their current APIs. The next refactor step can progressively replace target-side point projection with `CadIntersectionPoint.FirstCut` / `SecondCut` where the command already knows which entity is the target.


## Curve editing stabilization - EXTEND native elliptical arc phase

Extended `CadExtendService` to support `EllipticalArcEntity` targets.

Behavior added:

```text
EllipticalArcEntity + boundary -> EllipticalArcEntity
```

The service discovers candidate intersections using the full ellipse definition, chooses the nearest valid intersection outside the current elliptical-arc sweep in the picked extension direction, converts that point to the native ellipse parameter, and rebuilds the result as an `EllipticalArcEntity`.

`ExtendTool` now accepts `EllipticalArcEntity` as a target and creates highlighted preview fragments for the newly added elliptical-arc portion.

Regression tests added:

```text
ExtendEllipticalArc_ToLineBoundary_ShouldExtendPickedEndWithNativeGeometry
ExtendEllipticalArc_ToLineBoundary_ShouldExtendPickedStartWithNativeGeometry
ExtendEllipse_ShouldReturnNull
```

Full `EllipseEntity` targets remain unsupported for EXTEND because closed curves have no natural extension endpoint. `BezierSplineEntity` EXTEND remains deferred.


## 2026-05-20 - Permanent Polyline fallback cleanup for curve editing

Cleaned up the old command-level TRIM/BREAK fallback implementations that permanently converted native curve edits into `PolylineEntity` results.

Changed files:

```text
src/OpenCad2D.Core/Editing/CadTrimService.cs
src/OpenCad2D.Core/Editing/CadBreakService.cs
docs/curve-editing.md
docs/ai-handoff.md
```

`CadTrimService` is now a thin dispatcher over the shared curve-editing pipeline for supported targets: line, circle, arc, ellipse, elliptical arc, polyline and open Bezier spline. It collects boundary intersections, filters endpoints where needed, and delegates final interval removal to `CadCurveSplitService`. The obsolete private fragment builders for line, circle, arc, ellipse-as-polyline and polyline-as-two-point-fragments were removed.

`CadBreakService` is now similarly reduced to the public Break At Point / Break Between Points dispatch. It delegates supported open/native entities to `CadCurveSplitService`; full `CircleEntity` and `EllipseEntity` still intentionally return no result for one-point break until a full-sweep-open-arc policy is defined.

Current policy after cleanup:

- source polylines, rectangles and polygons may legitimately produce `PolylineEntity` fragments;
- full ellipses and elliptical arcs produce `EllipticalArcEntity` fragments;
- supported open Bezier splines produce `BezierSplineEntity` fragments;
- unsupported closed splines are deferred/no-op rather than silently degraded;
- sampled geometry remains allowed only for preview/discovery/projection, not as the permanent edited result when a native representation exists.


## 2026-05-20 - Save versus Export UX clarity

Clarified the UX distinction between native Save and external Export.

Changed files:

```text
src/OpenCad2D.App/ViewModels/MainWindowViewModel.cs
tests/OpenCad2D.App.Tests/MainWindowViewModelExportSaveSemanticsTests.cs
docs/export.md
docs/architecture.md
docs/ai-handoff.md
```

`ExportSvgToFile`, `ExportDxfToFile` and `ExportPdfToFile` still do not update `CurrentFilePath`, do not call `MarkSaved()` and do not clear the dirty state. This remains the correct data-integrity policy because SVG/PDF/DXF are derived outputs, not the editable native OpenCad2D project.

The post-export status message now explicitly says that the export did not save the editable OpenCad2D project. It distinguishes three cases:

- no native file path yet: tell the user to use Save As;
- native file exists but the drawing is dirty: tell the user unsaved project changes remain and to use Save;
- native file exists and the drawing is clean: tell the user the native drawing is already saved.

Added App tests proving SVG/DXF/PDF export do not change `CurrentFilePath` and do not clear `IsDirty`, plus coverage for the never-saved and already-saved message variants.


## Latest update - BREAK removal preview

Added a native BREAK Segment removal preview path. `CadCurveSplitService` now exposes `GetIntervalBetweenPoints`, `CadBreakService` exposes `GetRemovedSegmentBetweenPoints`, and `BreakBetweenPointsTool` implements `IToolPreviewDescriptorProvider`. During second-point preview, the tool returns regular preview entities for the remaining fragments and a `Removal` highlight for the interval that will be cut away. The app renderer already draws `Removal` highlights with the dashed red preview pen introduced for TRIM.

### 2026-05-20 - Preview UX command-line clarification

The Preview UX pass now aligns command-line messages with the visual preview semantics:

- TRIM reports that the dashed portion will be removed.
- BREAK Segment reports that the dashed portion between break points will be removed.
- EXTEND reports that the highlighted portion will be added.
- BREAK Segment explicitly warns that for closed curves the order of picked points defines which interval is removed.
- EXTEND tool boundary support now matches the native intersection work: line, circle, arc, ellipse, elliptical arc and polyline boundaries are accepted. EXTEND targets remain endpoint-based only: line, arc, elliptical arc and open polyline.

Keep this distinction in future UX work: complete closed curves can be boundaries, but they are not EXTEND targets unless a separate open-at-point policy is introduced.
