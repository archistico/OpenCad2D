# Command input v0.8 plan

This document defines the planned CAD-style command input refactor for OpenCad2D v0.8.

The goal is to turn the current command line into a guided command system similar in spirit to AutoCAD: the user should always understand which command is active, which phase the command is in, what input is expected and which options are available.

This is a design document. It describes the intended behavior before implementation.

---

## Core principle

Every time a tool asks for a point, the user must be able to provide that point in either of these ways:

- click on the canvas, using the existing snap/ortho/polar workflow;
- type a coordinate, relative coordinate or polar coordinate in the command input.

Mouse input and text input must feed the same tool state machine. A tool should not have two separate behaviors for “mouse point” and “typed point”. Internally both should become a typed tool input such as `Point`.

Example:

```text
Command: LINE
Specify first point:
```

The user may click on the canvas or type:

```text
100,100
```

Then:

```text
Specify second point:
```

The user may click again or type:

```text
@100,0
```

or:

```text
@100<45
```

---

## Planned input formats

### Absolute point

```text
100,100
100.5,200.25
-50,30
```

Meaning: absolute `X,Y` in the current drawing coordinate system.

### Relative cartesian point

```text
@100,0
@0,50
@-20,10
```

Meaning: apply `delta X, delta Y` from the command reference point.

The reference point depends on the current command phase:

- `LINE`: the first point while specifying the second point;
- `POLYLINE`: the last inserted vertex;
- `MOVE`: the base point while specifying the destination;
- `COPY`: the base point while specifying the destination.

### Relative polar point

```text
@100<45
@250<0
@50<90
```

Meaning: distance and angle from the command reference point.

Angles are user-facing degrees:

```text
0°   = right
90°  = up
180° = left
270° = down
```

### Distance / number

When a command expects a distance, a single numeric value is valid:

```text
100
25.5
```

The same text is not valid when a full point is required.

### Options

Options are available only when the current prompt exposes them.

Example:

```text
POLYLINE: Specify next point or [Close/Undo]:
```

Valid inputs:

```text
C
Close
U
Undo
```

Option matching should accept both full keyword and shortcut.

---

## Enter behavior

### Empty Enter when no command is active

Repeat the last valid command.

Example:

```text
Command: LINE
...
Line created.
Command:
```

Pressing Enter starts `LINE` again.

Invalid commands and coordinate inputs must not become repeatable commands.

### Empty Enter while a command is active

Confirm the current phase only if the phase explicitly accepts confirmation.

Examples:

- `POLYLINE` next-point phase: finish the open polyline;
- `TRIM` cutting-edge selection phase: confirm selected cutting edges;
- `BREAK` first-point phase: invalid, because a point is mandatory.

---

## Escape behavior

Keep the existing CAD rule:

- first `Esc`: cancel the active command/tool;
- second `Esc`: clear the current selection.

This behavior must remain predictable after the command input refactor.

---

## Command prompt state

Each command-driven tool should expose its current prompt state.

Planned model:

```csharp
public sealed class CommandPromptState
{
    public string? CommandName { get; init; }
    public string Prompt { get; init; }
    public IReadOnlyList<CommandOption> Options { get; init; }
    public CommandInputKind ExpectedInput { get; init; }
    public bool AcceptsEmptyEnter { get; init; }
}
```

Displayed examples:

```text
Command:
LINE: Specify first point:
LINE: Specify second point:
POLYLINE: Specify next point or [Close/Undo]:
BREAK: Select entity:
BREAK: Specify first break point:
TRIM: Select cutting edges or [All]:
TRIM: Select object to trim or [Undo]:
```

---

## Planned input kinds

```csharp
public enum CommandInputKind
{
    CommandName,
    Point,
    Distance,
    Angle,
    Number,
    Option,
    PointOrOption,
    DistanceOrOption,
    Selection,
    SelectionOrOption,
    Confirm
}
```

The parser must use the expected input kind from the prompt. The same raw text can mean different things in different phases.

Example:

- `100` is a valid distance when a tool asks for a radius or offset distance;
- `100` is not a complete point when a tool asks for `X,Y`.

---

## Planned command option model

