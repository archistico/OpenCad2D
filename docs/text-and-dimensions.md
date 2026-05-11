# Text and Dimensions

This document covers two kinds of annotation entities: text and dimensions.

Text entities display free-form annotated content with basic Markdown-style inline formatting. Dimension entities measure geometry and display the result with extension lines, arrows and a formatted value.

---

## Text

### Main idea

Text entities allow the user to annotate drawings with readable labels, titles and notes.

Formatting uses a small subset of Markdown syntax. This keeps the text model simple while allowing enough visual variety for practical annotation work. There is no embedded rich text format, no font table and no per-character attribute list.

---

### Supported formatting

Only inline formatting and heading levels are supported.

| Syntax | Result |
|---|---|
| `**bold**` | bold text |
| `*italic*` | italic text |
| `_italic_` | italic text (alternate) |
| `__underline__` | underlined text |
| `# Heading` | heading level 1 (largest) |
| `## Heading` | heading level 2 |
| `### Heading` | heading level 3 |

Heading prefixes apply to the entire line they appear on. Inline styles can be combined within the same line.

Unsupported Markdown constructs (lists, tables, code blocks, links, images) are rendered as literal text.

---

### TextEntity

`TextEntity` stores:

```text
Id              entity identifier
LayerId         controls text appearance through the layer line format
InsertionPoint  WCS anchor point
RawText         the raw Markdown-formatted string
TextHeight      base character height in model units
Rotation        angle in degrees, counterclockwise
HorizontalAlign left, center, right (relative to InsertionPoint)
VerticalAlign   bottom, middle, top (relative to InsertionPoint)
```

The text entity does not store pre-parsed formatting tokens. Parsing happens at render time. This keeps the entity model stable even if the parser is extended later.

`TextHeight` and `Rotation` are per-entity because they are geometric properties. Text stroke/fill color should resolve through the layer line format unless a later text-format system overrides this rule.

---

### Heading size scaling

Heading levels scale relative to `TextHeight`:

```text
Normal text    TextHeight × 1.0
### Heading 3  TextHeight × 1.25
## Heading 2   TextHeight × 1.5
# Heading 1    TextHeight × 2.0
```

These multipliers can be defined as constants in the renderer and adjusted later.

---

### TextTool

`TextTool` places a `TextEntity`.

Workflow:

```text
activate Text tool
pick insertion point
type text content in a dedicated input area or in the command line
confirm with Enter
execute AddEntityCommand
```

The text input area should support multi-line input before confirmation. Each newline in the input becomes a line break in the rendered text.

After confirmation, `AddEntityCommand` adds the entity to the document.

The default `TextHeight` is taken from `DrawingSettings`. The default rotation is 0.

---

### Text rendering

The renderer parses `RawText` at render time and produces a sequence of styled runs and line breaks.

For each line, the heading level determines the character height. Within a line, inline styles determine bold, italic and underline attributes.

Text is rendered using the color resolved from the layer line format. Text fill/background formats are not supported at this stage.

Text entities are selectable and can be moved, copied, deleted and grip-edited. Grip editing can expose the insertion point as a grip.

---

## Dimensions

### Main idea

Dimensions are semantic annotation entities that measure geometric distances and display the result with extension lines, arrows and formatted text.

A dimension entity is not a group of lines and text. It is a single entity that stores definition points and computes its visual representation at render time. This means the measured value is always derived from the definition points and never stored separately.

---

### DimensionEntity

`DimensionEntity` stores:

```text
Id               entity identifier
LayerId          controls dimension color
DimensionType    the kind of dimension
DefinitionPoints list of WCS points that define the measurement
TextPositionOverride  optional WCS point to reposition the label
DimensionStyleId reference to a named DimensionStyle
```

The exact number and meaning of definition points depends on the dimension type.

The rendered arrows, extension lines and text are all computed from the definition points at render time. Changing a definition point (via grip edit) updates the measurement and all visual elements automatically.

---

### Dimension types

#### Linear dimension

