# OpenCad2D AI handoff — 2026-06-21 Blocks v2 170C

This document is the current handoff for the next OpenCad2D development session. It replaces older v0.9-first wording: the active line is now the reconciled v0.8 consolidation line, and the next v0.9 gate is a future stabilization/release checkpoint.

## Current active line

OpenCad2D remains in the v0.8 consolidation line. Documentation reconciliation is complete, a first real Library content pack has been added, and the planning specification pass defines the shared contracts for the next feature families. The first Blocks v2 code slice, v0.8.170A, was added and the maintainer confirmed the automated tests passed after the singular/plural test fix. The second slice, v0.8.170B, added safe duplicate, selected delete and purge operations in the Block Manager. The third slice, v0.8.170C, now hardens the in-place Edit Block session by making the active state explicit, saving only session-scoped entities, ignoring pre-existing external drawing entities and discarding session-created entities on cancel. The next practical validation step is maintainer-side build/test plus the v0.8.170C manual checklist, then the broader v0.8.162 compatibility pass.

The source-of-truth planning documents are:

- `docs/roadmap.md` for the main roadmap;
- `docs/roadmap-v0.8.100.md` for the detailed v0.8.100+ continuation;
- `docs/known-limitations.md` for accepted current limitations;
- `docs/specs/` for feature-specific planning and shared implementation contracts.

## Implemented foundations to treat as complete for current scope

The following areas should not be reopened as if they were still planned foundations:

| Area | Current status |
|---|---|
| Import Drawing | Implemented with insertion point, scale/rotation and undoable merge. |
| Blocks v1 / v2 170A-170C | Blocks v1 remains implemented. The Block Manager now has inventory/diagnostics plus duplicate selected definition, selected delete for fully unreferenced definitions, bulk purge of definitions not reachable from drawing block references and hardened Edit Block session scope. |
| Dynamic Command HUD | Implemented and replaces the old fixed command row. |
| Library Browser | Implemented as a browser for `library/**/*.opencad2d.json` snippets, grouped by category, previewed and inserted as block references. |
| Stairs | Implemented as persistent straight `StairEntity` with plan/side/front generated linework, Property Panel editing, save/reopen and export. |
| External raster references | Implemented with linked PNG/JPG/JPEG files, relative paths, missing-reference workflow, transparency, Collect Refs and Image References Manager. |
| Boundary Fill v2 | Implemented for filled closed polyline output with preview/confirm, sampled curves, editable Gap HUD prompt, endpoint-to-endpoint and endpoint-to-segment gap bridges, and ignored-entity diagnostics. |
| Mixed polyline/curve editing | Stabilized for the current active scope, including bulge preservation where supported. |
| SmartPoint Tracking | Implemented as the current advanced snapping foundation. |

## Immediate next work

The next work should stay focused and evidence-driven.

First, run the automated tests on the maintainer Windows environment. This sandbox did not have `dotnet`, so v0.8.170C still needs local build/test confirmation.

Second, run `docs/testing/v0.8.170C-block-edit-session-hardening-checklist.md`: active-state buttons, save scope, cancel cleanup, external geometry safety and undo after save. Also keep `docs/testing/v0.8.170A-block-manager-inventory-diagnostics-checklist.md` and `docs/testing/v0.8.170B-block-manager-duplicate-purge-checklist.md` as regression checklists.

Third, run `docs/testing/v0.8.162-compatibility-smoke-checklist.md` on the maintainer Windows environment: publish copy, Library insertion/explode, save/reopen, SVG/PDF/DXF, Stairs Property Panel, Boundary Fill v2 Gap cases, image transparency and block workflows.

Fourth, mark v0.8.161 complete only after the Library Browser shows the first pack correctly and publish output contains `library/**` beside the executable.

Fifth, keep v0.8.164 as the design-contract baseline for future code. Before implementing doors/windows, arrays, hatch, arrows/callouts, UI customization or icon SVG workflow, read the relevant feature spec and the shared contracts it references.

Sixth, only promote bugfix work when it is tied to a real failing test, manual sample, confusing workflow or data-corruption risk. Avoid speculative refactors while the v0.8.160+ consolidation is still open.

Seventh, the next recommended Blocks v2 implementation slice after validation is `v0.8.170D`: Library import conflict policy and safe unique-name handling.


## v0.8.170A implementation notes

The first Blocks v2 implementation slice changed only the Block Manager surface and tests. It does not implement duplicate, bulk purge, edit-session changes, Library conflict replacement or preview thumbnails yet.

