# OpenCad2D v0.9 — Publish Commands

Use these commands from the repository root after the final build/test/manual gate has passed.

## 1. Final local verification

```powershell
dotnet clean
dotnet restore
dotnet build
dotnet test
git status
```

Optional project workflow:

```powershell
make check
make run
```

Expected result:

- build succeeds;
- all tests pass;
- no unexpected warnings or working-tree changes;
- only intentional release docs/samples are staged or committed.

## 2. Review release files

```powershell
git diff -- README.md docs/roadmap.md docs/known-limitations.md docs/ai-handoff.md docs/release-v0.9.md docs/release-checklist-v0.9.md docs/release-publish-v0.9.md
```

Confirm that:

- completed v0.9 work is marked complete;
- known limitations are explicit;
- raster-image export behavior is not overstated;
- release notes match the tested application.

## 3. Commit final release files

```powershell
git add README.md docs
git commit -m "docs: prepare OpenCad2D v0.9 release"
```

If there is nothing to commit because the release files are already committed, skip this step.

## 4. Create and push the tag

```powershell
git tag -a v0.9.0 -m "OpenCad2D v0.9.0"
git push
git push origin v0.9.0
```

## 5. Create the GitHub Release

Suggested title:

```text
OpenCad2D v0.9.0 — Stabilization, solid fill and external image references
```

Use `docs/release-v0.9.md` as the release body.

## 6. Optional local artifact publish

If a publish target exists in the project Makefile, prefer that. Otherwise, a basic Windows self-contained-free publish that still requires the .NET runtime can be produced with:

```powershell
dotnet publish .\src\OpenCad2D.App\OpenCad2D.App.csproj -c Release -r win-x64 --self-contained false -o .\artifacts\publish\win-x64
```

For cross-platform artifacts, repeat with the target runtime identifiers that the project officially supports and manually smoke-test each package before attaching it to the GitHub release.

## 7. Post-release branch planning

Recommended next work after v0.9:

- v1.0 scope consolidation;
- DXF/PDF raster-image export investigation;
- reference-image export compatibility notes;
- broader DXF compatibility audit;
- autosave/recovery v2;
- blocks/symbols;
- hatch/pattern tools;
- installer/package polish;
- first external user manual.
