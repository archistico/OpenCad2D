# OpenCad2D v0.4 - Basic Dimensions release notes

This document summarizes the v0.4 development milestone.

v0.4 focuses on basic dimensioning and the final polish needed to make the first dimension system usable in normal drawing workflows.

---

## Scope

Implemented dimension types:

- [x] horizontal dimension;
- [x] vertical dimension;
- [x] aligned dimension;
- [x] radius dimension;
- [x] diameter dimension;
- [x] angular dimension;
- [x] angular dimensions greater than 180°.

---

## Main architectural decisions

- [x] Dimensions are non-associative in v0.4.
- [x] DXF export writes dimensions as graphical primitives.
- [x] Horizontal and vertical dimensions use separate tools.
- [x] Angular dimensions support minor and reflex sweeps.
- [x] Dimension text uses `DimensionStyle.TextFormatId`.
- [x] Canvas rendering, SVG export and DXF export share `DimensionGeometryBuilder`.

---

## Implemented entities

- [x] `DimensionStyle`;
- [x] `DimensionStyleCollection`;
- [x] `LinearDimensionEntity`;
- [x] `AlignedDimensionEntity`;
- [x] `RadiusDimensionEntity`;
- [x] `DiameterDimensionEntity`;
- [x] `AngularDimensionEntity`.

---

## Implemented tools

- [x] `HorizontalDimensionTool`;
- [x] `VerticalDimensionTool`;
- [x] `AlignedDimensionTool`;
- [x] `RadiusDimensionTool`;
- [x] `DiameterDimensionTool`;
- [x] `AngularDimensionTool`.

All dimension tools support preview and commit changes through undoable commands.

---

## Persistence

The native `.opencad2d.json` format now persists:

- [x] document-level `dimensionStyles`;
- [x] linear dimensions;
- [x] aligned dimensions;
- [x] radius dimensions;
- [x] diameter dimensions;
- [x] angular dimensions.

---

## Export

SVG export now includes all implemented v0.4 dimensions as graphical primitives.

DXF export now includes all implemented v0.4 dimensions as graphical primitives:

```text
Lines/arrows/leaders -> LINE
Angular arcs         -> ARC
Measurement labels   -> TEXT
```

The DXF output is visually compatible but does not yet create native editable `DIMENSION` records.

---

## Tests

v0.4 added or extended tests for:

- [x] dimension styles;
- [x] dimension entities;
- [x] dimension geometry builder;
- [x] placement tools;
- [x] undo/redo behavior;
- [x] JSON round-trip persistence;
- [x] SVG export;
- [x] DXF export;
- [x] degenerate dimension cases;
- [x] transform robustness;
- [x] Trim/Extend highlighted previews.

---

## UI polish

- [x] Left tool panel reorganized into two columns.
- [x] Select, Draw, Dimension and Measure are grouped together.
- [x] Edit tools are separated into the second column.
- [x] Tool buttons are normalized in width.
- [x] Status bar color is aligned with the rest of the dark UI.
- [x] Horizontal, vertical and aligned dimension text placement was refined.

---

## Known limitations

- Dimensions are not associative.
- Dimension properties are read-only in Property Panel v1.
- There is no Dimension Style Manager window yet.
- DXF dimensions are graphical primitives, not native `DIMENSION` records.
- Dimension grip editing is not implemented yet.
- Trim/Extend highlighted preview is detailed for line targets; arcs, circles and polylines still use the normal result preview.

---

## Recommended next milestone

v0.5 should focus on advanced editing and refinement:

- Trim with two cutting edges;
- Break Point on arcs, circles and polylines;
- Break Segment on arcs, circles and polylines;
- improved Extend on all supported entity types;
- broader highlighted previews for Trim/Extend;
- locked/hidden layer behavior tests for all modify tools;
- no regressions in undo/redo behavior.
