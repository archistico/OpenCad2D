# OpenCad2D v0.8.100+ roadmap

This document records the extended v0.8 development line after the 2026-06-21 documentation reconciliation. The main roadmap remains `docs/roadmap.md`; this file gives the more detailed v0.8.100+ breakdown.

The v0.8.100+ line has already delivered several foundations that were originally planned as future work. The next step is therefore not another uncontrolled expansion, but a cleanup of planning documents and a focused continuation from a stable baseline.

---

## Versioning policy

Suggested numbering for the active line:

| Version range | Theme | Current status |
|---|---|---:|
| v0.8.100 - v0.8.109 | Import another OpenCad2D drawing into the current document | [x] |
| v0.8.110 - v0.8.119 | Block model, block references and first block UI | [x] |
| v0.8.120 - v0.8.129 | Dynamic HUD, Library Browser and symbol direction | [x] for HUD/Library foundation, [~] for symbol expansion |
| v0.8.130 - v0.8.139 | Parametric stair tools | [x] for straight-stair v1 |
| v0.8.140 - v0.8.149 | Boundary Fill v1/v2 | [x] for filled-polyline workflow |
| v0.8.150 - v0.8.159 | HatchEntity foundation | [ ] |
| v0.8.160 - v0.8.169 | Documentation reconciliation, Library content, shared planning contracts and compatibility pass | [~] — v0.8.160 and v0.8.164 done, v0.8.161 implemented pending manual validation |
| v0.8.170 - v0.8.239 | Next feature foundations: Blocks v2, doors/windows, arrays, hatch patterns, annotations, UI customization and icon SVG workflow | [~] — Blocks v2 slices 170A and 170B implemented, remaining slices planned |

Exact patch numbers can move, but each milestone should remain independently buildable, testable and documented.

---

## Strategic order after reconciliation

The historical order was correct: Import Drawing, Blocks, Dynamic HUD, Library, Stairs and Boundary Fill had to come before larger CAD workflows. The real state now requires a new continuation order:

1. **Documentation reconciliation**.
2. **Small real Library content pack**.
3. **Planning specification pass with shared contracts**.
4. **Blocks v2 slice 170A: Block Manager inventory and diagnostics**.
5. **Compatibility/manual smoke validation**.
6. **Blocks v2 remaining slices: rename/duplicate/purge, edit workflow and Library conflict policy**.
7. **Parametric doors/windows with wall masking and anchor control**.
8. **Array tools**.
9. **HatchEntity and hatch patterns**.
10. **Annotation marker tools: arrows, section labels and coordinate callouts**.
11. **UI customization and icon SVG workflow**.
12. **v0.9 stabilization gate**.

This order keeps dependencies clean. Blocks v2 improves the infrastructure used by Library items and repeated content. Doors/windows depend on reliable parametric insertion, anchor points and layer/line behavior. Arrays should operate on ordinary entities and block references before the library grows too much. HatchEntity should remain separate from Boundary Fill's current filled-polyline model. Annotation marker tools can reuse arrowhead/style/HUD concepts. UI customization and SVG icon import/export should happen after the main tool inventory has stabilized enough to expose configurable workspaces.

---

## Completed v0.8.100+ milestones

### v0.8.100 - v0.8.102 — Import Drawing

Specification: `docs/specs/v0.8.100-import-drawing.md`.

Status: [x].

Implemented behavior:

- import another `.opencad2d.json` drawing into the current document;
- pending insertion point workflow;
- uniform scale and rotation options;
- snap-aware placement;
- undoable merge;
- layer/style/block-safe merge behavior where implemented.

### v0.8.110 - v0.8.115 — Blocks v1

Specification: `docs/specs/v0.8.110-blocks.md`.

Status: [x] for first usable block system.

Implemented behavior:

- `BlockDefinition` and `BlockReferenceEntity` model;
- JSON persistence for definitions and references;
- rendering, hit testing and selection of block references;
- snapping to transformed internal block geometry;
- Create Block from selected entities;
- Insert Block from an existing definition;
- minimal Block Manager for rename, unused delete and insert selected;
- Explode Block;
- first Edit Block session workflow.

Remaining block work is now tracked as Blocks v2, not as unfinished v0.8.110 work.

### v0.8.120 — Symbols and Library direction

Specifications:

- `docs/specs/v0.8.120-architectural-symbols.md`
- `docs/specs/v0.8.122-library-browser.md`

Status: [x] for direction and Library Browser foundation; [~] for actual content breadth.

Implemented behavior:

- first-pass North Symbol;
- first-pass Metric Scale Bar;
- modal Library Browser;
- scan of `library/**/*.opencad2d.json`;
- category grouping;
- preview;
- insertion as block reference;
- snap-aware placement and undo.

Current decision:

- fixed furniture, sanitary fixtures, kitchen objects and reusable drafting snippets should be static Library items;
- direct toolbar tools should be reserved for parametric objects and annotation helpers that need dimensions/options before insertion.

