# Command Input

OpenCad2D has a compact CAD-style command input. It shows:

```text
[Active tool] [Current prompt] [Input box]
```

The active prompt tells the user what the current command expects.

---

## Core rule

Whenever a tool asks for a point, the user can either:

- click on the canvas using snap/ortho/polar tracking; or
- type a point/distance in the command input.

Mouse input and typed input feed the same tool state machine.

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

## Command-driven tools

Implemented command-driven tools include:

| Area | Tools |
|---|---|
| Draw | Point, Text, Line, Rectangle, Rect Sides, Circle, Arc, Arc 3P, Polyline |
| Transform | Move, Copy, Rotate, Scale, point-based Align |
| Modify | Delete, Break Point, Break Segment, Extend, Trim, Offset, Fillet |
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

Radius `0` creates a sharp-corner join.

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

The earlier large always-visible command history panel was removed. The current UI favors a compact command row to preserve drawing space.
