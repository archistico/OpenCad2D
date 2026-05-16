# OpenCad2D v0.8.x Final - GitHub Release Draft

This release consolidates the v0.8 line before the v0.9 roadmap. It combines the CAD-style command input milestone with the final baseline drawing tools and modify-tool stabilization.

## Highlights

- CAD-style guided command input with aliases, contextual prompts, coordinates, relative coordinates, polar input and direct distances.
- Native save/load using `.opencad2d.json`, including document settings and partial recovery for readable but partially invalid files.
- Export to SVG, DXF and PDF, with dimension export coverage across the current non-associative dimension types.
- DXF import for the practical 2D subset, including `MTEXT`.
- Final v0.8.x drawing tools: Polygon, Ellipse, MTEXT and Bezier Spline.
- Modify workflow stabilization for Trim, Break and Offset across the supported entity set.

## New drawing tools

- `POLYGON` / `PG`: regular polygons stored as closed polylines.
- `ELLIPSE` / `EL`: ellipse from center, major axis and minor radius.
- `MTEXT` / `MT`: multiline annotation text through the multiline text dialog.
- `SPLINE` / `SPL`: Bezier spline from control points, with `Undo`, `Close` and Enter-to-finish workflow.

## Modify tools

- `TRIM` supports `All`, additional cutting edges and in-command `Undo`.
- `BREAKPOINT` and `BREAK` support lines/arcs/circles where applicable, ellipses, polylines, polygons and sampled Bezier splines.
- `OFFSET` supports lines, circles, arcs, straight-segment polylines and sampled Bezier splines.
- Ellipse and spline modify results currently become polyline approximations when a partial curve is produced.

## Export/import

- SVG/PDF/DXF export for ellipses, multiline text and Bezier splines.
- DXF `MTEXT` import maps paragraph separators to OpenCad2D multiline text.
- DXF ELLIPSE/SPLINE import remains future work.

## Validation before publishing

Run:

```bash
dotnet build OpenCad2D.sln
dotnet test OpenCad2D.sln --no-build
```

Recommended manual smoke test:

```text
LINE -> 100,100 -> @100<45
POLYLINE -> 0,0 -> @100,0 -> @50<90 -> C
POLYGON -> sides -> center -> radius/vertex
ELLIPSE -> center -> major axis -> minor radius
MTEXT -> insertion point -> multiline dialog
SPLINE -> point -> point -> point -> U/C/Enter
TRIM -> All -> line/polyline/ellipse/spline side -> Undo
BREAK -> line/circle/ellipse/polyline/spline -> first point -> second point
OFFSET -> line/circle/arc/polyline/spline -> side point
EXPORT -> SVG/DXF/PDF
IMPORT DXF -> file with LINE/LWPOLYLINE/TEXT/MTEXT
```

## Known limitations

- Fillet is currently Line-Line only.
- Advanced Trim modes such as Fence, Crossing, Edge, Project and Erase are not implemented yet.
- Native partial ellipse/spline entities are not implemented; partial results are polyline approximations.
- DXF import does not yet reconstruct native ELLIPSE or SPLINE entities.
- Native associative dimensions remain future work.