```csharp
public sealed class CommandOption
{
    public string Keyword { get; init; }
    public string Shortcut { get; init; }
    public string Description { get; init; }
}
```

Examples:

```text
Close / C
Undo / U
All / A
```

---

## Planned parser output

```csharp
public sealed class CommandInputSubmission
{
    public CommandInputSubmissionKind Kind { get; init; }
    public string RawText { get; init; }
    public Point2D? Point { get; init; }
    public double? Number { get; init; }
    public string? OptionKeyword { get; init; }
    public string? CommandName { get; init; }
}
```

Submission kinds:

```text
Command
Point
Distance
Option
Confirm
Invalid
```

---

## Planned command-driven tool contract

Tools that participate in the guided command system should implement a contract similar to:

```csharp
public interface ICommandDrivenTool
{
    CommandPromptState GetPromptState(ToolContext context);

    ToolResult HandleCommandInput(
        CommandInputSubmission input,
        ToolContext context);
}
```

The tool owns its own phase logic. The command input layer only parses text and routes typed submissions to the active tool.

---

## Command history

v0.8 should add a small visible command history near the command input.

Initial target: compact history, not a full console.

Example:

```text
Command: LINE
Specify first point:
> 100,100
Specify second point:
> @100<45
Line created.
```

History entry types may start as plain text but should conceptually distinguish:

- command;
- prompt;
- user input;
- result;
- warning;
- error.

---

## Command workflows planned for v0.8

### LINE

Decision: `LINE` remains a single-segment command.

```text
Command: LINE
Specify first point:
Specify second point:
Line created.
Command:
```

After the second point the command ends.

Supported typed input:

```text
100,100
@100,0
@100<45
```

### POLYLINE

```text
Command: POLYLINE
Specify start point:
Specify next point or [Close/Undo]:
```

Options:

```text
Close / C
Undo / U
```

Rules:

- `Close` closes the polyline when enough vertices exist;
- `Undo` removes the last inserted vertex/segment;
- empty Enter finishes the open polyline.

### RECTANGLE

```text
Command: RECTANGLE
Specify first corner:
Specify opposite corner:
```

### CIRCLE

```text
Command: CIRCLE
Specify center point:
Specify radius:
```

### ARC 3P

```text
Command: ARC3P
Specify start point:
Specify point on arc:
Specify end point:
```

### MOVE

```text
Command: MOVE
Select objects:
Specify base point:
Specify destination point:
```

Relative and polar destination input should use the base point as reference.

### COPY

```text
Command: COPY
Select objects:
Specify base point:
Specify destination point:
```

Relative and polar destination input should use the base point as reference.

### BREAK

```text
Command: BREAK
Select entity:
Specify first break point:
Specify second break point:
```

Mouse selection and typed points must be combinable.

### TRIM advanced base

The v0.8 target is an advanced but controlled Trim foundation:

```text
Command: TRIM
Select cutting edges or [All]:
Select object to trim or [Undo]:
```

Options:

```text
All / A
Undo / U
```

Rules:

- during cutting-edge selection, clicks add cutting edges;
- `All` uses all selectable visible entities as cutting edges;
- empty Enter confirms the cutting-edge selection;
- during trimming, each picked object is trimmed and the command remains active;
- `Undo` reverts the last trim operation inside the current Trim session;
- `Esc` exits Trim.

Advanced options such as Fence, Crossing, Edge, Project, Erase and Shift-to-Extend are deferred.

---

## Picked entity input for Trim and future tools

A real Trim operation cannot rely only on `EntityId`; it also needs the pick point to determine which segment should be removed.

Plan a richer picked-entity input model:

```csharp
public sealed class ToolPickedEntityInput
{
    public EntityId EntityId { get; init; }
    public Point2D PickPoint { get; init; }
    public Point2D? SnapPoint { get; init; }
    public double? DistanceToEntity { get; init; }
}
```

This will also be useful for:

- Extend;
- Break;
- Fillet;
- Chamfer;
- Offset side selection.

---

## Implementation phases for v0.8

### Block 1 - Specification and parser infrastructure

- Add command input models.
- Add parser for absolute, relative and polar coordinates.
- Add option parsing.
- Add tests only; avoid changing existing tool behavior.

