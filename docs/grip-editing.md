# Grip Editing

Grip editing allows the user to modify an existing entity by dragging its characteristic control points directly on the canvas.

This document describes the design, architecture, data model, interaction flow, rendering and testing expectations for grip editing in OpenCad2D.

---

## Overview

After selecting a single entity, the user can press `TAB` to enter grip edit mode.

In grip edit mode, the entity is shown with a set of small square markers called grips. Each grip corresponds to a specific geometric point of the entity, such as a line endpoint or a circle center.

The user clicks a grip to make it active. Then the user moves the cursor and clicks a destination point. The entity is updated and the command is recorded in history so it can be undone.

For generic polylines, segment midpoint grips can also be used to insert new vertices. A vertex can be removed by hovering or activating its vertex grip and pressing `Delete`, as long as the operation would not make the polyline invalid.

The tool remains in grip edit mode after each edit. The user can continue editing other grips on the same entity. `ESC` exits grip edit mode and returns to `SelectionTool` with the entity still selected.

---

## User interaction flow

```text
1. User selects exactly one entity using SelectionTool
2. User presses TAB
3. CadWorkspace activates GripEditTool, passing the selected entity id
4. GripEditTool queries the appropriate IGripProvider for the entity's grips
5. CadCanvas renders grip markers (cold state)
6. User moves cursor over a grip -> grip highlights (hot state)
7. User clicks a grip -> grip becomes active (warm state), tool enters grip-active sub-state
8. User moves cursor -> CadCanvas shows preview of the modified entity
9. User clicks destination point -> tool creates ReplaceEntityCommand and executes it
10. For generic polylines, user can press Delete on a hot/warm vertex grip to delete that vertex
11. Document updates, entity is reloaded, grips refresh
11. Tool returns to idle grip state (step 5)
12. User presses ESC -> GripEditTool deactivates, SelectionTool resumes, entity remains selected
```

---

## TAB activation rule

`TAB` enters grip edit mode only when exactly one entity is selected.

```text
selection count == 1 -> activate GripEditTool
selection count == 0 -> ignore TAB
selection count >  1 -> ignore TAB (or show status message)
```

This rule keeps grip editing focused and avoids ambiguity when multiple entities share grips at the same point.

TAB handling should be coordinated by `CadWorkspace` or `SelectionTool`, not by the Avalonia UI.

---

## Grip states

Each grip can be in one of three visual states:

```text
Cold  -> visible but not interacted with
Hot   -> cursor is hovering within hover tolerance
Warm  -> clicked and active, waiting for destination point
```

Only one grip can be warm at a time.

---

## Grip data model

### GripKind

`GripKind` identifies the type of grip and how it affects the entity.

```csharp
public enum GripKind
{
    MoveVertex,   // moves a single geometric point (e.g. line endpoint)
    MoveEntity,   // moves the entire entity rigidly (e.g. line midpoint, circle center)
    ResizeRadius, // moves a quadrant point, changing radius (e.g. circle quadrant)
    InsertVertex  // inserts a new vertex on a polyline segment
}
```

### GripPoint

`GripPoint` is a value type describing one grip on one entity.

```csharp
public readonly struct GripPoint
{
    public Point2D Position   { get; }
    public GripKind Kind      { get; }
    public EntityId EntityId  { get; }
    public int GripIndex      { get; }
}
```

`GripIndex` uniquely identifies a grip within a single entity. It is used when a provider applies a grip move to the entity.

`GripPoint` belongs in `OpenCad2D.Interaction` or `OpenCad2D.Tools`. It must not reference Avalonia.

---

## IGripProvider

`IGripProvider` is an interface that grip-aware entities expose through a provider registry.

```csharp
public interface IGripProvider
{
    bool CanHandle(CadEntity entity);

    IReadOnlyList<GripPoint> GetGrips(CadEntity entity);

    CadEntity ApplyGripMove(CadEntity entity, int gripIndex, Point2D destination);
}
```

`GetGrips` returns all grips for the given entity in model coordinates.

`ApplyGripMove` takes the original entity, the grip index and the destination point and returns a new entity with the modification applied.

The returned entity must preserve the original `EntityId`.

`IGripProvider` belongs in `OpenCad2D.Interaction` or `OpenCad2D.Tools`.

---

## GripProviderRegistry

`GripProviderRegistry` maps entity types to grip providers.

```csharp
public class GripProviderRegistry
{
    public void Register(IGripProvider provider);
    public IGripProvider? FindProvider(CadEntity entity);
}
```

`GripEditTool` uses the registry to look up the correct provider at activation time.

---

## Grip providers per entity

### LineGripProvider

A `LineEntity` exposes three grips.

