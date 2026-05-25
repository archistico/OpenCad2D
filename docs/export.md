# Export

OpenCad2D supports external export formats separately from native persistence.

Native persistence saves and reopens `.opencad2d.json` drawings. Export creates derived output files such as SVG, DXF and PDF and must not change the document state.

---

## Project

Export code lives in:

```text
src/OpenCad2D.Export
```

Dependency rule:

```text
OpenCad2D.Export -> OpenCad2D.Core -> OpenCad2D.Geometry
```

`OpenCad2D.Export` must not depend on:

```text
OpenCad2D.App
OpenCad2D.Tools
OpenCad2D.Interaction
OpenCad2D.Persistence
Avalonia
```

The App owns file dialogs and error dialogs. The exporter owns file content generation.

---


## Save versus Export UX policy

Save and Export are intentionally different operations.

```text
Save / Save As
-> writes the native editable .opencad2d.json document
-> updates CurrentFilePath
-> clears the dirty state

Export SVG / PDF / DXF / PNG
-> writes a derived external file
-> does not update CurrentFilePath
-> does not clear the dirty state
-> does not mark the drawing as saved
```

This separation protects source data. Exported files may be final or interchange representations and may not preserve every native OpenCad2D editing feature.

After a successful export, the App must clearly tell the user that export did not save the editable OpenCad2D project. The status message must distinguish these cases:

```text
Native project never saved
-> tell the user to use Save As

Native project saved but dirty
-> tell the user that unsaved project changes remain

Native project saved and clean
-> tell the user that the native drawing is already saved
```

The export message is informational and non-modal. The existing close-warning flow remains responsible for preventing accidental loss of unsaved native project data.

---
## SVG export

Current SVG export is implemented by `SvgExporter`.

Detailed document: [`svg-export.md`](svg-export.md).

Supported entities:

```text
PointEntity               -> small marker
TextEntity                -> <text>
LineEntity                -> <line>
CircleEntity              -> <circle>, optionally filled
Polyline open             -> <polyline>
Polyline closed           -> <polygon>, optionally filled
ImageReferenceEntity      -> external <image href="..."> link
ArcEntity                 -> <path>
Horizontal dimension      -> lines + text
Vertical dimension        -> lines + text
Aligned dimension         -> lines + text
Radius dimension          -> leader/arrow lines + text
Diameter dimension        -> leader/arrow lines + text
Angular dimension         -> lines + arc + text
```

Layer rules:

```text
hidden layers         -> ignored
visible locked layers -> exported
visible normal layers -> exported
```

Appearance rules:

```text
stroke           -> LineFormat.Color referenced by the entity layer
stroke-width     -> LineFormat.LineWeight referenced by the entity layer
stroke-dasharray -> LineStyleDashPattern for non-continuous formats
text fill        -> TextFormat.Color referenced by TextEntity.TextFormatId
dimension text   -> DimensionStyle.TextFormatId -> TextFormat
text font        -> TextFormat.FontFamily
text size        -> TextFormat.Height
fill             -> Layer.FillColor for filled circles and filled closed polylines; none otherwise
image href       -> external raster path stored by ImageReferenceEntity; raster bytes are not embedded
```

This matches the project rule that stroke appearance comes from the layer's reusable line format, while solid fill color comes from the layer itself.

---

## Line formats in SVG

For each exported entity, the exporter resolves appearance through the document model:

```text
entity.LayerId
-> document.Layers.GetById(...)
-> layer.LineFormatId
-> document.LineFormats.GetById(...)
```

Continuous lines do not write `stroke-dasharray`. Dashed, dash-dot and dash-dot-dot formats write a `stroke-dasharray` attribute using the same model-space pattern used by the canvas renderer.

The SVG exporter must not use legacy layer color/weight fields or per-entity style overrides as the active stroke source.

---

## ViewBox and background

The SVG exporter computes the `viewBox` from the bounds of visible exported entities and applies a configurable margin.

By default, the SVG can include a background rectangle matching the OpenCad2D canvas dark background:

```text
#1E1E1E
```

The background rectangle is the size of the SVG viewBox.

`SvgExportOptions` supports `CanvasDark`, `White` and `Transparent` background modes. It can also group exported entities by layer with SVG `<g>` elements.

---

## Coordinate orientation

SVG export preserves the same visual Y orientation as the OpenCad2D canvas.

A shape that appears near the top of the OpenCad2D canvas should appear near the top of the exported SVG.

