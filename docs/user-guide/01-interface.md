# Interface

The OpenCad2D interface is centered on the drawing canvas. The canvas is where geometry is created, selected, edited, measured, and reviewed. Around it, the application exposes toolbars, command groups, snap controls, the Dynamic HUD, the Properties Panel, and status information.

The goal of the interface is to keep the drawing area dominant while still making the most common CAD operations immediately available. Commands can be started from tool buttons, keyboard shortcuts, aliases, or contextual workflows depending on the feature.

Recommended visual asset: `docs/assets/images/interface/main-window-overview.png`.

## Canvas

The canvas is the active drawing surface. It displays native CAD entities, image references, blocks, dimensions, library objects, fills, construction geometry, and selection grips. Most commands ask you to pick points or entities directly on the canvas.

Navigation is handled with the mouse: the middle mouse button pans the view, the wheel zooms, and dedicated commands such as Zoom Window and Zoom Extents help move quickly between details and the full drawing. Canvas navigation is described in detail in [Canvas Navigation](02-canvas-navigation.md).

## Top toolbar and command areas

The toolbar exposes the main command groups: file actions, import/export, drawing tools, edit tools, dimensions, measuring, images, layers, library access, and related utilities. Tool buttons are useful for discovery and for commands that are not used often enough to memorize.

For repeated work, keyboard aliases and the Dynamic HUD usually become faster than moving through the toolbar. The guide does not assume one single workflow: a user can work mostly with the mouse at first and gradually adopt shortcuts as needed.

Recommended visual asset: `docs/assets/images/interface/toolbar-groups.png`.

## Left command bar

The left command bar groups the main drawing and editing tools. Its purpose is to keep frequently used commands visible without hiding the canvas. Depending on the current version, buttons may be grouped by drawing, selection, dimension, edit, measure, or alignment workflows.

When documenting a command, this guide focuses on what the command does and how it behaves rather than on the exact visual position of its button, because the toolbar layout can evolve during development.

## Snap and constraint controls

Snap, Ortho, Grid, and Polar controls affect how points are chosen while drawing or editing. These controls are not isolated tools; they change the behavior of many commands. For example, Line, Move, Copy, Rectangle, Dimension, and Insert Library Item all become more precise when the correct snap or constraint is active.

The detailed behavior is documented in [Snaps](14-snaps.md) and [Ortho, Grid, and Polar Tracking](15-ortho-grid-polar.md).

Recommended visual asset: `docs/assets/images/snaps/snap-controls-overview.png`.

## Dynamic HUD

The Dynamic HUD appears near the cursor during commands. It shows command prompts, options, and numeric input fields such as Distance, Angle, X, Y, Radius, or command-specific values. It replaces the need for a large command line while keeping input close to the drawing action.

The HUD is intentionally controlled by keyboard focus rules. Text fields should not accidentally activate just because the mouse passes over them. Use `TAB` to enter and move through HUD fields, `ENTER` to confirm values, and `ESC` to cancel an input or command step.

Recommended visual asset: `docs/assets/images/hud/dynamic-hud-overview.png`.

## Properties Panel

The Properties Panel displays information about the current selection and allows editable properties to be changed. With no selection, it may show document or default drawing properties. With one selected entity, it shows specific properties for that entity. With multiple selected entities, it shows common editable properties where possible.

This panel is the main place to adjust layer, color, line format, text format, dimension style, draw order, image properties, and other entity-level values after creation. See [Properties Panel](12-properties-panel.md) for details.

Recommended visual asset: `docs/assets/images/interface/properties-panel.png`.

## Status area

The status area gives feedback about the current command, coordinates, active modes, and drawing state. It should help the user understand what OpenCad2D is waiting for: a point, an entity, a numeric value, a selection, or a command confirmation.

When a tool behaves unexpectedly, the status area and the HUD are the first places to look. They usually explain whether the application is waiting for a point, expecting an entity selection, or asking for an option.

## Modal windows and managers

Some workflows require more structured input than the HUD can reasonably provide. In those cases, OpenCad2D uses windows or managers, such as the Library browser, Dimension Style manager, image reference manager, or block insertion options.

These windows are used when the operation needs browsing, preview, multiple settings, or a persistent list. The HUD remains better suited for fast command input directly on the canvas.
