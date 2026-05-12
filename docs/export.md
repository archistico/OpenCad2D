# Export

OpenCad2D supports external export formats separately from native persistence.

Native persistence saves and reopens `.opencad2d.json` drawings. Export creates derived output files such as SVG and must not change the document state.

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

## SVG export

Current SVG export is implemented by `SvgExporter`.

Supported entities:

```text
PointEntity               -> small marker
TextEntity                -> <text>
LineEntity                -> <line>
CircleEntity              -> <circle>
Polyline open             -> <polyline>
Polyline closed           -> <polygon>
ArcEntity                 -> <path>
Horizontal dimension      -> lines + text
Vertical dimension        -> lines + text
Aligned dimension         -> lines + text
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
fill             -> none for closed geometry for now
```

This matches the project rule that entity appearance comes from the layer's reusable line format, not from per-entity overrides.

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

By default, the SVG includes a background rectangle matching the OpenCad2D canvas dark background:

```text
#1E1E1E
```

The background rectangle is the size of the SVG viewBox.

`SvgExportOptions` can disable the background or change its color.

---

## Coordinate orientation

SVG export preserves the same visual Y orientation as the OpenCad2D canvas.

A shape that appears near the top of the OpenCad2D canvas should appear near the top of the exported SVG.

This is a deliberate choice for the current export workflow, where the SVG is expected to look like the drawing shown in the application.

---


## DXF export

Current DXF export is implemented by `DxfExporter`.

The exporter writes a minimal AutoCAD 2000 ASCII DXF file:

```text
$ACADVER = AC1015
```

Current supported entities:

```text
PointEntity               -> POINT
TextEntity                -> TEXT
LineEntity                -> LINE
CircleEntity              -> CIRCLE
ArcEntity                 -> ARC
PolylineEntity            -> LWPOLYLINE
Horizontal dimension      -> LINE + TEXT graphical primitives
Vertical dimension        -> LINE + TEXT graphical primitives
Aligned dimension         -> LINE + TEXT graphical primitives
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

## DXF coordinate orientation

The current DXF exporter mirrors Y using the exported content bounds:

```text
DXF_Y = bounds.MinY + bounds.MaxY - modelY
```

This was added after practical viewer testing because the first DXF output appeared vertically flipped in external viewers.

The transformation is limited to export. It does not change the internal model coordinate system.

Arc angles are converted consistently with this Y flip.

---

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

The current implementation intentionally does not export multiline text because the model currently supports only single-line `TextEntity`. A future multiline text feature should be designed separately and may map to DXF `MTEXT`.

---


## Dimension export

The first v0.4 dimension export is intentionally graphical, not associative.

Horizontal, vertical and aligned dimensions are exported through the shared `DimensionGeometryBuilder` render model:

```text
DimensionEntity
-> DimensionStyle
-> DimensionGeometryBuilder
-> dimension lines, extension lines, arrow lines and measurement text
```

SVG writes dimensions as visual primitives:

```text
line primitives -> <line>
measurement     -> <text>
```

DXF writes dimensions as simple graphical entities:

```text
line primitives -> LINE
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

SVG export does not affect native document state.

It must not:

- change `CurrentFilePath`;
- call `MarkSaved()`;
- clear the dirty marker;
- change command history generation;
- modify the document.

If the drawing is dirty before export, it remains dirty after export.

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
dimension primitives use BYLAYER properties
```

These tests do not replace manual validation in LibreCAD, QCAD and Autodesk DWG TrueView. They are intended to catch structural regressions before external viewer testing.

---

## Future work

Possible improvements:

- export selected entities only;
- SVG layer groups using `<g>`;
- transparent background option in the UI;
- fill export once layer fill color is implemented;
- SVG layer groups using line format metadata where useful;
- export settings dialog;
- DXF export options dialog;
- export selected entities only;
- native DXF `DIMENSION` export after the internal dimension model is stable;
- hatches and blocks for DXF when the model supports them;
- PDF export.
