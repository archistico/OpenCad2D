# Edit Tools

Edit tools modify existing entities. They are used after geometry has been drawn, imported, inserted from the Library, or created by another command.

Some edit commands work best when entities are selected first. Others start by asking for target entities. In both cases, OpenCad2D should keep point picking and entity selection distinct: when the command needs an object, it should help the user select the object; when it needs a point, it should use snaps and precision input.

## Move

Move changes the position of selected entities. Select the entities, choose a base point, then choose the destination point or enter a displacement through the HUD.

A precise workflow is to snap the base point to a known location, constrain the direction with Ortho or Polar Tracking, type the distance, and confirm.

## Copy

Copy works like Move but keeps the original entities. It is useful for repeated objects, construction elements, furniture, symbols, annotations, and drawing details.

When copying many elements, make sure the base point is meaningful. A good base point makes the copied geometry easy to place with snaps.

## Rotate

Rotate turns selected entities around a base point. The rotation angle can be picked visually or entered through the HUD.

Use snaps to place the base point exactly. For standard angles, Ortho, Polar Tracking, or typed angle input are more reliable than freehand rotation.

## Scale

Scale changes the size of selected entities relative to a base point. A scale factor greater than 1 enlarges the selection; a factor between 0 and 1 reduces it.

Scaling should be used carefully with dimensions and text. If the visual result is not appropriate, adjust styles or annotation sizes instead of repeatedly scaling annotation entities.

## Mirror

Mirror creates a reflected copy of selected entities across an axis. Pick the first point of the mirror axis, pick the second point, then decide whether to keep or delete the original objects.

The mirror axis is just a construction direction for the operation; it does not need to remain as a drawn entity unless the user creates it separately.

## Offset

Offset creates parallel or concentric geometry at a specified distance. It is commonly used for walls, outlines, technical details, and repeated boundaries.

The command should preserve native geometry where possible. Offsetting a polyline with arcs should keep meaningful curves instead of unnecessarily degrading the result into disconnected approximations.

## Fillet

Fillet rounds a corner with an arc of a given radius. It can be used on compatible lines and polyline segments. The radius is entered through the HUD or reused from the current command value.

A correct fillet must be tangent to both participating segments. If the radius is too large for the available geometry, the command should not create an incorrect result.

Use this sequence:

1. Start `FILLET` / `F`. The HUD shows **1/2**, the current radius and Trim/NoTrim mode. The initial radius is zero, which joins line endpoints as a sharp corner. Use `R`, enter a positive radius, and press Enter for a rounded corner.
2. Click the first line or straight polyline segment on the side you want to **keep**. For a polyline, only the picked segment is emphasized.
3. In **2/2**, move over the second object or adjacent segment and check the preview. Click to apply it. You can continue with another pair, or press Enter/right click to finish and return to Selection.

The picks determine the retained sides. A pick at an interior crossing does not identify a side, so Fillet asks you to move away from the crossing. At a shared endpoint, the existing line direction is sufficient. A radius that would consume a retained line or reverse it is rejected without changing the drawing.

You can correct settings while waiting for the second object: `R` changes the radius and `T` opens Trim/NoTrim, preserving the first selection. Inside the mode prompt, choose `T` for Trim (modify the sources) or `N` for NoTrim (keep the sources and add only the arc). Enter in a parameter prompt keeps the current setting and resumes the pending selection. `F` / `First` chooses the first object again while keeping the radius and mode. During Fillet, this contextual `F` option takes precedence over the command alias.

Errors appear inside the HUD. An invalid radius stays editable; invalid geometry keeps the selected first object so you can change settings or pick another second object. If the first source is removed, changed, hidden or locked, Fillet requests a new first selection. Parameter editing clears the old geometric preview. Undo/Redo restores completed fillets; Esc exits without undoing completed work.

Supported combinations are two lines, adjacent segments of one wholly straight polyline in Trim mode, and lines or terminal segments of separate open straight polylines. Existing curved polylines remain unsupported. NoTrim requires a positive radius; rounding a corner within one polyline also requires a positive radius and Trim mode. Small radius values remain readable without rounding to zero in the Radius field.

## Chamfer

Chamfer cuts a corner with a straight bevel. It uses one or more distances depending on the command mode. Like Fillet, it should work on compatible lines and polyline segments.

Chamfer is useful for technical drawings where a corner must be flattened instead of rounded.

## Trim

Trim removes parts of entities using one or more cutting boundaries. It is used to clean intersections, shorten lines, open shapes, and remove unnecessary geometry.

During target selection, the command should focus on selecting entities or segments, not on ordinary point snaps. The user is choosing what to trim, not placing a new point.

The HUD labels the two steps explicitly:

1. **Choose cutting edges (1/2).** Click the boundary that will stop the cut. Alternatively, press Enter/right click or choose `A` / `All` to use all visible supported curves. This step changes no geometry.
2. **Choose the part to remove (2/2).** After the first cutting edge is chosen, move over the unwanted part. Cutting edges stay red. The green dashed portion will be removed; the green solid portions will remain. Click inside the dashed portion to cut it. Moving away restores the object's normal appearance without changing the drawing. You can continue cutting other parts with the same edges.

Do not click exactly on a crossing: that does not identify a side, so Trim keeps the drawing unchanged and explains where to click. A closed curve needs at least two distinct cutting intersections; a tangent alone is insufficient.

