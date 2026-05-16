# Text and Dimensions

OpenCad2D supports single-line text annotations and basic non-associative dimensions.

---

## Text

`TextEntity` stores:

- insertion point;
- text value;
- text format reference;
- rotation/transform data where supported.

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

Dimensions are exported as graphical primitives where supported.

Future work may add richer native DXF dimension export and deeper dimension editing.

---

## Future work

- associative dimensions;
- richer style manager UI;
- grips and property editing for all dimension sub-properties;
- additional dimension variants and formatting options.
