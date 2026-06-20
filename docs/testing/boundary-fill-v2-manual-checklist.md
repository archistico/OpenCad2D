# Boundary Fill v2 Manual Checklist

Use this checklist before marking a build as ready after Boundary Fill changes. Automated tests cover the service and tool contracts, but these cases verify the visible preview, HUD messages, confirmation behavior and user-facing failure modes.

## Basic preview and confirmation

1. Draw a rectangle using four separate lines.
2. Run `BFILL`.
3. Click inside the rectangle.
4. Verify that a filled closed polyline appears as a preview and that the document is not modified yet.
5. Press `Enter` or right-click.
6. Verify that the filled polyline is created on the current layer and that one Undo removes it.

Repeat the same test using typed coordinates through the Dynamic HUD instead of a mouse click.

## Cancel behavior

1. Create a valid preview inside a closed boundary.
2. Press `Esc`.
3. Verify that the preview disappears and no entity is added.

## Curve boundaries

1. Draw a circle.
2. Run `BFILL` and click inside it.
3. Verify that a preview is created from sampled curve boundaries and the message mentions sampled curves.
4. Confirm and verify that the resulting fill is a closed filled polyline.

Repeat with a boundary composed of arcs and lines, such as a D-shaped profile.

## Small gap tolerance

1. Draw a rectangle where one endpoint gap is smaller than the default tolerance.
2. Run `BFILL` and click inside.
3. Verify that a preview is created and the message reports a bridged small gap.
4. Confirm and undo.
5. Run `BFILL` again, choose `Gap`, and enter a smaller value than the gap.
6. Click inside the same rectangle.
7. Verify that no preview is created and the command reports that no closed boundary was found.


## Endpoint-to-segment gap tolerance

1. Draw a boundary where one line endpoint stops close to the interior of another boundary segment, with the shortest distance below the active `Gap` tolerance.
2. Run `BFILL` and click inside.
3. Verify that a preview is created and that the existing segment is not visually moved or averaged.
4. Confirm that the preview closes by adding only the short missing connection.
5. Repeat with a smaller `Gap` value below the endpoint-to-segment distance.
6. Verify that no preview is created and the command reports that no closed boundary was found.

## Recalculate preview after Gap change

1. Create a preview inside a boundary that requires a small bridged gap.
2. Choose `Gap` while the preview is active.
3. Enter a smaller tolerance that should no longer bridge the gap.
4. Verify that the preview is cleared and the HUD/status message reports the failed boundary search.

## Ignored unsupported entities

1. Draw a valid line rectangle.
2. Add unsupported visible entities inside or near it, such as point markers, text, dimensions, images or block references.
3. Run `BFILL` and click inside the rectangle.
4. Verify that the preview is still found from the supported boundary geometry.
5. Verify that the message reports ignored unsupported entities.
6. Confirm and verify that the completion message keeps the ignored-entity diagnostic.

## Outside and unsupported-only cases

1. Run `BFILL` and click outside all closed boundaries.
2. Verify that no preview appears and no entity is added.
3. Create a drawing containing only unsupported entities.
4. Run `BFILL` and click near them.
5. Verify that the command reports that supported line, polyline, arc or circle boundaries are needed and that unsupported entities were ignored.

## Known limits to confirm

Boundary Fill v2 should not be expected to create holes/islands, hatch patterns or associative hatch objects. It should not use text, dimensions, images, block references or stair annotation graphics as boundary sources. Those limitations are intentional until the later HatchEntity milestone.
