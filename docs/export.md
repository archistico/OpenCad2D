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
LineEntity      -> <line>
CircleEntity    -> <circle>
Polyline open   -> <polyline>
Polyline closed -> <polygon>
ArcEntity       -> <path>
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
fill             -> none for now
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

## Future work

Possible improvements:

- export selected entities only;
- SVG layer groups using `<g>`;
- transparent background option in the UI;
- fill export once layer fill color is implemented;
- SVG layer groups using line format metadata where useful;
- export settings dialog;
- PDF export;
- DXF export.
