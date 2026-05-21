# PDF Export

OpenCad2D can export the current drawing to a single-page vector PDF.

PDF export is an output feature. It does not save the OpenCad2D document, does not change `CurrentFilePath` and does not clear the dirty marker.

---

## Scope

The v0.7 PDF exporter creates a minimal PDF 1.4 file without external NuGet dependencies.

Current behavior:

```text
single-page PDF
vector geometry
fit-to-page scaling
page size selection
portrait/landscape orientation
margin in millimeters
print-friendly color mode
optional hidden-layer inclusion
```

Technical plotting scales such as `1:50`, `1:100` or layout/paper-space workflows are not implemented yet.

---

## Supported page sizes

The exporter supports:

```text
A4
A3
A2
A1
A0
```

Orientation:

```text
Portrait
Landscape
```

Margins are expressed in millimeters and converted to PDF points internally.

---

## Exported entities

Supported PDF output includes:

| OpenCad2D entity | PDF output |
|---|---|
| `LineEntity` | vector line |
| `CircleEntity` | cubic Bézier circle approximation, with optional solid fill |
| `PointEntity` | small marker |
| `ArcEntity` | segmented vector path |
| `PolylineEntity` | vector polyline / closed path, with optional solid fill for closed polylines |
| `TextEntity` | text output using built-in PDF font fallback |
| horizontal/vertical/aligned dimensions | graphical lines/arrows + measurement text |
| radius/diameter dimensions | graphical leader/arrow lines + measurement text |
| angular dimensions | graphical lines + segmented arc + angle text |

The exporter exports visible model geometry by default.

Layer behavior:

```text
hidden layers         -> ignored by default
visible locked layers -> exported
visible normal layers -> exported
```

`PdfExportOptions.IncludeHiddenLayers` can include hidden-layer entities when explicitly enabled.

---

## Solid fill

PDF export supports solid fill for:

```text
CircleEntity with IsFilled = true
closed PolylineEntity with IsFilled = true
```

Rectangles and polygons follow the closed-polyline path. Open polylines are stroked only, even if an internal fill flag is present.

The fill color is resolved from `Layer.FillColor`; the stroke color, lineweight and line style remain resolved from the layer's `LineFormatId`. Filled paths use PDF fill-and-stroke output so the border remains visible.

---

## Fit-to-page behavior

The exporter computes the bounds of the exported entities and scales them into the printable area:

```text
printable width  = page width  - left margin - right margin
printable height = page height - top margin  - bottom margin
scale            = min(printable width / drawing width, printable height / drawing height)
```

The drawing is centered inside the printable area.

PDF coordinates are mapped so the exported PDF matches the visual top/bottom orientation of the OpenCad2D canvas.

---

## Print-friendly colors

The default PDF export mode is intended for printing on a white page.

Rules:

```text
white or very light screen colors -> black
other colors                      -> preserved
background                        -> implicit white page
```

The user can disable print-friendly mode from the PDF export settings window if screen colors should be preserved.

---

## UI command

The UI command is:

```text
Export PDF
```

The command opens a PDF settings window before the save-file picker.

Available settings:

```text
page size
orientation
margin in millimeters
include hidden layers
use print-friendly colors
```

Invalid margins are rejected before the file picker opens.

---

## Main implementation files

```text
src/OpenCad2D.Export/Pdf/IPdfExporter.cs
src/OpenCad2D.Export/Pdf/PdfExporter.cs
src/OpenCad2D.Export/Pdf/PdfExportOptions.cs
src/OpenCad2D.Export/Pdf/PdfExportResult.cs
src/OpenCad2D.Export/Pdf/PdfPageSize.cs
src/OpenCad2D.Export/Pdf/PdfPageOrientation.cs
src/OpenCad2D.App/PdfExportSettingsWindow.axaml
src/OpenCad2D.App/ViewModels/Pdf/PdfExportSettingsWindowViewModel.cs
```

---

## Test coverage

PDF tests cover:

```text
basic PDF structure
file writing
page size/orientation behavior
line/circle/text output
solid fill for circles and closed polylines
hidden-layer handling
print-friendly color conversion
Y orientation regression
text rotation regression
dimension export for horizontal, vertical, aligned, radius, diameter and angular dimensions
PDF escaping for dimension symbols such as degree and diameter
custom options from the UI view-model
```

---

## Known limitations

- single-page only;
- fit-to-page only;
- no technical plotting scale yet;
- no layout/paper-space support;
- no font embedding;
- dimension entities are exported as graphical primitives, not as semantic CAD dimension objects;
- arc and angular dimension arc output uses segmented vector approximation;
- PDF output is intended as a clean practical export, not as a full plotting subsystem yet.
