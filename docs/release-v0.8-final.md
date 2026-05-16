# OpenCad2D v0.8 — Final Release Notes

OpenCad2D v0.8 is a major stabilization and feature release focused on completing the core 2D drawing set, improving modify tools, strengthening DXF interoperability, and preparing the project for a more stable public release cycle.

## Highlights

- New Ellipse drawing workflow with rendering, persistence, export, snap/grip support and modify-tool compatibility.
- New multiline text workflow through `MTEXT` / `MT`, including multiline dialog, persistence, SVG/PDF/DXF export and DXF MTEXT import.
- New Bezier-based `SPLINE` workflow with control points, `Undo`, `Close`, preview, rendering, persistence and export.
- Trim, Break and Offset now work on the newly introduced curved entities through controlled polyline approximation where needed.
- Fillet now has live preview and supports `Trim` / `NoTrim` mode.
- Offset now limits excessive miter spikes on sharp polyline turns.
- Command input now supports command history navigation with Up/Down and first-pass autocomplete with Tab.
- Non-associative dimensions can now be marked as potentially stale after geometry-changing operations.
- DXF import/export compatibility has been significantly improved and documented.
- Initial architectural cleanup reduced duplication in `MainWindow` and split entity/tool-preview rendering out of `CadCanvas`.

## New drawing entities and tools

### Ellipse

The new `ELLIPSE` / `EL` tool supports:

- center point;
- major axis definition;
- minor radius definition;
- canvas rendering and preview;
- SVG, PDF and DXF export;
- JSON persistence;
- snap and grip integration;
- Trim/Break support through approximation where required.

### Multiline text

The new `MTEXT` / `MT` workflow supports:

- insertion point;
- multiline dialog;
- canvas rendering with multiple lines;
- SVG `<text>/<tspan>` export;
- PDF multiline export;
- DXF `MTEXT` export/import;
- JSON persistence;
- text format integration.

### Spline

The new `SPLINE` / `SPL` workflow introduces a Bezier-based spline entity:

- control point input;
- `Undo` / `U` while drawing;
- `Close` / `C` while drawing;
- canvas preview and rendering;
- SVG, PDF and DXF export;
- JSON persistence;
- Trim/Break/Offset support via polyline approximation.

## Modify tools improvements

### Trim and Break

Trim and Break were consolidated across:

- open polylines;
- closed polylines and polygons;
- ellipses;
- Bezier splines;
- approximated curved entities where a native partial entity is not yet available.

When a modified result cannot currently be represented by the original native entity type, OpenCad2D returns a `PolylineEntity` approximation. This is intentional for v0.8 and avoids introducing unstable partial-ellipse or partial-spline entity models before they are mature.

### Fillet

Fillet now includes:

- live preview while selecting the second line;
- `Trim` mode, preserving the existing behavior;
- `NoTrim` mode, which adds only the tangent arc while keeping original lines intact;
- safer rejection of near-degenerate or almost-collinear cases.

### Offset

Offset now includes a conservative miter limit. Normal miter joins are preserved, but very sharp turns fall back to a bevel-style join to avoid excessive spikes.

## Command line UX

The command input now supports:

- Up/Down command history navigation;
- Tab autocomplete for known commands, aliases and action commands;
- filtering so coordinates, distances and tool option values are not treated as global commands.

## Dimension safety

Dimensions remain non-associative in v0.8, but geometry-changing operations can now mark dimensions as potentially stale.

The property panel displays the dimension status, and stale dimensions are visually distinguished in the canvas. This provides a safer workflow until fully associative dimensions are implemented in a later release.

## DXF interoperability

DXF support was improved in several areas:

- `LWPOLYLINE` bulge import now preserves curved geometry by converting bulge segments to line/arc entities instead of flattening them into straight segments.
- full DXF `ELLIPSE` entities import as `EllipseEntity`;
- partial DXF ellipses import as open polyline approximations;
- readable DXF `SPLINE` control-point data imports as `BezierSplineEntity`;
- fit-point-only splines import as polyline approximations;
- `MTEXT` import/export is supported;
- compatibility sample files were added under `samples/dxf/compatibility/`.

Manual validation for the v0.8 DXF compatibility samples was completed successfully before release.

## Architecture and stabilization

This release starts the v0.9 stabilization path while still closing the v0.8 feature set:

- `MainWindow` document-change refresh logic was centralized;
- entity rendering was extracted from `CadCanvas` into `CadEntityRenderer`;
- active-tool preview rendering was extracted into `CadToolPreviewRenderer`;
- active-tool keyboard behavior was delegated through a dedicated interface;
- pointer handling and text-dialog reentrancy were made safer.

## Testing

The release expands coverage with:

- end-to-end save/reopen tests;
- end-to-end draw/annotate/export tests;
- DXF import/modify/export tests;
- focused tests for Ellipse, MTEXT and SPLINE;
- Trim/Break/Offset regression tests;
- command history and autocomplete tests;
- dimension stale persistence and command integration tests;
- DXF bulge/ellipse/spline import tests.

## Known limitations

The following limitations remain intentionally documented for v0.8:

- DWG is not supported.
- Binary DXF is not supported.
- `BLOCK` / `INSERT` are not yet supported.
- `HATCH`, `IMAGE` and `LEADER` are not yet supported.
- Native DXF `DIMENSION` import/export is not yet implemented; dimensions are exported as drawable geometry where applicable.
- Dimensions are still non-associative, although stale marking now reduces silent-risk workflows.
- Partial DXF ellipses are approximated as polylines.
- Full NURBS fidelity for external DXF SPLINE data is not guaranteed yet.
- PNG export is not yet implemented.
- Autosave/recovery is not yet implemented.

## Suggested Git tag

```text
v0.8.0
```

## Suggested GitHub release title

```text
OpenCad2D v0.8.0 — Ellipse, MTEXT, SPLINE and DXF interoperability
```
