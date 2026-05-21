# Text and Dimensions

OpenCad2D supports single-line and multiline text annotations plus basic non-associative dimensions.

---

## Text

`TextEntity` stores single-line annotation text:

- insertion point;
- text value;
- text format reference;
- rotation/transform data where supported.

`MultilineTextEntity` stores multiline annotation text with the same insertion point, rotation and text format model. It is inserted with the `MTEXT` / `MT` command or the MText button, and the dialog accepts line breaks. It also stores an optional `ReferenceWidth` used by DXF `MTEXT` export/import; `0` means unconstrained wrapping. After insertion, the property panel can edit the MTEXT value, insertion coordinates, rotation, text format and reference width.

Text appearance is controlled by reusable `TextFormat` definitions.

---

## Text formats

Text formats define reusable appearance for text entities.

They are managed through the Text Format Manager, which includes compact ColorPicker support.

---

## Dimensions

Implemented dimension types:

- Horizontal Dimension;
- Vertical Dimension;
- Aligned Dimension;
- Radius Dimension;
- Diameter Dimension;
- Angular Dimension.

Dimensions are currently **non-associative**. They store their own measured points and dimension-line geometry independently from the source entity. If the measured geometry changes later, existing dimensions do not update automatically. OpenCad2D can mark dimensions as potentially stale after model edits, but the user must update or recreate the dimension manually.

---

## Dimension style

Dimension appearance is controlled by reusable `DimensionStyle` definitions. Each dimension stores a `DimensionStyleId`; the document stores a `CurrentDimensionStyleId` used by new dimension tools.

Current configurable style data includes:

- linked text format;
- arrow size;
- arrow/terminator symbol: closed arrow, open arrow, closed blank triangle, closed filled triangle, outside filled triangle, architectural tick, oblique slash, dot or none;
- text offset from the dimension line;
- preferred dimension line offset from the measured points;
- extension line offset and overshoot;
- decimal precision and decimal separator;
- generic prefix and suffix;
- radius and diameter prefixes;
- text rotation mode: readable, aligned with the dimension line, or always horizontal;
- text fit mode: inside, outside when needed, or always outside;
- terminator fit mode: inside, outside when needed, or always outside.

Dimension text primitives are positioned by their visual center. The renderer/exporters then center the text around that point using the text bounding box where available, so dimension labels are not placed by the top-left insertion corner.

The default `Readable` text rotation mode follows the dimension direction but flips text that would otherwise be upside down. Horizontal dimensions remain horizontal, vertical dimensions are readable from the left, according to the current OpenCad2D project convention, and aligned/angular dimensions are normalized into a readable orientation.

The current scope focuses on usable graphical dimensions rather than full professional associative dimension systems. A first Dimension Style Manager is available from the top bar near Layer, Line Formats and Text Formats. It can create/delete styles, choose the current style, edit text format, unit formatting, symbol type, symbol size and the main dimension/extension offsets. Radius and diameter prefixes are exposed for the selected style. The manager also includes a live preview for the selected style, showing a sample horizontal dimension with the current symbol, text offset, extension offsets, dimension offset and prefix/suffix formatting.

---

## Export

Text is exported to SVG, PDF and DXF. Multiline text is exported as SVG `<text>/<tspan>` content, PDF text lines, and DXF `MTEXT` with group code `41` for the reference width. DXF import recognizes `MTEXT`, maps `\P` paragraph separators to internal line breaks, and preserves reference width when present. Dimensions are exported as graphical primitives where supported.

Future work may add richer native DXF dimension export and deeper dimension editing.

---

## Future work

- associative dimensions;
- grips and property editing for all dimension sub-properties;
- additional dimension variants and formatting options.


## Dimension style preview consistency

The Dimension Style Manager preview uses the same `DimensionGeometryBuilder` used by real dimensions. This keeps terminators, extension spacing, text offset and formatted measurement text aligned with the drawing/export rendering path.


## Classic terminator symbols

Dimension styles support the classic CAD terminator set used by the shared `DimensionGeometryBuilder`, so the canvas, preview and graphical exports stay consistent. The currently available symbols are:

- closed arrow;
- open arrow;
- closed blank triangle;
- closed filled triangle;
- outside filled triangle;
- architectural tick;
- oblique slash;
- dot;
- none.

`ClosedArrow` is kept for compatibility with existing documents and behaves as a closed blank triangular arrowhead. The filled variants are represented with additional internal strokes because the current dimension render model is line-primitive based.


## ISO-readable dimension text orientation

`DimensionTextRotationMode.Readable` follows the aligned-dimension convention used for technical drawings:

- horizontal dimensions keep text at `0°`;
- vertical dimensions keep text at `270°`, so the value is readable from the left side of the sheet;
- aligned/rotated dimensions follow the dimension direction but are flipped by `180°` when they would become upside down;
- exact `90°` readable cases are normalized to `270°` to keep vertical dimensions project-readable from the left.

`DimensionTextRotationMode.Horizontal` implements the unidirectional convention: dimension text remains horizontal.

`DimensionTextRotationMode.AlignedWithDimensionLine` keeps the pure geometric angle and can therefore produce upside-down text when the measured direction itself is upside down.


## Vertical dimension text side

For the project convention currently used by OpenCad2D, readable vertical dimension text is placed on the left side of the vertical dimension line and uses a `270°` model rotation, so the value is read from the left side of the sheet.


## Built-in dimension style presets

OpenCad2D creates three built-in dimension styles in new documents and fallback default collections:

- `Standard`: generic readable style, closed arrow symbol, no unit suffix.
- `Architectural`: architectural tick symbol, `m` suffix, negative text offset for text placement on the opposite side of the dimension line.
- `Mechanical`: filled triangle terminators, `mm` suffix, horizontal text orientation.

These presets are marked as built-in in the Dimension Style Manager and provide practical starting points that can be duplicated or edited according to the drawing convention.


## Dimension style property editing

When a single dimension entity is selected, the property panel exposes `Dimension style` as a combo box populated from the document dimension style collection. Selecting a style applies it through the normal undoable entity replacement workflow.

The row stores and applies the style by resolving either the displayed style name or the underlying style id, so built-in and custom styles remain robust even if their ids and names differ.


## Dimension text fit rules

Dimension styles expose `Text fit` to control text placement on short linear/aligned dimensions:

- `Inside`: always keeps the text at the dimension midpoint.
- `OutsideWhenNeeded`: keeps the text inside when the measured span is long enough; otherwise places it outside the measured span.
- `AlwaysOutside`: always places the text outside the measured span.

The fit decision is implemented in `DimensionGeometryBuilder`, so the canvas, preview and exports all use the same geometry.


## Dimension terminator fit rules

Dimension styles expose `Terminator fit` to control whether arrows, ticks, dots or triangle terminators are drawn inside or outside the measured span:

- `Inside`: always draws terminators inside the span.
- `OutsideWhenNeeded`: draws terminators inside when there is enough room; otherwise flips them outside.
- `AlwaysOutside`: always draws terminators outside the span.

The fit decision is resolved in `DimensionGeometryBuilder` for linear and aligned dimensions, so canvas rendering, preview and exports all share the same terminator geometry.
