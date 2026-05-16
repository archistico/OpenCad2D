# DXF compatibility validation

This document tracks manual interoperability checks for the OpenCad2D ASCII DXF export/import pipeline.

The automated unit tests verify the structure emitted by the exporter and selected import workflows, but DXF interoperability must also be checked in real CAD viewers because different applications tolerate and interpret DXF subsets differently.

---

## Current v0.8 policy

| OpenCad2D entity / DXF feature | DXF export/import policy | Notes |
|---|---|---|
| Line | `LINE` | Native export/import. |
| Circle | `CIRCLE` | Native export/import. |
| Arc | `ARC` | Native export/import. |
| Point | `POINT` | Native export/import. |
| Open polyline | `LWPOLYLINE` | Straight segments stay as editable OpenCad2D polylines. |
| Closed polyline / polygon | `LWPOLYLINE` closed | Polygon is stored as a closed polyline. |
| `LWPOLYLINE` bulge | Imported as `LineEntity` / `ArcEntity` segments | Curved geometry is preserved, but the original compound polyline topology is not preserved yet. |
| Ellipse | `ELLIPSE` | Native export and full-ellipse import. Partial DXF ellipses import as open polyline approximations. |
| Bezier spline | `SPLINE` | Native export; readable control-point splines import as `BezierSplineEntity`; fit-point-only splines import as polyline approximations. |
| Text | `TEXT` | Native export/import. |
| Multiline text | `MTEXT` | Uses DXF `\P` line separators. |
| Dimensions | Graphic primitives | OpenCad2D dimensions are non-associative and are not exported/imported as native DXF `DIMENSION` entities yet. |

---

## Compatibility sample set

The sample folder is:

```text
samples/dxf/compatibility/
```

| Sample | Purpose |
|---|---|
| `01_basic_lines_layers.dxf` | LINE entities, layer names, linetype and lineweight table smoke test. |
| `02_text_mtext.dxf` | TEXT, rotated TEXT and MTEXT paragraph separators. |
| `03_arcs_circles_ellipses.dxf` | CIRCLE, ARC, full ELLIPSE and partial ELLIPSE. |
| `04_polylines_polygons.dxf` | Open LWPOLYLINE, closed polygon-style LWPOLYLINE and LWPOLYLINE bulge arcs. |
| `05_dimensions_as_geometry.dxf` | Dimension-like graphical primitives; intentionally not native DXF DIMENSION. |
| `06_spline_bezier.dxf` | Open and closed SPLINE control-point records. |
| `07_mixed_drawing.dxf` | Combined smoke test for the main v0.8 drawing/export subset. |

---

## Manual validation matrix

Fill this table during the release-candidate pass. `Pending` means the sample has been prepared but not yet opened in that external viewer.

| Sample | LibreCAD | QCAD | Autodesk DWG TrueView | Notes |
|---|---|---|---|---|
| `01_basic_lines_layers.dxf` | Pending | Pending | Pending | Basic lines, layers and construction linetype. |
| `02_text_mtext.dxf` | Pending | Pending | Pending | TEXT rotation and MTEXT line breaks. |
| `03_arcs_circles_ellipses.dxf` | Pending | Pending | Pending | Full ellipse should remain an ellipse; partial ellipse may be viewer-dependent. |
| `04_polylines_polygons.dxf` | Pending | Pending | Pending | Includes straight LWPOLYLINE, closed LWPOLYLINE and bulge arcs. |
| `05_dimensions_as_geometry.dxf` | Pending | Pending | Pending | Dimensions are intentionally graphical LINE/TEXT primitives. |
| `06_spline_bezier.dxf` | Pending | Pending | Pending | SPLINE control-point compatibility smoke test. |
| `07_mixed_drawing.dxf` | Pending | Pending | Pending | Full mixed drawing smoke test. |

---

## Manual validation checklist

For each viewer and sample:

1. Open the DXF without import errors or warnings that block display.
2. Confirm the drawing appears at the expected scale and orientation.
3. Confirm layers are present and visible.
4. Confirm text is readable and MTEXT line breaks are preserved acceptably.
5. Confirm CIRCLE/ARC/ELLIPSE/SPLINE records remain visible as curves where the viewer supports them.
6. Confirm `05_dimensions_as_geometry.dxf` is understood as graphical geometry, not as editable native dimensions.
7. Record any visual differences in the validation matrix above.

---

## Known limitations to keep visible

- Binary DXF is not supported.
- DWG is intentionally not supported.
- `BLOCK` / `INSERT` are not supported yet.
- `HATCH`, `IMAGE` and `LEADER` are not supported yet.
- `LWPOLYLINE` bulge import preserves curved geometry by converting bulge segments to separate line/arc entities; the original compound polyline topology is not preserved yet.
- Native DXF `DIMENSION` import/export is still pending.
- `SPLINE` import supports readable control-point splines but does not yet evaluate external NURBS knot vectors/weights.
- Full `ELLIPSE` import is supported; partial DXF ellipses are approximated as open polylines until a native ellipse-arc entity exists.

---

## Release rule

Before tagging the public v0.8 release, open at least the seven compatibility samples in LibreCAD and QCAD. Autodesk DWG TrueView validation is recommended when available.

When a viewer result is recorded, include the exact viewer version in this document or in the release checklist.
