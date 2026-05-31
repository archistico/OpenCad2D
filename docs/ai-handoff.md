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

---

## Current handoff — Dynamic Command HUD implementation start

The `v0.8.121` Dynamic Command HUD refactor has started with the safe, non-visual foundation step. The detailed specification remains `docs/specs/v0.8.121-dynamic-command-hud.md`.

Implemented in the first code step:

- `CadCanvasWorkspaceChangedEventArgs` now carries the pointer screen position through `PointerScreenPosition`.
- `CadCanvas.NotifyWorkspaceChanged(...)` passes the current `_pointerScreenPoint`.
- `MainWindow.CadCanvas_WorkspaceChanged(...)` forwards that screen position to the ViewModel.
- `MainWindowViewModel` now exposes `HudScreenPosition`, `CurrentPromptState`, `CommandHudState`, `HasLiveMeasurements`, `IsCommandHudVisible`, `LiveDistance`, `LiveAngle`, `LiveDeltaX` and `LiveDeltaY`.
- Added read-only `CommandHudStateViewModel` and `CommandHudFieldViewModel` classes so the future Avalonia HUD can bind to command state without owning command logic.
- Existing bottom command row and input behavior are unchanged.

Important direction:

- Treat this as a command-input architecture refactor, not as a visual-only task.
- Do not implement only for Line, Polyline, Rectangle and Circle; every command-driven tool must be reviewed.
- First stabilize the command prompt contract through `CommandPromptState`.
- Keep the old bottom command row until the HUD has passed regression.
- Add the HUD read-only before moving the real `CommandInputTextBox`.
- Keep one operational command input; do not create duplicate command boxes.
- Defer editable numeric HUD fields until after the read-only HUD and moved command input are stable.

Planned implementation order:

1. HUD-0 tool prompt inventory.
2. HUD-1 shared prompt contract cleanup.
3. HUD-2 pointer screen position and live measurement data. `[~] started`
4. HUD-3 read-only `CommandHudState`. `[~] started`
5. HUD-4 read-only visual HUD overlay.
6. HUD-5 move the real command input into the HUD.
7. HUD-6 remove the old bottom command row.
8. HUD-7 editable numeric HUD fields later.

Next recommended step: complete the prompt inventory and begin moving remaining ViewModel prompt fallbacks into `ICommandDrivenTool.GetPromptState(...)`, before adding any visual HUD overlay.



## 2026-05-30 — Dynamic Command HUD Step 2 prompt contract cleanup

The second Dynamic Command HUD code step extends the shared `CommandPromptState` contract before any visual HUD is added. This keeps the refactor safe and avoids duplicating command-phase logic in Avalonia code-behind.

Implemented in this step:

- `ArcTool` now implements `ICommandDrivenTool` and exposes prompt states for center, start/radius and end/angle phases.
- `PointTool` and `TextTool` now expose command prompt states instead of relying on `MainWindowViewModel` fallback text.
- Measurement tools now expose prompt states: `MeasureDistanceTool`, `MeasureAngleTool`, `MeasureEntityTool` and `MeasureAreaTool`.
- Dimension tools now participate in the command prompt contract through `ThreePointDimensionToolBase`, `RadialDimensionToolBase` and `AngularDimensionTool`.
- `ZoomWindowTool` now exposes first/opposite corner prompt states.
- Architectural insertion tools `NorthSymbolTool` and `ScaleBarTool` now expose insertion-point prompt states.

Current intentional exclusions:

- `SelectionTool` remains outside the HUD contract for now because normal selection should not show the dynamic command HUD.
- `GripEditTool` remains outside the HUD contract until the HUD behavior for grip workflows is designed explicitly.
- `TwoPointToolBase` remains a plain base class because many derived tools already provide specialized prompt states; adding a default implementation there could create accidental duplicate semantics.

Next recommended step: remove or simplify old tool-specific prompt fallbacks from `MainWindowViewModel.CommandPromptText` once this step is confirmed by build/tests, then move to the read-only visual HUD overlay.

## 2026-05-30 — Dynamic Command HUD Step 3 read-only overlay

The third Dynamic Command HUD code step adds the first visual HUD overlay without changing command input behavior.

Implemented in this step:

- `MainWindow.axaml` now wraps the CAD canvas in a `Grid` and adds a transparent `Canvas` overlay for the command HUD.
- The HUD panel is read-only and displays:
  - active tool name,
  - current prompt from `CommandHudState`,
  - live fields already exposed by the ViewModel,
  - command options as compact shortcut labels.
- The old bottom command line remains fully active and unchanged.
- `MainWindow.axaml.cs` positions the HUD near the current pointer screen position with a fixed offset and clamps it inside the overlay bounds.
- The overlay itself is not hit-test-visible, so it does not intercept canvas or command input events.

Important constraints preserved:

- No editable HUD numeric fields yet.
- No move of `CommandInputTextBox` yet.
- No removal of the old bottom command row yet.
- No command behavior change is intended in this step.

Next recommended step: validate the read-only HUD manually across draw, dimension, measurement and modify tools, then refine HUD field selection per tool before moving the real command input into the HUD.

## 2026-05-30 — Dynamic Command HUD Step 4 transitional command input

The fourth Dynamic Command HUD code step moves the active command input experience into the HUD while keeping a safe bottom fallback for idle/non-HUD states.

Implemented in this step:

- `MainWindow.axaml` adds `HudCommandInputTextBox` inside the command HUD panel.
- The bottom command row remains present but is visible only when `IsCommandHudVisible` is false.
- `MainWindow.axaml.cs` now treats the HUD input and the bottom input as synchronized views of the same command text.
- Command submission, autocomplete, history navigation, Backspace, Escape and typed-character forwarding all use helper methods instead of directly reading only the old bottom `CommandInputTextBox`.
- The HUD overlay is now hit-test-visible so the HUD textbox can be focused/clicked, while the panel remains small and clamped near the cursor.

Important constraints preserved:

- There are still no editable numeric HUD fields.
- The command parser and existing `SubmitCommandInput` flow are unchanged.
- The bottom command row is not deleted yet; it remains the fallback for idle states and selection-like workflows where the HUD is hidden.

