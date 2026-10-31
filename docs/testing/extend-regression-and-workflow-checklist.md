# EXTEND regression and workflow checklist

Automated coverage is in `CadExtendRegressionTests`, `CadExtendServiceTests`, `ExtendWorkflowRegressionTests`, `ExtendToolTests`, layer-rule tests and the headless `CommandHudKeyboardTests`.

## Desktop workflow

- Start EXTEND. Verify step 1/2, All option and absence of measurement fields. Click a boundary: it turns red and the prompt advances to 2/2. Alternatively, Enter/right-click/A uses all visible boundaries.
- Hover near a line endpoint: its existing geometry becomes solid green and the extension dashed green, ending on the nearest reachable boundary. The dashed interval must have clear gaps. Move away: normal drawing returns and the document stays clean. Click near the endpoint: preview and result agree, Undo becomes available and the command stays active.
- Repeat with circular arcs in both directions (including across angle zero), elliptical arcs and open mixed polylines with straight ends. Existing native curves and polyline bulges must remain intact.
- Ctrl-hover clears the extension simulation and explains adding a boundary. Ctrl-click adds a red boundary without editing geometry. Duplicate edges are rejected. In All mode Ctrl explains that all visible edges are already included; E permits explicit selection.
- Verify All mode does not overlay the hovered target in red. Add/remove/move a boundary while EXTEND is active: subsequent previews use current visible geometry. Removed/hidden boundaries must not remain as ghost stopping edges.
- Click the middle of a line, the opposite endpoint, a parallel/non-intersecting boundary, an already touching endpoint, a closed target, a curved polyline end and a zero-length endpoint segment. Feedback must explain recovery and no stale extension should remain. Move to a valid endpoint and verify immediate recovery.
- Test a visible boundary on a locked layer, a hidden boundary and a target on a locked layer. Only the visible reference boundary can be used; locked targets stay unchanged.
- Use E after a completed extension: step 1 returns and completed work remains. U restores the extension and the saved/dirty marker. Global Redo restores it; U can undo it again. An unrelated action at the top of history must prevent local U from undoing that action.
- Enter/right-click in step 2 returns to Selection. Esc exits without rolling back completed work. Restart EXTEND and verify that boundaries, feedback and previews from the previous session have cleared.

## Native operation contract

`CadExtendService.ExtendToBoundaries` excludes the target and chooses the smallest native extension on the explicitly picked side, independent of boundary order. Endpoint ties within distance tolerance are rejected. A boundary already touching that endpoint prevents skipping to farther intersections. Linear extension thresholds use physical distance rather than a fixed normalized parameter, including long target segments. No operation replaces an available native curve with sampled polylines.

The service keeps curved polyline endpoint extension and spline extension unsupported. EXTEND reports those limitations instead of flattening geometry or implying that choosing the other side will always work.
