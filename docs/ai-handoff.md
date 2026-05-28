# OpenCad2D roadmap

This roadmap tracks the active development path from the current v0.9 stabilization work toward the first stable v1.0 release.

OpenCad2D grows in small, testable phases. Each phase should compile, pass the relevant tests, update documentation and leave a clear handoff before the next phase begins.

Legend:

```text
[x] completed and stabilized
[~] in progress / partially stabilized
[ ] planned
[>] deferred beyond the current release target
```

---

## Current release target: v0.9 stabilization

v0.9 is a stabilization release. Its goal is not to add a large new feature group, but to make the current CAD foundation predictable, precise and safe enough to move toward v1.0.

Primary v0.9 themes:

- native curve editing precision;
- predictable modify-tool UX;
- reliable save/export behavior;
- DXF/SVG/PDF compatibility checks;
- documented limitations;
- clean release packaging.

---

## Completed foundations

The following foundations are considered complete for the active roadmap. Older implementation details are intentionally not repeated here; see Git history and release notes for historical milestones.

| Area | Status | Notes |
|---|---:|---|
| Core geometry/document model | [x] | Geometry primitives, entities, layers, line formats, text formats, dimension styles, command history and undo/redo are in place. |
| Application shell | [x] | Avalonia canvas, file command bar, top CAD bar, left tool panel, property panel, command row, snap bar and status bar are established. |
| Native persistence | [x] | `.opencad2d.json` save/load, dirty state, save-changes prompt, partial recovery, viewport/document settings persistence and layer/entity fill persistence are implemented. |
| Export/import baseline | [x] | SVG, PDF and DXF export exist; SVG/PDF/DXF include solid fill output for supported closed entities; ASCII DXF import covers the practical 2D entity set currently supported. |
| Command input | [x] | Aliases, prompt phases, coordinate input, relative/polar input, direct distances, history and first-pass autocomplete are implemented. |
| Drafting aids | [x] | Snap system, grid, Ortho, Polar Tracking, Zoom Window, Zoom Extents, pan and crosshair are implemented. |
| Draw tools baseline | [x] | Points, text, MTEXT, lines, rectangles, circles, arcs, ellipses, polylines, polygons and open Bezier splines are supported. Rectangles and polygons are closed polylines for fill/editing purposes. |
| Dimensions baseline | [x] | Horizontal, vertical, aligned, radius, diameter and angular dimensions exist, with conservative stale marking after model edits. |
| Transform tools | [x] | Move, Copy, Rotate, Scale, Mirror and point-based Align are usable and tested. |
| Selection and hit testing | [x] | Selection, Select All, Select Last, Deselect, entity cycling, text/MTEXT bounding-box hit testing and locked/hidden layer behavior are implemented. |
| Native curve editing | [x] | TRIM, BREAK and supported EXTEND flows use native parameters, shared cut points and adapter-backed splitting for supported curves. |
| Elliptical arcs | [x] | `EllipticalArcEntity` exists with rendering, snapping, persistence and SVG/PDF/DXF export support. |
| Open Bezier split | [x] | Open Bezier splines can be split/extracted natively and are no longer permanently degraded to polylines in TRIM/BREAK. |
| Preview UX base | [x] | TRIM/BREAK removal previews are dashed; EXTEND addition previews are highlighted; selected boundaries stay visible. |
| Save/export UX clarity | [x] | Export creates derived files and does not clear dirty state or replace the current native file path; user messages make this explicit. |
| Modify-tool confirmation policy | [x] | Right click/Enter confirmation, EntityOnly selection phases and clean transient-state reset are established for supported prompts and command phases. |
| Explode / Join essentials | [x] | EXPLODE converts selected polylines into lines/arcs and block references into world-space entities; JOIN converts connected selected lines/arcs/open polylines into bulge-capable polylines, with command aliases, buttons, undo and targeted tests. |

---

## Active v0.9 work

### 1. Modify Tools UX cleanup

Status: [x] completed for current v0.9 UX cleanup scope.

Completed:

- [x] documented the shared confirmation policy: left click for graphical input, right click/Enter to confirm valid defaults or current selections, Esc to cancel;
- [x] added Deselect command/button and refreshed the Point icon;
- [x] made Text and MTEXT selectable from their bounding boxes;
- [x] fixed Delete so it deletes the existing selection immediately, or allows multi-pick selection followed by Enter/right click;
- [x] added persistent boundary/first-entity highlights for TRIM, EXTEND and FILLET;
- [x] made FILLET entity selection use EntityOnly snapping;
- [x] allowed right click to finish POLYLINE when enough vertices exist;
- [x] allowed right click/Enter defaults for Polygon sides, Fillet radius and Mirror delete-source prompt;
- [x] fixed Ellipse axis input to use snap-resolved points;
- [x] fixed Rect Sides numeric second-side input so typed values create the exact requested side length.
- [x] completed final pass for right-click/Enter messaging, selection-phase EntityOnly snap policy and state-cleanup expectations across the remaining primary tools.
- [x] added essential Explode and Join tools before v0.9 planning: `EXPLODE`/`X` and `JOIN`/`J`.

Completed Offset cleanup:

- [x] rebuilt Offset workflow around typed distance, two-point distance, last distance and right-click/Enter default confirmation;
- [x] added Offset target highlight and addition preview;
- [x] made Offset geometry support explicit: lines, circles, arcs and straight-segment polylines are supported; ellipses, elliptical arcs and Bezier splines are deferred with clear messages;
- [x] ran the final pass over remaining tools for consistent right-click/Enter/Esc behavior and phase-specific snap modes;
- [x] updated command/tool docs after final UX consistency pass.

### 2. Offset stabilization

Status: [x] completed for v0.9 scope.

Completed behavior:

- [x] first use requires a typed distance or two picked distance points;
- [x] typed distance stores the last offset distance;
- [x] two picked points measure and store the last offset distance;
- [x] right click/Enter uses the last stored distance when one exists;
- [x] target selection uses EntityOnly snap;
- [x] side selection uses graphical input;
- [x] preview clearly shows the offset result before confirmation;
- [x] supported geometry and limitations are documented.

Geometry policy:

- line/circle/arc/polyline offset is supported and tested;
- ellipse, elliptical arc and Bezier spline offset is deferred because their true offsets are not the same native curve type;
- unsupported advanced curves return clear messages and create no silent permanent polyline approximation.


### 3. Solid fill for closed entities

Status: [x] completed for the current solid-fill scope.

Completed:

