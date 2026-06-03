# Dynamic HUD

The Dynamic HUD is the command input interface displayed near the cursor. It replaces the old fixed command-line workflow and keeps the active instructions, options, and numeric fields close to the area where the user is working.

The goal of the HUD is precision without interrupting the drawing flow. You should be able to start a command, pick points with the mouse, enter exact values from the keyboard, and continue without moving attention to a distant panel.

## What the HUD shows

The HUD changes depending on the active command. It can show the current instruction, available options, and one or more input fields. The common fields are Distance, Angle, X, and Y. Some commands show command-specific fields such as Radius, Offset, Width, Height, or Number of segments.

The HUD is not a property editor. It is a temporary command input surface. Once the command step is completed, the values are applied to the current operation and the HUD moves on to the next instruction or disappears.

## Direct numeric input

During many commands, typing a number immediately fills the default numeric field. For drawing and transform operations this usually means Distance. For a command that asks for a radius, offset, angle, or segment count, the default field should match the current command step.

This allows fast workflows. For example, when drawing a line, you can click the first point, type a distance, optionally set an angle, and confirm. When moving selected objects, you can choose a base point, type a distance, and confirm the displacement.

## Tab navigation

Press `Tab` to activate the HUD fields and move forward through them. Press `Shift+Tab` to move backward. The active field is the one that receives keyboard input.

This behavior is important because HUD fields should not accidentally take focus when the mouse passes over them. OpenCad2D often keeps the HUD close to the pointer; accidental focus would make drawing unstable. For this reason, text fields become active through keyboard navigation, not through hover.

## Confirming and cancelling

Press `Enter` to confirm the current value or advance the current command step. Right click is the mouse equivalent when the current phase has enough information to continue. Press `Esc` to cancel the active command or clear the current input override, depending on the phase.

A command should not guess a missing value. If a value, point, or selection is required and not available yet, OpenCad2D should keep the command active and show a clear instruction instead of committing an unexpected operation.

## Options and highlighted letters

Some commands expose options such as Arc, Close, Undo, Radius, Distance, Trim, NoTrim, or Yes/No confirmations. The HUD should make these options visible and highlight the key letter when applicable. This lets the user work with the keyboard while still seeing the available choices.

For example, a polyline command may allow switching to arc mode, closing the polyline, or undoing the last segment. A mirror command may ask whether the original objects should be deleted after the mirrored copy is created.

## Typical examples

Line uses the HUD for distance and angle after the start point is known. Move and Copy use it to define a displacement. Rotate uses it for the rotation angle. Fillet uses it for the radius. Chamfer uses it for distances. Rectangle by Sides uses it to enter width and height. Divide uses it to enter the number of segments.

These examples should be documented with short GIFs because the HUD is best understood as a live interaction, not as a static form.

Recommended assets include `docs/assets/gifs/hud/line-distance-angle.gif`, `docs/assets/gifs/hud/move-distance.gif`, `docs/assets/gifs/hud/fillet-radius.gif`, and `docs/assets/gifs/hud/chamfer-distance.gif`.

## Visual assets to add

The HUD is one of the most important interaction concepts in OpenCad2D. Use short GIFs rather than long recordings. The first assets should be `docs/assets/gifs/hud/line-distance-angle.gif`, `docs/assets/gifs/hud/tab-through-fields.gif`, `docs/assets/gifs/hud/move-distance.gif`, `docs/assets/gifs/hud/fillet-radius.gif`, and `docs/assets/gifs/hud/chamfer-distance.gif`.
