# DXF compatibility validation

This document tracks manual interoperability checks for the OpenCad2D ASCII DXF export/import pipeline.

The automated unit tests verify the structure emitted by the exporter, but DXF interoperability must also be checked in real CAD viewers because different applications tolerate and interpret DXF subsets differently.

## Current policy

| OpenCad2D entity | DXF export policy | Notes |
|---|---|---|
| Line | `LINE` | Native. |
| Circle | `CIRCLE` | Native. |
| Arc | `ARC` | Native. |
| Point | `POINT` | Native. |
| Open polyline | `LWPOLYLINE` | Straight segments. |
| Closed polyline / polygon | `LWPOLYLINE` closed | Polygon is stored as a closed polyline. |
| Ellipse | `ELLIPSE` | Native export; import is still future work. |
| Bezier spline | `SPLINE` | Native export; import is still future work. |
| Text | `TEXT` | Native. |
| Multiline text | `MTEXT` | Uses DXF `\P` line separators. |
| Dimensions | Graphic primitives | OpenCad2D dimensions are non-associative and are not exported as native DXF `DIMENSION` entities yet. |

## Manual validation matrix

Fill this table during the release-candidate pass.

| Sample | LibreCAD | QCAD | Autodesk DWG TrueView | Notes |
|---|---|---|---|---|
| `samples/dxf/compatibility/01_basic_lines_layers.dxf` | Pending | Pending | Pending | Basic lines and layer names. |
| `samples/dxf/compatibility/02_text_mtext.dxf` | Pending | Pending | Pending | TEXT and MTEXT line breaks. |
| `samples/dxf/compatibility/03_arcs_circles_ellipses.dxf` | Pending | Pending | Pending | To be generated from OpenCad2D export. |
| `samples/dxf/compatibility/04_polylines_polygons.dxf` | Pending | Pending | Pending | To be generated from OpenCad2D export. |
| `samples/dxf/compatibility/05_dimensions_as_geometry.dxf` | Pending | Pending | Pending | Dimensions are intentionally graphic primitives for now. |
| `samples/dxf/compatibility/06_spline_bezier.dxf` | Pending | Pending | Pending | Native SPLINE export. |
| `samples/dxf/compatibility/07_mixed_drawing.dxf` | Pending | Pending | Pending | Full mixed drawing smoke test. |

## Known limitations to keep visible

- Binary DXF is not supported.
- DWG is intentionally not supported.
- `BLOCK` / `INSERT` are not supported yet.
- `HATCH`, `IMAGE` and `LEADER` are not supported yet.
- `LWPOLYLINE` bulge import is still pending, so curved polyline segments from external CAD files may lose curvature until that milestone is implemented.
- Native DXF `DIMENSION` import/export is still pending.
- `ELLIPSE` and `SPLINE` export exists, while import is still future work.

## Release rule

Before tagging a public v0.9 release candidate, at least the first seven compatibility samples should be opened manually in LibreCAD and QCAD. Autodesk DWG TrueView validation is recommended when available.
