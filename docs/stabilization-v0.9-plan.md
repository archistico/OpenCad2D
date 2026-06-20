# OpenCad2D v0.9 stabilization plan

This document is the active working plan for the v0.9 release-candidate cycle.

v0.9 is a stabilization release. The goal is to make the existing CAD foundation predictable, precise and safe before moving toward v1.0. New large feature families are deferred unless they are required to stabilize existing workflows.

---

## Stabilization principles

1. **Native precision first**  
   CAD editing operations should modify native entities using native curve parameters. Sampling may support preview or coarse discovery, but sampled geometry must not become the permanent result when a native representation exists.

2. **Predictable command UX**  
   Left click provides graphical input. Right click and Enter confirm the current phase when a valid default, value or selection exists. Esc cancels the current phase.

3. **Entity selection is explicit**  
   Phases that select entities should use EntityOnly snapping and should visibly highlight acquired entities.

4. **Export is not Save**  
   SVG/PDF/DXF/PNG exports are derived files. They must not replace the native current file path and must not clear the drawing dirty state.

5. **Small testable phases**  
   Every stabilization step should have focused tests and documentation updates.

---

## Completed stabilization checkpoints

| Checkpoint | Status | Notes |
|---|---:|---|
| Native curve editing foundation | Done | `CurveCut`, `CurveInterval`, `ICurveAdapter`, `CadCurveSplitService` and adapter-backed splitting are in place. |
| TRIM/BREAK precision | Done | Supported curves use native parameters and shared cut points; line endpoints are reused exactly where applicable. |
| Multi-boundary curve trim | Done | Circle/Arc trim with multiple boundaries is stabilized for the supported workflows. |
| EllipticalArcEntity | Done | Native partial ellipses are represented by `EllipticalArcEntity` with rendering, snapping, persistence and export support. |
| Ellipse TRIM/BREAK | Done | Ellipse edits now produce native elliptical arcs rather than permanent polyline fragments. |
| Open Bezier TRIM/BREAK | Done | Open Bezier spline edits preserve `BezierSplineEntity` fragments where supported. |
| Rich intersections | Done, incremental | `CadIntersectionPoint` records shared points and native parameters; finite line/arc overlap boundaries are available through `IntersectDetailed(...)`; additional adoption remains optional/incremental. |
| EXTEND native alignment | Done for supported targets | EXTEND uses the native model for supported lines/arcs/polylines/elliptical arcs and native elliptical boundaries. |
| Preview UX | Done for TRIM/BREAK/EXTEND | Dashed removal previews and addition highlights are implemented. |
| Save/export clarity | Done | Export messages clarify that the editable native project may still need saving. |
| Modify-tool UX policy | In progress, nearly complete | Deselect, Delete multipick, right-click confirmations, entity-only snap phases, selected-boundary highlights and Offset workflow stabilization are implemented for the covered tools. |

---

## Active work: Modify Tools UX cleanup

Completed in this block:

- Deselect command/button;
- Point icon simplified to a small cross;
- Text/MTEXT bounding-box hit testing;
- Delete existing selection immediately or multi-pick then Enter/right-click;
- TRIM/EXTEND/FILLET selected boundary/first entity overlays;
- FILLET entity-selection phases use EntityOnly snap;
- POLYLINE right-click finish;
- Polygon/Fillet/Mirror right-click default confirmations;
- Ellipse axis input uses snap-resolved points;
- Rect Sides typed second-side length creates the exact requested length.

Completed in the Offset block:

- typed distance;
- two-point measured distance;
- stored last distance;
- right-click/Enter default when available;
- clear first-run message when no default exists;
- EntityOnly target selection;
- side selection and preview;
- explicit supported/deferred geometry policy.

Remaining in this block:

1. **Final UX consistency pass**
   - right-click/Enter/Esc behavior across draw/modify tools;
   - snap mode by phase;
   - command messages;
   - preview semantics.

2. **Documentation sync**
   - update `docs/commands.md` and `docs/modify-tools.md` after Offset is finalized.

---

## Validation work before v0.9

### Curve editing manual regression

Use:

```text
docs/testing/curve-editing-regression-v0.9.md
```

Validate TRIM, BREAK, EXTEND, shared cut points, native ellipse/spline preservation, persistence and export.

### Export/import compatibility

Validate a representative drawing containing:

- multiple layers and line formats;
- text and MTEXT;
- dimensions;
- lines, circles, arcs, ellipses, elliptical arcs, polylines, polygons and open splines;
- geometry produced by TRIM/BREAK/EXTEND.

Export to:

- SVG;
- PDF;
- DXF.

Open DXF samples in:

- LibreCAD;
- QCAD;
- optionally Autodesk DWG TrueView.

Record exact viewer versions and results.

### Property Panel review

Review core editable/read-only properties for:

- Arc;
- Ellipse;
- EllipticalArc;
- Polyline;
- BezierSpline;
- Text;
- MTEXT.

### Performance/robustness smoke pass

Review:

- selection and hit testing on denser drawings;
- snaps and intersections with many entities;
- preview performance;
- degenerate geometry cases;
- export time for representative drawings.

---

## v0.9 release gate

Before preparing a v0.9 release artifact:

```powershell
dotnet clean
dotnet restore
dotnet build
dotnet test
git status
make zip
```

Then manually verify:

- generated zip name/date;
- archive contains only the intended source/docs/tests/release files;
- README and roadmap are current;
- known limitations are current;
- release notes describe completed work and deferred items honestly.

---

## Deferred beyond v0.9 unless required

- closed Bezier spline editing;
- full-circle/full-ellipse Break Point convention;
- true associative dimensions;
- blocks;
- hatch;
- raster references;
- advanced NURBS fidelity;
- autosave/recovery v2;
- major renderer or spatial-index rewrite;
- installer/package polish;
- full v1.0 user manual.
