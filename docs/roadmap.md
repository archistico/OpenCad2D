# OpenCad2D roadmap

This roadmap is the current source of truth for OpenCad2D planning after the documentation reconciliation of 2026-06-21.

OpenCad2D remains a native 2D CAD. The project should keep growing in small, testable increments: each milestone must compile, pass the relevant automated tests, update documentation and leave a clear handoff before the next phase begins.

Legend:

```text
[x] completed and stabilized enough for the active line
[~] implemented but still requiring focused validation or polish
[ ] planned
[>] intentionally deferred beyond the current planning horizon
```

---

## Current release target: v0.8 consolidation line

The previous v0.9 stabilization gate is deferred until the expanded v0.8 feature line is reconciled and manually validated. The immediate target is not to add every new feature at once. The target is to consolidate what already exists, correct the planning documents, and then resume feature work from a clear sequence.

Current priority order:

1. **v0.8.161 first real Library content pack** — implemented in the repository as a small curated `library/` set; it still needs maintainer-side manual validation.
2. **v0.8.164 planning specification pass** — completed as a documentation-only contract pass for the next feature families. It defines shared anchor, leader/arrow, preview/commit/undo and wall-mask behavior before code resumes.
3. **v0.8.170A Block Manager inventory and diagnostics** — implemented and locally test-confirmed by the maintainer. It still needs the manual UI checklist.
4. **v0.8.170B Block Manager safe duplicate/purge** — implemented as the second narrow Blocks v2 code slice: duplicate selected definition, delete selected unused definition, bulk purge of definitions not reachable from drawing instances, and safer validation.
5. **v0.8.170C Edit Block session hardening** — implemented as the third narrow Blocks v2 code slice: explicit active state, safe session-scoped save, external-entity guardrail and cancel cleanup for session-created entities.
6. **v0.8.162 compatibility and manual smoke pass** — record save/reopen, SVG/PDF/DXF, Library, Stairs, Boundary Fill v2, image references and block workflows.
7. **v0.8.163 complex/error-prone behavior cleanup** — fix only evidence-backed fragile cases before opening the next large feature family.
8. **v0.8.170D+ feature development** — continue Blocks v2 only in small slices, then resume larger drafting features from a clear baseline.

The next stabilization gate can still be called v0.9, but only after the v0.8.160+ consolidation items are finished and the accepted limitations are explicit.

---

## Completed foundations

The following foundations are considered complete for the active roadmap. Older implementation details are intentionally not repeated here; see Git history, release notes and `docs/ai-handoff.md` for historical implementation logs.

| Area | Status | Notes |
|---|---:|---|
| Core geometry/document model | [x] | Geometry primitives, entities, layers, line formats, text formats, dimension styles, command history and undo/redo are in place. |
| Application shell | [x] | Avalonia canvas, file command bar, top CAD bar, left tool panel, right Property Panel, snap bar, status bar and cursor-adjacent command HUD are established. |
| Native persistence | [x] | `.opencad2d.json` save/load, dirty state, save-changes prompt, partial recovery, viewport/document settings persistence, block definitions, image references and parametric stair persistence are implemented. |
| Export/import baseline | [x] | SVG, PDF and DXF export exist; ASCII DXF import covers the practical 2D entity set currently supported, including LWPOLYLINE bulge preservation for mixed line/arc polylines. |
| Command input and Dynamic HUD | [x] | The old fixed bottom command row has been replaced by the dynamic cursor-adjacent command HUD with contextual prompts, options, editable numeric fields, command aliases, coordinates, relative/polar input, direct distances, history and autocomplete. |
| Drafting aids | [x] | Snap system, grid, Ortho, Polar Tracking, Zoom Window, Zoom Extents, pan, crosshair and SmartPoint Tracking foundation are implemented. |
| Draw tools baseline | [x] | Points, text, MTEXT, lines, rectangles, circles, arcs, ellipses, mixed line/arc polylines, polygons and open Bezier splines are supported. |
| Dimensions baseline | [x] | Horizontal, vertical, aligned, radius, diameter and angular dimensions exist, with style support and conservative stale marking after geometry modifications. |
| Transform tools | [x] | Move, Copy, Rotate, Scale, Mirror and point-based Align are usable and tested. |
| Selection and hit testing | [x] | Selection, Select All, Select Last, Deselect, entity cycling, text/MTEXT bounding-box hit testing and locked/hidden layer behavior are implemented. |
| Native curve editing | [x] | TRIM, BREAK and supported EXTEND flows use native parameters, shared cut points and adapter-backed splitting for supported curves. Mixed polylines preserve bulge segments where supported. |
| Explode / Join essentials | [x] | EXPLODE converts straight/mixed polylines and block references into ordinary world-space entities; JOIN creates one or more connected polylines with bulge preservation where supported. |
| Import Drawing | [x] | Another `.opencad2d.json` drawing can be imported into the current document with insertion point, uniform scale, rotation and undoable merge behavior. |
| Blocks v1 | [x] | Block definitions, block references, Create Block, Insert Block, minimal Block Manager, internal block snapping, Explode Block and first Edit Block workflow are implemented. |
| Library Browser | [x] | Reusable `.opencad2d.json` snippets under `library/` can be grouped, previewed and inserted as block references. The browser foundation is complete; a first content pack now exists and awaits manual validation. |
| External raster references | [x] | PNG/JPG/JPEG references are stored as linked files, transformed as oriented rectangles, snapped, relinked, collected into portable folders and managed with transparency percentage. |
| Parametric stairs | [x] | `StairEntity` supports plan/side/front generation, Property Panel editing, save/reopen and export as generated linework. Straight stairs only remain an accepted first-version limitation. |
| Boundary Fill v2 | [x] | `BFILL` uses preview/confirm, sampled circle/arc/bulged-polyline boundaries, editable `Gap` HUD sub-prompt, endpoint-to-endpoint and endpoint-to-segment gap bridging, and ignored-entity diagnostics. It still outputs a filled closed polyline, not a HatchEntity. |

