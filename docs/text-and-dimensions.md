# Text and Dimensions

This document describes the annotation direction for OpenCad2D.

Text is now implemented as single-line annotation text. Dimensions are still planned and will be built on top of the same general principles: semantic entities, reusable styles and undoable commands.

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
- [x] grip editing through insertion point;
- [x] snap point at insertion point;
- [x] JSON persistence;
- [x] SVG export;
- [x] DXF export as `TEXT`;
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

This keeps text appearance reusable and consistent:

```text
TextEntity -> TextFormatId -> Document.TextFormats -> TextFormat
```

---

## Single-line rule

The current implementation intentionally supports only single-line text.

This means:

- the text input window is single-line;
- DXF export uses native `TEXT`, not `MTEXT`;
- SVG export uses one `<text>` element;
- there is no paragraph layout;
- there are no per-character rich text runs.

Multiline text should not be added by stretching `TextEntity` too far. When needed, it should be designed explicitly, probably as a future `MTextEntity` or as a clearly separated multiline mode.

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

Heights are expressed in model units. They zoom together with the drawing, like other CAD geometry.

Built-in formats are editable but not deletable. User-defined formats can be deleted only when no text entity uses them.

---

## Text Format Manager

The Text Format Manager is opened from the top CAD bar through:

```text
Text formats...
```

It allows the user to edit:

- format name;
- font family;
- text height;
- color as hex value;
- bold;
- italic.

It also shows a simple preview.

Changes are applied through `UpdateTextFormatsCommand`, so they participate in undo/redo.

The manager follows the same design idea as the Line Format Manager:

```text
Reusable document-level style -> manager window -> undoable command
```

---

## TextTool

`TextTool` places a `TextEntity`.

Workflow:

```text
activate Text tool
pick insertion point
enter single-line text
choose text format
enter rotation if needed
OK -> execute AddEntityCommand
Cancel -> no entity is created
```

The text input window is opened asynchronously by the Avalonia app layer. The tool itself stays UI-independent through `ITextInputProvider`.

Dependency rule:

```text
OpenCad2D.Tools knows ITextInputProvider
OpenCad2D.App implements the Avalonia dialog provider
```

This keeps tools independent from Avalonia controls and windows.

---

## Text rendering

Canvas rendering resolves the text format at draw time:

```text
TextEntity.TextFormatId
-> document.TextFormats.GetById(...)
-> font family, height, color, bold, italic
```

The rendered text:

- uses model-space height converted through the current viewport scale;
- rotates around the insertion point;
- uses the selected entity highlight color when selected;
- uses the format color when not selected;
- remains visible only when its layer is visible.

Text rotation follows the expected CAD/user-facing direction in the canvas. Dedicated tests in `CadTextTransformTests` protect this behavior.

---

## Hit testing and bounds

`TextEntity` currently uses an estimated bounding box.

The estimate is based on:

```text
height = default estimated height or resolved text format height where available
width  = max(height, Text.Length * height * 0.6)
```

The rectangle is rotated according to `RotationDegrees` and then converted to an axis-aligned bounding box.

This is sufficient for v0.3 selection, culling and basic hit testing. A future text geometry service may make this font-aware if practical editing requires it.

---

## Grip editing and snapping

Current behavior:

- one grip at `InsertionPoint`;
- moving the grip moves the text;
- the operation is undoable through entity replacement;
- the insertion point is exposed as a snap point.

Future behavior:

- optional rotation grip;
- optional baseline/end grip;
- editable text content and format in Property Panel v2.

---

## Persistence

Native JSON stores text formats at document level and stores only `TextFormatId` inside text entities.

Conceptual shape:

```text
DocumentDto
  TextFormats[]
  Entities[]

TextFormatDto
  Id
  Name
  FontFamily
  Height
  Color
  IsBold
  IsItalic

TextEntityDto
  Type = Text
  Text
  InsertionX
  InsertionY
  RotationDegrees
  TextFormatId
```

If old files do not contain text formats, the serializer falls back to the default text format collection.

---

## Export

### SVG

`TextEntity` exports as SVG `<text>`.

The exporter writes:

- position;
- escaped text content;
- font family;
- font size;
- fill color;
- bold/italic style when enabled;
- rotation transform.

### DXF

`TextEntity` exports as native DXF `TEXT`.

Important DXF groups:

```text
0   TEXT
8   layer name
10  insertion X
20  insertion Y
30  insertion Z = 0
40  text height
1   text content
50  rotation degrees
7   text style / format name
```

The current implementation targets simple single-line interoperability. Multiline text should use a separate future design and likely DXF `MTEXT`.

---

## Dimensions

Dimensions are planned but not implemented yet.

The design goal is that dimensions should be semantic annotation entities, not groups of primitive lines and text.

A dimension entity should store definition points and a style reference, then compute its visual representation at render/export time.

---

## Planned dimension entities

### Linear and aligned dimension

Measures the distance between two definition points.

Planned definition data:

```text
DefinitionPoint1
DefinitionPoint2
DimensionLinePoint
DimensionStyleId
```

### Angular dimension

Measures an angle defined by three points or two selected entities.

### Radius dimension

Measures a circle or arc radius.

### Diameter dimension

Measures a circle or arc diameter.

---

## Dimension style direction

A future `DimensionStyle` should probably reference or embed text appearance rules.

Potential properties:

```text
Name
TextFormatId
ArrowSize
ExtensionLineOffset
ExtensionLineExtension
DecimalPrecision
UnitSuffix
ScaleFactor
```

The important rule is the same as text and line formats:

```text
Do not duplicate style settings inside every dimension entity.
```

Use reusable style objects instead.