- [x] added `Layer.FillColor` as the layer-owned fill color;
- [x] added `IsFilled` to `CircleEntity` and `PolylineEntity`;
- [x] preserved fill state across entity replacement, layer changes and transforms;
- [x] persisted layer fill color and entity fill state in `.opencad2d.json`;
- [x] rendered solid fill on the canvas for filled circles and closed polylines;
- [x] exposed `Fill: None/Solid` in the Property Panel for circles and closed polylines;
- [x] exposed layer fill color in the Layer Manager with a color picker and `#RRGGBB` text field;
- [x] exported solid fill to SVG and PDF;
- [x] exported solid fill to DXF as targeted `SOLID` HATCH records for filled circles and closed polylines.

Current limits:

- no transparency;
- no hatch/pattern selection;
- no per-entity fill color;
- open polylines never render/export fill;
- general editable hatch workflows remain future work.

### 4. Curve editing regression checklist

Status: [~] manual validation in progress. Status-message and preview-feedback regression checks are now covered by tests and included in the evening run sheet.

Reference: `docs/testing/curve-editing-regression-v0.9.md`. The focused evening run sheet is `docs/testing/curve-editing-evening-run-2026-05-21.md`; the prepared sample drawing is `docs/testing/samples/curve-editing-regression-v0.9.opencad2d.json`.

Validate:

- [ ] TRIM on lines, circles, arcs, polylines, ellipses, elliptical arcs and open Bezier splines;
- [ ] BREAK AT POINT and BREAK SEGMENT on supported entities;
- [ ] EXTEND on supported targets/boundaries;
- [ ] shared endpoints/no micro-gaps after reciprocal edits;
- [ ] persistence/export of edited elliptical arcs and spline fragments;
- [x] granular command failure messages for TRIM / BREAK / EXTEND;
- [x] preview-feedback consistency for TRIM / BREAK / EXTEND invalid hover cases is covered by tests; manual regression still has to validate the full geometry results.

### 5. Export/import compatibility pass

Status: [ ] planned.

Tasks:

- [ ] save/reopen edited drawings containing `EllipticalArcEntity` and open spline fragments;
- [ ] export mixed drawings to SVG/PDF/DXF, including filled circles and filled closed polylines;
- [ ] manually open DXF samples in LibreCAD and QCAD, including generated SOLID HATCH records;
- [ ] record viewer versions, OS, date and pass/partial/fail notes;
- [ ] decide whether DXF partial ELLIPSE import should map directly to `EllipticalArcEntity` in v0.9 or remain deferred.

### 6. Property Panel curve review

Status: [ ] planned.

Check that the Property Panel exposes coherent editable/read-only properties for:

- [ ] Arc;
- [ ] Ellipse;
- [ ] EllipticalArc;
- [ ] Polyline;
- [ ] BezierSpline;
- [ ] Text and MTEXT after bounding-box hit-test changes.

### 7. Performance and robustness pass

Status: [ ] planned.

Review:

- [ ] snap/hit testing on denser drawings;
- [ ] TRIM/BREAK/EXTEND with multiple boundaries;
- [ ] preview performance;
- [ ] degenerate geometry handling;
- [ ] tolerance deduplication around intersections.

### 8. Documentation and release gate

Status: [ ] planned.

Before tagging v0.9:

- [ ] update README current status;
- [ ] update commands/tools docs after Offset and final UX pass;
- [ ] update known limitations;
- [ ] update release notes;
- [ ] run full build/test locally;
- [ ] run `make zip` and inspect archive contents;
- [ ] prepare GitHub release notes.

---

## Property Panel final cleanup checkpoint

Completed:

- [x] `Layer id` is exposed as a combo box populated from document layer ids.
- [x] `Dimension style` is exposed as a combo box populated from document dimension styles.
- [x] Polyline `Closed` is exposed as a `Yes`/`No` combo box.
- [x] Polyline vertices are shown in a compact `Vertices` section.
- [x] Polyline vertex rows use a single editable `X, Y` value instead of separate X/Y fields.
- [x] The polyline vertex list is capped to the first 4 displayed vertices.
- [x] A `More vertices` row reports hidden vertices to keep the Property Panel responsive.
- [x] Per-vertex insert/delete rows were removed from the compact Property Panel section to avoid UI weight and confusion.

Deferred / future UI polish:

- [>] Dedicated vertex editor dialog/table for larger polylines.
- [>] Insert/delete/reorder vertex actions in a dedicated UI instead of the compact Property Panel list.
- [>] Broader enum/boolean audit for future entity properties.


## Deferred beyond the active v0.9 scope

These are valid future tasks but should not block the current stabilization flow unless they become critical bugs.

- [>] closed Bezier spline splitting/editing policy;
- [>] Break Point convention for full circles/full ellipses as almost-full open arcs;
- [>] true associative dimensions;
- [>] blocks;
- [>] general hatch/pattern tools beyond the current solid fill support;
- [>] raster references;
- [>] advanced NURBS fidelity;
- [>] autosave/recovery v2;
- [>] major renderer rewrite;
- [>] broad spatial-index rewrite unless performance testing proves it necessary;
- [>] installer/package polish;
- [>] complete user manual for v1.0.

---

## v1.0 candidate focus

After v0.9 stabilizes, v1.0 should focus on finishing the professional baseline rather than adding many new entity families.

Candidate v1.0 gates:

- [x] Offset workflow and documented offset limitations are stable;
- [ ] DXF import/export compatibility pass is recorded;
- [x] Property Panel coverage is coherent for the current primary entity/property set;
- [ ] command UX is consistent across major draw/modify tools;
- [ ] user-facing documentation is complete enough for first external users;
- [ ] release artifact and versioning workflow are repeatable.


## Dimension Style System checkpoint

- [x] Document-level current dimension style.
- [x] Dimension style persistence for prefix/suffix, symbols, rotation mode and offsets.
- [x] Center-anchored dimension text rendering/export.
- [x] First Dimension Style Manager UI near Layer/Line/Text Formats.
- [x] Live graphical preview in the Dimension Style Manager.
- [x] Property panel dimension style selector for selected dimensions.
- [x] Text/terminator fit controls are implemented; tolerances and alternate units remain future work.
## v0.9 curve-editing regression support

- [x] Add granular command status messages for complex curve-editing failures.
  - TRIM now distinguishes missing intersections from picked-side interval failures.
  - BREAK POINT now explains endpoint/vertex/tolerance split failures.
  - BREAK SEGMENT now explains coincident points, off-entity second points and unsupported closed spline cases.
  - EXTEND now distinguishes no projected boundary intersection from wrong endpoint-side selection.

