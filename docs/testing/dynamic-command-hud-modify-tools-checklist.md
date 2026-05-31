# Dynamic Command HUD — Modify tools checklist

This checklist covers the modify/edit tools after the dynamic command HUD migration.
It is intentionally split between editable HUD fields and selection-only tools.

## Editable or coordinate-driven tools

### Move / Copy

- Select one or more entities.
- Start `MOVE` or `COPY`.
- Pick or type the base point using `X` / `Y`.
- Type `Distance`, press `Tab`, type `Angle`, press `Enter`.
- Verify that Move translates the original selection and Copy creates translated duplicates.

### Mirror

- Select one or more entities.
- Start `MIRROR`.
- First axis point: use `X` / `Y` or click.
- Second axis point: use `Distance` / `Angle` or `X` / `Y`.
- Confirm `Yes` or `No` for deleting source objects.

### Break Point

- Start `BREAKPOINT`.
- Select a supported entity.
- The break point phase should show only `X` / `Y` fields.
- Enter both coordinates and press `Enter`.
- Verify that the entity is broken at the projected point.

### Break Segment

- Start `BREAK`.
- Select a supported entity.
- First break point: use `X` / `Y` or click.
- Second break point: use `Distance` / `Angle` or `X` / `Y`.
- Verify that the segment between the two projected points is removed.

### Offset

- Start `OFFSET`.
- Distance phase: type a positive `Distance` and press `Enter`.
- Entity phase: select the source entity.
- Side point phase: use `X` / `Y` or click the desired side.
- Alternative: define offset distance by two points, using `Distance` or `X` / `Y` in the second distance point phase.

### Fillet / Chamfer

- Start `FILLET` or `CHAMFER`.
- Use the command option (`Radius` or `Distance`) to enter the scalar setup phase.
- Type `Radius` / `Distance` and press `Enter`.
- Select the two supported entities or adjacent polyline segments.

### Boundary Fill

- Start `BFILL`.
- Seed point should expose `X` / `Y` fields.
- Enter both coordinates or click inside a closed boundary.

## Selection-only / immediate tools

These tools should not show editable numeric HUD fields. They should show prompt/options only:

- `TRIM`
- `EXTEND`
- `DELETE`
- `EXPLODE`
- `JOIN`

## Block commands

`Create Block` and `Insert Block` are still driven by modal option windows plus pending canvas picks, not by the normal `ICommandDrivenTool` pipeline. Their pending point picks now expose dedicated `X/Y` HUD input; dialog-owned options remain outside the shared resolver.

## Automated regression coverage added in Step 30B

The following cases are now covered by `MainWindowViewModelCommandLineTests`:

- Mirror field exposure: first axis point `X/Y`, second axis point `Distance/Angle/X/Y`.
- Offset initial distance field exposure and typed distance acceptance.
- Fillet radius option field exposure and typed radius acceptance.
- Chamfer distance option field exposure and typed distance acceptance.
- Boundary Fill seed-point `X/Y` exposure.
- Selection-only tools (`TRIM`, `EXTEND`, `DELETE`, `EXPLODE`, `JOIN`) do not expose scalar HUD overrides.

Manual testing should still cover actual canvas picking for Break Point, Break Segment, Trim, Extend, Join, and Offset side selection because those behaviors depend on hit testing and pointer interaction.


### Step 30C - Mirror HUD Tab and option fix

- Tab is reserved for logical HUD field traversal while a command-driven tool is active; CadCanvas grip-edit Tab is now limited to SelectionTool.
- Command option shortcuts such as Mirror Yes/No are handled by the window preview key path after the generic HUD textbox removal.
- Manual check: MIRROR with a selected curve, Tab should enter X for first axis point; at delete-source prompt, Y/N should execute the option.

### Step 30D - Offset / Fillet / Chamfer scalar validation

Manual checks:

```text
OFFSET
Distance = 0
Enter
```

Expected: rejected, distance must be greater than zero.

```text
FILLET
R
Radius = 0
Enter
```

Expected: accepted, radius set to zero.

