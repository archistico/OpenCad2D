# Shared Specification — Wall Mask and Opening Behavior

## Purpose

Parametric doors and windows need to appear as openings in wall linework. The first OpenCad2D implementation should support this without destructively cutting existing geometry. This document defines a shared wall mask/opening contract so doors, windows and any future architectural opening tools behave consistently.

## Design decision

The first implementation uses non-destructive visual masking. It does not modify, trim, split, delete or rewrite wall entities automatically. `DoorEntity` adopts this contract first in v0.8.180D through a persisted `MaskWallOpening` flag and a generated wall-mask polygon.

```text
Door/window entity + mask footprint -> hides underlying wall linework visually
```

A later milestone may add an explicit command to cut real wall openings, but it should not be implicit in the first insertion workflow.

## Why non-destructive first

Real wall cutting requires reliably identifying wall geometry, determining wall thickness, preserving layers, splitting polylines, handling blocks, avoiding duplicate segments, and keeping undo/recovery safe. Those behaviors are valuable, but they belong to a later explicit wall-editing feature.

The v1 goal is simpler: a door or window should look correct when placed over wall lines, while the original wall entities remain intact and recoverable.

## Mask footprint

A mask footprint is a model-space polygon or rectangle generated from the parametric opening object. It represents the area that should cover/hide existing wall linework.

Suggested properties:

| Property | Meaning |
|---|---|
| `maskEnabled` | whether the object masks underlying geometry |
| `maskWidth` | opening width, usually aligned with door/window width |
| `maskDepth` | wall thickness or opening depth |
| `maskOffset` | optional offset relative to insertion/anchor point |
| `maskBackgroundMode` | drawing background, explicit color, or future wipeout-like mode |

For the first implementation, mask background should normally match the drawing/canvas background for screen and PDF/SVG output.

## Draw order

The mask must draw above wall linework but below the visible door/window symbol. Internally the entity can render in two passes:

1. mask footprint;
2. visible parametric linework.

If draw order is user-editable for the door/window entity, the mask and visible linework should move together as a single logical object.

## Selection and hit testing

The mask footprint itself should not be independently selectable in v1. Selecting the visible door/window selects the parametric object. If the user clicks only the blank masked region, hit testing may either select the door/window or ignore the click; the chosen behavior should be consistent and documented once implemented.

## Snapping

The mask should not add ordinary snap points unless the feature-specific tool explicitly exposes opening endpoints. Recommended first behavior:

- snap to visible door/window control points;
- snap to insertion point and key parametric points;
- do not snap to mask-only rectangle corners unless they are also meaningful opening points.

## Export behavior

| Target | First-version expectation |
|---|---|
| SVG | Export mask as a filled polygon/rect before the visible symbol. |
| PDF | Export mask as a filled polygon/rect before the visible symbol. |
| PNG | Canvas rendering naturally includes the mask. |
| DXF | Prefer generated linework and document limitations if a true wipeout/mask is not exported. A safe fallback is to export visible door/window linework without destructive wall cutting and document that DXF masks may be limited. |

DXF wipeout support can be evaluated separately. The first priority is that OpenCad2D's native view and SVG/PDF output are visually correct.

## Interaction with layers

The door/window object belongs to its layer. The mask should follow the object's visibility and locking state. If the layer is hidden, both the symbol and mask disappear. If the layer is locked, the object should not be editable/selectable according to existing layer rules.

The mask should not alter the wall layer or write data into wall entities.

## Property Panel

Door/window entities should expose at least:

- `Mask wall lines`: on/off;
- `Wall thickness` or `Mask depth`;
- possibly `Mask offset` once needed.

Changing these values should update preview/rendering immediately and should be undoable.

## Failure behavior

If the mask footprint cannot be generated because width/thickness is invalid, the command must not commit. Existing objects loaded from invalid older data should recover by disabling the mask and preserving visible symbol geometry if possible.

## Future destructive opening command

A later command may implement real wall cutting. It should be explicit, for example:

```text
CREATEOPENING
CUTWALLFOROPENING
```

It should require selected wall entities or a clear wall-detection policy, show preview, commit as one undoable operation, and preserve enough diagnostic data for recovery.
