# Obsolete Documents to Remove

The active documentation set has been consolidated around the current v0.8.x state. The following historical files are no longer needed in the repository unless you intentionally want to keep an archive.

Recommended deletion list:

```text
docs/release-v0.4.md
docs/release-v0.5.md
docs/release-v0.6.md
docs/release-v0.7.md
docs/v0.5-modify-tools-audit.md
docs/v0.6-command-line-property-panel-plan.md
docs/v0.7-interoperability-plan.md
```

Windows command:

```bat
del docs\release-v0.4.md docs\release-v0.5.md docs\release-v0.6.md docs\release-v0.7.md docs\v0.5-modify-tools-audit.md docs\v0.6-command-line-property-panel-plan.md docs\v0.7-interoperability-plan.md
```

PowerShell command:

```powershell
Remove-Item docs/release-v0.4.md, docs/release-v0.5.md, docs/release-v0.6.md, docs/release-v0.7.md, docs/v0.5-modify-tools-audit.md, docs/v0.6-command-line-property-panel-plan.md, docs/v0.7-interoperability-plan.md
```

After deletion, run:

```bash
dotnet test OpenCad2D.sln
```
