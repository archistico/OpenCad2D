# OpenCad2D AI handoff

This file is the current technical handoff for continuing OpenCad2D work. It should be updated after each meaningful refactor or feature phase.

---

## Current focus

OpenCad2D is in the v0.9 stabilization cycle. The active goal is to make the existing 2D CAD foundation predictable, precise and release-ready before moving toward v1.0.

Current work area: Modify Tools UX cleanup and final v0.9 validation.

---

## Completed foundations

The following areas are considered stable enough to be summarized rather than repeated in full historical detail:

- core document/entity/layer/line-format model;
- Avalonia app shell, canvas, command row, snap/status UI and tool panels;
- native `.opencad2d.json` persistence;
- SVG/PDF/DXF export baseline and ASCII DXF import baseline;
- draw tools for lines, rectangles, circles, arcs, ellipses, polylines, polygons, text, MTEXT, points and open Bezier splines;
- dimensions baseline;
- selection, entity cycling, Select All, Select Last and Deselect;
- Text/MTEXT bounding-box hit testing;
- Save/Export UX clarity: export does not replace native save and does not clear dirty state.

---

## Native curve editing checkpoint

The curve-editing stabilization block is complete for the current supported native entity set.

Important types/services now in place:

- `CadCurveSplitService`;
- `ICurveAdapter` and adapter-backed curve splitting;
- `CurveCut` and `CurveInterval`;
- richer `CadIntersectionPoint` with shared point and native parameters;
- `EllipticalArcEntity`;
- `BezierSplineSplitService`.

Current behavior:

- `LineEntity` edits preserve native line fragments and reuse shared cut points as explicit endpoints;
- `CircleEntity` TRIM/BREAK Segment results become native `ArcEntity` fragments;
- `ArcEntity` remains native arc fragments;
- `PolylineEntity`, including rectangles/polygons represented as closed polylines, remains polyline geometry;
- `EllipseEntity` TRIM/BREAK Segment results become native `EllipticalArcEntity` fragments;
- `EllipticalArcEntity` remains native elliptical arc fragments;
- open `BezierSplineEntity` TRIM/BREAK results remain native Bezier spline fragments through De Casteljau splitting;
- closed Bezier spline editing is intentionally deferred/no-op.

The command-level permanent `PolylineEntity` fallback has been removed for supported ellipse and open-spline TRIM/BREAK operations.

---

## Preview UX checkpoint

Preview semantics are now explicit:

- `Removal`: geometry that will be removed, used by TRIM and BREAK Segment, rendered dashed;
- `Addition`: geometry that will be added, used by EXTEND and OFFSET previews;
- `Emphasis`: selected boundary/target/first entity overlays.

TRIM uses entity-only snap. TRIM, EXTEND and FILLET keep selected boundaries/first entities visibly highlighted.

---

## Modify Tools UX checkpoint

Established UX policy:

- left click provides graphical input or entity selection;
- right click confirms/advances the current phase when a valid default, value or selection exists;
- Enter is equivalent to right click for command prompts;
- Esc cancels the current phase;
- entity-selection phases use `SnapKind.EntityOnly`;
- point-input phases use geometric snaps.

Completed Modify UX work:

- Deselect command/button and aliases `DESELECT`, `CLEARSELECTION`, `CS`;
- Point icon simplified to a small cross;
- Delete tool deletes existing selection immediately, or multi-picks entities and confirms with Enter/right click;
- Rotate and Scale can initiate entity selection when no selection is active;
- TRIM/EXTEND/FILLET selected boundary/first-entity highlights;
- FILLET entity selection uses EntityOnly snap;
- POLYLINE can finish with right click when enough vertices exist;
- Polygon sides, Fillet radius and Mirror delete-source prompts accept right click/Enter defaults;
- Ellipse axis input uses snap-resolved points;
- Rect Sides numeric second-side input creates the exact typed side length;
- Offset workflow accepts typed distance, two picked distance points, stored last distance and right-click/Enter default confirmation.

---

## Offset checkpoint

Offset is stabilized for the v0.9 scope.

Supported targets:

- `LineEntity`;
- `CircleEntity`;
- `ArcEntity`;
- straight-segment open/closed `PolylineEntity`.

Deferred targets:

- `EllipseEntity` and `EllipticalArcEntity`: a true offset is not another exact ellipse;
- `BezierSplineEntity`: a true offset is not another exact Bezier spline.

The previous spline offset path that silently produced a sampled `PolylineEntity` approximation has been removed. Unsupported advanced curves return a clear deferred-support message and create no geometry.

Offset workflow:

1. specify distance by typed value, two clicks, or right click/Enter with a previous distance;
2. select target with EntityOnly snap;
3. choose side graphically;
4. preview is shown as an Addition highlight;
5. after creating an offset, the tool returns to target selection and keeps the same distance.

---

## Current next work

1. Final UX consistency pass across remaining draw/modify tools:
   - right-click/Enter/Esc behavior;
   - snap mode by phase;
   - command messages;
   - preview semantics.
2. Complete/execute `docs/testing/curve-editing-regression-v0.9.md`.
3. Export/import compatibility pass for SVG/PDF/DXF.
4. Property Panel curve review.
5. Release preparation for v0.9.

---

## Intentional deferred items

- closed Bezier spline editing;
- Break Point on complete circles/ellipses until a full-sweep open-arc convention is defined;
- true offset support for ellipses, elliptical arcs and Bezier splines;
- rounded/configurable Offset joins and advanced self-intersection cleanup;
- DXF partial ELLIPSE import mapping directly to `EllipticalArcEntity`, unless chosen for v0.9;
- broader adoption of `CadIntersectionPoint` in paths where it adds value;
- spatial indexing/performance pass for large drawings.
