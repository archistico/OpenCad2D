# Modify Tools

Modify tools change existing geometry. Tools own workflow state; geometry calculations should be delegated to focused services; mutations must be undoable.

---

## Command confirmation and snap policy

OpenCad2D modify tools use a consistent confirmation model:

```text
Left click  -> graphical input or entity pick
Right click -> confirm the current prompt when a default value or a valid current selection exists
Enter       -> keyboard equivalent of right click for command prompts
Esc         -> cancel the current phase; when idle, clear selection where applicable
```

Right click is not a generic cancel action. It advances or completes the current phase only when the tool can do so safely. If a command requires a value and no default exists yet, the tool must show a clear message instead of guessing.

Entity-selection phases should use `SnapKind.EntityOnly`. Geometric point phases should use the normal geometric snap set. Numeric prompts may be completed by typing a value, by supported graphical input, or by confirming the proposed value shown in angle brackets such as `<N>` or `<10>`.

The Select area includes explicit selection actions:

- `Select` starts the selection tool;
- `Select All` selects all visible and unlocked entities;
- `Select Last` restores the last cleared selection;
- `Deselect` clears the current selection without deleting geometry.

---

## Native curve-editing policy

Trim and Break are being consolidated around the shared curve-editing architecture described in `docs/curve-editing.md`.

Binding rule for future implementation:

```text
CAD editing operations modify native entities using native geometric parameters.
Sampling is allowed only as temporary support, never as the permanent source of edited geometry when a native representation exists.
```

For explicit-vertex entities such as lines and polylines, shared intersections should be reused directly as resulting endpoints or inserted vertices. For parametric entities such as circles, arcs and elliptical arcs, the native curve parameter rebuilds the resulting entity while the shared point is used as validation/refinement input. Open Bezier spline splitting is now native; closed spline editing remains deferred.

---

## Break Point

Current targets:

- Line;
- Arc;
- Ellipse;
- Polyline;
- Bezier Spline, open splines are split natively.

Circle is not applicable in the current Break Point workflow; use Break Segment for circles. Full-ellipse Break Point is intentionally a no-op until a stable full-sweep elliptical arc convention exists. Existing `EllipticalArcEntity` instances can be broken into native elliptical arc fragments. Open Bezier splines are split natively; closed splines remain deferred.

Workflow:

```text
BREAKPOINT: Select entity:
BREAKPOINT: Specify break point:
```

The picked point is projected onto the target entity. Degenerate pieces are rejected.

---

## Break Segment

Current targets:

- Line;
- Arc;
- Circle;
- Ellipse;
- Polyline;
- Bezier Spline, open splines are split natively.

Workflow:

```text
BREAK: Select entity:
BREAK: Specify first break point:
BREAK: Specify second break point:
```

For circles, the minor arc between the two projected points is removed and the result is a native `ArcEntity`. For ellipses, the removed portion is represented by native `EllipticalArcEntity` fragments rather than permanent polyline approximations. Open polylines can be broken on internal segments. Closed polylines and regular polygons are opened and the shortest path between points is removed.

---

## Extend

Boundary support:

- Line;
- Circle;
- Arc;
- Ellipse;
- Elliptical Arc;
- Polyline.

Target support:

- Line;
- Arc;
- Elliptical Arc;
- open Polyline.

Workflow:

```text
EXTEND: Select boundary entity:
EXTEND: Select target entity:
```

The target endpoint nearest the picked side is extended to the boundary when a valid extension exists.

---

## Trim

Cutting-edge support:

- Line;
- Circle;
- Arc;
- Ellipse;
- Elliptical Arc;
- Polyline.

Target support:

- Line;
- Circle;
- Arc;
- Ellipse;
- Elliptical Arc;
- Polyline;
- Bezier Spline, open splines are returned as native spline fragments.

Workflow:

```text
TRIM: Select cutting edge or [All]:
TRIM: Select entity side to trim or [All/Undo]:
```

Features:

- `All` uses all visible supported entities as cutting edges;
- target entity is excluded from its own cutting-edge set in All mode;
- additional cutting edges can be selected;
- `Undo` reverses the last trim inside the active Trim command;
- command remains active for repeated trims until cancelled or confirmed;
- while moving over a trimmable target, the removed portion is shown as a dashed preview highlight;
- open and closed polylines, including regular polygons stored as closed `PolylineEntity`, can be trimmed;
- trimmed polyline/polygon fragments are returned as open `PolylineEntity` fragments;
- trimmed ellipse fragments are returned as native `EllipticalArcEntity` fragments;
- trimmed open spline fragments are returned as native `BezierSplineEntity` fragments, preserving layer/style/draw-order metadata;
- current architecture: Trim uses `CadCurveSplitService` and `ICurveAdapter` so line/polyline outputs reuse shared cut points, circle/arc outputs remain native arcs, ellipse outputs become native elliptical arcs, and open spline outputs remain native Bezier fragments. During Trim, the active snap set is `SnapKind.EntityOnly`: geometric point snaps such as endpoint, midpoint, center, quadrant, intersection, nearest, perpendicular, tangent and grid are intentionally disabled because Trim is an entity/side selection workflow.

