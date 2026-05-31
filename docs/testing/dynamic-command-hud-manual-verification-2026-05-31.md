# Dynamic Command HUD manual verification — 2026-05-31

This note records the manual verification pass performed after the HUD stabilization, modify-tool snap audit and block workflow cleanup. Automated tests were also expanded during the same pass; this document is the human workflow checklist that should be repeated before the next release gate.

## Result

Status: pass for the current stabilization scope.

The verified scope includes HUD numeric routing, selection-only modify prompts, block creation/insertion pending workflows, snap behavior during entity selection and the removal of the visible bottom command row.

## HUD keyboard behavior

Verified expectations:

- typing the first numeric character activates the intended HUD field;
- `Tab` moves to the next logical field without replacing typed values with live mouse measurements;
- `Enter` confirms the current valid field/phase;
- `Escape` clears the active command or pending placement without leaving stale HUD overrides;
- command aliases and option shortcuts still work through the internal keyboard buffer even though the bottom command textbox is gone.

## Draw and geometry tools

Verified expectations:

- Rectangle keeps typed `Width` and `Height` stable while the mouse moves;
- Rectangle by Sides confirms first-side `Distance/Angle`, then second-side `Height`;
- Circle radius input creates the expected circle;
- Arc radius/angle and end-angle phases advance correctly;
- Ellipse major/minor radius phases advance correctly;
- Polygon sides and radius/angle routing behave as expected.

## Modify and selection tools

Verified expectations:

- Break Point, Break Segment and Boundary Fill expose only the relevant point HUD fields;
- Trim, Extend, Delete, Explode and Join remain prompt/selection tools without irrelevant numeric HUD fields;
- modify tools waiting for entity selection use entity-only snap, not endpoint/midpoint point snaps;
- Offset reaches entity-only snap after its distance has been entered.

## Blocks and pending placement

Verified expectations:

- Create Block cannot create an empty block;
- Create Block shows the selected-entity counter;
- Create Block can close for entity selection and reopen for review;
- normal single selection returns immediately to the Create Block dialog;
- `Shift` selection remains active for multi-selection and returns to the dialog on `Enter`;
- picked base points return to the dialog instead of creating the block immediately;
- the block is created only through the final `OK`;
- Insert Block keeps block choice, scale and rotation in the dialog and uses HUD/click only for insertion point;
- cancelling Insert Block clears pending insertion and stale HUD coordinates.

## Snapping and intersections

Verified expectations:

- entity selection prompts use entity-only snap;
- intersection snap works between circle and rectangle/polyline cases;
- the intersection provider can fall back to the core CAD entity intersection service for additional curve combinations.

## Release-gate reminder

Before the next release gate, repeat this checklist together with:

```bash
dotnet build OpenCad2D.sln
dotnet test OpenCad2D.sln --no-build
```

If a future tool introduces a new HUD field or non-standard point/scalar resolution, add a targeted automated regression test and update this checklist.
