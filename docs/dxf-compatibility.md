# DXF Compatibility — OpenCad2D v0.8.5

This document records the current DXF compatibility policy, the representative manual sample set, automated regression coverage and the external viewer validation log.

DXF compatibility has two different validation levels:

1. automated structure tests inside the OpenCad2D test suite;
2. manual external viewer checks with real CAD/viewer applications.

A round-trip generated and re-imported by OpenCad2D is useful, but it is not sufficient to claim broad DXF interoperability.

---

## Manual sample set

The current manual sample files are stored in:

```text
samples/dxf/compatibility/
```

| Sample | Purpose | Current validation state |
|---|---|---|
| `01_basic_lines_layers.dxf` | Lines, layers, linetypes and lineweights | Manual check passed; exact viewer/version not recorded |
| `02_text_mtext.dxf` | `TEXT` and `MTEXT`, including MTEXT line breaks and reference width | Manual check passed; exact viewer/version not recorded |
| `03_arcs_circles_ellipses.dxf` | `CIRCLE`, `ARC`, full `ELLIPSE` and partial `ELLIPSE` | Manual check passed; exact viewer/version not recorded |
| `04_polylines_polygons.dxf` | Open/closed `LWPOLYLINE`, mixed line/arc bulge segments and closed-polyline closing bulge | Manual check passed; exact viewer/version not recorded; needs a renewed v0.9 mixed-polyline pass |
| `05_dimensions_as_geometry.dxf` | Dimension-like graphics exported as primitive geometry | Manual check passed; exact viewer/version not recorded |
| `06_spline_bezier.dxf` | `SPLINE` with degree, knot vector and control points | Manual check passed; exact viewer/version not recorded |
| `07_mixed_drawing.dxf` | Mixed smoke drawing with the main supported entity families | Manual check passed; exact viewer/version not recorded |

Current note: the seven compatibility samples were manually checked and reported as opening correctly in external viewers. Exact external application names, versions/builds and operating systems were not recorded in this pass. For a stricter future compatibility audit, record the viewer name, version/build, operating system and date for each sample.

---

## Current manual result

The current sample set was manually checked after the S3/S8/S2 DXF work and reported as OK. Because the exact viewer versions were not recorded, this should be treated as a practical smoke pass, not as a fully reproducible compatibility certification. After the v0.9 mixed-polyline work, sample `04_polylines_polygons.dxf` must be regenerated or supplemented with a drawing that contains multiple bulges, negative bulges, closed-polyline closing bulges, JOIN-created mixed polylines and FILLET-created polyline bulges.

| Date | Sample set | Result | Notes |
|---|---|---|---|
| 2026-05-17 | `samples/dxf/compatibility/01` through `07` | Passed smoke check | External viewer check reported OK; exact applications and versions to be recorded in a future audit. |

## External viewer validation log

Use this table for every manual audit. Do not mark a sample as passed without recording the exact application version.

| Date | OS | Application | Version/build | Sample | Result | Notes |
|---|---|---|---|---|---|---|
| _pending_ | _pending_ | LibreCAD | _pending_ | `01_basic_lines_layers.dxf` | Pending | Validate line orientation, layer colors, linetypes and lineweights. |
| _pending_ | _pending_ | LibreCAD | _pending_ | `02_text_mtext.dxf` | Pending | Validate TEXT/MTEXT visibility and wrapping. |
| _pending_ | _pending_ | LibreCAD | _pending_ | `03_arcs_circles_ellipses.dxf` | Pending | Validate circle, arc, full ellipse and partial ellipse behavior. |
| _pending_ | _pending_ | LibreCAD | _pending_ | `04_polylines_polygons.dxf` | Pending | Validate closed polyline and bulge arc. |
| _pending_ | _pending_ | LibreCAD | _pending_ | `05_dimensions_as_geometry.dxf` | Pending | Validate dimension graphics as primitive geometry. |
| _pending_ | _pending_ | LibreCAD | _pending_ | `06_spline_bezier.dxf` | Pending | Validate spline rendering and control point interpretation. |
| _pending_ | _pending_ | LibreCAD | _pending_ | `07_mixed_drawing.dxf` | Pending | Validate mixed drawing smoke test. |
| _pending_ | _pending_ | QCAD | _pending_ | `01_basic_lines_layers.dxf` | Pending | Validate line orientation, layer colors, linetypes and lineweights. |
| _pending_ | _pending_ | QCAD | _pending_ | `02_text_mtext.dxf` | Pending | Validate TEXT/MTEXT visibility and wrapping. |
| _pending_ | _pending_ | QCAD | _pending_ | `03_arcs_circles_ellipses.dxf` | Pending | Validate circle, arc, full ellipse and partial ellipse behavior. |
| _pending_ | _pending_ | QCAD | _pending_ | `04_polylines_polygons.dxf` | Pending | Validate closed polyline and bulge arc. |
| _pending_ | _pending_ | QCAD | _pending_ | `05_dimensions_as_geometry.dxf` | Pending | Validate dimension graphics as primitive geometry. |
| _pending_ | _pending_ | QCAD | _pending_ | `06_spline_bezier.dxf` | Pending | Validate spline rendering and control point interpretation. |
| _pending_ | _pending_ | QCAD | _pending_ | `07_mixed_drawing.dxf` | Pending | Validate mixed drawing smoke test. |

