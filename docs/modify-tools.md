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

Entity-selection phases should use `SnapKind.EntityOnly`. Geometric point phases should use the normal geometric snap set. Numeric prompts may be completed by typing a value, by supported graphical input, or by confirming the proposed value shown in angle brackets such as `<N>` or `<10>`. Current confirmations explicitly covered by this policy include Polygon side count, Fillet radius/trim mode, Mirror delete-source choice, Polyline finish and Delete multi-pick confirmation.

When a tool acquires a geometric point from pointer input, it must use the snap-resolved point, not the raw cursor position. This applies to every point collection phase, including center points, axis endpoints, radius points, rectangle corners, polyline vertices and preview updates. Command-line coordinates are already explicit and should remain exact.

The Select area includes explicit selection actions:

- `Select` starts the selection tool;
- `Select All` selects all visible and unlocked entities;
- `Select Last` restores the last cleared selection;
- `Deselect` clears the current selection without deleting geometry.

---

## Polyline completion

Polyline follows the global confirmation policy:

```text
Left click  -> add another vertex
Enter       -> finish the open polyline when at least two vertices exist
Right click -> same as Enter, finish the open polyline when at least two vertices exist
C / Close   -> create a closed polyline when at least three vertices exist
U / Undo    -> remove the last vertex
```

If the user confirms with fewer than two vertices, the tool stays active and reports that at least two points are required.

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
OFFSET: Specify offset distance or first distance point <last>:
OFFSET: Specify second distance point or type distance:
OFFSET: Select object to offset:
OFFSET: Specify side to offset:
```

Distance input rules:

- a typed positive distance immediately becomes the current offset distance;
- two graphical clicks can be used to measure the distance;
- after a distance has been provided, Enter or right-click can reuse the last offset distance shown in `<...>`;
- on first use, Enter/right-click without a previous distance shows a clear message and does not advance;
- object selection uses `SnapKind.EntityOnly`;
- side selection uses geometric snaps and excludes entity-only snap.

Targets supported in v0.9:

- Line;
- Circle;
- Arc;
- straight-segment open/closed Polyline.

Deferred targets:

- Ellipse and Elliptical Arc: a true offset is not another exact ellipse, so OpenCad2D does not silently create an imprecise native-looking result;
- Bezier Spline: a true offset is not another exact Bezier spline, so spline offset remains deferred rather than being hidden as a permanent polyline approximation.

Rules:

- line offset creates a parallel line;
- circle/arc offset changes radius based on picked side;
- polyline offset uses miter joins with a conservative miter limit;
- very sharp joins fall back to a bevel-style corner instead of creating long spikes;
- unsupported advanced curves return a clear deferred-support message and create no geometry;
- invalid or degenerate results are rejected;
- live preview is shown while choosing the side.

Future work:

- configurable join style;
- rounded joins;
- advanced self-intersection cleanup;
- polyline bulge/arc segment support;
- Multiple/Through/Erase/Layer options.

Current cleanup status:

- Distance workflow completed: typed distance, two-click distance measurement, reusable last distance and right-click/Enter default confirmation.
- Target/side workflow completed for the v0.9 scope: EntityOnly target selection, geometric side picking, target highlight, addition preview and explicit unsupported-curve messages.

---

## Fillet

Workflow:

```text
FILLET: Select first line or [Radius/Trim] <r> (Trim):
FILLET: Specify fillet radius <r>:
FILLET: Specify trim mode <Trim>:
FILLET: Select second line:
```

Targets:

- Line-Line.

Rules:

- Radius option sets the active fillet radius;
- right click/Enter at the radius prompt confirms the current radius shown in `<...>`;
- right click/Enter at the trim-mode prompt confirms the current trim mode;
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

- if entities are already selected when the tool starts, `DELETE` removes the current selection immediately through an undoable command;
- if no entity is selected, the tool enters a multi-entity picking phase;
- the entity-picking phase uses `SnapKind.EntityOnly`;
- each left click toggles an entity in the pending delete selection;
- Enter or right click confirms deletion of all selected entities;
- text and multiline text can be picked by clicking anywhere inside their estimated bounding box.

Text hit testing intentionally uses the estimated text bounding box rather than only the insertion point. This makes text behave like a selectable annotation object in Move, Copy, Delete and other selection-driven workflows.

## Boundary and first-entity selection feedback

Selection-oriented modify phases should give persistent visual feedback after the user selects an entity that remains active in the command state.

Current rules:

- TRIM highlights selected cutting edges as `Emphasis` overlays while the removable interval remains a separate dashed `Removal` highlight.
- EXTEND highlights the selected boundary entity as an `Emphasis` overlay while the added interval remains a separate `Addition` highlight.
- FILLET highlights the first selected line as an `Emphasis` overlay while waiting for the second line.
- FILLET entity picking uses `SnapKind.EntityOnly` for both the first-line and second-line phases, because geometric endpoint/midpoint snaps are not useful when the command expects objects.

This keeps three different concepts visually distinct: command state entities, geometry that will be removed, and geometry that will be added.


### Rectangle Sides exact second-side length

After the first side is defined, the Rectangle Sides tool accepts either a second-side point or a typed distance. When a distance is typed, the current mouse side only determines the sign/direction of the second side; the typed value is used as the exact side length. For example, after a first side from `(0,0)` to `(100,0)`, typing `100` creates a rectangle with a second side exactly 100 units long, regardless of the current mouse distance.


### Rectangle Sides exact typed second side length

When the first side is already defined, a plain numeric command input such as `100` defines the exact second-side length. The current mouse position or snap point is used only to choose the side of the first segment. The resulting rectangle must therefore satisfy `P = 2A + 2B`, where `A` is the first side and `B` is the typed second-side length.


### Offset target/side preview

Offset now separates its workflow into distance, target selection and side selection phases. Target selection uses `EntityOnly` snapping and the selected target remains highlighted while the user chooses the side. Moving the pointer on either side of the target displays the offset result as an `Addition` preview; clicking the side creates the offset and keeps the tool active so another entity can be offset with the same distance.


---

## Final Modify Tools UX consistency pass

The final cleanup pass checks the remaining primary tools against the same interaction contract.

Covered tools:

- Select, Deselect and Delete;
- Move, Copy, Rotate, Scale, Align and Mirror;
- Trim, Break Point, Break Segment, Extend, Offset and Fillet;
- Polyline, Polygon, Rectangle Sides and Ellipse.

Consistency rules now documented and guarded by tests:

- right click is routed through the same confirmation path as empty Enter for command-driven tools;
- Move, Copy, Rotate, Scale and Mirror can enter an entity-selection phase when started without an existing selection, then right-click/Enter advances to the first geometric input once a valid selection exists;
- Delete confirms the pending multi-pick deletion with Enter/right-click;
- Polyline and Spline messages describe Enter/right-click completion consistently;
- Polygon side count, Fillet radius/trim mode, Mirror delete-source and Offset last-distance prompts expose safe defaults and reject missing defaults with clear messages;
- entity-selection phases expose `SnapKind.EntityOnly`;
- geometric point phases expose the active geometric snap set;
- option-only confirmation phases that do not need a canvas point expose no point snap mode;
- `Cancel`/`Deactivate` clears transient base points, previews and pending state so tools do not leave dirty intermediate state behind.

Additional snap-mode coverage was added for Break Point, Break Segment, Extend and Align. These tools now explicitly implement `ISnapModeProvider`, closing the remaining gap where a selection phase could otherwise leave geometric snap modes active in the UI.