Implemented behavior:

- Block Manager now separates drawing references, nested references and total references.
- A block used only inside another block is no longer considered deletable.
- The selected block details panel summarizes entity count, drawing refs, nested refs, total refs, bounds and diagnostics.
- Document-level diagnostics report block references that point to missing definitions, including references found inside block definitions.
- Empty block definitions are marked as diagnostic issues.
- Recursive/self-referencing block definitions are marked as blocking diagnostics and cannot be inserted or accepted from the manager.
- Automated tests were added for direct/nested counts, missing-reference diagnostics, empty definitions, recursive definitions and nested-use delete rejection.

## v0.8.170B implementation notes

The second Blocks v2 implementation slice adds manager-side duplicate and purge behavior. The accepted semantic is important: `Delete Selected` is conservative and only removes a selected definition with zero drawing and nested references, while `Purge Unused` removes every block definition that is not reachable from model-space block references. This allows unused parent/child block trees to be purged together without breaking drawing-used nested blocks.

Implemented behavior:

- Duplicate creates a new definition with a new `BlockDefinitionId`, unique `Copy` name and copied internal entity ids.
- Existing references stay attached to the original block definition.
- Delete Selected still rejects drawing-used or nested-used definitions.
- Purge Unused follows drawing block references through nested definitions and retains the reachable set.
- Unreachable invalid/recursive definitions can be removed by purge as a recovery path.
- The final accepted manager result still flows through `UpdateBlockDefinitionsCommand`, so OK after duplicate/purge is one undoable block-definition update.
- Automated tests were added for duplicate, duplicate rejection on blocking diagnostics, purge of drawing-unreachable block trees, preservation of drawing-reachable nested definitions and result composition.


## v0.8.170C implementation notes

The third Blocks v2 implementation slice hardens the in-place Edit Block workflow. The previous behavior relied too much on the current selection at save time, which made it possible to accidentally absorb unrelated model-space entities into the block definition. The accepted semantic is now session-scoped: entities that existed before the edit session started remain external drawing geometry, while temporary block contents and entities created during the session belong to the edit session.

Implemented behavior:

- `BlockEditSession` now records the model-space entity ids that existed at session start.
- `SaveActiveBlockEdit` rebuilds the definition from session-scoped non-block entities only.
- Pre-existing external drawing entities are ignored even if selected when Save is invoked.
- Entities created during the session are included in Save, so new geometry can be added to a block by drawing it while the session is active.
- `CancelActiveBlockEdit` removes both the temporary editable block contents and session-created entities, then restores the original block reference.
- Main UI buttons now expose explicit state: Edit is disabled during an active session; Save and Cancel are disabled outside one.
- Automated tests were added for external selection safety, created-entity save behavior and created-entity cancel cleanup.

Known limitation for this slice: nested `BlockReferenceEntity` instances created during an edit session are not committed into the rebuilt definition yet. Keep nested-block edit/import rules for a later Blocks v2 slice.

Known remaining Blocks v2 work:

- visual preview thumbnail/panel;
- stronger rename UX beyond inline validation;
- nested-block edit/import policy;
- Library same-name conflict policy and structural-equivalence handling.

## Shared contracts added by v0.8.164

The planning specification pass added these implementation contracts:

| Contract | File | Applies to |
|---|---|---|
| 9-point anchor system | `docs/specs/shared-anchor-system.md` | Blocks, Library insertion, doors/windows, symbols, callouts and future insertable objects. |
| Leader and arrowhead system | `docs/specs/shared-leader-arrow-system.md` | Arrow, Section Label, Coordinate Callout and future annotation/dimension helpers. |
| Preview/commit/grouped undo workflow | `docs/specs/shared-preview-and-commit-workflow.md` | Blocks v2, arrays, doors/windows, HatchEntity, Library insertion and annotation tools. |
| Wall mask/opening behavior | `docs/specs/shared-wall-mask-openings.md` | Door/window visual openings and future explicit wall-cutting commands. |

Key decisions: first arrays are non-associative and emit real copies; first door/window wall openings are non-destructive masks; HatchEntity is separate from Boundary Fill v2; UI and icon preferences are local application settings, not drawing-file data.

## New planned feature sequence

The current recommended order after consolidation is:

