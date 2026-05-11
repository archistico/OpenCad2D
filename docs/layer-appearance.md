# Layer Appearance

This document records the current design for layer-based appearance in OpenCad2D.

---

## Current implemented model

Layers now reference reusable line formats instead of directly storing their active stroke appearance.

Current layer responsibilities:

```text
Id
Name
LineFormatId
IsVisible
IsLocked
```

The visual stroke is resolved through the document's `LineFormatCollection`:

```text
Entity -> LayerId -> Layer -> LineFormatId -> LineFormat
```

A `LineFormat` provides:

```text
Name
Color
LineWeight
LineStyle
```

This means several layers can share the same visual format, and a single edit to the format can update all layers that use it.

---

## Design rule: entities stay ByLayer

The current rule is:

```text
entities store geometry + layer reference
layers reference reusable appearance formats
line formats store visual stroke information
```

Individual entities should not carry their own active color, line weight or line style in this phase.

This avoids ambiguity between per-entity overrides and layer-based appearance. It also keeps rendering, SVG export and persistence simpler.

---

## Built-in line formats

Every new document starts with these line formats:

| Name | Color | Weight | Style |
|---|---|---:|---|
| Continua | white | 1 | Continuous |
| Asse | red | 0.5 | DashDot |
| Tratteggiata | yellow | 1 | Dashed |
| Tratto due punti | light blue | 0.5 | DashDotDot |
| Tratto e punto | green | 0.75 | DashDot |

Built-in formats cannot be deleted, but they can be renamed and edited.

---

## Layer Manager

The Layer Manager is a dedicated window opened from the `Layers...` button.

It allows editing:

- layer name;
- visibility;
- locked state;
- current layer;
- selected line format.

It no longer edits color and line weight directly. Those values belong to line formats and are edited in the Line Format Manager.

Rules:

- layer `0` is protected;
- layer `0` cannot be deleted or renamed;
- the current layer cannot be hidden or locked;
- layer names must be non-empty and unique;
- layers with entities cannot be deleted;
- every layer must reference an existing line format.

---

## Line Format Manager

The Line Format Manager is opened from `Line formats...`.

It allows editing:

- format name;
- color;
- line weight;
- line style;
- user-defined format creation;
- user-defined format deletion when allowed.

Line format changes are applied through an undoable command:

```text
Line Format Manager -> UpdateLineFormatsCommand -> undo/redo
```

Layer changes remain separate:

```text
Layer Manager -> UpdateLayersCommand -> undo/redo
```

---

## Rendering

Canvas rendering resolves the stroke at render time.

For each visible entity:

```text
layer = document.Layers.GetById(entity.LayerId)
format = document.LineFormats.GetById(layer.LineFormatId)
```

Then the renderer uses:

```text
format.Color
format.LineWeight
format.LineStyle
```

Selected entities keep the same line weight and line style. Selection changes only the highlight color.

---

## SVG export

SVG export uses the same resolution rule as the canvas:

```text
Entity -> Layer -> LineFormat
```

SVG attributes are generated from the resolved format:

```text
stroke           -> LineFormat.Color
stroke-width     -> LineFormat.LineWeight
stroke-dasharray -> LineStyleDashPattern, omitted for Continuous
```

Hidden layers are ignored. Locked visible layers are exported normally.

---

## Persistence

Native persistence stores line formats in the document and stores only `LineFormatId` on layers.

Conceptually:

```text
DocumentDto.LineFormats
LayerDto.LineFormatId
```

If a document has no line formats, the loader falls back to the default line format collection. If a layer references an unknown format, the loader falls back to `Continuous`.

Old color and line weight layer fields are legacy compatibility data only. They are not the active source of appearance.

---

## Future appearance features

Future layer/model appearance work may add:

```text
FillColor
DrawOrder
per-layer fill behavior
text formats
dimension formats
```

Draw order should still be layer-based unless a future design decision explicitly introduces per-entity ordering.

Fill should apply only to fillable closed entities:

```text
CircleEntity
PolylineEntity with IsClosed = true
```

Open polylines and lines are never filled.

---

## Assign selected entities to current layer

The top CAD bar exposes an `Assegna` button immediately before the current-layer ComboBox.

Behavior:

```text
select one or more entities
choose the target current layer
click Assegna
selected entities receive the current layer id
```

Rules:

- entities already on the current layer are skipped;
- no command is created when there is nothing to change;
- the operation is undoable;
- selection is preserved after assignment;
- geometry and entity ids are preserved.
