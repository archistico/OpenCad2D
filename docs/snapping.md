# Snapping

Snapping helps the user place points precisely.

Instead of relying only on the raw cursor position, a tool can ask the snapping system for a better point near the cursor. This can be an endpoint, midpoint, center, intersection, perpendicular point, tangent point, grid point or, when a tool is selecting objects, an entity pick candidate.

Snapping is implemented in `OpenCad2D.Interaction`, not in the UI. This means the same logic can be tested without Avalonia and can be reused by any future user interface.

---

## Main idea

The UI sends model-space coordinates to the active tool.

The tool, through `ToolContext`, asks `SnapService` for a snap candidate.

If a candidate is found within the configured tolerance, the tool uses the snapped point instead of the raw cursor point. Tools that support Polar Tracking then apply the angular constraint after snapping.

The UI can also ask the snapping system for the current snap candidate while the pointer moves, so it can draw a visual marker.

Snapping works in model coordinates. The canvas is responsible for converting screen coordinates to model coordinates before snapping is evaluated.

---

## Main types

The snapping system is based on these main types:

```text
SnapKind
SnapRequest
SnapCandidate
ISnapProvider
SnapService
GridSettings
EntitySnapProvider
```

`SnapKind` identifies the available snap modes.

`SnapRequest` describes a snap query.

`SnapCandidate` represents one possible snap point.

`ISnapProvider` is implemented by each snap provider.

`SnapService` coordinates all providers and chooses the best candidate.

`GridSettings` describes the grid used by grid rendering and grid snapping.

`EntitySnapProvider` is the selection-oriented snap provider. It does not represent a geometric construction point; it represents the selectable entity under the cursor.

---

## SnapKind

`SnapKind` is a flags enum.

Several snap modes can be enabled at the same time.

Current snap modes are:

```text
Endpoint
Midpoint
Center
Quadrant
Intersection
Nearest
Perpendicular
Tangent
Grid
Entity
```

`Entity` is intentionally separate from the geometric snap set. `SnapKind.All` means all geometric snaps and does not include `Entity`. `SnapKind.EntityOnly` is used by tools that are currently picking objects instead of points.

The UI exposes geometric modes through snap controls. Entity snap is not a normal user toggle; it is activated by tools such as `SelectionTool` or by phases such as the first phase of `MoveTool` when no entity was selected before activating Move.

---

## SnapRequest

`SnapRequest` contains the information needed to evaluate snapping:

```text
Document
CursorPoint
BasePoint
Tolerance
EnabledSnaps
GridSettings
SearchArea
```

`CursorPoint` is the current cursor point in model coordinates.

`Tolerance` is the snap tolerance in model units.

`BasePoint` is optional and is used by contextual snaps.

`SearchArea` is a bounding box around the cursor point based on the tolerance. Entity-based snap providers use this search area to query candidate entities instead of scanning the whole document.

---

## Spatial search

Snap providers should not iterate all document entities when a spatial query is available.

The preferred pattern is:

```csharp
foreach (CadEntity entity in request.Document.GetVisibleEntities(request.SearchArea))
{
    // evaluate candidates
}
```

This delegates candidate lookup to the document/entity spatial index.

The current spatial index implementation is linear, but the API is ready for future Quadtree, R-Tree or grid-based implementations.

Visibility remains a document rule. Snap providers should work only with visible entities.

---

## SnapCandidate

A `SnapCandidate` contains the result produced by a snap provider.

It stores:

```text
snap kind
snapped point
optional entity id
distance from cursor
```

The distance is used by `SnapService` to choose the best candidate among results with the same priority.

The UI uses the candidate to draw a snap marker and display the current snap kind in the status bar. For `SnapKind.Entity`, the candidate also carries the hit entity id so selection-oriented tools can know which entity is being targeted.

---

## SnapService

`SnapService` owns the list of snap providers.

When a request arrives, it asks only the providers whose `SnapKind` is enabled.

Each provider can return zero or more candidates.

Then `SnapService` orders candidates by priority and distance.

