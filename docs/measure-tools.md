# Measure Tools

Measure tools query drawing geometry without modifying the document.

They do not execute commands, do not add entities and do not change any document state. They exist only to give the user quantitative information about the drawing.

---

## Main idea

Measure tools use the same snapping and point input infrastructure as drawing tools. The user picks points and the tool computes and displays measurements.

Results are displayed in a dedicated measurement readout area, either in the status bar or in a small persistent panel near the command line. The result remains visible until the tool is cancelled, deactivated or a new measurement is started.

Measure tools must not modify the document. They must not create commands. They must not add entities to the drawing.

---

## DistanceTool

`DistanceTool` measures the straight-line distance between two picked points.

### Workflow

```text
activate Distance
pick first point
pick second point
display result
tool remains active for a new measurement
```

### Displayed values

After picking both points, the tool displays:

```text
Distance   straight-line distance between the two points
DX         horizontal component (X difference)
DY         vertical component (Y difference)
Angle      angle of the segment from the positive X axis, in degrees
```

Values are displayed in the current document units with the current linear precision setting.

### Continuous measurement

After the second point is picked and the result is shown, the tool resets and waits for a new first point. The user can measure multiple distances in sequence without reactivating the tool.

### Snapping

Snapping applies normally during point selection. The user can snap to endpoints, midpoints, intersections and other snap candidates just as with drawing tools.

---

## AreaTool

`AreaTool` measures the area of a closed region.

Two modes of operation are supported: entity selection and manual point picking.

### Mode 1: entity selection

The user clicks on a closed entity. The tool computes and displays its area and perimeter immediately.

Supported entities:

```text
PolylineEntity with IsClosed = true
CircleEntity
```

Clicking an open polyline or a line entity does nothing. The tool waits for a valid entity.

Displayed values:

```text
Area       enclosed area in current document units squared
Perimeter  total boundary length in current document units
```

For a `CircleEntity`:

```text
Area      = π × r²
Perimeter = 2 × π × r
```

For a closed `PolylineEntity`, the shoelace formula is used for area. Perimeter is the sum of segment lengths.

### Mode 2: manual point picking

The user picks a sequence of points. The tool computes the area of the polygon defined by those points.

Workflow:

```text
activate Area tool
pick point 1
pick point 2
pick point 3
...
press Enter to close and display result
```

While points are being added, the tool shows a preview polygon and updates the area in real time as each point is added.

On Enter, the polygon is closed and the final area and perimeter are displayed.

Displayed values:

```text
Area       area of the manually defined polygon
Perimeter  total boundary of the polygon (sum of segments plus closing segment)
```

### Mode switching

The tool mode is determined automatically:

- if the first click hits a closed entity within hit-test tolerance, entity selection mode activates;
- if the first click does not hit any closed entity, manual point picking mode activates.

The user does not need to select a mode explicitly.

### Snapping

Snapping applies normally in both modes.

---

## Measure tools and the tool pipeline

Measure tools receive `PointerInfo` through the same tool pipeline as drawing tools.

They use `ToolContext` for snapping and document queries.

They must not call `CommandHistory.Execute`.

They must not call any `CadDocument` mutation method.

They should expose current measurement state for the UI to display.

---

## Display format

Measurement results use:

- the document unit system (see `DrawingSettings`);
- the linear precision setting for lengths and areas;
- the angular precision setting for angles;
- squared units for area (e.g. `mm²`, `m²`).

The readout should be clear enough to be copied or read quickly. A future improvement can add a copy-to-clipboard action for measurement results.
