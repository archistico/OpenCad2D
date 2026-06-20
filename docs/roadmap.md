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
- dynamic cursor-adjacent command HUD and command UX unification;
- architectural symbols and technical drafting helpers;
- stair plan/elevation/front-elevation generation;
- explicit-boundary hatch and fill workflows;
- advanced drafting aids such as opt-in Nearest snapping and temporary extension/tracking points;
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
| Export/import baseline | [x] | SVG, PDF and DXF export exist; SVG/PDF/DXF include solid fill output for supported closed entities; SVG export includes external raster image references; ASCII DXF import covers the practical 2D entity set currently supported, including LWPOLYLINE bulge preservation for mixed line/arc polylines. |
| Command input | [x] | Aliases, prompt phases, coordinate input, relative/polar input, direct distances, history and first-pass autocomplete are implemented. |
| Drafting aids | [x] | Snap system, grid, Ortho, Polar Tracking, Zoom Window, Zoom Extents, pan and crosshair are implemented. |
| Draw tools baseline | [x] | Points, text, MTEXT, lines, rectangles, circles, arcs, ellipses, mixed line/arc polylines, polygons and open Bezier splines are supported. Rectangles and polygons are closed polylines for fill/editing purposes. |
| Dimensions baseline | [x] | Horizontal, vertical, aligned, radius, diameter and angular dimensions exist, with conservative stale marking after model edits. |
| Transform tools | [x] | Move, Copy, Rotate, Scale, Mirror and point-based Align are usable and tested. |
| Selection and hit testing | [x] | Selection, Select All, Select Last, Deselect, entity cycling, text/MTEXT bounding-box hit testing and locked/hidden layer behavior are implemented. |
| Native curve editing | [x] | TRIM, BREAK and supported EXTEND flows use native parameters, shared cut points and adapter-backed splitting for supported curves. Mixed polylines preserve bulge segments where supported; curved-end EXTEND is intentionally conservative. |
| Elliptical arcs | [x] | `EllipticalArcEntity` exists with rendering, snapping, persistence and SVG/PDF/DXF export support. |
| Boundary Fill v1 | [x] | `BFILL`/`FILL`/`RIEMPIMENTO` click inside a closed visible linear boundary, build planar faces and create a filled closed `PolylineEntity` on the current layer. |
| Open Bezier split | [x] | Open Bezier splines can be split/extracted natively and are no longer permanently degraded to polylines in TRIM/BREAK. |
| Preview UX base | [x] | TRIM/BREAK removal previews are dashed; EXTEND addition previews are highlighted; selected boundaries stay visible. |
| Save/export UX clarity | [x] | Export creates derived files and does not clear dirty state or replace the current native file path; user messages make this explicit. |
| Modify-tool confirmation policy | [x] | Right click/Enter confirmation, EntityOnly selection phases and clean transient-state reset are established for supported prompts and command phases. |
| Explode / Join essentials | [x] | EXPLODE converts straight and mixed polylines into lines/arcs; JOIN converts connected lines, arcs and open polylines into one or more polylines, with bulge preservation, diagnostics, undo and targeted tests. |
| External raster references | [x] | PNG/JPG/JPEG files can be attached as external references, transformed as oriented rectangles, snapped, relinked, collected into portable folders and managed through Image References Manager. |
| Advanced snapping foundation | [x] | Consolidated pre-v0.9 scope. Nearest is opt-in and disabled by default. SmartPoint Tracking has its own Snap bar toggle and includes SmartPoint capture, direct SmartPoint snap, horizontal/vertical/polar tracking, numeric distance input, tracking intersections, tracking/real linear geometry intersections, line/straight-polyline extension tracking, temporary HUD labels, and guarded Grid/Tracking overlap behavior. Arc/tangent extension remains deferred. |

---

## Mixed polyline stabilization checkpoint

Status: [x] completed for the current mixed-polyline foundation.

Completed:

- [x] `PolylineEntity` supports DXF-compatible `SegmentBulges` for AutoCAD-style mixed line/arc segments.
- [x] DXF `LWPOLYLINE` import/export preserves bulge values instead of exploding compound polylines.
- [x] JSON persistence keeps older straight polylines compatible and writes bulges only when needed.
- [x] Polyline drawing supports explicit `POLYLINE LINE` and `POLYLINE ARC` modes, with three-point arc segment creation.
- [x] Hit testing, crossing selection, snapping and measurement use the visible mixed-polyline interaction geometry.
- [x] Property Panel exposes editable per-segment bulge rows for precise low-level edits.
- [x] Grip editing preserves existing bulges and adds a first visual arc-shape grip for curved segments.
- [x] JOIN supports lines, arcs and open polylines, reports clear failure reasons and creates mixed polylines where needed.
- [x] EXPLODE converts mixed polylines back into `LineEntity` and `ArcEntity` fragments.
- [x] BREAK/TRIM preserve bulged segments where supported; EXTEND preserves existing bulges for straight open-polyline endpoints and refuses curved endpoints instead of flattening them.
- [x] OFFSET supports straight polylines natively and mixed/bulged polylines through a conservative linear approximation of the offset result, without modifying or flattening the source object.
- [x] FILLET and CHAMFER support standalone lines, adjacent straight segments of the same polyline, single-segment separate polylines and terminal segments of separate open linear multi-segment polylines.