The current priority favors precise geometric snaps over generic snaps.

For example, endpoint and intersection have higher priority than nearest and grid.

This prevents the grid from stealing the cursor when a more meaningful geometric snap is available nearby. Entity snap is normally enabled alone during entity-picking phases, so it does not compete with ordinary geometric snaps.

---


## Snapping and Polar Tracking

OpenCad2D intentionally applies Polar Tracking after snapping:

```text
raw cursor point -> snap candidate -> Polar Tracking / Ortho constraint
```

This matches the current design decision for point-placement tools. A snap can provide a precise candidate first, then the active angular constraint projects the final input point onto the nearest configured direction from the current base point.

This order is implemented in the tool layer rather than in `SnapService`, because snapping itself should remain a pure candidate-selection service.

---

## Direct snaps

Some snaps are direct.

They only need the cursor point, document and tolerance.

Direct snaps include:

```text
Endpoint
Midpoint
Center
Quadrant
Intersection
Nearest
Grid
Entity   selection-oriented only
```

They can work even before a tool has a first point.

For example, when the user starts drawing a line, the first point can snap to an endpoint or to the grid.

---

## Contextual snaps

Some snaps are contextual.

They need a base point.

Current contextual snaps are:

```text
Perpendicular
Tangent
```

These snaps become useful after a tool already has a first point.

For example, if the user is drawing a line, the first click defines the start point. While choosing the second point, the snapping system can calculate the perpendicular foot from the start point to another entity, or the tangent point from the start point to a circle.

Without a base point, contextual snap providers return no candidates.

---

## Endpoint snapping

Endpoint snapping finds endpoints of entities.

It currently supports line endpoints, polyline vertices, arc endpoints and raster image reference corners.

This is one of the most important snaps because it allows the user to connect geometry precisely.

---

## Midpoint snapping

Midpoint snapping finds the middle point of a line segment.

For polylines, each segment can provide a midpoint. Straight segments use the segment midpoint. Mixed polylines with DXF bulges use the length midpoint of the approximated curved segment, so snapping follows the visible arc rather than the chord.

For raster image references, each image border can provide a midpoint.

For arcs, the provider calculates the angular midpoint of the arc.

---

## Center snapping

Center snapping finds the center of circles, arcs and raster image references.

This is useful when drawing from or toward circular geometry.

---

## Quadrant snapping

Quadrant snapping finds the four cardinal points of a circle.

The quadrant points are located at 0, 90, 180 and 270 degrees.

For arcs, only quadrant points that actually lie on the arc are returned.

---

## Intersection snapping

Intersection snapping finds intersections between visible entities.

It supports exact intersections for common analytic combinations such as line-line, line-polyline, polyline-polyline, line-circle, circle-circle, circle-polyline, line-arc and circle-arc when the entities are represented analytically or by straight segments.

Closed rectangles are stored as `PolylineEntity` objects. Circle/rectangle intersection snapping therefore uses the circle-polyline path, checking each rectangle side as a segment and returning distinct intersection points. This covers both axis-aligned rectangles and rotated rectangles created by Rectangle by Sides.

Mixed polylines with DXF bulges are converted through `PolylineEntity.GetInteractionGeometry()` before intersection snapping. This makes line/polyline, polyline/polyline and circle/polyline snaps follow the visible curved segment instead of the original chord.

It also supports first-pass curve intersections for `EllipseEntity` and `BezierSplineEntity` by converting curves to a high-resolution polyline approximation for snapping. This covers practical line/ellipse, polyline/ellipse, circle/ellipse, ellipse/ellipse, line/spline, polyline/spline, circle/spline, ellipse/spline and spline/spline intersections.

For entity pairs not covered by the snap provider's exact fast paths, intersection snapping delegates to the core `CadEntityIntersectionService`. This keeps snapping aligned with edit-command intersection support and covers native curve combinations such as arc/arc, arc/polyline, line/elliptical-arc and polyline/elliptical-arc.

Intersection snapping should use the document search area to collect candidate entities near the cursor, then evaluate actual geometric intersections.

