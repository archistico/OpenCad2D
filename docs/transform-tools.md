# Transform Tools and Utilities

This document covers tools that transform existing entities geometrically, copy properties between entities and create regular geometric shapes.

---

## RotateTool

`RotateTool` rotates the current selection around a base point by a specified angle.

### Workflow

```text
select entities
activate Rotate
pick base point (center of rotation)
pick a reference point, or type angle on the command line
execute RotateEntitiesCommand
```

### Angle input

Two input modes are supported:

**Interactive:** pick a reference point after the base point, then pick a destination point. The rotation angle is the angular difference between the two vectors from the base point.

**Direct:** type a numeric angle in degrees on the command line after picking the base point. Positive angles are counterclockwise.

Ortho mode constrains interactive angle input to multiples of 90 degrees.

Explicit typed angles are not altered by Ortho mode.

### Command

`RotateEntitiesCommand` stores:

```text
entity ids
original entity copies (for undo)
base point
rotation angle in degrees
```

Execute applies a rotation transformation to each entity and replaces the originals through `CadDocument.ReplaceEntities(...)`.

Undo restores the original entities.

Locked-layer entities are rejected by `CadDocument` at the mutation boundary. Selection normally prevents this because locked-layer entities are not selectable.

### Preview

While the user moves the pointer to choose the destination point, the canvas renders a rotated preview of the selected entities. The rotation angle is shown in the status bar.

---

## ScaleTool

`ScaleTool` scales the current selection uniformly around a base point.

### Workflow

```text
select entities
activate Scale
pick base point (center of scaling)
pick a reference point and then a destination point, or type scale factor on the command line
execute ScaleEntitiesCommand
```

### Scale factor input

Two input modes are supported:

**Interactive:** pick a reference point after the base point, then pick a destination point. The scale factor is:

```text
factor = distance(base, destination) / distance(base, reference)
```

**Direct:** type a positive numeric factor on the command line after picking the base point. For example, `2` doubles the size. `0.5` halves it.

The scale factor must be strictly positive. Zero and negative values are rejected.

### Command

`ScaleEntitiesCommand` stores:

```text
entity ids
original entity copies (for undo)
base point
scale factor
```

Execute applies a uniform scale transformation to each entity and replaces the originals through `CadDocument.ReplaceEntities(...)`.

Undo restores the original entities.

### Preview

While the user moves the pointer to choose the destination point, the canvas renders a scaled preview of the selected entities. The current scale factor is shown in the status bar.

---

## AlignTool

`AlignTool` moves, rotates and optionally scales the current selection by mapping two source points to two destination points. This matches the behavior of the ALIGN command in AutoCAD.

### Workflow

```text
select entities
activate Align
pick source point 1
pick destination point 1
pick source point 2
pick destination point 2
confirm or decline scale option
execute AlignCommand
```

### Transformation computation

The transformation is composed of three steps:

**Step 1 – Translation:** translate the selection so that source point 1 coincides with destination point 1.

**Step 2 – Rotation:** rotate the selection around destination point 1 so that the direction from source 1 to source 2 matches the direction from destination 1 to destination 2.

**Step 3 – Scale (optional):** after the rotation, uniformly scale around destination point 1 so that the distance between the two source points matches the distance between the two destination points.

The scale step is optional. The tool asks the user whether to apply scaling after the four points are chosen. If the user declines, only translation and rotation are applied.

If the two source points or the two destination points coincide, the rotation and scale steps are skipped and only translation is applied.

### Four-point input model

`AlignTool` manages its own point collection state and does not derive from `TwoPointToolBase`.

The tool state machine is:

```text
WaitingForSourcePoint1
WaitingForDestinationPoint1
WaitingForSourcePoint2
WaitingForDestinationPoint2
WaitingForScaleConfirmation
```

At each state transition, a preview of the partial or complete transformation is shown.

### Command

`AlignCommand` stores:

```text
entity ids
original entity copies (for undo)
composed transformation matrix (translation + rotation + optional scale)
```

The transformation is pre-computed before the command is created. The command stores the final matrix, not the four input points.

Execute replaces entities with transformed versions through `CadDocument.ReplaceEntities(...)`.

Undo restores the original entities.

### Preview

During point selection, the canvas renders a preview showing the progressively constrained transformation. After all four points are picked, the fully transformed preview is shown before the scale confirmation.

---

## MatchPropertiesTool

`MatchPropertiesTool` copies the layer assignment from a source entity to one or more destination entities.

### Workflow

```text
activate Match Properties
click source entity (reads its layer)
click destination entity 1 (assigned to source layer)
click destination entity 2
...
press Enter or Escape to finish
```

The source entity is picked first. All subsequent clicks select destination entities. Each destination entity is immediately moved to the source layer.

The tool remains active until the user presses Escape or Enter, allowing many entities to be updated in a single operation.

### Command

`MatchLayerCommand` stores:

```text
source layer id
list of (entity id, original layer id) pairs
```

Execute sets each target entity's layer to the source layer.

Undo restores each entity to its original layer.

All replacements go through `CadDocument.ReplaceEntities(...)`.

### Restrictions

Destination entities on locked layers are skipped silently.

Source entities on locked layers can be read. The layer itself is not locked; only mutation of entities on that layer is restricted.

---

## PolygonTool

`PolygonTool` creates regular polygons.

A regular polygon is a closed `PolylineEntity` with N equal sides and equal interior angles.

### Input modes

Three input modes are supported:

**Inscribed (default):** the user picks the center point and a vertex point. The circumradius is the distance from the center to the vertex. All vertices lie on the circumscribed circle.

**Circumscribed:** the user picks the center point and a midpoint of one edge. The inradius is the distance from the center to the edge midpoint. All edge midpoints lie on the inscribed circle.

**By edge:** the user picks two consecutive vertex points. The polygon is constructed from the edge length and direction, extending in a consistent direction.

### Workflow

```text
activate Polygon tool
type number of sides on the command line (minimum 3)
choose input mode (inscribed, circumscribed, by edge)
pick first point (center or first vertex)
pick second point (vertex, edge midpoint or second vertex)
execute AddEntityCommand
```

The number of sides can be typed before picking the first point. The mode can be cycled with a key (for example Tab or a shortcut shown in the prompt).

### Number of sides

The minimum is 3 (equilateral triangle). There is no enforced maximum, but values above a practical limit such as 256 are unusual and may be clamped.

After choosing a number of sides once, the tool remembers it for the next polygon until the tool is deactivated.

### Result

The result is always a closed `PolylineEntity` with exactly N vertices.

Vertex coordinates are computed analytically from the center, radius and angle offset:

```text
for i in 0..N-1:
    angle = baseAngle + i * (2π / N)
    vertex = center + Vector(cos(angle), sin(angle)) * radius
```

The base angle is determined by the second picked point.

### Command

`AddEntityCommand` with the computed closed polyline.

### Preview

While the user moves the pointer to choose the second point, the canvas renders the polygon preview updated in real time.

---

## ToolRegistry additions

When adding the tools described in this document, the following entries must be added to `ToolRegistry`:

```text
Rotate
Scale
Align
MatchProperties
Polygon
```

Each entry requires:

```text
ToolId enum value
ToolRegistry registration
UI tool button
keyboard shortcut binding
tests
documentation
```