A linear dimension measures the horizontal, vertical or aligned distance between two definition points.

Definition points:

```text
DefinitionPoint1   first measured point (WCS)
DefinitionPoint2   second measured point (WCS)
DimensionLinePoint a point on the dimension line, determines offset and mode
```

The dimension line point position determines the mode:

- if aligned horizontally with both definition points → horizontal dimension
- if aligned vertically with both definition points → vertical dimension
- otherwise → aligned dimension (measures direct distance)

Rendered elements:

```text
two extension lines from definition points to dimension line
dimension line with arrows at each end
text label showing the measured value centered on the dimension line
```

#### Radius dimension

A radius dimension measures the radius of a circle or arc.

Definition points:

```text
CenterPoint    center of the circle or arc
RadiusPoint    a point on the circle or arc edge, determines text position
```

Rendered elements:

```text
leader line from center toward edge
arrow at the RadiusPoint end
text label with 'R' prefix and measured value
```

#### Diameter dimension

A diameter dimension measures the diameter of a circle or arc.

Definition points:

```text
CenterPoint      center of the circle or arc
DiameterPoint1   one end of the diameter line
DiameterPoint2   opposite end of the diameter line
```

Rendered elements:

```text
line spanning the full diameter
arrows at both ends
text label with diameter symbol (Ø) prefix and measured value
```

---

### Dimension tools

#### LinearDimensionTool

Workflow:

```text
activate Linear Dimension
pick first definition point
pick second definition point
move pointer to set dimension line position and mode (horizontal / vertical / aligned)
click to confirm
execute AddEntityCommand
```

While the pointer moves after picking the two definition points, the tool shows a live preview of the dimension including mode switching.

#### RadiusDimensionTool

Workflow:

```text
activate Radius Dimension
click on a circle or arc entity (reads center and radius automatically)
move pointer to set label position
click to confirm
execute AddEntityCommand
```

#### DiameterDimensionTool

Workflow:

```text
activate Diameter Dimension
click on a circle or arc entity (reads center and diameter automatically)
move pointer to set label position
click to confirm
execute AddEntityCommand
```

---

### DimensionStyle

`DimensionStyle` is a named document-level object that controls the visual appearance of dimensions.

It stores:

```text
Id                  unique identifier
Name                display name
TextHeight          character height for dimension text
ArrowSize           length of arrowheads
ExtLineOffset       gap between definition point and extension line start
ExtLineBeyond       length of extension line beyond the dimension line
TextOffset          gap between dimension line and text baseline
DecimalPlaces       number of decimal places in the measured value
UnitSuffix          optional unit string appended to values (e.g. "mm")
```

The document can contain multiple named styles. Each dimension entity references one style by id.

A default style named `Standard` is created automatically with a new document.

Changes to a `DimensionStyle` affect all dimension entities that reference it. This matches the behavior of named text styles and layer definitions.

---

### Measured value format

The displayed value is computed at render time from the definition points.

Format:

```text
[prefix][value][suffix]
```

For a linear dimension with `DecimalPlaces = 2` and `UnitSuffix = "mm"`:

```text
123.45mm
```

For a radius dimension:

```text
R45.00mm
```

For a diameter dimension:

```text
Ø90.00mm
```

The value uses the document linear precision setting from `DrawingSettings` unless overridden by the dimension style's `DecimalPlaces`.

---

### Grip editing dimensions

Dimension definition points are exposed as grips.

Moving a grip changes the corresponding definition point and therefore changes the measurement, label position or both.

`GripEditTool` handles dimension grips through the standard grip provider mechanism.

`ReplaceEntitiesCommand` commits the change. Undo restores the original definition points and therefore the original measurement.

---

### Persistence

`DimensionEntity` and `DimensionStyle` are serialized in the `.opencad2d.json` format.

The serialized entity stores only definition points and style reference, not computed visual elements. Computed elements are reconstructed at load time.

Unknown dimension types encountered in a file from a newer build are skipped to preserve backward compatibility.
