# SVG Export

OpenCad2D can export the current drawing to SVG.

SVG export is an output feature. It does not save the OpenCad2D document, does not change `CurrentFilePath` and does not clear the dirty marker.

---

## Supported output

Current SVG export supports:

```text
PointEntity
TextEntity
LineEntity
CircleEntity
ArcEntity
PolylineEntity
ImageReferenceEntity as external <image href="..."> link
basic dimensions as graphical primitives
```

The SVG exporter uses layer line formats for stroke color, line weight and dash pattern. Text export uses the document text format referenced by each `TextEntity`. Supported filled closed entities use `Layer.FillColor`.

---

## External raster image references

SVG export writes `ImageReferenceEntity` as an external raster link. The image file is not embedded as base64.

Expected behavior:

- the SVG contains an `<image href="...">` reference;
- the linked file must remain available next to the SVG or at the referenced path;
- the image rectangle follows the stored CAD orientation, size, rotation and opacity;
- the export remains consistent with the project policy that raster attachments are external references, not embedded document payloads.

For portable SVG output, run `Collect Refs` before export and keep the drawing/SVG and `images/` folder together as needed.

## Background modes

Selectable SVG background modes:

```text
CanvasDark   -> preserves the OpenCad2D dark canvas look
White        -> useful for print-friendly diagrams and vector editors
Transparent  -> useful for websites, documentation and compositing
```

---

## Solid fill

SVG export supports solid fill for:

```text
CircleEntity with IsFilled = true
closed PolylineEntity with IsFilled = true
```

Rectangles and polygons are exported through the closed-polyline path. Open polylines always write `fill="none"`.

Fill color is resolved from the entity layer:

```text
Entity -> LayerId -> Layer.FillColor
```

Stroke remains independent and continues to come from the layer line format.

---

## Layer grouping

Optional grouping by layer wraps entities in SVG groups:

```xml
<g id="layer-Walls" data-layer-name="Walls">
  ...
</g>
```

This makes the SVG easier to inspect or edit in external vector tools.

---

## Layer behavior

Default behavior:

```text
hidden layers         -> ignored
visible locked layers -> exported
visible normal layers -> exported
```

The settings window exposes an option to include hidden layers when explicitly requested.

---

## Line formats

SVG export uses the effective `LineFormat.DashPattern`.

Example:

```xml
stroke-dasharray="8 4"
```

Continuous lines omit `stroke-dasharray`.

---

## Coordinate orientation

SVG export preserves the same visual Y orientation as the OpenCad2D canvas.

A shape that appears near the top of the OpenCad2D canvas should also appear near the top of the exported SVG.
