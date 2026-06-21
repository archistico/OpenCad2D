# Shared Specification — Leader and Arrowhead System

## Purpose

OpenCad2D needs a common leader/arrowhead model for several features: the Arrow tool, section labels, coordinate callouts, future annotation markers, and possibly future dimension-style improvements. This document defines shared terminology and behavior so each feature can reuse the same visual and editing concepts.

## Core concepts

Use a common style vocabulary:

```text
LeaderGeometry
LeaderStyle
ArrowHeadStyle
ArrowHeadPlacement
```

Suggested first persistent properties:

| Property | Meaning |
|---|---|
| `startPoint` | first point of the leader/arrow path |
| `endPoint` | last point of the leader/arrow path |
| `vertices` | optional intermediate leader vertices for broken leaders |
| `arrowAtStart` | whether an arrowhead is drawn at the first node |
| `arrowAtEnd` | whether an arrowhead is drawn at the last node |
| `arrowHeadType` | visual arrowhead shape |
| `arrowHeadSize` | model-space size of arrowhead |
| `lineStyle` | layer/color/line format inheritance or explicit overrides |

The first implementation may support only straight leaders. The data model should not prevent a later broken leader with one or more elbows.

## Arrowhead types

Initial supported types should be small and reliable:

```text
Open
Closed
Filled
Dot
Slash
ArchitecturalTick
```

A feature may expose fewer types in its first implementation if rendering/export support is not ready. The canonical names above should still be reserved so future tools do not invent incompatible names.

## Placement

Arrowheads can be placed at:

```text
None
Start
End
Both
```

The UI may display this as separate toggles (`Start arrow`, `End arrow`) or as a single placement option. Internally it should map to the same two booleans or equivalent enum.

## Geometry rules

Arrowhead size is in model units, not screen pixels. Zooming must not change the model-space size of committed arrowheads. A future annotation scale system may override this, but until then arrow size should behave like text size or dimension arrow size.

The arrowhead direction is derived from the local tangent of the leader path at the endpoint. For a straight leader this is the vector from start to end. For a polyline leader, the start arrow uses the first segment and the end arrow uses the last segment.

Zero-length leaders are invalid. Commands should reject them with a clear message instead of creating invisible or degenerate geometry.

## HUD behavior

The Dynamic HUD should expose only context-relevant values:

- while picking the first/last point, show point coordinates;
- when editing arrow options, show arrow placement, size and type;
- `TAB` enters/cycles editable HUD fields;
- direct mouse hover over HUD fields must not focus them before edit mode;
- changes update preview but do not commit until confirmation.

Suggested options:

```text
S = toggle start arrow
E = toggle end arrow
Z = arrow size
T = arrow type
```

Specific commands can choose different shortcuts if there is a conflict, but the meaning should stay consistent.

## Rendering and export

Rendering should use generated linework/fill geometry so SVG/PDF/PNG output looks like the canvas. DXF export can either export true annotation primitives when safe or export generated line/polyline geometry. The first reliable target is visible compatibility, not semantic DXF fidelity.

If `Filled` arrowheads are used before a general HatchEntity exists, filled triangles can be exported as closed lightweight polylines with fill support where available. If a target export format cannot preserve the fill, fallback to `Closed` arrowheads and document the limitation.

## Reuse by tools

The following tools should use this shared system:

- `ARROW`: direct line/leader with configurable arrowheads;
- Section Label: section direction arrows and optional line caps;
- Coordinate Callout: leader from bubble to measured point;
- future detail/elevation tags;
- future dimension-style enhancements if aligned with the existing dimension model.

## Persistence

Use stable string names for arrowhead types and placements. Missing values in older files should load with tool defaults. Invalid values should fall back to a safe visible style such as `Open` or `Closed` and should not prevent the drawing from loading.

## Testing requirements

Automated tests should cover:

- start/end/both/none placement;
- arrowhead size changes;
- save/reopen;
- transform behavior under move/rotate/scale where applicable;
- SVG/PDF/DXF generated output presence;
- zero-length rejection.

Manual tests should check preview orientation, HUD editing, and visual consistency at multiple zoom levels.
