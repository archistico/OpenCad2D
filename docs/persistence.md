# Persistence

OpenCad2D uses `.opencad2d.json` as its native save/reopen format.

The native format is intended for OpenCad2D editing fidelity. It is not a DXF/DWG interchange format.

---

## Native file contents

The native document stores:

- document version;
- layers, including `FillColor`;
- line formats;
- text formats;
- dimension styles;
- entities;
- viewport state;
- document-level drafting settings.

---

## Line formats

Line formats include:

- color;
- lineweight;
- line style;
- effective `dashPattern`.

Compatibility:

- if `dashPattern` is missing, it is derived from `lineStyle`;
- if `dashPattern` is invalid, recovery falls back to the style default;
- empty `dashPattern` means continuous line.

---

## Layer fill and entity fill state

The native format stores layer fill color separately from reusable stroke line formats:

```text
LayerDto.FillColor
LayerDto.LineFormatId
```

Supported filled entities store only whether solid fill is enabled:

```text
CircleEntityDto.IsFilled
PolylineEntityDto.IsFilled
```

The actual fill color remains layer-based and is resolved at render/export time from `Layer.FillColor`.

Compatibility rules:

```text
missing LayerDto.FillColor -> default from the layer line format color
missing IsFilled on old entities -> false
```

This keeps old drawings visually unchanged when reopened.

---

## Document settings

The `settings` section stores portable drafting state:

- current layer;
- current text format;
- current dimension style where available;
- grid settings;
- snap enabled state and snap modes;
- snap tolerance;
- Ortho mode;
- Polar Tracking mode.

Files without `settings` must load using defaults.

---

## Startup template

The application starts from:

```text
src/OpenCad2D.App/Templates/default.opencad2d.json
```

The template contains default layers, formats, styles and settings, but no demo geometry.

If the template is missing or invalid, the application must use a safe internal fallback.

---

## Dirty state

Native load/save and imported documents use explicit dirty-state rules:

- opening a native `.opencad2d.json` marks the workspace clean;
- importing DXF creates a new unsaved native document and marks it dirty;
- document settings changes should mark the document dirty when they affect saved document state.

---

## Recovery

The serializer supports recovery for partially invalid native documents.

Recovery behavior:

- valid entities are preserved;
- invalid entities are skipped;
- missing layers can fall back to Layer 0;
- missing settings fall back to defaults;
- recovery reports recovered and skipped items.

JSON that is syntactically unreadable should fail clearly rather than attempting unsafe reconstruction.

---

## Export is separate

SVG, DXF and PDF export do not update the native file path and do not clear the dirty marker.
