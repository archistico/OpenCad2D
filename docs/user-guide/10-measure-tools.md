# Measure Tools

Measure tools inspect geometry without necessarily creating new drawing entities. They are useful while checking a project, validating distances, confirming areas, reading coordinates, or understanding imported geometry.

Use Measure tools when you need information. Use Dimension tools when the result must remain visible as an annotation in the final drawing.

## Distance measurement

Distance measurement reports the distance between two picked points. Snaps are important here: if the points are approximate, the measurement is approximate; if the points are snapped, the measurement is geometrically meaningful.

A typical workflow is to snap to the first endpoint, snap to the second endpoint, and read the resulting distance.

## Point coordinates

Point coordinate measurement reports the position of a selected point in the drawing coordinate system. It is useful for checking origins, insertion points, survey references, imported geometry, and precise construction positions.

When using this tool, make sure the point is selected with the intended snap mode. A nearby endpoint, midpoint, grid point, or intersection may produce a different coordinate.

## Area measurement

Area measurement reports the area of a closed shape or boundary. It is useful for rooms, zones, filled regions, closed polylines, and technical checks.

For reliable results, the boundary must be closed or otherwise clearly defined. If small gaps exist, the area may fail or return an unexpected value.

## Measuring curves and polylines

Lines, arcs, circles, ellipses, and polylines can expose different values: length, radius, diameter, circumference, arc length, or approximate area depending on the entity and the current implementation.

Curved geometry should be measured using its native definition whenever possible. This gives better results than treating curves as rough visual approximations.

## Measurements and documentation

Measurements are usually temporary. They help the user verify the drawing, but they do not replace dimensions in a deliverable technical drawing.

If a value must be visible to another person reading the drawing, create a dimension or annotation instead of relying on a temporary measurement result.