## 2026-05-21 — Curve-editing preview descriptor follow-up

Break Point now implements `IToolPreviewDescriptorProvider`. Its descriptor keeps the selected target visible as an Emphasis overlay and shows a Hot marker at the projected native break point when a valid preview exists.

Break Segment descriptors now also keep the selected target visible as an Emphasis overlay. They show a Primary marker at the first break point and a Hot marker at the projected second break point when a removable interval preview exists.

This aligns BREAK previews with the existing TRIM/EXTEND rule: selected context geometry remains visible while the operation-specific preview uses semantic highlighting/markers. Next manual regression should include the new `Passata 0.5 — Preview visiva comune` checks in `docs/testing/curve-editing-evening-run-2026-05-21.md`.


## 2026-05-21 — TRIM/EXTEND preview target markers

TRIM and EXTEND preview descriptors now keep the hovered target visible as an `Emphasis` overlay when a valid preview exists. TRIM also emits a Hot marker on the picked side that would be removed; EXTEND emits a Hot marker on the picked endpoint side that would be extended. This completes the visual context rule for the manual curve-editing regression pass: boundary/target context remains visible, and the operation-specific interval is still rendered with Removal or Addition semantics.


## 2026-05-21 — TRIM/EXTEND no-preview status messages

TRIM and EXTEND now reuse the granular curve-editing status-message logic during hover, not only after a failed commit click. When the pointer is over a target but no preview can be built, TRIM reports the same missing-intersection/picked-side/unsupported/non-editable reasons that the commit would report. EXTEND does the same for no projected boundary intersection, wrong endpoint side, unsupported closed targets and non-editable targets.

The evening regression sheet now includes `PREVIEW-TRIM-03` and `PREVIEW-EXT-03` so manual testing checks both valid previews and clear no-preview feedback.


### 2026-05-21 - Curve editing BREAK hover status follow-up

- BREAK POINT invalid hover positions now reuse `EditingStatusMessageBuilder.BuildBreakAtPointFailureMessage(...)` instead of the generic “inside target entity” message.
- BREAK SEGMENT invalid second-point hover positions now reuse `EditingStatusMessageBuilder.BuildBreakBetweenPointsFailureMessage(...)` instead of the generic “different and on target entity” message.
- Regression docs now require invalid BREAK previews to report the same reason as the commit click.

## 2026-05-21 — Curve editing status/preview consolidation

The granular status-message pass is test-green for TRIM, BREAK POINT, BREAK SEGMENT and EXTEND. Invalid hover/no-preview feedback now mirrors commit-click failure reasons across the four tools. The last fixes covered two regressions: BREAK POINT hover directly on a line endpoint now reports the endpoint/tolerance message instead of the generic inside-target message, and BREAK SEGMENT hover with coincident second point now reports the distinct-points message.

Next manual work should resume from `docs/testing/curve-editing-evening-run-2026-05-21.md`, starting with Passata 0 and Passata 0.5 only as quick smoke checks, then moving to real geometry validation: TRIM base, polylines/advanced curves, BREAK and EXTEND/micro-gap checks.

## 2026-05-25 — External raster image references

Implemented the first raster-reference foundation for PNG/JPG files.

- Added `ImageReferenceEntity` in Core. It stores only an external file path plus oriented-rectangle geometry: origin, width vector and height vector. The image bytes are not embedded in the `.opencad2d.json` document.
- Image references participate in the normal entity pipeline: bounding box, hit testing, closest point, layer visibility/locking, selection, move/copy/rotate/scale/mirror transforms and draw order.
- Added JSON persistence through `ImageReferenceEntityDto` and the polymorphic entity converter.
- Added Avalonia rendering with a small bitmap cache. If the external file is missing/unreadable, the rectangle is still drawn with a diagonal placeholder so the reference remains selectable and recoverable.
- Added `Attach Image` in the file toolbar for local `.png`, `.jpg` and `.jpeg` files. The inserted image is selected immediately and sized to about 30% of the current visible world width, preserving the source pixel aspect ratio.
- Added grip support: four corner grips and one center move grip. General transform tools can still be used for precise rotation and scaling.
- SVG export writes an external `<image href="...">` reference plus an outline polygon. DXF/PDF export currently skip the raster content; this should be documented as a current limitation until dedicated external-image support is designed for those formats.

Recommended manual checks:

1. Attach a PNG and JPG from local disk.
2. Save, close, reopen and verify the image reloads from the external path.
3. Move, copy, rotate, scale and mirror the reference.
4. Hide/lock the layer and verify rendering/selection behavior is consistent with other entities.
5. Rename or move the external image and reopen the drawing; the placeholder rectangle should remain visible/selectable.


### 2026-05-25 — Image reference editing and relinking

Follow-up improvements for external raster references:

- `ImageReferenceEntity` now exposes helper methods for relinking, origin edits, size edits, rotation edits and transparency/opacity edits while keeping the external-reference model intact.
- The Property Panel exposes editable fields for the selected image reference: file path, origin X/Y, width, height, rotation and transparency percentage. Edits are applied through normal replace-entity commands, so undo/redo remains coherent.
- Added a `Replace Image` toolbar action. It requires exactly one selected image reference, opens the same PNG/JPG picker and relinks the selected entity while preserving its current drawing geometry. Pixel dimensions are refreshed from the newly selected raster file.
- Added tests for relinking, rotation around center, size-preserving vector direction, and ViewModel relink behavior.

Manual checks:

1. Attach a PNG/JPG, select it and change width/height/rotation from the Property Panel.
2. Use Replace Image on a selected reference and verify geometry is preserved while the visual raster changes.
3. Try Replace Image with no selected image and verify the explanatory message is shown.
4. Undo/redo Property Panel edits and Replace Image.

### 2026-05-25 — Image aspect reset refinement

Follow-up refinement for external raster references:

- `ImageReferenceEntity` now exposes `NaturalAspectRatio`, `HasNaturalAspectRatio`, `WithSizeAroundCenter(...)` and `WithNaturalAspectRatio()`.
- Added a `Reset Aspect` toolbar action for exactly one selected image reference. It restores the rectangle height from the linked raster pixel metadata while preserving width, center, rotation and the external-reference model.
- The Property Panel now shows the natural pixel aspect ratio when pixel metadata is available.
- Added Core and App tests for centered resizing, natural aspect reset and the no-selection rejection path.

Manual checks:

