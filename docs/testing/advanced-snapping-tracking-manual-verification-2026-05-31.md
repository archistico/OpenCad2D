# Advanced snapping and SmartPoint tracking manual verification - 2026-05-31

This checklist records the manual verification scope for the pre-v0.9 advanced snapping milestone.

The implemented scope is intentionally conservative:

- Nearest snapping is available but disabled by default.
- SmartPoints are captured only from strong geometric snaps.
- Tracking overlays are temporary, command-scoped drafting aids.
- Horizontal/vertical tracking, tracking intersections and first-pass line/polyline entity extension are implemented.
- Direct numeric distance input can resolve along an active tracking or extension line.

---

## Nearest snap

Expected behavior:

- Nearest is off in a new document unless explicitly enabled from the Snap bar.
- When enabled, Nearest snaps to the closest valid point on supported geometry.
- Explicit geometric snaps have priority over Nearest.
- Nearest can beat Grid, because it is a real entity-derived point.
- Nearest uses a small circle with an internal X marker.

Manual checks:

```text
[ ] New document: Nearest checkbox is off.
[ ] Enable Nearest: marker appears on the closest point along a line segment.
[ ] Move near a line endpoint: Endpoint wins over Nearest.
[ ] Move near a midpoint: Midpoint wins over Nearest.
[ ] Move near a circle/arc/polyline: closest-point snapping is stable.
[ ] Disable Nearest: closest-point snapping disappears.
```

---

## SmartPoint Tracking toggle

Expected behavior:

- The Snap bar exposes a **SmartPoint Tracking** checkbox.
- When disabled, SmartPoint markers, tracking lines, tracking intersections and extension markers are cleared and no longer produced.
- Normal object snaps remain controlled by their own snap checkboxes.

Manual checks:

```text
[ ] Disable SmartPoint Tracking and hover over endpoint/midpoint/center snaps: no SmartPoint marker is captured.
[ ] Re-enable SmartPoint Tracking and repeat the hover: SmartPoint marker is captured again.
[ ] Disable SmartPoint Tracking while markers are visible: existing temporary markers disappear.
```

---

## SmartPoint capture

Expected behavior:

- SmartPoint capture works only while a point-based command is active.
- Hovering over a strong snap for about 400 ms captures a temporary SmartPoint.
- Captured SmartPoints are not persisted, exported, selectable or undoable.
- The store keeps a maximum of five points and removes the oldest one when needed.
- Nearest, Grid, Entity, Perpendicular and Tangent do not create SmartPoints.

Manual checks:

```text
[ ] Start Line and hover on an endpoint: a SmartPoint marker appears after the delay.
[ ] Move quickly over an endpoint: no SmartPoint is captured.
[ ] Capture six points: the first captured point disappears.
[ ] Hover over Nearest: no SmartPoint is captured.
[ ] Hover over Grid: no SmartPoint is captured.
[ ] Finish or cancel the command: SmartPoints are cleared.
```

---

## Horizontal and vertical tracking lines

Expected behavior:

- Each captured SmartPoint emits horizontal and vertical tracking overlays.
- Tracking lines are dashed runtime overlays and are not real drawing entities.
- When the cursor is within snap tolerance of a tracking line, the active snap can become `Tracking`.
- Strong geometric snaps remain higher priority.

Manual checks:

```text
[ ] Capture one SmartPoint: horizontal and vertical dashed lines appear.
[ ] Move near the horizontal line: Tracking marker appears on the projected point.
[ ] Move near the vertical line: Tracking marker appears on the projected point.
[ ] Move near a real endpoint while also near tracking: Endpoint wins.
[ ] End the command: tracking lines disappear.
```

---

## Direct distance input on tracking

Expected behavior:

- When the active candidate is `Tracking`, typing a plain number resolves from the SmartPoint origin along the signed tracking direction.
- Cursor side determines the sign/direction.
- Explicit coordinate input remains higher priority than direct-distance tracking input.

Manual checks:

```text
[ ] Capture a SmartPoint, move right on its horizontal tracking line, type 100: point is 100 units to the right.
[ ] Capture a SmartPoint, move left on its horizontal tracking line, type 100: point is 100 units to the left.
[ ] Capture a SmartPoint, move up/down on its vertical tracking line, type 100: point follows the active vertical direction.
[ ] Type explicit coordinates such as 10,20: explicit coordinate input wins over tracking distance.
```

