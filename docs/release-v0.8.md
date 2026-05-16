# OpenCad2D v0.8.x Release Notes

This is the current consolidated release note for the v0.8/v0.8.x line. Older milestone release notes are obsolete and can be removed from the active documentation tree.

---

## Highlights

- CAD-style guided command input;
- command-driven drawing, edit and modify tools;
- native document settings persistence;
- document recovery improvements;
- advanced base Trim;
- Offset with polyline support and live preview;
- line-line Fillet;
- independent draw order / Z-order;
- align and distribute object tools;
- custom line style dash patterns;
- improved Line Format Manager and Text Format Manager with compact ColorPicker;
- UI cleanup and improved tooltips.

---

## Command input

The command input now supports:

- contextual prompts;
- command aliases;
- absolute point input: `100,50`;
- relative point input: `@50,0`;
- relative polar input: `@100<45`;
- direct distance/angle/factor input when expected;
- options such as `Close`, `Undo`, `All` and `Radius`;
- empty Enter repeat-last-command when idle.

The command row is compact and shows active tool + prompt + input box.

---

## Drawing and editing tools

Command-driven workflows were added or consolidated for:

- Line;
- Polyline;
- Rectangle;
- Circle;
- Arc 3P;
- Move;
- Copy;
- Rotate;
- Scale;
- Align;
- Break Point;
- Break Segment;
- Extend;
- Trim;
- Offset;
- Fillet;
- Delete.

---

## Modify tools

### Trim

Trim supports cutting edges, All mode, repeated trimming and in-command Undo.

### Offset

Offset supports:

- Line;
- Circle;
- Arc;
- straight-segment open/closed Polyline.

Polyline offset uses miter joins. Offset preview is shown before confirmation.

### Fillet

Fillet supports Line-Line fillets with Radius option. Radius `0` creates a sharp-corner join.

---

## Appearance and line formats

Line formats now distinguish:

```text
LineStyle  = pattern category/style
LineFormat = color + weight + style + effective dash pattern
```

Dash patterns are stored as numeric lists in drawing units.

The Line Format Manager includes:

- compact ColorPicker;
- pattern value editor;
- dash preview;
- automatic Custom style when editing pattern manually.

---

## Document settings and persistence

Native `.opencad2d.json` files now persist document-level settings such as:

- grid;
- snap modes;
- snap tolerance;
- Ortho;
- Polar Tracking;
- current layer;
- current text/dimension settings where supported.

Old files remain supported through defaults and recovery behavior.

---

## Draw order and arrangement

Draw order is independent from layers.

Added:

- To Front;
- To Back;
- Forward;
- Backward;
- draw order display in Property Panel;
- Align Left / Right / Top / Bottom;
- Distribute Horizontally / Vertically by centers.

Hit testing follows draw order when entities overlap.

---

## UI refinements

- main window starts maximized;
- startup loads `default.opencad2d.json` with no demo entities;
- modal dialogs open centered on owner;
- save-changes dialog improved;
- About dialog uses `info@opencad2d.org` and `www.opencad2d.org`;
- Delete moved to the Edit group;
- tooltips added to main UI buttons;
- command input layout made compact.

---

## Suggested validation before publishing

```bash
dotnet build OpenCad2D.sln
dotnet test OpenCad2D.sln --no-build
```

Manual checks:

- save/reopen `.opencad2d.json` with grid/snap/polar settings;
- edit line format pattern, save/reopen and export SVG;
- offset line/circle/arc/polyline and verify preview;
- fillet two lines with radius `0` and positive radius;
- draw overlapping entities and verify draw order + hit testing;
- align and distribute objects with Undo;
- export SVG/DXF/PDF from a mixed drawing.

---

## Known limitations

- not production CAD software yet;
- DXF custom linetype definitions for arbitrary custom dash patterns are future work;
- polyline offset uses miter joins only;
- Fillet is Line-Line only;
- dimensions are non-associative;
- advanced Trim modes such as Fence/Crossing/Project/Edge are future work;
- PNG export is still planned.
