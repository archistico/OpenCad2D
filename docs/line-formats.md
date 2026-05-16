# Line Formats and Line Styles

OpenCad2D separates **line style** from **line format**.

```text
LineStyle  = pattern category/style
LineFormat = complete appearance: color + lineweight + style + effective dash pattern
```

Layers reference line formats. Entities remain effectively ByLayer for primary appearance.

---

## LineFormat

A line format contains:

- `Id`;
- `Name`;
- `Color`;
- `LineWeight`;
- `LineStyle`;
- `DashPattern`.

`DashPattern` is the effective pattern used by rendering and export.

---

## LineStyle

Current styles:

| Style | Default pattern |
|---|---|
| `Continuous` | `[]` |
| `Dashed` | `[8, 4]` |
| `DashDot` | `[12, 4, 1, 4]` |
| `DashDotDot` | `[12, 4, 1, 4, 1, 4]` |
| `Custom` | user-defined |

Pattern values are expressed in **drawing units**, not pixels.

The pattern list must contain positive numeric values and should have an even number of entries so it can be interpreted as dash/gap pairs.

---

## Line Format Manager

The Line Format Manager supports:

- compact ColorPicker;
- editable lineweight;
- style preset selection;
- pattern values editor;
- compact dash preview.

Changing a preset applies that preset's default pattern.

Manually editing the pattern changes the style to `Custom`.

Examples:

```text
8,4
12,4,1,4
20,5
10,5,2,5
```

Invalid patterns are rejected by the view-model and should not be saved.

---

## Rendering and export

Rendering must use `LineFormat.DashPattern`, not only the `LineStyle` enum.

SVG writes the effective pattern as:

```xml
stroke-dasharray="8 4"
```

PDF uses the effective pattern where supported by the current export implementation.

DXF currently maps known preset styles to standard linetypes. Full custom DXF `LTYPE` generation for arbitrary patterns is future work.

---

## Persistence

Native `.opencad2d.json` stores `dashPattern` for line formats.

Compatibility rules:

- missing `dashPattern` -> derive default from `lineStyle`;
- invalid `dashPattern` -> recover using the style default;
- empty `dashPattern` -> continuous line.

The startup template includes explicit patterns for default line formats.