Next recommended step: test command entry both while idle and while a command HUD is visible, especially command history, autocomplete, polyline shortcuts, Align scale confirmation and Escape behavior. After that, remove the fallback row only when idle command discoverability is solved cleanly.

## 2026-05-30 — Dynamic Command HUD Step 5 icon polish

The fifth Dynamic Command HUD code step is a visual/UX consolidation step. It does not change command behavior.

Implemented in this step:

- The HUD header now uses a real `Path` named `HudToolIcon` instead of the temporary textual marker.
- `MainWindow.axaml.cs` maps the active tool display name to the existing icon resources already defined in `Resources/Icons.axaml`.
- Icon refresh is called from `UpdateCommandHudPosition()`, keeping the HUD header synchronized with active command changes.
- The transitional bottom command row remains available for idle/non-HUD command entry.

Important constraints preserved:

- No editable HUD numeric fields yet.
- No command parser changes.
- No removal of the bottom command row yet.

Next recommended step: manually verify icons across draw, modify, measure and dimension tools. Once stable, decide whether to implement an idle command HUD before deleting the bottom command row.

## 2026-05-30 — Dynamic Command HUD Step 6 bottom fallback demotion

The sixth Dynamic Command HUD code step is a conservative UX consolidation step. It does not remove the fallback input row yet, but it stops presenting it as the active command prompt UI.

Implemented in this step:

- `BottomCommandLinePanel` remains available only through the existing `IsBottomCommandLineVisible` fallback rule.
- The bottom fallback now shows only a `Command` label plus the synchronized `CommandInputTextBox`.
- The bottom fallback no longer displays `ActiveToolName` or `CommandPromptText`; active command identity and prompt information now belong to the cursor HUD.
- No command parser, tool, focus, history or autocomplete behavior is changed.

Important constraints preserved:

- No editable HUD numeric fields yet.
- No deletion of the fallback row yet.
- The fallback remains useful for idle command entry until a dedicated idle HUD/launcher is implemented.

Next recommended step: validate that idle command entry, active HUD command entry, autocomplete, history navigation and Escape still behave as before. After that, design the idle command launcher or proceed to editable HUD fields only if the fallback strategy is accepted.

## 2026-05-30 — Dynamic Command HUD Step 7 contextual read-only fields

The seventh Dynamic Command HUD step improves the read-only field mapping in `MainWindowViewModel.BuildCommandHudFields()`.

Implemented in this step:

- `RectangleTool` uses `Width` and `Height` during opposite-corner input.
- `RectangleBySidesTool` uses `Width`/`Angle` for the first side and `Height` for the second side.
- `CircleTool` uses `Radius` during radius input.
- `ArcTool` uses `Radius`/`Angle` for start-point/radius input and `Angle` for end-direction input.
- Other live-measurement phases continue to use the generic `Distance`/`Angle` field pair.

Important constraints preserved:

- The HUD fields remain read-only.
- No command parser changes were made.
- No bottom fallback removal was made.
- No tool state machine changes were made.

Next recommended step: manually verify field labels with Line, Rectangle, Rectangle Sides, Circle, Arc, Polyline and a few modify tools. After validation, proceed either to a dedicated idle command launcher or to the first editable numeric field experiment, starting with Line only.

## 2026-05-30 — Dynamic Command HUD Step 8 extended contextual read-only fields

The eighth Dynamic Command HUD step extends the read-only HUD field mapping beyond the first draw tools.

Implemented in this step:

- `EllipseTool` now shows `Major radius`/`Angle` while defining the major axis and `Minor radius` while defining the minor radius.
- `PolygonTool` now shows `Radius`/`Angle` while defining the polygon vertex/radius.
- `RotateTool` now shows `Angle` during the destination/angle phase, using the tool preview angle when available.
- `ScaleTool` now shows `Factor` during the destination/factor phase, using the tool preview factor when available.
- `OffsetTool` now shows `Distance` during the two-point distance phase.
- `MirrorTool` and `BreakBetweenPointsTool` use `Distance`/`Angle` during their second-point phases.
- `MeasureAngleTool` and angular dimension workflows show `Angle` in angle-defining phases.
- Radial dimension workflows show `Radius` while defining the point on the circle.
- A generic prompt-kind fallback now maps `PointOrDistance`, `PointOrAngle`, `Distance` and `Angle` expected inputs to suitable read-only HUD fields.

Important constraints preserved:

- The HUD fields remain read-only.
- No command parser changes were made.
- No bottom fallback removal was made.
- No tool state machine changes were made.

Next recommended step: manually verify field labels with ellipse, polygon, rotate, scale, offset, mirror, break-between, radial dimension and angular dimension. After validation, the project can either add a compact idle command launcher or begin the first editable HUD numeric field experiment, preferably limited to Line only.


## 2026-05-30 — Dynamic Command HUD Step 9 generic option shortcuts

The ninth Dynamic Command HUD step adds generic shortcut handling for command options exposed by `CommandPromptState.Options`.

Implemented in this step:

- `MainWindow` now checks the active `ICommandDrivenTool` prompt for matching option shortcuts when a key is pressed and the command input buffer is empty.
- Matching shortcuts are submitted through the existing `SubmitCommandInput` pipeline, preserving parser behavior, command history, status refresh and document invalidation.
- This brings commands such as Fillet, Trim, Chamfer and Spline closer to the same immediate option behavior already expected from Polyline.
- Existing polyline Enter completion and Align scale confirmation logic remain in place.

Important constraints preserved:

- No tool state machine changes were made.
- No parser changes were made.
- No editable numeric HUD fields were added.
- The bottom command fallback remains present.

Next recommended step: validate direct option shortcuts in Polyline, Fillet, Trim, Chamfer and Spline. After that, the next architectural step can be either a dedicated idle command launcher or a carefully isolated first editable numeric field experiment for Line.

## 2026-05-30 — Dynamic Command HUD Step 10 editable-field metadata scaffold

The tenth Dynamic Command HUD step prepares the numeric HUD field model for the future editable-field milestone without changing the current UI behavior.

Implemented in this step:

