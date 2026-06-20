# OpenCad2D v0.8.100+ roadmap

This document defines the extended v0.8 development line before the next general stabilization release.

The goal of v0.8.100+ is to add the next major drafting foundations while keeping the project in small, testable increments. The v0.9 release gate is intentionally deferred until these foundations have reached a usable and documented baseline.

## Versioning policy

The v0.8 line remains active.

Suggested numbering:

| Version range | Theme |
|---|---|
| v0.8.100 - v0.8.109 | Import another OpenCad2D drawing into the current document |
| v0.8.110 - v0.8.119 | Block model, block references and block editing |
| v0.8.120 - v0.8.129 | Command HUD refactor, library browser, reusable drawing snippets and parametric drafting helpers |
| v0.8.130 - v0.8.139 | Stair tools for plan/elevation/front elevation drafting |
| v0.8.140 - v0.8.159 | Boundary Fill v1/v2 and hatch system |
| v0.8.160+ | Consolidation, compatibility, documentation and release gate preparation |

The exact patch numbers can move, but each milestone should remain independently buildable, testable and documented.

## Strategic order

The recommended order is:

1. Import Drawing
2. Blocks
3. Dynamic Command HUD and command UX unification
4. Drawing Library and parametric helpers
5. Stairs
6. Boundary Fill / Hatch
7. Consolidation

This order is intentional.

Import Drawing is a low-risk foundation for reusing existing work. Blocks should follow because many future symbols should be generated as reusable block definitions. Before adding more UI-heavy drafting workflows, the command input should be consolidated into a dynamic cursor HUD so every command exposes coherent prompt state, options and confirmation behavior. Reusable library items and parametric helpers can then use the block infrastructure instead of becoming isolated one-off tools. Fixed symbols should mostly live as `.opencad2d.json` library snippets, while the Symbols/tools area should be reserved for parametric generators such as doors, windows, stairs or markers that need user-provided dimensions. Boundary Fill should progress conservatively: first create filled polylines from detected linear faces, then add preview/curve/gap support, and only then introduce a true hatch entity for holes and richer hatch behavior.

---

## Milestone v0.8.100 — Import Drawing

Specification: `docs/specs/v0.8.100-import-drawing.md`.

Status: v0.8.102 implemented.

Goal: allow a `.opencad2d.json` file to be imported into the current drawing.

Initial scope:

- Import entities from another native OpenCad2D file.
- Clone imported entities with fresh IDs.
- Merge layers, line formats, text formats and dimension styles safely.
- Resolve external image paths relative to the imported document before inserting them.
- Preserve visual appearance as much as possible.
- v0.8.100: import at origin and undoable merge.
- v0.8.101: insertion point workflow with pending import and Escape cancellation.
- v0.8.102: import options dialog with uniform scale and rotation in degrees.
- Defer live preview and command-line aliases to later refinement passes.

Exit criteria:

- Importing a valid drawing does not replace the current document.
- Existing entities remain unchanged.
- Imported entity IDs do not collide with current entity IDs.
- Imported image references remain valid when possible.
- Undo removes the imported batch as one operation.
- Insertion point can be selected in the canvas, including snap points.
- Scale and rotation options transform the imported entities around source origin before placement.
- Tests cover merge behavior, ID regeneration, pending placement, cancellation, scale and rotation.

---

## Milestone v0.8.110 — Block model

Specification: `docs/specs/v0.8.110-blocks.md`.

Status: implemented through v0.8.115 native block workflow.

Goal: introduce reusable block definitions and block references.

Implemented scope:

- Add `BlockDefinition` to the document model.
- Add `BlockReferenceEntity` as a drawing entity.
- Persist block definitions and block references in `.opencad2d.json`.
- Render block references by transforming definition geometry into world space.
- Support selection, hit testing and basic transforms of block references.
- v0.8.110: add block definitions, block references, persistence and rendering foundation.
- v0.8.111: create block from selection with numeric base point and undo as a single operation.
- v0.8.112: insert existing block definitions with pending insertion-point workflow.
- v0.8.113: add first Block Manager workflow.
- v0.8.114: support snapping to transformed geometry inside block references.
- v0.8.115: add Explode Block and first Edit Block session workflow.

