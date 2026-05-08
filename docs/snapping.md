# Snapping

Snapping is the system that helps the user place points precisely.

Instead of relying only on the raw cursor position, a tool can ask the snapping system for a better point near the cursor. This can be an endpoint, a midpoint, the center of a circle, an intersection, a perpendicular point, a tangent point or a grid point.

Snapping is implemented in `OpenCad2D.Interaction`, not in the UI. This means the same snapping logic can be tested without Avalonia and can be reused by any future user interface.

---

## Main idea

The UI sends model-space coordinates to the active tool.

The tool, through `ToolContext`, asks `SnapService` for a snap candidate.

If a candidate is found within the configured tolerance, the tool uses the snapped point instead of the raw cursor point.

The UI can also ask the snapping system for the current snap candidate in order to draw a visual marker.

The important point is that snapping works in model coordinates. The canvas is responsible for converting screen coordinates to model coordinates before snapping is evaluated.

---

## Main types

The snapping system is based on a few main types.

`SnapKind` identifies the available snap modes.

`SnapRequest` describes a snap query. It contains the document, cursor point, tolerance, enabled snap modes, optional base point and grid settings.

`SnapCandidate` represents one possible snap point.

`ISnapProvider` is implemented by each snap provider.

`SnapService` coordinates all providers and chooses the best candidate.

`GridSettings` describes the grid used by grid snapping.

---

## SnapKind

`SnapKind` is a flags enum.

