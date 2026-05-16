# OpenCad2D v0.9 stabilization plan

This document converts the post-v0.8 critical review into a concrete stabilization track for v0.9.

The goal is not to stop feature development permanently. The goal is to make the current v0.8.x foundation safer before larger features such as hatch, blocks, richer DXF import and autosave are added.

---

## Current triage status

| Area | Issue | Status | Target milestone | Notes |
|---|---|---|---|---|
| Runtime safety | `CadCanvas.OnPointerPressed` is `async void` without a top-level exception boundary | Completed | v0.8.1 | Added a guarded async body and reports failures through the normal canvas status path. |
| Runtime safety | Text/MTEXT modal dialog uses a boolean reentrancy flag | Completed | v0.8.1 | Replaced the boolean gate with a non-blocking semaphore-based guard. |
| Testing | No end-to-end save/reopen workflow test | Completed | v0.8.1 | Added a persistence-level workflow covering geometry, annotation and current v0.8.x entities. |
| Documentation | Critical review contains outdated entity status | In progress | v0.8.1 | Ellipse, MTEXT and Bezier spline are now implemented and should be tracked as stabilization/import follow-up work. |
| Documentation | Roadmap needs explicit stabilization track | In progress | v0.8.1 | Keep v0.9 stabilization separate from future feature backlog. |
| Interop | DXF export/import not externally validated | In progress | v0.8.2 | Compatibility sample folder and validation document have been added; external viewer results are still pending. |
| Testing | No import DXF -> modify -> export workflow test | Completed | v0.8.2 | Added a first simple DXF import -> trim -> export regression test. |
| Architecture | `CadCanvas` is too large and knows concrete tools | In progress | v0.8.3 | Entity rendering has been extracted to `CadEntityRenderer`; active-tool preview rendering has been moved to `CadToolPreviewRenderer`; keyboard delegation is still pending. |
| Architecture | `MainWindow.axaml.cs` duplicates full UI refresh after document replacement | Completed | v0.8.3 | Introduced `RefreshAllUiAfterDocumentChange()` for document replacement refresh paths. |
| UX correctness | Non-associative dimensions can become stale silently | Planned | v0.8.4 | Add a conservative stale marker before attempting associative dimensions. |
| Command UX | Command history navigation with up/down is missing | Planned | v0.8.4 | Reuse existing command history where possible. |
| Command UX | Command autocomplete is missing | Planned | v0.8.4 | Start with simple Tab completion before a visual dropdown. |
| Modify tools | Fillet lacks preview and NoTrim | Planned | v0.8.5 | Preview first, NoTrim second, Line-Arc/Arc-Arc later. |
| Modify tools | Offset lacks miter limit / round join | Planned | v0.8.5 | Introduce join-style planning and miter limit before advanced curve offset. |
| DXF import | LWPOLYLINE bulge is not converted to arcs | Planned | v0.9+ | High-value import improvement after stabilization. |
| DXF import | ELLIPSE/SPLINE import is deferred | Planned | v0.9+ | Export exists; import should be added after compatibility validation. |
| Future feature | Hatch/campiture | Deferred | post-v0.9 | Requires a dedicated entity and render/export strategy. |
| Future feature | Blocks/symbols | Deferred | post-v0.9 | Large model-level feature; should not be mixed with stabilization. |
| Future feature | PNG export | Deferred | post-v0.9 | Useful but lower risk than runtime, testing and DXF validation. |
| Persistence | Autosave and recovery | Deferred | post-v0.9 | Should follow save/reopen and safe-write groundwork. |

---

## v0.8.1 scope

v0.8.1 should remain small and low-risk:

- document the stabilization plan;
- add a top-level exception boundary around canvas pointer input;
- replace the TEXT/MTEXT dialog boolean gate with a reusable reentrancy guard;
- add one end-to-end save/reopen regression test covering the current primary entity set;
- update handoff notes so the next development pass starts from the stabilization track.

No large canvas refactor should be done in v0.8.1.

---

## v0.8.2 scope

Focus on test and DXF validation:

- add draw -> annotate -> export SVG/PDF/DXF workflow tests;
- add import DXF -> modify -> export workflow tests;
- create `samples/dxf/compatibility/` with representative exported files;
- create `docs/dxf-compatibility.md` with external viewer result placeholders;
- document that dimensions are exported as graphical primitives, not native DXF `DIMENSION` entities.

---

## v0.8.3 scope

Start architectural cleanup without changing behavior:

- [x] add `RefreshAllUiAfterDocumentChange()` in `MainWindow.axaml.cs` for New/Open/Import DXF document replacement paths;
- [x] extract entity rendering from `CadCanvas` into a dedicated renderer;
- [x] keep tool behavior unchanged during entity-render extraction;
- [x] extract active-tool preview rendering from `CadCanvas` into `CadToolPreviewRenderer`;
- [x] delegate active-tool keyboard handling through `IKeyboardAwareTool`;
- [ ] replace concrete tool preview dispatch with tool-provided preview descriptors in a later pass.

The second v0.8.3 pass moved entity drawing, text drawing and dimension drawing into `CadEntityRenderer`. The third v0.8.3 pass moved active-tool preview drawing into `CadToolPreviewRenderer`. This pass introduced `IKeyboardAwareTool` so active tools handle their own keyboard-specific actions without `CadCanvas` checking `AlignTool`, `MoveTool`, `CopyTool`, `PolylineTool` or `GripEditTool` directly. `CadCanvas` still owns grid, UCS, snap overlays, crosshair and pointer input. The next architecture pass should focus on replacing the remaining concrete preview dispatch with tool-provided preview descriptors.

---

## v0.8.4 scope

UX correctness and command line improvements:

- add a conservative dimension stale marker;
- render stale dimensions distinctly;
- add a way to mark dimensions as checked;
- implement command history navigation with up/down;
- implement first-pass command autocomplete.

---

## v0.8.5 scope

Modify-tool refinement:

- Fillet preview;
- Fillet NoTrim;
- stronger near-collinear Fillet tests;
- Offset miter limit;
- plan for bevel/round join style.

---

## v0.9 release candidate criteria

Before tagging v0.9, the project should have:

- runtime-safe pointer input;
- TEXT/MTEXT dialog reentrancy protection;
- end-to-end save/reopen test coverage;
- at least one export-oriented end-to-end workflow test;
- documented DXF compatibility checks;
- roadmap and known limitations aligned with the current code;
- explicit known limitation notes for non-associative dimensions and DXF import gaps.
