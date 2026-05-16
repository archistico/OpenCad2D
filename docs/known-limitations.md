# Known Limitations

OpenCad2D is still an early prototype. The following limitations should remain visible until they are resolved.

---

## Native save vs export

`.opencad2d.json` is the native save format. DXF/SVG/PDF are export/interchange formats and may not preserve every OpenCad2D-specific concept.

---

## DXF

Current DXF support targets practical AutoCAD 2000 ASCII 2D interoperability.

Known limits:

- custom DXF `LTYPE` generation for arbitrary line format dash patterns is future work;
- some OpenCad2D dimensions are exported as graphical primitives rather than full associative CAD dimensions;
- broad compatibility should still be manually verified in LibreCAD, QCAD and Autodesk viewers.

---

## Dimensions

Dimensions are currently non-associative. Editing the measured geometry does not automatically update existing dimension entities.

---

## Offset

Offset supports lines, circles, arcs and straight-segment polylines.

Known limits:

- polyline offset uses miter joins only;
- rounded joins are not implemented;
- polyline bulge/arc segments are not implemented;
- advanced self-intersection cleanup is limited.

---

## Fillet

Fillet currently supports Line-Line only.

Future work:

- Line-Arc;
- Arc-Arc;
- polyline fillet;
- NoTrim option.

---

## Trim and Extend

Trim has an advanced base workflow with All and Undo, but advanced CAD modes such as Fence, Crossing, Project and Edge are not implemented yet.

Extend supports selected entity types only and intentionally does not extend closed entities like circles.

---

## Command input

The command input supports coordinate and option workflows, but future improvements remain:

- command history navigation;
- autocomplete;
- richer right-click behavior;
- additional option sets for advanced tools.

---

## Export formats

SVG, DXF and PDF are implemented. PNG export is planned.
