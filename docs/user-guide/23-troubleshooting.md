# Troubleshooting

This chapter collects common situations that can make OpenCad2D feel confusing even when the application is behaving correctly. It is written from the user's point of view and should remain practical.

## I cannot see my drawing

Use Zoom Extents first. The drawing may be outside the current view, very small compared to the current zoom level, or located far from the area currently shown on the canvas.

If Zoom Extents does not show what you expect, check the layer visibility. Hidden layers are not displayed. Also check whether the drawing contains only very small geometry or imported content placed far from the origin.

## I cannot select an object

The object may be on a locked or hidden layer. Hidden objects cannot be selected because they are not visible. Locked layers protect reference geometry from accidental edits.

If several objects overlap, zoom in and try again. Selection cycling can help when multiple selectable entities occupy the same visual area. Blocks and images may also have larger or different selectable extents than simple line geometry, so zooming in usually makes the intended selection clearer.

## Snap chooses the wrong point

Zoom into the detail. When snap candidates are visually close, the correct solution is usually to increase the visual separation between them. OpenCad2D reduces the effective snap tolerance as you zoom in, so close points become easier to distinguish.

Also check which snap modes are enabled. Grid Snap can compete with object snaps when the pointer is close to both a grid point and a geometric snap point. SmartPoint and extension tracking can also introduce useful candidates, but they should be disabled temporarily if they are not needed for the current operation.

When a modify command is asking for an entity, OpenCad2D should use entity-selection behavior rather than ordinary point snaps. If a tool appears to pick points when it should select objects, report it as a command-specific issue.

## The HUD field does not edit when I move the mouse over it

This is intentional. HUD fields should not become editable just because the pointer passes over them. This prevents accidental focus changes while the HUD follows the cursor.

Use `Tab` to activate the first editable field, `Tab` again to move to the next field, and `Enter` to confirm. Use `Esc` to cancel the active input.

## The value I typed is not used

Check which HUD field is active. If no specific field is active, a direct number is usually interpreted as the value expected by the current command, commonly a distance, radius, angle or factor. When you need to edit a specific field such as X, Y, Radius or Distance, press `Tab` until that field is active.

Some commands also reset temporary values after confirmation. For example, a distance override may apply only to the current segment or operation, depending on the command.

## Ortho or Polar does not behave as expected

Ortho constrains input to the main orthogonal directions. Polar Tracking can suggest or constrain other angles depending on the selected polar mode. If both feel restrictive, turn one of them off and repeat the operation.

For precise angle input, the HUD is often clearer than relying only on pointer direction. Type the distance and angle explicitly when the geometry must be exact.

## The Library is empty after publishing

Verify that the `Library` folder was copied beside the published executable. The Library browser depends on the object files being available at runtime. If the folder is missing, the browser may open but show no available items.

When preparing a release, the publish process must include the Library folder, not just the compiled executable files.

## An attached image is missing

The drawing stores a reference to the image file. If the image was moved, renamed or not copied with the drawing, OpenCad2D cannot display it until the reference is fixed.

Use Relink Missing or Replace Image to connect the drawing to the correct raster file. Use Collect Refs before sharing a project that depends on external images, because it copies the referenced images beside the drawing and updates the paths.

## Imported drawings create unexpected resources

When importing another drawing, OpenCad2D may need to merge layers, line formats, text formats and dimension styles. If an imported file contains resources with the same name but different properties, review the resulting managers after import.

The desired behavior is to avoid duplicates when resources are truly equivalent and to preserve differences when they are not. If an import creates duplicate resources that appear identical, it should be treated as an import-cleanup issue.

## Exported files are not editable like the original drawing

This is expected. SVG, DXF, PDF and PNG are exported outputs. The editable OpenCad2D project is the `.opencad2d.json` file.

Always keep the native file if further editing is needed. PDF and PNG are especially output-oriented formats. SVG and DXF may be editable in other applications, but they should not be treated as perfect replacements for the native project file.
