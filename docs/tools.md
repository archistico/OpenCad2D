# Tools

Tools are UI-independent CAD interaction objects. They live in `OpenCad2D.Tools` and receive input through the tool pipeline.

A tool should not depend on Avalonia controls, windows or drawing APIs.

---

## Tool pipeline

Typical flow:

```text
Avalonia input
-> CadCanvas / MainWindow
-> CadWorkspace
-> ToolController
-> active ICadTool
-> command creation/execution when needed
```

The App converts UI events into CAD input and forwards them. The tool owns command-specific interaction state.

---

## ToolContext

`ToolContext` provides shared runtime services and state:

- document;
- command history;
- selection set;
- selection service;
- snap service;
- grid settings;
- current layer;
- UCS/WCS conversion;
- current base point;
- Ortho state;
- tolerances.

Tools should use `ToolContext` instead of reaching into UI code.

---

## Command line input

The command line supports point input:

```text
100,50   absolute UCS point
@50,0    relative UCS offset
5        direct distance entry
```

The command line resolves typed input to a point and submits it to the active tool as if it came from the mouse.

Special command keys may be interpreted by active tools. Examples:

```text
PolylineTool: Enter finishes, C closes
AlignTool:    Enter/N confirms without scale, Y confirms with scale
```

---

## Drawing tools

### LineTool

Creates a line from two points.

Supports:

- mouse points;
- typed absolute/relative coordinates;
- direct distance;
- snap;
- Ortho;
- preview;
- undo/redo through `AddEntityCommand`.

### RectangleTool

Creates a closed `PolylineEntity` from two opposite corners.

### CircleTool

Creates a `CircleEntity` from center and radius point.

The radius can come from:

- a second mouse point;
- snap;
- command line direct distance.

### PolylineTool

Creates an open or closed `PolylineEntity` from a sequence of points.

State model:

```text
WaitingForFirstPoint
CollectingVertices
```

Behavior:

```text
point input -> add vertex
Enter       -> finish open polyline, requires at least 2 vertices
C           -> close polyline, requires at least 3 vertices
Esc         -> cancel
```

The current base point is updated to the last accepted vertex so relative input and direct distance continue from the last segment.

Ortho applies to the current segment only.

---

## SelectionTool

Selection supports:

- click selection;
- Shift-click toggle;
- window selection;
- crossing selection.

Selection uses selectable entities:

```text
visible and unlocked entities
```

Hidden entities and locked-layer entities are not selectable.

---

## Basic edit tools

### MoveTool

Moves selected entities by base point and destination point.

### CopyTool

Copies selected entities by base point and destination point.

### DeleteTool

Deletes selected entities.

All edit tools use document commands and remain protected by locked-layer rules.

---

## Transform tools

### RotateTool

Workflow:

```text
select entities
activate Rotate
pick base point
pick reference point
pick destination point
commit rotation
```

The angle is:

```text
angle(base -> destination) - angle(base -> reference)
```

Ortho constrains interactive rotation to multiples of 90 degrees.

### ScaleTool

Workflow:

```text
select entities
activate Scale
pick base point
pick reference point
pick destination point
commit scale
```

Factor:

```text
factor = distance(base, destination) / distance(base, reference)
```

Scale factors must be positive.

### AlignTool

Workflow:

```text
select entities
activate Align
pick source point 1
pick destination point 1
pick source point 2
pick destination point 2
confirm scale option
```

After the fourth point:

```text
Enter or N -> apply translation + rotation only
Y          -> apply translation + rotation + uniform scale
```

Align owns a multi-step state machine and does not derive from `TwoPointToolBase`.

---

## GripEditTool

Grip editing modifies an existing selected entity through grip points.

Activation:

```text
Tab
```

Selection rule:

```text
one selected entity       -> edit that entity
multiple selected entities -> edit the last selected entity
no selection              -> no operation
```

Grip edit state:

```text
Idle       -> show grips, hover detection
GripActive -> one grip is active, preview modified entity
```

Grip edits commit through replacement commands and remain undoable.

---

## Ortho

Ortho constrains point input from the current base point.

```text
if |DX| >= |DY| -> horizontal
if |DY| >  |DX| -> vertical
```

Tools that use Ortho should apply it before preview, measurements and direct distance results so “what you see is what you get”.

---

## Future modify tools

Next planned modify tools:

```text
Break
Trim
Extend
```

They should follow these rules:

- work on selected or picked entities;
- use hit testing/snapping where appropriate;
- calculate geometry in service classes;
- preview before committing when useful;
- commit through undoable commands;
- modify documents only through `CadDocument` APIs;
- respect locked-layer protection.
