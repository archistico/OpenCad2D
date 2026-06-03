# Canvas Navigation

This chapter explains how to move around the drawing area without changing the drawing itself. Navigation is independent from drawing and editing commands: you can zoom or pan to inspect a detail, then continue the active operation with better precision.

## Pan

Use the middle mouse button to move the visible area of the canvas. Press and hold the middle mouse button, move the mouse in the desired direction, then release it when the view is in the right position. Panning changes only the view; it does not move entities, change coordinates, or affect the current selection.

Recommended visual asset: `docs/assets/gifs/navigation/pan-canvas.gif`.

## Mouse wheel zoom

Use the mouse wheel to zoom in and out. Scrolling up zooms in, while scrolling down zooms out. The cursor position is used as the visual reference point, so zooming over a corner, endpoint, dimension, or small detail keeps that area under control while the view changes.

This is especially important when several snap points are close together. If OpenCad2D proposes a snap point that is too close to another one, zoom into the area and choose again. The snap behavior is designed to become more precise as the visual distance between points increases on screen.

Recommended visual asset: `docs/assets/gifs/navigation/mouse-wheel-zoom.gif`.

## Zoom Window

Zoom Window lets you choose a rectangular area and enlarge it to fill the canvas. Activate Zoom Window, click the first corner of the area, then click the opposite corner. OpenCad2D adjusts the view so the selected rectangle becomes the new working area.

Use Zoom Window when you know exactly which part of the drawing you want to inspect. It is useful for architectural details, dense symbols, dimensions, intersections, and imported drawings that contain small geometry in a larger plan.

Recommended visual asset: `docs/assets/gifs/navigation/zoom-window.gif`.

## Zoom Extents

Zoom Extents adjusts the view so that all visible drawing content fits inside the canvas. It is the fastest way to recover the drawing when you have zoomed too far, panned away from the model, opened a file and cannot see the content, or imported geometry that appears outside the current view.

Zoom Extents considers visible entities. If a layer is hidden, its content should not be used as the visible reference for the operation.

Recommended visual asset: `docs/assets/gifs/navigation/zoom-extents.gif`.

## Navigation during commands

Navigation should feel transparent while a command is active. You can zoom in to pick a point more accurately, pan to another area, and then continue the operation. This is common when drawing long walls, moving objects between distant areas, trimming geometry, or selecting a precise snap point inside a dense detail.

When documenting a command, prefer screenshots or GIFs that show the real workflow: start the command, zoom or pan when necessary, then finish the command. This explains OpenCad2D better than showing navigation as a separate isolated feature.

## Screenshot and GIF notes

The first images for this chapter should show a simple drawing with a few clear entities, not a crowded test file. Use the same sample drawing for Pan, Mouse Wheel Zoom, Zoom Window, and Zoom Extents so the user immediately understands that only the view is changing.

## Visual assets to add

This chapter should eventually include one consistent sample drawing shown through all navigation examples. The most useful assets are `docs/assets/gifs/navigation/pan-canvas.gif`, `docs/assets/gifs/navigation/mouse-wheel-zoom.gif`, `docs/assets/gifs/navigation/zoom-window.gif`, and `docs/assets/gifs/navigation/zoom-extents.gif`. The drawing should be simple enough that the user understands the view is changing, not the geometry.
