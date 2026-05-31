# DIVIDE command manual verification - 2026-05-31

This checklist validates the AutoCAD-style `DIVIDE` command and the shared Dynamic Command HUD integer-field fix used by `DIVIDE` and `POLYGON`.

## Command identity

- [ ] Toolbar button is visible as `Divide` in the Draw tools area.
- [ ] `DIVIDE` alias starts the command.
- [ ] `DIV` alias starts the command.

## Basic open-entity behavior

- [ ] Draw a line from `0,0` to `300,0`.
- [ ] Select the line and start `DIVIDE`.
- [ ] HUD asks for `Segments` and shows a default value.
- [ ] Change `Segments` to `3` and confirm.
- [ ] Two persistent `PointEntity` markers are created at `100,0` and `200,0`.
- [ ] The original line remains unchanged and is not split.
- [ ] Undo removes both created points together.
- [ ] Redo restores both created points together.

## Selection workflow

- [ ] Start `DIVIDE` with no selected entity.
- [ ] The command asks to select one entity to divide.
- [ ] Pick a supported line, arc, circle or polyline.
- [ ] The command advances to the `Segments` HUD field.
- [ ] Multiple selected entities do not cause multiple divide operations in v1; the workflow must resolve to one source entity.

## Closed-entity behavior

- [ ] Divide a circle into `4` segments.
- [ ] Four point entities are created on the circle.
- [ ] Divide a closed polyline into `4` segments.
- [ ] Four point entities are created along the closed path from the conventional start point.

## Polyline cumulative length

- [ ] Create an open polyline with unequal segment lengths, for example 50 + 250.
- [ ] Divide it into `3` segments.
- [ ] Points are placed by cumulative path length, not one per original polyline segment.

## Validation

- [ ] `Segments = 1` is rejected and creates no points.
- [ ] `Segments = 1001` is rejected and creates no points.
- [ ] Non-integer input is rejected and creates no points.
- [ ] Unsupported entities such as text, image references, dimensions, blocks or points are not divided.

## Current layer rule

- [ ] Set a current layer different from the source entity layer.
- [ ] Run `DIVIDE`.
- [ ] Created points are on the current layer, not automatically on the source entity layer.

## HUD integer editing regression

- [ ] Start `POLYGON`.
- [ ] Press `Tab` until the `Sides` field is active.
- [ ] Type `5`.
- [ ] The field remains `5` while editing and does not revert to `6`.
- [ ] Confirm and verify a 5-sided polygon workflow.
- [ ] Select a line and start `DIVIDE`.
- [ ] Press `Tab` until the `Segments` field is active, if it is not already active.
- [ ] Type `3`.
- [ ] The field remains `3` while editing and does not revert to the default.
- [ ] Confirm and verify two internal point entities are created.
