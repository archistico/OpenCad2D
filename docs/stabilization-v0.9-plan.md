# OpenCad2D v0.9 stabilization plan

This document is the working plan for the v0.9 release-candidate cycle.

The goal of v0.9 is not to add another large group of CAD features. The goal is to make the current v0.8.x foundation reliable enough to approach the first stable v1.0 release with confidence.

v0.9 should therefore prioritize:

- predictable user workflows;
- regression tests around existing behavior;
- external DXF compatibility records with exact viewer versions;
- complete user/developer documentation;
- safe performance review;
- release packaging discipline.

Large features such as blocks, hatch, raster references, richer native DXF `DIMENSION` support, autosave/recovery v2 and advanced NURBS fidelity are intentionally deferred.

---

## Current baseline before v0.9

The v0.8.x line already includes the core CAD baseline needed for stabilization:

- clean startup from `Templates/default.opencad2d.json`;
- maximized main window on startup;
- native `.opencad2d.json` save/load;
- document-level drafting settings persisted in the drawing file;
- document recovery for partially invalid native files;
- SVG, DXF and PDF export;
- ASCII DXF import for the current practical 2D entity set;
- layers, line formats, text formats and dimension styles;
- editable Property Panel v2 for supported properties, including MTEXT;
- independent draw order / Z-order;
- command input with aliases, coordinates, relative/polar input, history and first-pass autocomplete;
- snapping, grid snapping, Ortho mode and Polar Tracking;
- drawing tools for points, text, MTEXT, lines, rectangles, circles, arcs, ellipses, polylines, polygons and Bezier splines;
- dimension tools for horizontal, vertical, aligned, radius, diameter and angular dimensions;
- transform/modify tools including Move, Copy, Rotate, Scale, Align, Break, Trim, Extend, Offset, Fillet and Mirror;
- align/distribute object tools;
- measure tools;
- tool-provided preview descriptors/entities, so active tool previews no longer require concrete app-renderer dispatch;
- minimal application logging for tool/UI exceptions.

---

## Completed stabilization work inherited from v0.8.x

| Area | Status | Notes |
|---|---|---|
| Runtime safety | Done | Canvas pointer input has guarded async execution and tool/UI exceptions are logged before status reporting. |
| Dialog reentrancy | Done | TEXT/MTEXT dialog reentrancy uses a non-blocking guard. |
| Save/reopen workflow | Done | End-to-end native persistence coverage exists for the current entity set. |
| Import/modify/export workflow | Done | DXF import -> modify -> export regression coverage exists. |
| Default startup | Done | The app starts from a clean template and falls back to safe defaults if the template is invalid. |
| Draw order | Done | Draw order is independent from layers and is undoable. |
| Dimension stale marker | Done | Non-associative dimensions are conservatively marked stale after geometry-changing operations. |
| Command UX | Done | Command history and first-pass autocomplete are implemented. |
| DXF import coverage | Done for v0.8.x scope | LWPOLYLINE bulge arcs, full ELLIPSE and readable SPLINE data are handled with documented limitations. |
| DXF export coverage | Done for v0.8.x scope | SPLINE export writes degree, knot count and knot-vector data. |
| Preview architecture | Done | Active tool previews are exposed through `IToolPreviewDescriptorProvider` / `IToolPreviewEntityProvider`. |
| MTEXT editing | Done | MTEXT text, insertion, rotation, text format and reference width are editable through the Property Panel. |

---

## v0.9 working phases

### Phase 0 - Documentation alignment

Status: completed by the v0.9 planning pass.

- [x] Roadmap re-triaged against the actual v0.8.x state.
- [x] Already completed items removed from the active v0.9 backlog.
- [x] Post-v1.0 feature backlog cleaned so completed entity types are not listed as future work.
- [x] Known limitations focused on real current limitations.
- [x] Handoff updated with the v0.9 starting point and first implementation target.

### Phase 1 - Local application/session settings

Document-level drafting settings are already stored in `.opencad2d.json`. v0.9 should now decide and implement the small local settings layer, separate from drawing content.

Candidate local settings:

- last open/save folder;
- last export folder;
- optional last opened file metadata;
- window/session preferences, only if they are safe and do not make startup fragile;
- future shortcut preferences, if kept small.

Required behavior:

- missing settings file must use defaults;
- partial settings file must use defaults for missing values;
- corrupt settings file must not prevent startup;
- local settings must not be written into `.opencad2d.json`;
- tests must cover save, load and fallback behavior.

