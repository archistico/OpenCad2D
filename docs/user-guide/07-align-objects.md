# Align Objects

## ALIGN by reference points

`ALIGN` moves and rotates objects using two source/destination point pairs, with optional uniform scaling. It is separate from the bounding-box alignment buttons described below.

Start ALIGN and select objects by click or drag a selection window. Shift toggles selection; Ctrl cycles overlapping objects, using the same rules as Selection. Press Enter/right click to confirm the objects. If editable objects were already selected before starting ALIGN, the command goes directly to the first reference point. Hidden objects and objects on locked layers cannot be aligned.

The HUD guides five steps:

1. Pick the **first source point** on the original objects.
2. Pick its **first destination point**. Green previews the translation without committing it.
3. Pick the **second source point** to define the original direction.
4. Pick the **second destination point** to define the new direction. Green previews the alignment without scaling; reference markers and guide lines show the source/destination pairs.
5. Choose **Yes/Y** to scale uniformly and match both pairs, or **No/N**, Enter or right click to preserve the original size. Without scaling, the first pair coincides and the second pair sets the direction; different reference lengths do not change object size. Confirmation applies one undoable operation and returns to Selection.

Use geometric snaps or HUD X/Y fields for references. Relative coordinates for the first destination and second source use the first source as their base; the second destination uses the first destination. Coordinates are resolved in the current UCS before reaching the tool.

`B` / `Back` clears the last reference and returns to that step. `O` / `Objects` restarts object selection and clears the references. Esc exits without applying the preview. Coincident points are rejected with feedback inside the HUD. If selected objects change, disappear or become non-editable, the old preview is cleared and ALIGN asks you to review the selection and choose references again.

## Bounding-box alignment and distribution

Align tools reposition selected objects relative to each other. They are useful when a drawing contains repeated elements that must be visually organized, such as symbols, blocks, furniture, annotations, images or text labels.

The tools normally work from the bounding boxes of the selected objects. This means OpenCad2D considers the visual extent of each selected entity and aligns those extents, rather than aligning only insertion points or geometric origins.

## Alignment tools

Align Left moves the selected objects so their left edges match. Align Right does the same with the right edges. Align Top and Align Bottom align the upper or lower edges. Horizontal and vertical center alignment place selected objects on the same center line.

Distribution tools are different. They do not simply move everything to one edge. They spread selected objects evenly across a horizontal or vertical direction, preserving a regular spacing between their centers or extents according to the tool behavior.

## Selection workflow

Select the objects first, then choose the alignment command. This is the safest workflow because it makes the operation explicit: you can see the selected set before OpenCad2D moves anything.

Alignment is most useful with more than one selected object. Distribution requires multiple objects and is meaningful only when there is enough space between the outer items to distribute the intermediate ones.

## Practical use

Use Align tools to clean up symbols, arrange annotations, line up blocks or organize imported library objects. They are also useful after inserting several objects manually, because small placement differences are easy to correct in one operation.

When aligning objects of very different sizes, check the result visually. Since the operation is based on bounding boxes, a large object and a small object may align correctly but still look visually unbalanced depending on the drawing context.
