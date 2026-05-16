# Export

OpenCad2D supports export to SVG, DXF and PDF.

Export is not native save. Export commands do not change `CurrentFilePath` and do not clear the dirty state. Use `.opencad2d.json` for native save/reopen.

---

## SVG export

SVG export supports:

- points;
- text;
- lines;
- circles;
- arcs;
- polylines;
- basic dimensions as graphical primitives;
- layer grouping;
- selectable background modes;
- effective line format colors, weights and dash patterns.

Dash patterns are emitted through `stroke-dasharray` when the effective line format has a non-empty pattern.

---

## DXF export

DXF export targets practical AutoCAD 2000 ASCII 2D output.

Current scope:

- base 2D entities;
- layer records;
- colors, lineweights and known linetype mappings;
- basic dimensions as graphical primitives where supported.

Known limitation: arbitrary custom line format dash patterns are not yet emitted as custom DXF `LTYPE` definitions. Known presets are mapped to standard linetypes.

---

## PDF export

PDF export creates a single-page vector PDF with page size, orientation, margins, fit-to-page and print-friendly color options.

PDF export uses effective line format information where supported.

---

## Layer behavior

Default export behavior:

```text
hidden layers         -> ignored
visible locked layers -> exported
visible normal layers -> exported
```

Export options may allow hidden layers to be included explicitly.

---

## Coordinate orientation

SVG and DXF export are tested to preserve expected visual orientation for OpenCad2D workflows. Y-orientation issues should be covered by regression tests whenever export transforms are changed.

---

## Dimensions

Dimensions are currently non-associative. Exported dimensions are graphical representations rather than fully associative native CAD dimensions.

---

## Future work

- PNG export;
- custom DXF `LTYPE` definitions for arbitrary line format dash patterns;
- stronger manual compatibility testing in LibreCAD, QCAD and Autodesk viewers;
- richer technical plotting and scale workflows.
