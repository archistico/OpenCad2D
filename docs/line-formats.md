# Line Formats

Line formats define the visual stroke used by layers.

A line format is a named, reusable object that combines:

```text
Id
Name
Color
LineWeight
LineStyle
DashPattern
```

Layers do not directly decide their stroke color, stroke thickness or dash pattern. A layer references one line format through `LineFormatId`; rendering, SVG export and persistence resolve the actual appearance through the document's `LineFormatCollection`.

---

## Core model

The line format system lives in `OpenCad2D.Core.Styling` and `OpenCad2D.Core.Identifiers`.

Main types:

```text
LineFormatId
LineStyle
LineFormat
LineFormatCollection
LineStyleDashPattern
UpdateLineFormatsCommand
```

Relationship:

```text
Entity -> LayerId -> Layer -> LineFormatId -> LineFormat
```

This keeps entities independent from visual stroke settings and keeps visual appearance reusable across multiple layers.

---

## Built-in formats

Every new document starts with these built-in line formats:

| Name | Color | Weight | Style |
|---|---|---:|---|
| Continua | white | 1 | Continuous |
| Asse | red | 0.5 | DashDot |
| Tratteggiata | yellow | 1 | Dashed |
| Tratto due punti | light blue | 0.5 | DashDotDot |
| Tratto e punto | green | 0.75 | DashDot |

Built-in formats are:

- always present in new documents;
- editable;
- renamable;
- not deletable.

User-defined formats can be added and deleted, unless they are currently used by one or more layers.

---

## Line styles

Supported line styles:

```text
Continuous
Dashed
DashDot
DashDotDot
Custom
```

`LineStyle` is the semantic style name. `LineFormat` is the complete reusable appearance object. The actual dash/gap values are stored in `LineFormat.DashPattern`, expressed as a numeric list in drawing/model units.

Current default dash patterns:

| LineStyle | Default pattern | Meaning |
|---|---:|---|
| Continuous | none / `[]` | solid line |
| Dashed | `8, 4` | dash, gap |
| DashDot | `12, 4, 1, 4` | dash, gap, dot, gap |
| DashDotDot | `12, 4, 1, 4, 1, 4` | dash, gap, dot, gap, dot, gap |
| Custom | user-defined | dash/gap pairs |

Pattern values must be positive dash/gap pairs. Missing legacy patterns are reconstructed from the style default during load. Invalid persisted patterns fall back to the style default instead of breaking file loading.

The intended rendering model is that dash patterns are drawing-unit values. The canvas converts model-space dash lengths to screen-space values using the current viewport scale, so dash patterns zoom together with the drawing.

SVG export should write the line format dash pattern to `stroke-dasharray`. DXF currently keeps stable preset style mapping; custom DXF linetype definitions are planned as a later refinement.

---


### Custom pattern persistence

The `.opencad2d.json` format stores line format patterns explicitly:

```json
{
  "id": "Dashed",
  "name": "Tratteggiata",
  "color": "#FFFF00",
  "lineWeight": 0.75,
  "lineStyle": "Dashed",
  "dashPattern": [8, 4]
}
```

A missing `dashPattern` keeps backward compatibility with older files. The loader rebuilds the default pattern from `lineStyle`.

## Layer Manager

The Layer Manager does not edit color or line weight directly anymore.

Each layer row exposes a line format combo box. The selected format controls the visual stroke of entities on that layer.

Layer Manager responsibilities:

```text
create/delete/rename layers
set current layer
toggle visibility
set locked state
choose LineFormatId
apply changes through UpdateLayersCommand
```

The current layer must remain visible and unlocked.

---

## Line Format Manager

The Line Format Manager is opened from the main top bar through `Line formats...`.

The manager uses a compact row-level color picker for visual color selection and keeps the `#RRGGBB` text field for precise/manual input. Both controls update the same line format color.

It allows the user to:

- add a user-defined line format;
- rename a format;
- edit color;
- edit line weight;
- edit line style;
- delete user-defined formats when allowed;
- apply all changes with OK;
- discard all changes with Cancel.

Changes are applied through `UpdateLineFormatsCommand`, so they participate in undo/redo.

---

## Rendering rules

Canvas rendering resolves appearance at draw time:

```text
entity.LayerId
-> document.Layers.GetById(...)
-> layer.LineFormatId
-> document.LineFormats.GetById(...)
-> color, line weight, line style
```

Selection highlighting changes only the highlight color. It must not change line weight or line style.

Hidden layers are not rendered. Locked visible layers are rendered normally.

---

## SVG export rules

SVG export resolves stroke appearance from the line format referenced by the entity layer.

For each exported visible entity:

```text
stroke           -> LineFormat.Color
stroke-width     -> LineFormat.LineWeight
stroke-dasharray -> LineStyleDashPattern, omitted for Continuous
fill             -> none for now
```

Export must not use per-entity style overrides for stroke appearance in this phase.

---

## Persistence rules

Native `.opencad2d.json` files store:

```text
Document.LineFormats
Layer.LineFormatId
```

If a document has no line formats, the serializer falls back to the default collection. If a layer references an unknown line format id, the loader falls back to `Continuous`.

Old layer color and line weight fields are treated only as legacy compatibility data. They are not the active source of rendering or SVG appearance.

---

## Current scope

Implemented:

- reusable line formats;
- built-in default formats;
- layer-to-line-format references;
- canvas rendering through line formats;
- SVG export through line formats;
- JSON persistence;
- Layer Manager combo box;
- Line Format Manager;
- undoable line format updates.

Out of scope for this phase:

- per-entity line format override;
- per-entity line type scale;
- physical print units;
- fill formats;
- text formats;
- dimension formats.

## v0.8.x line style pattern editor

The Line Format Manager now exposes the effective dash pattern of each line format.
The distinction is:

- `LineStyle` describes the stroke style/pattern category, including `Custom`.
- `LineFormat` remains the complete reusable appearance: color, line weight, style and effective dash pattern.

Dash patterns are edited as comma-separated dash/gap pairs in drawing units, for example `8,4` or `12,4,1,4`.
Changing a preset style applies its default pattern. Editing the pattern manually marks the style as `Custom`.
The manager also shows a compact textual preview of the resulting pattern.
