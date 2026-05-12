# Text and Dimensions

This document describes the annotation system implemented in OpenCad2D.

The current annotation model covers:

- single-line text entities;
- reusable text formats;
- reusable dimension styles;
- non-associative basic dimensions;
- canvas rendering, preview, persistence and graphical SVG/DXF export for the implemented dimension types.

---

## Text status

Implemented:

- [x] `TextEntity`;
- [x] `TextTool`;
- [x] single-line text input dialog;
- [x] document-level text formats;
- [x] Text Format Manager;
- [x] text rendering in the canvas;
- [x] selection highlight;
- [x] corrected screen rotation direction;
- [x] grip editing through insertion point;
- [x] snap point at insertion point;
- [x] JSON persistence;
- [x] SVG export;
- [x] DXF export as native `TEXT`;
- [x] tests for entity behavior, formats, persistence, export, tools and rotation direction.

Planned:

- [ ] editable text properties in Property Panel v2;
- [ ] text format selector in a dedicated annotation toolbar or improved top-bar control;
- [ ] rotation grip;
- [ ] more accurate font-aware hit testing if needed;
- [ ] multiline text as a later, separate entity or mode.

---

## TextEntity

`TextEntity` is a semantic CAD entity for single-line annotations.

It stores:

```text
Id                 entity identifier
LayerId            entity layer
InsertionPoint     WCS anchor point
Text               single-line text content
RotationDegrees    rotation angle in degrees
TextFormatId       reference to a reusable TextFormat
```

The entity does **not** store font family, height, color, bold or italic directly. Those properties belong to the referenced `TextFormat`.

```text
TextEntity -> TextFormatId -> Document.TextFormats -> TextFormat
```

---

## TextFormat

Text appearance is controlled by document-level `TextFormat` objects.

A text format contains:

```text
Id
Name
FontFamily
Height
Color
IsBold
IsItalic
```

Built-in formats:

| Id | Name | Default height | Notes |
|---|---|---:|---|
| `Standard` | Standard | 10 | default format |
| `Title` | Title | 18 | bold title format |
| `Annotation` | Annotation | 8 | annotation format |
| `Small` | Small | 6 | small note format |

Heights are expressed in model units and zoom together with the drawing.

The Text Format Manager is opened from the top CAD bar through:

```text
Text formats...
```

It edits name, font family, height, color, bold and italic. Updates are applied through `UpdateTextFormatsCommand`, so they participate in undo/redo.

---

# Basic dimensions

The v0.4 dimension system is implemented as **non-associative** dimensions.

This means a dimension stores the points needed to draw and measure itself. It does not keep a live reference to the entity that was measured.

Example:

```text
If a line is dimensioned and the line is later moved,
the dimension does not automatically update in v0.4.
```

This is deliberate. It keeps the first dimension system stable and testable before future associative dimensions are designed.

---

## Implemented dimension types

Implemented:

- [x] horizontal dimension;
- [x] vertical dimension;
- [x] aligned dimension;
- [x] radius dimension;
- [x] diameter dimension;
- [x] angular dimension;
- [x] minor angular dimensions;
- [x] reflex angular dimensions greater than 180°.

---

## Dimension tools

### Horizontal Dimension

Workflow:

```text
activate Horizontal Dim
pick first measured point
pick second measured point
pick dimension-line placement point
```

The measured value is:

```text
abs(second.X - first.X)
```

The automatic label uses the active dimension style decimal settings.

### Vertical Dimension

Workflow:

```text
activate Vertical Dim
pick first measured point
pick second measured point
pick dimension-line placement point
```

The measured value is:

```text
abs(second.Y - first.Y)
```

### Aligned Dimension

Workflow:

```text
activate Aligned Dim
pick first measured point
pick second measured point
pick dimension-line placement point
```

The measured value is the true distance between the two measured points.

### Radius Dimension

Workflow:

```text
activate Radius Dim
pick center point
pick point on circle
pick text placement point
```

The automatic label uses the `R` prefix:

```text
R 25.00
```

### Diameter Dimension