This means several snap modes can be enabled at the same time.

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
```

The UI exposes most of these modes through snap checkboxes in the toolbar.

---

## SnapRequest

`SnapRequest` contains all information needed to evaluate snapping.

It includes the current document, the cursor point in model coordinates, the snap tolerance, the enabled snap kinds, the optional base point and the grid settings.

The optional base point is important for contextual snaps.

For example, perpendicular and tangent snapping cannot be calculated from the cursor point alone. They need to know the point from which the perpendicular or tangent should be constructed.

In a two-point tool such as `LineTool`, the first point becomes the base point while the user is choosing the second point.

---

## SnapCandidate

A `SnapCandidate` contains the result produced by a snap provider.

It stores the snap kind, the snapped point, the optional entity id and the distance from the cursor.

The distance is used by `SnapService` to choose the best candidate among multiple results with the same priority.

The UI can also use the candidate to draw a snap marker and display the snap kind in the status bar.

---

## SnapService

`SnapService` owns the list of snap providers.

When a request arrives, it asks only the providers whose `SnapKind` is enabled in the request.

Each provider can return zero or more candidates.

Then `SnapService` orders candidates by priority and distance.

The current priority is designed to favor precise geometric snaps over more generic ones.

For example, endpoint and intersection have higher priority than nearest and grid.

This prevents the grid from stealing the cursor when a more meaningful entity snap is available nearby.

---

## Direct snaps

Some snaps are direct.

They only need the cursor point, document and tolerance.

Endpoint, midpoint, center, quadrant, intersection, nearest and grid are direct snaps.

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

It currently supports line endpoints, polyline vertices and arc endpoints.

This is one of the most important snaps because it allows the user to connect geometry precisely.

---

## Midpoint snapping

Midpoint snapping finds the middle point of a line segment.

For polylines, each segment can provide a midpoint.

For arcs, the provider calculates the angular midpoint of the arc.

---

## Center snapping

Center snapping finds the center of circles and arcs.

This is useful when drawing from or toward circular geometry.

---

## Quadrant snapping

Quadrant snapping finds the four cardinal points of a circle.

The quadrant points are located at 0, 90, 180 and 270 degrees.

For arcs, only quadrant points that actually lie on the arc are returned.

---

## Intersection snapping

Intersection snapping finds intersections between visible entities.

It currently supports several common combinations such as line-line, line-polyline, polyline-polyline, line-circle, circle-circle, line-arc and circle-arc.

Intersection snapping is one of the most useful precision tools in a CAD system.

As the geometry engine grows, this provider can be extended to support more combinations.

---

## Nearest snapping

Nearest snapping finds the closest point on an entity to the cursor.

It is more generic than endpoint, midpoint or intersection snapping.

For this reason it has lower priority than the more explicit snap modes.

Nearest snapping is useful, but it can also be noisy if enabled together with many precise snaps.

---

## Perpendicular snapping

Perpendicular snapping is contextual.

It needs a base point.

Given a base point and a target entity, it tries to find the point on the entity that creates a perpendicular connection.

For a line or segment, this is the projection of the base point onto the segment.

For a circle or arc, it is the radial point aligned with the base point.

This snap is especially useful when drawing lines perpendicular to existing geometry.

---

## Tangent snapping

Tangent snapping is contextual.

It needs a base point.

Given a base point outside a circle, the provider calculates the possible tangent points from that point to the circle.

For arcs, the tangent points are filtered so that only points lying on the arc are accepted.

If the base point is inside the circle or exactly on the circle, no tangent candidate is returned.

This snap is useful when drawing lines tangent to circles or arcs.

---

## Grid snapping

Grid snapping snaps the cursor to the nearest grid point.

The grid is described by `GridSettings`, which contains the step and origin.

For example, with a step of 10, the point `(23.2, 46.8)` snaps to `(20, 50)` if it is within tolerance.

Grid snapping works independently from visible entities.

The visual grid in the Avalonia canvas and the snapping grid should remain conceptually aligned, although they are separate concerns.

---

## Snap tolerance

Snap tolerance controls how far the cursor can be from a candidate.

If the distance between the cursor and the candidate is greater than the tolerance, the candidate is ignored.

The tolerance is stored in `ToolContext.SnapTolerance`.

The UI currently passes model-space cursor points to the snapping system, so the tolerance is also interpreted in model units.

This means that future work may be needed to make tolerance feel more consistent across zoom levels. A common CAD behavior is to define pick tolerance in screen pixels and convert it to model units through the viewport transform.

---

## Snap priority

When multiple candidates are available, `SnapService` chooses one.

The decision is based first on snap priority and then on cursor distance.

This means a high-priority snap can win even if another lower-priority snap is slightly closer.

The current priority favors precise snaps first, then generic snaps.

Grid has low priority, so it should not override endpoint or intersection snapping when both are available.

This behavior can be adjusted later if the user experience requires it.

---

## Snapping in tools

Most two-point tools use snapping through `TwoPointToolBase`.

When the tool is waiting for the first point, snapping is evaluated without a base point.

When the tool is waiting for the second point, the first point is passed as `BasePoint`.

This makes contextual snaps work naturally.

For example, `LineTool` can use tangent snapping only after the first click, because before that there is no base point.

---

## Snapping in the UI

The Avalonia UI uses snapping in two ways.

First, tools use snapping to decide the actual point used for drawing or editing.

Second, `CadCanvas` asks the snapping system for the current candidate while the mouse moves, so it can draw a visual snap marker.

The status bar displays the current snap kind, such as `Endpoint`, `Grid`, `Perpendicular` or `Tangent`.

This gives immediate feedback to the user.

---

## Future improvements

The snapping system can be improved in several ways.

One useful improvement is to make snap tolerance screen-based instead of purely model-based. This would make snapping feel more consistent when zooming in and out.

Another improvement is to draw different marker shapes for different snap kinds.

The intersection provider can also be extended as new entity types are added.

Future snap modes may include extension, apparent intersection, parallel, object tracking and polar tracking.

---

## Guidelines for new snap providers

A new snap provider should implement `ISnapProvider`.

It should return only candidates that are valid for its snap kind.

It should respect visibility rules and ignore invisible entities.

It should not modify the document.

It should work in model coordinates.

It should return candidates only within the requested tolerance.

If the snap requires a base point, it should return no candidates when `SnapRequest.BasePoint` is null.

The provider should have focused tests for normal cases, edge cases and invisible entities.