1. Attach a landscape or portrait PNG/JPG.
2. Distort width/height from the Property Panel or grips.
3. Select the image and use `Reset Aspect`.
4. Verify the center and rotation stay stable while height returns to the pixel aspect ratio.
5. Try `Reset Aspect` with no image selected and verify the explanatory message.

### 2026-05-25 — Image reference relative path persistence

Follow-up persistence refinement for external raster references:

- Added `ExternalReferencePathHelper` in `OpenCad2D.Persistence`.
- `JsonDocumentSerializer.SaveToFile(...)` now normalizes image reference paths before writing JSON. Fully qualified image paths are stored relative to the `.opencad2d.json` document folder whenever possible.
- `JsonDocumentSerializer.LoadFromFile(...)` now resolves relative image paths against the folder containing the loaded drawing before deserialization.
- Existing absolute paths remain supported for compatibility and for references that cannot be safely relativized.
- Added persistence tests for saving an attached image as a relative path and loading a relative image path as a resolved full path.

Manual checks:

1. Save a drawing next to an `images/` folder and attach `images/plan.png`.
2. Inspect the `.opencad2d.json`: the image path should be similar to `images/plan.png`, not a machine-specific absolute path.
3. Close and reopen: the raster should render normally.
4. Move the drawing together with the `images/` folder and reopen: the reference should still resolve.
5. Open an older drawing that contains absolute image paths and verify it remains compatible.

### 2026-05-25 — Missing image reference workflow

Follow-up workflow refinement for external raster references:

- `MainWindowViewModel` now exposes `MissingImageReferenceCount` and `HasMissingImageReferences`, computed from the current `ImageReferenceEntity` file paths.
- Opening a drawing now warns the user when one or more external raster references cannot be found.
- Added a `Relink Missing` toolbar action. It relinks the selected missing image reference, or the first missing image reference in the document, to a newly chosen PNG/JPG while preserving the existing CAD geometry: center, size, rotation and layer state remain unchanged.
- Added `SelectNextMissingImageReference()` in the view model for diagnostics/testability and future UI workflows.
- Added App tests for missing-reference counting, selecting the first missing image and relinking while preserving geometry.

Manual checks:

1. Attach an image, save, then rename or move the external PNG/JPG.
2. Reopen the drawing and verify that a missing-reference warning is shown.
3. Use `Relink Missing` and select the new image file.
4. Verify the raster reappears in the same CAD position, size and rotation.
5. Save/reopen again and verify the warning no longer appears.

### 2026-05-25 — Raster image snap support

Follow-up snapping refinement for external raster references:

- `EndpointSnapProvider` now exposes the four corners of `ImageReferenceEntity` as endpoint snap candidates.
- `MidpointSnapProvider` now exposes the midpoint of each image border.
- `CenterSnapProvider` now exposes the image rectangle center.
- `ImageReferenceEntity.GetClosestPoint(...)` now returns the closest point on the image border, so nearest snap works on the rectangle outline instead of returning an arbitrary point inside the filled raster area.
- Added interaction tests for endpoint, midpoint, center and nearest snapping on image references.

Manual checks:

1. Attach an image.
2. Enable Endpoint and verify snap markers on the four corners.
3. Enable Midpoint and verify snap markers on the four border midpoints.
4. Enable Center and verify the center snap.
5. Enable Nearest and verify the cursor snaps to the image border.

### 2026-05-25 — Collect external image references

Follow-up project-portability refinement for external raster references:

- Added a `Collect Refs` toolbar action for raster image references.
- The command requires the drawing to be saved first, so the target package folder can be derived from the current `.opencad2d.json` path.
- Existing linked PNG/JPG files are copied into an `images/` folder beside the drawing file.
- Duplicate source images are collected only once; multiple references can point to the same collected file.
- If two different source images have the same filename, the collector creates a unique filename such as `name_2.png` rather than overwriting an existing file.
- Missing references are skipped and reported; their placeholder/relink workflow remains unchanged.
- CAD geometry is preserved: position, size, rotation, pixel metadata, layer and entity ids are not changed.
- The UI saves the drawing after collecting, so `JsonDocumentSerializer.SaveToFile(...)` persists the collected paths as relative references like `images/plan.png`.
- Added App tests for collecting into the drawing folder, reusing one copied file for duplicate source references and rejecting collection for unsaved drawings.

Manual checks:

1. Save a drawing.
2. Attach one PNG/JPG from another folder.
3. Use `Collect Refs`.
4. Verify an `images/` folder appears beside the `.opencad2d.json` file.
5. Inspect the JSON and verify the image path is relative.
6. Move the drawing file together with the `images/` folder and reopen.

### 2026-05-25 — Image References Manager

Implemented a first `Manage Refs` workflow for external raster image references.

- Added `ImageReferenceManagerWindow` and `ImageReferenceManagerWindowViewModel`.
- The manager lists linked PNG/JPG references with status (`OK` / `Missing`), filename, path, pixel size, CAD size, rotation and instance count.
- References that use the same file path are grouped into one row; the instance count shows how many image entities use that linked file.
- Added manager actions:
  - `Select`: selects the reference in the drawing.
  - `Relink`: chooses a new local PNG/JPG and updates the selected reference while preserving geometry.
  - `Replace`: same file replacement workflow for non-missing references, also preserving geometry.
  - `Open Folder`: opens the containing folder with the system shell when it exists.
- Added `MainWindowViewModel.SelectImageReference(...)`, `ReplaceImageReference(...)` and `RelinkImageReference(...)` to support manager-driven operations by entity id.
- Added App tests for selecting image references by id, replacing by id while preserving geometry, grouping duplicate file paths in the manager and summarizing missing references.

Manual checks:

1. Attach two images and open `Manage Refs`.
2. Verify status, path, pixel size, CAD size, rotation and instance count.
3. Use `Select` and verify the reference is selected in the drawing.
4. Rename one linked image, reopen the drawing and verify the manager shows `Missing`.
5. Use `Relink` from the manager and verify geometry is preserved.
6. Use `Open Folder` for an existing reference.

### 2026-05-25 — Documentation and v0.9 release preparation

Documentation consolidation for the external raster-image reference milestone and the v0.9 release gate.

Updated:

- `README.md` now mentions external raster image references, relative paths, Collect Refs, Manage Refs and the current SVG/DXF/PDF export distinction.
- `docs/roadmap.md` now treats raster-reference management, relinking, relative paths and Collect Refs as completed v0.9 work; DXF/PDF raster-image export parity remains deferred.
- `docs/known-limitations.md` no longer says that a reference manager/relink dialog is missing; it now describes the remaining raster-reference limitations more accurately.
- `docs/persistence.md` documents image-reference metadata, relative path normalization, load-time resolution and the portable `images/` folder workflow.
- `docs/svg-export.md` and `docs/export.md` document SVG external `<image href="...">` output and clarify that PDF/DXF raster output is deferred.
- `docs/architecture.md` now describes the split of image-reference responsibilities across Core, Persistence, Interaction, App and Export.

