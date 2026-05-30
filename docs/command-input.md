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


## Dynamic Command HUD Step 2 — prompt contract cleanup

The dynamic HUD must read the active command state from `ICommandDrivenTool.GetPromptState(...)`. The following tools have been converted from ViewModel fallback prompt text into the shared prompt contract: arc, point, text, measurement tools, dimension tools, zoom window and architectural insertion tools.

The old command input row remains active until the HUD has a read-only visual overlay and then safely moves the single existing `CommandInputTextBox`.

## Dynamic Command HUD — Step 3 read-only overlay

The first visual HUD implementation is intentionally read-only. It mirrors command state near the cursor but does not replace command input yet.

Current Step 3 behavior:

- The existing bottom command line remains the active input surface.
- The HUD follows the mouse cursor on the CAD canvas.
- The HUD shows the active tool name, current prompt, live measurement fields and command options when available.
- The HUD overlay is non-interactive and does not intercept pointer or keyboard input.

This preserves the existing command-line regression surface while making it possible to test the new visual command experience incrementally.


## Dynamic Command HUD Step 4 — transitional HUD command input

The HUD now contains its own command input textbox for active command-driven tools. During this transition the bottom command row is still present, but it is hidden while the HUD is visible and remains available as the idle fallback.

The two textboxes are synchronized by the `MainWindow` code-behind helper methods. Command submission, autocomplete, history, Escape and Backspace should therefore behave as one logical command input buffer rather than as two separate inputs.

This step deliberately does not implement direct editing of Distance/Angle/Width/Height/Radius fields. Those fields still require a separate override model and will be handled after the HUD command input has been validated.

## Dynamic Command HUD Step 5 — HUD icon polish

The HUD header now uses the same icon resource family as the main toolbar. This keeps the cursor-adjacent command prompt visually tied to the selected tool without changing the command parser or tool state machines.

This step is intentionally visual only. The bottom command row remains as the idle fallback until command entry while no command is active has a dedicated HUD behavior.


## Dynamic Command HUD Step 6 — bottom fallback demotion

The bottom row is no longer treated as the active command prompt UI. It is now a compact idle fallback/launcher containing only the synchronized command input textbox. Active command name, prompt text, options and live measurements belong to the cursor-adjacent HUD.

This keeps command entry available when the HUD is hidden, while making the new HUD the primary command experience for active command-driven tools. The bottom row should not gain new command-specific behavior; future work should either move idle command entry into a dedicated HUD/launcher or delete the fallback when that path is stable.

## Dynamic Command HUD Step 7 — contextual read-only fields

The cursor HUD now chooses read-only field labels according to the active command phase rather than always displaying generic distance/angle data.

Current mapping:

- Line and generic two-point phases: `Distance`, `Angle`
- Rectangle opposite corner: `Width`, `Height`
- Rectangle Sides first side: `Width`, `Angle`
- Rectangle Sides second side: `Height`
- Circle radius phase: `Radius`
- Arc start-point/radius phase: `Radius`, `Angle`
- Arc end-direction phase: `Angle`

This is still a display-only feature. Typing into the command input continues to use the existing parser and submission flow. Direct editing of HUD numeric fields remains deferred until an explicit override model is introduced.

## Dynamic Command HUD Step 8 — extended contextual read-only fields

The read-only field mapping has been extended beyond the first draw tools. The HUD now displays more command-specific labels for ellipse, polygon, rotate, scale, offset, mirror, break-between, measure-angle and dimension phases.

Examples:

- Ellipse major axis: `Major radius`, `Angle`
- Ellipse minor radius: `Minor radius`
- Polygon vertex/radius: `Radius`, `Angle`
- Rotate destination/angle phase: `Angle`
- Scale destination/factor phase: `Factor`
- Offset two-point distance phase: `Distance`
- Radial dimensions: `Radius`
- Angular measurement/dimension phases: `Angle`

A generic fallback also maps prompt kinds such as `PointOrDistance`, `PointOrAngle`, `Distance` and `Angle` to appropriate read-only fields. This keeps the HUD useful for more commands without changing parser behavior or making the HUD responsible for geometry decisions.

The numeric fields are still display-only. Editable HUD field overrides remain deferred.


## Dynamic Command HUD Step 9 — generic option shortcuts

Command option shortcuts are now handled generically for command-driven tools. If the active prompt exposes options and the command input buffer is empty, pressing a matching shortcut key submits that option through the same command-input pipeline used by typed text.

This makes option behavior more harmonious across commands:

