# Line Formats

Line formats define the visual stroke used by layers.

A line format is a named, reusable object that combines:

```text
Id
Name
Color
LineWeight
LineStyle
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
```

Dash patterns are expressed in model units. OpenCad2D does not assume that model space means millimetres, metres or another physical unit.

Current dash patterns:

| LineStyle | Pattern | Meaning |
|---|---:|---|
| Continuous | none | solid line |
| Dashed | `6, 3` | dash, gap |
| DashDot | `6, 2, 1, 2` | dash, gap, dot, gap |
| DashDotDot | `6, 2, 1, 2, 1, 2` | dash, gap, dot, gap, dot, gap |

The canvas converts these model-space dash lengths to screen-space values using the current viewport scale. Therefore dash patterns zoom together with the drawing.

SVG export writes the same logical dash pattern to `stroke-dasharray`.

---

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
