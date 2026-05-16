# DXF compatibility samples

This folder contains small, focused ASCII DXF files used for manual compatibility checks in external CAD viewers.

Recommended viewers for v0.9 stabilization:

- LibreCAD
- QCAD
- Autodesk DWG TrueView

Record results in `docs/dxf-compatibility.md` before publishing a release candidate.

## Sample set

| File | Purpose |
|---|---|
| `01_basic_lines_layers.dxf` | Basic LINE entities on separate layers. |
| `02_text_mtext.dxf` | TEXT and MTEXT import/display validation. |

Additional samples should be generated from OpenCad2D exports during the release-candidate pass:

- `03_arcs_circles_ellipses.dxf`
- `04_polylines_polygons.dxf`
- `05_dimensions_as_geometry.dxf`
- `06_spline_bezier.dxf`
- `07_mixed_drawing.dxf`
