# Text and Dimensions

OpenCad2D supports single-line and multiline text annotations plus basic non-associative dimensions.

---

## Text

`TextEntity` stores single-line annotation text:

- insertion point;
- text value;
- text format reference;
- rotation/transform data where supported.

`MultilineTextEntity` stores multiline annotation text with the same insertion point, rotation and text format model. It is inserted with the `MTEXT` / `MT` command or the MText button, and the dialog accepts line breaks.

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

Dimensions are currently **non-associative**. If the measured geometry changes later, existing dimensions do not update automatically.

---

## Dimension style

Dimension appearance is controlled by `DimensionStyle` definitions.

The current scope focuses on usable graphical dimensions rather than full professional associative dimension systems.

---

## Export

Text is exported to SVG, PDF and DXF. Multiline text is exported as SVG `<text>/<tspan>` content, PDF text lines, and DXF `MTEXT`. DXF import recognizes `MTEXT` and maps `\P` paragraph separators to internal line breaks. Dimensions are exported as graphical primitives where supported.

Future work may add richer native DXF dimension export and deeper dimension editing.

---

## Future work

- associative dimensions;
- richer style manager UI;
- grips and property editing for all dimension sub-properties;
- additional dimension variants and formatting options.
