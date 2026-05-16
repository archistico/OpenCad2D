# DXF compatibility samples

These ASCII DXF samples are used for the v0.8 manual interoperability pass.

Open each file in LibreCAD and QCAD. Autodesk DWG TrueView is recommended when available.
Record results in `docs/dxf-compatibility.md`.

## Samples

| File | Purpose |
|---|---|
| `01_basic_lines_layers.dxf` | LINE entities, layer names, linetype/lineweight table smoke test. |
| `02_text_mtext.dxf` | TEXT, rotated TEXT and MTEXT paragraph separators. |
| `03_arcs_circles_ellipses.dxf` | CIRCLE, ARC, full ELLIPSE and partial ELLIPSE. |
| `04_polylines_polygons.dxf` | Open LWPOLYLINE, closed polygon-style LWPOLYLINE and bulge arcs. |
| `05_dimensions_as_geometry.dxf` | Dimension-like graphical primitives, intentionally not native DXF DIMENSION. |
| `06_spline_bezier.dxf` | Open and closed SPLINE control-point records. |
| `07_mixed_drawing.dxf` | Combined smoke test for the main v0.8 drawing/export subset. |

## Validation notes

For each file, check:

- the file opens without errors;
- geometry appears at the expected scale and orientation;
- layers are present and visible;
- text and MTEXT line breaks are readable;
- curves remain curves where the target viewer supports them;
- dimension sample is understood as graphical geometry, not as editable native dimensions.