- Polyline options such as `A`, `C`, `U` and `L` still work.
- Fillet options such as `R` and `T` can be triggered directly.
- Trim options such as `A` and `U` can be triggered directly.
- Chamfer, Spline and other option-based commands benefit automatically when their prompt exposes `CommandOption` entries.

The logic intentionally does not fire when the input buffer already contains text. This prevents option shortcuts from interfering with normal command typing, autocomplete and numeric input.

## Dynamic Command HUD Step 10 — editable-field metadata scaffold

The HUD field model now includes metadata for the future editable numeric-field workflow. Fields are still displayed as read-only, but each field can now identify its semantic kind instead of relying only on its label.

Supported field kinds:

- `Distance`
- `Angle`
- `Width`
- `Height`
- `Radius`
- `Factor`
- `Generic`

This prepares a safer future implementation of workflows such as `Distance -> Tab -> Angle` because the UI can target stable field kinds rather than parsing labels like `Distance`, `Width` or `Radius`.

No input behavior has changed in this step. Numeric values typed by the user still go through the existing command input parser. HUD fields do not yet capture focus, do not freeze live values and do not apply geometry overrides.

## Dynamic Command HUD Step 11 — editable field input shell

HUD numeric fields are now focusable text boxes. This is a UI/input bridge, not the final override system.

Current behavior:

- Enter in a HUD numeric field submits the typed value through the existing command input parser.
- Escape resets the field text to its current live value and returns focus to the canvas.
- Empty field text is restored on focus loss.
- Typing in a HUD field is isolated from the global command-input text routing.

Important limitation: fields do not yet hold persistent overrides. For example, typing a distance and then an angle as two separate field values is not yet the final `1000 Tab 45 Enter` workflow. That requires the planned `CommandHudInputOverride` model and should be implemented first for Line only.

## Dynamic Command HUD Step 12 — Line one-shot field override

The first geometry-aware HUD field behavior is now enabled only for Line and only after the first point has been selected.

Current behavior:

- Enter in the `Distance` HUD field creates the second line point using the typed distance and the current live angle.
- Enter in the `Angle` HUD field creates the second line point using the current live distance and the typed angle.
- Unsupported fields and unsupported command phases fall back to the normal command input submission path.

This is intentionally a one-shot endpoint submission. It is not yet the final persistent override workflow. The field does not freeze the preview, and `Distance -> Tab -> Angle -> Enter` is still deferred to the future `CommandHudInputOverride` milestone.

## Dynamic Command HUD Step 13 — Line persistent Distance/Angle override

Line now has the first persistent HUD numeric override workflow.

Current behavior for `LineTool` after the first point:

- Editing `Distance` and leaving the field stores a temporary distance override and updates the preview.
- Editing `Angle` and leaving the field stores a temporary angle override and updates the preview.
- If both values are present, the preview uses both together.
- Pressing Enter in a HUD field confirms the endpoint using the stored override values.
- Pressing Escape in a HUD field clears the temporary overrides and restores the live value.

This enables the first version of the intended workflow:

```text
LINE
click first point
Distance = 100
Tab
Angle = 45
Enter
```

The implementation is still deliberately limited to Line. Other commands continue to use the editable-field shell or the normal command input parser until their specific override semantics are implemented.

## Dynamic Command HUD Step 14 — Polyline line-mode persistent Distance/Angle override

Step 14 extends the persistent HUD Distance/Angle override model from `LineTool` to `PolylineTool` in line mode.

Supported now:

```text
LINE
click first point
Distance = 100
Tab
Angle = 45
Enter
```

and:

```text
POLYLINE
click first point
Distance = 100
Tab
Angle = 45
Enter
```

For Polyline, the confirmed point becomes the next vertex and the command remains active for the following segment. The override applies only while the polyline is in straight segment collection mode. Polyline arc mode remains intentionally excluded from this step.

Implementation notes:

- the previous Line-only helper has been generalized to a Distance/Angle HUD override helper;
- supported active targets are currently:
  - `LineTool` waiting for the second point;
  - `PolylineTool` collecting straight vertices;
- preview still flows through `PreviewPointFromCommandLine(...)`;
- confirmation still flows through `SubmitPointFromCommandLine(...)`.


### Dynamic Command HUD mouse transparency update

The command HUD is now treated as a keyboard-driven overlay, not as a mouse target. The overlay is transparent to hit testing so fast mouse movement over `Distance`, `Angle` or the HUD command input cannot steal pointer events from the CAD canvas. Numeric HUD fields are entered from the keyboard using `Tab` from the command input, then `Tab` moves to the next numeric field and `Enter` confirms. This preserves the CAD rule that the mouse always remains free for picking points on the canvas.

