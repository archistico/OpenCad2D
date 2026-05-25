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
| Explode / Join essentials | [x] | EXPLODE converts selected polylines into lines; JOIN converts connected selected lines into polylines, with command aliases, buttons, undo and targeted tests. |

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

- `ImageReferenceEntity` now exposes helper methods for relinking, origin edits, size edits and rotation edits while keeping the external-reference model intact.
- The Property Panel exposes editable fields for the selected image reference: file path, origin X/Y, width, height and rotation. Edits are applied through normal replace-entity commands, so undo/redo remains coherent.
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