Manual regression checklist:

- [ ] Draw a polyline with straight segments, switch to Arc, create a three-point arc, then return to Line.
- [ ] Close a mixed polyline and verify selection/hit testing on the curved segment.
- [ ] Edit a segment bulge from the Property Panel and undo/redo the edit.
- [ ] Drag the arc-shape grip and verify the curved segment changes without moving unrelated segments.
- [ ] JOIN line + arc, arc + line and open polyline + line; verify command-line diagnostics for invalid selections.
- [ ] EXPLODE a mixed open polyline and a mixed closed polyline.
- [ ] BREAK/TRIM a mixed polyline and confirm curved fragments remain curved.
- [ ] OFFSET a mixed polyline and confirm the source remains bulged while the result is an explicit linear approximation.
- [ ] FILLET/CHAMFER adjacent straight segments of one polyline and terminal segments of separate polylines.

Deferred refinements:

- [>] friendly segment editor modal with radius/included-angle display and Straight/Arc CW/Arc CCW actions;
- [>] additional polyline arc construction modes beyond three-point arcs;
- [>] native curved-end EXTEND for bulged polyline endpoints;
- [>] true analytic Offset for bulged mixed polylines that preserves arc/bulge segments in the result;
- [>] center/quadrant/tangent/perpendicular snaps that expose individual polyline arc-segment geometry directly.

Consolidation added after this checkpoint:

- [x] DXF automated coverage now includes mixed-polyline bulge group export and OpenCad2D round-trip preservation.
- [x] Known limitations were updated to distinguish supported conservative approximations from future analytic curve-preserving operations.

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
| v0.8.120 | [~] | `docs/specs/v0.8.120-architectural-symbols.md` | Keep first-pass North Symbol and Metric Scale Bar; reserve future direct tools for parametric helpers rather than many fixed-symbol toolbar buttons. |
| v0.8.121 | [x] | `docs/specs/v0.8.121-dynamic-command-hud.md` | Fixed command row replaced by a dynamic cursor-adjacent command HUD with unified prompt state, editable numeric fields, block placement flows and regression coverage. |
| v0.8.122 | [ ] | `docs/specs/v0.8.122-library-browser.md` | Add Library browser for reusable `.opencad2d.json` snippets grouped by category, with preview and insert workflow. |
| v0.8.130 | [ ] | `docs/specs/v0.8.130-stairs.md` | Add stair plan, side elevation and front elevation generators. |
| v0.8.140 | [~] | `docs/specs/v0.8.140-hatch.md` | Boundary Fill v1 is implemented as click-inside linear face detection that creates filled closed polylines; HatchEntity remains planned. |
| v0.8.145 | [~] | `docs/specs/v0.8.145-boundary-fill-v2.md` | Boundary Fill v2 is in progress: result/options model, segment collector, sampled curve boundaries and endpoint gap-tolerance clustering are implemented at service level; preview and tool confirmation remain. |
| v0.8.150 | [ ] | `docs/specs/v0.8.140-hatch.md` | Add real HatchEntity support for holes/islands and composite hatch boundaries. |
| v0.8.160+ | [ ] | `docs/roadmap-v0.8.100.md` | Consolidate the expanded v0.8 line before the next release gate. |

Implementation should follow the order above. Import Drawing and Blocks are foundations. Before adding more drafting UI weight, the next safety-oriented UI milestone is the dynamic command HUD: it must first unify command prompt state across all tools, then replace the fixed command row in small reversible steps. After that, the Library browser remains the next drafting workflow priority because fixed symbols, furniture and reusable drawing snippets should be loaded from `.opencad2d.json` files instead of becoming separate toolbar buttons. Parametric tools should remain for objects that need dimensions/options before generation. Boundary Fill v1 has started the click-inside workflow conservatively for linear boundaries. The next BF work should improve confidence and coverage before introducing a real hatch entity: preview first, then sampled curve boundaries, then gap tolerance, then HatchEntity for holes/islands.

---

## Active UI refactor checkpoint — Dynamic Command HUD

Status: [x] completed for the current stabilization scope. The HUD architecture, draw/modify flows, selection-only modify tools, block pending-point workflows, manual validation pass and final command-line cleanup are documented.