## Dynamic Command HUD Step 16 — compact polar/coordinate input

The dynamic command HUD no longer uses a visible generic command textbox. The primary command input is now made of explicit numeric fields.

Compact field order for point-based commands:

```text
Distance [ ... ]  Angle [ ... ]  X [ ... ]  Y [ ... ]
```

Rules:

- A first numeric value typed while the canvas is focused enters the first editable HUD field, usually `Distance`.
- `Tab` advances through the field sequence.
- `Enter` confirms using the current HUD override state.
- `Escape` clears HUD overrides.
- `X` and `Y` represent absolute UCS coordinates.
- Mouse coordinates are displayed in the HUD, not in the status bar.
- The HUD remains transparent to the mouse; all point picking still belongs to the CAD canvas.

Polyline options and similar command options are rendered without square brackets. Only the shortcut letter is highlighted in the OpenCad2D yellow accent, for example:

```text
Line  Arc  Close
```

with only `L`, `A`, and `C` highlighted.

The visible bottom command row has been removed. Advanced command aliases still have an internal keyboard buffer, but the user-facing command UI is now the cursor HUD.

## Dynamic Command HUD Step 17 — numeric routing and first-point coordinates

Numeric routing has been refined after the compact HUD change:

- A first typed number is routed automatically only to a primary measure field: `Distance`, `Width`, `Radius`, or `Factor`.
- The first typed number is not routed automatically to `X`/`Y`, because coordinate entry should be an intentional mode selected with `Tab`.
- `Tab` can still enter coordinate mode: `X` first, then `Y`.
- Complete `X`/`Y` values can submit absolute UCS point input even when the command is waiting for the first point.
- Polar `Distance`/`Angle` point override remains limited to Line and Polyline line-mode for now.

## Dynamic HUD keyboard Tab trapping

The compact dynamic command HUD is keyboard-driven and mouse-transparent. `Tab` must never leave the HUD and move to the property panel while an interactive command HUD is visible and the command input buffer is empty.

Current rules:

- From the canvas, `Tab` focuses the first editable HUD field.
- From a HUD field, `Tab` commits the typed value without confirming the point and moves to the next editable HUD field.
- In a first-point phase with only `X`/`Y` fields, `Tab` starts at `X`.
- In a polar phase such as Line/Polyline after the first point, typing a number routes to `Distance`, and `Tab` moves to `Angle`.
- The HUD remains mouse-transparent; this behavior is implemented through keyboard routing, not pointer hit testing.

### Dynamic HUD keyboard routing stabilization

HUD numeric entry is keyboard-driven and mouse-transparent. The visual TextBox controls inside the HUD are display/edit affordances only; the active field is tracked logically by field kind. `Tab` cycles through the available fields, for example `Distance -> Angle -> X -> Y`, without using Avalonia focus traversal. After a point is confirmed by mouse or by Enter, temporary HUD overrides are cleared before the next input step.

## Dynamic HUD active field indication

The compact HUD uses logical keyboard focus instead of Avalonia TextBox focus. The current logical field is highlighted visually in the HUD using the OpenCad2D yellow accent. This makes keyboard input clear while preserving the rule that the HUD is transparent to the mouse.

Typical flows:

```text
POLYLINE
Tab        -> X is highlighted
100
Tab        -> Y is highlighted
50
Enter      -> first point at UCS X=100, Y=50
```

```text
POLYLINE
click first point
200        -> Distance is highlighted and overridden
Tab        -> Angle is highlighted
45
Enter      -> next vertex from Distance/Angle
```

The bottom command line and generic HUD textbox remain removed. Advanced command aliases and option routing still use the internal command buffer, but point entry is driven by explicit HUD fields.

## Dynamic HUD Step 23 — active-field routing correction

The numeric router must respect the active logical HUD field before choosing a preferred default field.

This prevents the following regression:

```text
POLYLINE
click first point
200        -> Distance
Tab        -> Angle
45         -> must stay in Angle, not return to Distance
Enter
```

Automatic numeric routing still does not choose `X` as the first field. First-point coordinate entry remains intentional:

```text
POLYLINE
Tab        -> X
100
Tab        -> Y
50
Enter
```

`Width`, `Height`, `Radius` and `Factor` are now accepted by the shared HUD field commit path, although full geometric semantics for every tool remain intentionally incremental.


### Step 24 — Freeze complementary polar HUD value