| Milestone | Specification | Summary |
|---|---|---|
| v0.8.170 | `docs/specs/v0.8.170-blocks-v2.md` | Blocks v2: richer manager, edit-session safety, purge/preview diagnostics, Library naming/conflict behavior. |
| v0.8.180 | `docs/specs/v0.8.180-parametric-doors-windows.md` | Parametric doors/windows with 9-point anchor selector and optional wall-line masking/opening behavior. |
| v0.8.190 | `docs/specs/v0.8.190-array-tools.md` | `ARRAYRECT`, `ARRAYPOLAR`, `ARRAYPATH` with preview and grouped undo. |
| v0.8.200 | `docs/specs/v0.8.200-hatch-patterns.md` | Real HatchEntity with solid/pattern modes, outer/inner loops, scale/angle and export strategy. |
| v0.8.210 | `docs/specs/v0.8.210-annotation-markers.md` | Arrow, Section Label and Coordinate Callout tools. |
| v0.8.220 | `docs/specs/v0.8.220-ui-customization.md` | Icon-only mode, panel/workspace preferences and reset layout. |
| v0.8.230 | `docs/specs/v0.8.230-icon-svg-workflow.md` | Export current icons to SVG and import validated custom SVG replacements. |

## Key design decisions to preserve

OpenCad2D is 2D only. Export targets remain DXF, SVG, PDF and PNG. Fixed reusable objects belong in the Library as `.opencad2d.json` snippets. Only doors, windows and stairs are approved as persistent parametric architectural objects for now.

Boundary Fill v2 must not be overloaded with holes, islands or patterns. Its completed contract is a previewed filled closed polyline workflow. Holes, islands, pattern scale/angle and hatch-specific persistence belong to HatchEntity.

The Dynamic HUD is the primary interaction layer. Text boxes must not become interactive until `TAB` enters editing mode. Tools that need a scalar value should use explicit options or sub-prompts, like Boundary Fill does with `Gap` / `G`. Mouse movement over the HUD must not steal focus or block canvas input before edit mode.

Complex geometry commands should fail conservatively with clear messages rather than silently creating misleading approximate geometry.

## Files updated in this pass

Documentation and planning/code updates:

- `src/OpenCad2D.App/BlockManagerWindow.axaml`
- `src/OpenCad2D.App/ViewModels/Blocks/BlockManagerWindowViewModel.cs`
- `src/OpenCad2D.App/ViewModels/Blocks/EditableBlockDefinitionViewModel.cs`
- `tests/OpenCad2D.App.Tests/BlockManagerWindowViewModelTests.cs`
- `docs/testing/v0.8.170A-block-manager-inventory-diagnostics-checklist.md`
- `docs/testing/v0.8.170B-block-manager-duplicate-purge-checklist.md`

Previous documentation and planning updates retained in this package:

- `README.md`
- `docs/roadmap.md`
- `docs/roadmap-v0.8.100.md`
- `docs/ai-handoff.md`
- `docs/index.md`
- `docs/developer/technical-documentation-map.md`
- `docs/developer/release-and-roadmap-map.md`
- `docs/specs/v0.8.164-planning-specification-pass.md`
- `docs/specs/shared-anchor-system.md`
- `docs/specs/shared-leader-arrow-system.md`
- `docs/specs/shared-preview-and-commit-workflow.md`
- `docs/specs/shared-wall-mask-openings.md`
- `docs/specs/v0.8.170-blocks-v2.md`
- `docs/specs/v0.8.180-parametric-doors-windows.md`
- `docs/specs/v0.8.190-array-tools.md`
- `docs/specs/v0.8.200-hatch-patterns.md`
- `docs/specs/v0.8.210-annotation-markers.md`
- `docs/specs/v0.8.220-ui-customization.md`
- `docs/specs/v0.8.230-icon-svg-workflow.md`

Library content added:

- `library/README.md`
- `library/arredo/divano_3_sopra.opencad2d.json`
- `library/arredo/sedia_sopra.opencad2d.json`
- `library/arredo/tavolo_4_sopra.opencad2d.json`
- `library/cucina/fornello_sopra.opencad2d.json`
- `library/cucina/frigo_sopra.opencad2d.json`
- `library/cucina/lavandino_sopra.opencad2d.json`
- `library/sanitari/bidet_sopra.opencad2d.json`
- `library/sanitari/lavello_sopra.opencad2d.json`
- `library/sanitari/wc_sopra.opencad2d.json`
- `library/simboli/nord_semplice.opencad2d.json`
- `library/simboli/scala_grafica_100.opencad2d.json`
