# Images

Image tools manage external raster image references. Images are useful as tracing backgrounds, scanned plans, site references, logos, or visual context, but they are not native CAD geometry.

A drawing stores a reference to the image file. The image file itself must remain available on disk, or the reference must be relinked later.

## Attach Image

Attach Image inserts a raster image reference into the drawing. The user chooses an image file and places it on the canvas.

The image can then be moved, scaled, aligned, or used as a visual reference. When tracing over a scan or plan, place the image on a dedicated layer so it can be locked, hidden, or adjusted without disturbing the drawing geometry.

## Replace Image

Replace Image changes the file used by an existing image reference. The placed reference remains in the drawing, but the raster source is swapped.

Use this when a better scan, corrected image, or updated reference file should replace the previous one without recreating the placement from scratch.

## Relink Missing

Relink Missing is used when the drawing cannot find an image file. This can happen after moving the project folder, renaming files, sharing a drawing without its images, or opening the drawing on another computer.

Relinking connects the existing image reference to the correct file path again.

## Reset Aspect

Reset Aspect restores the image proportions. Use it when an image has been stretched accidentally or when the displayed width and height no longer match the original raster aspect ratio.

This is especially useful for scans and plans, where distorted proportions can cause tracing and measurement errors.

## Collect Refs

Collect Refs gathers referenced image files into a predictable location beside the drawing or inside a project structure. This makes the drawing easier to share and archive.

Use Collect Refs before sending a project to someone else or before preparing a release package that includes sample drawings with images.

## Manage Refs

Manage Refs lists the image references used by the drawing and provides maintenance actions such as checking paths, replacing files, relinking missing images, or adjusting reference-related settings.

When image transparency is implemented, it should be documented here as a percentage-based display property managed from the image reference tools.

## Snaps on images

Image references may expose snap points such as corners or edges. This helps position geometry relative to the raster reference, but it does not convert the image into editable vector geometry.

For precise CAD work, use the image as a guide and create real OpenCad2D entities over it.

## Visual assets to add

Image reference workflows are easier to understand with screenshots. Add `docs/assets/gifs/images/attach-image.gif`, `docs/assets/images/images/manage-refs-window.png`, `docs/assets/gifs/images/relink-missing-image.gif`, and `docs/assets/gifs/images/reset-aspect.gif` when the captures are available.