### v0.8.121 — Dynamic Command HUD

Specification: `docs/specs/v0.8.121-dynamic-command-hud.md`.

Status: [x].

Implemented behavior:

- fixed bottom command row removed;
- cursor-adjacent HUD with prompt, tool name, options and editable fields;
- coordinate fields for point phases;
- command-specific scalar fields where appropriate;
- `TAB` enters/cycles edit fields;
- `Enter`/right-click confirms only in valid phases;
- mouse hover over the HUD does not steal canvas input before edit mode;
- Break, Boundary Fill, Trim, Extend, Delete, Explode, Join, Create Block and Insert Block workflows have dedicated HUD handling where needed.

### v0.8.130 — Stair tools v1

Specification: `docs/specs/v0.8.130-stairs.md`.

Status: [x] for straight-stair v1.

Implemented behavior:

- persistent `StairEntity`;
- plan, side and front view generation;
- plan direction arrow and optional section marker;
- Property Panel editing;
- save/reopen;
- SVG/PDF/DXF export as generated linework;
- snap/hit-test behavior through generated stair geometry.

Accepted first-version limitations:

- straight stairs only;
- no L/U stair, landing or winder support yet;
- no building-code validation;
- text labels such as `UP`/`DN` remain deferred.

### v0.8.140 - v0.8.145 — Boundary Fill v1/v2

Specifications:

- `docs/specs/v0.8.140-hatch.md`
- `docs/specs/v0.8.145-boundary-fill-v2.md`

Status: [x] for the filled-polyline Boundary Fill workflow.

Implemented behavior:

- `BFILL` / `FILL` / `RIEMPIMENTO` command aliases;
- click/typed seed point inside a detectable boundary;
- preview before commit;
- `Enter`/right-click confirmation;
- sampled circle, arc and bulged-polyline boundaries;
- editable `Gap` / `G` HUD sub-prompt;
- endpoint-to-endpoint and endpoint-to-segment gap bridging through explicit synthetic segments;
- ignored unsupported-entity diagnostics;
- result created as one filled closed `PolylineEntity`.

Important boundary:

Boundary Fill v2 is complete for the current output model. Holes, islands, associative behavior and hatch patterns must not be forced into this filled-polyline result. They belong to a real `HatchEntity` milestone.

---

## Active consolidation: v0.8.160 - v0.8.169

### v0.8.160 — Documentation reconciliation

Specification: `docs/specs/v0.8.160-documentation-reconciliation.md`.

Status: [x].

Goal: make documentation match the real code state and the new roadmap.

Tasks:

- update `README.md` current status and stabilization checkpoint;
- update `docs/roadmap.md` as source of truth;
- update this v0.8.100+ roadmap;
- update `docs/ai-handoff.md` with the current continuation plan;
- update `docs/known-limitations.md` where old “planned” wording conflicts with completed features;
- add compact specs for the next feature families;
- make clear that v0.9 is a future stabilization gate, not the current active feature bucket.

### v0.8.161 — First Library content pack

Specification: `docs/specs/v0.8.161-library-content-pack.md`.

Status: [~] implemented, pending maintainer-side manual validation.

Goal: make the implemented Library Browser useful with a small curated set of static objects.

Implemented first set:

```text
library/
  arredo/
    tavolo_4_sopra.opencad2d.json
    sedia_sopra.opencad2d.json
    divano_3_sopra.opencad2d.json
  sanitari/
    wc_sopra.opencad2d.json
    bidet_sopra.opencad2d.json
    lavello_sopra.opencad2d.json
  cucina/
    frigo_sopra.opencad2d.json
    lavandino_sopra.opencad2d.json
    fornello_sopra.opencad2d.json
  simboli/
    nord_semplice.opencad2d.json
    scala_grafica_100.opencad2d.json
```

Rules applied:

- the pack is intentionally small and coherent;
- furniture, kitchen and sanitary items use the object center as `(0,0)`;
- symbols use their natural reference as `(0,0)`;
- geometry is simple and first-pass safe;
- no item contains nested block references;
- parametric doors/windows are intentionally excluded from the static Library;
- publish copy still needs to be verified on the maintainer Windows environment.

### v0.8.162 — Compatibility and manual smoke pass

Checklist: `docs/testing/v0.8.162-compatibility-smoke-checklist.md`.

Status: [ ].

Goal: record a practical verification pass before expanding again.

Minimum checks:

- build/test on the maintainer Windows environment;
- save/reopen of drawings using blocks, Library items, stairs, image references and Boundary Fill v2 results;
- SVG/PDF/DXF export smoke pass;
- Library insertion/explode workflow;
- Stairs Property Panel edit/save/export;
- Boundary Fill v2 seed, preview, `Gap`, endpoint-to-endpoint and endpoint-to-segment cases;
- image transparency from Property Panel and Manage Refs;
- block rename/delete/insert/explode/edit workflows;
- DXF compatibility notes with exact viewer versions when checked.