- `CommandHudFieldViewModel` now has a `CommandHudFieldKind` classification for distance, angle, width, height, radius, factor and generic fields.
- Numeric display formatting is separated from unit display through `NumericValueText` and `DisplayValue`.
- Fields expose `CanAcceptTypedOverride` and `InputPlaceholder` metadata for the future typed override UI.
- Existing HUD field builders continue to work through backward-compatible constructor parameters.

Important constraints preserved:

- HUD numeric fields are still rendered as read-only by the current XAML.
- No command parser changes were made.
- No tool state machine changes were made.
- No geometry override is applied yet.
- The bottom command fallback remains present.

Next recommended step: after build/test validation, add an isolated `CommandHudInputOverride` model and wire it only for Line distance/angle preview. Do not enable editable fields for all tools at once.

## 2026-05-30 — Dynamic Command HUD Step 11 editable field input shell

The eleventh Dynamic Command HUD step turns HUD numeric field visuals into focusable text boxes, but still keeps geometry behavior conservative.

Implemented in this step:

- HUD numeric fields are rendered as `TextBox` controls using `NumericValueText` and `InputPlaceholder` from `CommandHudFieldViewModel`.
- Pressing Enter while a HUD field is focused submits the typed value through the existing `SubmitCommandInput` pipeline.
- Pressing Escape while a HUD field is focused restores the live value text and returns focus to the canvas.
- Empty fields are restored to their current live value when focus is lost.
- Window-level text routing now treats HUD field text boxes as command-input sources, so typing inside a field does not duplicate characters into the main command input.

Important constraints preserved:

- No `CommandHudInputOverride` model has been introduced yet.
- No persistent distance/angle override is applied to previews.
- No tool state machine changes were made.
- Enter from a field is still a one-shot submission using the existing command parser.
- The bottom fallback remains present.

Recommended validation:

- Line after first point: focus Distance, type a numeric value, press Enter, and verify the existing numeric distance behavior still works.
- Circle after center point: focus Radius, type a numeric value, press Enter.
- Rectangle and angle/factor fields should be treated as experimental shells until the future override model is implemented.

Next recommended step: implement `CommandHudInputOverride` only for Line Distance + Angle so Tab-based `distance -> angle -> Enter` can adjust the preview before final confirmation.

## 2026-05-30 — Dynamic Command HUD Step 12 Line one-shot field override

The twelfth Dynamic Command HUD step introduces the first controlled geometry override from HUD numeric fields, limited to `LineTool` while it is waiting for the second point.

Implemented in this step:

- `MainWindowViewModel.TrySubmitCommandHudFieldInput(...)` handles HUD field submissions before falling back to the normal command input pipeline.
- For `LineTool` in `WaitingForSecondPoint`:
  - submitting `Distance` creates the endpoint using the typed distance and the current live angle;
  - submitting `Angle` creates the endpoint using the current live distance and the typed angle.
- Invalid numeric input and unsupported line field states produce normal tool-result messages.
- Other tools continue to use the Step 11 behavior and fall back to the existing command input parser.

Important constraints preserved:

- No persistent `CommandHudInputOverride` model has been introduced yet.
- No preview-freeze behavior has been added.
- No Tab-based `distance -> angle -> Enter` workflow has been implemented yet.
- No parser or tool state machine changes were made.
- The bottom fallback remains present.

Recommended validation:

- Start Line, click the first point, focus `Distance`, type `100`, press Enter and verify that the line is created using the live cursor angle.
- Start Line, click the first point, move the cursor to establish a live distance, focus `Angle`, type `45`, press Enter and verify that the line is created with the live distance at 45 degrees.
- Verify that Circle/Rectangle fields still behave as Step 11 shell fields.

Next recommended step: introduce a real `CommandHudInputOverride` model for Line only so Distance and Angle can be edited independently, previewed together and confirmed after Tab navigation.

## 2026-05-30 — Dynamic Command HUD Step 13 Line persistent Distance/Angle override

The thirteenth Dynamic Command HUD step replaces the Line-only one-shot field behavior with a small persistent override model, still limited to `LineTool` while it is waiting for the second point.

Implemented in this step:

- `MainWindowViewModel` now keeps Line HUD overrides for distance and angle independently.
- Leaving a HUD field with a non-empty value commits the override without finalizing the command.
- Pressing Enter in a HUD field commits the current value and confirms the line endpoint.
- Distance and angle overrides are combined when both are present, enabling the intended `Distance -> Tab -> Angle -> Enter` workflow for Line.
- The line preview is refreshed from the stored overrides while the mouse moves.
- `CadWorkspace.PreviewPointFromCommandLine(...)` was added to update previews from already-resolved command-line/HUD points without applying snapping, ortho or angle constraints a second time.
- Escape in a HUD field clears the temporary Line overrides and restores the live value.

Important constraints preserved:

- The persistent override scope is only `LineTool` in `WaitingForSecondPoint`.
- Other tools still use the previous editable-field shell/fallback behavior.
- No parser changes were made.
- No direct override logic has been added yet for Rectangle, Circle, Polyline, Arc, Rotate or Scale.
- The bottom fallback remains present.

Recommended validation:

- Start Line, click the first point, focus `Distance`, type `100`, press Tab or click the Angle field, type `45`, press Enter. Verify that the created line is 100 units at 45 degrees.
- Start Line, type only Distance and press Enter. Verify that the line uses the typed distance and the current live angle.
- Start Line, type only Angle and press Enter after moving the cursor. Verify that the line uses the live distance and typed angle.
- Press Escape while a HUD field is focused and verify that temporary overrides are cleared.

Next recommended step: after validation, either add tests around the new Line HUD override path or extend the same override model to Polyline line-mode, which can share most of the Distance/Angle logic.

## 2026-05-30 — Dynamic Command HUD Step 14 Polyline line-mode Distance/Angle override

The fourteenth Dynamic Command HUD step extends the persistent Distance/Angle HUD override model from `LineTool` to `PolylineTool` while it is in `PolylineToolState.CollectingVertices`.

Implemented scope:

- `MainWindowViewModel.BuildCommandHudFields()` now uses the override-aware Distance/Angle field builder for:
  - `LineTool` in `WaitingForSecondPoint`;
  - `PolylineTool` in `CollectingVertices`.
