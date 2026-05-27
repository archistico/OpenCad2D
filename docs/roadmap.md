# OpenCad2D roadmap

This roadmap tracks the active development path from the current extended v0.8 line toward the next stabilization gate and, later, the first stable v1.0 release.

OpenCad2D grows in small, testable phases. Each phase should compile, pass the relevant tests, update documentation and leave a clear handoff before the next phase begins.

Legend:

```text
[x] completed and stabilized
[~] in progress / partially stabilized
[ ] planned
[>] deferred beyond the current release target
```

---

## Current release target: v0.8.100+ expansion line

The v0.9 stabilization gate is deferred. The project will continue inside the v0.8 line, using v0.8.100+ milestones for larger drafting foundations before the next general stabilization release.

Primary v0.8.100+ themes:

- import another `.opencad2d.json` drawing into the current document;
- reusable block definitions and block references;
- architectural symbols and technical drafting helpers;
- stair plan/elevation/front-elevation generation;
- explicit-boundary hatch and fill workflows;
- careful documentation/specification before implementation.

See `docs/roadmap-v0.8.100.md` for the detailed v0.8.100+ plan and `docs/specs/` for milestone specifications.

---

## Deferred target: v0.9 stabilization

v0.9 remains the next stabilization release after the v0.8.100+ expansion line. Its goal is not to add another large feature group, but to make the expanded CAD foundation predictable, precise and safe enough to move toward v1.0.

Primary future v0.9 themes:

- native curve editing precision;
- predictable modify-tool UX;
- reliable save/export behavior;
- DXF/SVG/PDF compatibility checks;
- documented limitations;
- clean release packaging;
- documented external raster-image reference workflow.

---

## Completed foundations

The following foundations are considered complete for the active roadmap. Older implementation details are intentionally not repeated here; see Git history and release notes for historical milestones.

| Area | Status | Notes |
|---|---:|---|
| Core geometry/document model | [x] | Geometry primitives, entities, layers, line formats, text formats, dimension styles, command history and undo/redo are in place. |
| Application shell | [x] | Avalonia canvas, file command bar, top CAD bar, left tool panel, property panel, command row, snap bar and status bar are established. |
| Native persistence | [x] | `.opencad2d.json` save/load, dirty state, save-changes prompt, partial recovery, viewport/document settings persistence and layer/entity fill persistence are implemented. |
| Export/import baseline | [x] | SVG, PDF and DXF export exist; SVG/PDF/DXF include solid fill output for supported closed entities; SVG export includes external raster image references; ASCII DXF import covers the practical 2D entity set currently supported. |
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
| External raster references | [x] | PNG/JPG/JPEG files can be attached as external references, transformed as oriented rectangles, snapped, relinked, collected into portable folders and managed through Image References Manager. |

---

## Active v0.8.100+ specification plan

Status: [~] planning/specification.

The following milestones are planned before the future v0.9 stabilization gate:

| Milestone | Status | Specification | Goal |
|---|---:|---|---|
| v0.8.100 | [x] | `docs/specs/v0.8.100-import-drawing.md` | Import another `.opencad2d.json` drawing into the current document. |
| v0.8.110 | [x] | `docs/specs/v0.8.110-blocks.md` | Introduce block definitions, block references and persistence. |
| v0.8.111 | [x] | `docs/specs/v0.8.110-blocks.md` | Create Block from selected entities with optional picked base point. |
| v0.8.112 | [x] | `docs/specs/v0.8.110-blocks.md` | Insert existing block definitions with scale, rotation and picked insertion point. |
| v0.8.113 | [x] | `docs/specs/v0.8.110-blocks.md` | Add minimal Block Manager for rename, unused delete and insert-selected workflow. |
| v0.8.114 | [x] | `docs/specs/v0.8.110-blocks.md` | Add snap candidates from block-internal geometry. |
| v0.8.115 | [x] | `docs/specs/v0.8.110-blocks.md` | Add Explode Block and the first in-place Edit Block session workflow. |
| v0.8.120 | [~] | `docs/specs/v0.8.120-architectural-symbols.md` | Add north symbol, metric scale, section/elevation markers and title block helpers. North Symbol first pass is implemented. |
| v0.8.130 | [ ] | `docs/specs/v0.8.130-stairs.md` | Add stair plan, side elevation and front elevation generators. |
| v0.8.140 | [ ] | `docs/specs/v0.8.140-hatch.md` | Add explicit-boundary hatch/solid fill entity. |
| v0.8.150 | [ ] | `docs/specs/v0.8.140-hatch.md` | Add holes/islands and composite hatch boundaries. |
| v0.8.160+ | [ ] | `docs/roadmap-v0.8.100.md` | Consolidate the expanded v0.8 line before the next release gate. |

Implementation should follow the order above. Import Drawing and Blocks are foundations; architectural symbols and stairs should build on blocks where useful; hatch is deferred because robust boundary recognition is the most geometry-sensitive part of the plan.

---

## Legacy v0.9 stabilization backlog

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



### External raster image references

Status: [x] basic foundation implemented.

OpenCad2D can now attach local PNG/JPG files as external image references. The drawing stores the source path and an oriented rectangle, never the raster bytes. The reference can be selected and transformed like other rectangular entities: move, copy, rotate, scale, mirror and grip-edit are supported. Missing files are shown as selectable placeholders so drawings remain recoverable. A selected raster reference can also be relinked/replaced and reset to its natural pixel aspect ratio. Missing raster references are reported on open and can be relinked with a dedicated command while preserving drawing geometry. Image paths are normalized on save: when possible, fully qualified paths are written relative to the `.opencad2d.json` document folder and resolved again on load. The `Collect Refs` workflow can copy linked PNG/JPG files into an `images/` folder beside the drawing and save the project with portable relative references. The `Manage Refs` window provides a compact reference manager with status, path, pixel size, CAD size, rotation, instance count, select/relink/replace and open-folder actions.

Current limitations and deferred work:

- relative image paths are supported for project portability; older absolute paths remain compatible;
- `Collect Refs` packages existing linked PNG/JPG files into a sibling `images/` folder and preserves geometry;
- `Manage Refs` lists external raster references, groups duplicate file paths by instance count and offers select/relink/replace/open-folder actions;
- missing image references are detected on open and can be relinked without changing position, size or rotation;
- SVG export links the external raster through `<image href="...">`;
- DXF/PDF raster-image output remains deferred;
- future reference types such as PDF underlays, DXF underlays or block-style XREFs are not part of this raster-only workflow.

## Deferred beyond the active v0.9 scope

These are valid future tasks but should not block the current stabilization flow unless they become critical bugs.

- [>] closed Bezier spline splitting/editing policy;
- [>] Break Point convention for full circles/full ellipses as almost-full open arcs;
- [>] true associative dimensions;
- [>] blocks;
- [>] general hatch/pattern tools beyond the current solid fill support;
- [x] external raster references for PNG/JPG/JPEG as non-embedded image entities;
- [x] raster-reference management, relinking, relative paths and Collect Refs packaging;
- [>] DXF/PDF raster-image export parity;
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

- [x] Preview no-op feedback is consistent with commit-click feedback for TRIM, BREAK POINT, BREAK SEGMENT and EXTEND.
  - Invalid hover positions no longer fall back to generic messages when the failure reason is known.
  - BREAK endpoint and coincident-point hover regressions are covered by passing tests.

