# Commands

OpenCad2D uses undoable document commands for mutations and command-line aliases for user actions.

---

## Mutation rule

All document changes must go through undoable command objects. UI controls and tools must not directly mutate the document model without using command history.

Common command types:

- `AddEntityCommand`
- `DeleteEntitiesCommand`
- `ReplaceEntitiesCommand`
- `MoveEntitiesCommand`
- `CopyEntitiesCommand`
- `RotateEntitiesCommand`
- `ScaleEntitiesCommand`
- `TransformEntitiesCommand`
- `MirrorEntitiesCommand`
- `ModifyEntitiesCommand`
- `CompositeCommand`
- `UpdateLayersCommand`
- `UpdateLineFormatsCommand`
- `UpdateTextFormatsCommand`

---

## File-level commands

These actions are currently exposed from the toolbar/window UI rather than the command-line parser. They still use undoable document commands when they mutate the active drawing.

| Action | Behavior | Undo semantics |
|---|---|---|
| Import Drawing | Imports another `.opencad2d.json` into the current drawing with scale, rotation and insertion point | Undo removes the imported batch and rolls back added resources when possible |
| Import DXF | Imports an ASCII DXF into a new unsaved document | Replaces the active document, so normal undo is not used for the import operation |
| Attach Image | Links a PNG/JPG/JPEG as an external raster reference | Undo removes the image reference |
| Collect Refs | Copies linked raster references beside the drawing and rewrites their paths | Undo restores the previous image reference paths |

---

## Confirmation policy

Interactive tools follow the shared CAD confirmation policy:

```text
Left click  -> graphical input or entity pick
Right click -> confirm/advance the current phase when a valid default, value or selection exists
Enter       -> keyboard equivalent of right click for command prompts
Esc         -> cancel the current phase
```

A tool must not guess a missing value. If a prompt has no valid default yet, right click/Enter should show a clear message instead of committing an operation.

Selection phases in modify tools expose `SnapKind.EntityOnly`; point phases expose the active geometric snap set; option-only confirmations disable point snaps when no canvas point is meaningful.

## Command input aliases

Aliases are case-insensitive.

### Selection

| Action | Aliases |
|---|---|
| Select | `SELECT`, `S` |
| Select All | `SELECTALL`, `SA`, `ALL` |
| Select Last | `SELECTLAST`, `SL`, `LAST` |
| Deselect | `DESELECT`, `CLEARSELECTION`, `CS` |

`Select Last` restores the previous effective selection before deselection, not the last created entity.

### Drawing

| Tool | Aliases |
|---|---|
| Point | `POINT`, `PT` |
| Text | `TEXT`, `T` |
| Multiline text | `MTEXT`, `MT` |
| Line | `LINE`, `L` |
| Rectangle | `RECTANGLE`, `RECT` |
| Rectangle by sides | `RECTSIDES`, `RS` |
| Circle | `CIRCLE`, `C` |
| Ellipse | `ELLIPSE`, `EL` |
| Arc | `ARC`, `A` |
| Arc 3P | `ARC3P`, `A3` |
| Polyline | `POLYLINE`, `PL` |
| Spline | `SPLINE`, `SPL` |

### Dimensions

| Tool | Aliases |
|---|---|
| Horizontal Dimension | `HORIZONTALDIMENSION`, `HDIM` |
| Vertical Dimension | `VERTICALDIMENSION`, `VDIM` |
| Aligned Dimension | `ALIGNEDDIMENSION`, `ADIM` |
| Radius Dimension | `RADIUSDIMENSION`, `RDIM` |
| Diameter Dimension | `DIAMETERDIMENSION`, `DDIM` |
| Angular Dimension | `ANGULARDIMENSION`, `ANGDIM` |

### Modify / edit / transform

| Tool | Aliases |
|---|---|
| Delete | `DELETE`, `DEL` |
| Move | `MOVE`, `M` |
| Copy | `COPY`, `CP` |
| Rotate | `ROTATE`, `RO` |
| Scale | `SCALE`, `SC` |
| Align by points | `ALIGN` |
| Break Point | `BREAKPOINT`, `BP` |
| Break Segment | `BREAKSEGMENT`, `BREAK`, `BR`, `BS` |
| Extend | `EXTEND`, `EX` |
| Trim | `TRIM`, `TR` |
| Offset | `OFFSET`, `O` |
| Fillet | `FILLET`, `F` |
| Mirror | `MIRROR`, `MI` |
| Explode | `EXPLODE`, `X` |
| Join | `JOIN`, `J` |

### Draw order

| Action | Aliases |
|---|---|
| Bring to Front | `BRINGTOFRONT`, `BTF`, `FRONT` |
| Send to Back | `SENDTOBACK`, `STB`, `BACK` |
| Bring Forward | `BRINGFORWARD`, `BF`, `FORWARD` |
| Send Backward | `SENDBACKWARD`, `SB`, `BACKWARD` |

### Object alignment and distribution

| Action | Aliases |
|---|---|
| Align Left | `ALIGNLEFT`, `ALEFT` |
| Align Right | `ALIGNRIGHT`, `ARIGHT` |
| Align Top | `ALIGNTOP`, `ATOP` |
| Align Bottom | `ALIGNBOTTOM`, `ABOTTOM` |
| Distribute Horizontally | `DISTRIBUTEHORIZONTAL`, `DISTRIBUTEHORIZONTALLY`, `DH` |
| Distribute Vertically | `DISTRIBUTEVERTICAL`, `DISTRIBUTEVERTICALLY`, `DV` |

### Navigation

| Action | Aliases |
|---|---|
| Zoom Window | `ZOOMWINDOW`, `ZW` |
| Zoom Extents | `ZOOMEXTENTS`, `ZE` |

---

## Coordinate input

The command input supports:

```text
100,50      absolute point
@50,0       relative cartesian point
@100<45     relative polar point
25          distance/angle/factor when expected
```

Empty Enter repeats the last command only when no command is active.


## Polygon

- `POLYGON` / `PG` starts the regular polygon tool.
- Workflow: number of sides, center point, vertex point or radius.
- The tool creates a closed `PolylineEntity`, so existing selection, offset, export and edit logic applies.
- Supported command input: absolute coordinates, relative coordinates, polar input and direct distance for the radius/vertex step.

---

## External image reference UI actions

The raster image reference commands are currently toolbar/dialog actions rather than command-line aliases:

| Action | Effect |
|---|---|
| Attach Image | Attach a PNG/JPG/JPEG as an external image reference. |
| Replace Image | Replace or relink the selected image reference while preserving CAD geometry. |
| Relink Missing | Relink the selected missing image, or the first missing image in the drawing. |
| Reset Aspect | Restore the selected image rectangle to the stored natural pixel aspect ratio. |
| Collect Refs | Copy existing linked raster files into an `images/` folder beside the drawing and save relative paths. |
| Manage Refs | Open the Image References Manager for status, selection, relink, replace and open-folder operations. |

All image reference mutations are executed through undoable replace/add commands in the normal command history.