Exit criteria:

- Multiple references can point to the same definition.
- Editing a definition updates all references after reload/render.
- Block references can be moved, copied, rotated, scaled and mirrored as references.
- Persistence round-trip preserves definitions and references.

---

## Milestone v0.8.115 — Block tools and editing workflow

Specification: `docs/specs/v0.8.110-blocks.md`.

Status: implemented for the first native OpenCad2D block workflow.

Goal: make blocks usable from the UI.

Implemented scope:

- Create Block from selection.
- Insert Block.
- Edit Block definition in a first isolated/session workflow.
- Explode Block into regular entities.
- Minimal Block Manager.

Current conservative policy:

- The first edit workflow avoids advanced dynamic-block behavior.
- Nested blocks remain constrained where needed, especially for library snippets.
- Attributes, dynamic blocks and per-reference layer overrides remain deferred.

Exit criteria:

- Creating a block removes or optionally keeps the original selection according to a clear prompt.
- Inserting a block creates a reference, not duplicated geometry.
- Editing a definition updates all instances.
- Exploding a block produces regular entities with correct world-space geometry.


---

## Milestone v0.8.120 — Symbols and library direction

Specifications:

- `docs/specs/v0.8.120-architectural-symbols.md`
- `docs/specs/v0.8.122-library-browser.md`

Status: first-pass Library Browser implemented. North Symbol and Metric Scale Bar remain direct first-pass tools, while future fixed symbols should move through the Library workflow instead of adding many toolbar buttons.

Goal: separate two concepts that should not grow into one overloaded toolbar:

1. **Library items** — reusable fixed drawings stored as `.opencad2d.json` files under a `library/` folder and inserted through a modal Library Browser with preview and categories.
2. **Parametric symbol tools** — true generators that ask for dimensions/options before creating geometry, such as doors, windows, stairs, section markers with configurable labels, or title blocks.

Updated scope:

- Keep existing North Symbol and Metric Scale Bar as useful first-pass tools, but do not keep adding one toolbar button for every fixed symbol.
- Add a `Library` button that opens a modal browser.
- Load `.opencad2d.json` items from `library/<category>/...`.
- Group items by folder/category, for example `arredo`, `simboli`, `sanitari`, `porte-finestre`, `scale`, `annotazioni`.
- Show a vector preview for each item.
- Insert selected items using the existing block infrastructure as reusable block references.
- Reserve the `Symbols`/parametric tools area for objects that need parameters before generation.

Recommended insertion policy:

- Insert library items as a `BlockReferenceEntity` by default, creating or reusing a `BlockDefinition` derived from the source file.
- Preserve layers/formats from the source file where possible.
- Provide an explicit later option to insert exploded geometry when useful.
- Use the item file origin `(0,0)` as the insertion base point.

Exit criteria:

- A Library button opens a modal browser.
- Browser scans the `library/` folder and groups items by category.
- Each valid `.opencad2d.json` item can show a preview.
- User can select an item and insert it by picking a point on the canvas.
- Object snaps work for the insertion point.
- Insertion is undoable as one operation.
- Inserted library items are selectable, movable, rotatable, scalable, copyable and exportable through existing mechanisms.

---

## Milestone v0.8.121 — Dynamic Command HUD

Specification: `docs/specs/v0.8.121-dynamic-command-hud.md`.

Status: implemented for the current command-input scope.

Goal: replace the fixed bottom command row with a cursor-adjacent dynamic HUD while using one coherent command-state contract across all interactive tools.

This milestone is intentionally placed before the Library Browser because it touches the global command UX. It should be stabilized before adding more modal insertion workflows and parametric tools.

Implemented sequence:

- HUD-0: command tool prompt inventory.
- HUD-1: shared `CommandPromptState` cleanup.
- HUD-2: pointer screen position and live measurement data.
- HUD-3: read-only `CommandHudState`.
- HUD-4: visual HUD overlay.
- HUD-5: remove the generic command textbox and fixed bottom command row; keep keyboard command capture as a logical buffer.
- HUD-6: editable numeric fields for primary draw tools.
- HUD-7: transform/modify tool coverage.
- HUD-8: Break Point, Break Segment and Boundary Fill HUD coverage.
- HUD-9: selection-only cleanup for Trim, Extend, Delete, Explode and Join.
- HUD-10: Create Block and Insert Block pending-point `X/Y` coverage.
- HUD-11: documentation cleanup and residual command-line UI removal.

Exit criteria:

- Every primary command-driven tool has a coherent prompt state.
- The HUD follows the cursor and remains clamped inside the drawing area.
- The HUD does not block canvas input.
- There is only one operational command input.
- Existing command aliases, typed coordinates, relative input, polar input, direct distances, history, autocomplete, Enter, right click and Escape behavior still work.
- Manual regression covers draw, dimension, transform, modify, measure, navigation and selection/order tools.
- The fixed command row has been removed after HUD regression and manual workflow checks.

---

## Milestone v0.8.130 — Stair tools v1

Specification: `docs/specs/v0.8.130-stairs.md`.

Status: in progress / first pass implemented.

Goal: generate persistent parametric stair drawings for plan and elevations.

Initial scope:

- Stair plan.
- Side elevation.
- Front elevation.
- Optional underlying slab/structure line.
- Plan direction arrow and optional 30-degree plan section marker.
- Parameters for riser, tread, width, step count, slab thickness and plan annotations.

Exit criteria:

- The tool can insert a persistent parametric straight stair in plan, side elevation or front elevation.
- The tool can generate a side elevation with risers/treads.
- The tool can generate a front elevation useful for sections/elevations.
- The slab/structure line uses a configurable thickness, defaulting to 3 drawing units.
- Plan view can show a direction arrow from first-to-last or last-to-first and an optional 30-degree section marker.
- Native JSON persistence and SVG/PDF/DXF export preserve or emit the stair representation.

---

## Milestone v0.8.140 — Hatch v1

Specification: `docs/specs/v0.8.140-hatch.md`.

Status: partial. Boundary Fill v1 is implemented; HatchEntity remains planned.

Goal: evolve the current solid-fill system into robust boundary fill and hatch workflows without trying to replicate all AutoCAD boundary detection immediately.

Implemented BF v1 scope:

- `BFILL` / `FILL` / `RIEMPIMENTO` command aliases.
- Click inside a closed visible linear boundary.
- Split linear boundaries at intersections and build planar faces.
- Create a new closed `PolylineEntity` for the picked face.
- Set `IsFilled = true`, use the current layer and support undo through `AddEntityCommand`.

BF v2 scope:

- Preview the detected boundary before creation.
- Add sampled arc and circle boundaries while still generating a filled `PolylineEntity`.
- Add configurable small-gap tolerance with conservative failure messages.
- Keep holes/islands deferred until a true hatch entity exists.

HatchEntity scope:

- `HatchEntity` with explicit loops.
- Solid fill.
- Boundary from selected closed polyline.
- Boundary from selected connected entities where a single loop can be assembled.
- Support lines, polylines, arcs and circles/ellipses through curve sampling where necessary.
- Persistence and SVG/PDF export.

Exit criteria:

- BF v1: clicking inside a rectangle made of lines creates a filled closed polyline.
- BF v2: moving the cursor previews the detected boundary before committing.
- BF v2: arc/circle boundaries can participate through documented sampling.
- BF v2: small endpoint gaps can be closed within a configured tolerance, and larger gaps fail clearly.
- HatchEntity: a selected closed polyline can become a hatch boundary.
- A selected set of connected line/arc entities can become a hatch boundary if it forms a valid loop.
- Hatch can contain inner loops for holes in a controlled explicit workflow.
- Hatch rendering honors holes.
- Open or ambiguous boundaries fail with clear messages.

---

## Milestone v0.8.145 — Boundary Fill v2

Specification: `docs/specs/v0.8.140-hatch.md`.

Status: planned.

Goal: improve the existing click-inside BF workflow before introducing a true hatch entity.