Recommended optional viewers for later passes:

- Autodesk DWG TrueView;
- AutoCAD or AutoCAD LT, if available;
- FreeCAD DXF importer;
- browser-based DXF viewers only as secondary smoke checks, not as primary validation.

---

## Manual validation checklist

For each sample, check at least:

- the file opens without importer errors;
- the geometry is visible and not collapsed to the origin;
- the Y orientation matches the expected OpenCad2D visual orientation;
- layers are present with meaningful names;
- layer color, linetype and lineweight are reasonably preserved;
- text is readable;
- MTEXT wraps or breaks predictably;
- arcs, bulged polyline segments and ellipses are curved, not silently dropped;
- splines are visible and approximate the intended curve;
- primitive dimension graphics remain legible.

If a viewer opens the file but changes some visual details, record the result as `Partial`, not `Passed`.

---

## Export policy

| OpenCad2D entity | DXF export policy |
|---|---|
| Point | `POINT` |
| Line | `LINE` |
| Circle | `CIRCLE` |
| Arc | `ARC` |
| Polyline | `LWPOLYLINE`; mixed line/arc polylines write DXF group code `42` bulge values on the owning vertices |
| Polygon | closed `LWPOLYLINE` |
| Ellipse | `ELLIPSE` |
| Text | `TEXT` |
| Multiline text | `MTEXT` |
| Bezier spline | `SPLINE` with degree, knot vector and control points |
| Dimensions | drawable geometry, not native associative DXF `DIMENSION` |

---

## Import policy

| DXF entity | v0.8.5 import behavior |
|---|---|
| `LINE` | imported as `LineEntity` |
| `CIRCLE` | imported as `CircleEntity` |
| `ARC` | imported as `ArcEntity` |
| `LWPOLYLINE` without bulge | imported as `PolylineEntity` |
| `LWPOLYLINE` with bulge | imported as one `PolylineEntity` with `SegmentBulges`, preserving compound mixed-polyline topology |
| `TEXT` | imported as `TextEntity` |
| `MTEXT` | imported as `MultilineTextEntity` |
| full `ELLIPSE` | imported as `EllipseEntity` |
| partial `ELLIPSE` | imported as open `PolylineEntity` approximation |
| readable `SPLINE` control points | imported as `BezierSplineEntity` |
| fit-point-only `SPLINE` | imported as `PolylineEntity` approximation |

---

## Automated mixed-polyline regression coverage

The export/import test suite now protects the core DXF behavior used by mixed polylines:

- `DxfExportCompatibilityTests.Export_MixedPolyline_ShouldWriteBulgeGroupsOnOwningVertices` verifies that only non-zero bulges are emitted as group code `42` values and that closed flags remain correct.
- `DxfRoundTripTests.ExportThenImport_WithMixedPolylineBulges_ShouldPreserveCompoundPolylineTopology` verifies that a closed mixed polyline round-trips as one `PolylineEntity`, including positive, negative and zero bulges.
- `DxfDocumentImporterPolylineTests` covers open bulged polylines, mixed straight/bulged polylines and closed polylines whose closing segment has a bulge.

These automated tests do not replace external viewer validation. They ensure OpenCad2D keeps its own DXF contract stable before a manual LibreCAD/QCAD/Autodesk pass.

---

## Known DXF limitations

- Binary DXF is not supported.
- DWG is not supported.
- `BLOCK` / `INSERT` are not yet supported.
- `HATCH`, `IMAGE` and `LEADER` are not yet supported.
- Native associative DXF `DIMENSION` import/export is not yet implemented.
- Partial ellipses are approximated as polylines on import.
- Full NURBS spline fidelity is not guaranteed yet.
- External compatibility is viewer-dependent and must be validated with explicit viewer versions.
