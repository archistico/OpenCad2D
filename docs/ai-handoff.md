# OpenCad2D AI handoff — 2026-06-21 v0.8.180G Door/window HUD status closeout

This document is the current handoff for the next OpenCad2D development session. It replaces older v0.9-first wording: the active line is now the reconciled v0.8 consolidation line, and the next v0.9 gate is a future stabilization/release checkpoint.

## Current active line

OpenCad2D remains in the v0.8 consolidation line. Documentation reconciliation is complete, a first real Library content pack has been added, and the planning specification pass defines the shared contracts for the next feature families. Blocks v2 now has five narrow implementation slices: v0.8.170A inventory/diagnostics, v0.8.170B duplicate/delete/purge, v0.8.170C edit-session hardening, v0.8.170D Library/block conflict policy and v0.8.170E rename closeout polish. The parametric doors/windows line has now started in code: v0.8.180A adds the shared 9-point anchor model and resolver, v0.8.180B adds the reusable Dynamic HUD 3x3 anchor selector foundation, v0.8.180C adds the first persistent `DoorEntity`, v0.8.180D adds non-destructive wall-opening masks for doors, and v0.8.180E adds the first persistent schematic `WindowEntity` using the same anchor and wall-mask contracts, v0.8.180F adds Property Panel editing plus per-command insertion defaults for the minimal door/window pair, and v0.8.180G makes the effective insertion state visible in the HUD prompt before commit.

The block/base-point invariant remains explicit and must be preserved: normal block references and static Library items are inserted by the base point chosen when the block definition/item is created, not by a derived 9-point bounding-box anchor. The 9-point anchor selector is for parametric/annotation tools such as doors, windows and future callouts. The next practical validation step is local build/test plus the v0.8.180D, v0.8.180E and v0.8.180F checklists, followed by the expanded v0.8.162 compatibility pass. The next feature slice after validation should be decided deliberately: either door/window polish if smoke finds issues, or the next planned family such as Array tools.

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
| Blocks v1 / v2 170A-170E | Blocks v1 remains implemented. The Block Manager now has inventory/diagnostics, duplicate selected definition, selected delete for fully unreferenced definitions, bulk purge of definitions not reachable from drawing block references, hardened Edit Block session scope, Library/block conflict handling and rename closeout polish. |
| Shared anchor foundation v0.8.180A | Implemented in core as `AnchorPoint`, `AnchorPointDescriptor`, `AnchorPlacement` and `AnchorPointService`, with tests for CAD-coordinate anchor resolution, 3x3 grid order, keypad shortcuts, placement translation and invalid persisted value recovery. The shared anchor system is for future parametric/annotation entities, not for changing block base-point insertion. |
| HUD anchor selector foundation v0.8.180B | Implemented in the app layer as `CommandHudAnchorSelectorViewModel`, row/option view-models, hidden-by-default HUD XAML binding and keyboard shortcut helpers in `MainWindowViewModel`. It does not yet opt any existing command into anchor-based placement. |
| Dynamic Command HUD | Implemented and replaces the old fixed command row. |
| Library Browser | Implemented as a browser for `library/**/*.opencad2d.json` snippets, grouped by category, previewed and inserted as block references. |
| Stairs | Implemented as persistent straight `StairEntity` with plan/side/front generated linework, Property Panel editing, save/reopen and export. |
| External raster references | Implemented with linked PNG/JPG/JPEG files, relative paths, missing-reference workflow, transparency, Collect Refs and Image References Manager. |
| Boundary Fill v2 | Implemented for filled closed polyline output with preview/confirm, sampled curves, editable Gap HUD prompt, endpoint-to-endpoint and endpoint-to-segment gap bridges, and ignored-entity diagnostics. |
| Mixed polyline/curve editing | Stabilized for the current active scope, including bulge preservation where supported. |
| SmartPoint Tracking | Implemented as the current advanced snapping foundation. |

## Immediate next work

First, run the maintainer-side build and test suite after applying the v0.8.180G patch:

```bash
dotnet build OpenCad2D.sln
dotnet test OpenCad2D.sln --no-build
```

Second, run `docs/testing/v0.8.180D-door-wall-mask-foundation-checklist.md` and `docs/testing/v0.8.180E-minimal-window-entity-checklist.md`. The most important manual checks are: wall linework is visually hidden only when the door mask is enabled; `M = Mask` toggles the inserted door default; the Property Panel `Wall mask` combo updates the selected door and supports undo; save/reopen preserves masked and unmasked doors; SVG/PDF/DXF output contains the expected wipeout-style mask for masked doors only.

Third, keep the v0.8.180C checklist as a regression checklist for the base `DoorEntity` behavior: insertion, aliases, HUD anchor selector, persistence and exports must still work after the mask change.