When the user starts typing a polar HUD value, the complementary live value is frozen immediately. Typing `Distance` freezes the current live `Angle`; typing `Angle` freezes the current live `Distance`. This prevents the preview update from recalculating the missing polar component from a changed/stale pointer state and keeps the value that was visible when typing began.

## Dynamic HUD stabilization checkpoint

The dynamic command HUD is currently considered stable only for Line and Polyline straight-segment input.

Stable keyboard rules:

- `Tab` enters and cycles logical HUD fields; it must not focus the Property Panel.
- First-point coordinate entry is explicit: `Tab -> X`, type X, `Tab -> Y`, type Y, `Enter`.
- A plain number during a first-point prompt does not automatically fill `X`.
- After a first/base point exists, a plain number fills `Distance` for Line and Polyline line-mode.
- `Distance -> Tab -> Angle -> Enter` confirms the next point from polar input.
- Starting to type `Distance` freezes the visible live `Angle`; starting to type `Angle` freezes the visible live `Distance`.
- Overrides are cleared after each confirmed point.

Rectangle, Circle, Arc and modify tools may show HUD fields, but editable routing must be added tool by tool in later isolated steps.

## Dynamic HUD modify tools — Move and Copy

`MOVE` and `COPY` now participate in the dynamic HUD input model.

When the tool asks for the base point, the HUD supports coordinate entry with:

```text
X [ ... ]  Y [ ... ]
```

When the tool asks for the destination point, the HUD supports:

```text
Distance [ ... ]  Angle [ ... ]
       X [ ... ]      Y [ ... ]
```

The distance-angle behavior follows the same rule already stabilized for Line and Polyline: entering a distance freezes the currently visible angle, entering an angle freezes the currently visible distance, and Enter confirms the destination point.

### Rotate / Scale scalar HUD input parser note

The contextual parser now treats `PointOrAngle` as accepting either a point or a numeric angle, and `PointOrNumber` as accepting either a point or a numeric value. This allows the HUD `Angle` field for Rotate and the HUD `Factor` field for Scale to submit scalar values through the same command-driven input path as typed command input.

## Dynamic HUD behavior for modify tools

The dynamic HUD now distinguishes point/scalar tools from selection-only tools.

Editable modify-tool HUD fields currently include:

- `Mirror`: `X/Y` for the first axis point; `Distance/Angle/X/Y` for the second axis point.
- `Break Point`: `X/Y` for the break point after the target entity has been selected.
- `Break Segment`: `X/Y` for the first break point; `Distance/Angle/X/Y` for the second break point.
- `Offset`: `Distance` in the distance phase; `Distance/X/Y` in the second distance point phase; `X/Y` for the side point.
- `Fillet`: `Radius` in the radius setup phase.
- `Chamfer`: `Distance` in the distance setup phase.
- `Boundary Fill`: `X/Y` for the seed point.

Selection-only tools such as `Trim`, `Extend`, `Delete`, `Explode`, and `Join` keep prompt/options only and should not show editable numeric fields.

`Create Block` and `Insert Block` remain outside the normal command-driven tool pipeline because they are controlled by option dialogs and pending placement state.

## Modify tool HUD regression coverage

The dynamic command HUD now has automated coverage for the first modify-tool integration pass. The tests protect the intended split between editable numeric/coordinate phases and selection-only phases:

- editable scalar/point phases: Mirror, Offset, Fillet, Chamfer, Boundary Fill;
- selection-only or immediate phases: Trim, Extend, Explode, Join.

Create Block and Insert Block remain outside this path because their current workflows use modal option windows and pending placement state rather than a normal `ICommandDrivenTool` pipeline.


### Step 30C - Mirror HUD Tab and option fix

- Tab is reserved for logical HUD field traversal while a command-driven tool is active; CadCanvas grip-edit Tab is now limited to SelectionTool.
- Command option shortcuts such as Mirror Yes/No are handled by the window preview key path after the generic HUD textbox removal.
- Manual check: MIRROR with a selected curve, Tab should enter X for first axis point; at delete-source prompt, Y/N should execute the option.

### Step 30D - Offset / Fillet / Chamfer scalar validation alignment

The HUD scalar validation now mirrors the modify tool rules instead of applying one global positive-distance rule everywhere.

- `Offset` distance must be greater than zero.
- `Fillet` radius may be zero; negative radius is rejected.
- `Chamfer` distance may be zero; negative distance is rejected.

This step does not alter the stable Line/Polyline/Rectangle/Circle/Rectangle-by-sides paths.