Added release preparation files:

- `docs/release-v0.9.md`
- `docs/release-checklist-v0.9.md`
- `docs/release-publish-v0.9.md`

Before tagging v0.9, run the full build/test gate and perform the manual smoke tests listed in `docs/release-checklist-v0.9.md`, especially the external image-reference workflow and SVG/DXF/PDF export expectations.


---

## Current planning pivot — v0.8.100+ expansion line

The project is intentionally staying in the v0.8 line before the next v0.9 stabilization gate. The next work should be treated as v0.8.100+ milestones, not as an immediate v0.9 release.

The planned order is:

1. `v0.8.100` — Import another `.opencad2d.json` drawing into the current document.
2. `v0.8.110` — Block model with `BlockDefinition` and `BlockReferenceEntity`.
3. `v0.8.115` — Block tools: Create Block, Insert Block, Edit Block, Explode and minimal Block Manager.
4. `v0.8.120` — Architectural symbols: north symbol, metric scale, section/elevation markers and title block/testalino helpers.
5. `v0.8.130` — Stair tools for plan, side elevation and front elevation, including optional slab/structure line.
6. `v0.8.140+` — Boundary Fill v1 exists for click-inside linear regions that generate filled polylines; BF v2 should add preview, sampled arc/circle boundaries and gap tolerance before a true HatchEntity handles holes/islands and composite boundaries.
7. `v0.8.160+` — Consolidation before the future v0.9 release gate.

North Symbol note: the current default geometry is circle + upward arrow made with ordinary entities. The picked point is the `(0,0)` symbol base point; the `N` label is offset beside/above the arrow tip and must not overlap the arrow shaft.

Detailed planning documents added for this pivot:

- `docs/roadmap-v0.8.100.md`
- `docs/specs/v0.8.100-import-drawing.md`
- `docs/specs/v0.8.110-blocks.md`
- `docs/specs/v0.8.120-architectural-symbols.md`
- `docs/specs/v0.8.130-stairs.md`
- `docs/specs/v0.8.140-hatch.md`

Important implementation opinion preserved from planning: do not jump from BF v1 directly to full AutoCAD-style hatch detection. The implemented linear click-inside workflow is a useful bridge, but the next steps should be preview, sampled curve boundaries and controlled gap tolerance before holes/islands or associative hatch behavior. Blocks should be implemented before symbol/stair libraries so that generated architectural content can reuse definitions instead of becoming disconnected one-off geometry.


## v0.8.100-v0.8.102 Import Drawing

Implemented the first native OpenCad2D import workflow. The toolbar exposes `Import Drawing`, which loads another `.opencad2d.json` and appends it to the current document. The active document is not replaced and the current file path is preserved. Imported entities receive fresh IDs and are selected after import. Layers, line formats, text formats and dimension styles are merged with conflict-safe remapping. The whole merge is committed as a single undoable command.

The workflow now uses a pending placement step. After file selection, v0.8.102 shows a small options dialog with uniform `Scale` and `Rotation °`. The imported drawing is then committed when the user clicks an insertion point in the canvas; an active snap candidate is used when available. Escape cancels the pending import without changing the document.

Deferred refinements: live import preview, command-line alias and a dedicated import report window.

## Blocks foundation — v0.8.110

The project now has the first block model layer:

- `BlockDefinitionId` in `OpenCad2D.Core/Identifiers`.
- `BlockDefinition` and `BlockDefinitionCollection` in `OpenCad2D.Core/Blocks`.
- `BlockReferenceEntity` in `OpenCad2D.Core/Entities`.
- `CadDocument.BlockDefinitions` stores reusable definitions separately from drawing entities.
- `EntityKind.BlockReference` identifies block reference entities.
- JSON persistence supports `blockDefinitions` at document level and `BlockReference` entities.
- `CadEntityRenderer` can render a block reference by transforming definition entities into world coordinates.

This foundation checkpoint has since grown into user-facing Create Block, Insert Block, Block Manager, internal snapping, Explode Block and first in-place Edit Block workflows.


## v0.8.111 block workflow handoff

Create Block from selection is implemented as the first user-facing block workflow. The main pieces are:

- `AddBlockDefinitionCommand` in Core for undoable definition creation/removal.
- `CreateBlockOptionsWindow` for block name, numeric base point and optional canvas base-point picking.
- `CreateBlockOptions` in the App ViewModels/Blocks namespace.
- `MainWindowViewModel.CreateBlockFromSelection(...)` creates the definition, converts selected entities into local block coordinates by translating them by `-basePoint`, replaces the selection with a `BlockReferenceEntity`, and selects the reference.

Canvas picking for the block base point is supported through `BeginCreateBlockBasePointPick`, `CommitCreateBlockBasePointPick` and `CancelCreateBlockBasePointPick` on `MainWindowViewModel`; active snap candidates are used when available. Nested block creation is intentionally rejected for now.

Insert Block is implemented through `InsertBlockOptions`, `InsertBlockOptionsWindow`, `BeginInsertBlockPlacement`, `CommitPendingBlockInsertion` and `CancelPendingBlockInsertion`. It inserts an additional `BlockReferenceEntity` for an existing definition with uniform scale, rotation and a picked insertion point. Active snap candidates are honored and Escape cancels the pending insert without modifying the document.

The v0.8.113 minimal Block Manager is implemented through `BlockManagerWindow`, `BlockManagerWindowViewModel`, `EditableBlockDefinitionViewModel`, `BlockManagerResult` and `BlockManagerAction`. It lists definitions, shows entity/reference counts and bounds, allows direct rename validation, deletes only unused definitions, and can start insertion of the selected definition. Changes are applied with `UpdateBlockDefinitionsCommand`, so rename/delete operations are undoable. Internal block snapping, Explode Block and the first in-place Edit Block session are now implemented.


## v0.8.114-v0.8.115 block snap and explode handoff

Block references are now usable through their internal transformed geometry for click selection and object snaps. This fixes the earlier limitation where a block was effectively selectable only by its reference bounds/window. Endpoint, midpoint, center and nearest snap providers can resolve candidates from child geometry transformed into world coordinates.

