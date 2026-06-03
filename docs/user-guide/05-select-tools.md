# Select Tools

Selection is the bridge between drawing and editing. Before an entity can be moved, copied, deleted, inspected, or changed in the Properties Panel, it must be selected.

OpenCad2D should make selection predictable. Visible and unlocked entities can be selected; hidden entities cannot be selected because they are not visible; locked layers are intended to protect their content from accidental edits.

## Single selection

A single click selects the entity under the cursor. When only one entity is selected, the Properties Panel can show its specific properties, such as geometry, layer, line format, text settings, dimension settings, image reference information, or block information.

If the wrong object is selected, zoom in and try again. Precision in selection improves when overlapping or nearby entities are visually separated on screen.

## Multiple selection

Multiple selection lets several entities be edited together. It is used before commands such as Move, Copy, Rotate, Scale, Mirror, Delete, Align, Distribute, Explode, and Join.

When multiple entities are selected, the Properties Panel should show common editable properties. If selected entities have different values for the same property, the panel should make the mixed state clear instead of pretending that all objects share one value.

## Window selection

Window selection selects entities inside a rectangular area. It is useful when a group of objects is easier to select spatially than one by one.

Use window selection carefully in dense drawings. If the result includes too much geometry, zoom in first or select objects in smaller groups.

## Overlapping entities and selection cycling

When several entities overlap or are very close, OpenCad2D may need to cycle through possible selections. This avoids the common CAD problem where only the topmost or nearest entity can be selected.

Selection cycling should be documented with a simple example: two overlapping lines, or a line crossing a dimension or block. The user should be able to understand which object is currently proposed before confirming the selection.

## Select Last

Select Last restores the last meaningful selection set. It is useful after completing a command, cancelling a command, or accidentally clearing a selection that is still needed.

This command is especially helpful in workflows where the same group of entities is moved, copied, aligned, or inspected several times.

## Grips

Grips are direct editing handles shown on selected entities. They allow the user to modify geometry visually, for example by moving an endpoint, changing a shape, or adjusting a dimension-related point.

Grip editing should still respect snaps and precision aids where appropriate. If a grip is close to other geometry, zoom in before dragging it.

## Blocks, images, and dimensions

Blocks, images, and dimensions can be selected like other entities, but their editing behavior may be specific. A block may move as one object until exploded. An image remains an external raster reference. A dimension is an annotation entity with style and geometry-related properties.

When selection feels unexpected, check the Properties Panel. It is often the fastest way to confirm what kind of entity is currently selected.

## Visual assets to add

Selection needs clear visuals because many editing problems begin with the wrong selection state. Add `docs/assets/gifs/edit-tools/select-single-and-multiple.gif`, `docs/assets/gifs/edit-tools/window-selection.gif`, and `docs/assets/images/tools/selection-grips-overview.png` when screenshots are available.
