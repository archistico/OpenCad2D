# OpenCad2D v0.8 — Publish Commands

Use these commands from the repository root after the final build/test gate has passed.

## 1. Final local verification

```powershell
dotnet clean
dotnet restore
dotnet build
dotnet test
git status
```

Expected result:

- build succeeds;
- all tests pass;
- no unexpected working-tree changes;
- only intentional release docs/samples are staged or committed.

## 2. Commit final release files

```powershell
git add README.md docs samples
git commit -m "docs: prepare OpenCad2D v0.8 release"
```

If there is nothing to commit because the release files are already committed, skip this step.

## 3. Create and push the tag

```powershell
git tag -a v0.8.0 -m "OpenCad2D v0.8.0"
git push
git push origin v0.8.0
```

## 4. Create the GitHub Release

Suggested title:

```text
OpenCad2D v0.8.0 — Ellipse, MTEXT, SPLINE and DXF interoperability
```

Use `docs/release-v0.8-final.md` as the release body.

## 5. Post-release branch planning

Recommended next work after v0.8:

- v0.9 stabilization milestone;
- autosave/recovery;
- PNG export;
- blocks/symbols;
- hatch/campiture;
- deeper DXF compatibility;
- visual regression testing;
- continued `CadCanvas` refactor.
