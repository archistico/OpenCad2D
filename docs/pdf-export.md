# PDF Export

OpenCad2D can export the current drawing to a single-page vector PDF.

PDF export is an output feature. It does not save the OpenCad2D document, does not change `CurrentFilePath` and does not clear the dirty marker.

---

## Scope

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
| `CircleEntity` | cubic Bézier circle approximation |
| `PointEntity` | small marker |
| `ArcEntity` | segmented vector path |
| `PolylineEntity` | vector polyline / closed path |
| `TextEntity` | text output using built-in PDF font fallback |
| dimensions | graphical primitives where supported |

The exporter exports visible model geometry by default.

---

## Layer behavior

```text
hidden layers         -> ignored by default
visible locked layers -> exported
visible normal layers -> exported
```

`PdfExportOptions.IncludeHiddenLayers` can include hidden-layer entities when explicitly enabled.

---

## Line formats

PDF export uses the effective line format:

- color;
- lineweight;
- dash pattern where supported.

The effective dash pattern comes from `LineFormat.DashPattern`.