- The HUD override commit path was generalized from a Line-only helper to a Distance/Angle point helper.
- Supported override target detection is centralized in `IsDistanceAngleHudOverrideTargetActive()`.
- The preview path still uses `CadWorkspace.PreviewPointFromCommandLine(...)`.
- The confirm path still uses `CadWorkspace.SubmitPointFromCommandLine(...)`.

Behavior:

- In Polyline line mode, entering Distance and/or Angle in the HUD updates the preview point.
- Pressing Enter confirms the next polyline vertex using the current override combination.
- Arc mode is deliberately excluded; entering arc points remains handled by the normal command input and pointer flow.
- Unsupported tools clear the temporary HUD overrides defensively.

This keeps the override model incremental: Line and Polyline line-mode share the same Distance/Angle point-resolution path, while Rectangle/Circle/Arc remain future steps.


### Dynamic Command HUD mouse transparency update

The command HUD is now treated as a keyboard-driven overlay, not as a mouse target. The overlay is transparent to hit testing so fast mouse movement over `Distance`, `Angle` or the HUD command input cannot steal pointer events from the CAD canvas. Numeric HUD fields are entered from the keyboard using `Tab` from the command input, then `Tab` moves to the next numeric field and `Enter` confirms. This preserves the CAD rule that the mouse always remains free for picking points on the canvas.

## 2026-05-30 — Dynamic Command HUD Step 16 Compact polar/coordinate command HUD

The sixteenth Dynamic Command HUD step changes the UX direction from a generic command textbox to a compact, keyboard-driven HUD made of explicit numeric fields.

Implemented in this step:

- Removed the visible generic command text box from the HUD.
- Removed the bottom command line fallback above the status bar.
- Removed the blue `active-command-label` command box from the visible command UI by removing the bottom command row that used it.
- The HUD field layout is now compact and wraps fields horizontally.
- Command options no longer render as `[L]ine [A]rc [C]lose`; instead the shortcut letter is rendered in the project yellow and the remaining keyword text is rendered normally, e.g. `Line Arc Close` with only `L`, `A`, `C` highlighted.
- Added a persistent generic `CommandHudInputState` with `Distance`, `AngleDegrees`, `X`, and `Y` overrides. The state is no longer tied to Line-specific private fields.
- Line and Polyline line-mode now expose the compact field sequence: `Distance`, `Angle`, `X`, `Y`.
- When no base point exists, the HUD can still show live `X` and `Y` mouse coordinates in UCS space.
- The status bar no longer includes the live mouse coordinate text; coordinates now belong to the HUD.
- Initial numeric typing while the CAD canvas is focused is routed to the first editable HUD field instead of the removed generic command input buffer.
- `Tab` cycles through HUD fields instead of returning to a generic textbox.
- `X` and `Y` overrides can resolve absolute UCS coordinates for Line and Polyline line-mode when both coordinates are supplied.

Important constraints:

- The mouse-transparent HUD rule remains: HUD fields are keyboard-driven and do not capture mouse hit testing.
- Advanced command aliases still use an internal command buffer for now, but there is no longer a visible generic textbox.
- The full geometric override behavior remains validated first on Line and Polyline line-mode. Other tools still need their own semantics before becoming fully editable through the new generic state.

Recommended validation:

- Start Line, click the first point, type `100`, verify the value goes into `Distance` and not into any generic textbox.
- Press `Tab`, type `45`, press `Enter`, verify the line is confirmed using Distance/Angle.
- Start Line, click the first point, press `Tab` until `X`, type an X coordinate, press `Tab`, type a Y coordinate, press `Enter`, verify the endpoint is set by absolute UCS coordinates.
- Start Polyline, verify the same Distance/Angle and X/Y sequence for straight segments.
- Verify options such as Line/Arc/Close display without brackets and with only the shortcut letter in yellow.
- Verify there is no bottom command row above the status bar.

## 2026-05-30 — Dynamic Command HUD Step 17 Numeric routing and first-point coordinates fix

The seventeenth Dynamic Command HUD step fixes the first regression found after removing the generic command textbox.

Changes in this step:

- Initial numeric typing from the CAD canvas is now routed automatically only to a primary numeric field such as `Distance`, `Width`, `Radius`, or `Factor`.
- Initial numeric typing is no longer routed automatically to `X` or `Y` when the command is still waiting for the first point.
- `X`/`Y` coordinate entry remains available intentionally through `Tab` navigation.
- Coordinate overrides are no longer limited to Line/Polyline after a base point exists: when an `ICommandDrivenTool` is expecting point input, complete `X` and `Y` values can submit an absolute UCS point.
- Distance/Angle polar override remains restricted to Line and Polyline line-mode until other tools receive explicit semantics.

Validation focus:

- Start Polyline while it says to click the first point, type `100`: it must not jump into `X` automatically.
- Press `Tab` intentionally to enter `X`, type X, press `Tab`, type Y, press `Enter`: the first point should be submitted from absolute UCS coordinates.
- After the first point exists, typing `100` should still route to `Distance` for Line/Polyline line-mode.

## 2026-05-30 — Dynamic Command HUD Step 18 Keyboard Tab focus trap fix

This step fixes the second regression found after the compact HUD removed the visible generic command textbox.

Problem observed:

- In `Polyline`, while the command was still waiting for the first point, pressing `Tab` did not reliably focus the first HUD coordinate field `X`.
- After clicking the first polyline point and typing `100` into `Distance`, pressing `Tab` could leave the HUD and focus the `X` property in the right property panel instead of moving to HUD `Angle`.

Changes in this step:

- `MainWindow` now registers a tunneling `KeyDown` handler for `Tab` using `InputElement.KeyDownEvent` and `RoutingStrategies.Tunnel`.
- When the dynamic HUD is visible and the command input buffer is empty, `Tab` is trapped before Avalonia focus traversal can move to external controls.
- If a HUD numeric field is focused, `Tab` commits that field without confirming the command and moves to the next editable HUD field.
- If focus is still on the canvas, `Tab` focuses the first editable HUD field.
- A keyboard-only active HUD field reference is tracked so that numeric text routed from the canvas to `Distance` can still advance to `Angle` even if normal focus traversal would otherwise escape.

Expected validation:

- Start `Polyline` and keep it at `Click first point`; press `Tab`: focus must go to HUD `X`.
- Type X, press `Tab`: focus must move to HUD `Y`.
- Press `Enter`: the first point should be submitted from complete X/Y coordinates.
- Click the first polyline point, type `100`: value must go to HUD `Distance`.
- Press `Tab`: focus must move to HUD `Angle`, not to the property panel.
- Type `45`, press `Enter`: the next polyline vertex should be confirmed using Distance/Angle.

### Dynamic Command HUD Step 21 — Logical keyboard input reset

The editable HUD fields have been moved away from real Avalonia TextBox focus traversal. Numeric HUD input is now handled as a logical keyboard state in `MainWindow.axaml.cs`, so `Tab` cycles through the available HUD field kinds instead of relying on focused TextBox controls. This avoids focus escaping to the Property Panel. Pointer clicks clear the temporary HUD overrides so distance/angle values do not persist unexpectedly into the next polyline segment or command point.

## 2026-05-30 — Dynamic Command HUD Step 22 active field highlight and first-point coordinates

This step keeps the logical, mouse-transparent HUD model introduced in Step 21 and adds an explicit visual cue for the active logical field.

Changes:

- The active HUD field is now highlighted in the OpenCad2D yellow accent.
- The highlight is driven by the logical `CommandHudFieldKind`, not by Avalonia focus.
- HUD `TextBox` controls remain mouse-transparent; the mouse is still dedicated to the CAD canvas.
- `Tab` navigation is still logical and cycles through available field kinds.
- Coordinate entry for first points is supported through the existing `X`/`Y` HUD fields:
  - while a command is waiting for the first point, press `Tab` to enter `X`;
  - type X, press `Tab` to enter `Y`;
  - type Y, press `Enter` to submit the absolute UCS point.

Validation focus:

- Start `Polyline`, before clicking the first point press `Tab`: `X` must be visibly highlighted.
- Type an X value, press `Tab`: `Y` must be visibly highlighted.
- Press `Enter` after both X and Y are set: the first point must be submitted.
- After the first point, type `Distance`, press `Tab`: `Angle` must be visibly highlighted.

## 2026-05-30 — Dynamic Command HUD Step 23 active-field numeric routing verification

Claude's external review was checked against the current code. One proposed change was accepted and one was intentionally rejected.

Accepted:

- Numeric routing now respects the existing logical HUD field before falling back to the preferred initial field. This keeps the sequence `Distance -> Tab -> Angle -> type number` from routing the next number back to `Distance`.
- The HUD commit handler now recognizes `Width`, `Height`, `Radius` and `Factor` as valid HUD numeric field kinds, preparing the common persistent input state for later tool-specific semantics.

Rejected intentionally:

- `X` was not added to the automatic preferred numeric field list. First-point coordinate entry must remain intentional through `Tab`; otherwise typing `100` while a command asks for the first point would again jump into `X`, which was already identified as undesirable.
- Complete X/Y coordinate confirmation still requires both coordinates when confirming a point. Missing coordinate fallback to the live pointer may be considered later, but it is not part of this safe correction.

Validation focus:

- Start `Polyline`, click the first point, type `200`, press `Tab`, type `45`, press `Enter`: the second value must remain on HUD `Angle`, not return to `Distance`.
- Start `Polyline` while it asks for the first point, press `Tab`, enter X, press `Tab`, enter Y, press `Enter`: the first point should be submitted from X/Y.
- Typing a plain number during first-point prompt should not automatically route to X.


### Step 24 — Freeze complementary polar HUD value

When the user starts typing a polar HUD value, the complementary live value is frozen immediately. Typing `Distance` freezes the current live `Angle`; typing `Angle` freezes the current live `Distance`. This prevents the preview update from recalculating the missing polar component from a changed/stale pointer state and keeps the value that was visible when typing began.

## 2026-05-30 — Dynamic Command HUD Step 25-bis stabilization checkpoint

The active stable baseline is the dynamic HUD after Step 24. A later attempt to extend Rectangle routing caused broad regressions, so expansion is paused until the Line/Polyline HUD behavior is protected by tests.

This checkpoint adds regression coverage for the stable behavior without adding new UI features:

- Line distance entry freezes the live angle that was visible when typing started.
- Polyline first point can be created through HUD `X`/`Y` coordinate fields.
- Polyline `Distance`/`Angle` creates the next vertex and clears the override so subsequent segments start from live values.

Important constraints for future changes:

- Do not generalize `TryResolveCommandHudOverridePoint` for Rectangle/Circle/Arc without tool-specific tests.
- Do not make `X` the default target for plain numeric input during first-point prompts.
- Do not rely on Avalonia focus for HUD numeric fields; the HUD is mouse-transparent and uses logical field state.
- The next safe feature step should be Rectangle read-only/visual verification first, then a dedicated rectangle resolver, not a shared resolver rewrite.

### Step 29A — Dynamic Command HUD for Move/Copy

The dynamic command HUD has been extended to the first modify tools: `MoveTool` and `CopyTool`.

Scope:

- `MOVE` and `COPY` still accept base points through the existing point input path, including HUD `X / Y`.
- During destination point input, both tools now expose editable `Distance / Angle / X / Y` fields.
- The implementation reuses the stable Line/Polyline distance-angle point resolver only for the destination phase of Move/Copy.
- Existing dedicated resolvers for Rectangle/Circle/Rectangle by Sides remain isolated and unchanged.

Regression tests cover:

- moving a selected line with HUD `Distance / Angle`;
- copying a selected line with HUD `Distance / Angle`;
- verifying that the Move destination phase exposes editable `Distance / Angle / X / Y` fields.

### Dynamic Command HUD - Step 29B fix

- Fixed contextual command input parsing for `PointOrAngle` and `PointOrNumber` prompt kinds.
- `Rotate` can now accept a scalar angle through the HUD `Angle` field in the destination phase.
- `Scale` can now accept a scalar factor through the HUD `Factor` field in the destination phase.
- The fix is in `CommandInputParser` and does not change the stabilized HUD resolvers for Line, Polyline, Rectangle, Circle, Rectangle by Sides, Move or Copy.

## Dynamic Command HUD — modify tools audit step

