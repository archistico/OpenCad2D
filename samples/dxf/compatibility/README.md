# DXF compatibility samples

This folder contains the manual DXF compatibility sample set used by OpenCad2D release validation.

The files are intentionally small and human-readable ASCII DXF files. They are not native persistence files and should not be used as exhaustive CAD conformance fixtures.

## Files

| File | Purpose |
|---|---|
| `01_basic_lines_layers.dxf` | Lines, layers, linetypes and lineweights. |
| `02_text_mtext.dxf` | `TEXT` and `MTEXT`, including MTEXT line breaks and reference width. |
| `03_arcs_circles_ellipses.dxf` | `CIRCLE`, `ARC`, full `ELLIPSE` and partial `ELLIPSE`. |
| `04_polylines_polygons.dxf` | Open/closed `LWPOLYLINE` and one bulge segment. |
| `05_dimensions_as_geometry.dxf` | Dimension-like graphics exported as primitive geometry. |
| `06_spline_bezier.dxf` | `SPLINE` with degree, knot vector and control points. |
| `07_mixed_drawing.dxf` | Mixed smoke drawing with the main supported entity families. |

## Manual validation rule

For each external application, record:

- application name;
- exact version/build;
- operating system;
- date;
- pass/fail/partial result for each sample;
- short notes for visual orientation, layer appearance, text wrapping and spline rendering.

Record the result in `docs/dxf-compatibility.md`.