Future work can replace the sampled curve path with exact analytic/NURBS-specific solvers where needed.

---

## Nearest snapping

Nearest snapping finds the closest point on an entity to the cursor. For raster image references, the closest point is evaluated on the image rectangle border, not inside the filled image area.

It is more generic than endpoint, midpoint or intersection snapping.

For this reason it has lower priority than more explicit snap modes. Endpoint, midpoint, center, quadrant, intersection, perpendicular and tangent candidates win before Nearest; Grid remains below Nearest.

Nearest snapping is intentionally disabled by default because it can be noisy when drafting near existing geometry. Users can enable it from the Snap bar only for workflows where closest-point picking is useful.

Implementation note: Nearest is already part of the document/application snap settings, so enabling it can be persisted, but new documents and missing legacy snap settings keep it off unless explicitly enabled.

---

## Entity snapping

Entity snapping is used when the active tool needs to pick an object rather than a geometric point.

Current uses:

```text
SelectionTool                         -> entity snap only
MoveTool, no pre-existing selection    -> entity snap only until entities are selected
MoveTool, base/destination phase       -> geometric snaps from ToolContext.EnabledSnaps
```

`EntitySnapProvider` uses hit testing and returns selectable entities under the cursor. The returned `SnapCandidate` contains:

```text
SnapKind.Entity
closest point on the hit entity
entity id
distance from cursor
```

The visual marker for entity snap is a simple rectangle. This keeps it visually distinct from endpoint, midpoint, center, grid and other geometric point markers.

Entity snapping follows selection rules, not geometric reference rules:

```text
hidden layer entity -> not entity-snappable
locked layer entity -> not entity-snappable
visible unlocked entity -> entity-snappable
```

This differs from ordinary geometric snapping, where visible locked-layer entities may still be used as references.

### Overlapping entity cycling

When several selectable entities overlap at the cursor, `SelectionService.SelectNextByPoint(...)` can cycle through the hit-test results.

Current interaction:

```text
click         -> select first hit entity
Shift+click   -> toggle first hit entity
Ctrl+click    -> cycle to next hit entity under the cursor
Ctrl+Shift+click -> cycle and toggle
```

`Ctrl` is used for cycling because `Shift` is already assigned to selection toggling and `Alt` may interfere with menu/focus behavior on Windows.

---

## Perpendicular snapping

Perpendicular snapping is contextual.

It needs a base point.

Given a base point and a target entity, it tries to find the point on the entity that creates a perpendicular connection.

For a line or segment, this is the projection of the base point onto the segment.

For a circle or arc, it is the radial point aligned with the base point.

---

## Tangent snapping

Tangent snapping is contextual.

It needs a base point.

Given a base point outside a circle, the provider calculates possible tangent points from that point to the circle.

For arcs, tangent points are filtered so that only points lying on the arc are accepted.

If the base point is inside the circle or exactly on the circle, no tangent candidate is returned.

---

## Grid snapping

Grid snapping snaps the cursor to the nearest grid point.

The grid is described by `GridSettings`, which contains layout type, minor step, major step, origin, visibility, screen spacing thresholds and isometric angle.

Supported layouts:

```text
Rectangular  horizontal + vertical grid
Isometric    vertical grid + diagonal families at +angle and -angle
```

For a rectangular grid with a step of 10, the point `(23.2, 46.8)` snaps to `(20, 50)` if it is within tolerance.

For an isometric grid, snap candidates come from the isometric grid vertices. With the default 30-degree layout, the vertical spacing is derived from the diagonal spacing using:

```text
verticalStep = diagonalSpacing / (2 * tan(angle))
```

This makes the vertical grid lines pass through the vertices created by the intersections of the two diagonal families.

Grid snapping works independently from visible entities.

The visual grid in the Avalonia canvas and the snapping grid should remain conceptually aligned, although they are separate concerns. Grid visibility does not enable or disable `SnapKind.Grid`.

---

## Snap tolerance

Snap tolerance controls how far the cursor can be from a candidate.

