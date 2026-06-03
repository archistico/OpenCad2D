# Align Objects

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