`ExplodeTool` supports selected `PolylineEntity` instances and selected `BlockReferenceEntity` instances. For polylines it now decomposes each segment: straight segments become `LineEntity` instances and bulged segments become `ArcEntity` instances. Closing segments on closed polylines are included, so a closing bulge becomes a closing arc. For block references it reads the matching `BlockDefinition`, transforms each definition entity through `BlockReferenceEntity.TransformContainedEntity(...)`, assigns each resulting entity a fresh `EntityId`, and commits the replacement through `ModifyEntitiesCommand`. Undo restores the original polyline or block reference. The shared block definition is intentionally kept in the document because other references may still use it.

Recommended next step: stabilize the first `Edit Block` workflow with manual testing, then decide whether a later isolated block editor is needed.

## v0.8.115 first Edit Block handoff

The first Edit Block workflow is implemented as an in-place edit session started from a selected `BlockReferenceEntity`.

Current behavior:

- `BeginEditSelectedBlock()` requires exactly one editable block reference selected.
- The selected reference is temporarily replaced by world-space copies of its definition entities through `ModifyEntitiesCommand`.
- The temporary edit entities are selected so the user can move, edit, delete or replace them with normal tools.
- `SaveActiveBlockEdit()` updates the source `BlockDefinition` from the currently selected non-block entities when any are selected; otherwise it uses the tracked temporary edit entities.
- Save converts edited world-space geometry back into block-local coordinates through `Matrix2D.Invert()` and restores the original block reference id with updated definition bounds.
- `CancelActiveBlockEdit()` removes the temporary edit entities and restores the original reference without changing the definition.

This is intentionally not a full isolated block editor yet. It gives a safe, testable first workflow while keeping nested blocks unsupported.


## v0.8.120 Architectural symbols — North Symbol first pass

The architectural-symbols milestone has started with `NorthSymbolTool`. The tool is registered as `ToolId.NorthSymbol` in the `Symbols` category and exposed in the left toolbar under a new `SYMBOLS` section. Command aliases are `NORTH`, `NORTHSYMBOL` and `NS`.

Current North Symbol behavior: one click inserts a fixed-size north arrow at the snapped insertion point. The symbol is intentionally made of ordinary entities rather than a specialized entity type: three `LineEntity` objects, one `CircleEntity` and one `TextEntity` with label `N`. Geometry uses the current layer and current text format. Insertion is committed as a single undoable composite command.

Orientation note: the north arrow uses the picked point as its local `(0,0)` base point; the arrow tip points visually upward and the `N` label is offset beside the arrow rather than overlapping the shaft.

Metric Scale Bar first pass: `ScaleBarTool` is registered as `ToolId.ScaleBar` in the `Symbols` category and exposed in the left toolbar under `SYMBOLS`. Command aliases are `SCALEBAR`, `SBAR` and `GRAPHICSCALE`. One click inserts a fixed metric scale bar at the snapped insertion point. The generated geometry is ordinary geometry. After the latest geometry update it creates the requested 0–1000 bar using 6 closed polylines, 7 vertical tick lines and 7 text labels. Geometry uses the current layer and current text format. Insertion is committed as a single undoable composite command.

Tests cover registry creation/category, command aliases, basic insertion, current-layer assignment, undo, endpoint snapping and deterministic generated geometry for the symbol tools.

Recommended next step changed: do not continue adding many fixed-symbol toolbar buttons. Add a `Library` workflow first. Fixed reusable content should be stored as `.opencad2d.json` files under a `library/` folder and shown in a modal browser with categories and preview. Keep direct symbol/tool buttons for parametric generators only.


## Modify tool preview vectors

Move already exposed a base-to-current measurement vector. The preview pass extends the same visual guidance to Copy, Rotate and Scale:

- Copy draws the displacement vector from base point to current destination while preserving the copied-entity preview.
- Rotate and Scale now update a transient reference preview while the user is choosing the reference point, then draw base-to-reference and base-to-destination guide vectors during the final destination phase.
- The implementation lives in `CadToolPreviewRenderer`; `RotateTool` and `ScaleTool` also update `CurrentDestinationPoint` during `WaitingForReferencePoint` so the renderer can show the live reference vector before the reference point is accepted.


## Latest handoff — Metric Scale Bar geometry

- `ScaleBarTool` uses the picked point as local origin `(0,0)` and offsets the requested 0–1000 scale bar geometry from there.
- Output entity count: 20 entities = 6 closed polylines, 7 vertical ticks, 7 text labels.
- `MainWindow` includes active-button synchronization for both North Symbol and Scale Bar.


## Latest planning handoff — Library Browser direction

The next milestone should be `v0.8.122 Library Browser`, documented in `docs/specs/v0.8.122-library-browser.md`.

Decision:

- Avoid adding one toolbar button per fixed symbol.
- Add a single `Library` button/window for fixed reusable `.opencad2d.json` snippets.
- Group snippets by category folders under `library/`, for example `arredo`, `simboli`, `sanitari`, `porte-finestre`, `annotazioni`.
- The modal window should show categories, item list/grid, preview, Insert and Cancel.
- Default insertion policy should create/reuse a block definition and place a `BlockReferenceEntity` at the picked point.
- Use `(0,0)` in the library file as the item base point.
- Honor active snaps for the insertion point.
- Keep direct `Symbols`/tool buttons for parametric generators such as doors, windows, stairs, configurable section/elevation markers and title blocks.

Recommended implementation sequence:

1. Add `library/` folder convention and a `LibraryItemCatalog` service that scans folders.
2. Add a minimal `LibraryWindow` with categories and item list.
3. Add a first vector preview control using the native document loader and bounding-box fit.
4. Add pending insertion workflow: select item -> close dialog -> pick insertion point.
5. Insert as block reference by default, using import/block infrastructure and one undoable command.
6. Add tests for scan, grouping, preview-load safety, insertion, snap and undo.


## v0.8.130 Geometry note — arc selection and mixed lightweight polylines

Implemented a first core pass for AutoCAD-style mixed `PolylineEntity` geometry. `PolylineEntity` now owns `SegmentBulges`, where each segment has a DXF-compatible bulge value: `0` for straight segments and non-zero for circular arc segments. Existing straight polylines remain compatible because the constructor defaults all bulges to zero.

DXF `LWPOLYLINE` import now preserves bulge values on one `PolylineEntity` instead of exploding mixed geometry into separate `LineEntity` and `ArcEntity` instances. DXF export writes group code `42` for non-zero segment bulges. Persistence writes `SegmentBulges` only when curved segments exist, so older JSON files remain readable.