---

## Offset

Workflow:

```text
OFFSET: Specify offset distance:
OFFSET: Select object to offset:
OFFSET: Specify side to offset:
```

Targets:

- Line;
- Circle;
- Arc;
- straight-segment open/closed Polyline;
- Bezier Spline, offset through sampled polyline approximation.

Rules:

- line offset creates a parallel line;
- circle/arc offset changes radius based on picked side;
- polyline/spline approximation offset uses miter joins with a conservative miter limit;
- very sharp joins fall back to a bevel-style corner instead of creating long spikes;
- invalid or degenerate results are rejected;
- live preview is shown while choosing the side.

Future work:

- configurable join style;
- rounded joins;
- advanced self-intersection cleanup;
- polyline bulge/arc segment support;
- Multiple/Through/Erase/Layer options.

---

## Fillet

Workflow:

```text
FILLET: Select first line or [Radius/Trim] <r> (Trim):
FILLET: Specify fillet radius:
FILLET: Specify trim mode <Trim>:
FILLET: Select second line:
```

Targets:

- Line-Line.

Rules:

- Radius option sets the active fillet radius;
- radius `0` creates a sharp-corner join;
- radius greater than `0` creates a tangent arc;
- while selecting the second line, Fillet shows a live preview of the final result;
- `Trim` mode trims/replaces the source lines and adds the tangent arc;
- `NoTrim` mode keeps the source lines unchanged and adds only the tangent arc;
- radius `0` requires `Trim` mode because `NoTrim` would not create new geometry.

Future work:

- Line-Arc;
- Arc-Arc;
- polyline fillet.

### Break Segment removal preview

Break Segment now exposes a semantic preview descriptor while the second break point is being chosen. The remaining native fragments are shown as normal preview geometry, while the interval that will be removed is emitted as a `Removal` highlight so the application can render it with the same dashed removal style used by TRIM.

The preview uses `CadBreakService.GetRemovedSegmentBetweenPoints`, which delegates to `CadCurveSplitService.GetIntervalBetweenPoints`; therefore the displayed removal interval is built from the same native curve adapters used by the final BREAK operation.


## Preview semantics for modify tools

Modify tools use semantic preview highlights so that the user can distinguish the effect before confirming the command:

- `Trim` and `Break Segment` highlight the interval that will be removed as `Removal`; the renderer draws it dashed.
- `Extend` highlights the new interval that will be added as `Addition`; the renderer draws it as a solid added-geometry highlight.
- Other generic transient highlights can keep `Emphasis`.

The preview must remain consistent with the geometry service used by the final command. A dashed or highlighted interval should not be computed with a separate approximation if the final command uses native curve parameters.


## Preview UX clarification for modify commands

TRIM, BREAK and EXTEND now use explicit preview semantics so the command line and canvas describe the same operation:

- TRIM highlights the portion that will be removed with a dashed removal preview.
- BREAK Segment highlights the interval between the two break points with a dashed removal preview.
- EXTEND highlights the portion that will be added with an addition preview.

For closed curves, BREAK Segment uses the order of the two picked points to determine which interval is removed. The command message should make this explicit because closed curves have two possible paths between the same two points.

EXTEND accepts boundary entities that can produce native intersection points, including lines, circles, arcs, ellipses, elliptical arcs and polylines. Targets remain limited to entities with extendable endpoints: lines, arcs, elliptical arcs and open polylines. Closed curves such as circles, complete ellipses and closed polylines are intentionally rejected as EXTEND targets because they do not have a natural endpoint.

## Modify tools selection-first UX

Interactive modify tools should follow the same selection-first pattern where practical:

- if a compatible selection already exists, the tool starts directly at its first geometric input;
- if no entities are selected, the tool enters an entity-selection phase;
- during entity selection, only entity selection snaps should be active;
- after confirming the selection, geometric snaps return for point input;
- command-line prompts should tell the user whether they are selecting entities or specifying geometry.

This pass aligns `RotateTool` and `ScaleTool` with the existing Move, Copy and Mirror behavior. They now support selecting entities after the command starts, use `EntityOnly` snapping during that selection phase, and then continue with their base/reference/destination workflow.

---

## Delete UX cleanup

Delete follows the entity-selection policy used by Modify tools:

- if entities are already selected, `DELETE` removes the current selection through an undoable command;
- if no entity is selected, the tool enters an entity-picking phase;
- the entity-picking phase uses `SnapKind.EntityOnly`;
- after an entity is picked, Enter confirms deletion;
- text and multiline text can be picked by clicking anywhere inside their estimated bounding box.

Text hit testing intentionally uses the estimated text bounding box rather than only the insertion point. This makes text behave like a selectable annotation object in Move, Copy, Delete and other selection-driven workflows.
