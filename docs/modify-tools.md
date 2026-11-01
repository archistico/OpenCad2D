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

Entity-selection phases should use `SnapKind.EntityOnly`. Geometric point phases should use the normal geometric snap set. Numeric prompts may be completed by typing a value, by supported graphical input, or by confirming the proposed value shown in angle brackets such as `<N>` or `<10>`. Current confirmations explicitly covered by this policy include Polygon side count, Fillet radius/trim mode, Mirror delete-source choice, Polyline finish, Delete multi-pick confirmation, Explode confirmation and Join confirmation.

When a tool acquires a geometric point from pointer input, it must use the snap-resolved point, not the raw cursor position. This applies to every point collection phase, including center points, axis endpoints, radius points, rectangle corners, polyline vertices and preview updates. Command-line coordinates are already explicit and should remain exact.

The Select area includes explicit selection actions:

- `Select` starts the selection tool;
- `Select All` selects all visible and unlocked entities;
- `Select Last` restores the last cleared selection;
- `Deselect` clears the current selection without deleting geometry.

---

## Explode and Join

`EXPLODE` and `JOIN` are selection-first modify tools:

```text
EXPLODE: Select polylines or blocks to explode, or press Enter to finish:
JOIN: Select lines, arcs and open polylines to join, or press Enter to finish:
```

Behavior:

- `EXPLODE` converts selected polylines into `LineEntity` (straight segments) and `ArcEntity` (bulge segments) objects, including the closing segment of closed polylines, and block references into their world-space entities.
- `JOIN` converts selected connected line, arc and open-polyline chains into one or more `PolylineEntity` objects. Closed chains are detected and stored as closed polylines.
- Both tools use `SnapKind.EntityOnly` during selection.
- A click always picks: it adds or removes the clicked entity, also when a selection already existed before the command started.
- Enter/right-click executes the current selection and ends the command; with nothing selected it ends the command directly. If nothing can be converted, the tool stays active with an explanation.
- Only editable entities are used: deleted entities and entities on hidden or locked layers are removed from the selection and reported as skipped.
- Zero-length segments (degenerate lines, repeated polyline vertices, a repeated closing vertex) are ignored: Explode creates no zero-length entities and Join does not mistake them for a branching junction or keep duplicate vertices.
- Join rejects two segments that describe the same curve twice (for example two overlapping copies of a line), which would otherwise become a zero-area closed polyline; a line and an arc closing a D shape stay valid.
- Explode places block entities whose layer no longer exists on the block reference layer and skips nested block references whose definition is missing; the completion message reports both cases.
- Created entities preserve the source layer, style, visibility, lock flag and draw order.
- The operation is a single undoable command. `CadDocument.AddEntities` validates the whole batch before adding anything, and `ModifyEntitiesCommand` restores the removed entities if the additions are rejected, so a failure never leaves the document half-modified.

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
BREAKPOINT: Select entity or press Enter to finish:
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
BREAK: Select entity or press Enter to finish:
BREAK: Specify first break point:
BREAK: Specify second break point:
```

For circles, the minor arc between the two projected points is removed and the result is a native `ArcEntity`. For ellipses, the removed portion is represented by native `EllipticalArcEntity` fragments rather than permanent polyline approximations. Open polylines can be broken on internal segments. Closed polylines and regular polygons are opened and the shortest path between points is removed.

### Rules shared by Break Point and Break Segment

- Both break points (and the Break Point point) are projected onto the native curve, also when they are typed far from the entity.
- When exactly one editable supported entity is selected before the command starts, it is used as the target and the command starts at the first point prompt; otherwise, or for unsupported/non-editable entities, the target is picked on the canvas.
- The target is checked again before every preview and before the break: if it was deleted, changed (undo, grip edit, another command) or its layer was hidden or locked in the meantime, the command asks for the target again instead of failing or restoring the old geometry.
- Typed coordinates are exact; snaps apply to pointer input only.
- After each break the tool asks for the next target; Enter/right-click at the target prompt ends the command. The broken entity is removed from the selection.

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

## Move, Copy, Rotate and Scale

Workflow:

```text
MOVE / COPY:  [select objects] -> base point -> destination point
ROTATE:       [select objects] -> base point -> rotation angle or [Copy/Reference] <last>
                Reference: reference angle <last> (typed, or two points) -> new angle or [Points] <reference>
