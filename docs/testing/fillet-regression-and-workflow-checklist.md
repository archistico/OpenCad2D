# Fillet regression and workflow checks — 2026-10-31

## Reproduced issues

| Case | Original problem | Correct behavior |
|---|---|---|
| Excessive line-line radius | Sources could become reversed or zero-length; NoTrim could add an arc beyond the finite retained sides | Reject without changing sources or history; offer R/F recovery. |
| Closing corner of a closed polyline | Duplicate fillet arc/path and extra vertex | One native bulge arc, five vertices for a filleted rectangle, closed/fill flags preserved. |
| Second pick on the same polyline segment | Excluding the first segment could select another segment far from the cursor | Enforce selection tolerance on the actual candidate segment. |
| First source removed, changed, hidden or locked | Stale geometry could be previewed/committed; a removed source could throw on commit | Clear stale selection/preview, retain settings, request a new first source. |
| Pick at a line intersection | An arbitrary ray could be chosen, including the outside ray at a reversed terminal endpoint | Reject ambiguous interior picks; infer the existing finite ray at an endpoint. |
| Parameter edit with a visible preview | Previous radius/mode geometry remained visible | Clear the stale preview while keeping the first source selected. |
| Invalid hover | No local explanation of the failure | Show the cause and recovery guidance inside the HUD. |
| Very small radius | Display could round a nonzero radius to zero | Display a meaningful full value and preserve it through untouched confirmation. |

`FilletToolRegressionTests` covers these cases plus non-finite scalar rejection, both line orientations, native radius/tangency, all four closed-polyline corners and undo/redo. `CommandHudKeyboardTests` covers actual keyboard/pointer routing, first-selection recovery, parameter defaults, NoTrim, validation, small radii and dirty state.

The internal bulge convention is preserved; the existing right-angle arc midpoint/tangency check passes. Existing restrictions on curved polylines, same-polyline NoTrim and non-terminal segments of separate objects remain explicit.

## Desktop smoke checklist

These checks supplement automated tests; this file does not record a completed desktop run.

- Start FILLET and verify step 1/2 shows current radius and mode. Confirm it explains that R=0 joins a sharp corner and a positive R creates a round one.
- Set R, Tab through the Radius field and confirm with Enter/right click. Verify zero is allowed, negative/non-finite input is rejected, and small values do not appear as zero.
- Pick the first source on the side to keep. Verify only the chosen polyline segment is emphasized when applicable.
- Hover/select the second source. Check the preview against the resulting native arc and retained portions for both line directions and acute/obtuse corners.
- Try a radius larger than the source legs and one that exactly consumes a leg in Trim mode. Verify no reversed/zero-length source or document mutation is produced.
- With a first source selected, choose R or T. Verify the old preview disappears, the first selection remains, and confirming the parameter returns to step 2/2.
- In T mode, choose N. Verify NoTrim keeps source objects and adds only the arc. Correct an unsupported combination using T or R without restarting first selection.
- Choose an incorrect first source, press F and choose the correct source. Verify radius/mode are retained and the contextual option does not restart the command alias.
- Click a polyline segment twice well away from the common vertex. Verify a distant segment is not selected. Check adjacent segment selection near a shared vertex.
- Fillet every corner of a closed rectangle, including the first/last segment corner. Verify exactly one arc is inserted and no duplicate path appears. Check filled closed polylines.
- Try an interior intersection pick and a terminal endpoint pick with reversed line direction. Verify explicit side guidance for the former and the correct finite ray for the latter.
- Remove/change/hide/lock the first source while the command is active. Verify safe reselection and no stale overlay/preview or exception.
- Hover parallel, nearly collinear and unsupported curved sources. Verify the HUD explains the failure and does not leave an earlier valid preview visible.
- Enter/right click at a selection prompt must return to Selection; at a parameter prompt it keeps the current parameter and resumes selection. Esc clears transient state and retains completed fillets.
- Verify completed Trim and NoTrim fillets are undoable/redone as one command and that the dirty marker tracks the history.

Before handoff: `dotnet build OpenCad2D.sln` and `dotnet test OpenCad2D.sln --no-build`.