Specification: `docs/specs/v0.8.121-dynamic-command-hud.md`.

This checkpoint was treated as a command-input architecture refactor, not as a visual-only task. The fixed bottom command row has been removed after HUD prompt, input, focus, command-buffer and manual workflow regression checks.

Milestone order:

1. [x] **HUD-0 Tool prompt inventory** — list every command-driven tool, its phases, prompts, options, expected input, Enter/right-click policy, Escape behavior and possible live fields.
2. [x] **HUD-1 Shared prompt contract cleanup** — make `CommandPromptState` the common source of truth for interactive tools and reduce ViewModel-specific prompt fallbacks.
3. [x] **HUD-2 Pointer position and live measurements** — propagate pointer screen position and extract reusable live distance/angle/delta measurements.
4. [x] **HUD-3 Read-only `CommandHudState`** — expose a ViewModel-level HUD model independent from Avalonia controls.
5. [x] **HUD-4 Read-only visual HUD overlay** — add the cursor-adjacent overlay.
6. [x] **HUD-5 Remove generic command textbox and fixed bottom command row** — HUD input is now logical, keyboard-driven and mouse-transparent.
7. [x] **HUD-6 Editable fields for primary draw tools** — Line, Polyline, Rectangle, Rectangle by Sides and Circle are covered with tool-specific routing/resolvers.
8. [x] **HUD-7 Transform/modify first pass** — Move, Copy, Rotate, Scale, Align, Mirror, Offset, Fillet and Chamfer are covered or validated.
9. [x] **HUD-8 Step 30E Break / Boundary Fill** — Break Point, Break Segment and Boundary Fill HUD input have ViewModel-level regression coverage.
10. [x] **HUD-9 Step 30F selection-only cleanup** — Trim, Extend, Delete, Explode and Join remain prompt/options-only and have ViewModel-level regression coverage.
11. [x] **HUD-10 Step 31 block tools** — Create Block base-point pick and Insert Block placement expose dedicated pending-point `X/Y` HUD input without entering the common tool resolver.
12. [x] **HUD-11 Final cleanup** — docs updated and residual visible command-line helper code removed; the internal command buffer remains intentionally for aliases, options, autocomplete and history.

Regression requirement satisfied for the current scope through targeted automated tests plus the manual verification pass documented in `docs/testing/dynamic-command-hud-manual-verification-2026-05-31.md`. Future command phases must still add narrow regression tests whenever they introduce non-standard HUD routing.

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

Boundary Fill v1 is available as a bridge between current solid-fill support and future hatch entities. It detects the visible linear face containing a picked point and creates a new filled closed `PolylineEntity`. BF v2 is now in progress: the core result/options contract, segment collector and sampled curve-boundary support are in place behind `BoundaryFillOptions`. Preview and user-facing confirmation/diagnostics remain before the tool should be considered v2-complete. Holes/islands should wait for a real `HatchEntity`, because subtractive inner loops do not fit the current single-polyline fill model.

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

OpenCad2D can now attach local PNG/JPG files as external image references. The drawing stores the source path and an oriented rectangle, never the raster bytes. The reference can be selected and transformed like other rectangular entities: move, copy, rotate, scale, mirror and grip-edit are supported. Missing files are shown as selectable placeholders so drawings remain recoverable. A selected raster reference can also be relinked/replaced and reset to its natural pixel aspect ratio. Missing raster references are reported on open and can be relinked with a dedicated command while preserving drawing geometry. Image paths are normalized on save: when possible, fully qualified paths are written relative to the `.opencad2d.json` document folder and resolved again on load. The `Collect Refs` workflow can copy linked PNG/JPG files into an `images/` folder beside the drawing and save the project with portable relative references. The `Manage Refs` window provides a compact reference manager with status, path, pixel size, CAD size, rotation, transparency percentage, instance count, select/relink/replace/open-folder actions and an undoable transparency update.

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


### 2026-05-31 - Advanced snapping and SmartPoint tracking consolidation

Consolidated the pre-v0.9 advanced snapping milestone after local compile/test verification by the maintainer. The current completed scope is:

- opt-in `Nearest` snap, disabled by default;
- SmartPoint capture from strong geometric snaps with a five-point runtime cap;
- horizontal/vertical SmartPoint tracking overlays;
- direct distance input along active tracking lines;
- tracking intersections from different SmartPoints;
- first-pass entity-extension tracking for lines and straight polyline segments.

A manual verification checklist was added at `docs/testing/advanced-snapping-tracking-manual-verification-2026-05-31.md`. Remaining advanced tracking work is intentionally deferred unless it becomes necessary before v0.9: broader polar directions, tangent/arc extension and richer snap/settings UI polish.

