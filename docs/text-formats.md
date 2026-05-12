# Text Formats

Text formats define reusable appearance for single-line text annotations.

They are the text equivalent of line formats: the drawing stores a named configuration once, and text entities reference it by id.

---

## Core model

The text format system lives mainly in:

```text
OpenCad2D.Core.Identifiers.TextFormatId
OpenCad2D.Core.Styling.TextFormat
OpenCad2D.Core.Styling.TextFormatCollection
OpenCad2D.Core.Commands.UpdateTextFormatsCommand
```

Relationship:

```text
TextEntity -> TextFormatId -> CadDocument.TextFormats -> TextFormat
```

This keeps each `TextEntity` small and makes text appearance reusable.

---

## TextFormat properties

A `TextFormat` contains:

```text
Id
Name
FontFamily
Height
Color
IsBold
IsItalic
```

`Height` is a model-space height. It is not a screen pixel size.

This means a text with height `10` behaves like CAD geometry: it becomes visually larger or smaller as the viewport zoom changes.

---

## Built-in formats

Every new document starts with these built-in text formats:

| Id | Name | Font | Height | Color | Style |
|---|---|---|---:|---|---|
| `Standard` | Standard | Arial | 10 | white | regular |
| `Title` | Title | Arial | 18 | white | bold |
| `Annotation` | Annotation | Arial | 8 | yellow | regular |
| `Small` | Small | Arial | 6 | gray | regular |

Built-in formats are:

- always present in new documents;
- editable;
- renamable;
- not deletable.

User-defined formats can be added and deleted, unless they are currently used by one or more `TextEntity` instances.

---

## Text Format Manager

The Text Format Manager is opened from the main top bar through:

```text
Text formats...
```

It allows the user to:

- add a user-defined text format;
- rename a format;
- edit font family;
- edit text height;
- edit color as a hex value;
- toggle bold;
- toggle italic;
- preview the result;
- delete user-defined formats when allowed;
- apply all changes with OK;
- discard all changes with Cancel.

Changes are applied through `UpdateTextFormatsCommand`, so they participate in undo/redo.

---

## Current text format during insertion

`TextTool` uses the current text format stored in tool creation settings.

Workflow:

```text
activate Text
pick insertion point
text input dialog opens
choose format
confirm
TextEntity.TextFormatId = selected format id
```

After insertion, the selected text format becomes the current text format for the next text insertion.

---

## Rendering rules

Canvas rendering resolves text appearance at draw time:

```text
TextEntity.TextFormatId
-> document.TextFormats.GetById(...)
-> font family, model-space height, color, bold, italic
```

If an entity is selected, selection highlighting overrides the normal text color. It must not modify the stored text format.

Hidden layer text is not rendered. Locked visible layer text is rendered normally but is not selectable/editable.

---

## SVG export rules

SVG export resolves text appearance from the referenced text format.

For each exported visible text entity:

```text
<text>
  x/y              -> insertion point transformed to SVG space
  font-family      -> TextFormat.FontFamily
  font-size        -> TextFormat.Height
  fill             -> TextFormat.Color
  font-weight      -> bold when IsBold is true
  font-style       -> italic when IsItalic is true
  transform        -> rotation when needed
```

Text content must be XML-escaped.

---

## DXF export rules

DXF export writes single-line text as native `TEXT`.

The exported entity uses:

```text
TEXT
layer name
insertion point
text height
text content
rotation angle
text format/style name
```

The current export intentionally does not use `MTEXT` because OpenCad2D currently supports only single-line text.

---

## Persistence rules

Native JSON stores text formats at document level:

```text
DocumentDto.TextFormats[]
```

Each text entity stores only:

```text
TextFormatId
```

Loading rules:

- if `TextFormats` is missing or empty, use the default collection;
- if a text entity references an unknown format, the serializer/application should fall back to `Standard` where needed.

Saving rules:

- save `Document.TextFormats`;
- save each `TextEntity.TextFormatId`;
- do not duplicate font, height or color inside every text entity.

---

## Design rule

Do not move font, height or color directly into `TextEntity` unless there is a strong reason.

The intended model is:

```text
TextEntity = content + geometry + format reference
TextFormat = reusable appearance
```
