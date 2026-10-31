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

Navigation wraps at the first and last editable fields. It stores the current number without completing the command step. A malformed number or an out-of-range integer keeps the current field active so you can correct it. An untouched field remains a live measurement, rather than becoming an explicit value merely because you tabbed through it.

This behavior is important because HUD fields should not accidentally take focus when the mouse passes over them. OpenCad2D often keeps the HUD close to the pointer; accidental focus would make drawing unstable. For this reason, text fields become active through keyboard navigation, not through hover.

## Confirming and cancelling

Press `Enter` to confirm the current value or advance the current command step. Right click is the mouse equivalent when the current phase has enough information to continue. Press `Esc` to cancel the active command or clear the current input override, depending on the phase.

A command should not guess a missing value. If a value, point, or selection is required and not available yet, OpenCad2D should keep the command active and show a clear instruction instead of committing an unexpected operation.

Validation errors appear inside the HUD and preserve your draft. Correct the value and confirm again. A rejected value never submits an older value in its place. When setting absolute coordinates, enter both X and Y; when entering rectangle dimensions, enter both Width and Height. An untouched Enter uses a default only when the current prompt provides one.

Clicking the canvas while a typed measurement is pending confirms that exact input through the same validation rules. Without pending typed input, canvas clicks continue to pick points normally.

Moving the mouse while editing keeps the field, text and caret in place. Backspace or Delete edits the active field; clearing one field leaves the other entered measurements available. Esc cancels the active command and clears its temporary values.

## Decimal values and coordinate expressions

Inside a field activated with Tab, either `12.5` or `12,5` means twelve and a half. When typing directly on the canvas during a point prompt, the comma separates coordinates: `100,50` means X=100, Y=50. Use a dot for a direct decimal distance in these prompts.

Canvas input also accepts relative coordinates such as `@10,5` and polar expressions such as `10<45` or `@10<45`. These forms work when typed progressively or pasted together. Numeric options that do not ask for a point accept scalar decimal input.

## Options and highlighted letters

Some commands expose options such as Arc, Close, Undo, Radius, Distance, Trim, NoTrim, or Yes/No confirmations. The HUD should make these options visible and highlight the key letter when applicable. This lets the user work with the keyboard while still seeing the available choices.

For example, a polyline command may allow switching to arc mode, closing the polyline, or undoing the last segment. A mirror command may ask whether the original objects should be deleted after the mirrored copy is created.

Option shortcuts also work while a numeric field is active. Choosing an option discards the unfinished numeric draft and starts the requested mode or sub-prompt. Generic numeric sub-prompts show a Value field; the prompt identifies the parameter being edited.

## Typical examples

Line uses the HUD for distance and angle after the start point is known. Move and Copy use it to define a displacement. Rotate uses it for the rotation angle. Fillet uses it for the radius. Chamfer uses it for distances. Rectangle by Sides uses it to enter width and height. Divide uses it to enter the number of segments.

These examples should be documented with short GIFs because the HUD is best understood as a live interaction, not as a static form.

Recommended assets include `docs/assets/gifs/hud/line-distance-angle.gif`, `docs/assets/gifs/hud/move-distance.gif`, `docs/assets/gifs/hud/fillet-radius.gif`, and `docs/assets/gifs/hud/chamfer-distance.gif`.

## Visual assets to add

The HUD is one of the most important interaction concepts in OpenCad2D. Use short GIFs rather than long recordings. The first assets should be `docs/assets/gifs/hud/line-distance-angle.gif`, `docs/assets/gifs/hud/tab-through-fields.gif`, `docs/assets/gifs/hud/move-distance.gif`, `docs/assets/gifs/hud/fillet-radius.gif`, and `docs/assets/gifs/hud/chamfer-distance.gif`.