### 2026-05-31 - SmartPoint tracking intersections

Implemented temporary snap candidates at intersections between tracking lines generated by different SmartPoints. This completes the base SmartTrack workflow before adding entity-extension tracking.


## 2026-05-31 update - SmartPoint Tracking polar directions

Completed an incremental extension of Advanced Snapping:

- SmartPoint Tracking now reuses the active Polar Tracking step.
- Captured SmartPoints emit additional polar-direction construction lines when Polar Tracking is enabled.
- Polar 90° does not duplicate horizontal/vertical tracking.
- Polar 45°, 30° and 15° add the expected diagonal/intermediate directions.
- Polar-direction candidates behave as normal `Tracking` snaps and support direct distance input.

Manual verification should confirm that the **SmartPoint Tracking** checkbox disables these polar overlays together with the rest of the temporary SmartPoint subsystem.


### 2026-05-31 - SmartPoint tracking intersections with real linear geometry

Extended SmartPoint Tracking so temporary tracking lines can snap to intersections with real line entities and straight polyline segments. The behavior remains conservative: candidates are only produced near the cursor, only finite segment intersections are accepted, and curved/tangent cases remain deferred.

### 2026-05-31 - SmartPoint Tracking HUD label

Added a visual polish step for the advanced snapping milestone: active `Tracking`, `Extension` and `TrackingIntersection` candidates now show a compact HUD label near the snap marker. Distance/angle are shown for candidates that carry tracking origin/direction metadata; point-only intersections show `TRACK INT`. This does not change geometry behavior, but improves manual drafting feedback before resuming the remaining v0.9 work.


### Direct SmartPoint snap update

Captured SmartPoints now produce `SnapKind.SmartPoint` temporary candidates when the cursor is close to the marker. The click is resolved to the captured point, and the marker can therefore be used directly as a snap point as well as a tracking/extension origin.

### 2026-05-31 - Advanced snapping final consolidation

Consolidated the Advanced Snapping / SmartPoint Tracking milestone before resuming the remaining v0.9 work. Final validated scope:

- `Nearest` is available as an opt-in snap and remains disabled by default.
- **SmartPoint Tracking** is the official UI name for the advanced temporary tracking subsystem.
- The Snap bar includes an independent **SmartPoint Tracking** toggle. Disabling it clears SmartPoints, tracking lines, temporary markers and tracking HUD labels.
- SmartPoints are captured only from strong object snaps and can be clicked directly via `SnapKind.SmartPoint`.
- Tracking lines include horizontal, vertical and active Polar Tracking directions.
- Entity extension tracking supports line entities and straight polyline segments.
- Tracking intersections work between temporary tracking lines and between tracking lines and real linear geometry.
- Numeric distance input works along active `Tracking` and `Extension` candidates.
- Temporary markers are resolved as real candidate points; tools must not re-snap them to Grid/Ortho/Polar after selection.
- Grid overlap with Tracking/Extension is handled conservatively: `TRACK GRID` / `EXT GRID` is shown only when the grid node lies on the temporary line, while the tracking/extension constraint remains dominant.
- The tracking HUD label is drawn in the lower-left canvas overlay area to avoid the Dynamic Command HUD.

Deferred beyond this milestone: arc/tangent extension, curve intersections, persistent/pinned SmartPoints and richer SmartPoint management UI.



### 2026-05-31 - DIVIDE command foundation

Added the AutoCAD-style `DIVIDE` command as a draw/construction tool. The first version works on a single selected or picked `LineEntity`, `ArcEntity`, `CircleEntity` or `PolylineEntity`. It asks for an integer segment count from 2 to 1000 and creates persistent `PointEntity` markers on the current layer without modifying or splitting the source entity. Open entities create `N - 1` internal points; closed entities create `N` points starting from their conventional start point. All created points are committed through one `AddEntityCommand`, so undo/redo treats the operation as a single step.


### 2026-05-31 - DIVIDE and deferred HUD integer input consolidation

Stabilized the AutoCAD-style `DIVIDE` command after manual testing. The command is now considered part of the pre-v0.9 construction-tool set. It keeps the finalized behavior contract: source entities are not split, persistent `PointEntity` markers are created on the current layer, open entities create `N - 1` points, closed entities create `N` points, segment count is limited to integers from 2 to 1000, and undo/redo is single-step for all generated points.

The same pass fixed a shared Dynamic Command HUD issue affecting both `DIVIDE` and `POLYGON`: whole-number scalar fields such as `Segments` and `Sides` must allow the user to type and edit the value while the field is active. They are now treated as deferred integer fields and validated only on confirmation, so typed values are not immediately reset to the command default/minimum.

Manual verification reference: `docs/testing/divide-command-manual-verification-2026-05-31.md`.