```text
CHAMFER
D
Distance = 0
Enter
```

Expected: accepted, distance set to zero.


## Automated regression coverage added in Step 30E

- `Break Point` after target selection exposes only `X/Y`, accepts coordinate confirmation and breaks a line at the projected point.
- `Break Segment` exposes `X/Y` for the first break point, then `Distance/Angle/X/Y` for the second break point, and removes the expected line segment.
- `Boundary Fill` seed point `X/Y` creates a filled closed polyline inside a closed line boundary.
- `Boundary Fill` outside seed point leaves the drawing unchanged and reports a clear no-boundary message.
- `DELETE` is included in the selection-only no-scalar-HUD guard.

Manual testing should still cover actual canvas picking, hover previews, Tab focus traversal, and failure messaging because the automated tests drive pointer selection directly through the ViewModel/tool pipeline.


## Automated regression coverage added in Step 30F

- `Trim`, `Extend`, `Delete`, `Explode`, and `Join` expose no editable scalar HUD fields.
- `Trim`, `Extend`, `Delete`, `Explode`, and `Join` cancel back to Selection with `Escape`.
- `Delete` selects by pointer and confirms with Enter.
- `Explode` selects a polyline by pointer and confirms with Enter.
- `Join` selects connected lines by pointer and confirms with Enter.
- `Trim` and `Extend` complete their boundary/target pointer flows through the ViewModel/tool pipeline.

Manual testing should still cover actual UI `Tab` interception and hover feedback. The window-level handler reserves `Tab` while the command HUD is visible, and tools with no editable fields keep focus on the canvas instead of entering grip edit.


## Automated regression coverage added in Step 31

- `Create Block` picked-base-point pending state shows the command HUD as `Create Block`.
- `Create Block` pending point exposes `X/Y` only and accepts complete HUD coordinates as the block base point.
- `Insert Block` pending placement state shows the command HUD as `Insert Block`.
- `Insert Block` pending point exposes `X/Y` only and accepts complete HUD coordinates as the insertion point.
- Block name, definition selection, scale and rotation remain owned by their existing dialogs; the HUD only supplies the final picked point.

Manual testing should still cover the dialog-to-canvas transition, snap-assisted mouse picking and Escape cancellation from the real canvas.


## Remaining manual checks to resume next session

### Step 30E — Break / Boundary Fill smoke checks

- `BREAK PT`
  - Select a supported curve/entity.
  - Press `Tab`; verify the HUD enters `X`.
  - Enter `X`, press `Tab`, enter `Y`, press `Enter`.
  - Verify the break point is projected/validated correctly and the command completes or reports a clear error.

- `BREAK SEG`
  - Select a supported curve/entity.
  - First break point: enter `X/Y` through the HUD.
  - Second break point: enter either `Distance/Angle` or `X/Y`.
  - Verify the removed segment matches the two projected points.

- `BOUNDARY FILL`
  - Press `Tab`; verify the HUD enters `X`.
  - Enter `X/Y` for a point inside a closed boundary.
  - Verify a fill entity is created.
  - Repeat with a point outside the boundary and verify a clear failure/no-op.

### Step 30F — selection-only smoke checks

For each of `TRIM`, `EXTEND`, `DELETE`, `EXPLODE`, and `JOIN`:

- Start the command with and without a preselection.
- Verify the HUD shows prompt/options only.
- Verify no editable scalar HUD fields appear.
- Verify `Tab` does not enter grip edit while the command is active.
- Verify `Esc` cancels the command and preserves the expected selection behavior.
- Verify mouse picking still works.

### Step 31 - Block tools smoke checks

- `CREATE BLOCK`
  - Verify the dialog-driven workflow still works.
  - With `Pick base point from drawing`, press `Tab`, enter `X/Y`, then press `Enter`.
  - Verify the created block reference uses the typed base point.

- `INSERT BLOCK`
  - Verify the dialog/pending placement workflow still works.
  - Press `Tab`, enter insertion `X/Y`, then press `Enter`.
  - Verify scale/rotation still come from the dialog and insertion point comes from HUD coordinates.