```text
GripIndex 0 -> start point, GripKind.MoveVertex
GripIndex 1 -> midpoint (midpoint of segment), GripKind.MoveEntity
GripIndex 2 -> end point, GripKind.MoveVertex
```

`ApplyGripMove` behavior:

```text
GripIndex 0 -> new LineEntity(destination, originalEnd, sameId, sameLayer)
GripIndex 1 -> translation vector = destination - midpoint
               new LineEntity(start + vector, end + vector, sameId, sameLayer)
GripIndex 2 -> new LineEntity(originalStart, destination, sameId, sameLayer)
```

### CircleGripProvider

A `CircleEntity` exposes five grips.

```text
GripIndex 0 -> center, GripKind.MoveEntity
GripIndex 1 -> quadrant at   0 degrees, GripKind.ResizeRadius
GripIndex 2 -> quadrant at  90 degrees, GripKind.ResizeRadius
GripIndex 3 -> quadrant at 180 degrees, GripKind.ResizeRadius
GripIndex 4 -> quadrant at 270 degrees, GripKind.ResizeRadius
```

`ApplyGripMove` behavior:

```text
GripIndex 0 -> new CircleEntity(destination, originalRadius, sameId, sameLayer)
GripIndex 1..4 -> newRadius = Distance(originalCenter, destination)
                  new CircleEntity(originalCenter, newRadius, sameId, sameLayer)
```

The quadrant grip positions are computed from the center and radius:

```text
Quadrant 0   -> center + (radius, 0)
Quadrant 90  -> center + (0, radius)
Quadrant 180 -> center + (-radius, 0)
Quadrant 270 -> center + (0, -radius)
```

---

### PolylineGripProvider

A generic `PolylineEntity` exposes three categories of grips.

```text
Vertex grips       -> GripKind.MoveVertex
Segment insert grips -> GripKind.InsertVertex
Centroid grip      -> GripKind.MoveEntity
```

For an open polyline with `n` vertices:

```text
vertex grips: n
insert grips: n - 1
center grip: 1
```

For a closed generic polyline with `n` vertices:

```text
vertex grips: n
insert grips: n, including the closing segment from last vertex to first vertex
center grip: 1
```

Insertion behavior:

```text
Click segment insert grip
Move to destination
Click destination
-> new vertex is inserted between the two segment vertices
```

Deletion behavior:

```text
Hover or activate a vertex grip
Press Delete
-> vertex is removed through an undoable ReplaceEntitiesCommand
```

Safety rules:

```text
open polyline cannot go below 2 vertices
closed polyline cannot go below 3 vertices
rectangle-like closed four-vertex polylines keep rectangle-specific resize behavior
```

Rectangle-like closed polylines intentionally keep their existing corner/edge/center grips, so their right-angle rectangle behavior is preserved.

For mixed AutoCAD-style polylines, insert grips are placed on the actual curved segment approximation instead of the straight chord midpoint. Moving an existing vertex or moving the whole entity preserves `SegmentBulges`. Inserting a new vertex into an arc segment currently flattens that split segment into two straight segments, because the destination can be any point and native arc-segment split editing is planned as a later segment-aware refinement. Deleting a vertex keeps unaffected bulges and sets the newly merged segment to a straight segment.

---

## GripEditTool

`GripEditTool` is a UI-independent CAD tool that lives in `OpenCad2D.Tools`.

It does not derive from `TwoPointToolBase` because its interaction pattern is different: it works on an existing entity and has an internal idle/active state machine.

### Constructor

`GripEditTool` receives the `EntityId` of the entity to be edited. It looks up the entity from the document through `ToolContext` and resolves its grips at activation time.

### Internal state

```text
Idle      -> showing cold grips, waiting for a grip click
GripActive -> one grip is warm, waiting for destination point
```

### PointerMoved

In `Idle` state: update which grip is hot based on cursor proximity.

In `GripActive` state: apply snapping and the effective point constraint to the cursor, compute a preview of the modified entity and expose it for rendering.

### PointerPressed

In `Idle` state: check if cursor is within click tolerance of any grip. If yes, set that grip as warm and switch to `GripActive` state. Set `ToolContext.CurrentBasePoint` to the grip's position.

In `GripActive` state: resolve the destination point (after snapping and the effective point constraint), call `provider.ApplyGripMove(entity, warmGripIndex, destination)`, create and execute `ReplaceEntityCommand`, reload the entity from the document, refresh grips and return to `Idle` state.

### Cancel / ESC

Clears the active grip if one is warm. If already idle, signals to `CadWorkspace` that `GripEditTool` should deactivate. `CadWorkspace` restores `SelectionTool`, preserving the current selection.