Scope:

- Hover/preview of the boundary that would be generated.
- Sampled arc and circle boundary support.
- Configurable gap tolerance for small endpoint gaps.
- Better diagnostics for ambiguous, open or self-intersecting detected regions.

Deferred beyond BF v2:

- Holes/islands.
- Hatch patterns.
- Associativity.
- Full AutoCAD-style boundary detection across arbitrary curves and blocks.

---

## Milestone v0.8.150 — Hatch v2: islands and composite boundaries

Specification: `docs/specs/v0.8.140-hatch.md`.

Status: planned.

Goal: support more realistic hatch regions through a real hatch entity.

Scope:

- Outer loop plus multiple inner loops.
- Holes made from circles, ellipses, polylines and composed line/arc loops.
- Loop orientation normalization.
- Point-in-polygon/curve containment tests for island classification.
- Conservative tolerance handling.

Deferred beyond v0.8.150:

- Fully general click-inside automatic boundary detection beyond the BF v2 supported boundary set.
- Pattern libraries equivalent to AutoCAD `.pat`.
- Associative hatch that automatically updates after boundary edits.

---

## Milestone v0.8.160+ — Consolidation and release gate

Status: planned.

Goal: stabilize the expanded v0.8 line before a future v0.9 release.

Tasks:

- Update README.
- Update architecture, commands, tools, persistence and export docs.
- Update known limitations.
- Add manual regression checklists.
- Verify native save/reopen.
- Verify SVG/PDF/DXF behavior for new entities where supported.
- Decide which limitations remain accepted for the v0.9 release gate.

## Non-goals for v0.8.100+

The following are intentionally deferred unless explicitly promoted later:

- DWG support.
- Full AutoCAD-compatible hatch pattern libraries.
- Fully automatic click-inside boundary detection in the first hatch milestone.
- Dynamic block parameters.
- Block attributes.
- In-place block editing.
- Associative hatch updates after boundary edits.
- Raster image embedding in native files.

## v0.8.110 — Blocks foundation

Implemented as the first structural block milestone:

- `BlockDefinitionId`
- `BlockDefinition`
- `BlockDefinitionCollection`
- `BlockReferenceEntity`
- `CadDocument.BlockDefinitions`
- JSON persistence for block definitions and block references
- canvas rendering of block references by transforming contained entities
- selection/hit testing through the transformed definition bounding box

This milestone originally did not include the full block UI. The follow-up native block workflow is now implemented through v0.8.115:

- create block from selection — implemented in v0.8.111;
- insert block from existing definition — implemented in v0.8.112;
- block manager — implemented in v0.8.113;
- snaps against transformed block contents — implemented in v0.8.114;
- edit block definition workflow and explode block — implemented in v0.8.115.


## v0.8.111 — Create Block from selection

Implemented as the first usable block workflow:

- `Create Block` button in the modify tools area
- options dialog with block name and numeric base point
- creation of a `BlockDefinition` from selected entities in local coordinates
- replacement of the selected entities with a single `BlockReferenceEntity`
- selection of the created reference
- undo as one operation: restore original entities and remove the new definition/reference
- duplicate block names are rejected
- nested block creation is deferred and currently rejected

`v0.8.112 — Insert Block from existing definition` is implemented. It adds a toolbar command, options dialog, pending insertion-point workflow, snap support, Escape cancellation and single-step undo for newly inserted block references.

The follow-up Block Manager, block-internal snapping, Explode Block and first Edit Block workflow are now implemented as part of the completed v0.8.110-v0.8.115 block line.


## Dynamic Command HUD completion note

The dynamic command HUD milestone is complete for the current scope. Step 30E, Step 30F and Step 31 are implemented and covered by targeted ViewModel regression tests: Break Point, Break Segment and Boundary Fill expose their required point/measurement fields; Trim, Extend, Delete, Explode and Join remain selection-only or prompt/options-only; Create Block and Insert Block expose pending-point `X/Y` fields through dedicated placement states.

Future commands should continue using tool/phase-specific HUD behavior with focused regression tests rather than broad generalization of the shared resolver.
