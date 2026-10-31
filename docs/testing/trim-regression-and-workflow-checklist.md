# Trim regression and workflow checks — 2026-10-31

This pass covers native interval selection, cutting-edge state, local Undo and the cursor HUD workflow. Native geometry and the existing deferred closed-spline limitation are preserved.

## Reproduced defects and regression coverage

| Defect | Reproduction | Expected after the fix |
|---|---|---|
| Arbitrary side selected at a crossing | Pick exactly on the target/boundary intersection, or within geometry distance tolerance | No mutation and no removal preview. Feedback asks for a pick inside the part to remove. Covered for open lines and closed circles. |
| Selected edge masks a nearby target | Pick a target within selection tolerance of a closer explicit cutting edge | The supported target is selected; preview and committed side agree. |
| Stale cutting geometry | Trim in All mode, add a visible edge, replace an explicit edge, or hide an edge while Trim is active | Calculations and emphasis overlays use current visible entity values and ids. Removed geometry is not highlighted. |
| Local Undo affects unrelated work | Trim, then perform another undoable action; or globally undo the trim and press U | U leaves unrelated work and the pre-session drawing intact. Global Undo/Redo of an owned trim does not break later local Undo. |
| Ctrl-hover advertises removal | Produce a valid preview, then hover while holding Ctrl | No dashed removal preview; guidance explains adding a cutting edge. |
| Finishing leaves Trim active | Enter or right click while waiting for targets | Selection becomes active. Completing an individual cut keeps Trim active. |

Core coverage: `CadTrimServiceTests`, `CadCurveSplitServiceTests`, `CommandHistoryTests`.

Tool coverage: `TrimToolTests`, `TrimToolRegressionTests`, and existing `ModifyToolLayerRuleTests`. This includes locked boundaries, locked targets, native fragments, no-op failures and undo/redo.

Headless window coverage in `CommandHudKeyboardTests` exercises Enter/All, pointer picking, local U, dirty state, E recovery, right-click completion and visible HUD feedback.

## Desktop smoke checklist

The following checks supplement automation; they are not a record of a completed desktop run.

- Start TRIM. Verify the HUD says **1/2** and explains choosing an edge or Enter/All. Pick a boundary and verify it immediately says **2/2**, including what Enter will do.
- Move over the unwanted portion: cutting edges stay red, retained fragments are solid green and the removed interval is dashed green with clear gaps. Repeat with All cutting edges: the hovered target must not have a red overlay or its original solid stroke underneath the dashed portion. Move away and verify its normal appearance returns without marking the document dirty. Then click inside the unwanted portion. Repeat on several sides without restarting the command.
- Try a pick directly on an intersection. Verify that nothing is removed and the local feedback explains moving inside a side.
- Near an intersection, verify that an explicit boundary does not prevent selecting the target underneath it.
- Hold Ctrl, move over another edge and Ctrl-click. Verify the candidate guidance, absence of a removal preview, and updated edge count. Release Ctrl and verify normal trimming resumes.
- Choose an incorrect edge, press E and choose the correct edge. Verify earlier cuts remain and U can still undo them.
- Test Enter/All and right-click/All at the initial prompt. During target selection, Enter/right click must finish and return to Selection rather than silently restart boundary selection.
- Test All mode across consecutive cuts and Undo/Redo. Verify no removed segment remains emphasized or acts as an invisible cutting edge.
- Use a visible locked layer as a boundary, try trimming a locked target, and hide a previously selected edge. Verify locked/hidden semantics and clear recovery instructions.
- Hover a non-intersecting target, a tangent-only closed curve and an unsupported target. Verify feedback is visible in the HUD, explains the cause and does not show a stale removal preview.
- Verify arcs, circles, ellipses and bulged polylines keep native geometry after trimming, and Undo/Redo restores exact geometry.
- Confirm that finished cuts remain undoable after Enter or Esc, and that cancel/finish clears transient highlights.

Before handoff run `dotnet build OpenCad2D.sln` and `dotnet test OpenCad2D.sln --no-build`.
