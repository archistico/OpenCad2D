# Snaps

Snaps help place points precisely. Instead of using the raw cursor position, OpenCad2D can propose a meaningful point near the cursor, such as an endpoint, midpoint, center, intersection, perpendicular point, tangent point, grid point, or image reference point.

Snaps are one of the most important parts of a CAD workflow. Most drawing errors come from points that look correct visually but are not geometrically exact. When a snap marker appears, the point used by the command is the snapped point, not merely the approximate mouse position.

## Object snaps

Endpoint snaps to the start or end of a line, arc, polyline segment, or other supported entity. Midpoint snaps to the middle of a segment. Center snaps to the center of circles, arcs, ellipses, and other supported circular or curved entities. Intersection snaps to the geometric crossing point between entities.

Perpendicular and Tangent are contextual snaps. They depend on the current command phase and the base point already chosen. They are useful when constructing geometry with exact geometric relationships rather than approximate visual alignment.

Nearest snaps to the closest supported point on an entity. It is useful when the exact position along an object matters less than staying exactly on the object.

## Grid snap

Grid snap constrains point picking to grid intersections. It is different from the visible grid. The grid may be visible without forcing picks to the grid, and grid snap may be enabled as a precision aid when a regular spacing is useful.

Grid snap should not be confused with object snaps. Object snaps use existing geometry. Grid snap uses the drawing grid.

## Image snaps

Image references can provide snap points such as corners or edges, depending on the current implementation. This is useful when positioning or tracing over attached raster references.

Images are references, not native vector geometry. Snapping to image reference points helps place entities relative to the image, but it does not turn the image into editable CAD geometry.

## Entity-only snapping

When a command is asking the user to select an entity, OpenCad2D should use an entity-oriented snap mode rather than normal point snaps. This prevents a common problem where the software proposes an endpoint or midpoint when the user is actually trying to choose the object itself.

This behavior matters in modify commands such as Trim, Extend, Fillet, Chamfer, Break, Join, Explode, and similar tools. Point phases and selection phases should feel different because they serve different purposes.

## Snap and zoom

Snap tolerance is visual by nature. If several snap points are very close on screen, zoom into the detail before choosing. OpenCad2D is designed so that zooming in makes it easier to distinguish nearby snap candidates.

This is especially important in small details, dense imported drawings, architectural symbols, and intersections near dimensions or blocks. The best practical rule is simple: when two points are visually too close, zoom in until the intended point is clearly separated.

## Hidden and locked layers

Entities on hidden or locked layers should not be selectable or snappable during normal editing. Hidden layers are not part of the visible working context. Locked layers are visible references but should not participate in operations that could accidentally use or change them unless a specific tool explicitly documents an exception.

## SmartPoint and extension tracking

SmartPoint tracking allows OpenCad2D to remember a point after the cursor remains over it long enough. The remembered point can then generate extension lines or intersections that help construct new geometry without creating temporary construction entities.

This is useful when aligning a new point with an existing endpoint, projecting from a corner, or finding an implied intersection between two extension directions. The activation delay is intentional: a SmartPoint should appear when the user deliberately pauses on a point, not every time the cursor passes over it.

Recommended assets include `docs/assets/gifs/snaps/snap-endpoint.gif`, `docs/assets/gifs/snaps/snap-intersection.gif`, and `docs/assets/gifs/snaps/smartpoint-extension.gif`.

## Visual assets to add

Snap documentation should use zoomed-in examples with visible markers. Recommended assets are `docs/assets/gifs/snaps/snap-endpoint.gif`, `docs/assets/gifs/snaps/snap-midpoint.gif`, `docs/assets/gifs/snaps/snap-intersection.gif`, `docs/assets/gifs/snaps/snap-nearest.gif`, and `docs/assets/gifs/snaps/smartpoint-extension.gif`.
