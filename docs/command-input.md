# Command Input

OpenCad2D currently has a compact CAD-style command input. The active roadmap replaces the fixed bottom command row with a dynamic cursor-adjacent command HUD.

The command input is not only a visual widget: it is the shared interaction contract between the UI and every command-driven tool. The active prompt tells the user what the current command expects, and both mouse input and typed input feed the same tool state machine.

---

## Core rule

Whenever a tool asks for a point, the user can either:

- click on the canvas using snap/ortho/polar tracking; or
- type a point/distance in the command input.

Mouse input and typed input feed the same tool state machine.

---

## Dynamic Command HUD roadmap

The fixed bottom command row is being replaced by a cursor-adjacent HUD in milestone `v0.8.121`. The full specification is `docs/specs/v0.8.121-dynamic-command-hud.md`.

The migration must happen in safe stages:

1. inventory every command-driven tool and its prompt phases;
2. make `CommandPromptState` the common source for prompts, options and expected input;
3. propagate pointer screen position and reusable live measurements;
4. expose a read-only `CommandHudState`;
5. render a read-only visual HUD while the old command row remains active;
6. move the single real `CommandInputTextBox` into the HUD;
7. remove the fixed row only after regression;
8. implement editable numeric HUD fields later.

Until that migration is complete, this document describes the command-input behavior that must be preserved.

The final HUD should show, when meaningful:

```text
[tool icon] TOOL NAME
Prompt or live fields
Command options
Command input
```

Examples of live fields include Distance/Angle for line-like phases, Width/Height for opposite-corner rectangles and Radius for circle phases. The first HUD implementation should show these fields read-only. Directly editable HUD fields are deferred because they require temporary distance/angle/width/radius overrides rather than ordinary one-shot command-input submission.

---

## Supported input forms

| Input | Meaning |
|---|---|
| `100,50` | absolute point |
| `@50,0` | relative cartesian point from the current reference point |
| `@100<45` | relative polar point, distance 100 at 45 degrees |
| `25` | distance, angle or scale factor when the prompt expects it |
| `C`, `Close` | option when exposed by the active prompt |
| `U`, `Undo` | option when exposed by the active prompt |
| `A`, `All` | option when exposed by the active prompt |
| `R`, `Radius` | Fillet radius option |
| `T`, `Trim` | Fillet trim mode option |
| `N`, `NoTrim` | Fillet no-trim mode option |

Angles are user-facing degrees:

```text
0°   = right
90°  = up
180° = left
270° = down
```

---

## Enter and Escape

Empty Enter while idle repeats the last valid command.

Empty Enter while a command is active is routed to that command. It confirms the phase only if that command phase allows confirmation.

Examples:

- Polyline next-point phase: Enter finishes the open polyline.
- Trim cutting-edge phase: Enter confirms selected cutting edges.
- Circle center-point phase: Enter is invalid because a point is required.

Escape cancels the active command. A following Escape can clear selection according to the current selection workflow.

---

## Command history navigation

The command input supports CAD-style history navigation:

| Key | Behavior |
|---|---|
| `↑` | recalls the previous command/action from command history |
| `↓` | moves forward through recalled commands; after the newest entry it clears the input |

Only command/action submissions are stored in this navigable history. Point, distance and option input used inside an active command remains visible in the compact command log but is not recalled as a standalone command. This keeps `↑` focused on reusable commands such as `LINE`, `CIRCLE`, `TRIM`, `OFFSET`, `SELECTALL` and similar actions.

---

## Command autocomplete

The command input supports a first-pass autocomplete workflow:

| Key | Behavior |
|---|---|
| `Tab` | completes the current command prefix when a known command/action match exists |

Examples:

```text
LI  + Tab -> LINE
MT  + Tab -> MTEXT
M   + Tab -> MOVE
SELECTA + Tab -> SELECTALL
```

Autocomplete only targets command/action names. It intentionally ignores point and distance syntax such as `100,50`, `@25,0` or `25`, so Tab does not interfere with numeric CAD input. When the command input is empty, Tab keeps its canvas behavior and can still enter grip editing for the current selection.

---

## Command-driven tools

Implemented command-driven tools include:

| Area | Tools |
|---|---|
| Draw | Point, Text, MTEXT, Line, Rectangle, Rect Sides, Circle, Arc, Arc 3P, Ellipse, Polyline, Polygon, Spline, symbol/helper tools where registered |
| Transform | Move, Copy, Rotate, Scale, point-based Align |
| Modify | Delete, Break Point, Break Segment, Extend, Trim, Offset, Fillet, Chamfer, Explode, Join and other registered modify tools |
| Navigation | Zoom Window, Zoom Extents |
| Selection | Select, Select All, Select Last |
| Order | To Front, To Back, Forward, Backward |
| Arrange | Align Left/Right/Top/Bottom, Distribute H/V |

---

## Examples

### Line

```text
L
100,100
@100<45
```

`LINE` creates one segment and ends after the second point.

### Polyline

```text
PL
0,0
@100,0
@100<90
C
```

Options:

```text
C / Close
U / Undo
Enter = finish open polyline
```

### Offset

```text
O
25
pick entity
pick side
```

Offset supports line, circle, arc and straight-segment polyline targets. A live preview is shown while choosing the side.

### Fillet

```text
F
R
10
pick first line
pick second line
```

Radius `0` creates a sharp-corner join in Trim mode. With a positive radius, the canvas shows a live preview while the second line is being selected. Use `T` / `Trim` and then `N` / `NoTrim` to keep the original lines and add only the tangent fillet arc.

---

## Parser architecture

Main command-input types live in `OpenCad2D.Tools/Input`:

- `CommandPromptState`
- `CommandOption`
- `CommandInputKind`
- `CommandInputSubmission`
- `CommandInputParser`
- `ICommandDrivenTool`
- `ToolPickedEntityInput`

The parser is contextual. The same text may mean different things depending on what the prompt expects.

Example:

```text
25
```

can mean distance for `OFFSET`, angle for `ROTATE`, scale factor for `SCALE`, or direct distance from a reference point for a point-or-distance phase.

---

## Notes

The earlier large always-visible command history panel was removed. The current UI still uses a compact command row, but the active roadmap replaces it with a dynamic command HUD that preserves the same command parser and tool state machines.