The dynamic HUD has been extended/audited for the first group of modify tools without changing the stable Line/Polyline/Rectangle/Circle/Rectangle-by-sides paths.

Implemented HUD behavior:

- `Mirror`: first axis point uses `X/Y`; second axis point uses `Distance/Angle/X/Y`; final `Yes/No` option remains command-option based.
- `Break Point`: after selecting the entity, the break point phase exposes `X/Y` only.
- `Break Segment`: first break point exposes `X/Y`; second break point exposes `Distance/Angle/X/Y`.
- `Offset`: distance phase exposes `Distance`; second distance point exposes `Distance/X/Y`; side point remains point-based via `X/Y` or click.
- `Fillet`: radius setup phase exposes `Radius`.
- `Chamfer`: distance setup phase exposes `Distance`.
- `Boundary Fill`: seed point uses `X/Y` through the generic point-input path.
- `Trim`, `Extend`, `Delete`, `Explode`, and `Join` remain selection-only/confirm tools with no editable numeric HUD fields.
- `Create Block` and `Insert Block` are not normal `ICommandDrivenTool` tools; they remain modal/pending-placement flows and need a later dedicated HUD step.

A manual checklist was added at `docs/testing/dynamic-command-hud-modify-tools-checklist.md`.

### Dynamic Command HUD — Step 30B modify tools regression guard

Added automated regression coverage for the modify tools audited in Step 30A.

Covered behaviors:

- `Mirror` selected-entity flow exposes `X/Y` for the first axis point and `Distance/Angle/X/Y` for the second axis point.
- `Offset` exposes editable `Distance` in the initial distance phase and accepts a typed distance.
- `Fillet` exposes editable `Radius` after the `Radius` option and accepts a typed radius.
- `Chamfer` exposes editable `Distance` after the `Distance` option and accepts a typed distance.
- `Boundary Fill` exposes only `X/Y` for the seed point.
- `Trim`, `Extend`, `Explode`, and `Join` do not expose scalar HUD overrides.

This step is intentionally test/documentation-only. It does not change the HUD routing or any tool resolver.


### Step 30C - Mirror HUD Tab and option fix

- Tab is reserved for logical HUD field traversal while a command-driven tool is active; CadCanvas grip-edit Tab is now limited to SelectionTool.
- Command option shortcuts such as Mirror Yes/No are handled by the window preview key path after the generic HUD textbox removal.
- Manual check: MIRROR with a selected curve, Tab should enter X for first axis point; at delete-source prompt, Y/N should execute the option.

### Step 30D - Offset / Fillet / Chamfer scalar validation alignment

- Kept the existing HUD routing/resolvers unchanged.
- Aligned HUD scalar validation with the underlying tools:
  - `Offset.Distance` remains strictly positive.
  - `Fillet.Radius` accepts zero and rejects negative values.
  - `Chamfer.Distance` accepts zero and rejects negative values.
- Added regression tests so this distinction is not lost while extending other modify tools.

### Step 30E - Break / Boundary Fill HUD regression guard

- Kept the existing Break / Boundary Fill HUD routing unchanged.
- Added ViewModel-level regression coverage for:
  - `Break Point` after entity selection: HUD exposes `X/Y` only and coordinate confirmation breaks a line at the projected point.
  - `Break Segment`: first break point exposes `X/Y`; second break point exposes `Distance/Angle/X/Y`; distance/angle confirmation removes the expected segment.
  - `Boundary Fill`: seed point `X/Y` creates a filled closed polyline inside a line boundary.
  - `Boundary Fill`: outside seed point leaves the drawing unchanged and reports the no-boundary message.
  - `Delete` is now included with `Trim`, `Extend`, `Explode`, and `Join` in the selection-only no-scalar-HUD guard.
- `dotnet test tests\OpenCad2D.App.Tests\OpenCad2D.App.Tests.csproj` passes with 291 tests.

### Step 30F - Selection-only tools regression guard

- Kept the existing selection-only command routing unchanged.
- Added ViewModel-level regression coverage for:
  - `Trim`, `Extend`, `Delete`, `Explode`, and `Join` expose no editable scalar HUD fields.
  - All five commands cancel back to Selection with `Escape`.
  - `Delete` selects by pointer and confirms with Enter.
  - `Explode` selects a polyline by pointer and confirms with Enter.
  - `Join` selects connected lines by pointer and confirms with Enter.
  - `Trim` and `Extend` complete their boundary/target pointer flows through the ViewModel/tool pipeline.
- `dotnet test tests\OpenCad2D.App.Tests\OpenCad2D.App.Tests.csproj` passes with 301 tests.

### Step 31 - Block tools HUD point input

- Kept `Create Block` and `Insert Block` outside the normal `ICommandDrivenTool` pipeline.
- The existing dialogs still own block name, picked-base-point choice, selected definition, scale and rotation.
- Pending `Create Block` base-point pick now makes the command HUD visible as `Create Block` with editable `X/Y` only.
- Pending `Insert Block` placement now makes the command HUD visible as `Insert Block` with editable `X/Y` only.
- Confirming complete HUD coordinates calls the existing pending commit methods:
  - `CommitCreateBlockBasePointPick(...)`;
  - `CommitPendingBlockInsertion(...)`.
- The shared Distance/Angle resolver was not broadened for blocks.
- Added ViewModel-level regression coverage for HUD visibility/fields and coordinate confirmation for both block pending flows.
- `dotnet test tests\OpenCad2D.App.Tests\OpenCad2D.App.Tests.csproj` passes with 303 tests.

### Step 31A - Escape cancellation regression

- Restored the pre-HUD `Escape` behavior for active commands: pressing `Esc` while the command HUD or an editable HUD field has focus now cancels the active command and returns to Selection instead of only clearing HUD text.
- `MainWindowViewModel.Escape()` now clears HUD overrides and cancels pending non-tool workflows first:
  - `Create Block` base-point pick;
  - `Insert Block` placement;
  - library insertion;
  - imported drawing placement.