The configured `SnapTolerance` value is now interpreted as a screen-space pixel radius by the Avalonia canvas. Before creating a `SnapRequest`, `CadCanvas` converts that pixel radius to model units through the current `ViewportTransform`.

This keeps snapping visually consistent at every zoom level:

- zoomed out: the same pixel radius covers a larger model-space distance;
- zoomed in: the same pixel radius covers a smaller model-space distance;
- when two snap candidates are visually close, zooming in makes them easier to choose independently.

The lower-level snapping providers still receive and compare a model-space tolerance. The screen-to-model conversion is intentionally kept at the UI boundary so the interaction layer remains independent from Avalonia viewport state.

---

## Geometry tolerance vs snap tolerance

Snap tolerance and geometry tolerance are different concepts.

```text
Snap tolerance       user interaction tolerance, usually related to cursor distance
GeometryTolerance    mathematical tolerance used by algorithms
```

A candidate can be within snap tolerance while the geometric algorithm still uses `GeometryTolerance` internally to avoid floating-point edge cases.

Do not use snap tolerance as a replacement for geometric precision rules.

---

## Snap priority

When multiple candidates are available, `SnapService` chooses one.

The decision is based first on snap priority and then on cursor distance.

This means a high-priority snap can win even if another lower-priority snap is slightly closer.

The current priority favors precise snaps first, then generic snaps.

Grid has low priority, so it should not override endpoint or intersection snapping when both are available.

---

## Hidden layer behavior

Snapping must ignore entities on hidden layers.

This rule is enforced by querying visible entities through the document:

```text
request.Document.GetVisibleEntities(request.SearchArea)
```

The snap provider should not independently decide layer visibility. It should use the document-level visibility API.

Locked layers are different: entities on locked layers should still be usable as snap references unless a future UX decision says otherwise.

---

## Snapping in tools

`ISnapModeProvider` lets a tool override the snap modes that are active for its current phase. This is how the application separates geometric snapping from entity picking.

Most two-point tools use snapping through `TwoPointToolBase`.

When the tool is waiting for the first point, snapping is evaluated without a base point.

When the tool is waiting for the second point, the first point is passed as `BasePoint`.

This makes contextual snaps work naturally.

For example, `LineTool` can use tangent snapping only after the first click, because before that there is no base point.

`SelectionTool` implements `ISnapModeProvider` and always returns `SnapKind.EntityOnly`. `MoveTool` implements the same interface and returns `SnapKind.EntityOnly` only while it is waiting for entities to move; after that it returns the normal enabled geometric snap set.

Modify tools that are waiting for entity selection must always return `SnapKind.EntityOnly`, regardless of the user's enabled geometric snap modes. This applies to Break Point, Break Segment, Trim, Extend, Offset target selection, Fillet, Chamfer, Move, Copy, Rotate, Scale, Mirror, Align, Explode, Join and Delete. When those tools later move to a point-input phase they may return the normal geometric snap set or a narrowed snap set appropriate to the command phase.

The UI also gives non-selection active tools priority over temporary canvas snap overrides. Temporary overrides are reserved for modal pending-placement workflows such as block/library/import insertion, where the active tool is still the selection tool but the UI is asking for a placement point. This prevents stale placement overrides from leaking into modify tools and showing endpoint/midpoint markers while the command is actually asking the user to select an entity.

---

## Snapping in the UI

The Avalonia UI uses snapping in two ways.

First, tools use snapping to decide the actual point used for drawing or editing.

Second, `CadCanvas` asks the snapping system for the current candidate while the mouse moves, so it can draw a visual snap marker.

The status bar displays the current snap kind.

The canvas draws different marker shapes for different snap kinds.

Examples:

```text
Endpoint       L-shaped marker
Midpoint       X marker
Center         circle marker
Quadrant       diamond marker
Intersection   plus marker
Nearest        small circle with internal X marker
Perpendicular  T marker
Tangent        circle plus tangent line
Grid           grid-like marker
Entity         simple rectangle marker
```

