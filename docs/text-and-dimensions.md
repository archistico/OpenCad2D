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

Dimensions are currently **non-associative**. If the measured geometry changes later, existing dimensions do not update automatically.

---

## Dimension style

Dimension appearance is controlled by `DimensionStyle` definitions.

The current scope focuses on usable graphical dimensions rather than full professional associative dimension systems.

---

## Export

Text is exported to SVG, PDF and DXF. Multiline text is exported as SVG `<text>/<tspan>` content, PDF text lines, and DXF `MTEXT` with group code `41` for the reference width. DXF import recognizes `MTEXT`, maps `\P` paragraph separators to internal line breaks, and preserves reference width when present. Dimensions are exported as graphical primitives where supported.

Future work may add richer native DXF dimension export and deeper dimension editing.

---

## Future work

- associative dimensions;
- richer style manager UI;
- grips and property editing for all dimension sub-properties;
- additional dimension variants and formatting options.
