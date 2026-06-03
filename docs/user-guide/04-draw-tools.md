# Draw Tools

Draw tools create new geometry and annotation entities. They are the starting point for most OpenCad2D drawings: lines, polylines, rectangles, circles, arcs, ellipses, polygons, points, text, north symbols, and scale bars.

Most draw commands follow the same rhythm. Start the tool, pick the required points on the canvas, and use snaps, Ortho, Polar Tracking, Grid Snap, or the Dynamic HUD whenever the geometry must be exact. Mouse input gives speed; snaps and HUD input give precision.

## Line

Line draws a single segment between two points. Pick the start point, then pick the end point. After the first point is known, the HUD can be used to enter an exact distance, angle, X coordinate, or Y coordinate.

Line is usually combined with Endpoint, Midpoint, Intersection, Grid, Ortho, and Polar Tracking. For example, to draw a wall segment from an existing corner, snap to the corner, constrain the direction with Ortho, type the distance, and confirm.

## Polyline

Polyline creates a connected sequence of segments. Each click adds a new vertex and continues the same entity. This is useful for walls, outlines, paths, boundaries, and any geometry that should remain connected after drawing.

The command can also support options such as closing the polyline, undoing the last segment, or switching to arc mode. When Close is used, the last point is connected back to the first point and the polyline becomes a closed boundary.

## Polyline Arc Mode

Polyline Arc mode lets a polyline contain curved segments instead of only straight ones. The arc is defined as part of the same polyline, so later operations such as selection, offset, area calculation, or editing can treat the shape as one object.

Use this mode when a boundary contains both straight and curved portions. It is preferable to drawing unrelated lines and arcs when the final object should behave as one continuous outline.

## Rectangle

Rectangle creates a rectangular shape from two opposite corners. Pick the first corner, then pick the opposite corner. Snaps can be used for both corners, and the HUD can be used when one or both coordinates must be precise.

This command is useful for rooms, openings, simple furniture outlines, title blocks, frames, and construction geometry.

## Rectangle by Sides

Rectangle by Sides is intended for cases where the size is known before the opposite corner is visually picked. Start from the insertion corner, enter the width or distance, define the direction, then enter the height. This is more precise than dragging a rectangle approximately and correcting it later.

The command is especially useful for architectural objects, furniture blocks, panels, and repeated elements with known dimensions.

## Circle

Circle creates a circle from a center point and a radius. Pick the center, then pick a point on the circumference or enter the radius through the HUD.

Use Center, Endpoint, Midpoint, Intersection, and Grid snaps to place the center exactly. When the radius is known, typing it is usually faster and more accurate than picking it with the mouse.

## Arc

Arc creates a curved segment. Depending on the active implementation, the command may ask for a start point, intermediate point, end point, center, or radius. The important rule is that the resulting entity remains a native arc, not an approximated polyline, whenever possible.

Native arcs are important because they allow more accurate snapping, trimming, extending, dimensioning, and exporting.

## Arc 3P

Arc 3P creates an arc through three points. Pick the start point, a point on the arc, and the end point. This is useful when the curve must pass through known construction points or existing geometry.

Endpoint and Intersection snaps are usually the most useful snaps for the first and last points. The middle point controls the curvature and should be chosen carefully.

## Ellipse

Ellipse creates an elliptical curve from its defining points and orientation. Use it for technical details, furniture shapes, symbols, and curved objects that are not circular.

When precision matters, define the center and axes with snaps and HUD input rather than dragging the shape freely.

## Polygon

Polygon creates a regular multi-sided shape. The command asks for the number of sides and then uses points or numeric input to define its size and orientation.

Use Polygon for repeated regular shapes such as hexagons, octagons, technical symbols, and construction references. The number of sides should be chosen before the final geometry is confirmed.

## Point

Point creates a marker at a precise position. It is useful as a construction reference, a division marker, a survey point, or a temporary geometric aid.

Point entities are also created by tools such as Divide. They should be placed on an appropriate layer so they can be shown, hidden, or removed without affecting the main drawing.

## Text

Text creates a single-line annotation. It is appropriate for short labels, room names, notes, IDs, and simple drawing annotations.

For longer notes, use MText instead. Text should use the current text format unless the selected format is changed in the Properties Panel or through the text format tools.

## MText

MText creates multiline text. It is intended for longer annotations, paragraphs, legends, and notes where line wrapping and readable formatting matter.

Use MText when the content should behave as one annotation block instead of many independent single-line text entities.

## North Symbol

North Symbol inserts a north arrow annotation. It is normally used in plans to indicate drawing orientation. Place it on an annotation layer and keep it visually clear, usually outside the most detailed part of the plan.

If several north symbols are available in the future, fixed reusable variants should belong to the Library, while a parametric or tool-generated north symbol can remain a command.

## Metric Scale Bar

Metric Scale Bar creates a graphic scale reference. It is useful when a drawing may be viewed or printed at different sizes and the user still needs a visual indication of scale.

The scale bar should be placed where it does not interfere with the drawing itself, typically near the title area or annotation area.

## Visual assets to add

The first screenshots or GIFs for this chapter should cover the most common drawing workflows before documenting every command visually. Start with `docs/assets/gifs/draw-tools/draw-line-with-hud.gif`, `docs/assets/gifs/draw-tools/draw-polyline.gif`, `docs/assets/gifs/draw-tools/draw-rectangle-by-sides.gif`, `docs/assets/gifs/draw-tools/draw-circle-radius.gif`, and `docs/assets/images/tools/draw-tools-overview.png`. Later assets can cover Arc, Arc 3P, Ellipse, Polygon, Text, MText, North Symbol, and Metric Scale Bar.
