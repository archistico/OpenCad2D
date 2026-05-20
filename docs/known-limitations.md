# Known Limitations

OpenCad2D is in the v0.9 stabilization track before the first stable v1.0 release. The following limitations should remain visible until they are resolved.

---

## Native save vs export

`.opencad2d.json` is the native save format. DXF/SVG/PDF are export/interchange formats and may not preserve every OpenCad2D-specific concept.

---

## Local application/session settings

Document-level drafting settings such as grid, snap, Ortho, Polar Tracking and current drawing settings are persisted in `.opencad2d.json`.

A first local application/session settings layer now stores last opened file metadata, recent native drawing files and last open/save/export directories outside the drawing file.

Still deferred: window size/position, panel widths, theme preference and shortcut persistence. These should remain outside `.opencad2d.json` and should only be added if they do not make startup fragile.

---

## DXF

Current DXF support targets practical AutoCAD 2000 ASCII 2D interoperability.

Implemented import coverage now includes core 2D entities, layer tables, `TEXT`, `MTEXT`, `LWPOLYLINE` straight segments and bulge arcs, full `ELLIPSE` entities and readable `SPLINE` control/fit point data.

Known limits:

- binary DXF is not supported;
- DWG is intentionally not supported;
- `BLOCK` / `INSERT` are not supported yet;
- `HATCH`, `IMAGE` and `LEADER` are not supported yet;
- native DXF `DIMENSION` import/export remains future work; current OpenCad2D dimensions export as graphical primitives;
- custom DXF `LTYPE` generation for arbitrary line format dash patterns is future work;
- `LWPOLYLINE` bulge import preserves curved geometry as separate native line/arc entities, but does not preserve the original compound polyline topology;
- full DXF `ELLIPSE` entities import as native `EllipseEntity`; native `EllipticalArcEntity` exists for edited partial ellipses, while DXF partial-ellipse import may still require a dedicated native importer pass;
- readable DXF `SPLINE` control points import as `BezierSplineEntity`; fit-point-only splines import as polyline approximations; full external NURBS knot/weight evaluation is not implemented yet;
- broad compatibility should be re-checked periodically with recorded LibreCAD, QCAD and Autodesk viewer versions.

---

## Dimensions

Dimensions are currently non-associative. Editing measured geometry does not automatically recompute existing dimension values.

The v0.8 line mitigates this by marking dimensions as potentially stale after geometry-changing commands, including deletion of model geometry. True associative dimensions remain future work.

---

## Snapping

Intersection snaps for `BezierSplineEntity` may still use sampled polyline approximations. `EllipseEntity` and `EllipticalArcEntity` now have native line/polyline and circle/arc intersection support in the editing pipeline, but some snapping and unsupported curve-pair helpers may still use sampled/coarse paths.

---


## Curve editing precision

Trim and Break now use the shared native curve-editing pipeline for the current supported entity set. The main command-level permanent-polyline fallbacks for ellipses and supported open Bezier splines have been removed.

Current supported results:

- line Trim/Break returns native `LineEntity` fragments and reuses shared cut points for explicit endpoints;
- circle Trim/Break Segment returns native `ArcEntity` fragments;
- arc Trim/Break returns native `ArcEntity` fragments;
- polyline, rectangle and polygon editing returns `PolylineEntity` fragments because those sources are polyline-based;
- ellipse Trim and Break Between Points return native `EllipticalArcEntity` fragments;
- existing `EllipticalArcEntity` Trim/Break returns native `EllipticalArcEntity` fragments;
- open Bezier spline Trim/Break returns native `BezierSplineEntity` fragments.

Known remaining limits:

- one-point Break on full closed circles and full closed ellipses remains deferred until a safe full-sweep open-arc convention is defined;
- closed Bezier spline editing remains deferred/no-op;
- Offset still needs a dedicated native-geometry preservation review, especially for spline workflows;
- sampled approximations may still be used for preview, snapping, broad-phase discovery or unsupported import cases, but supported edit commands should project/refine cuts back to native parameters before creating final geometry.

Target rule:

```text
CAD editing operations modify native entities using native geometric parameters.
Shared intersections used by multiple explicit-vertex entities must reuse the same cut point to avoid micro-gaps.
```

---

## Offset

Offset supports lines, circles, arcs, straight-segment polylines and sampled Bezier splines.

Known limits:

- polyline offset uses miter joins with a conservative bevel fallback when the miter would become too long;
- user-selectable join styles such as Miter/Bevel/Round are not implemented yet;
- rounded joins are not implemented yet;
- imported DXF `LWPOLYLINE` bulge segments are converted to separate line/arc entities rather than preserved as compound polyline segments;
- advanced self-intersection cleanup is limited.

---

## Fillet

Fillet currently supports Line-Line only, with live preview, Radius and Trim/NoTrim modes.

Future work:

- Line-Arc;
- Arc-Arc;
- polyline fillet;
- repeated/multiple fillet workflows.

---

## Trim and Extend

Trim has an advanced base workflow with All and Undo, but advanced CAD modes such as Fence, Crossing, Project and Edge are not implemented yet.

Extend supports selected entity types only and intentionally does not extend closed entities like circles. Shift-click-to-extend inside Trim is future work.

---

## Command input

The command input supports coordinate and option workflows, command history navigation with `↑` / `↓`, and first-pass autocomplete with `Tab`.

Future improvements remain:

- richer autocomplete UI/dropdown;
- richer right-click behavior;
- additional option sets for advanced tools;
- full docked CAD console/history view.

---

## Export formats

SVG, DXF and PDF are implemented. PNG export is planned.
