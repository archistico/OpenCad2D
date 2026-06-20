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
- general editable `HATCH`, `IMAGE` and `LEADER` workflows are not supported yet; export has a limited `SOLID` HATCH path for filled circles and closed polylines;
- native DXF `DIMENSION` import/export remains future work; current OpenCad2D dimensions export as graphical primitives;
- custom DXF `LTYPE` generation for arbitrary line format dash patterns is future work;
- `LWPOLYLINE` bulge import/export now preserves compound mixed polyline topology through `PolylineEntity.SegmentBulges`; automated regression covers group-code `42` output and OpenCad2D round-trip, while broader viewer compatibility still needs a recorded LibreCAD/QCAD/Autodesk pass;
- full DXF `ELLIPSE` entities import as native `EllipseEntity`; edited partial ellipses are represented internally as `EllipticalArcEntity`, while DXF partial-ellipse import still needs a dedicated native importer pass if required for v0.9;
- readable DXF `SPLINE` control points import as `BezierSplineEntity`; fit-point-only splines import as approximations; full external NURBS knot/weight evaluation is not implemented yet;
- broad compatibility should be checked and recorded with exact LibreCAD/QCAD/Autodesk viewer versions before v0.9 release.

---

## Offset and polylines

Straight polylines are offset as editable polylines. Mixed/bulged polylines are currently offset conservatively by approximating the curved source segments into a linear polyline result. The source entity is not flattened or modified.

Current limitations:

- mixed-polyline offset does not yet preserve arc/bulge segments in the offset result;
- analytic arc-aware offset for bulged polylines is deferred;
- complex self-intersections or very tight offsets may still require user cleanup, especially on dense approximated curves.

---

## Solid fill

Solid fill currently supports only:

- Circle;
- closed Polyline, including rectangles and polygons represented as closed polylines.

Current intentional limits:

- no transparency;
- no hatch/pattern selection;
- no per-entity fill color;
- no fill for arcs, ellipses, elliptical arcs, splines, text or dimensions;
- DXF fill export is limited to generated `SOLID` HATCH records for the supported closed entities.

`Boundary Fill` creates a filled closed polyline from a picked seed point and uses the v2 preview/confirm workflow with sampled curve boundaries, editable small-gap tolerance and ignored-entity diagnostics. It still generates a single filled `PolylineEntity`, so holes/islands, hatch patterns, associative hatch behavior, block-reference boundary expansion and advanced self-intersection repair remain deferred until a real `HatchEntity` or a dedicated later boundary engine milestone.

---

## Dimensions

Dimensions are currently non-associative. Editing measured geometry does not automatically recompute existing dimension entities.

OpenCad2D mitigates this by marking dimensions as potentially stale after geometry-changing operations, including deletion of model geometry. True associative dimensions remain future work.

---

## Snapping and intersections

The editing pipeline now has native line/polyline/circle/arc intersection support for ellipses, elliptical arcs and mixed polyline segments in the important TRIM/BREAK/EXTEND paths.

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
- curved-end `EXTEND` for bulged polyline endpoints is intentionally deferred; current behavior refuses the operation instead of flattening the arc segment;
- additional command paths may adopt `CadIntersectionPoint` incrementally when useful;
- `CadIntersectionKind.Tangent` exists but is not yet a general detailed-intersection classification contract; use tangent snap behavior separately until explicit classifier tests are added;
- coincident full circles deliberately produce no synthetic intersection points because they have no finite boundary cut.

---

## Offset

Offset has a stabilized v0.9 workflow for distance input, target selection, side selection and preview. Its supported target set is intentionally explicit:

- Line;
- Circle;
- Arc;
- open/closed Polyline. Straight polylines are offset directly; bulged mixed polylines are offset by conservative linear approximation.

Bulged mixed polylines are now accepted by Offset through a conservative approximation pass: curved bulge segments are sampled into straight segments before the offset is generated. This makes the command usable without silently pretending to create a mathematically exact circular-arc offset.

Current intentional limitations:

- ellipse and elliptical arc offsets are deferred because a true offset is not another exact ellipse;
- Bezier spline offsets are deferred because a true offset is not another exact Bezier spline;
- unsupported advanced curves return a clear message and create no geometry;
- no silent permanent `PolylineEntity` approximation is created for ellipse, elliptical arc or spline offset;
- true arc-aware offsets that preserve bulge segments, rounded joins, configurable join styles and advanced self-intersection cleanup remain future work.

---

## Property Panel

The Property Panel supports many primary properties and now exposes editable `Segment N bulge` rows for mixed polylines. A broader review pass is still useful for curve entities after the native curve-editing work:

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

## External raster image references

OpenCad2D supports PNG/JPG/JPEG attachment as external references. The drawing stores the image path and oriented rectangle geometry only; raster bytes are not embedded. On save, image paths are stored relative to the `.opencad2d.json` file whenever possible, so a drawing folder can be moved together with its image folder. `Collect Refs` can copy existing linked images into an `images/` folder beside the drawing and save portable relative references.

Current limitations:

- moving or renaming the image file outside the drawing folder still breaks the live raster preview, but the drawing now warns about missing references on open and they can be restored with Relink Missing, Replace Image, or the editable File property;
- `Collect Refs` skips missing image files; relink them first if they should be included in the portable drawing package;
- `Manage Refs` is currently a compact manager for raster image references only; it does not yet manage future external reference types such as DXF underlays or PDF underlays;
- Reset Aspect depends on stored pixel metadata; very old/corrupt image references without pixel dimensions cannot infer the natural aspect ratio;
- SVG export writes an external `<image href="...">` link;
- DXF and PDF export do not yet emit raster image content.

## Fillet / Chamfer

Fillet and Chamfer now support standalone lines, adjacent straight segments of the same linear polyline, and terminal segments of separate open linear polylines. The separate-polyline case supports multi-segment polylines only when the picked segment is terminal. Internal segment trims are still rejected conservatively because moving an internal vertex would require a topology-aware local rewrite of adjacent segments.

Current limitations:

- Fillet/Chamfer on curved or bulged polyline segments is not supported yet;
- Fillet/Chamfer between separate polylines only edits terminal segments;
- Chamfer still uses a single equal distance on both selected branches;
- Chamfer does not yet have a NoTrim mode.

## DXF mixed-polyline consolidation

Automated tests now cover mixed-polyline DXF group-code `42` export and OpenCad2D export/import round-trip. Manual external validation is still required before a release claim: use current LibreCAD and QCAD builds at minimum, and record exact versions in `docs/dxf-compatibility.md`.
