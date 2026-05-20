# Known Limitations

OpenCad2D is in the v0.9 stabilization track before the first stable v1.0 release. The following limitations should remain visible until they are resolved.

---

## Native save vs export

`.opencad2d.json` is the native editable project format. DXF, SVG, PDF and PNG are derived export/interchange formats.

Export does not update the native current file path and does not clear dirty state. This is intentional: an exported file may not preserve every OpenCad2D-specific editing concept.

---

## Local application/session settings

Document-level drafting settings such as grid, snap, Ortho, Polar Tracking and current drawing settings are persisted in `.opencad2d.json`.

A first local settings layer stores last opened file metadata, recent native drawing files and last open/save/export directories outside the drawing file.

Still deferred:

- window size/position;
- panel widths;
- theme preference;
- shortcut persistence.

---

## DXF

Current DXF support targets practical AutoCAD 2000 ASCII 2D interoperability.

Known limits:

- binary DXF is not supported;
- DWG is intentionally not supported;
- `BLOCK` / `INSERT` are not supported yet;
- `HATCH`, `IMAGE` and `LEADER` are not supported yet;
- native DXF `DIMENSION` import/export remains future work; current OpenCad2D dimensions export as graphical primitives;
- custom DXF `LTYPE` generation for arbitrary line format dash patterns is future work;
- `LWPOLYLINE` bulge import preserves curved geometry as separate native line/arc entities, but does not preserve the original compound polyline topology;
- full DXF `ELLIPSE` entities import as native `EllipseEntity`; edited partial ellipses are represented internally as `EllipticalArcEntity`, while DXF partial-ellipse import still needs a dedicated native importer pass if required for v0.9;
- readable DXF `SPLINE` control points import as `BezierSplineEntity`; fit-point-only splines import as approximations; full external NURBS knot/weight evaluation is not implemented yet;
- broad compatibility should be checked and recorded with exact LibreCAD/QCAD/Autodesk viewer versions before v0.9 release.

---

## Dimensions

Dimensions are currently non-associative. Editing measured geometry does not automatically recompute existing dimension entities.

OpenCad2D mitigates this by marking dimensions as potentially stale after geometry-changing operations, including deletion of model geometry. True associative dimensions remain future work.

---

## Snapping and intersections

The editing pipeline now has native line/polyline/circle/arc intersection support for ellipses and elliptical arcs in the important TRIM/BREAK/EXTEND paths.

Remaining limitations:

- some snapping paths and unsupported curve-pair helpers may still use sampled/coarse discovery;
- Bezier spline intersections may still use sampled discovery followed by projection/refinement to native spline parameters where supported;
- dense drawings may require future performance review around snap and intersection queries.

---

## Curve editing

The former permanent-polyline fallback for supported ellipse and open-spline TRIM/BREAK operations has been removed.

Current intentional limitations:

- closed Bezier spline editing is deferred/no-op;
- Break Point on complete circles and complete ellipses is deferred until a full-sweep open-arc convention is defined;
- rectangles and polygons are represented as closed polylines for editing; once trimmed/broken open, the result is a `PolylineEntity`;
- additional command paths may adopt `CadIntersectionPoint` incrementally when useful.

---

## Offset

Offset is the main geometry/UX item still awaiting v0.9 stabilization.

Current limitations to resolve or document clearly:

- first-run distance/default behavior must be rebuilt around typed distance, two-point distance and last distance;
- right click/Enter default confirmation must be made consistent;
- target selection should use EntityOnly snap;
- supported native geometry should remain explicit;
- true offsets of ellipses, elliptical arcs and Bezier splines are not generally the same native curve type, so these require an explicit policy before being advertised as fully native support;
- sampled approximations must not be hidden as precise native results.

---

## Property Panel

The Property Panel supports many primary properties, but v0.9 still needs a review pass for curve entities after the native curve-editing work:

- Arc;
- Ellipse;
- EllipticalArc;
- Polyline;
- BezierSpline;
- Text;
- MTEXT.

---

## Performance

The project already has viewport culling and spatial-query foundations, but v0.9 still needs a smoke performance pass on:

- denser drawings;
- snap/hit-test behavior;
- multi-boundary TRIM/BREAK/EXTEND;
- preview performance;
- representative export time.

Major renderer or spatial-index rewrites are deferred unless testing reveals a concrete blocker.
