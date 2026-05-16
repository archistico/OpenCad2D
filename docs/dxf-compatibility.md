# DXF Compatibility — OpenCad2D v0.8

This document records the current DXF compatibility policy and the manual validation checklist for the v0.8 release.

## v0.8 manual validation status

The v0.8 compatibility samples in `samples/dxf/compatibility/` were opened successfully during release validation.

| Sample | Status | Notes |
|---|---|---|
| `01_basic_lines_layers.dxf` | Passed | Basic lines and layers validated. |
| `02_text_mtext.dxf` | Passed | TEXT and MTEXT opened successfully. |
| `03_arcs_circles_ellipses.dxf` | Passed | Circle, arc and ellipse geometry validated. |
| `04_polylines_polygons.dxf` | Passed | Open/closed polylines and polygon-like geometry validated. |
| `05_dimensions_as_geometry.dxf` | Passed | Dimension graphics opened as drawable geometry. |
| `06_spline_bezier.dxf` | Passed | Spline sample opened successfully. |
| `07_mixed_drawing.dxf` | Passed | Mixed v0.8 showcase opened successfully. |


Validation note: the sample set has been manually opened successfully for the v0.8 release gate. Exact viewer names/versions were not recorded in this pass; add them during a future compatibility audit.

For future releases, keep recording the specific external application versions used for validation, especially LibreCAD, QCAD and Autodesk DWG TrueView.

## Export policy

| OpenCad2D entity | DXF export policy |
|---|---|
| Line | `LINE` |
| Circle | `CIRCLE` |
| Arc | `ARC` |
| Polyline | `LWPOLYLINE` |
| Polygon | closed `LWPOLYLINE` |
| Ellipse | `ELLIPSE` where supported by the exporter path |
| Text | `TEXT` |
| Multiline text | `MTEXT` |
| Bezier spline | `SPLINE` or compatible approximation depending on the export path |
| Dimensions | drawable geometry, not native associative DXF `DIMENSION` |

## Import policy

| DXF entity | v0.8 import behavior |
|---|---|
| `LINE` | imported as `LineEntity` |
| `CIRCLE` | imported as `CircleEntity` |
| `ARC` | imported as `ArcEntity` |
| `LWPOLYLINE` without bulge | imported as `PolylineEntity` |
| `LWPOLYLINE` with bulge | imported as line/arc entities to preserve curved geometry |
| `TEXT` | imported as `TextEntity` |
| `MTEXT` | imported as `MultilineTextEntity` |
| full `ELLIPSE` | imported as `EllipseEntity` |
| partial `ELLIPSE` | imported as open `PolylineEntity` approximation |
| readable `SPLINE` control points | imported as `BezierSplineEntity` |
| fit-point-only `SPLINE` | imported as `PolylineEntity` approximation |

## Known DXF limitations

- Binary DXF is not supported.
- DWG is not supported.
- `BLOCK` / `INSERT` are not yet supported.
- `HATCH`, `IMAGE` and `LEADER` are not yet supported.
- Native associative DXF `DIMENSION` import/export is not yet implemented.
- Partial ellipses are approximated as polylines.
- Full NURBS spline fidelity is not guaranteed yet.
