# Shortcuts and Commands

OpenCad2D is designed to work both with the mouse and with command-style input. The visible interaction is centered on the Dynamic HUD, but aliases remain useful because they allow fast command activation without moving through the toolbar.

Aliases are case-insensitive. For example, `L`, `l` and `line` can all be treated as the Line command when the alias is registered.

## General keys

| Key | Behavior |
|---|---|
| `Esc` | Cancels the current input step or the active command. |
| `Enter` | Confirms the current value or step. When no command is active, it can repeat the last command. |
| `Tab` | Moves to the next editable HUD field. |
| `Shift+Tab` | Moves to the previous editable HUD field. |
| Left mouse button | Picks a point, selects an entity or confirms graphical input depending on the current command phase. |
| Right mouse button | Confirms or advances the current phase when a valid value, selection or default exists. |
| Middle mouse button | Pans the canvas while pressed. |
| Mouse wheel | Zooms in or out around the pointer. |

## Coordinate input

Command input supports absolute coordinates, relative Cartesian coordinates and relative polar coordinates.

| Input | Meaning |
|---|---|
| `100,50` | Absolute point at X 100, Y 50. |
| `@50,0` | Relative point 50 units in X from the previous point. |
| `@100<45` | Relative polar point: 100 units at 45 degrees. |
| `25` | A direct numeric value when the command expects distance, angle, radius, factor or a similar value. |

## Selection aliases

| Action | Aliases |
|---|---|
| Select | `SELECT`, `S` |
| Select All | `SELECTALL`, `SA`, `ALL` |
| Select Last | `SELECTLAST`, `SL`, `LAST` |
| Deselect | `DESELECT`, `CLEARSELECTION`, `CS` |

## Drawing aliases

| Tool | Aliases |
|---|---|
| Point | `POINT`, `PO` |
| Divide | `DIVIDE`, `DIV` |
| Text | `TEXT`, `T` |
| Multiline Text | `MTEXT`, `MT` |
| Line | `LINE`, `L` |
| Rectangle | `RECTANGLE`, `RECT` |
| Rectangle by Sides | `RECTSIDES`, `RS` |
| Circle | `CIRCLE`, `C` |
| Ellipse | `ELLIPSE`, `EL` |
| Arc | `ARC`, `A` |
| Arc 3P | `ARC3P`, `A3` |
| Polyline | `POLYLINE`, `PL` |
| Spline | `SPLINE`, `SPL` |

## Dimension aliases

| Tool | Aliases |
|---|---|
| Horizontal Dimension | `HORIZONTALDIMENSION`, `HDIM` |
| Vertical Dimension | `VERTICALDIMENSION`, `VDIM` |
| Aligned Dimension | `ALIGNEDDIMENSION`, `ADIM` |
| Radius Dimension | `RADIUSDIMENSION`, `RDIM` |
| Diameter Dimension | `DIAMETERDIMENSION`, `DDIM` |
| Angular Dimension | `ANGULARDIMENSION`, `ANGDIM` |

## Edit and transform aliases

| Tool | Aliases |
|---|---|
| Delete | `DELETE`, `DEL` |
| Move | `MOVE`, `M` |
| Copy | `COPY`, `CP` |
| Rotate | `ROTATE`, `RO` |
| Scale | `SCALE`, `SC` |
| Align by Points | `ALIGN` |
| Break Point | `BREAKPOINT`, `BP` |
| Break Segment | `BREAKSEGMENT`, `BREAK`, `BR`, `BS` |
| Extend | `EXTEND`, `EX` |
| Trim | `TRIM`, `TR` |
| Offset | `OFFSET`, `O` |
| Fillet | `FILLET`, `F` |
| Mirror | `MIRROR`, `MI` |
| Explode | `EXPLODE`, `X` |
| Join | `JOIN`, `J` |

## Draw order aliases

| Action | Aliases |
|---|---|
| Bring to Front | `BRINGTOFRONT`, `BTF`, `FRONT` |
| Send to Back | `SENDTOBACK`, `STB`, `BACK` |
| Bring Forward | `BRINGFORWARD`, `BF`, `FORWARD` |
| Send Backward | `SENDBACKWARD`, `SB`, `BACKWARD` |

## Object alignment aliases

| Action | Aliases |
|---|---|
| Align Left | `ALIGNLEFT`, `ALEFT` |
| Align Right | `ALIGNRIGHT`, `ARIGHT` |
| Align Top | `ALIGNTOP`, `ATOP` |
| Align Bottom | `ALIGNBOTTOM`, `ABOTTOM` |
| Distribute Horizontally | `DISTRIBUTEHORIZONTAL`, `DISTRIBUTEHORIZONTALLY`, `DH` |
| Distribute Vertically | `DISTRIBUTEVERTICAL`, `DISTRIBUTEVERTICALLY`, `DV` |

## Navigation aliases

| Action | Aliases |
|---|---|
| Zoom Window | `ZOOMWINDOW`, `ZW` |
| Zoom Extents | `ZOOMEXTENTS`, `ZE` |

This chapter should be checked whenever a command is added, renamed or removed. The command registry, toolbar labels and documentation must stay aligned.
