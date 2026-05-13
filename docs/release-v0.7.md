# OpenCad2D v0.7 - Interoperability: DXF import, PDF export and SVG options

v0.7 is the first interoperability-focused OpenCad2D release.

The main goal of this release is to make OpenCad2D more useful in real workflows: import simple DXF drawings, edit supported 2D geometry, save the result as `.opencad2d.json`, and export to DXF, SVG or PDF.

---

## Highlights

- ASCII DXF import pipeline;
- DXF import for base 2D entities;
- DXF layer table import;
- DXF import diagnostics and report window;
- DXF export/import round-trip regression tests;
- single-page vector PDF export;
- PDF export settings window;
- print-friendly PDF color handling;
- corrected PDF Y-orientation behavior;
- SVG export background options;
- SVG layer grouping;
- updated interoperability documentation.

---

## DXF import

OpenCad2D can now import a focused ASCII DXF subset.

Supported DXF entities:

```text
LINE
CIRCLE
ARC
POINT
LWPOLYLINE
TEXT
```

Supported layer behavior:

```text
TABLES/LAYER import
basic ACI color mapping
basic linetype mapping
lineweight group code 370
negative ACI color as hidden/off
frozen layers as hidden
locked layers as locked
automatic layer creation for undeclared referenced layers
```

Unsupported entities are skipped with readable diagnostics instead of crashing the import.

Examples of intentionally unsupported DXF content in v0.7:

```text
DWG
binary DXF
BLOCK / INSERT
native DIMENSION
HATCH
SPLINE
ELLIPSE
MTEXT
paper-space layouts
viewports
```

---

## DXF import UI

The file command bar now includes:

```text
Import DXF
```

Import behavior:

```text
Import DXF replaces the current document.
Unsaved changes are protected by the existing Save / Don't Save / Cancel flow.
Successful import creates an unsaved OpenCad2D document.
CurrentFilePath is cleared.
The document is marked dirty.
```

This is intentional because DXF is an interoperability format, not the native editable OpenCad2D save format.

When an import has warnings or errors, OpenCad2D shows a dedicated DXF Import Report window with:

```text
imported entity count
layer count
warning count
error count
skipped record count
entity counts by kind
diagnostic entries with severity and line number
```

---

## DXF round-trip validation

v0.7 adds regression tests for this workflow:

```text
OpenCad2D document -> DXF export -> DXF import -> semantic comparison
```

Round-trip coverage includes:

```text
LINE
CIRCLE
POINT
ARC
LWPOLYLINE
TEXT
layers
hidden/locked layer state where supported
import statistics
```

The normal DXF exporter keeps its existing viewer-friendly coordinate conversion. Round-trip tests use an internal model-coordinate export option so tests can compare geometry deterministically.

---

## PDF export

OpenCad2D can now export a single-page vector PDF.

Supported PDF options:

```text
A4 / A3 / A2 / A1 / A0
Portrait / Landscape
margin in millimeters
fit-to-page
include hidden layers
print-friendly colors
```

PDF export is available from:

```text
Export PDF
```

The command opens a settings window before the save-file picker.

The default output is designed for printing:

```text
white page
fit-to-page drawing
light screen colors converted to black
visible layers only
```

The PDF exporter is dependency-free and writes a minimal PDF 1.4 file from `OpenCad2D.Export`.

---

## SVG export improvements

v0.7 improves SVG export with configurable options.

New background modes:

```text
CanvasDark
White
Transparent
```

New layer grouping option:

```xml
<g id="layer-Walls" data-layer-name="Walls">
  ...
</g>
```

This makes exported SVG files more useful in web, documentation and vector-editor workflows.

---

## Documentation updated

Updated or added documents:

```text
README.md
docs/roadmap.md
docs/export.md
docs/dxf-export.md
docs/dxf-import.md
docs/pdf-export.md
docs/svg-export.md
docs/ai-handoff.md
docs/v0.7-interoperability-plan.md
docs/release-v0.7.md
```

---

## Known limitations

DXF import remains intentionally limited:

- no DWG import;
- no binary DXF import;
- no block insertion support;
- no native DXF dimension import;
- no hatches;
- no multiline text;
- no spline or ellipse import;
- no paper-space/layout reconstruction;
- no bulge-to-arc conversion for `LWPOLYLINE` yet.

PDF export remains intentionally simple:

- single-page only;
- fit-to-page only;
- no technical plotting scales yet;
- no layout/paper-space support;
- no font embedding.

---

## Suggested verification before GitHub release

Run:

```bash
dotnet clean
dotnet build
dotnet test
```

Manual smoke test:

```text
1. create a small drawing with layers, text, lines, circles, arcs and polylines;
2. export DXF;
3. import that DXF again;
4. export PDF;
5. export SVG with transparent background;
6. save as .opencad2d.json;
7. reopen the saved file.
```

---

## Next milestone

```text
v0.8 - UI, colors and settings
```

Recommended focus:

- application settings;
- last-file persistence;
- default export settings persistence;
- color picker improvements;
- final visual theme polish;
- draw order / Z-order independent from layers.
