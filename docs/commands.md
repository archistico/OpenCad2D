# Commands

OpenCad2D uses undoable document commands for mutations and command-line aliases for user actions. The visible command surface is the dynamic HUD; the internal command buffer still accepts aliases, options, autocomplete and history navigation without a fixed bottom command row.

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

Fillet supports Line-Line, adjacent straight segments of the same polyline, and terminal segments of separate open linear polylines. Line-Line remains supported with Radius and Trim/NoTrim options.

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
| Manage Refs | Open the Image References Manager for status, selection, transparency percentage, relink, replace and open-folder operations. |

All image reference mutations are executed through undoable replace/add commands in the normal command history.



## Insert Block

`Insert Block` creates a new instance of an existing block definition. The command opens an options dialog where the user selects the block definition and enters a uniform scale plus rotation in degrees. After confirmation, the canvas/HUD asks only for the insertion point through click/snap or editable `X/Y` fields. Escape cancels the pending insertion without modifying the document and clears stale HUD coordinates. Undo removes the inserted block reference as a single operation.

## Create Block

`Create Block` converts the current non-empty selection into a reusable block definition. The dialog shows the selected-entity counter and blocks empty creation. It can temporarily close for entity selection and then reopen: normal single selection returns immediately, while `Shift` selection stays active until `Enter`. The base point can be typed numerically, entered through HUD `X/Y`, or picked from the drawing; picked points return to the dialog for review. The selected entities are stored in local block coordinates and replaced by a single `BlockReferenceEntity` only when OK is pressed. The operation is undoable as one step.

Current limitation: nested blocks are not supported yet. Existing block references should be exploded before they are included in a new block.

## Polyline three-point arc segments

The `Polyline` command now supports mixed straight and curved segments while keeping a single `PolylineEntity`.

Workflow:

1. Start `Polyline` and specify the first vertex.
2. Specify straight vertices normally, or choose `Arc` / `A`.
3. In arc mode, the previous polyline vertex is the arc start point.
4. Specify the point on the arc.
5. Specify the arc endpoint.
6. The tool stores the resulting segment as a DXF-compatible bulge on the polyline segment.
7. The next segment returns to straight mode; choose `Arc` again to draw another curved segment.

`Undo` while entering a three-point arc first cancels the pending arc point; another undo returns to regular straight-segment input. A polyline cannot be completed while an arc segment is half-entered.

### EXPLODE mixed polyline support

`EXPLODE` accepts selected polylines and block references. For polylines, the command now reads each segment instead of assuming that every segment is straight:

- straight segments (`bulge == 0`) become `LineEntity`;
- curved segments (`bulge != 0`) become `ArcEntity`;
- closed polylines also explode their closing segment;
- layer, style, visibility, lock state and draw order are preserved on the generated entities;
- undo restores the original polyline or block reference through the existing modify-entities command flow.

This makes `EXPLODE` the inverse workflow for mixed polylines created by three-point polyline arcs, DXF LWPOLYLINE bulges or `JOIN` of line/arc chains.

### JOIN diagnostic feedback and mixed polylines

`JOIN` now accepts lines, arcs and open polylines. The command creates one or more `PolylineEntity` results. Arc geometry is preserved as per-segment bulge values, so joining a line with an arc creates a mixed polyline instead of exploding the arc into a separate entity.

The command is deliberately explicit when it cannot complete the operation:

- unsupported entities return `Only lines, arcs and open polylines can be joined.`;
- closed polylines return `Closed polylines cannot be joined.`;
- selections with different layer/style visibility metadata return `Selected entities use different layers or styles and cannot be joined.`;
- disconnected selections return `Selected entities do not touch at endpoints.`;
- branching junctions return `Selected entities create a branching junction and cannot be joined into a single polyline.`.

Disconnected valid chains are still supported: selecting two independent chains can create two polylines in one command.

### FILLET / CHAMFER polyline segment picking

When `FILLET` or `CHAMFER` is active, clicking a linear `PolylineEntity` selects the closest linear segment, not only the whole entity.
After the first segment is selected, the second pick on the same polyline ignores the first segment and can resolve the adjacent segment at a shared vertex.

### FILLET / CHAMFER polyline segment notes

`FILLET` and `CHAMFER` can be used by clicking linear polyline segments. When selecting a second segment on the same polyline, the command ignores the first segment already picked so that clicks near a shared vertex resolve the adjacent segment.

### CHAMFER separate simple polylines

`CHAMFER` supports standalone lines, adjacent straight segments of the same linear polyline, separate open single-segment linear polylines, and mixed line/polyline pairs. Multi-segment polylines are supported only when the selected segment is terminal; internal-segment trims are rejected with a conservative diagnostic.
