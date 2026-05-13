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
basic dimensions as graphical primitives
```

The SVG exporter uses layer line formats for stroke color, line weight and dash pattern. Text export uses the document text format referenced by each `TextEntity`.

---

## Background modes

v0.7 adds selectable SVG background modes through `SvgBackgroundMode`:

```text
CanvasDark   -> preserves the previous OpenCad2D dark canvas look
White        -> useful for print-friendly diagrams and vector editors
Transparent  -> useful for websites, documentation and compositing
```

The previous dark-background behavior remains the exporter default for compatibility.

---

## Layer grouping

v0.7 adds optional grouping by layer.

When enabled, entities are wrapped in SVG groups like:

```xml
<g id="layer-Walls" data-layer-name="Walls">
  ...
</g>
```

This makes the SVG easier to inspect or edit in tools such as Inkscape, Illustrator or browser-based workflows.

The generated `id` is sanitized for SVG compatibility. The `data-layer-name` attribute preserves the original layer name.

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

## Coordinate orientation

SVG export preserves the same visual Y orientation as the OpenCad2D canvas.

A shape that appears near the top of the OpenCad2D canvas should also appear near the top of the exported SVG.

---

## UI command

The UI command is:

```text
Export SVG
```

The command opens an SVG settings window before the save-file picker.

Available settings:

```text
background mode
margin
include hidden layers
group by layer
include metadata
```

Layer grouping is enabled by default in the settings dialog.

---

## Main implementation files

```text
src/OpenCad2D.Export/Svg/ISvgExporter.cs
src/OpenCad2D.Export/Svg/SvgExporter.cs
src/OpenCad2D.Export/Svg/SvgExportOptions.cs
src/OpenCad2D.Export/Svg/SvgExportResult.cs
src/OpenCad2D.Export/Svg/SvgBackgroundMode.cs
src/OpenCad2D.App/SvgExportSettingsWindow.axaml
src/OpenCad2D.App/ViewModels/Svg/SvgExportSettingsWindowViewModel.cs
```

---

## Test coverage

SVG tests cover:

```text
basic SVG output
entity export
line-format export
text export
dimension export
hidden-layer behavior
white background
transparent background
layer grouping
settings view-model validation
```
