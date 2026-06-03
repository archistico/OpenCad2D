# Line Formats

Line formats control how drawing geometry is displayed and exported. In OpenCad2D, a line format is not just a dash pattern: it is the complete appearance assigned to layers and, where supported, resolved by entities when they are drawn.

A line format normally combines a name, a color, a line weight and a line style. The line style defines the pattern, such as continuous, dashed or dash-dot. The line format is the reusable object that can be assigned to a layer so that all geometry on that layer follows the same visual rule.

OpenCad2D currently treats the layer as the main place where line appearance is managed. This keeps drawings predictable: instead of changing every entity one by one, you usually create or edit a line format, assign it to a layer, and draw the relevant geometry on that layer.

## Built-in line styles

The common built-in styles are continuous, dashed, dash-dot and dash-dot-dot. Continuous lines are used for normal visible geometry. Dashed lines are useful for hidden, reference or construction geometry. Dash-dot styles are commonly used for axes, center lines and other technical references.

Custom dash patterns may also be available depending on the current state of the application. Dash pattern values are expressed in drawing units, not screen pixels. This is important because the pattern must remain meaningful when exported to vector formats such as SVG, DXF or PDF.

## Line weight

Line weight controls stroke thickness. In technical drawings, this is not only a visual preference: it helps distinguish primary geometry, secondary references, construction lines, annotations and axes.

A practical convention is to keep main geometry heavier, construction and axis geometry lighter, and annotation geometry visually readable but not dominant. For example, wall geometry may use a stronger line weight, while axes and construction lines may use thinner dashed or dash-dot formats.

## Relationship with layers

The normal workflow is to define the visual appearance at layer level. New entities are created on the current layer and use that layer appearance. This makes layer management more important than individual visual overrides.

For example, if the `Walls` layer uses a thick continuous line format, every wall entity drawn on that layer should appear consistently. If the `Axis` layer uses a red dash-dot format, all axis geometry immediately follows that convention.

This approach is also useful during import and export. When a drawing is imported, OpenCad2D should avoid creating duplicate formats when an equivalent line format already exists. When a drawing is exported, the resolved line format is used to produce the closest representation supported by the target format.

## Typical use

Use continuous formats for real visible geometry, dashed formats for hidden or construction elements, and dash-dot formats for axes and center references. Keep names clear and functional, such as `Walls continuous 2.0`, `Construction dashed 0.75` or `Axis dash-dot 0.75`, instead of generic names that become hard to understand later.

The goal is not to create many formats. The goal is to create a small set of formats that make the drawing readable and consistent.