### Phase 2 - Undo/redo audit

The project already has command-based undo/redo, but v0.9 should audit the full current user-facing workflow.

Audit groups:

- drawing tools: Point, Text, MTEXT, Line, Rectangle, Circle, Arc, Ellipse, Polyline, Polygon, Spline and dimensions;
- transform/modify tools: Move, Copy, Rotate, Scale, Mirror, Align, Distribute, Break, Trim, Extend, Offset and Fillet;
- Property Panel edits;
- Layer Manager, Line Format Manager, Text Format Manager and draw-order operations.

For each primary operation, verify:

1. operation changes the document as expected;
2. Undo restores the previous state;
3. Redo restores the operation result;
4. selection and dirty state remain coherent where applicable.

### Phase 3 - Export workflow hardening

Existing export tests should be expanded into release-candidate workflows.

Required workflows:

- draw -> annotate -> export DXF;
- draw -> annotate -> export SVG;
- draw -> annotate -> export PDF;
- import DXF -> modify -> export DXF regression remains green.

The mixed drawing should include:

- multiple layers;
- line formats, lineweights and dash patterns;
- text and MTEXT;
- current dimension types;
- circles, arcs, ellipses, polylines, polygons and splines;
- representative modified geometry.

### Phase 4 - DXF compatibility audit

The compatibility sample set already exists. v0.9 should record exact external validation details instead of generic smoke-check notes.

Record for each viewer:

- viewer name;
- exact version;
- operating system;
- test date;
- sample file result: pass / partial / fail;
- visual notes when relevant.

Minimum target viewers:

- LibreCAD;
- QCAD.

Optional:

- Autodesk DWG TrueView.

### Phase 5 - Performance review

This phase is measurement-first.

Review:

- rendering/repaint behavior;
- large file open and viewport interaction;
- snap and hit testing on dense drawings;
- export time for representative files.

Allowed v0.9 fixes:

- avoid obvious repeated calculations;
- guard degenerate geometry cases;
- improve small hot paths when tests stay stable;
- add regression tests for discovered performance-related correctness bugs.

Deferred:

- major renderer rewrite;
- major spatial-index rewrite;
- speculative caching with complex invalidation.

### Phase 6 - Documentation completion

Required user docs:

- installation guide;
- first-use guide;
- import/export guide;
- shortcuts/command input guide;
- known limitations;
- v0.9 release notes.

Required developer docs:

- architecture overview updated with current module boundaries;
- tools/commands docs updated with preview-provider conventions;
- persistence and export docs updated with current capabilities;
- handoff kept current after each v0.9 milestone.

### Phase 7 - Release gate

The v0.9 release gate is:

```powershell
dotnet clean
dotnet restore
dotnet build
dotnet test
git status
```

Only after the release gate passes locally:

```powershell
git add README.md docs samples src tests
git commit -m "docs: prepare OpenCad2D v0.9 release candidate"
git tag -a v0.9.0 -m "OpenCad2D v0.9.0"
git push
git push origin v0.9.0
```

---

## v0.9 completion checklist

```text
[ ] Local settings scope decided
[ ] Local settings storage implemented
[ ] Local settings fallback tests added
[ ] Undo/redo audit completed for drawing tools
[ ] Undo/redo audit completed for modify tools
[ ] Undo/redo audit completed for Property Panel and manager edits
[ ] DXF export E2E workflow completed
[ ] SVG export E2E workflow completed
[ ] PDF export E2E workflow completed
[ ] LibreCAD exact compatibility audit recorded
[ ] QCAD exact compatibility audit recorded
[ ] Optional TrueView compatibility audit recorded
[ ] Rendering performance reviewed
[ ] Large-file behavior reviewed
[ ] Snap/hit-test behavior reviewed
[ ] User documentation completed
[ ] Developer documentation completed
[ ] v0.9 release notes completed
[ ] Full clean/build/test release gate passed
[ ] GitHub release package prepared
[ ] v0.9.0 tag published
```

---

## Do not include in v0.9 unless required by a blocking bug

- Hatch/campiture.
- Blocks/symbols.
- Raster reference images.
- PNG export.
- SVG import.
- Associative dimensions.
- Native DXF `DIMENSION` import/export.
- Advanced Trim modes: Fence, Crossing, Edge, Project, Erase.
- Advanced Fillet pairs: Line-Arc, Arc-Arc and polyline fillet.
- Full NURBS knot/weight evaluation.
- Large UI redesign.