This is a deliberate choice for the current export workflow, where the SVG is expected to look like the drawing shown in the application.

---


## DXF export

Current DXF export is implemented by `DxfExporter`.

Detailed document: [`dxf-export.md`](dxf-export.md).

The exporter writes a minimal AutoCAD 2000 ASCII DXF file:

```text
$ACADVER = AC1015
```

Current supported entities:

```text
PointEntity               -> POINT
TextEntity                -> TEXT
MultilineTextEntity       -> MTEXT
LineEntity                -> LINE
CircleEntity              -> CIRCLE, plus HATCH when filled
EllipseEntity             -> ELLIPSE
ArcEntity                 -> ARC
PolylineEntity            -> LWPOLYLINE, plus HATCH when closed and filled
BezierSplineEntity        -> SPLINE
Horizontal dimension      -> LINE + TEXT graphical primitives
Vertical dimension        -> LINE + TEXT graphical primitives
Aligned dimension         -> LINE + TEXT graphical primitives
Radius dimension          -> LINE + TEXT graphical primitives
Diameter dimension        -> LINE + TEXT graphical primitives
Angular dimension         -> LINE + ARC + TEXT graphical primitives
```

Current DXF structure:

```text
HEADER
TABLES
  LTYPE
  LAYER
ENTITIES
EOF
```

Layer and appearance rules:

```text
all document layers -> written to the LAYER table
hidden layer records -> written with negative ACI color
hidden layer entities -> ignored by default
visible locked layers -> exported
entity color/style/weight -> BYLAYER
layer color/style/weight -> resolved from LineFormat
fill hatch color -> resolved from Layer.FillColor
```

`DxfExportOptions.IncludeHiddenLayers` can include entities on hidden layers when explicitly enabled.

The UI exposes DXF export through the file command bar as `Export DXF`. The App owns the save file dialog; `OpenCad2D.Export` owns the DXF content generation.

---

## Line formats in DXF

DXF export resolves layer appearance through the same model used by rendering and SVG export:

```text
entity.LayerId
-> document.Layers.GetById(...)
-> layer.LineFormatId
-> document.LineFormats.GetById(...)
```

The resolved `LineFormat` is written on the layer record:

| DXF group | Meaning | Source |
|---:|---|---|
| `62` | ACI color | nearest basic AutoCAD color index |
| `420` | true color | RGB value |
| `6` | linetype | `CONTINUOUS`, `DASHED`, `DASHDOT`, `DASHDOTDOT` |
| `370` | lineweight | converted from the graphic line-weight value |

Entities use:

```text
62  256       // BYLAYER color
6   BYLAYER   // BYLAYER linetype
370 -1        // BYLAYER lineweight
```

If a layer references a missing line format, the exporter falls back to `Continuous`.

---

## Solid fill export

Solid fill is currently supported for circles and closed polylines. Rectangles and polygons use the closed-polyline path.

Model rules:

```text
CircleEntity.IsFilled
PolylineEntity.IsFilled + PolylineEntity.IsClosed
Layer.FillColor
```

Export behavior:

```text
SVG -> fill attribute with Layer.FillColor
PDF -> fill-and-stroke path for supported filled entities
DXF -> separate SOLID HATCH entity plus the normal border entity
```

Open polylines and unsupported entity types always export without fill. Stroke remains controlled by line formats.

---

## DXF coordinate orientation

The current DXF exporter mirrors Y using the exported content bounds:

```text
DXF_Y = bounds.MinY + bounds.MaxY - modelY
```

This was added after practical viewer testing because the first DXF output appeared vertically flipped in external viewers.

The transformation is limited to export. It does not change the internal model coordinate system.

Arc angles are converted consistently with this Y flip.

---


---

## DXF import

DXF import is implemented by `DxfDocumentImporter`.

Detailed document: [`dxf-import.md`](dxf-import.md).

Current imported DXF entities:

```text
LINE
CIRCLE
ARC
POINT
LWPOLYLINE
TEXT
```

The importer reads `TABLES/LAYER`, maps common layer appearance information and returns `DxfImportResult` with diagnostics and aggregate `DxfImportStatistics`.

Unsupported entities are skipped with warnings instead of crashing the import pipeline.

---

## PDF export

PDF export is implemented by `PdfExporter`.

Detailed document: [`pdf-export.md`](pdf-export.md).

Current PDF behavior:

```text
single-page vector PDF
A4/A3/A2/A1/A0
portrait/landscape
margins in millimeters
fit-to-page
print-friendly colors
optional hidden-layer inclusion
```