This gives immediate visual feedback and makes the cursor behavior more CAD-like.

---

## Temporary SmartPoint capture foundation

The advanced tracking/extension feature is exposed in the Snap bar as **SmartPoint Tracking**. It is enabled by default, but it can be disabled independently from normal object snap modes when the temporary tracking aids are not needed.

A SmartPoint is a runtime-only reference point captured from a strong object snap while a point-based command is active.

Current behavior:

```text
Hover delay: about 700 ms on the same snap candidate
Maximum captured SmartPoints: 5
Captured snap kinds: Endpoint, Midpoint, Center, Quadrant, Intersection
Excluded snap kinds: Entity, Grid, Nearest, Perpendicular, Tangent
Lifetime: current command / transient canvas state only
Persistence/export: never saved, never exported
```

SmartPoint capture is intentionally conservative:

- it is disabled while the Selection tool is active;
- it does not capture `EntityOnly` selection prompts;
- it does not capture `Nearest`, because Nearest is too noisy as a tracking reference;
- it requires an intentional hover on the same snap candidate for about 700 ms;
- it rejects the pending capture if the mouse moves more than a few screen pixels during the hover delay;
- it clears pending hover state when the pointer leaves the canvas, when panning starts, or when snap state is cleared;
- completed/cancelled tool results clear captured SmartPoints.

The current implementation captures and displays SmartPoints, then generates temporary horizontal, vertical and active Polar Tracking direction lines from each captured point while a non-selection command is active. When the cursor is within snap tolerance of a tracking line, OpenCad2D shows a temporary tracking snap marker and feeds the projected tracking point to the active tool. Direct distance input can now use the active tracking line: typing a plain distance while the tracking candidate is active resolves the point from the SmartPoint origin along the signed cursor-side tracking direction.

## Basic SmartPoint tracking lines

Each captured SmartPoint currently emits runtime-only construction lines:

```text
horizontal through the SmartPoint
vertical through the SmartPoint
active Polar Tracking directions through the SmartPoint, when Polar Tracking is enabled
```

For example, with Polar Tracking set to 45°, SmartPoint Tracking also emits diagonal 45° and 135° construction lines. With Polar Tracking set to 30°, it emits 30°, 60°, 120° and 150° directions. 0° and 90° are not duplicated because they are already represented by the horizontal and vertical tracking lines.

These lines are drawn as cyan dashed overlays. They are not document entities, are not selectable, are not exported, and are cleared together with the SmartPoints at the end of the command.

Tracking snap priority is conservative:

- explicit object snaps such as Endpoint, Midpoint, Center, Quadrant and Intersection still win;
- Tracking can win over Grid and Nearest, because both are lower-priority drafting aids;
- Tracking is not included in persisted snap settings and is generated only from active SmartPoints.


## Manual distance input on active tracking lines

When the active snap candidate is `SnapKind.Tracking`, plain distance input is resolved from the tracking line origin instead of from the command base point.

Example:

```text
SmartPoint at 10,20
cursor on the horizontal tracking line to the right
typed input: 5
resolved point: 15,20
```

The tracking candidate stores both:

```text
TrackingOrigin
TrackingDirection
```

`TrackingDirection` is signed according to the cursor side, so moving left/down from the SmartPoint and typing a distance creates a point in that negative direction. Explicit coordinate input still has priority over tracking distance input.

---

## Guidelines for new snap providers

A new snap provider should:

```text
implement ISnapProvider
return only candidates valid for its snap kind
respect enabled snap flags through SnapService
query visible candidate entities through the document
use request.SearchArea when applicable
not modify the document
work in model coordinates
return candidates only within requested tolerance
use GeometryTolerance for geometric edge cases
return no candidates when a required BasePoint is missing
have focused tests
```

---

## Future improvements

The snapping system can be improved in several ways:

```text
screen-based snap tolerance
apparent intersection
parallel snap
additional SmartPoint Tracking behaviors only after manual validation
entity-extension tracking for arcs and tangent directions
better tangent handling for arcs and complex entities
spatial index implementation beyond LinearSpatialIndex
```

