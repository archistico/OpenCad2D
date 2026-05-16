# OpenCad2D v0.8 Release Notes

OpenCad2D v0.8 focuses on CAD-style command input, guided tool workflows and essential modify tools.

This release is a major usability milestone: the command line now helps the user understand the active command phase and can drive tools with exact typed input while preserving mouse-based workflows.

---

## Highlights

- CAD-style guided command input with contextual prompts.
- Compact visible command history near the command input.
- Absolute coordinate input, for example `100,100`.
- Relative cartesian coordinate input, for example `@100,0`.
- Relative polar input, for example `@100<45`.
- Direct distance input where the active prompt can resolve a distance from the current cursor direction.
- Empty Enter repeats the last valid command when the workspace is idle.
- Empty Enter confirms or completes active command phases only when that phase allows it.
- New `OFFSET` / `O` command.
- New `FILLET` / `F` command.
- First advanced `TRIM` workflow with `All`, Ctrl-click additional cutting edges and in-command `Undo`.

---

## Command input

The command input is now a guided CAD-style workflow instead of only a command launcher.

Examples:

```text
LINE: Specify first point:
LINE: Specify second point:

POLYLINE: Specify first point:
POLYLINE: Specify next point or [Close/Undo]:

OFFSET: Specify offset distance:
OFFSET: Select object to offset:
OFFSET: Specify side to offset:

FILLET: Select first line or [Radius] <0>:
FILLET: Specify fillet radius:
FILLET: Select second line:
```

Typed input and mouse input share the same tool state for migrated commands. A point request can be satisfied either by clicking on the canvas or by typing coordinates.

Supported typed point formats:

```text
100,50      absolute point
@100,0      relative cartesian point
@100<45     relative polar point
```

The decimal separator is always `.`. The comma is reserved for separating X/Y coordinates.

---

## Command-driven tools

The following tools now expose guided command phases:

- Line
- Polyline
- Rectangle
- Circle
- Arc 3P
- Move
- Copy
- Rotate
- Scale
- Align
- Break Point
- Break Segment
- Extend
- Trim
- Delete
- Offset
- Fillet

---

## Trim advanced base

`TRIM` now supports a first advanced workflow:

```text
TRIM: Select cutting edge or [All]:
TRIM: Select entity side to trim or [All/Undo]:
```

Implemented behavior:

- click a cutting edge to begin trimming;
- Ctrl-click while trimming to add further cutting edges;
- type `A` or `All` to use all visible supported entities as cutting edges;
- click the side of the target entity to remove;
- type `U` or `Undo` to undo the last trim operation inside the current Trim session;
- press Enter while trimming to finish/reset the current Trim command.

Advanced modes such as Fence, Crossing, Edge, Project, Erase and Shift-to-Extend remain future work.

---

## Offset

Aliases:

```text
OFFSET
O
```

Workflow:

```text
OFFSET: Specify offset distance:
OFFSET: Select object to offset:
OFFSET: Specify side to offset:
```

Supported entities in v0.8:

- Line
- Circle
- Arc

After creating one offset, the command stays active and returns to object selection while keeping the current distance.

Polyline offset is intentionally deferred until a robust offset/join service is introduced.

---

## Fillet

Aliases:

```text
FILLET
F
```

Workflow:

```text
FILLET: Select first line or [Radius] <0>:
FILLET: Specify fillet radius:
FILLET: Select second line:
```

Supported in v0.8:

- Line-Line fillet
- `Radius` / `R` option
- radius `0` for sharp-corner joins
- trim mode always enabled

Line-Arc, Arc-Arc, polyline fillet, multiple fillet and NoTrim mode are deferred.

---

## Startup and template improvements

v0.8 also stabilizes startup behavior:

- the application starts maximized;
- normal startup no longer seeds a sample drawing;
- the default document is loaded from `src/OpenCad2D.App/Templates/default.opencad2d.json`;
- the template contains default layers, line formats, text formats and dimension style definitions;
- an internal fallback is used if the template cannot be loaded.

---

## Selection and navigation improvements

- Select All.
- Select Last restores the last real selection before deselection.
- Zoom Window.
- Zoom Extents is also available in the Navigate group of the left tool panel.

---

## UI refinements

- About dialog updated with `info@opencad2d.org` and `www.opencad2d.org`.
- Modal dialogs open centered on their owner.
- Save-changes confirmation has a dedicated styled window.

---

## Grip editing

Arc grip behavior was refined:

- moving a start/end grip in the 3-point arc workflow preserves the other two construction points;
- moving the point-on-arc grip preserves the two endpoints and recalculates the arc center/radius;
- moving the center grip moves the whole arc.

---

## File recovery

The native JSON loader now has a partial recovery path for readable but partially invalid `.opencad2d.json` documents:

- valid entities are preserved;
- invalid entities can be skipped;
- missing layer references can be reassigned to `Layer 0`;
- the loader reports recovered and skipped counts.

Syntax-invalid JSON still fails explicitly because there is no reliable document structure to recover from.

---

## Known limitations

- Offset does not yet support polylines.
- Fillet is limited to Line-Line.
- Trim does not yet support Fence/Crossing/Edge/Project/Erase modes.
- Shift-click Extend inside Trim is not implemented yet.
- The command history is compact and visible, but not yet a full docked CAD console.

---

## Suggested validation before publishing

Run:

```bash
dotnet build OpenCad2D.sln
dotnet test OpenCad2D.sln --no-build
```

Manual smoke checks:

```text
LINE -> 100,100 -> @100<45
POLYLINE -> 0,0 -> @100,0 -> @50<90 -> C
OFFSET -> distance -> line/circle/arc -> side point
FILLET -> R -> 10 -> first line -> second line
TRIM -> All -> target side -> Undo
MOVE/COPY -> selection -> base point -> @50,0
BREAK -> entity -> first point -> second point
```


## Dimension export stabilization

- PDF export now includes horizontal, vertical, aligned, radius, diameter and angular dimensions.
- Dimension PDF output uses graphical primitives plus text, matching the SVG/DXF export strategy.
- PDF dimension symbols such as degree and diameter are escaped with WinAnsi octal sequences for better viewer compatibility.
- Added export tests covering PDF output for every current dimension type.