Workflow:

```text
activate Diameter Dim
pick center point
pick point on circle
pick text placement point
```

The automatic label uses the `Ø` prefix:

```text
Ø 50.00
```

### Angular Dimension

Workflow:

```text
activate Angular Dim
pick angle center
pick point on first ray
pick point on second ray
pick arc placement point
```

The fourth click chooses the measured angular sector.

If the arc placement point falls inside the counter-clockwise sweep from the first ray to the second ray, the dimension uses that counter-clockwise sweep. Otherwise it uses the clockwise sweep. This supports both minor and reflex angles.

Examples:

```text
90.00°
270.00°
```

---

## DimensionStyle

`DimensionStyle` is a reusable document-level configuration object. It references an existing text format through `TextFormatId`, so dimension text appearance remains integrated with the text format system.

Current properties:

```text
Id
Name
TextFormatId
ArrowSize
TextOffset
ExtensionLineOffset
ExtensionLineOvershoot
DecimalPlaces
DecimalSeparator
Suffix
```

The important rule is:

```text
Do not duplicate style settings inside every dimension entity.
```

Use reusable style objects instead.

Current default:

```text
DimensionStyle.Standard -> TextFormat.Annotation
```

---

## Dimension rendering

Canvas rendering uses a shared renderer-agnostic model:

```text
DimensionEntity
-> DimensionStyle
-> TextFormat
-> DimensionGeometryBuilder
-> DimensionRenderModel
```

The render model contains:

- dimension line primitives;
- extension line primitives;
- arrow line primitives;
- arc primitives for angular dimensions;
- measurement text primitive;
- computed bounds.

This avoids duplicating the geometry calculations in canvas rendering, SVG export and DXF export.

---

## Dimension export

Dimension export in v0.4 is graphical, not associative.

SVG export writes:

```text
line primitives -> <line>
arc primitives  -> <path>
measurement     -> <text>
```

DXF export writes:

```text
line primitives -> LINE
arc primitives  -> ARC
measurement     -> TEXT
```

The exported drawing is visually compatible with external viewers, but the dimensions are not native editable DXF `DIMENSION` records yet.

Native DXF `DIMENSION` export remains a future interoperability improvement.

---

## Dimension persistence

The native `.opencad2d.json` format stores:

```text
dimensionStyles[]
entities[] with dimension-specific DTOs
```

Implemented dimension entity types:

```text
LinearDimension
AlignedDimension
RadiusDimension
DiameterDimension
AngularDimension
```

The serialized dimension stores only geometric definition, style id and optional text override. Style details are kept in `dimensionStyles`.

---

## Dimension robustness

Implemented edge-case tests cover:

- zero-length horizontal dimensions;
- zero-length vertical dimensions;
- aligned dimensions with coincident points;
- radius dimensions with zero radius;
- diameter dimensions with zero radius;
- angular dimensions with invalid ray points;
- angular dimensions with coincident rays;
- negative coordinates;
- very small dimensions;
- very large dimensions;
- `DistanceTo` behavior on dimension geometry;
- transformation behavior;
- rotated horizontal/vertical dimensions becoming aligned dimensions when they are no longer axis-aligned;
- mirrored angular dimensions flipping sweep direction to preserve the measured angle.

---

## Current v0.4 decisions

- [x] dimensions are non-associative;
- [x] DXF export writes dimensions as graphical primitives;
- [x] horizontal and vertical dimensions use separate tools;
- [x] angular dimensions support angles greater than 180°;
- [x] rendering/export share `DimensionGeometryBuilder`;
- [x] dimensions use `DimensionStyle.TextFormatId` for label appearance.

---

## Future dimension work

Planned after v0.4:

- [ ] editable dimension properties in Property Panel v2;
- [ ] Dimension Style Manager window;
- [ ] dimension grip editing;
- [ ] native DXF `DIMENSION` export;
- [ ] associative dimensions;
- [ ] richer text placement rules;
- [ ] alternate units and tolerances;
- [ ] arrowhead styles;
- [ ] multiline dimension text.