---

## Immediate reconciliation milestones

| Milestone | Status | Document | Goal |
|---|---:|---|---|
| v0.8.160 | [x] | `docs/specs/v0.8.160-documentation-reconciliation.md` | Roadmap, README, handoff, limitations and future specs have been reconciled with the implemented state. |
| v0.8.161 | [~] | `docs/specs/v0.8.161-library-content-pack.md` | First small static Library content pack has been added under `library/`; manual validation is still required. |
| v0.8.162 | [ ] | `docs/testing/v0.8.162-compatibility-smoke-checklist.md` | Run and record a manual compatibility pass for save/reopen, Library, Stairs, Boundary Fill v2, images, blocks and SVG/PDF/DXF. |
| v0.8.163 | [ ] | `docs/known-limitations.md` | Decide which complex/error-prone behaviors must be fixed before the next public stabilization gate. |
| v0.8.164 | [x] | `docs/specs/v0.8.164-planning-specification-pass.md` | Add shared contracts and expand future specs before writing the next feature code. |
| v0.8.170A | [~] | `docs/specs/v0.8.170-blocks-v2.md` / `docs/testing/v0.8.170A-block-manager-inventory-diagnostics-checklist.md` | First Blocks v2 code slice: richer manager inventory with direct/nested reference counts, selected diagnostics, missing-reference reporting and recursive-reference blocking. Automated tests pass on the maintainer environment; manual UI validation remains. |
| v0.8.170B | [~] | `docs/specs/v0.8.170-blocks-v2.md` / `docs/testing/v0.8.170B-block-manager-duplicate-purge-checklist.md` | Second Blocks v2 code slice: duplicate selected block definitions, delete selected unused definitions, bulk purge drawing-unreachable block definition trees, safe validation and grouped undo through the existing block-definition update result. Automated tests were added; maintainer-side build/test and manual UI validation are required. |
| v0.8.170C | [~] | `docs/specs/v0.8.170-blocks-v2.md` / `docs/testing/v0.8.170C-block-edit-session-hardening-checklist.md` | Third Blocks v2 code slice: Edit Block now has explicit active UI state, Save commits session-scoped entities while ignoring pre-existing external drawing entities, and Cancel removes session-created entities before restoring the original reference. Automated tests were added; maintainer-side build/test and manual UI validation are required. |

This block should be finished before starting another large entity family. It prevents roadmap drift and gives a clean baseline for future work.

---

## Next feature roadmap

The following milestones are the recommended order after v0.8.160+ consolidation. Exact version numbers may move, but the dependency order should remain stable.

Before implementing these milestones, use the shared contracts added by v0.8.164:

- `docs/specs/shared-anchor-system.md`;
- `docs/specs/shared-leader-arrow-system.md`;
- `docs/specs/shared-preview-and-commit-workflow.md`;
- `docs/specs/shared-wall-mask-openings.md`.

