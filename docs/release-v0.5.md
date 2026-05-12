# Release notes - v0.5 Advanced editing and refinement

OpenCad2D v0.5 is a stability-first milestone focused on the modify tools. It follows v0.4 Basic Dimensions and deliberately avoids major UI/command-line changes so the editing core can be consolidated before v0.6.

---

## Main goals

- Improve Break Point beyond simple line splitting.
- Improve Break Segment beyond simple line splitting.
- Add two-cutting-edge Trim for line targets.
- Consolidate Extend behavior for supported entity types.
- Make hidden/locked layer behavior explicit and covered by tests.
- Preserve undo/redo behavior through command-based document mutations.

---

## Completed features

### Break Point

Implemented support:

```text
LineEntity       -> two line segments
ArcEntity        -> two arcs preserving direction
Open Polyline    -> two open polylines
Closed Polyline  -> one open polyline cut at the break point
CircleEntity     -> not applicable with a clear message
```

The CircleEntity decision is intentional: one break point on a circle is ambiguous. Circle splitting is supported through Break Segment instead.

### Break Segment

Implemented support:

```text
LineEntity       -> zero, one or two remaining line segments
ArcEntity        -> zero, one or two remaining arcs
CircleEntity     -> remaining major arc after removing the minor arc
Open Polyline    -> zero, one or two open polylines
Closed Polyline  -> one open polyline after removing the shortest path
```

Break points are projected onto the target entity before the operation is applied.

### Trim

Implemented:

- existing one-cutting-edge Trim preserved;
- optional two-cutting-edge workflow for line targets;
- second cutting edge selected with Ctrl-click;
- click on the target portion decides which interval is removed;
- remaining fragments are returned through the same undoable modify command path;
- preview shows remaining geometry;
- removed line segment is highlighted when available.

Workflow:

```text
activate Trim
pick first cutting edge
Ctrl-click second cutting edge
pick target line portion to remove
```

### Extend

Consolidated target support:

```text
LineEntity          supported
ArcEntity           supported
Open PolylineEntity supported at endpoints
CircleEntity        not applicable
Closed Polyline     not applicable
Point/Text/Dimension not applicable
```

Preview highlights the newly added portion for line, arc and open-polyline targets where supported.

---

## Layer behavior

The v0.5 layer rule is now explicit:

```text
Hidden layer:
    entity is not a target
    entity is not a reference/cutting edge/boundary

Locked visible layer:
    entity can be a reference/cutting edge/boundary
    entity cannot be a target modified by the operation
```

This allows, for example, a locked wall/reference line to be used as an Extend boundary without making it editable.

---

## Tests added or consolidated

v0.5 includes tests for:

- Break Point on line, arc and polyline targets;
- Break Segment on line, arc, circle and polyline targets;
- Trim with one and two cutting edges;
- Extend on line, arc and open-polyline targets;
- unsupported targets with clear messages;
- hidden-layer behavior;
- locked-layer behavior;
- undo/redo regression paths for covered modify workflows.

---

## Known limits after v0.5

The following are intentionally not part of v0.5:

- native command-line workflows for modify tools;
- editable Property Panel integration;
- two-cutting-edge Trim for all entity types;
- advanced choice of minor/major arc in Break Segment on circles;
- grip editing of dimensions;
- DXF import or PDF export.

These areas are better handled by later milestones after v0.6 introduces stronger command input and editable properties.

---

## Next milestone

The next planned milestone is:

```text
v0.6 - Real command line and Property Panel v2
```

Main direction:

- command aliases;
- absolute and relative coordinate input;
- command history;
- contextual command feedback;
- right-click repeat-last-command;
- editable Property Panel v2;
- all property edits must be undoable commands.