---

## Tracking intersections

Expected behavior:

- Tracking intersections are created only from tracking lines belonging to different SmartPoints.
- Parallel lines are ignored.
- The horizontal/vertical pair from the same SmartPoint does not create a self-intersection snap.
- The candidate is offered only when the cursor is close enough to the computed intersection.

Manual checks:

```text
[ ] Capture two SmartPoints with different X/Y coordinates.
[ ] Move near the intersection of one horizontal and one vertical tracking line: TrackingIntersection appears.
[ ] Click while the circular marker with the internal cross is visible: the command uses the intersection point, not the raw cursor point.
[ ] Move far from the intersection: no TrackingIntersection appears.
[ ] Verify that the same SmartPoint's own horizontal/vertical crossing is not exposed as an intersection candidate.
[ ] Verify strong real snaps still win over TrackingIntersection.
```

---

## Entity extension tracking

Expected behavior:

- A SmartPoint captured from a line entity can emit an extension tracking line using the real line direction.
- A SmartPoint captured from a straight polyline segment can emit an extension tracking line using that segment direction.
- Bulged/arc polyline segments, arcs and tangents are intentionally deferred.
- Extension candidates carry tracking origin/direction metadata, so direct distance input works along the extension.

Manual checks:

```text
[ ] Draw a slanted line and capture one endpoint.
[ ] Move along the continuation of the line: Extension marker appears on the projected point.
[ ] Type 100 while on the extension: point resolves 100 units along the extension direction.
[ ] Draw a polyline with straight segments and capture a segment endpoint/midpoint: extension follows the straight segment direction.
[ ] Verify extension does not appear for unsupported arc/bulge cases.
[ ] Finish or cancel the command: extension overlays disappear.
```


---

## Tracking intersections with real geometry

Expected behavior:

- A tracking line can expose a temporary intersection snap with a real line entity.
- A tracking line can expose a temporary intersection snap with a straight polyline segment.
- The candidate is shown only when the cursor is near the computed intersection.
- The real segment intersection must be finite; intersections outside the line/polyline segment are ignored.
- Curved/tangent/overlapping cases are intentionally deferred.

Manual checks:

```text
[ ] Capture a SmartPoint and move along its horizontal/vertical tracking line near a real line crossing: circular TrackingIntersection marker appears.
[ ] Click while the marker is visible: the command uses the computed crossing point, not the raw cursor point.
[ ] Repeat with a straight polyline segment crossing the tracking line.
[ ] Move near where the infinite extension would cross but the finite segment does not: no TrackingIntersection is exposed.
[ ] Verify strong real snaps still win if an Endpoint/Midpoint/Intersection is also within tolerance.
```

---

## Regression rules

Do not regress these behaviors while extending SmartTrack:

```text
[ ] Selection-only prompts must keep using EntityOnly snapping.
[ ] SmartPoints must not be persisted in .opencad2d.json files.
[ ] Tracking overlays must not be exported to SVG/DXF/PDF/PNG.
[ ] Tracking and Extension must not add undo/redo entries by themselves.
[ ] Nearest must remain disabled by default.
[ ] Strong geometric snaps must keep priority over Tracking, TrackingIntersection, Extension, Nearest and Grid.
```

---

## SmartPoint Tracking HUD label

Expected behavior:

- When a `Tracking` marker is active, a compact label appears near the marker.
- Tracking and Extension labels show distance and angle from the captured SmartPoint origin.
- TrackingIntersection labels show `TRACK INT` when no distance direction is available.
- The label is temporary UI only; it must not be saved, exported, selected or added to undo/redo.

Manual checks:

```text
[ ] Capture a SmartPoint and move along a horizontal/vertical tracking line: label shows TRACK with length and angle.
[ ] Enable Polar Tracking and move along a diagonal SmartPoint tracking line: label angle follows the diagonal direction.
[ ] Move along an entity extension: label shows EXT with length and angle.
[ ] Move near a tracking intersection: label shows TRACK INT.
[ ] Turn off SmartPoint Tracking: marker, tracking line and label disappear.
```


### Direct SmartPoint snap update

Captured SmartPoints now produce `SnapKind.SmartPoint` temporary candidates when the cursor is close to the marker. The click is resolved to the captured point, and the marker can therefore be used directly as a snap point as well as a tracking/extension origin.
