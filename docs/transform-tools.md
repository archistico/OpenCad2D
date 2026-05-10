# Transform Tools

This document describes implemented and planned transform tools.

---

## Implemented transform tools

### RotateTool

Rotates the current selection around a base point.

Workflow:

```text
select entities
activate Rotate
pick base point
pick reference point
pick destination point
commit rotation
```

The rotation angle is computed from the angular difference between the reference vector and destination vector:

```text
angle = angle(base -> destination) - angle(base -> reference)
```

Ortho constrains interactive rotation to multiples of 90 degrees.

The tool shows a preview and commits through an undoable command.

---

### ScaleTool

Scales the current selection uniformly around a base point.

Workflow:

```text
select entities
activate Scale
pick base point
pick reference point
pick destination point
commit scale
```

Scale factor:

```text
factor = distance(base, destination) / distance(base, reference)
```

The factor must be positive. The tool shows a preview and commits through an undoable command.

---

### AlignTool

Aligns the current selection by mapping two source points to two destination points.

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

The transformation is:

```text
1. translate source point 1 to destination point 1
2. rotate source vector onto destination vector
3. optionally apply uniform scale
```

After the fourth point:

```text
Enter or N -> apply without scale
Y          -> apply with scale
```

The confirmation is case-insensitive at the key level.

---

## Design rules

Transform tools must:

- require a valid selection;
- show preview where meaningful;
- commit with undoable commands;
- preserve entity ids when replacing entities;
- use `CadDocument.ReplaceEntities(...)` or equivalent document APIs;
- respect locked-layer protection;
- keep calculations outside Avalonia.

---

## Future transform utilities

### MatchPropertiesTool

Future tool to copy layer assignment from a source entity to destination entities.

Since appearance is layer-driven, copying properties initially means assigning the destination entities to the source entity's layer.

### PolygonTool

Future drawing utility to create regular closed polylines.

Potential modes:

- inscribed;
- circumscribed;
- by edge.

### Direct typed angle/factor

Future enhancement:

- Rotate: type an explicit angle.
- Scale: type an explicit factor.

Typed angle/factor should be distinct from ordinary direct distance entry to avoid ambiguity.
