# OpenCad2D AI handoff

This file is the current technical handoff for continuing OpenCad2D work. It should be updated after each meaningful refactor or feature phase.

---

## Current focus

OpenCad2D is in the v0.9 stabilization cycle. The active goal is to make the existing 2D CAD foundation predictable, precise and release-ready before moving toward v1.0.

Current work area: v0.9 stabilization after completing the first solid-fill pass for closed entities.

---

## Completed foundations

The following areas are considered stable enough to be summarized rather than repeated in full historical detail:

- core document/entity/layer/line-format model;
- Avalonia app shell, canvas, command row, snap/status UI and tool panels;
- native `.opencad2d.json` persistence, including layer fill color and fill flags for supported entities;
- SVG/PDF/DXF export baseline, including solid fill output for supported closed entities, and ASCII DXF import baseline;
- draw tools for lines, rectangles, circles, arcs, ellipses, polylines, polygons, text, MTEXT, points and open Bezier splines;
- dimensions baseline;
- selection, entity cycling, Select All, Select Last and Deselect;
- Text/MTEXT bounding-box hit testing;
- Save/Export UX clarity: export does not replace native save and does not clear dirty state.

---

## Dimension Style System checkpoint

Dimension styles are now being promoted from a basic rendering helper to a document-level drafting system.

Current implemented baseline:

- `CadDocument.CurrentDimensionStyleId` stores the current dimension style;
- `ToolCreationContext.CurrentDimensionStyleId` is used by dimension tools when creating new dimensions;
- dimension style persistence includes the current dimension style id in document settings;
- `DimensionStyle` supports generic prefix/suffix plus radius and diameter prefixes;
- `DimensionStyle` now also stores arrow symbol, text rotation mode and preferred dimension line offset;
- `DimensionGeometryBuilder` resolves readable dimension text rotations and supports closed arrow, open arrow, architectural tick, dot and no terminator symbols;
- dimension text is treated as center-anchored for canvas, SVG, PDF and DXF graphical dimension export;
- a first `DimensionStyleManagerWindow` is available from the top bar near Layers, Line Formats and Text Formats;
- the manager supports adding/removing non-built-in unused styles, setting the current style, and editing text format, unit precision/separator, generic prefix/suffix, R/Ø prefixes, symbol type/size, text rotation mode and main offsets;
- dimension style changes are applied through `UpdateDimensionStylesCommand`, so they are undoable and keep the current style synchronized with the document/tool context.

Next planned step: add a live preview to the Dimension Style Manager and then wire the dimension style selector into the property panel for selected dimensions.

---

## Solid fill checkpoint

The first solid-fill pass is implemented for the current scope.

Model rules:

- `Layer.FillColor` owns the fill color;
- `CircleEntity.IsFilled` enables/disables solid fill for circles;
- `PolylineEntity.IsFilled` enables/disables solid fill only when `PolylineEntity.IsClosed` is true;
- rectangles and polygons are handled as closed polylines;
- entities do not store their own fill color;
- open polylines never render/export fill.

UI/rendering/export state:

- canvas rendering fills supported entities with `Layer.FillColor`;
- selected filled entities keep their fill color and only change stroke highlight color;
- Property Panel exposes `Fill: None/Solid` for circles and closed polylines;
- Layer Manager exposes fill color through a color picker plus `#RRGGBB`;
- native persistence stores `LayerDto.FillColor`, `CircleEntityDto.IsFilled` and `PolylineEntityDto.IsFilled`;
- SVG and PDF export write fill for supported filled entities;
- DXF export writes targeted `SOLID` HATCH records for filled circles and closed polylines.

Deferred fill work:

- transparency;
- hatch/pattern definitions;
- general hatch editing tools;
- fill for additional entity types;
- manual DXF viewer validation of generated HATCH output.

---

## Native curve editing checkpoint

The curve-editing stabilization block is complete for the current supported native entity set.

Important types/services now in place:

- `CadCurveSplitService`;
- `ICurveAdapter` and adapter-backed curve splitting;
- `CurveCut` and `CurveInterval`;
- richer `CadIntersectionPoint` with shared point and native parameters;
- `EllipticalArcEntity`;
- `BezierSplineSplitService`.

Current behavior:

- `LineEntity` edits preserve native line fragments and reuse shared cut points as explicit endpoints;
- `CircleEntity` TRIM/BREAK Segment results become native `ArcEntity` fragments;
- `ArcEntity` remains native arc fragments;
- `PolylineEntity`, including rectangles/polygons represented as closed polylines, remains polyline geometry;
- `EllipseEntity` TRIM/BREAK Segment results become native `EllipticalArcEntity` fragments;
- `EllipticalArcEntity` remains native elliptical arc fragments;
- open `BezierSplineEntity` TRIM/BREAK results remain native Bezier spline fragments through De Casteljau splitting;
- closed Bezier spline editing is intentionally deferred/no-op.

The command-level permanent `PolylineEntity` fallback has been removed for supported ellipse and open-spline TRIM/BREAK operations.

---

## Preview UX checkpoint

Preview semantics are now explicit:

- `Removal`: geometry that will be removed, used by TRIM and BREAK Segment, rendered dashed;
- `Addition`: geometry that will be added, used by EXTEND and OFFSET previews;
- `Emphasis`: selected boundary/target/first entity overlays.

TRIM uses entity-only snap. TRIM, EXTEND and FILLET keep selected boundaries/first entities visibly highlighted.

---

## Modify Tools UX checkpoint

Established UX policy:

- left click provides graphical input or entity selection;
- right click confirms/advances the current phase when a valid default, value or selection exists;
- Enter is equivalent to right click for command prompts;
- Esc cancels the current phase;
- entity-selection phases use `SnapKind.EntityOnly`;
- point-input phases use geometric snaps.

Completed Modify UX work:

- Deselect command/button and aliases `DESELECT`, `CLEARSELECTION`, `CS`;
- Point icon simplified to a small cross;
- Delete tool deletes existing selection immediately, or multi-picks entities and confirms with Enter/right click;
- Rotate and Scale can initiate entity selection when no selection is active;
- TRIM/EXTEND/FILLET selected boundary/first-entity highlights;
- FILLET entity selection uses EntityOnly snap;
- POLYLINE can finish with right click when enough vertices exist;
- Polygon sides, Fillet radius and Mirror delete-source prompts accept right click/Enter defaults;
- Ellipse axis input uses snap-resolved points;
- Rect Sides numeric second-side input creates the exact typed side length;
- Offset workflow accepts typed distance, two picked distance points, stored last distance and right-click/Enter default confirmation.
- Final Modify Tools UX pass completed for the primary tool set: right-click/Enter messaging, EntityOnly selection phases, geometric snap phases and transient-state cleanup expectations are documented and covered by targeted tests.
- Break Point, Break Segment, Extend and Align now explicitly expose phase-specific snap modes through `ISnapModeProvider`.
- Added `ExplodeTool` and `JoinTool` as essential pre-v0.9 modify tools. Explode turns selected straight-segment polylines into line entities; Join turns connected selected line chains into open/closed polylines. Both use EntityOnly selection, Enter/right-click confirmation, command aliases and single-step undo.

---

## Offset checkpoint

Offset is stabilized for the v0.9 scope.

Supported targets:

- `LineEntity`;
- `CircleEntity`;
- `ArcEntity`;
- straight-segment open/closed `PolylineEntity`.

Deferred targets:

- `EllipseEntity` and `EllipticalArcEntity`: a true offset is not another exact ellipse;
- `BezierSplineEntity`: a true offset is not another exact Bezier spline.

The previous spline offset path that silently produced a sampled `PolylineEntity` approximation has been removed. Unsupported advanced curves return a clear deferred-support message and create no geometry.

Offset workflow:

1. specify distance by typed value, two clicks, or right click/Enter with a previous distance;
2. select target with EntityOnly snap;
3. choose side graphically;
4. preview is shown as an Addition highlight;
5. after creating an offset, the tool returns to target selection and keeps the same distance.

---


## Property Panel final cleanup checkpoint

Current state:

- `Layer id` is a combo-box row populated from the current document layer ids.
- `Dimension style` is a combo-box row populated from the current document dimension styles.
- Polyline `Closed` is a `Yes`/`No` combo-box row.
- Selected polylines show a compact `Vertices` section with at most 4 editable rows.
- Each polyline vertex row uses a single `X, Y` value, for example `10.5, 20`.
- If a polyline has more than 4 vertices, the panel adds a `More vertices` row with the hidden count.
- Per-vertex insert/delete action rows were intentionally removed from the compact Property Panel list; they should return later in a dedicated vertex editor UI, not mixed into the lightweight property list.

Implementation notes:

- `PropertyRowViewModel` already supports combo-box rows through `Options`, `IsComboBox` and `IsTextBox`.
- `SelectionPropertyPanelBuilder` is the current source of truth for Property Panel rows.
- Keep the compact vertex list lightweight; avoid creating hundreds of editable rows for dense polylines.

## Current next work

1. Validate Explode/Join manually in the UI after local build/test.
2. Execute the focused evening pass using `docs/testing/curve-editing-evening-run-2026-05-21.md` and the sample drawing `docs/testing/samples/curve-editing-regression-v0.9.opencad2d.json`; record reproducible bugs before adding new code.
3. After the evening pass, triage any Blocker/High curve editing bugs before moving to the export/import compatibility pass for SVG/PDF/DXF, including manual HATCH validation in LibreCAD/QCAD.
4. Manual UI check of Property Panel combo boxes and compact polyline vertices.
5. Release preparation for v0.9.

---

## Intentional deferred items

- closed Bezier spline editing;
- Break Point on complete circles/ellipses until a full-sweep open-arc convention is defined;
- true offset support for ellipses, elliptical arcs and Bezier splines;
- rounded/configurable Offset joins and advanced self-intersection cleanup;
- DXF partial ELLIPSE import mapping directly to `EllipticalArcEntity`, unless chosen for v0.9;
- broader adoption of `CadIntersectionPoint` in paths where it adds value;
- spatial indexing/performance pass for large drawings.