Fourth, run `docs/testing/v0.8.162-compatibility-smoke-checklist.md` on the maintainer Windows environment before opening the next major feature slice. This is the gate for publish copy, Library insertion/explode, save/reopen, SVG/PDF/DXF/PNG, Stairs Property Panel, Boundary Fill v2 Gap cases, image transparency and block workflows.

Fifth, if the above is clean, run the v0.8.180F and v0.8.180G checklists and decide whether the minimal door/window pair needs another polish slice. Do not start HatchEntity or UI customization until the minimal door/window pair and their shared anchor/wall-mask contracts are validated.

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

Known remaining non-blocking Blocks v2 polish:

- visual preview thumbnail/panel;
- deeper nested-block edit/import policy if needed before doors/windows.

## v0.8.180A — Shared anchor foundation

This slice starts the parametric doors/windows line by turning the 9-point anchor planning contract into core code. The implementation lives under `src/OpenCad2D.Core/Anchors/`.

Implemented behavior:

- `AnchorPoint` defines the nine canonical values.
- `AnchorPointDescriptor` exposes stable key, display name, 3x3 UI row/column and keypad shortcut metadata.
- `AnchorPointService.Descriptors` returns anchors in visual grid order.
- `AnchorPointService.GetPoint` resolves anchors against a CAD-oriented `BoundingBox2D`: top is `MaxY`, bottom is `MinY`.
- `AnchorPointService.TryFromNumericShortcut` uses keypad mapping `7/8/9`, `4/5/6`, `1/2/3`.
- `AnchorPointService.GetTranslationToPlaceAnchor` and `CreatePlacement` provide a reusable placement calculation for future insertable/parametric tools.
- Invalid persisted values can recover to a caller-provided default through `ParseOrDefault`.
- Automated tests were added in `AnchorPointServiceTests`.

Out of scope for this slice: visible HUD 3x3 selector, Property Panel anchor editor, entity persistence changes, changed Library/block insertion behavior, DoorEntity, WindowEntity and wall masks.

## Shared contracts added by v0.8.164

The planning specification pass added these implementation contracts:

| Contract | File | Applies to |
|---|---|---|
| 9-point anchor system | `docs/specs/shared-anchor-system.md` | Blocks, Library insertion, doors/windows, symbols, callouts and future insertable objects. |
| Leader and arrowhead system | `docs/specs/shared-leader-arrow-system.md` | Arrow, Section Label, Coordinate Callout and future annotation/dimension helpers. |
| Preview/commit/grouped undo workflow | `docs/specs/shared-preview-and-commit-workflow.md` | Blocks v2, arrays, doors/windows, HatchEntity, Library insertion and annotation tools. |
| Wall mask/opening behavior | `docs/specs/shared-wall-mask-openings.md` | Door/window visual openings and future explicit wall-cutting commands. |

Key decisions: first arrays are non-associative and emit real copies; first door/window wall openings are non-destructive masks; HatchEntity is separate from Boundary Fill v2; UI and icon preferences are local application settings, not drawing-file data.

## v0.8.162 compatibility gate

The v0.8.162 checklist has been expanded after Blocks v2 170A-170E. Use `docs/testing/v0.8.162-compatibility-smoke-checklist.md` as the runnable checklist and `docs/testing/v0.8.162-compatibility-smoke-report-template.md` as the report template.

The pass should create one representative smoke drawing with ordinary entities, text/MTEXT, dimensions, a manually created block, inserted block references, Library items from multiple categories, a StairEntity, an image with non-default opacity and a Boundary Fill v2 result using a non-zero Gap. That drawing should then be saved/reopened and exported to SVG, PDF, DXF and PNG.

Do not open v0.8.180 doors/windows until this pass is completed or explicitly waived. Any failure must be classified as immediate v0.8.163 cleanup, accepted limitation or environmental retest.

## New planned feature sequence

The current recommended order after consolidation is:

| Milestone | Specification | Summary |
|---|---|---|
| v0.8.170 | `docs/specs/v0.8.170-blocks-v2.md` | Blocks v2: richer manager, edit-session safety, purge/preview diagnostics, Library naming/conflict behavior. |
| v0.8.180A | `docs/specs/v0.8.180A-shared-anchor-foundation.md` | Shared 9-point anchor model/resolver for future parametric insertion tools. |
| v0.8.180 | `docs/specs/v0.8.180-parametric-doors-windows.md` | Parametric doors/windows with 9-point anchor selector and optional wall-line masking/opening behavior. |
| v0.8.190 | `docs/specs/v0.8.190-array-tools.md` | `ARRAYRECT`, `ARRAYPOLAR`, `ARRAYPATH` with preview and grouped undo. |
| v0.8.200 | `docs/specs/v0.8.200-hatch-patterns.md` | Real HatchEntity with solid/pattern modes, outer/inner loops, scale/angle and export strategy. |
| v0.8.210 | `docs/specs/v0.8.210-annotation-markers.md` | Arrow, Section Label and Coordinate Callout tools. |
| v0.8.220 | `docs/specs/v0.8.220-ui-customization.md` | Icon-only mode, panel/workspace preferences and reset layout. |
| v0.8.230 | `docs/specs/v0.8.230-icon-svg-workflow.md` | Export current icons to SVG and import validated custom SVG replacements. |


