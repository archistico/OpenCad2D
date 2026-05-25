# OpenCad2D v0.9 — Release Checklist

Use this checklist before creating the GitHub release tag.

## 1. Repository state

- [ ] Working tree is clean or contains only intentional release files.
- [ ] `README.md` reflects the current v0.9 feature set.
- [ ] `docs/roadmap.md` has no obsolete pending items for completed v0.9 work.
- [ ] `docs/known-limitations.md` is aligned with the current implementation.
- [ ] `docs/ai-handoff.md` is updated for the next development session.
- [ ] Release files exist:
  - [ ] `docs/release-v0.9.md`
  - [ ] `docs/release-checklist-v0.9.md`
  - [ ] `docs/release-publish-v0.9.md`

## 2. Build and test gate

Run from the repository root:

```powershell
dotnet clean
dotnet restore
dotnet build
dotnet test
```

Expected result:

- [ ] Build succeeds.
- [ ] All tests pass.
- [ ] No new critical warnings are introduced.

If using the project workflow:

```powershell
make check
```

- [ ] Project check succeeds.

## 3. Manual application smoke test

Start the app:

```powershell
make run
```

Create a small mixed drawing and verify:

- [ ] LINE
- [ ] RECTANGLE
- [ ] CIRCLE
- [ ] ARC
- [ ] POLYLINE
- [ ] POLYGON
- [ ] ELLIPSE
- [ ] TEXT
- [ ] MTEXT
- [ ] SPLINE
- [ ] TRIM
- [ ] BREAK POINT
- [ ] BREAK SEGMENT
- [ ] EXTEND
- [ ] OFFSET
- [ ] FILLET Trim
- [ ] FILLET NoTrim
- [ ] MOVE
- [ ] COPY
- [ ] ROTATE
- [ ] SCALE
- [ ] MIRROR
- [ ] ALIGN
- [ ] EXPLODE
- [ ] JOIN
- [ ] Align Left/Right/Top/Bottom
- [ ] Distribute Horizontal/Vertical
- [ ] Dimension stale marker
- [ ] Command history Up/Down
- [ ] Command autocomplete Tab

## 4. Native persistence smoke test

- [ ] Draw a mixed file with line/circle/arc/polyline/polygon/ellipse/text/mtext/spline/dimensions.
- [ ] Add at least one filled circle and one filled closed polyline.
- [ ] Attach at least one PNG/JPG image reference.
- [ ] Save as `.opencad2d.json`.
- [ ] Reopen the file.
- [ ] Verify geometry is still present.
- [ ] Verify text and MTEXT content.
- [ ] Verify layers, line formats, text formats and dimension styles.
- [ ] Verify fill state persists.
- [ ] Verify image references reload.
- [ ] Verify stale dimension state persists when expected.

## 5. External image reference smoke test

- [ ] Attach a PNG.
- [ ] Attach a JPG/JPEG.
- [ ] Move, rotate, scale and mirror an image reference.
- [ ] Edit image origin/size/rotation in the Property Panel.
- [ ] Snap to image corners, side midpoints, center and nearest border.
- [ ] Use Reset Aspect after distorting width/height.
- [ ] Use Replace Image on a selected reference.
- [ ] Rename or move a linked image and reopen the drawing.
- [ ] Confirm the missing-image warning appears.
- [ ] Use Relink Missing.
- [ ] Open Manage Refs and verify OK/Missing status, path, pixels, CAD size, rotation and instance count.
- [ ] Use Manage Refs Select, Relink, Replace and Open Folder where applicable.
- [ ] Use Collect Refs on a saved drawing.
- [ ] Confirm an `images/` folder is created beside the drawing.
- [ ] Confirm JSON image paths are relative.
- [ ] Move the drawing and `images/` folder together, then reopen successfully.

## 6. Export smoke test

Export the mixed drawing to:

- [ ] SVG
- [ ] PDF
- [ ] DXF

Verify:

- [ ] exported files are created;
- [ ] exported files are non-empty;
- [ ] SVG opens in a browser/viewer;
- [ ] SVG preserves external raster links when the linked image file is available;
- [ ] PDF opens in a PDF viewer;
- [ ] vector geometry and supported solid fill are visible in PDF;
- [ ] DXF opens in at least one CAD viewer;
- [ ] vector geometry and supported solid fill HATCH output are visible in DXF;
- [ ] raster images are not expected in DXF/PDF for v0.9.

## 7. DXF compatibility validation

Open representative DXF output in at least one viewer, preferably more:

- [ ] LibreCAD
- [ ] QCAD
- [ ] Autodesk DWG TrueView or another Autodesk viewer, if available

Record:

- [ ] viewer name;
- [ ] viewer version;
- [ ] operating system;
- [ ] date;
- [ ] pass/partial/fail notes.

Document results in:

```text
docs/dxf-compatibility.md
```

## 8. Release notes

- [ ] `docs/release-v0.9.md` reviewed.
- [ ] GitHub release title prepared.
- [ ] Known limitations copied or linked from release notes.
- [ ] Raster-image SVG/DXF/PDF export behavior is stated clearly.

Suggested release title:

```text
OpenCad2D v0.9.0 — Stabilization, solid fill and external image references
```

## 9. Tag and publish

Recommended commands are in:

```text
docs/release-publish-v0.9.md
```

## 10. After release

Create or update the next development plan:

- [ ] v1.0 scope gate;
- [ ] DXF/PDF raster-image export decision;
- [ ] broader DXF compatibility audit;
- [ ] autosave/recovery v2;
- [ ] blocks/symbols;
- [ ] hatch/pattern workflows;
- [ ] installer/package polish;
- [ ] user manual for first external users.