SCALE:        [select objects] -> base point -> scale factor or [Copy/Reference] <last>
                Reference: reference length <last> (typed, or two points) -> new length or [Points] <reference>
```

Rotate and Scale follow the AutoCAD workflow:

- right after the base point a typed value is the rotation angle (counter-clockwise degrees, negative values allowed) or the scale factor; Enter/right-click reuses the last value shown in `<...>`;
- a clicked point defines the angle of the direction from the base point, measured from the X axis of the current UCS; for Scale its distance from the base point, in drawing units, is used as the factor;
- `C` / `Copy` transforms a copy and keeps the source objects (it applies to the current operation only);
- `R` / `Reference` asks for a reference angle or length (typed, or two clicked points) and then for the new angle or length (typed, clicked from the base point, or `P` / `Points` for two points); the result is new minus reference for Rotate and new divided by reference for Scale;
- ortho and polar tracking apply to clicked direction points only; typed coordinates stay exact;
- the HUD shows `Angle` (Rotate) or `Factor` (Scale) as the first editable field in the value phases, `Reference length`/`New length` in the Scale reference phases, and `X/Y` for clicked points.

Rules:

- the tools only change editable entities: selected entities that were deleted (for example by undo) or that are on hidden or locked layers are removed from the selection and skipped, and the completion message reports how many were skipped; if nothing editable remains the tool returns to object selection;
- the operation is a single undoable command; the document never ends up partially transformed when a selected entity cannot be changed (`CadDocument.ReplaceEntities` and `RemoveEntities` validate the whole batch before changing anything);
- operations that would not change the geometry are rejected with a message and create no undo step: zero displacement, a rotation of 0° or a full turn (including a destination point on the reference ray or on the base point) and a scale factor of 1;
- typed points stay exact for every tool: ortho and polar adjustments apply only to pointer input;
- after each completed operation the tool stays active for another base point; Enter or right-click at the base-point prompt ends the command, so a new command can be typed.

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
- the first distance point can also be typed (`x,y`); in the second-point phase a typed point (`x,y`, `@dx,dy`, `@d<a`) measures the distance and a plain number is used directly as the distance, without needing a cursor direction;
- after a distance has been provided, Enter or right-click can reuse the last offset distance shown in `<...>`;
- on first use, Enter/right-click without a previous distance shows a clear message and does not advance;
- object selection uses `SnapKind.EntityOnly`;
- side selection uses geometric snaps and excludes entity-only snap;
- Enter or right-click while selecting the object or choosing the side ends the command (AutoCAD `<Exit>` behaviour).

Targets supported in v0.9:

- Line;
- Circle;
- Arc;
- open/closed Polyline (straight segments; bulged segments through the conservative linear approximation described in `known-limitations.md`).

Deferred targets:

- Ellipse and Elliptical Arc: a true offset is not another exact ellipse, so OpenCad2D does not silently create an imprecise native-looking result;
- Bezier Spline: a true offset is not another exact Bezier spline, so spline offset remains deferred rather than being hidden as a permanent polyline approximation.

Rules:

- line offset creates a parallel line;
- circle/arc offset changes radius based on picked side;
- polyline offset uses miter joins with a conservative miter limit on outer corners;
- very sharp outer joins fall back to a bevel-style corner instead of creating long spikes;
- inner corners are always trimmed at the exact intersection of the offset segments, so sharp inner corners do not create self-crossing loops;
- segments fully consumed by the offset (short segments on the inner side, arc approximations whose radius is smaller than the distance) are removed and their neighbours joined again; if the whole polyline collapses the offset is rejected with a "distance is too large" message;
- repeated consecutive vertices, including a closing vertex repeated at the end of a closed polyline, are ignored;
- when the side point is equally close to two segments (for example on the extension of an edge beyond a vertex), the segment that clearly sees the point on one side decides the offset side;
- the target is re-read from the document when the side is chosen, so an undo or delete in the meantime does not create an offset of a stale entity;
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
- Move, Copy, Rotate and Scale show a live guide vector between the base/reference point and the current pointer during point-picking phases;
- option-only confirmation phases that do not need a canvas point expose no point snap mode;
- `Cancel`/`Deactivate` clears transient base points, previews and pending state so tools do not leave dirty intermediate state behind.

Additional snap-mode coverage was added for Break Point, Break Segment, Extend and Align. These tools now explicitly implement `ISnapModeProvider`, closing the remaining gap where a selection phase could otherwise leave geometric snap modes active in the UI.