## v0.8.170D — Library/block conflict policy

The fourth Blocks v2 slice implemented deterministic Library insertion conflict handling. `LibraryBlockDefinitionBuilder` now prepares the incoming Library item in the target document context, compares the prepared block entities against existing definitions while ignoring regenerated entity ids, and chooses between reuse or safe duplication.

The policy is non-destructive. Same item id plus equivalent content reuses the existing definition. Same item id plus changed content creates a unique id/name pair. Same block name plus equivalent content reuses the existing definition even if the item id differs. Same block name plus different content creates a unique name. Library insertion still never replaces an existing definition silently; explicit replace remains deferred to a future Block Manager-only operation.

Automated tests were added in `MainWindowViewModelLibraryTests` for changed same-id Library items and same-name equivalent definitions with different item ids. Maintainer-side build/test and manual checklist validation are required for this slice.

## v0.8.170E — Block Manager rename closeout

The fifth Blocks v2 slice closes the manager-side rename polish before moving on to the next feature family. Rename remains inline in the `Name` column, but it now has explicit pending state. The manager summary includes pending rename count, the selected-block details show the original name when a block has been renamed, and the new `Reset Names` button restores all edited block names before the manager result is committed.

Implemented behavior:

- `EditableBlockDefinitionViewModel` tracks `OriginalName`, `IsRenamed` and `RenameStatusText`.
- `BlockManagerWindowViewModel` exposes `PendingRenameCount`, `RenameSummaryText`, `HasPendingRenames`, `CanResetBlockNames` and `ResetBlockNames()`.
- Renaming clears stale validation messages and refreshes summary/detail state while typing.
- `OK` still validates empty and duplicate names and returns trimmed names.
- Block references remain id-based, so rename does not invalidate existing references.
- Automated tests were added for pending rename reporting, reset names and trimmed committed rename output.

Remaining non-blocking Blocks v2 polish: visual preview thumbnails and deeper nested-block editing/import policy.

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
- `src/OpenCad2D.App/ViewModels/Library/LibraryBlockDefinitionBuilder.cs`
- `tests/OpenCad2D.App.Tests/MainWindowViewModelLibraryTests.cs`
- `docs/testing/v0.8.170A-block-manager-inventory-diagnostics-checklist.md`
- `docs/testing/v0.8.170B-block-manager-duplicate-purge-checklist.md`
- `docs/testing/v0.8.170C-block-edit-session-hardening-checklist.md`
- `docs/testing/v0.8.170D-library-block-conflict-policy-checklist.md`
- `docs/testing/v0.8.170E-block-manager-rename-closeout-checklist.md`
- `src/OpenCad2D.Core/Anchors/AnchorPoint.cs`
- `src/OpenCad2D.Core/Anchors/AnchorPointDescriptor.cs`
- `src/OpenCad2D.Core/Anchors/AnchorPlacement.cs`
- `src/OpenCad2D.Core/Anchors/AnchorPointService.cs`
- `tests/OpenCad2D.Core.Tests/AnchorPointServiceTests.cs`
- `docs/specs/v0.8.180A-shared-anchor-foundation.md`
- `docs/testing/v0.8.180A-shared-anchor-foundation-checklist.md`
- `docs/testing/v0.8.162-compatibility-smoke-checklist.md`
- `docs/testing/v0.8.162-compatibility-smoke-report-template.md`

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

## v0.8.180B — HUD anchor selector foundation

This slice adds the first reusable Dynamic HUD surface for the 9-point anchor model without changing any existing tool placement behavior. It lives in the app view-model layer and is hidden by default until a future command explicitly opts into anchor-edit mode.

Implemented behavior:

- `CommandHudAnchorOptionViewModel` represents one selector cell and exposes anchor key, display name, row, column, keypad shortcut, compact label and selected marker.
- `CommandHudAnchorRowViewModel` groups three options for XAML rendering.
- `CommandHudAnchorSelectorViewModel.Hidden` keeps existing HUD states backward-compatible.
- `CommandHudAnchorSelectorViewModel.Create(...)` builds the visual 3x3 selector using `AnchorPointService.Descriptors`, so the UI does not duplicate the anchor order or keypad mapping.
- `CommandHudStateViewModel.AnchorSelector` exposes the selector to the HUD, defaulting to hidden.
- `MainWindow.axaml` includes a hidden-by-default selector block inside the existing command HUD. The overlay remains mouse-inaccessible.
- `MainWindowViewModel` stores the currently selected HUD anchor and exposes shortcut helpers, but `IsCommandHudAnchorSelectorActive()` intentionally returns false in this slice.
- Existing block and Library insertion semantics are unchanged: both continue to use the block/item base point chosen at creation/origin time.
- Automated tests were added in `CommandHudAnchorSelectorViewModelTests`.

