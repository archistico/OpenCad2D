# OpenCad2D AI handoff

This file records the current working state for the next AI/developer session. It is intentionally concise. Older detailed milestone history should be read from Git history, release notes, roadmap documents and the focused technical documents in `docs/`.

## Current project direction

OpenCad2D remains a native 2D CAD. The project scope is deliberately limited to 2D drafting, editing, dimensions, layers, reusable objects, external raster references and export/import workflows. The supported export targets remain DXF, SVG, PDF and PNG.

The active planning source is `docs/roadmap.md`. The v0.8.100+ expansion line has already completed the main reusable-foundation work through Import Drawing, native Blocks, Dynamic Command HUD and first-pass Library Browser. The next release gate is still a stabilization/consolidation gate rather than a broad feature-growth gate.

## Current completed foundations

The following foundations are considered implemented and should not be reopened as planned milestones unless a concrete bug or regression appears:

- core geometry/document model, persistence, undo/redo and application shell;
- SVG/PDF/DXF/PNG export baseline and practical ASCII DXF import baseline;
- command aliases, coordinate input, relative/polar input, direct distance input, autocomplete/history and Dynamic Command HUD;
- snap system, grid, Ortho, Polar Tracking, SmartPoint Tracking and opt-in Nearest snapping;
- draw tools baseline: point, text, MTEXT, line, rectangle, circle, arc, ellipse, mixed line/arc polyline, polygon and open Bezier spline;
- dimension baseline: horizontal, vertical, aligned, radius, diameter and angular dimensions;
- transform tools: Move, Copy, Rotate, Scale, Mirror and Align;
- modify tools: Delete, Deselect, Explode, Join, Offset, Fillet, Chamfer, Trim, Extend, Break Point, Break Segment and Divide;
- native curve editing for supported curves, including ellipse/elliptical arc/open Bezier split paths where implemented;
- Import Drawing with placement, scale and rotation options;
- native blocks: BlockDefinition, BlockReferenceEntity, Create Block, Insert Block, Block Manager, block-internal snapping, Explode Block and first Edit Block workflow;
- first-pass Library Browser for static `.opencad2d.json` snippets inserted as reusable block references;
- external PNG/JPG/JPEG raster references with transform, relink/replace, missing-reference handling, relative paths, Collect Refs, Manage Refs and transparency percentage;
- Boundary Fill v1 for visible linear closed boundaries that create filled closed polylines;
- geometry/intersection stabilization for clockwise arcs crossing 0°/360°, finite overlap boundary detection, Trim overlap cuts, Break regressions and Extend same-support boundary candidates.

## Current roadmap status

Use this status before proposing new work:

| Area | Current status |
|---|---|
| Import Drawing | Done |
| Blocks native model/workflows | Done for first pass |
| Dynamic Command HUD | Done for current scope |
| Library Browser | Done for first pass; library content/package quality can still be expanded |
| Architectural symbols/helpers | Partial: North Symbol and Metric Scale Bar exist; doors/windows/stairs should be parametric, general fixed objects should remain Library items |
| Boundary Fill | v1 done; v2 planned |
| HatchEntity | Planned |
| Stairs | Planned |
| DXF/PDF raster image export | Deferred |
| DXF BLOCK/INSERT compatibility | Deferred/separate from native block support |
| v0.8.160+ consolidation | Not started as a dedicated pass |

## Intersection and curve-editing policy

The detailed policy is in `docs/geometry-intersections.md` and `docs/curve-editing.md`.

Important rules:

- `Intersect(...)` remains a point-only compatibility API, suitable for snap-like workflows.
- `IntersectDetailed(...)` is the editing-oriented API and may expose `CadIntersectionKind.Overlap` boundary cuts.
- Finite overlap boundaries are valid editing cut candidates when a tool explicitly consumes them.
- Full coincident circles do not synthesize arbitrary points.
- Trim can consume overlap boundary cuts.
- Break works from explicit user-picked points and should not receive injected overlap cuts.
- Extend remains direction-aware and may use finite same-support boundary endpoints; do not treat generic overlap as automatic extension.
- `CadIntersectionKind.Tangent` exists but is not yet a global classification contract until explicit classifier tests are added.

## Dynamic Command HUD policy

The fixed bottom command row has been replaced by the Dynamic HUD. The command buffer still exists logically for aliases, options, autocomplete and history, but it is not a visible bottom command textbox.

Rules to preserve:

- The HUD is mouse-transparent during drawing/edit commands until the user explicitly enters field editing with `TAB`.
- `TAB` cycles editable fields for phases that have real editable values.
- `ENTER` or right click confirms when the current phase has a valid default/value/selection.
- `ESC` cancels the current override/phase/command according to the tool contract.
- Selection-only modify tools should remain prompt/options-only unless a real numeric point/value phase exists.
- New HUD routing should be tool/phase-specific and covered by focused ViewModel regression tests.

## Library and object strategy

The Library is the preferred path for fixed reusable 2D objects. Library items are static `.opencad2d.json` snippets stored under `library/<category>/...`, scanned by the Library Browser and inserted as block references.

Parametric objects should be limited to cases where dimensions/options are essential before generation. Current design decision: doors, windows and stairs may become parametric objects; broad furniture, symbols, sanitary fixtures and details should remain static Library items.

Before adding more toolbar buttons, ask whether the item belongs in the Library instead.

## Known open work

The next work should be selected from the current roadmap, not from stale old notes. Open items are:

- reconcile/check actual `library/` content included in the repository/release package and expand the object pack if needed;
- implement Stair tools v1 if parametric architectural generation is the priority;
- implement Boundary Fill v2: preview, sampled arc/circle boundaries, small-gap tolerance and clearer diagnostics;
- implement real `HatchEntity` after BF v2, with explicit loops and holes/islands;
- run DXF compatibility pass in LibreCAD/QCAD and record exact versions/results in `docs/dxf-compatibility.md`;
- review DXF `BLOCK`/`INSERT` interoperability separately from native OpenCad2D blocks;
- review Property Panel coverage for Arc, Ellipse, EllipticalArc, Polyline, BezierSpline, Text and MTEXT;
- run performance/robustness smoke tests on denser drawings, snaps, hit-testing, previews and multi-boundary edits;
- prepare v0.8.160+ consolidation/release-gate documentation when feature work pauses.

## Documentation state after roadmap reconciliation

Roadmap reconciliation has been completed for the main planning documents:

- `docs/roadmap.md` is the current decision source and now marks Library Browser as implemented.
- `docs/roadmap-v0.8.100.md` is historical/reconciled context for the expansion line, not the primary planning source.
- `docs/stabilization-v0.9-plan.md` no longer treats native blocks or external raster references as deferred feature families.
- `docs/developer/release-and-roadmap-map.md` explains how to read the planning documents.

When adding or changing a feature, update the User Guide first for visible behavior, then the technical document, then roadmap/release notes if the milestone state changes.
