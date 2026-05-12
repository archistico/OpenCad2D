# OpenCad2D v0.6 - Real command line and Property Panel v2

v0.6 completes the command-line and editable-property milestone.

The focus of this release is usability: OpenCad2D can now be driven more like a CAD application, with command aliases, typed coordinates and undoable property editing.

---

## Highlights

- real command-line tool activation;
- command aliases;
- absolute coordinates;
- relative coordinates;
- direct distance input;
- distance-angle input;
- repeat-last-command with empty `Enter` and right-click;
- editable Property Panel v2;
- undoable property edits;
- validation for property edits;
- documentation updated for the next milestone.

---

## Command line

The command line can now activate tools by command name or alias.

Examples:

```text
L / LINE                    -> Line
PL / POLYLINE               -> Polyline
C / CIRCLE                  -> Circle
A / ARC                     -> Arc
T / TEXT                    -> Text
PO / POINT                  -> Point
HDIM / H                    -> Horizontal Dimension
VDIM / V                    -> Vertical Dimension
ADIM / AL                   -> Aligned Dimension
RAD / RDIM                  -> Radius Dimension
DIA / DDIM                  -> Diameter Dimension
ANG / ANGDIM                -> Angular Dimension
TR / TRIM                   -> Trim
EX / EXTEND                 -> Extend
BP / BREAKPOINT             -> Break Point
BS / BREAKSEGMENT           -> Break Segment
DI / DISTANCE               -> Measure Distance
ME / MEASURE                -> Measure Entity
```

Command resolution is case-insensitive.

Unknown textual commands produce a clear message and do not change the active tool.

---

## Coordinate input

Supported point input forms:

```text
100,50      absolute model coordinate
@100,0      relative coordinate from the current base point
50          direct distance in the current pointer/constrained direction
100<45      distance plus angle from the current base point
```

Numeric parsing uses invariant culture.

The decimal separator is always `.`. The comma is reserved for X/Y separation.

Distance-angle input uses CAD orientation:

```text
0°   = right
90°  = up
180° = left
270° = down
```

Command-line coordinate input is routed through the same active tools used by mouse input. Exact typed coordinates bypass snap/ortho/polar side effects so entered values remain precise.

---

## Repeat last command

The command line remembers the last valid tool activation as the repeatable command.

Rules:

- valid tool commands become repeatable;
- coordinate input does not become repeatable;
- invalid commands do not replace the last valid command;
- empty `Enter` repeats the last valid command;
- right-click on the canvas repeats the last valid command when the workspace is idle;
- right-click does not interrupt an active multi-step point command.

---

## Property Panel v2

The Property Panel now supports editable rows for supported entity properties.

Supported edits include:

- `PointEntity`: position;
- `LineEntity`: start/end points;
- `CircleEntity`: center and radius;
- `ArcEntity`: center, radius, start angle and end angle;
- `TextEntity`: value, insertion point, rotation and text format;
- `PolylineEntity`: common state such as closed/open;
- dimension entities: dimension style and text override;
- common layer assignment where applicable.

Detailed polyline vertex editing remains handled by grip editing.

Every successful Property Panel edit is undoable. The UI validates and parses input, creates a modified entity copy and commits it through command history, normally with `ReplaceEntitiesCommand`.

---

## Validation and safety

The Property Panel rejects invalid edits before executing a command.

Examples:

- invalid numbers;
- non-positive circle radius;
- empty text content;
- line start/end points becoming coincident;
- invalid style or format references;
- edits to hidden or locked selected entities where they are not allowed.

---

## Documentation updated

Updated documents:

```text
README.md
docs/roadmap.md
docs/commands.md
docs/tools.md
docs/ai-handoff.md
docs/v0.6-command-line-property-panel-plan.md
docs/release-v0.6.md
```

---

## Out of scope

The following items remain intentionally outside v0.6:

- DXF import;
- PDF export;
- command macros or script files;
- complete compatibility with external CAD command languages;
- Property Panel vertex table editing for polylines;
- associative dimensions;
- blocks and groups.

---

## Next milestone

```text
v0.7 - Interoperability: DXF import and PDF export
```

Recommended starting points:

- audit current DXF export coverage;
- design minimal DXF import for base entities and layers;
- define unsupported-entity skip/log behavior;
- design PDF export page setup, scale and margins.