### Snapping in GripEditTool

Snapping applies during `GripActive` state in the same way as any two-point tool.

The warm grip's position becomes `ToolContext.CurrentBasePoint`. Contextual snaps such as perpendicular and tangent work from this base point.

### Ortho and Polar Tracking in GripEditTool

Ortho/Polar-style point constraints apply in `GripActive` state for `GripKind.MoveVertex` and `GripKind.MoveEntity` when integrated through the shared input constraint service.

For `GripKind.ResizeRadius`, the destination point is used as-is and only the distance to the center matters. Angular constraint on a resize grip would prevent reaching off-axis radii, so it should not be applied to `ResizeRadius` grips.

---

## Command used

Grip editing produces `ReplaceEntityCommand` (or the existing `ReplaceEntitiesCommand` with a single entity).

The entity id must be preserved. The command stores the original entity and the replacement entity. Undo restores the original.

```text
Execute -> document.ReplaceEntity(newEntity)
Undo    -> document.ReplaceEntity(originalEntity)
```

No new command type is needed if `ReplaceEntitiesCommand` already handles single-entity replacement.

---

## Grip hover tolerance

The hover tolerance determines when a grip becomes hot.

```text
hot when: Distance(cursor, grip.Position) <= GripHoverTolerance
```

`GripHoverTolerance` should be defined in screen pixels and converted to model units through the viewport transform, consistent with how future pick tolerance improvements are planned.

For a first implementation a model-unit constant is acceptable.

---

## Preview during grip active state

While a grip is warm and the user moves the cursor, `GripEditTool` should expose a preview entity.

The preview is produced by calling `provider.ApplyGripMove(entity, warmGripIndex, currentSnappedPoint)` without committing it.

`CadCanvas` should render the preview entity with a distinct style, for example using a dashed or lighter stroke.

The original entity should still be rendered in its normal style behind the preview, so the user can see what is changing.

---

## Visual representation of grips

Grip rendering belongs to `CadCanvas` in `OpenCad2D.App`.

`GripEditTool` must expose the current grips and their states through a data structure that the UI can query, without referencing Avalonia.

A suitable approach:

```csharp
public IReadOnlyList<GripPoint> CurrentGrips { get; }
public int? HotGripIndex { get; }
public int? WarmGripIndex { get; }
```

`CadCanvas` reads these properties and renders the grip markers.

### Suggested visual style

Grip markers are small squares centered on the grip position.

```text
Cold grip  -> hollow square, blue or cyan stroke
Hot grip   -> filled square, green fill
Warm grip  -> filled square, red fill
```

All grips use the same square shape regardless of kind. Visual distinction between vertex, move and resize grips can be added in a future iteration.

The grip marker size should be defined in screen pixels and remain constant regardless of zoom level. The canvas converts the model position to screen coordinates and draws the marker centered on that point.

---

## Status bar and active command indicator

While `GripEditTool` is active, the active command indicator should show a label such as:

```text
"Grip Edit"
```

While a grip is warm (GripActive state), the status bar should show:

```text
L, DX, DY values from the warm grip position to the current preview destination
```

This uses the same measurement feedback infrastructure as `TwoPointToolBase`.

---

## Integration with CadWorkspace

`CadWorkspace` must support activating and deactivating `GripEditTool`.

Suggested method:

```csharp
public void EnterGripEditMode(EntityId entityId);
public void ExitGripEditMode();
```

`EnterGripEditMode` is called when `TAB` is pressed with exactly one entity selected.

`ExitGripEditMode` is called when `GripEditTool` signals cancellation in idle state.

After `ExitGripEditMode`, `SelectionTool` resumes and the current selection is preserved.

---

## TAB key handling

The TAB key should be intercepted either in `CadWorkspace` or in `SelectionTool`.

Suggested rule:

```text
if active tool is SelectionTool
and selection count == 1
and the selected entity has a registered grip provider
then: call workspace.EnterGripEditMode(selectedEntityId)
```

If selection count is not exactly 1, TAB does nothing. No error message is required for this prototype, but a status bar message may be useful in the future.

---

## Locked layer protection

Grip editing must not bypass the locked layer protection at `CadDocument`.

If the entity being edited is on a locked layer, `CadDocument.ReplaceEntity` will reject the replacement. This should not happen in normal usage because locked layer entities are not selectable, and `GripEditTool` is activated only from a selection.

As an additional safeguard, `GripEditTool` should check at activation time whether the entity is on a locked layer. If it is, the tool should deactivate immediately without showing grips.

---

## Files to create

