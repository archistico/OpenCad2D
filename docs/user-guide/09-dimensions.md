# Dimensions

Dimension tools create persistent annotations that show measured values in the drawing. They are different from Measure tools: a measurement is usually temporary information, while a dimension remains in the document as part of the technical drawing.

Dimensions should be placed on an annotation-oriented layer and controlled through dimension styles. This keeps the drawing readable and makes it easier to update text size, arrows, offsets, symbols, and formatting consistently.

## Horizontal Dimension

Horizontal Dimension measures the horizontal distance between two points. The measured value is based on the X direction, regardless of the vertical difference between the picked points.

Use it for plans, room widths, horizontal offsets, and technical details where the horizontal component is the value that matters.

## Vertical Dimension

Vertical Dimension measures the vertical distance between two points. The measured value is based on the Y direction.

Use it for heights, vertical offsets, section details, elevations, and any situation where the vertical component must be annotated.

## Aligned Dimension

Aligned Dimension measures the true distance between two points along their direction. Unlike horizontal or vertical dimensions, it follows the line between the picked points.

Use it for sloped elements, diagonal walls, inclined details, and any geometry where the real segment length must be shown.

## Angular Dimension

Angular Dimension measures the angle between two directions or entities. It is useful for slopes, rotations, construction geometry, mechanical details, and non-orthogonal layouts.

The picked entities or points must clearly define the angle. If the result is ambiguous, zoom in and choose the defining geometry more carefully.

## Radius Dimension

Radius Dimension annotates the radius of a circle or arc. It is normally used when the radius value is more relevant than the diameter.

Use radius dimensions for rounded corners, arcs, fillets, curved details, and circular construction features.

## Diameter Dimension

Diameter Dimension annotates the diameter of a circle or circular feature. It is normally used when the full width across the circle is the required value.

Use diameter dimensions for holes, circular objects, round symbols, and technical details where diameter is the standard notation.

## Dimension styles

Dimension appearance is controlled by dimension styles. A style can define text format, arrow or symbol type, offsets, prefix, suffix, arrow size, and other visual settings.

Use styles instead of manually adjusting every dimension. A drawing is easier to maintain when dimensions share a coherent visual system.

See also: [Dimension Styles](19-dimension-styles.md).

## Dimensions and editing

Dimensions are drawing entities. They can be selected, moved, assigned to layers, and edited through the Properties Panel where supported.

When a dimension does not look right, first check the dimension style. The problem is often a style setting such as text size, offset, arrow type, or suffix rather than the measured geometry itself.

## Visual assets to add

Dimensions should be documented with static examples and at least one interaction GIF. Start with `docs/assets/images/dimensions/dimension-types-overview.png`, `docs/assets/gifs/draw-tools/aligned-dimension.gif`, and `docs/assets/images/dimensions/dimension-style-example.png`. The images should show readable text and avoid crowded geometry.
