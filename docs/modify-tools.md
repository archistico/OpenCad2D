# Modify Tools: Break, Trim, Extend

This document defines the planned direction for the next modification tools.

These tools are not implemented yet. They are the next planned CAD editing area after Rotate, Scale, Align and PolylineTool.

---

## Shared design rules

Break, Trim and Extend must:

- be UI-independent tools in `OpenCad2D.Tools`;
- use geometry services for calculations;
- use hit testing and snapping where appropriate;
- provide preview when useful;
- commit through undoable commands;
- mutate the document only through `CadDocument`;
- respect hidden and locked layer behavior;
- preserve entity ids when replacing entities;
- use `CompositeCommand` when a single user operation creates multiple low-level document changes.

---

## BreakTool

Break splits an entity at one or two points.

### Recommended v1

Start with `LineEntity` only.

Workflow:

```text
activate Break
pick line entity
pick break point
split line into two line entities
commit as one undoable operation
```

For a line from A to B and break point P:

```text
result 1: A -> P
result 2: P -> B
```

Validation:

- break point must lie on or near the entity;
- break point must not be too close to the start or end;
- locked-layer entity cannot be broken.

Potential command structure:

```text
remove original line
add two resulting lines
```

wrapped in a `CompositeCommand`, or a dedicated `BreakEntityCommand` that stores original and pieces.

### Future improvements

- two-point break;
- break circle into arc(s);
- break polyline segment;
- break at intersections.

---

## ExtendTool

Extend lengthens an entity until it reaches a boundary.

### Recommended v1

Start with line-to-line extension.

Workflow:

```text
activate Extend
pick boundary entity
pick line entity near the end to extend
extend that end until intersection with boundary
commit replacement
```

Rules:

- use pick location to decide which endpoint extends;
- compute intersection with boundary;
- replace the original line with the extended line;
- if no valid intersection exists, do nothing and report a message.

### Future improvements

- multiple boundaries;
- extend to circle/arc/polyline;
- fence/window selection;
- continuous extend mode.

---

## TrimTool

Trim cuts an entity using one or more boundaries.

### Recommended v1

Start with line trimmed by line boundary.

Workflow:

```text
activate Trim
pick cutting boundary
pick line segment side to remove
replace original line with remaining part
```

Rules:

- use pick location to decide which side is removed;
- compute intersection with boundary;
- if the picked portion can be removed, replace with the remaining portion;
- if trimming produces no valid geometry, remove the entity;
- if trimming creates multiple parts in future, use composite command.

### Future improvements

- multiple cutting boundaries;
- circle/arc trim;
- polyline trim;
- trim by window/fence;
- preview highlighted part to remove.

---

## Geometry services

The first step should be testable geometry services, for example:

```text
BreakGeometryService
ExtendGeometryService
TrimGeometryService
```

These services should operate on geometry primitives/entities and return result objects describing:

```text
success/failure
new entity or entities
message/reason when no operation is possible
```

Only after services are tested should UI tool integration be added.

---

## Suggested implementation order

```text
Phase 1: Break line at point service + tests
Phase 2: BreakTool for LineEntity + UI + tests
Phase 3: Extend line to line service + tests
Phase 4: ExtendTool v1
Phase 5: Trim line by line service + tests
Phase 6: TrimTool v1
```

This keeps each step small and recoverable.