### Block 2 - ViewModel and UI integration

- Add current prompt text.
- Add compact visible command history.
- Route empty Enter to repeat the last valid command when idle.
- Keep existing aliases and action commands working.

### Block 3 - Convert LINE

- Support mouse and typed point input through the same command phase.
- Support absolute, relative and polar second point.
- End command after second point.

### Block 4 - Convert POLYLINE

- Add prompt state and options.
- Support `Close`, `Undo` and empty Enter finish.
- Support absolute, relative and polar point input.

### Block 5 - Convert base drawing tools

- Rectangle exposes guided first-corner/opposite-corner prompts and accepts typed points.
- Circle exposes guided center/radius prompts and accepts typed points/direct radius-style input through the shared point/distance path.
- Arc 3P exposes guided start/point-on-arc/end prompts and accepts typed points.

### Block 6 - Convert Move, Copy and Break

- Guided selection phases.
- Base/destination point input.
- Break entity/point/point workflow.

### Block 7 - Trim advanced base

- Cutting-edge selection phase.
- `All` option.
- Trim-object phase.
- `Undo` option inside Trim.
- Picked-entity input with pick point.

### Block 8 - v0.8 stabilization

- Documentation updates.
- Regression tests.
- Release notes.
- README update if user-facing command input behavior changes substantially.

---

## Non-goals for v0.8

The following are intentionally deferred unless the implementation proves smaller than expected:

- full command console with scrollback and rich formatting;
- command autocomplete;
- command history navigation with arrow keys;
- right-click-as-Enter redesign;
- advanced Trim Fence/Crossing/Edge/Project/Erase modes;
- Shift-click Extend inside Trim;
- polar absolute input without `@`;
- advanced dynamic tracking from typed distances.

---

## Implementation status

### 2026-05-16 - Block 1 started

The initial parser infrastructure has been added without converting existing tools yet.

Implemented in `OpenCad2D.Tools.Input`:

- `CommandPromptState`;
- `CommandOption`;
- `CommandInputKind` for prompt expectations;
- `CommandInputSubmissionKind`;
- `CommandInputSubmission`;
- `ICommandDrivenTool`;
- contextual `CommandInputParser.Parse(input, promptState, referencePoint)`.

The legacy low-level parser is still used by the current command line path and remains compatible. Its result enum is now `CommandInputParseKind`, while `CommandInputKind` is reserved for the new v0.8 prompt expectation model.

The contextual parser already supports:

- command names when idle;
- empty Enter confirmation when allowed by the prompt;
- option shortcut/full-keyword matching;
- absolute coordinates: `100,50`;
- relative coordinates: `@25,-10`;
- relative polar coordinates: `@100<45`;
- distance, angle, number and text inputs.

Next implementation block: add current prompt state and a compact command history to the view-model/UI, then keep existing tools working before converting `LINE`.

## v0.8 block 2 - visible prompt and command history

The command line now exposes two separate histories:

- Logical command history: stores command aliases used for repeat/history logic. Coordinate input is intentionally not stored here.
- Visible command history: a short UI log that shows command input, prompts, feedback, errors and repeat messages.

The visible history is intentionally small and capped to the latest entries so it can stay embedded in the main window without becoming a full console.

Current UI structure:

```text
Command history, latest entries
Current prompt
Input box
```

The current input box placeholder is contextual. Until individual tools are converted to `ICommandDrivenTool`, it shows general examples such as:

```text
100,50   |   @50,0   |   @100<45
```

Empty Enter is part of the command input workflow. When no command is actively consuming a confirm action, it repeats the last command.

## v0.8 block 3 - LINE migration

`LINE` is the first command migrated to the command-driven model.

Flow:

```text
Command: LINE
LINE: Specify first point:
LINE: Specify second point:
Line created.
```

Supported text input:

```text
100,50      absolute user coordinate
@50,0       relative coordinate from the first point
@100<45     relative polar coordinate from the first point
5           direct distance in the current cursor direction
```

Mouse clicks continue to work as before. Command-line points are resolved explicitly and submitted to the tool without snap re-resolution so typed coordinates remain exact.


## Implemented v0.8 command-driven tools