While trimming, hold Ctrl and click to add cutting edges. Ctrl-hover highlights an edge candidate without suggesting a cut. The HUD reports the active edge count or All mode, and shows failure details next to the command instructions. Locked visible layers can provide reference edges; targets on locked layers cannot be trimmed. Hidden edges are ignored.

If you selected the wrong boundary, use `E` / `Edges` to choose the boundaries again. Completed cuts are kept. `U` / `Undo` undoes a cut from the current Trim session only when it is the next undoable action; it never undoes an unrelated drawing action. Enter or right click during step 2 finishes Trim and returns to Selection. Esc also leaves the command while keeping completed cuts; use Undo to restore a cut.

## Extend

Extend lengthens entities until they meet selected boundaries. It is the counterpart of Trim and is useful when lines or curves need to reach an existing edge.

The HUD labels two steps:

1. **Choose boundaries (1/2).** Click a visible boundary, or press Enter/right click or `A` / `All` to use all visible supported curves. Selected boundaries appear red. Ctrl-click adds boundaries during step 2.
2. **Choose the endpoint (2/2).** Hover near the end of the object you want to lengthen. Solid green shows the existing geometry; dashed green shows the portion that will be added. Click near that endpoint to apply the extension to the nearest reachable boundary. You can extend other objects without restarting. Moving away clears the simulation without changing the drawing.

Click closer to an endpoint rather than at the middle: an equidistant pick does not choose a side and leaves the drawing unchanged. An endpoint already touching a boundary does not jump through it to another intersection. Failure details and recovery instructions appear in the HUD.

Use `E` / `Edges` to choose different boundaries while keeping completed extensions. `U` / `Undo` undoes an extension made during the current command only when it is the next undoable action; it never undoes unrelated work. Enter/right click in step 2 finishes and returns to Selection. Esc exits while keeping completed extensions.

Targets can be lines, circular arcs, elliptical arcs and open polylines with a straight segment at the chosen end. Curved polyline ends, closed curves and splines cannot be extended. Boundaries can be lines, circles, arcs, ellipses, elliptical arcs and polylines. Visible locked layers can supply reference boundaries; targets on locked layers cannot be changed. Hidden boundaries are ignored. Changed or removed geometry invalidates the old preview and is refreshed before the next operation.

## Break Pt

Break Pt splits or breaks an entity at a chosen point. Use snaps when the break point must be exact, for example at an intersection or endpoint.

The command is useful when a continuous entity must become editable in separate parts.

## Break Seg

Break Seg removes a portion of an entity between two points. It is useful for openings, gaps, and local cleanup.

The two break points should be selected carefully. If the segment is small or close to other geometry, zoom in before confirming the operation.

## Divide

Divide places point markers along an entity without modifying the original entity. For open entities it creates internal division points; for closed entities it can distribute points around the full loop.

Use Divide when you need construction markers, repeated placement references, or equal subdivisions without changing the source geometry.


## Boundary Fill

Boundary Fill creates a filled closed polyline from the area around a picked internal point. Start the command, click or type a point inside a closed boundary, check the preview, then press Enter or right-click to confirm. Esc cancels the preview without adding anything to the drawing.

The v2 workflow can use visible line and polyline boundaries, sampled circles/arcs and bulged polyline segments. It can also bridge small endpoint-to-endpoint gaps and endpoint-to-segment gaps. Use the `Gap` option before picking the point, or while a preview is active, to set the gap tolerance. If you change `Gap` during preview, OpenCad2D recalculates the preview from the same seed point.

Boundary Fill still creates a single filled `PolylineEntity`, not a true hatch object. Holes, islands, hatch patterns and associative updates are intentionally left to the later HatchEntity milestone. Text, dimensions, images, blocks and annotation-only geometry are ignored as boundary sources; the command reports ignored unsupported entities when they are present.

## Delete

Delete removes the selected entities from the drawing. It should be undoable like other editing operations.

Before deleting a large selection, check whether locked or hidden layers are involved and whether the selection contains blocks, images, or dimensions that should be preserved.

## Explode

Explode breaks a compound entity into simpler entities. A block may become its contained drawing entities; a polyline may become its component segments, depending on the current implementation.

Explode is powerful but should be used deliberately. Once an object is exploded, it may lose some of the higher-level behavior that made it convenient to edit as one object.

## Join

Join combines compatible entities into a polyline or continuous object where possible. It is useful after drawing or importing disconnected segments that should behave as one boundary.

Join requires geometric compatibility. If endpoints do not meet within tolerance, or if the selected entities cannot form a valid continuous result, the command should explain why it cannot complete the operation.

## Visual assets to add

The edit tools chapter should first document the commands that users repeat most often: Move, Copy, Rotate, Scale, Offset, Trim, Extend, Fillet, Chamfer, Break, Explode, and Join. Recommended assets are `docs/assets/gifs/edit-tools/move-with-hud.gif`, `docs/assets/gifs/edit-tools/rotate-selection.gif`, `docs/assets/gifs/edit-tools/trim-boundary.gif`, `docs/assets/gifs/edit-tools/fillet-radius.gif`, and `docs/assets/gifs/edit-tools/chamfer-distance.gif`.