Canvas rendering, hit testing, closest-point logic, bounding boxes, SVG/PDF export and HATCH fallback use an internal polyline approximation for curved segments. This allows clicking/selecting curved polyline portions and keeps downstream export stable. Advanced edit commands that still operate on straight `Polyline2D` adapters currently use the approximation for mixed polylines; future work should add native segment-aware editing/grips for bulge segments.

Added regression coverage for: clicking an `ArcEntity` stroke, clicking a bulged polyline arc segment, preserving DXF bulges on import, writing bulge group `42` on export, and bounding/distance behavior for curved polyline segments.

## Latest handoff — mixed polyline grip/property stabilization

The mixed-polyline pass has been stabilized so common editing surfaces no longer accidentally drop DXF bulge data.

Implemented refinements:

- `PolylineGripProvider` now preserves `SegmentBulges` when moving a vertex or moving the whole polyline.
- Insert grips on curved polyline segments are placed on the approximated arc instead of on the chord midpoint.
- Inserting a vertex into a curved segment keeps the polyline valid by replacing that one curved segment with two straight segments; other segment bulges are preserved.
- Deleting a polyline vertex preserves unaffected segment bulges and sets the newly merged segment to straight.
- Rectangle-specific grip behavior is disabled for closed four-vertex polylines that contain arc bulges, so mixed polylines are edited with generic polyline grips.
- The Property Panel preserves `SegmentBulges` when editing vertex coordinates and resizes the bulge list safely when toggling the closed flag.
- Polyline Geometry now shows segment count and arc segment count; length and area use the internal approximation when a polyline contains arc segments.

Recommended next step: add a real segment editor for mixed polylines: select segment, convert line/arc, edit bulge/radius, split arc natively, and expose per-segment data in a dedicated modal instead of overloading the basic Property Panel.


## Latest handoff — mixed polyline interaction geometry

Mixed `PolylineEntity` segments now expose a shared interaction approximation through `GetInteractionGeometry()`. This keeps hit/selection/snap/measurement behavior consistent for DXF bulge segments without forcing every caller to know how bulges are represented internally.

Implemented refinements:

- Crossing-window selection now tests bulged polylines against the approximated curved path, not only against the original chord-based `Polyline2D`.
- Midpoint snapping on mixed polylines returns the length midpoint of each curved segment approximation, so a semicircular bulge snaps near the visual arc midpoint instead of the chord midpoint.
- Intersection snapping now converts mixed polylines through the same interaction approximation before line-polyline, polyline-polyline, polyline-ellipse and polyline-spline intersection checks.
- Core `MeasurementService` now calculates polyline length and closed-polyline area from the interaction geometry when bulges are present.

New regression coverage was added for arc-segment midpoint snaps, line/bulged-polyline intersection snaps, crossing-window selection on a curved polyline portion, and measurement of open/closed bulged polylines.

Recommended next step: introduce native segment-level operations for bulged polylines instead of relying on approximation in edit services. Priority targets are perpendicular snap to curved segments, center/quadrant snaps for arc segments, and TRIM/BREAK/EXTEND preserving bulge topology where feasible.

## Latest handoff — editable mixed-polyline segment bulges

Mixed polylines now expose per-segment bulge editing in the Property Panel. A selected `PolylineEntity` gets a dedicated `Segments` section with editable rows named `Segment N bulge`. This allows a straight segment to be converted into an arc segment, an arc segment to be flattened by entering `0`, or the arc direction/curvature to be changed by editing the signed DXF bulge value directly.

Implementation notes:

- `SelectionPropertyPanelBuilder` adds `BuildPolylineSegmentsSection` and caps displayed segment rows to keep the panel responsive on large imported DXF polylines.
- `ReplacePolylineSegmentBulge` replaces the entity through the normal undoable command path and preserves all other entity metadata.
- Invalid non-numeric values are rejected by the existing invariant-culture numeric parser.
- Tests cover row exposure, undo support and invalid-value rejection.

Recommended next step: replace the raw bulge value editor with a friendlier segment editor modal that can show segment type, bulge, included angle/radius and quick actions such as Straight, Arc CW and Arc CCW.

## 2026-05-28 - Polyline draw tool: three-point arc segments

The `PolylineTool` now supports creating curved segments during drawing.

Implementation notes:

- `PolylineToolState` now includes `WaitingForArcPointOnArc` and `WaitingForArcEndPoint`.
- `PolylineTool` keeps a `_segmentBulges` list in parallel with committed segments.
- Option `Arc` / `A` starts a three-point arc segment. The previous polyline vertex is the arc start point.
- The point-on-arc input is stored temporarily in `_arcPointOnArc`; the following point becomes the segment endpoint.
- `ArcCreationService.TryCreateFromThreePoints` is used to validate the three points and calculate the circular arc.
- The arc is converted to a DXF-compatible bulge with `tan(sweep / 4)`. In the current coordinate convention, clockwise arcs produce positive bulges and counter-clockwise arcs produce negative bulges, matching the existing `PolylineEntity` approximation logic.
- Completing a closed polyline appends a straight closing bulge for now.
- The next segment returns to straight mode after one three-point arc. Future work may add persistent arc mode or additional arc construction modes.

Tests were added in `PolylineToolTests` for command-line arc mode, pointer input, preview bulge, incomplete arc completion, and undo behaviour.

## 2026-05-28 - Join tool stabilization for mixed polylines

The `JoinTool` now supports `LineEntity`, `ArcEntity` and open `PolylineEntity` inputs. It converts selected entities into oriented join segments, builds endpoint-connected chains, rejects branching junctions, and creates `PolylineEntity` results. Arc inputs become bulge segments; reversed arc/polyline segments invert bulge signs.

Important diagnostics were added so command-line feedback explains failure causes: unsupported entity kinds, closed polylines, incompatible layer/style metadata, disconnected endpoints and branching junctions. Undo/redo remains atomic through a `CompositeCommand` that deletes consumed source entities and adds the generated polylines.
## 2026-05-28 - Explode stabilization for mixed polylines

The `ExplodeTool` now handles mixed `PolylineEntity` geometry. It iterates `PolylineEntity.SegmentCount`, reads the corresponding `SegmentBulges` value, creates `LineEntity` for zero-bulge segments and reconstructs `ArcEntity` for non-zero bulge segments. Source layer/style/visibility/lock/draw-order metadata are preserved on every generated entity. Closed polylines include the closing segment, including closing arcs.

