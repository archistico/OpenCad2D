# DXF Import

OpenCad2D can import a practical subset of ASCII DXF files.

DXF import is an interoperability feature. It is not the native save format. After importing a DXF file, the drawing is treated as a new unsaved OpenCad2D document and should be saved as `.opencad2d.json` if it must remain editable in OpenCad2D.

---

## Supported DXF scope

Supported scope:

```text
ASCII DXF
AutoCAD 2000 / AC1015-style files as the main target
TABLES/LAYER
ENTITIES
base 2D model-space geometry
```

Unsupported scope:

```text
binary DXF
DWG
paper-space/layout reconstruction
viewports
BLOCK / INSERT workflows
native DIMENSION entities
HATCH
SPLINE
ELLIPSE
MTEXT
IMAGE
LEADER / MLEADER
3D entities
```

Unsupported records do not stop the import. They are skipped and reported in the import diagnostics.

---

## Imported entities

| DXF entity | OpenCad2D result | Notes |
|---|---|---|
| `LINE` | `LineEntity` | zero-length lines are skipped with a warning |
| `CIRCLE` | `CircleEntity` | zero or negative radius is skipped |
| `ARC` | `ArcEntity` | angles are read from DXF degrees |
| `POINT` | `PointEntity` | missing coordinates are skipped |
| `LWPOLYLINE` | `PolylineEntity` | straight segments are supported |
| `TEXT` | `TextEntity` | single-line text only |

`LWPOLYLINE` bulge values are detected but not converted to arcs yet. When bulge values are present, the polyline is imported as straight segments and the import report shows a warning.

`TEXT` currently imports the text value, insertion point and optional rotation. Imported text uses `TextFormatId.Standard`.

---

## Layer import

The importer reads the DXF `TABLES` / `LAYER` table when available.

Imported layer information:

```text
layer name
basic ACI color
basic linetype mapping
lineweight from group code 370 where present
hidden/off state from negative ACI color
frozen state as hidden
locked state as locked
```

If an entity references a layer that is not declared in the DXF layer table, OpenCad2D creates that layer automatically.

---

## Linetype and appearance mapping

The importer maps common DXF linetype names to built-in OpenCad2D line formats:

| DXF linetype | OpenCad2D line format |
|---|---|
| `CONTINUOUS` | `LineFormatId.Continuous` |
| `DASHED` | `LineFormatId.Dashed` |
| `DASHDOT` | `LineFormatId.DashDot` |
| `DASHDOTDOT` | `LineFormatId.DashDotDot` |
| `CENTER` / `CENTER2` / `CENTERX2` | `LineFormatId.Axis` |

Custom DXF linetype-table definitions are not expanded into custom OpenCad2D line formats yet.

---

## Import behavior

The UI command is:

```text
Import DXF
```

The command replaces the current document with the imported DXF drawing.

Before replacement, OpenCad2D uses the normal dirty-document protection:

```text
Save       -> save the current document, then continue
Don't Save -> discard current unsaved changes, then continue
Cancel     -> abort import
```

After a successful DXF import:

```text
CurrentFilePath is cleared
workspace is marked dirty
viewport is reset
layer controls are refreshed
```

This is intentional. The imported DXF is not considered the native OpenCad2D file.

---

## Import diagnostics

The importer returns diagnostics and statistics. The UI shows a DXF Import Report window when warnings or errors exist.

Behavior:

```text
success without warnings -> import silently
success with warnings    -> import and show report
errors                   -> keep current document and show report
```

---

## Known limitations

- no DWG import;
- no binary DXF import;
- no native DXF `DIMENSION` import;
- no block insertion support;
- no hatches;
- no multiline `MTEXT`;
- no spline/ellipse reconstruction;
- no curved `LWPOLYLINE` bulge-to-arc conversion yet;
- no paper-space or layout reconstruction.
