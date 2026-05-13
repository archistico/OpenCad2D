# Polar Tracking

Polar Tracking is the configurable angular constraint used while placing points interactively.

It generalizes classic Ortho mode:

```text
Ortho             horizontal/vertical only
Polar Tracking    multiples of a selected angle step
```

The current UI exposes Polar Tracking in the top CAD bar with a `Polar:` ComboBox.

Available options:

```text
Off
90°
45°
30°
15°
```

Examples:

```text
90° -> 0°, 90°, 180°, 270°
45° -> 0°, 45°, 90°, 135°, 180°, ...
30° -> 0°, 30°, 60°, 90°, 120°, ...
15° -> 0°, 15°, 30°, 45°, 60°, ...
```

---

## Runtime model

Polar Tracking is currently a runtime/session setting, not document data.

Main types:

```text
AngleConstraintSettings
AngleConstraintService
ToolInputConstraintService
PolarTrackingOptionViewModel
```

`AngleConstraintSettings` stores whether Polar Tracking is enabled and the angular step in degrees.

`AngleConstraintService` is a pure service. It receives:

```text
base point
candidate point
angle settings
```

and returns either the original point or a projected point constrained to the nearest allowed direction.

`ToolInputConstraintService` is the shared entry point used by tools. It applies Polar Tracking when enabled, otherwise it can fall back to legacy Ortho.

---

## Input order

OpenCad2D currently uses this order:

```text
raw cursor point
-> snapping
-> Polar Tracking / Ortho angle constraint
-> preview and command commit
```

This means snapping is evaluated first. The snapped candidate can then be projected onto the nearest polar direction from the current base point.

The reason for keeping this logic outside `SnapService` is that snapping should only choose candidates. Angular constraints are a tool/input concern.

---

## Distance preservation

Polar Tracking preserves the distance from the base point to the candidate point.

Conceptually:

```text
dx = candidate.X - base.X
dy = candidate.Y - base.Y
distance = length(dx, dy)
angle = atan2(dy, dx)
constrainedAngle = nearest multiple of StepDegrees
result = base + direction(constrainedAngle) * distance
```

For a zero-length vector, the original point is returned unchanged.

---

## Tool integration

Current integration points:

```text
TwoPointToolBase
MoveTool
PolylineTool
```

This covers ordinary two-point drawing and edit workflows, including preview geometry and committed commands.

Direct distance input also uses the effective constrained direction. For example, with Polar `45°`, after picking a base point the user can move the cursor near 45°, type `100`, and create a 100-unit segment along the 45° direction.

---

## Relationship with Ortho

Polar Tracking has priority when enabled.

```text
Polar enabled -> use Polar Tracking
Polar Off and Ortho enabled -> use Ortho
Polar Off and Ortho Off -> free point input
```

The `90°` Polar option is similar to Ortho, but it goes through the generalized angular-constraint path.

---

## Scope notes

Polar Tracking is a point-placement aid. It should not be confused with explicit typed angles used by transformation tools such as Rotate.

For tools where the second point does not represent a free direction, apply the constraint carefully. Rectangle creation, radius resize grips and explicit typed transform values may need separate rules.


## RotateTool limitation

Polar Tracking is currently a point-placement aid. `RotateTool` computes the rotation angle from base/reference/destination points and can use Ortho for 90-degree constraint, but it does not yet apply the selected Polar Tracking angle step to explicit rotate-angle computation.