The current abstraction is ready for these improvements without moving snap logic into the UI.



### SmartPoint Tracking and Polar Tracking directions

When Polar Tracking is enabled from the top bar, SmartPoint Tracking reuses the same angular step to emit additional temporary construction lines from each captured SmartPoint. This keeps the behavior consistent with the existing drafting constraint instead of introducing a separate angle setting.

Rules:

- Polar Tracking Off: SmartPoint Tracking emits only horizontal, vertical and entity-extension lines.
- Polar 90°: no extra polar lines are added because horizontal/vertical already cover the step.
- Polar 45°: 45° and 135° tracking lines are added.
- Polar 30°: 30°, 60°, 120° and 150° tracking lines are added.
- Polar 15°: every 15° half-turn direction is added except 0° and 90°.

The generated polar lines use normal `SnapKind.Tracking` candidates. They support the same projected-point behavior and the same plain-distance input as horizontal/vertical tracking lines.

### SmartPoint tracking intersections

SmartPoint tracking now also exposes a temporary snap at the intersection of two tracking lines generated from different SmartPoints. The candidate is only produced when the cursor is within the active snap tolerance. Parallel tracking lines and the horizontal/vertical pair belonging to the same SmartPoint are ignored. Geometric snaps still have priority; tracking intersections only replace weak candidates such as Grid or Nearest. When the circular marker with the internal cross is shown, pointer input is resolved to that temporary intersection point, so it behaves as a real snap for the active command.



### Tracking intersections with real linear geometry

SmartPoint Tracking now also exposes temporary snap candidates where a tracking line crosses real linear geometry. The first implementation is intentionally limited to:

- line entities;
- straight polyline segments.

Bulged polyline segments, arcs, circles, splines and tangent intersections remain deferred. The candidate uses the same circular marker with an internal cross as other tracking intersections and is returned as `SnapKind.TrackingIntersection`. When the marker is visible, pointer input is resolved to the computed intersection point before the active tool receives the click.

The intersection is only offered when the cursor is within the active snap tolerance and the computed point lies on the finite real segment. Parallel/collinear cases are ignored for now to avoid ambiguous overlapping-line behavior. Strong geometric snaps still have priority; tracking/real-geometry intersections only replace weak candidates such as Grid and Nearest.

## Entity extension tracking

SmartPoint tracking now also supports linear entity extension lines. When a captured SmartPoint comes from a real line entity, OpenCad2D generates an additional temporary tracking line using that line direction. For polylines, only straight segments are considered; bulged/arc segments are intentionally ignored for the first stable implementation.

Supported extension sources:

- line entities;
- straight polyline segments from endpoint or segment-midpoint SmartPoints.

Extension candidates use `SnapKind.Extension`. They carry the same `TrackingOrigin` and signed `TrackingDirection` metadata as normal tracking-line candidates, so plain distance input can resolve points along the entity extension. The extension overlay is runtime-only: it is not persisted, selectable, undoable or exported.

Geometric object snaps still have priority over extension tracking. Extension can override weaker candidates such as Grid and Nearest when the cursor is within snap tolerance of the temporary extension line.


## Manual verification checklist

The current advanced snapping milestone is covered by the manual checklist in:

```text
docs/testing/advanced-snapping-tracking-manual-verification-2026-05-31.md
```

Use that checklist before extending SmartTrack further or before packaging the next v0.9 stabilization build.

### SmartPoint Tracking HUD label

Temporary SmartPoint Tracking candidates display a compact HUD label beside the snap marker. This makes it clearer that the visible marker is a real snap candidate and shows the construction information used by direct distance input.

Current labels:

```text
TRACK L=120 A=0°
EXT L=250 A=45°
TRACK INT
```

`Tracking` and `Extension` candidates use the candidate `TrackingOrigin` and signed `TrackingDirection` metadata to report distance and angle. `TrackingIntersection` is point-only, so it displays only the intersection kind unless future metadata is added.

### Direct SmartPoint snap