- Window-level HUD key handling now routes `Esc` through the same active-command escape path and ends point-placement snapping after pending workflows are cancelled.
- Added ViewModel-level regression coverage for HUD coordinate override cancellation plus `Create Block` / `Insert Block` pending cancellation.
- `dotnet test tests\OpenCad2D.App.Tests\OpenCad2D.App.Tests.csproj` passes with 306 tests.
- `dotnet test OpenCad2D.sln` passes with 2054 tests.

### Step 31B - Additional point-driven commands

- Fixed the generic HUD field fallback so `CommandInputKind.Point` and `PointOrOption` still expose editable `X/Y` even after a previous base point/live measurement exists.
- Added HUD coordinate regression coverage for:
  - `Point`;
  - `Text`;
  - `Multiline Text`;
  - `Zoom Window` second corner.
- Extended `Measure Distance` second-point HUD support to expose `Distance/Angle/X/Y`, matching the command-line direct-distance behavior.
- Added regression coverage for `Measure Distance` typed `Distance/Angle` completion through the HUD.
- `dotnet test tests\OpenCad2D.App.Tests\OpenCad2D.App.Tests.csproj` passes with 311 tests.

### Step 31C - Text HUD async confirmation fix

- Fixed a crash introduced by the `Text` / `Multiline Text` HUD coordinate path.
- Root cause: HUD `Enter` confirmation used the synchronous ViewModel point submission path, while the real Avalonia text provider intentionally throws from `RequestText()` and must be called through `RequestTextAsync()`.
- Added `MainWindowViewModel.TryCommitCommandHudFieldInputAsync(...)` for HUD confirmations when the active tool implements `IAsyncCadTool`.
- Window-level HUD `Enter` handling now awaits that async path, so `Text` and `Multiline Text` open their dialog through the same non-blocking route as canvas clicks.
- Added regression coverage with an async-only text provider that throws if the synchronous path is used.
- `dotnet test tests\OpenCad2D.App.Tests\OpenCad2D.App.Tests.csproj` passes with 313 tests.
- `dotnet test OpenCad2D.sln` passes with 2061 tests.

### Dynamic Command HUD — remaining work checkpoint after Step 31

Current stable HUD coverage includes Line, Polyline, Rectangle, Rectangle by Sides, Circle, Move, Copy, Rotate, Scale, Align, Mirror, Offset, Fillet and Chamfer. The fixed bottom command row and generic command textbox are removed; HUD input is logical, mouse-transparent and keyboard-driven.

Remaining work to resume next session:

1. **Final cleanup**
   - Update `docs/ai-handoff.md`, `docs/command-input.md`, `docs/tools.md`, `docs/commands.md`, and the HUD specification.
   - Remove or simplify residual legacy command-line helper code only after all HUD flows are covered.

Important guardrail: do not broaden the shared HUD resolver while finishing the remaining tools. Continue using narrow tool/phase-specific behavior and regression tests, as done for Rectangle, Circle, Rectangle by Sides and modify tools.

### Step 31D - HUD stability hardening

- Added `CommandHudFieldRoutingPolicy` to centralize logical keyboard routing for editable HUD fields.
- Replaced the hard-coded Window-only preferred-field heuristic with a priority policy that supports:
  - active field preservation when the field still exists;
  - scalar-first routing for Distance/Radius/Width/Height/Factor;
  - Angle-only phases such as rotate/angle tools;
  - Sides-only phases such as Polygon side count;
  - coordinate-only phases using `X` as the first logical field.
- Removed the non-command-source blocker that prevented numeric HUD routing when stale command text was present; routed HUD numeric input now clears the command text deliberately.
- Added a ViewModel-to-Window event, `CommandHudInputOverridesCleared`, so ViewModel-level override resets can explicitly clear the Window logical HUD field state instead of relying on manual paired calls.
- Extended HUD scalar handling to include `Sides` and added validation for whole numbers greater than or equal to 3.
- Polygon now exposes an editable `Sides` HUD field while waiting for side count and accepts it through the same HUD commit path.
- Added regression coverage for the routing policy and Polygon side-count HUD input.
- Test note: this environment did not have the `dotnet` executable available, so the test suite could not be executed here. Run `dotnet test OpenCad2D.sln` locally after applying the patch.

### Step 31E - Rectangle HUD width/height stabilization

- Fixed a Rectangle HUD regression where typed `Width`/`Height` values were stored in `CommandHudInputState` but the displayed HUD fields continued to show live mouse-derived `DeltaX/DeltaY` after `Tab` or pointer movement.
- `BuildWidthHeightFields(...)` now prefers typed HUD overrides before falling back to live measurements.
- Added a Rectangle-specific size resolver that converts typed `Width`/`Height` into the opposite corner point while preserving the cursor quadrant/sign.
- HUD preview now applies resolvable size overrides, not only polar `Distance/Angle` overrides.
- Added regression coverage for:
  - typed Rectangle width remaining visible after mouse movement;
  - typed Rectangle width + height creating an exact closed polyline rectangle.
- Test note: this environment did not have the `dotnet` executable available, so run `dotnet test OpenCad2D.sln` locally after applying the patch.

### Step 31F - Rectangle by Sides HUD polar/height fix

- Fixed a Rectangle by Sides HUD regression where the first side was exposed as `Width`, causing the generic size override resolver to intercept the value instead of resolving a polar first-side endpoint.
- Rectangle by Sides first-side HUD now exposes `Distance/Angle` and participates in the shared polar point override target list while waiting for the first side endpoint.
- Rectangle by Sides second-side `Height` confirmation is handled as a dedicated scalar command input, matching the tool's existing exact-distance command path.
- Added regression coverage for:
  - typed `Distance/Angle` advancing Rectangle by Sides from first-side input to height input;
  - typed `Height` creating the expected closed polyline rectangle.
- Test note: this environment did not have the `dotnet` executable available, so run `dotnet test OpenCad2D.sln` locally after applying the patch.

### Step 31G - HUD polar/radius audit

- Audited draw and dimension tools that displayed radius-like HUD labels but could not resolve those typed values into tool points.
- Fixed the shared HUD field builders so radius-like geometric point phases use `CommandHudFieldKind.Distance` when the value is actually a polar distance from a base point.
- HUD fields now preserve typed overrides instead of reverting to live mouse measurements for:
  - Circle radius;
  - Arc start radius/angle;
  - Arc end angle;
  - Ellipse major radius/angle;
  - Ellipse minor radius;
  - Polygon radius/angle;
  - generic `PointOrDistance` / `PointOrAngle` prompts where a base point exists.
