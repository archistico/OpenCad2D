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
- `ModifyEntitiesCommand`
- `CompositeCommand`
- `UpdateLayersCommand`
- `UpdateLineFormatsCommand`
- `UpdateTextFormatsCommand`

---

## Command input aliases

Aliases are case-insensitive.

### Selection

| Action | Aliases |
|---|---|
| Select | `SELECT`, `S` |
| Select All | `SELECTALL`, `SA`, `ALL` |
| Select Last | `SELECTLAST`, `SL`, `LAST` |

`Select Last` restores the previous effective selection before deselection, not the last created entity.

### Drawing

| Tool | Aliases |
|---|---|
| Point | `POINT`, `PT` |
| Text | `TEXT`, `T` |
| Line | `LINE`, `L` |
| Rectangle | `RECTANGLE`, `RECT` |
| Rectangle by sides | `RECTSIDES`, `RS` |
| Circle | `CIRCLE`, `C` |
| Arc | `ARC`, `A` |
| Arc 3P | `ARC3P`, `A3` |
| Polyline | `POLYLINE`, `PL` |

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