| Milestone | Status | Specification | Goal |
|---|---:|---|---|
| v0.8.170 | [~] | `docs/specs/v0.8.170-blocks-v2.md` | Blocks v2 is now started. Slices 170A, 170B and 170C implement inventory/diagnostics, safe duplicate/delete/purge and hardened in-place edit-session scope. Preview thumbnails, stronger rename UX and Library conflict policy remain planned. |
| v0.8.180 | [ ] | `docs/specs/v0.8.180-parametric-doors-windows.md` | Add parametric doors and windows with 9-point anchor selection in the HUD and optional wall-line masking/opening behavior at insertion. |
| v0.8.190 | [ ] | `docs/specs/v0.8.190-array-tools.md` | Add AutoCAD-style array tools: `ARRAYRECT`, `ARRAYPOLAR` and `ARRAYPATH`, with preview, grouped undo and clear explode/edit policy. |
| v0.8.200 | [ ] | `docs/specs/v0.8.200-hatch-patterns.md` | Add real `HatchEntity` behavior with outer/inner loops, solid and patterned fills, scale/angle, preview and export strategy. |
| v0.8.210 | [ ] | `docs/specs/v0.8.210-annotation-markers.md` | Add annotation helpers: Arrow, Section labels, and coordinate callout labels with leader from marker to point. |
| v0.8.220 | [ ] | `docs/specs/v0.8.220-ui-customization.md` | Start Blender-inspired UI customization: icon-only mode, panel visibility/layout preferences, saved workspace state and safer toolbar density options. |
| v0.8.230 | [ ] | `docs/specs/v0.8.230-icon-svg-workflow.md` | Add icon SVG export/import workflow so the current icons can be exported, edited externally and reloaded from user-provided SVG assets. |

### Why this order

Blocks v2 should come before doors/windows, arrays and richer library content because many future objects will either be inserted as block references or will need block-like management. Doors/windows should come before large architectural library expansion because they are one of the few agreed parametric object families, together with stairs. Array tools should come before heavy library authoring because repeated objects and layout grids become much easier to test and draw. HatchEntity and hatch patterns should follow Boundary Fill v2, but should not be mixed back into the filled-polyline `BFILL` output model. Annotation markers and arrow tools can then reuse the same Dynamic HUD, anchor, arrowhead and style concepts. UI customization and icon import/export are important, but they should be implemented after the core drafting workflows are stable enough to expose through customizable workspaces.

---

## Accepted design decisions

These decisions should guide future implementation unless explicitly changed:

- OpenCad2D remains 2D only.
- Export formats remain DXF, SVG, PDF and PNG.
- Fixed furniture, sanitary objects, kitchen objects and general reusable details should be static `.opencad2d.json` Library items.
- Only doors, windows and stairs are approved as persistent parametric architectural objects for now.
- Boundary Fill v2 creates a filled closed `PolylineEntity`; holes, islands and hatch patterns belong to `HatchEntity`.
- The Dynamic HUD is the primary command interaction layer. Mouse and keyboard input must feed the same command/tool state machine.
- HUD text boxes must stay non-interactive until `TAB` enters editing mode. Pointer movement over the HUD must not steal focus or block the canvas.
- Tools that need a picked point should expose coordinate fields. Tools that need a parameter should use explicit options/sub-prompts, as Boundary Fill does with `G`/`Gap`.
- Complex commands should fail conservatively with clear messages rather than creating approximate geometry silently.

---

## v0.9 stabilization gate

After the v0.8.160+ consolidation and the selected next feature milestones, v0.9 should become a stabilization gate rather than a feature bucket.

Candidate v0.9 gates:

- [ ] full build/test pass on the maintainer environment;
- [ ] manual smoke checklist recorded for HUD, snaps, modify tools, Boundary Fill v2, Library, Stairs, images, blocks and exports;
- [ ] DXF/SVG/PDF compatibility pass recorded with exact external viewer versions where applicable;
- [ ] known limitations reviewed and accepted;
- [ ] release artifacts and publish instructions verified;
- [ ] user guide reflects visible behavior, not historical intent.

---

## v1.0 candidate focus

v1.0 should not mean “every CAD feature exists”. It should mean that the chosen OpenCad2D niche is coherent, documented and reliable.

Candidate v1.0 baseline:

- precise 2D drafting and editing workflow;
- stable HUD/command behavior;
- reliable save/reopen and export workflows;
- usable blocks, library and parametric architectural basics;
- documented limits for hatch, arrays, dimensions, imports and advanced curves;
- repeatable release process;
- user-facing documentation good enough for first external users.