- Broadened polar HUD point resolution through the active command prompt instead of relying only on a brittle manual whitelist.
- Added dedicated geometry resolution for:
  - Arc end angle, which must use the existing arc radius rather than the current mouse distance;
  - Ellipse minor radius, which must create a point perpendicular to the major axis instead of depending on the current mouse direction.
- Added regression coverage for Circle, Polygon, Arc and Ellipse HUD input.
- Test note: this environment did not have the `dotnet` executable available, so run `dotnet test OpenCad2D.sln` locally after applying the patch.

### Step 31H - Ellipse major-axis HUD regression fix

- Fixed the Ellipse major-axis HUD commit path after the polar/radius audit.
- Ellipse `WaitingForMajorAxis` displays `Major radius` + `Angle`, but its command prompt is a plain `Point`; it therefore must be explicitly treated as a polar HUD point override target.
- Without that explicit target, typed `Distance/Angle + Enter` was handled but did not advance to the minor-radius phase.
- Tightened the Ellipse regression test so it asserts that confirming the major-axis angle advances to the minor-axis prompt before entering the minor radius.
- Test note: this environment did not have the `dotnet` executable available, so run `dotnet test OpenCad2D.sln` locally after applying the patch.

### Step 31I - Intersection snap circle/polyline regression fix

- Fixed an intersection snapping gap where `CircleEntity` did not intersect with `PolylineEntity`.
- Rectangles and Rectangle by Sides results are stored as closed polylines, so circle/rectangle intersection snap could not be found even though line/circle and polyline/ellipse already worked.
- `IntersectionSnapProvider` now handles both `PolylineEntity + CircleEntity` and `CircleEntity + PolylineEntity` using exact segment-circle intersections over the polyline interaction geometry.
- Added distinct-point filtering so intersections shared by adjacent segments are not duplicated.
- Added regression coverage for:
  - circle intersection with an axis-aligned closed rectangle polyline;
  - circle intersection with a rotated closed rectangle polyline, matching Rectangle by Sides geometry.
- Test note: this environment did not have the `dotnet` executable available, so run `dotnet test OpenCad2D.sln` locally after applying the patch.

### Step 31J - Intersection snap fallback audit

- Added a conservative `CadEntityIntersectionService.Intersect(...)` fallback to `IntersectionSnapProvider` for entity pairs that are not covered by the provider's exact fast-path switch.
- Kept the existing exact/direct cases for line-line, line/polyline, polyline/polyline, line/circle, circle/circle, circle/polyline, line/arc and circle/arc.
- The fallback now covers previously fragile or missing curve-pair combinations such as:
  - `ArcEntity` + `ArcEntity`;
  - `ArcEntity` + `PolylineEntity`;
  - `LineEntity` + `EllipticalArcEntity`;
  - `PolylineEntity` + `EllipticalArcEntity`;
  - other native curve pairs already supported by the core editing intersection service.
- Added regression coverage for arc/arc, arc/polyline, line/elliptical-arc and polyline/elliptical-arc intersection snaps.
- Test note: this environment did not have the `dotnet` executable available, so run `dotnet test OpenCad2D.sln` locally after applying the patch.

### Step 31K - Pending placement HUD completion

- Extended the dynamic HUD to pending point-placement workflows that do not run as normal `ICommandDrivenTool` instances.
- `IsCommandHudVisible`, `CommandHudToolName`, `GetCurrentPromptState()` and the HUD point submission path now cover:
  - Create Block base-point pick;
  - Insert Block insertion point;
  - Library item insertion point;
  - OpenCad2D import drawing insertion point.
- These workflows expose only absolute `X/Y` coordinate fields while waiting for the placement point. Dialog-owned options remain in their dialogs.
- Added regression coverage for HUD-driven Library insertion and HUD-driven OpenCad2D import placement. Existing block tests already cover Create Block and Insert Block.
- Updated `docs/command-input.md`, `README.md` and `docs/specs/v0.8.121-dynamic-command-hud.md` so the documentation describes the dynamic HUD as implemented rather than as a future migration.
- Internal command-buffer helpers were intentionally kept because aliases, option shortcuts, autocomplete and history navigation still depend on them after removal of the visible bottom command row.
- Test note: this environment did not have the `dotnet` executable available, so run `dotnet test OpenCad2D.sln` locally after applying the patch.

### Step 31L - Modify-tool entity selection snap guard

- Fixed a UI-level snapping leak where a temporary point-placement snap override could remain active when switching to a modify tool.
- `CadCanvas.GetEffectiveEnabledSnaps()` now gives non-selection active tools priority over canvas-level snap overrides; the override remains available for modal pending-placement workflows that still run while the selection tool is active.
- Added `MainWindow.ActivateTool(...)` so toolbar tool activation clears pending point-placement snapping before changing the active tool.
- Audited modify tools that wait for entity selection and added a consolidated regression test asserting `SnapKind.EntityOnly` for Align, Break Point, Break Segment, Chamfer, Copy, Delete, Explode, Extend, Fillet, Join, Mirror, Move, Offset, Rotate, Scale and Trim.
- This keeps Break tools and other selection-oriented modify tools on the entity rectangle marker while they ask for a target entity, then allows geometric snaps only after the command transitions to a point-input phase.
- Test note: this environment did not have the `dotnet` executable available, so run `dotnet test OpenCad2D.sln` locally after applying the patch.


### Step 31M - Modify-tool snap audit test isolation fix

- Fixed the consolidated modify-tool snap audit so each tool is created and evaluated with its own fresh `ToolContext`.
- This avoids false regressions caused by sharing mutable selection/tool state across different tools inside the same test.
- The audit still checks the same contract: when a modify tool starts in an entity-selection phase with no preselection, `GetActiveSnapKind(...)` must return `SnapKind.EntityOnly`.
- The assertion now includes the tool name in the failure message so any future regression identifies the offending tool directly.
- Test note: this environment did not have the `dotnet` executable available, so run `dotnet test OpenCad2D.sln` locally after applying the patch.
