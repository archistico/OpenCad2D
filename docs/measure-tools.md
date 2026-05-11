# Measure Tools

Measure tools query drawing geometry without modifying the document.

They do not execute commands, do not add entities and do not change document state. They only compute quantitative information and show it through the command/status feedback area.

OpenCad2D model space currently has no physical unit. Measurement values are therefore displayed as plain model values, without `mm`, `m`, inches or other unit suffixes.

---

## Implemented tools

The current `MEASURE` group in the left tool panel contains:

```text
Distance
Entity
Angle
Area
```

Implemented tool classes:

```text
MeasureDistanceTool
MeasureEntityTool
MeasureAngleTool
MeasureAreaTool
```

All measure tools share these rules:

- they do not create entities;
- they do not execute document commands;
- they do not affect undo/redo history;
- they do not mark the document as dirty;
- they use the same snap and input infrastructure as other tools where appropriate;
- they report results as command/status messages.

---

## Core measurement services

Pure measurement logic lives in `OpenCad2D.Core.Measurements`.

Main types:

```text
MeasurementService
DistanceMeasurement
AngleMeasurement
EntityMeasurement
MeasurementFormatter
```

`MeasurementService` contains the geometry-independent measurement rules used by the interactive tools. `MeasurementFormatter` formats values for UI output using invariant numeric formatting and no physical unit suffix.

Current numeric display uses up to three decimal places:

```text
0.###
```

Examples:

```text
Distance: 125.43 | ΔX: 100 | ΔY: 75 | Angle: 36.87°
Angle: 45° | Supplementary: 315°
Circle | Radius: 25 | Diameter: 50 | Circumference: 157.08 | Area: 1963.495
```

---

## Measure Distance

`MeasureDistanceTool` measures the straight-line distance between two picked points.

### Workflow

```text
activate Distance
pick first point
pick second point
show result
tool resets and waits for another first point
```

### Displayed values

```text
Distance   straight-line distance between the two points
ΔX         horizontal component
ΔY         vertical component
Angle      direction from first point to second point, normalized to 0..360 degrees
```

Example:

```text
Distance: 100 | ΔX: 80 | ΔY: 60 | Angle: 36.87°
```

### Preview and input

After the first point, the canvas shows a temporary measurement preview from the first point to the current cursor/snap point.

The tool supports:

```text
mouse point
absolute coordinates
relative coordinates
direct distance
object snap
Polar Tracking / Ortho
```

The same point resolution rule used by drawing tools applies:

```text
raw point -> snap -> Polar Tracking / Ortho -> preview/result
```

---

## Measure Entity

`MeasureEntityTool` measures the entity clicked by the user.

### Workflow

```text
activate Entity
click entity
show result
tool stays active for another entity
```

The tool uses entity snap only:

```text
SnapKind.EntityOnly
```

It supports `Ctrl+click` to cycle through overlapping entities, matching the selection-oriented entity picking behavior.

### Supported entities

#### LineEntity

```text
Length
Angle
```

#### CircleEntity

```text
Radius
Diameter
Circumference
Area
```

#### ArcEntity

```text
Radius
Diameter
Sweep
Length
```

#### PolylineEntity

For open polylines:

```text
Length
Vertices
Closed: No
```

For closed polylines:

```text
Length / Perimeter
Area
Vertices
Closed: Yes
```

Polyline area is calculated with the shoelace formula and returned as a positive value regardless of vertex winding direction.

---

## Measure Angle

`MeasureAngleTool` measures an angle from three picked points.

### Workflow

```text
activate Angle
pick first ray point
pick vertex point
pick second ray point
show result
tool resets and waits for another first ray point
```

The measured angle is the angle at the vertex between:

```text
vertex -> first ray point
vertex -> second ray point
```

### Displayed values

```text
Angle          smaller angle in degrees
Supplementary 360° - Angle
```

Example:

```text
Angle: 45° | Supplementary: 315°
```

### Preview and input

The tool shows temporary ray previews while collecting the points. It supports snap, typed coordinates, relative coordinates, direct distance and Polar Tracking / Ortho.

---

## Measure Area

`MeasureAreaTool` measures the area of a closed polyline entity.

### Workflow

```text
activate Area
click closed polyline
show area/perimeter result
tool stays active for another closed polyline
```

The tool uses entity snap only:

```text
SnapKind.EntityOnly
```

It supports `Ctrl+click` to cycle through overlapping entities.

### Supported entity

Current implementation intentionally accepts only:

```text
PolylineEntity with IsClosed = true
```

The tool rejects:

```text
LineEntity
CircleEntity
ArcEntity
open PolylineEntity
```

Circle and arc area values are still available through `MeasureEntityTool`; `MeasureAreaTool` is currently focused on closed polyline regions.

---

## Non-mutating behavior

Measure tools are intentionally different from drawing, modify and transform tools.

They must not call:

```text
CommandHistory.Execute
CadDocument.AddEntity
CadDocument.RemoveEntity
CadDocument.ReplaceEntity
CadDocument.ReplaceEntities
```

They may update tool-local state and status messages, but they must not change the document.

Undo/redo must therefore remain unchanged after using a measure tool.

---

## Future improvements

Planned or possible follow-ups:

```text
Measure Point / Coordinates
Measure Area by picked points
measurement history panel
copy measurement result to clipboard
configurable decimal precision
document/user unit display settings
entity hover highlight during Measure Entity / Measure Area
```