### v0.8.163 — Complex error and edge-case cleanup

Status: [ ].

Goal: identify remaining behaviors that still feel fragile or too complex for users.

Possible areas:

- ambiguous command phases;
- inconsistent confirmation messages;
- invalid geometry recovery;
- import duplicate layer/style/block names;
- block edit conflicts;
- hatch/fill failure diagnostics;
- DXF import/export warnings;
- HUD focus regressions.

This milestone should be evidence-driven: only promote a fix if a real manual case, test failure or confusing workflow has been identified.

### v0.8.164 — Planning specification pass

Specification: `docs/specs/v0.8.164-planning-specification-pass.md`.

Status: [x].

Goal: define shared implementation contracts before the next code milestone.

Documents added:

- `docs/specs/shared-anchor-system.md`;
- `docs/specs/shared-leader-arrow-system.md`;
- `docs/specs/shared-preview-and-commit-workflow.md`;
- `docs/specs/shared-wall-mask-openings.md`.

Documents expanded:

- `docs/specs/v0.8.170-blocks-v2.md`;
- `docs/specs/v0.8.180-parametric-doors-windows.md`;
- `docs/specs/v0.8.190-array-tools.md`;
- `docs/specs/v0.8.200-hatch-patterns.md`;
- `docs/specs/v0.8.210-annotation-markers.md`;
- `docs/specs/v0.8.220-ui-customization.md`;
- `docs/specs/v0.8.230-icon-svg-workflow.md`.

This is documentation-only. It does not replace the v0.8.162 manual validation or v0.8.163 evidence-driven cleanup. It provides the contracts to use when code work resumes.

---

## Next feature milestones

### v0.8.170 — Blocks v2

Specification: `docs/specs/v0.8.170-blocks-v2.md`.

Status: [~] started. Slices `v0.8.170A` and `v0.8.170B` are implemented in code. They need maintainer-side build/test plus manual checklist validation before being considered complete.

Implemented in `v0.8.170A`:

- Block Manager now reports drawing references, nested references and total references.
- Blocks used only inside another block are protected from Delete Unused.
- Selected-block details show entity count, reference counts, bounds and diagnostics.
- Missing drawing/nested block references are reported as diagnostics.
- Empty block definitions are flagged.
- Recursive/self-referencing block definitions are blocking diagnostics and cannot be inserted or accepted from the manager.

Implemented in `v0.8.170B`:

- Duplicate creates a new block definition with a new id, unique `Copy` name and copied internal entity ids.
- Delete Selected still removes only definitions with no drawing or nested references.
- Purge Unused removes every definition not reachable from model-space block references, including unused nested block trees.
- Drawing-reachable nested definitions are preserved.
- Blocking diagnostics can be purged only when the offending definition is not reachable from the drawing.
- Result application still flows through the existing block-definition update command, so the final manager commit remains one undoable block-definition update.

Remaining Blocks v2 work:

- visual preview panel/thumbnail;
- stronger rename UX and validation messaging beyond inline name validation;
- edit-session hardening;
- Library import conflict policy.

Goal: make block management strong enough for architectural objects, Library workflows and repeated technical details.

### v0.8.180 — Parametric doors and windows

Specification: `docs/specs/v0.8.180-parametric-doors-windows.md`.

Goal: add persistent parametric door/window objects with 9-point anchor selection in the HUD and optional wall-line masking/opening behavior at insertion.

### v0.8.190 — Array tools

Specification: `docs/specs/v0.8.190-array-tools.md`.

Goal: add AutoCAD-style rectangular, polar and path arrays: `ARRAYRECT`, `ARRAYPOLAR` and `ARRAYPATH`.

### v0.8.200 — HatchEntity and hatch patterns

Specification: `docs/specs/v0.8.200-hatch-patterns.md`.

Goal: add real hatch entities with solid/pattern modes, outer/inner loops, scale/angle and export behavior.

### v0.8.210 — Annotation markers and arrow tools

Specification: `docs/specs/v0.8.210-annotation-markers.md`.

Goal: add Arrow, Section Label and Coordinate Callout helpers as dynamic annotation tools.

### v0.8.220 — UI customization

Specification: `docs/specs/v0.8.220-ui-customization.md`.

Goal: begin Blender-inspired UI customization without destabilizing the main command workflow.

### v0.8.230 — Icon SVG workflow

Specification: `docs/specs/v0.8.230-icon-svg-workflow.md`.

Goal: export the current icon set as editable SVG, allow user-provided SVG replacement, validate imports and reload icons safely.

---

## Non-goals for the current v0.8 line

The following remain deferred unless explicitly promoted:

- DWG support;
- full AutoCAD compatibility for every hatch pattern and boundary-detection edge case;
- associative hatch updates after boundary edits;
- dynamic block parameters equivalent to AutoCAD dynamic blocks;
- block attributes as a full data system;
- raster image embedding in native files;
- broad 3D features;
- major renderer/spatial-index rewrites without a measured blocker.
