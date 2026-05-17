# DXF Export

OpenCad2D can export the visible model-space drawing to an ASCII DXF file.

DXF export is an interoperability/export feature. It is separate from native persistence and does not save the OpenCad2D document.

---

## Current scope

The current DXF exporter writes a minimal AutoCAD 2000 ASCII DXF structure:

```text
$ACADVER = AC1015
```

Supported sections:

```text
HEADER
TABLES
  LTYPE
  LAYER
ENTITIES
EOF
```

Supported entity mappings:

| OpenCad2D entity | DXF entity |
|---|---|
| `PointEntity` | `POINT` |
| `TextEntity` | `TEXT` |
| `MultilineTextEntity` | `MTEXT` |
| `LineEntity` | `LINE` |
| `CircleEntity` | `CIRCLE` |
| `ArcEntity` | `ARC` |
| `EllipseEntity` | `ELLIPSE` |
| `PolylineEntity` | `LWPOLYLINE` |
| `BezierSplineEntity` | `SPLINE` with open-uniform knot vector |
| basic dimensions | graphical primitives: `LINE`, `ARC`, `TEXT` |

Hatches, blocks, layouts and paper space are not exported yet.

---

## Project location

DXF export lives in:

```text
src/OpenCad2D.Export/Dxf
```

Main types:

```text
DxfExporter
IDxfExporter
DxfExportOptions
DxfExportResult
DxfDocumentWriter
DxfColorMapper
DxfLineTypeMapper
DxfLineWeightMapper
```

The exporter follows the same dependency rule as SVG export:

```text
OpenCad2D.Export -> OpenCad2D.Core -> OpenCad2D.Geometry
```

It must not depend on App, Tools, Interaction, Persistence or Avalonia.

---

## UI entry point

The file command bar contains:

```text
Export DXF
```

The button opens a save file dialog for `.dxf` files. The UI owns the dialog and error handling; `OpenCad2D.Export` owns only the file content generation.

Exporting DXF does not:

- change `CurrentFilePath`;
- call `MarkSaved()`;
- clear the dirty marker;
- create an undoable command;
- mutate the document.

After export, the status message reports the exported file name and entity count.

---

## Layer and line format export

DXF export uses the same layer appearance model as canvas rendering and SVG export:

```text
Entity -> LayerId -> Layer -> LineFormatId -> LineFormat
```

The `LAYER` table is written for all document layers.

For each layer, DXF properties are derived from the resolved `LineFormat`:

| DXF group | Meaning | Source |
|---:|---|---|
| `62` | ACI color | nearest basic ACI color from `LineFormat.Color` |
| `420` | true color | RGB value from `LineFormat.Color` |
| `6` | linetype | `LineFormat.LineStyle` |
| `370` | lineweight | `LineFormat.LineWeight` |

Hidden layers are written with negative ACI color, following the common DXF convention for off layers.

If a layer references a missing line format, the exporter falls back to `Continuous`.

---

## Linetypes

The `LTYPE` table currently exports these linetypes:

| OpenCad2D `LineStyle` | DXF linetype |
|---|---|
| `Continuous` | `CONTINUOUS` |
| `Dashed` | `DASHED` |
| `DashDot` | `DASHDOT` |
| `DashDotDot` | `DASHDOTDOT` |

Pattern values are derived from the same conceptual line-style families used by the canvas and SVG exporter.

DXF linetype patterns use positive values for drawn segments, negative values for gaps and zero-length segments for dots.

---

## Entity appearance

Entities are written with `BYLAYER` properties:

```text
8   layer name
62  256
6   BYLAYER
370 -1
```

This keeps DXF output CAD-like: layer records control color, linetype and lineweight; entities keep their own geometry and layer reference.

---

## Hidden and locked layers

Default entity export behavior:

```text
hidden layers         -> entities ignored
visible locked layers -> entities exported
visible normal layers -> entities exported
```

`DxfExportOptions.IncludeHiddenLayers` can include hidden-layer entities when explicitly enabled.

Locked layers are exported as locked layer records in the `LAYER` table and their visible entities are exported normally.

---


## SPLINE export

`BezierSplineEntity` is exported as a DXF `SPLINE` entity. The exporter writes:

```text
70  spline flags
71  degree
72  knot count
73  control point count
74  fit point count
40  knot values
10/20/30 control points
```

The knot vector is an open-uniform vector with `controlPointCount + degree + 1` values. The degree is capped at 3 and also limited by the available control point count. Closed OpenCad2D splines are written with the closed + planar flags, but without the periodic flag, because the exported knot vector is not periodic.

This keeps the DXF file structurally complete for CAD viewers that expect knot values on `SPLINE` entities. Full external NURBS fidelity, rational weights and fit-point reconstruction are still outside the current exporter scope.

## Coordinates and Y orientation

DXF is exported in model space, but the current exporter mirrors Y using the exported content bounds so the result matches the visual top/bottom orientation seen in OpenCad2D and tested with external DXF viewers.

For each exported point:

```text
DXF_X = X
DXF_Y = bounds.MinY + bounds.MaxY - Y
```

Arc angles are adjusted consistently with this Y flip.

This is an export/display compatibility choice. It does not change the internal model coordinate system.

---

## Arc export

DXF `ARC` entities use:

```text
10,20,30 center
40       radius
50       start angle in degrees
51       end angle in degrees
```

The exporter converts OpenCad2D arc angles to DXF degrees and accounts for the Y flip. Since DXF arcs are represented counterclockwise, clockwise/counterclockwise orientation is normalized during export.

---

## Test coverage

Current DXF tests cover:

- minimal file structure;
- section order;
- custom ACAD version;
- file writing;
- `LINE`;
- `CIRCLE`;
- `ARC`;
- clockwise/counterclockwise arc conversion;
- `LWPOLYLINE`;
- hidden-layer exclusion/inclusion;
- `LTYPE` table;
- `LAYER` table;
- color, true color, linetype and lineweight from `LineFormat`;
- entity `BYLAYER` properties;
- hidden layer records;
- missing line format fallback;
- Y flip behavior.

---

## Future work

Recommended next DXF improvements:

1. test output in LibreCAD, QCAD, Autodesk DWG TrueView and one online viewer, then document tested versions and results;
2. add an export options dialog;
3. optionally export selected entities only;
4. add an export options dialog for DXF when needed;
5. optionally export selected entities only;
6. add hatches/fills when fill support exists;
7. consider blocks only after the entity model has block references.

---

## Related DXF import document

DXF import is documented separately in:

```text
docs/dxf-import.md
```
