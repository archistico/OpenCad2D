# Known Limitations

OpenCad2D is still an early prototype. The following limitations should remain visible until they are resolved.

---

## Native save vs export

`.opencad2d.json` is the native save format. DXF/SVG/PDF are export/interchange formats and may not preserve every OpenCad2D-specific concept.

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
- full DXF `ELLIPSE` entities import as native `EllipseEntity`; partial DXF ellipses import as open polyline approximations until a native ellipse-arc entity exists;
- readable DXF `SPLINE` control points import as `BezierSplineEntity`; fit-point-only splines import as polyline approximations; full external NURBS knot/weight evaluation is not implemented yet;
- broad compatibility should be re-checked periodically with recorded LibreCAD, QCAD and Autodesk viewer versions.

---

## Dimensions

Dimensions are currently non-associative. Editing measured geometry does not automatically recompute existing dimension values.

The v0.8 line mitigates this by marking dimensions as potentially stale after geometry-changing commands, including deletion of model geometry. True associative dimensions remain future work.

---


## Snapping

Intersection snaps for `EllipseEntity` and `BezierSplineEntity` are supported through sampled polyline approximations. This is practical for the current interactive workflow but is not yet an exact analytic/NURBS intersection solver.

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