Captured SmartPoints are now also direct temporary snap candidates. When the cursor is within snap tolerance of a captured SmartPoint marker, OpenCad2D returns a `SnapKind.SmartPoint` candidate at the exact captured point. Pointer input is resolved to that point, so the marker can be clicked directly instead of only being used as an origin for tracking lines.

This keeps SmartPoint Tracking closer to Rhino SmartTrack behavior: captured points can be used both as construction origins and as real temporary snap points. Object snaps on real geometry still have priority when present; the SmartPoint candidate is mainly useful when the captured reference point remains available as a temporary construction point during the current command.

The label is runtime-only. It is not a drawing entity, is not persisted, is not exported and does not participate in undo/redo.

## Advanced snapping final consolidation - 2026-05-31

The current pre-v0.9 advanced snapping scope is now considered consolidated. The user-facing name for the runtime tracking subsystem is **SmartPoint Tracking**. It is exposed as an independent Snap bar toggle next to the ordinary snap modes. Turning it off clears all captured SmartPoints, temporary tracking overlays, temporary snap markers and related HUD labels without changing the normal snap settings.

Implemented advanced snap kinds and roles:

```text
Nearest                    opt-in closest-point snap on real geometry; disabled by default
SmartPoint                 direct snap to a captured temporary reference point
Tracking                   projected point on a SmartPoint tracking line
Extension                  projected point on a real linear entity extension
TrackingIntersection       temporary tracking/tracking or tracking/real-geometry intersection
TrackingGridIntersection   temporary marker for a grid node that also lies on Tracking/Extension
```

Priority rules:

- strong real object snaps such as Endpoint, Midpoint, Center, Quadrant and Intersection still win;
- SmartPoint Tracking candidates win over weak drafting aids such as Grid and Nearest;
- when Grid and Tracking/Extension are both active, Grid must not pull the point away from the visible temporary line;
- a grid node is highlighted only when it also lies on the active Tracking/Extension line, producing `TRACK GRID` or `EXT GRID`;
- in that combined case the temporary line remains dominant: the chosen point is constrained to the tracking/extension line and coincides with the grid node only because the grid node lies on that line.

Pointer input resolution rule:

```text
If a temporary SmartPoint Tracking marker is visible, the click must use that displayed candidate point.
The active tool must not re-snap that point to Grid, Ortho or Polar in a way that moves it away from the marker.
```

HUD labels are now drawn in the lower-left canvas overlay area instead of directly beside the marker, to avoid overlapping the Dynamic Command HUD. Current labels include:

```text
TRACK L=120 A=0°
EXT L=250 A=45°
TRACK INT
TRACK GRID L=120 A=0°
EXT GRID L=250 A=45°
SMART POINT
```

The labels, SmartPoints, tracking lines and temporary markers remain runtime-only: they are never persisted, exported, selectable or undoable.


## 2026-06-02 - Tool-level viewport-aware snap tolerance

Follow-up stabilization for zoom-proportional snapping.

The canvas-level snap marker and the lower-level tool input path must use the same effective tolerance. `Workspace.Context.SnapTolerance` remains the configured screen-space pixel radius, but pointer events sent to active tools are now executed with a temporary model-space tolerance computed from the current viewport.

Behavior contract:

- the saved/configured snap tolerance stays in pixels;
- `CadCanvas` converts that pixel value to model units for each pointer press, move and release;
- active tools such as Move and Grip Edit that perform their own `SnapRequest` receive the converted model-space tolerance;
- after the pointer event returns, the original configured pixel value is restored immediately;
- Grid snap, object snap, Move preview and Grip Edit preview should therefore agree with the visible snap marker at the current zoom.

Manual verification expectations:

- With grid step 5 and grid snap enabled, moving an entity or grip by one visible grid cell should propose 5, not unexpectedly jump to 10 or another farther grid node.
- Zooming in should make adjacent grid/object snap candidates easier to distinguish without requiring any setting change.
- Releasing the pointer after a grip or move operation should commit to the same snap target shown by the preview marker.
