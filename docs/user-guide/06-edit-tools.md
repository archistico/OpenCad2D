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

## Chamfer

Chamfer cuts a corner with a straight bevel. It uses one or more distances depending on the command mode. Like Fillet, it should work on compatible lines and polyline segments.

Chamfer is useful for technical drawings where a corner must be flattened instead of rounded.

## Trim

Trim removes parts of entities using one or more cutting boundaries. It is used to clean intersections, shorten lines, open shapes, and remove unnecessary geometry.

During target selection, the command should focus on selecting entities or segments, not on ordinary point snaps. The user is choosing what to trim, not placing a new point.

## Extend

Extend lengthens entities until they meet selected boundaries. It is the counterpart of Trim and is useful when lines or curves need to reach an existing edge.

As with Trim, the selection phase should feel entity-oriented. The command should make it clear which entity will be extended and which boundary is being used.

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

The v2 workflow can use visible line and polyline boundaries, sampled circles/arcs and bulged polyline segments. It can also bridge small endpoint gaps. Use the `Gap` option before picking the point, or while a preview is active, to set the gap tolerance. If you change `Gap` during preview, OpenCad2D recalculates the preview from the same seed point.

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
