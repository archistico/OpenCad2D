# OpenCad2D - AI handoff

This document is the current handoff for future AI/developer work. It intentionally summarizes the latest state only; historical phase notes were removed from the active handoff to keep it useful.

---

## Project identity

OpenCad2D is an experimental open-source 2D CAD application written in C#/.NET 8 with Avalonia UI.

Key constraints:

- OpenCad2D remains a **2D-only CAD**.
- Native files use `.opencad2d.json`.
- Export targets are DXF, SVG, PDF and later PNG.
- DWG is intentionally avoided.
- The project should grow through small, testable phases.
- Every feature change should update tests and documentation.

Author/contact shown in About dialog:

```text
OpenCad2D
Created by Emilie Rollandin
info@opencad2d.org
www.opencad2d.org
```

---

## Current status

Current baseline: **v0.8.x usability stabilization**.

Important completed areas:

- clean startup from `Templates/default.opencad2d.json`;
- startup window maximized;
- startup drawing contains default layers/formats/settings and no demo entities;
- document-level settings saved in `.opencad2d.json`;
- document recovery path for partially invalid native files;
- modal polish and centered manager dialogs;
- compact command input row with active tool, prompt and input box;
- command-driven workflow for drawing/edit/modify tools;
- advanced base Trim;
- Offset for line/circle/arc/polyline with preview;
- line-line Fillet with Radius option;
- independent draw order / Z-order;
- align/distribute tools;
- ColorPicker improvements;
- custom line style dash pattern foundation and Line Format Manager editor.

---

## Architectural rules

### Dependency direction

Keep dependency direction clean:

```text
Geometry -> Core -> Interaction/Tools -> Persistence/Export/App
```

The UI must not own geometry decisions. Geometry calculations should live in Core or Tools services depending on whether they are pure document geometry or workflow-specific.

### Document mutation

All document mutations must go through undoable commands.

Do not mutate document entities directly from UI controls or tools unless the change is explicitly routed through command history.

### Hidden and locked layers

- Hidden layer entities are not rendered, selectable or snap candidates.
- Locked layer entities are rendered but not selectable/editable.
- Modify tools must not mutate locked/hidden-layer entities.

### Draw order

Draw order is independent from layers.

- Lower `DrawOrder` renders first.
- Higher `DrawOrder` renders later and appears on top.
- Hit testing should prefer the higher draw order entity when objects overlap.
- Order actions are undoable and preserve selection.

---

## Command input status

The command input is now CAD-style and contextual.

Supported forms:

```text
100,50      absolute point
@50,0       relative cartesian point
@100<45     relative polar point
25          distance/angle/factor when expected
C / Close   option when exposed by prompt
U / Undo    option when exposed by prompt
A / All     option when exposed by prompt
```

Important Enter rule:

```text
Idle + empty Enter          repeat last valid command
Active command + empty Enter routed to active command
```

Do not let an input like `C` start Circle while another command is waiting for a point. During an active command, text belongs to the active command first.

The command row is compact. The previous always-visible command history area was removed because it consumed too much canvas space.

---

## Tool status

### Drawing

Command-driven:

- Point;
- Text;
- Line;
- Rectangle;
- Rectangle by sides;
- Circle;
- Arc center/start/end;
- Arc 3P;
- Polyline.

Polyline supports typed points, relative/polar points, `Close` and `Undo`.

### Edit/modify/transform

Implemented:

- Delete;
- Move;
- Copy;
- Rotate;
- Scale;
- point-based Align;
- Break Point;
- Break Segment;
- Extend;
- Trim;
- Offset;
- Fillet.

Offset currently supports:

- Line;
- Circle;
- Arc;
- straight-segment open/closed Polyline.

Offset preview uses the same geometry path as final creation. Keep that invariant.

Fillet currently supports:

- Line-Line;
- Radius option;
- Radius `0` as sharp-corner join;
- trim mode always on.

### Object alignment/distribution

Implemented:

- Align Left / Right / Top / Bottom;
- Distribute Horizontally / Vertically by centers.

Align uses the bounding box of the whole selection. Top/Bottom are defined visually for the canvas, not by abstract mathematical Y labels.

---

## Line formats and line styles

Important terminology:

```text
LineStyle  = line pattern category/style
LineFormat = complete format: color + weight + style + dash pattern
```

`LineFormat.DashPattern` is the effective pattern. It is a numeric list in drawing units:

```text
Continuous -> []
Dashed     -> [8,4]
DashDot    -> [12,4,1,4]
DashDotDot -> [12,4,1,4,1,4]
Custom     -> user-defined even-length positive list
```

The Line Format Manager has:

- compact ColorPicker;
- pattern editor;
- pattern preview;
- automatic switch to `Custom` when pattern values are manually edited.

Rendering, SVG and PDF should use the effective `DashPattern`, not only the preset enum.

DXF currently maps known preset line styles to standard linetypes. Full custom DXF LTYPE definitions are future work.

---

## Native persistence

Native file: `.opencad2d.json`.

The native file stores:

- layers;
- line formats, including `dashPattern`;
- text formats;
- dimension styles;
- entities;
- viewport;
- document settings.

Document settings include:

- grid settings;
- snap enabled/modes/tolerance;
- Ortho mode;
- Polar Tracking mode;
- current layer;
- current text format;
- current dimension style if available.

Old files without settings or dash patterns must load with defaults.

---

## Current docs to keep

Keep and maintain:

- `README.md`
- `docs/architecture.md`
- `docs/roadmap.md`
- `docs/commands.md`
- `docs/command-input.md`
- `docs/tools.md`
- `docs/modify-tools.md`
- `docs/transform-tools.md`
- `docs/measure-tools.md`
- `docs/line-formats.md`
- `docs/text-formats.md`
- `docs/layer-appearance.md`
- `docs/application-settings.md`
- `docs/draw-order.md`
- `docs/persistence.md`
- `docs/export.md`
- `docs/dxf-import.md`
- `docs/dxf-export.md`
- `docs/svg-export.md`
- `docs/pdf-export.md`
- `docs/grip-editing.md`
- `docs/snapping.md`
- `docs/known-limitations.md`
- `docs/release-v0.8.md`

Historical release/planning files from v0.4-v0.7 can be deleted from the active docs tree.

---

## Next recommended work

Before opening v0.9:

1. remove obsolete historical docs listed in `docs/obsolete-documents.md`;
2. run full build/test;
3. manually verify offset preview and line format pattern export;
4. optionally tag/publish a v0.8.x release;
5. then start v0.9 stabilization.