### LINE

`LINE` is implemented as a command-driven two-point tool. It remains a single-segment command:

```text
LINE: Specify first point:
LINE: Specify second point:
Line created.
```

The second point accepts absolute coordinates, relative coordinates, relative polar coordinates and direct distance input.

### POLYLINE

`POLYLINE` is implemented as a command-driven multi-point tool:

```text
POLYLINE: Specify first point:
POLYLINE: Specify next point or [Close/Undo]:
```

Supported input while collecting vertices:

- `100,50` for an absolute point;
- `@50,0` for a relative point from the previous vertex;
- `@100<45` for a relative polar point from the previous vertex;
- a direct distance value using the current cursor direction;
- `C` / `Close` to close the polyline;
- `U` / `Undo` to remove the last vertex;
- empty Enter to finish an open polyline.

Mouse input and text input share the same command state. Mouse input still resolves snaps and angle constraints; typed coordinates are submitted as already resolved points.

## v0.8 block 6 - Edit tools

The first edit tools now participate in the command-driven model.

### MOVE

```text
MOVE: Select objects to move:
MOVE: Specify base point:
MOVE: Specify destination point:
```

If objects are already selected, the command starts from the base point phase. If no objects are selected, the user selects them on the canvas and presses Enter. Destination input accepts absolute coordinates, relative coordinates, relative polar coordinates and direct distance.

### COPY

```text
COPY: Select objects to copy:
COPY: Specify base point:
COPY: Specify destination point:
```

`COPY` mirrors the `MOVE` workflow but creates translated copies of the selected entities.

### BREAK

```text
BREAK: Select entity:
BREAK: Specify first break point:
BREAK: Specify second break point:
```

The target entity is selected from the drawing canvas because breaking requires an entity pick. The two break points can be supplied either by mouse or by command input.

## v0.8 command coverage pass before advanced Trim

The following edit/modify commands are now part of the CAD-style prompt system:

| Command | Prompt/input behavior |
| --- | --- |
| `ROTATE` | Requires a preselection, then asks for base point, reference point, and destination point or typed angle in degrees. |
| `SCALE` | Requires a preselection, then asks for base point, reference point, and destination point or typed scale factor. |
| `ALIGN` | Requires a preselection, then asks for source/destination point pairs and a final scale confirmation. Enter or `N` means no scale; `Y` applies scale. |
| `BREAKPOINT` / `BP` | Select the entity on the canvas, then type or click the break point. |
| `BREAKSEGMENT` / `BREAK` / `BR` / `BS` | Already command-driven: select the entity on the canvas, then type or click the two break points. |
| `EXTEND` | Exposes selection prompts for boundary and target entity selection. Entity picking remains canvas-based. |
| `TRIM` | Exposes selection prompts for cutting edge and target side selection. This is the baseline before the advanced Trim redesign. |
| `DELETE` / `DEL` | Press Enter to delete the current selection; otherwise select objects first. |

Additional parser prompt kinds were introduced for edit tools:

- `PointOrAngle`: used by Rotate destination input.
- `PointOrNumber`: used by Scale destination input.

Typed point formats remain the same:

```text
100,50
@50,0
@100<45
```

For Rotate, a plain number in the final phase is interpreted as an angle in degrees. For Scale, a plain number in the final phase is interpreted as a scale factor.

## Trim advanced base in v0.8

`TRIM` is the first command that uses a richer guided workflow.

```text
TRIM: Select cutting edge or [All]:
TRIM: Select entity side to trim or [All/Undo]:
```

Supported v0.8 behavior:

- click a cutting edge to start trimming;
- Ctrl-click while trimming to add further cutting edges;
- type `A` or `All` to use all visible supported entities as cutting edges;
- click the side of the target entity to remove;
- type `U` or `Undo` to undo the last trim operation made during the current Trim command;
- press Enter while trimming to finish/reset the Trim command;
- press Escape to cancel.

When `All` is used, the picked target entity is removed from the effective cutting-edge list for that trim operation. This allows CAD-style all-edge trimming without preventing the target from being picked.

Future versions can extend this workflow with Fence, Crossing, Edge, Project and Shift-to-Extend behavior.
