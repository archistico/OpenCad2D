# OpenCad2D v0.8 — Final Release Checklist

Use this checklist before creating the GitHub release tag.

## Current final-gate status

The v0.8 DXF compatibility sample set has been manually opened successfully. Exact external viewer names and versions were not recorded during this pass; record them in a future compatibility audit when available.

The remaining mandatory local gate before tagging is:

```powershell
dotnet clean
dotnet restore
dotnet build
dotnet test
git status
```


## 1. Repository state

- [ ] Working tree is clean or contains only intentional release files.
- [ ] Latest documentation packages have been applied.
- [ ] `README.md` reflects the current v0.8 feature set.
- [ ] `docs/roadmap.md` has no obsolete pending items for completed v0.8 work.
- [ ] `docs/known-limitations.md` is aligned with the current implementation.
- [ ] `docs/ai-handoff.md` is updated for the next development session.

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

If using the project check script/workflow:

```powershell
make check
```

- [ ] Project check succeeds.

## 3. Manual application smoke test

Create a small mixed drawing and verify these tools manually:

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
- [ ] OFFSET
- [ ] FILLET Trim
- [ ] FILLET NoTrim
- [ ] MOVE
- [ ] COPY
- [ ] ROTATE
- [ ] SCALE
- [ ] MIRROR
- [ ] Dimensions stale marker
- [ ] Command history Up/Down
- [ ] Command autocomplete Tab

## 4. Persistence smoke test

- [ ] Draw a mixed file with line/circle/arc/polyline/polygon/ellipse/text/mtext/spline/dimensions.
- [ ] Save as `.opencad2d.json`.
- [ ] Reopen the file.
- [ ] Verify geometry is still present.
- [ ] Verify text and MTEXT content.
- [ ] Verify layers and line/text formats.
- [ ] Verify stale dimension state persists when expected.

## 5. Export smoke test

Export the mixed drawing to:

- [ ] SVG
- [ ] PDF
- [ ] DXF

Verify:

- [ ] exported files are created;
- [ ] exported files are non-empty;
- [ ] SVG opens in a browser/viewer;
- [ ] PDF opens in a PDF viewer;
- [ ] DXF opens in at least one CAD viewer.

## 6. DXF compatibility sample validation

Open all sample files in `samples/dxf/compatibility/`.

- [ ] `01_basic_lines_layers.dxf`
- [ ] `02_text_mtext.dxf`
- [ ] `03_arcs_circles_ellipses.dxf`
- [ ] `04_polylines_polygons.dxf`
- [ ] `05_dimensions_as_geometry.dxf`
- [ ] `06_spline_bezier.dxf`
- [ ] `07_mixed_drawing.dxf`

Recommended viewers:

- [ ] LibreCAD
- [ ] QCAD
- [ ] Autodesk DWG TrueView, if available

Document results in:

```text
docs/dxf-compatibility.md
```

## 7. Release notes

- [ ] `docs/release-v0.8-final.md` reviewed.
- [ ] GitHub release title prepared.
- [ ] Known limitations copied or linked from release notes.
- [ ] Manual DXF validation result mentioned.

Suggested release title:

```text
OpenCad2D v0.8.0 — Ellipse, MTEXT, SPLINE and DXF interoperability
```

## 8. Tag and publish

Recommended commands:

```powershell
git status
git add README.md docs samples
git commit -m "docs: prepare OpenCad2D v0.8 release"
git tag -a v0.8.0 -m "OpenCad2D v0.8.0"
git push
git push origin v0.8.0
```

Then create the GitHub release using `docs/release-v0.8-final.md` as the body.

## 9. After release

Create or update the next development plan:

- [ ] v0.9 stabilization milestone
- [ ] autosave/recovery
- [ ] PNG export
- [ ] blocks/symbols
- [ ] hatch/campiture
- [ ] deeper DXF compatibility
- [ ] visual regression testing
- [ ] further `CadCanvas` refactor
