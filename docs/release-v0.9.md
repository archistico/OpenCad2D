# OpenCad2D v0.9 — Release Notes Draft

OpenCad2D v0.9 is a stabilization and usability release. It consolidates the CAD foundation built in previous milestones, improves modify-tool predictability, adds solid fill support, introduces external raster image references, and prepares the project for a more disciplined path toward v1.0.

## Highlights

- Stabilized modify-tool confirmation rules for Enter, right click and Esc across the main draw/modify workflows.
- Improved TRIM, BREAK and EXTEND preview/status feedback with clearer no-op and failure messages.
- Added or consolidated essential edit tools including Explode, Join, object alignment and object distribution.
- Added solid fill support for circles and closed polylines, including rectangles and polygons represented as closed polylines.
- Added external PNG/JPG/JPEG image references as linked raster files, never embedded inside `.opencad2d.json`.
- Added relative image paths, missing-image warnings, Relink Missing, Replace Image, Reset Aspect, Collect Refs and Image References Manager.
- Improved snapping on raster image references: corners, border midpoints, center and nearest border points.
- Strengthened Property Panel coverage for common editable entity properties and compact polyline vertex inspection.
- Kept export behavior explicit: SVG preserves external raster links; DXF/PDF raster-image output remains deferred.

## External raster image references

This release introduces a practical raster-underlay workflow while preserving OpenCad2D's native-file philosophy.

Supported behavior:

- attach local `.png`, `.jpg` and `.jpeg` files;
- store only the file path and CAD rectangle geometry;
- render the raster on the Avalonia canvas;
- show a selectable placeholder if the file is missing;
- move, copy, rotate, scale, mirror and grip-edit the image rectangle;
- edit file path, origin, size and rotation in the Property Panel;
- replace or relink a selected image while preserving drawing geometry;
- reset the image rectangle to the natural pixel aspect ratio;
- snap to corners, side midpoints, center and nearest border point;
- save image paths relative to the drawing file whenever possible;
- collect linked rasters into an `images/` folder beside the drawing;
- manage references through `Manage Refs` with status, path, pixels, CAD size, rotation, instance count, select, relink, replace and open-folder actions.

Portable project layout:

```text
drawing.opencad2d.json
images/
  plan.png
  reference-photo.jpg
```

The native file stores references such as:

```json
"filePath": "images\\plan.png"
```

Raster bytes are intentionally not embedded in the native JSON file.

## Solid fill

Solid fill is now available for:

- `CircleEntity`;
- closed `PolylineEntity`, including rectangles and polygons.

Fill color is layer-based through `Layer.FillColor`. Stroke color, lineweight and dash style continue to resolve through the layer's reusable line format. SVG and PDF export preserve solid fill; DXF export writes targeted `SOLID` HATCH records for the supported filled entities.

Current fill limits remain documented: no transparency, no hatch pattern selection and no per-entity fill color.

## Modify-tool UX stabilization

v0.9 standardizes the interaction contract for command-driven tools:

- left click provides graphical input or entity selection;
- right click and Enter confirm only when a valid value, default or selection exists;
- Esc cancels the active phase;
- selection phases use EntityOnly snapping;
- point phases use the active geometric snap set;
- invalid confirmations must show clear messages instead of guessing.

Offset, Fillet, Mirror, Polygon, Polyline, Delete, TRIM, BREAK and EXTEND were reviewed under this policy.

## Curve editing feedback

TRIM, BREAK POINT, BREAK SEGMENT and EXTEND now provide more specific failure and no-preview messages. Invalid hover feedback is aligned with commit-click feedback so the user sees why an edit is unavailable before committing.

The remaining manual regression work should be tracked with the v0.9 curve-editing regression sheet before final tagging.

## Property Panel and managers

The Property Panel now exposes clearer editable/read-only fields for the current primary workflow, including:

- layer combo selection;
- dimension style combo selection;
- closed polyline state;
- compact polyline vertex display;
- image reference file, origin, size, rotation and natural aspect metadata.

The application also includes dedicated manager windows for layers, line formats, text formats, dimension styles and image references.

## Export/import notes

Current expected behavior:

- SVG export preserves layer stroke appearance, solid fill and external raster image links;
- PDF export preserves vector geometry and supported solid fill, but does not emit raster image references yet;
- DXF export preserves vector geometry and supported solid fill HATCH output, but raster IMAGE/IMAGEDEF output is deferred;
- native `.opencad2d.json` remains the source-of-truth editing format.

DXF raster-image parity is intentionally left for a future compatibility pass because it requires IMAGE/IMAGEDEF object support and real viewer validation.

## Testing focus before release

Before tagging v0.9, run:

```powershell
dotnet clean
dotnet restore
dotnet build
dotnet test
make run
```

Manual smoke tests should cover:

- save/reopen with mixed geometry;
- SVG/PDF/DXF export of a mixed drawing;
- TRIM/BREAK/EXTEND regression sample;
- solid fill persistence/export;
- external image attach, transform, snap, relink, collect and manager workflows;
- missing-image warning and recovery.

## Known limitations

- DWG is not supported.
- Binary DXF is not supported.
- Blocks, INSERT workflows and external CAD XREFs are not supported yet.
- General hatch/pattern editing is not implemented; only targeted solid fill is supported.
- Native DXF DIMENSION import/export remains future work; dimensions export as graphical primitives where supported.
- External raster images export to SVG as linked files only; DXF/PDF raster-image output is deferred.
- Dimensions remain non-associative, although stale-state marking reduces silent-risk workflows.
- Full NURBS fidelity for imported DXF SPLINE data is not guaranteed yet.
- Autosave/recovery v2 and installer/package polish remain future work.

## Suggested Git tag

```text
v0.9.0
```

## Suggested GitHub release title

```text
OpenCad2D v0.9.0 — Stabilization, solid fill and external image references
```
