# Ortho, Grid, and Polar Tracking

Ortho, Grid, and Polar Tracking are drawing aids. They do not create geometry by themselves, but they influence how points are chosen while drawing or editing.

These aids are useful because technical drawing often requires clean directions, regular spacing, and predictable angles. They should help the user draw faster without replacing exact object snaps or numeric input.

## Ortho

Ortho constrains movement to the main horizontal and vertical directions. It is useful when drawing walls, axes, construction lines, rectangular layouts, orthogonal moves, and any operation where the next point must stay aligned with the previous one.

When Ortho is active, the cursor may move freely, but the point used by the command is projected onto an orthogonal direction. This lets the user draw visually while keeping the result geometrically clean.

## Grid

The grid is a visual reference on the canvas. It helps estimate distances, understand scale, and keep orientation while working. The visible grid does not necessarily mean that points will snap to it.

The grid should remain a background aid. It should not visually interfere with the drawing, the top bar, the side panels, or the status bar.

## Grid Snap

Grid Snap constrains point picking to grid intersections. It is useful when working with regular modules, repeated spacing, schematic layouts, or early design sketches where a strict grid is desired.

Because Grid and Grid Snap are separate concepts, it is possible to use the grid only as a visual guide, or to enable snapping when exact grid spacing is needed.

## Polar Tracking

Polar Tracking helps draw or move along predefined angular directions. Instead of limiting input only to horizontal and vertical directions, it can guide the cursor along angular presets such as 90 degrees, 45 degrees, 30 degrees, or 15 degrees, depending on the selected mode.

Polar Tracking is useful for diagonals, technical details, roof slopes, furniture layouts, and repeated angular construction. It is less restrictive than Ortho but more controlled than completely free movement.

## Relationship with snaps and HUD input

Object snaps define exact geometric points. Ortho and Polar Tracking define directions. The HUD can define exact numeric values. In practice, the strongest workflows combine all three: snap the base point, constrain the direction, then type the exact distance.

For example, to draw a precise horizontal segment from an existing endpoint, snap to the endpoint, keep Ortho active, type the distance in the HUD, and confirm. To draw a diagonal at a known angle, use Polar Tracking or enter the angle in the HUD.

## Recommended visuals

This chapter should include simple diagrams exported from OpenCad2D itself. A good set of images would show one line drawn freely, the same line constrained by Ortho, the same idea with Polar Tracking, and a point constrained to Grid Snap.

## Visual assets to add

This chapter should compare free drawing, Ortho, Grid Snap, and Polar Tracking using the same simple line command. Recommended assets are `docs/assets/gifs/snaps/ortho-line.gif`, `docs/assets/gifs/snaps/grid-snap-line.gif`, and `docs/assets/gifs/snaps/polar-tracking-angle.gif`.