```text
OpenCad2D.Interaction/ or OpenCad2D.Tools/
  Grips/
    GripKind.cs
    GripPoint.cs
    IGripProvider.cs
    GripProviderRegistry.cs
    LineGripProvider.cs
    CircleGripProvider.cs

OpenCad2D.Tools/
  GripEditTool.cs
```

---

## Files to modify

```text
OpenCad2D.Tools/
  ToolId.cs              -> add GripEdit tool id
  ToolRegistry.cs        -> register GripEditTool
  CadWorkspace.cs        -> add EnterGripEditMode / ExitGripEditMode
  SelectionTool.cs       -> handle TAB, call workspace.EnterGripEditMode

OpenCad2D.App/
  CadCanvas.cs           -> render grip markers, render grip preview entity
  MainViewModel.cs       -> forward TAB key to workspace (if not already handled)
```

---

## Tests to write

### GripPoint and GripKind

No behavior to test, but confirm struct equality and construction.

### LineGripProvider

```text
GetGrips returns 3 grips at the correct positions
GripIndex 0 -> start point
GripIndex 1 -> midpoint
GripIndex 2 -> end point
ApplyGripMove(0, destination) -> start moves, end unchanged, id preserved
ApplyGripMove(1, destination) -> whole line translates correctly
ApplyGripMove(2, destination) -> end moves, start unchanged, id preserved
```

### CircleGripProvider

```text
GetGrips returns 5 grips at the correct positions
GripIndex 0 -> center
GripIndex 1..4 -> four quadrant points at correct angles
ApplyGripMove(0, destination) -> center moves, radius unchanged, id preserved
ApplyGripMove(1, destination) -> radius = distance(center, destination), center unchanged
ApplyGripMove(2..4, destination) -> same radius rule applies
```

### GripProviderRegistry

```text
Register and FindProvider returns correct provider for LineEntity
Register and FindProvider returns correct provider for CircleEntity
FindProvider returns null for unknown entity type
```

### GripEditTool

```text
Activation with a LineEntity -> grips are loaded
Activation with a CircleEntity -> grips are loaded
Activation with locked layer entity -> tool deactivates immediately
PointerMoved in idle state -> hot grip index updates based on proximity
PointerPressed on a grip in idle state -> switches to GripActive, CurrentBasePoint set
PointerPressed in GripActive state -> ReplaceEntityCommand executed, entity updated
Entity id is preserved after grip edit
After grip commit, tool returns to idle and grips refresh
ESC in GripActive state -> returns to idle, grip deactivated
ESC in idle state -> tool signals exit, SelectionTool should resume
Ortho/Polar constraints apply for MoveVertex and MoveEntity grips
Angular constraints do not apply for ResizeRadius grips
```

### Undo behavior

```text
Grip edit -> undo -> entity returns to original geometry
Undo stack contains exactly one entry per grip commit
```

---

## Design rules to preserve

```text
GripEditTool must not depend on Avalonia.
Grip positions are in model coordinates.
CadCanvas converts model positions to screen coordinates for rendering.
ReplaceEntityCommand is used for all grip edits.
Entity id is preserved.
CadDocument rejects replacement of locked layer entities.
Snapping applies during grip-active state.
Angular constraints apply to vertex and move grips, not to resize grips.
GripProviderRegistry decouples tool logic from entity types.
Preview does not modify the document.
```

### Arc 3-point grip rule

Arc grips behave like a 3-point arc editor. The three construction points are the start point, a point on the arc and the end point.

```text
Start grip -> rebuild the arc through new start + current point-on-arc + current end
Point-on-arc grip -> rebuild the arc through current start + new point-on-arc + current end
End grip -> rebuild the arc through current start + current point-on-arc + new end
Center grip -> move the whole arc rigidly
```

This means moving one of the three arc construction grips keeps the other two construction points fixed and recalculates center, radius and sweep from the resulting three points. The center grip remains a pure translation grip.

## Mixed polyline segment bulges

The Property Panel now exposes editable DXF-compatible bulge values for selected polylines. Enter `0` to keep a segment straight, a positive value for one arc direction and a negative value for the opposite arc direction. This is intentionally a low-level but precise editing surface, useful while the dedicated visual segment editor is still planned.

## Mixed polyline arc-shape editing

For `PolylineEntity` objects with non-zero segment bulges, the grip provider now adds an arc-shape grip in addition to vertex, insert and move grips.

- Vertex grips still move vertices and preserve existing bulges.
- Insert grips still insert a new vertex; if the original segment was curved, the split segment is intentionally flattened.
- Arc-shape grips use `GripKind.ResizeRadius` and update only the selected segment bulge.
- Moving an arc-shape grip onto the chord flattens the segment.

This gives a first graphical way to change the curvature of mixed polylines without opening the Property Panel.
