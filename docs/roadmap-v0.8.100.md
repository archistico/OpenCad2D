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
| v0.8.120 - v0.8.129 | Architectural symbols and parametric drafting helpers |
| v0.8.130 - v0.8.139 | Stair tools for plan/elevation/front elevation drafting |
| v0.8.140 - v0.8.159 | Hatch and boundary fill system |
| v0.8.160+ | Consolidation, compatibility, documentation and release gate preparation |

The exact patch numbers can move, but each milestone should remain independently buildable, testable and documented.

## Strategic order

The recommended order is:

1. Import Drawing
2. Blocks
3. Architectural Symbols
4. Stairs
5. Hatch
6. Consolidation

This order is intentional.

Import Drawing is a low-risk foundation for reusing existing work. Blocks should follow because many future symbols should be generated as reusable block definitions. Architectural symbols and stairs can then use the block infrastructure instead of becoming isolated one-off tools. Hatch is deferred because robust boundary recognition is geometrically more complex and should be built on top of a stable entity/document model.

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

Status: planned.

Goal: introduce reusable block definitions and block references.

Initial scope:

- Add `BlockDefinition` to the document model.
- Add `BlockReferenceEntity` as a drawing entity.
- Persist block definitions and block references in `.opencad2d.json`.
- Render block references by transforming definition geometry into world space.
- Support selection, hit testing and basic transforms of block references.
- Support snapping to transformed geometry inside block references.

Exit criteria:

- Multiple references can point to the same definition.
- Editing a definition updates all references after reload/render.
- Block references can be moved, copied, rotated, scaled and mirrored as references.
- Persistence round-trip preserves definitions and references.

---

## Milestone v0.8.115 — Block tools and editing workflow

Specification: `docs/specs/v0.8.110-blocks.md`.

Status: planned.

Goal: make blocks usable from the UI.

Initial scope:

- Create Block from selection.
- Insert Block.
- Edit Block definition in a simple isolated editing workflow.
- Explode Block into regular entities.
- Minimal Block Manager.

Recommended first implementation:

- Use a separate block-editing mode or window instead of in-place editing.
- Avoid nested blocks initially unless the document model naturally permits them safely.
- Defer attributes, dynamic blocks and per-reference layer overrides.

Exit criteria:

- Creating a block removes or optionally keeps the original selection according to a clear prompt.
- Inserting a block creates a reference, not duplicated geometry.
- Editing a definition updates all instances.
- Exploding a block produces regular entities with correct world-space geometry.

---

## Milestone v0.8.120 — Architectural symbols v1

Specification: `docs/specs/v0.8.120-architectural-symbols.md`.

Status: planned.

Goal: add practical drafting helpers for technical and architectural drawings.

Initial scope:

- North symbol.
- Metric scale bar.
- Section marker.
- Elevation marker.
- Simple title block/testalino generator.

Design rule:

These tools should generate normal OpenCad2D entities or block definitions/references. They should not introduce unnecessary special entity types unless a symbol truly needs parametric editing after insertion.

Exit criteria:

- Each symbol is inserted at a picked point.
- Each symbol supports at least scale/size and rotation where relevant.
- Generated geometry is selectable, transformable and exportable through existing mechanisms.
- Symbols can be generated as reusable blocks where useful.

---

## Milestone v0.8.130 — Stair tools v1

Specification: `docs/specs/v0.8.130-stairs.md`.

Status: planned.

Goal: generate stair drawings for plan and elevations.

Initial scope:

- Stair plan.
- Side elevation.
- Front elevation.
- Optional underlying slab/structure line.
- Parameters for riser, tread, width, step count and slab thickness.

Exit criteria:

- The tool can generate a basic straight stair in plan.
- The tool can generate a side elevation with risers/treads.
- The tool can generate a front elevation useful for sections/elevations.
- The slab/structure line can be offset from the inner tread/riser corner by a configurable thickness, defaulting to 25 cm.

---

## Milestone v0.8.140 — Hatch v1

Specification: `docs/specs/v0.8.140-hatch.md`.

Status: planned.

Goal: introduce a robust fill/hatch entity without trying to replicate all AutoCAD boundary detection immediately.

Initial scope:

- `HatchEntity` with explicit loops.
- Solid fill.
- Boundary from selected closed polyline.
- Boundary from selected connected entities where a single loop can be assembled.
- Support lines, polylines, arcs and circles/ellipses through curve sampling where necessary.
- Persistence and SVG/PDF export.

Exit criteria:

- A selected closed polyline can become a hatch boundary.
- A selected set of connected line/arc entities can become a hatch boundary if it forms a valid loop.
- Hatch can contain inner loops for holes in a controlled explicit workflow.
- Hatch rendering honors holes.
- Open or ambiguous boundaries fail with clear messages.

---

## Milestone v0.8.150 — Hatch v2: islands and composite boundaries

Specification: `docs/specs/v0.8.140-hatch.md`.

Status: planned.

Goal: support more realistic hatch regions.

Scope:

- Outer loop plus multiple inner loops.
- Holes made from circles, ellipses, polylines and composed line/arc loops.
- Loop orientation normalization.
- Point-in-polygon/curve containment tests for island classification.
- Conservative tolerance handling.

Deferred beyond v0.8.150:

- Full click-inside automatic boundary detection.
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