Out of scope for this slice: `DoorEntity`, `WindowEntity`, wall masking, Property Panel anchor editing and JSON persistence. Anchor-aware block/Library insertion is not planned for normal blocks because their insertion reference remains the creation base point.

Historical next step for this slice was v0.8.180C. That slice is now implemented, followed by v0.8.180D door masking; the remaining next feature slice is v0.8.180E minimal `WindowEntity`.



## v0.8.180C — Minimal DoorEntity

Implemented as the first code slice after the shared anchor foundations. The slice adds `DoorEntity` in the core architectural model, generated single-swing plan geometry, `DoorTool`, command aliases `DOOR`, `PORTA` and `DR`, UI button/icon wiring, HUD anchor selector opt-in only while the door tool is active, JSON persistence, SVG/PDF/DXF linework export and automated tests across core, persistence, tools and app HUD integration.

Important semantic decision: block and Library insertion still use their creation/origin base point. The HUD 3x3 anchor selector is now active for `DoorTool`, but it must not affect ordinary block references. Door anchors are resolved against the wall-opening footprint, not the full swing arc, so the default `MiddleLeft` anchor acts as the hinge/mid-wall point.

Out of scope for this historical slice: wall masking, automatic wall detection, real wall trimming, width/thickness/angle HUD editing, property-panel editing and window entities. v0.8.180D implements the first non-destructive wall mask for doors; the remaining next slice is v0.8.180E minimal `WindowEntity`.

## v0.8.180D — Door wall-mask foundation

This slice adds the first non-destructive wall-opening behavior to `DoorEntity`. Doors now persist `MaskWallOpening`, defaulting to enabled for new and legacy-missing JSON data. Generated door geometry exposes a wall-mask polygon based on the opening width and wall thickness, transformed by the same local axes/anchor placement as the visible door linework.

Implemented behavior:

- `DoorEntity` includes `MaskWallOpening` and preserves it through transforms, `WithParameters`, native JSON round-trip and DTO default recovery.
- `DoorGeometry` exposes `WallMaskPolygon` and `HasWallMask`.
- `DoorTool` exposes `M = Mask`, toggling the default for subsequent insertion while keeping single-click insertion and preview behavior.
- The Property Panel exposes `Wall mask: Yes/No` and replaces the selected door undoably.
- Canvas rendering draws the mask before the visible door linework.
- SVG/PDF/DXF export writes a white wipeout-style polygon/hatch before the generated door linework.
- Automated tests cover geometry mask generation, disabled masks, persistence, tool toggle, Property Panel editing and SVG/DXF export behavior.

Known limitations: the mask is visual/export-only and does not trim or split wall entities. It only covers linework drawn before the door according to draw order. SVG/PDF/DXF use a white wipeout-style shape, which is suitable for paper-style output but can be visible in transparent-background SVG workflows.

Recommended next slice: `v0.8.180E` minimal `WindowEntity`, reusing the same anchor and wall-mask contracts.



## v0.8.180E — Minimal WindowEntity

Implemented as the first persistent parametric window slice. The new `WindowEntity` stores insertion point, width, wall thickness, frame offset, anchor, mask flag and local axes. `WindowTool` inserts it with `WINDOW`/`FINESTRA`/`WN`, uses the HUD anchor selector, supports `M = Mask`, exposes preview geometry, persists through JSON and exports as normal linework plus optional wipeout-style mask. Blocks and static Library items continue to use base-point insertion.


## v0.8.180F — Door/window property editing and command defaults

This slice adds Property Panel editing for the core `DoorEntity` and `WindowEntity` parameters and per-tool command defaults. Doors can edit insertion, anchor, width, wall thickness, opening angle, swing and wall mask. Windows can edit insertion, anchor, width, wall thickness, frame offset and wall mask. The `DOOR` command now supports Width, Thickness and Opening numeric sub-prompts; the `WINDOW` command now supports Width, Thickness and Offset numeric sub-prompts. These defaults are per active tool instance and are not yet saved as application presets.

## v0.8.180G — Door/window HUD status closeout

This small slice makes the current insertion state visible in the `DOOR` and `WINDOW` prompts. `DOOR` now reports width, wall thickness, opening angle, swing, anchor and wall-mask state. `WINDOW` now reports width, wall thickness, frame offset, anchor and wall-mask state. The change is deliberately HUD-only: it does not alter geometry, persistence, block insertion, Library insertion or wall-mask semantics.