PDF export is independent from Avalonia and does not mutate or save the document.

## Text and point export

`PointEntity` is exported as a native point where possible:

```text
SVG -> small marker
DXF -> POINT
```

`TextEntity` is exported as single-line text:

```text
SVG -> <text>
DXF -> TEXT
```

Text export resolves appearance through `Document.TextFormats`:

```text
TextEntity.TextFormatId
-> TextFormat.FontFamily
-> TextFormat.Height
-> TextFormat.Color
-> TextFormat.IsBold / IsItalic
```

`MultilineTextEntity` is exported as multiline SVG text with `<tspan>` lines, PDF text lines and DXF `MTEXT` with paragraph separators.

---


## Dimension export

The first v0.4 dimension export is intentionally graphical, not associative.

Horizontal, vertical, aligned, radius, diameter and angular dimensions are exported through the shared `DimensionGeometryBuilder` render model:

```text
DimensionEntity
-> DimensionStyle
-> DimensionGeometryBuilder
-> dimension lines, extension lines, arrow lines, angular arcs and measurement text
```

SVG writes dimensions as visual primitives:

```text
line primitives -> <line>
arc primitives  -> <path>
measurement     -> <text>
```

DXF writes dimensions as simple graphical entities:

```text
line primitives -> LINE
arc primitives  -> ARC
measurement     -> TEXT
```

This means external CAD programs can display the dimension graphics, but the exported DXF dimensions are not native editable `DIMENSION` objects yet. Native DXF `DIMENSION` export is intentionally left for a later interoperability phase because it requires more complex DXF block/style handling and viewer-specific validation.

Dimension text appearance is resolved from:

```text
DimensionEntity.DimensionStyleId
-> Document.DimensionStyles
-> DimensionStyle.TextFormatId
-> Document.TextFormats
```

The default `Standard` dimension style uses the `Annotation` text format.

---

## Export is not Save

SVG/DXF export does not affect native document state.

It must not:

- change `CurrentFilePath`;
- call `MarkSaved()`;
- clear the dirty marker;
- change command history generation;
- modify the document.

If the drawing is dirty before export, it remains dirty after SVG or DXF export.

---

## Automated DXF checks

The test suite includes internal DXF compatibility checks before manual validation in external CAD viewers.

Covered automated checks:

```text
ASCII DXF code/value pairs are balanced
representative entity records are written for POINT, TEXT, LINE, CIRCLE, ARC, LWPOLYLINE
entity records use BYLAYER color, linetype and lineweight
layer records write a single linetype group
built-in line formats map to expected DXF linetype, true color and lineweight values
TEXT export writes content, height, style name and mirrored angle
open/closed LWPOLYLINE records write expected vertex count and flags
horizontal, vertical and aligned dimensions export as LINE + TEXT graphical primitives
radius and diameter dimensions export as LINE + TEXT graphical primitives
angular dimensions export as LINE + ARC + TEXT graphical primitives
dimension primitives use BYLAYER properties
solid HATCH records for filled circles and closed polylines
```

These tests do not replace manual validation in LibreCAD, QCAD and Autodesk DWG TrueView. They are intended to catch structural regressions before external viewer testing.

---

## Future work

Possible improvements:

- export selected entities only;
- SVG layer groups using `<g>`;
- transparent background option in the UI;
- SVG layer groups using line format metadata where useful;
- user-editable hatch/pattern definitions beyond the current solid fill support;
- export settings dialog;
- DXF export options dialog;
- export selected entities only;
- native DXF `DIMENSION` export after the internal dimension model is stable;
- blocks for DXF when the model supports them;
- PNG export.


## v0.4 dimension export status

The v0.4 export scope is complete for the implemented basic dimension types:

- horizontal dimensions;
- vertical dimensions;
- aligned dimensions;
- radius dimensions;
- diameter dimensions;
- angular dimensions, including reflex angles.

All are exported as graphical primitives. This keeps external-viewer compatibility predictable while the internal dimension model is still evolving. Native editable DXF `DIMENSION` records remain a future interoperability task.


## Raster images in export

External raster image references are currently preserved in SVG export as external `<image href="...">` links. The raster bytes are not embedded.

PDF and DXF raster-image output are intentionally deferred. DXF support will require `IMAGE` / `IMAGEDEF` objects, dictionary wiring and compatibility checks in CAD viewers such as QCAD, LibreCAD and Autodesk viewers.