This completes the practical inverse of `JOIN` for mixed polylines: line/arc chains can be joined into a bulge-capable `PolylineEntity`, then exploded back into explicit lines and arcs with undo/redo support through `ModifyEntitiesCommand`.


## 2026-05-28 - Fillet on adjacent linear polyline segments

`FilletTool` has been extended beyond Line-Line fillets. It can now pick two adjacent straight segments from the same `PolylineEntity` and replace the shared corner with a tangent bulge segment while keeping the result as one polyline. This is implemented in `FilletTool` by introducing an internal fillet pick model that stores the picked entity and, for polylines, the closest segment index.

Important constraints:

- Line-Line behavior remains unchanged, including Radius, Trim/NoTrim, radius 0, preview and undo/redo.
- Polyline fillet only supports adjacent straight segments from the same polyline.
- Polyline fillet requires Trim mode and radius greater than zero.
- Existing curved/bulged polyline segments are rejected with a clear diagnostic rather than approximated.
- The replacement entity is a new `PolylineEntity` with preserved layer/style/visibility/lock/draw-order/fill metadata and a non-zero bulge on the generated fillet segment.

Regression tests were added for successful adjacent-segment fillet, undo restoration, non-adjacent rejection and existing-bulge rejection.

## 2026-05-28 - Fillet/Chamfer polyline segment pick stabilization

`FilletTool` now resolves selectable objects through `SelectAllByPoint` and supports excluding the first selected polyline segment when resolving the second segment. This fixes the workflow where a second click at the shared polyline vertex selected the same segment again.

`ChamferTool` now supports two adjacent linear segments of the same `PolylineEntity`, mirroring the existing fillet behavior. It preserves the entity as a single polyline and inserts a straight chamfer segment. Existing bulged segments remain intentionally unsupported for this phase.

### 2026-05-28 - Polyline fillet radius correction

Fixed polyline segment fillet geometry for non-90 degree corners. The tangent distance still uses `radius / tan(cornerAngle / 2)`, but the bulge now uses the fillet arc sweep `PI - cornerAngle`, not the original corner angle. This keeps the generated bulge arc tangent to both trimmed polyline segments with the requested radius.


## 2026-05-28 - Fillet support for separate simple polylines

`FilletTool` now also supports terminal segments of separate open linear `PolylineEntity` objects. The tool converts the picked polyline segments to temporary `LineEntity` geometry, reuses the existing Line-Line fillet solver, then converts trimmed line results back to simple polylines while keeping the fillet as an `ArcEntity`.

This intentionally does not yet support separate multi-segment polylines, because replacing only one selected segment while preserving all unrelated vertices needs a more complete segment-surgery implementation. Such cases return a clear conservative diagnostic instead of modifying the drawing.

## 2026-05-28 - Chamfer support for separate simple polylines and line/polyline pairs

`ChamferTool` now supports separate object chamfers where the selected objects are either terminal segments of separate open linear `PolylineEntity` objects, or one standalone `LineEntity` plus one terminal segment of an open linear `PolylineEntity`.

The implementation reuses the existing Line-Line chamfer solver, then converts only the trimmed source that came from a polyline back into a simple `PolylineEntity`. The generated chamfer edge remains a `LineEntity`. Separate multi-segment polylines remain intentionally unsupported and return a clear conservative diagnostic.


## 2026-05-28 - Fillet/Chamfer support for terminal segments of separate multi-segment polylines

`FilletTool` and `ChamferTool` now support separate multi-segment open linear polylines when the picked segment is terminal, meaning segment 0 or the last segment. The tools reuse the existing Line-Line solvers, then write the trimmed endpoint back into the original polyline so the source remains a `PolylineEntity`. This allows CAD-like operations on polyline ends without exploding the geometry.

The implementation intentionally rejects internal segment trims on separate polylines because moving an internal vertex would either disconnect adjacent segments or require a more complex local topology edit. Curved/bulged segments remain unsupported for Fillet/Chamfer in this phase.

## 2026-05-28 - Consolidation pass for mixed polylines and DXF regressions

After the mixed-polyline, JOIN/EXPLODE, OFFSET, FILLET and CHAMFER work, the consolidation pass adds automated DXF regression coverage and documentation cleanup rather than new editing geometry.

New DXF regression tests:

- `DxfExportCompatibilityTests.Export_MixedPolyline_ShouldWriteBulgeGroupsOnOwningVertices` verifies that `PolylineEntity.SegmentBulges` are exported as LWPOLYLINE group code `42` values on the owning vertices. Zero bulges are intentionally omitted.
- `DxfRoundTripTests.ExportThenImport_WithMixedPolylineBulges_ShouldPreserveCompoundPolylineTopology` verifies that a closed mixed polyline round-trips through DXF as one `PolylineEntity`, preserving positive, negative and zero bulges.

Documentation updates:

- `docs/dxf-compatibility.md` now states that bulged `LWPOLYLINE` imports remain a single `PolylineEntity` with `SegmentBulges`, not exploded line/arc entities.
- `docs/known-limitations.md` now distinguishes implemented conservative mixed-polyline offset from the deferred analytic bulge-preserving offset.
- `docs/roadmap.md` now records the completed mixed-polyline modify-tool consolidation and the new automated DXF coverage.

Manual release validation still needs a recorded external viewer pass in LibreCAD/QCAD/Autodesk tools with exact versions. The automated tests protect OpenCad2D's internal DXF contract, but do not prove broad external interoperability by themselves.

### 2026-05-28 — Image reference transparency

Added per-image transparency support for external raster references.

- `ImageReferenceEntity` now stores `Opacity` internally as a normalized `0.0` to `1.0` value and exposes `TransparencyPercent` for UI-facing editing.
- JSON persistence now writes/reads the image reference opacity, defaulting older drawings to fully opaque.
- Avalonia rendering applies the stored opacity when drawing linked raster files; missing-image placeholders remain normal vector placeholders.
- SVG export emits the opacity on external `<image>` elements when the reference is partially transparent.
- `Manage Refs` now displays a transparency percentage column and provides an Apply action for the selected image reference.
- The selected-image Property Panel also exposes an editable `Transparency %` row. All edits use normal replace-entity commands, so undo/redo remains coherent.

Manual checks:

1. Attach a PNG/JPG, open `Manage Refs`, set transparency to `50` and verify the canvas displays the image at half opacity.
2. Save, close and reopen the drawing and verify the transparency is preserved.
3. Export SVG and verify the external `<image>` element keeps the expected opacity.
4. Undo/redo a transparency edit from either `Manage Refs` or the Property Panel.
