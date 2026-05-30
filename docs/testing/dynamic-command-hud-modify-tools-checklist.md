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

`Create Block` and `Insert Block` are still driven by modal option windows plus pending canvas picks, not by the normal `ICommandDrivenTool` pipeline. Their HUD integration should be handled in a later dedicated step.

## Automated regression coverage added in Step 30B

The following cases are now covered by `MainWindowViewModelCommandLineTests`:

- Mirror field exposure: first axis point `X/Y`, second axis point `Distance/Angle/X/Y`.
- Offset initial distance field exposure and typed distance acceptance.
- Fillet radius option field exposure and typed radius acceptance.
- Chamfer distance option field exposure and typed distance acceptance.
- Boundary Fill seed-point `X/Y` exposure.
- Selection-only tools (`TRIM`, `EXTEND`, `EXPLODE`, `JOIN`) do not expose scalar HUD overrides.

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
