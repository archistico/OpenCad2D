# OpenCad2D AI handoff

This file is the current development handoff for future work on OpenCad2D. It intentionally contains only the active state and stabilized checkpoints. Older milestone logs have been removed from this active handoff; use Git history and release notes for historical detail.

---

## Project snapshot

OpenCad2D is a 2D-only CAD application built with C#/.NET 8 and Avalonia.

Core principles currently guiding development:

- keep the application strictly 2D;
- preserve native geometry whenever possible;
- avoid silent conversion of supported native curves into permanent polylines;
- use small testable phases;
- update documentation after each stabilized block;
- provide zip packages containing only modified/added files when making project changes.

Native/export formats currently expected:

- native: `.opencad2d.json`;
- export/interchange: DXF, SVG, PDF, PNG.

---

## Current high-level status

Completed/stabilized foundations:

- native save/load with dirty-state handling;
- SVG/DXF/PDF export baseline;
- ASCII DXF import baseline for the current practical 2D entity set;
- layers, line formats, text formats and dimension styles;
- property panel for supported entity properties;
- command input with aliases, coordinates, relative/polar input, direct distance input, history and prompt phases;
- snapping, grid, Ortho and Polar Tracking;
- draw tools for points, text, MTEXT, lines, rectangles, circles, arcs, ellipses, polylines, polygons and open Bezier splines;
- dimensions baseline;
- transform tools: Move, Copy, Rotate, Scale, Mirror and Align;
- modify tools: Delete, Break, Trim, Extend, Offset and Fillet;
- measure, align/distribute and navigation tools;
- tool preview descriptor/entity architecture;
- application logging for tool/UI exceptions.

Active release target:

- v0.9 stabilization.

Active focus:

- finish Modify Tools UX cleanup;
- stabilize Offset workflow;
- run manual curve-editing regression;
- complete export/import compatibility validation;
- prepare v0.9 documentation and release gate.

---

## Native curve editing checkpoint

The native curve editing block is considered complete for the currently supported entity set.

Important implemented types/services:

- `CurveCut`;
- `CurveInterval`;
- `ICurveAdapter`;
- `ICurveAdapterFactory`;
- `DefaultCurveAdapterFactory`;
- `CadCurveSplitService`;
- `CadIntersectionPoint`;
- `CadIntersectionKind`;
- `EllipticalArcEntity`;
- `BezierSplineSplitService`.

Current policy:

```text
CAD editing operations modify native entities using native curve parameters.
Sampling is allowed only as provisional support, never as the definitive source of edited geometry when a native representation exists.
Shared intersections should reuse the same geometric point and native parameters to avoid micro-gaps.
```

Current native edit behavior:

| Source entity | TRIM/BREAK result |
|---|---|
| Line | `LineEntity` fragments with shared explicit endpoints |
| Circle | `ArcEntity` fragments |
| Arc | `ArcEntity` fragments |
| Polyline / Rectangle / Polygon | `PolylineEntity` fragments |
| Ellipse | `EllipticalArcEntity` fragments |
| EllipticalArc | `EllipticalArcEntity` fragments |
| Open BezierSpline | `BezierSplineEntity` fragments |
| Closed BezierSpline | deferred/no-op |

Preview UX:

- TRIM removal interval: dashed removal preview;
- BREAK segment removal interval: dashed removal preview;
- EXTEND added interval: highlighted addition preview;
- selected TRIM/EXTEND boundaries and first FILLET entity remain visibly highlighted.

Deferred curve-editing items:

- closed Bezier spline editing policy;
- full-circle/full-ellipse Break Point convention;
- wider adoption of `CadIntersectionPoint` where it simplifies command code;
- Offset review under the same native-geometry preservation policy.

Reference docs:

- `docs/curve-editing.md`;
- `docs/testing/curve-editing-regression-v0.9.md`;
- `docs/known-limitations.md`.

---

## Modify Tools UX checkpoint

A common CAD-style confirmation policy is now documented and partially implemented.

Policy:

```text
Left click = graphical input / entity selection.
Right click = confirm or advance the current phase when a valid default, value or selection exists.
Enter = equivalent to right click in command prompts.
Esc = cancel current phase.
Entity selection phases = EntityOnly snapping.
Geometric point phases = geometric snapping.
```

Completed in the Modify Tools UX cleanup:

- Deselect command/button added;
- Point icon simplified to a small cross;
- Text and MTEXT hit testing includes their bounding boxes;
- Delete behavior:
  - existing selection is deleted immediately;
  - no selection starts a multi-pick Delete flow;
  - Enter/right click confirms deletion;
- TRIM, EXTEND and FILLET show selected boundaries/first entity as persistent overlays;
- FILLET entity selection uses EntityOnly snap;
- POLYLINE can be finished with right click when enough points exist;
- Polygon sides prompt can be confirmed with right click/Enter;
- Fillet radius prompt can be confirmed with right click/Enter;
- Mirror `Delete source objects? <No>` can be confirmed with right click/Enter;
- Ellipse axis input uses the snap-resolved point, not the raw mouse position;
- Rect Sides numeric second-side input now creates the exact typed side length and has stronger perimeter/side-length tests.

Remaining Modify Tools UX work:

1. Offset workflow:
   - typed distance;
   - two-click measured distance;
   - stored last distance;
   - right-click/Enter default when available;
   - clear first-run behavior when no distance exists;
   - EntityOnly target selection;
   - side selection and preview.

2. Final consistency pass:
   - right-click/Enter/Esc behavior across remaining draw/modify tools;
   - phase-specific snap modes;
   - command messages;
   - preview semantics.

Reference docs:

- `docs/modify-tools.md`;
- `docs/commands.md`;
- `docs/roadmap.md`.

---

## Save/export UX checkpoint

Current policy:

- Save/Save As writes the native editable `.opencad2d.json` file;
- Save/Save As updates `CurrentFilePath`;
- Save/Save As clears the dirty state;
- Export creates a derived SVG/DXF/PDF/PNG file;
- Export does not update `CurrentFilePath`;
- Export does not clear dirty state;
- Export messages clarify that the native editable project may still need saving.

Reference doc:

- `docs/export.md`.

---

## Current roadmap summary

Immediate next work:

1. finish Offset workflow;
2. run final Modify Tools UX consistency pass;
3. complete manual curve-editing regression checklist;
4. verify persistence/export/import after native elliptical arcs and open spline fragments;
5. review Property Panel for curve entities;
6. perform performance/robustness smoke pass;
7. update final v0.9 docs and release notes;
8. prepare v0.9 release artifact.

The active roadmap has been cleaned. Old v0.7/v0.8 implementation logs are no longer duplicated in `docs/roadmap.md`; completed historical work is summarized as completed foundations.

---

## Important deferred items

- closed Bezier spline editing;
- Break Point convention for full circles/full ellipses;
- true associative dimensions;
- blocks;
- hatch;
- raster references;
- advanced NURBS fidelity;
- autosave/recovery v2;
- installer/package polish;
- full v1.0 user manual.

---

## Development conventions to keep

- Prefer small phases with focused tests.
- Do not mix large geometry refactors with UI-only cleanup unless required.
- Keep native curve preservation policy explicit when touching TRIM/BREAK/EXTEND/OFFSET.
- For project modifications, provide a zip containing only added/modified files.
- Do not include `.patch` files unless explicitly requested.
- Keep `docs/ai-handoff.md` current after each meaningful phase.
