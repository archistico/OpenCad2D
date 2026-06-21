# Release and Roadmap Map

This page explains how to read the release, roadmap, publishing, and stabilization documents currently stored in the root of `docs/`. The purpose is not to move files yet. The purpose is to make the current structure understandable while the project is still evolving quickly.

For now, release and roadmap files remain where they are. They contain useful history and practical publishing notes, and moving them too early would create unnecessary link maintenance. When the documentation structure becomes stable, these files may be moved into a dedicated release or project-planning area.

## Current planning documents

`roadmap.md` is the main project roadmap and the current source of truth after the 2026-06-21 documentation reconciliation. It should be the first file to check when deciding what belongs in the next milestone.

`roadmap-v0.8.100.md` is the detailed continuation document for the active v0.8.100+ line. It records that Import Drawing, Blocks v1, the Dynamic Command HUD, Library Browser, parametric Stairs and Boundary Fill v2 are implemented for their current scopes, then defines the v0.8.160+ consolidation, the first Library content pack, the planning specification pass and the next feature sequence.

`stabilization-v0.9-plan.md` is now a future stabilization-gate checklist. Use it only after the v0.8.160+ reconciliation, Library content and compatibility pass has decided what is allowed into the next public stabilization release. It should not be used as the active feature roadmap.

## Release notes

`release-v0.8.md`, `release-v0.8-final.md`, and `release-v0.9.md` are release-note documents. They should be read as user-facing or semi-user-facing summaries of what changed in a version line.

When preparing a new release, start from the most recent relevant release note and update it with the actual state of the code. Avoid copying old notes forward without verifying that the behavior still matches the application.

## Release checklists

`release-checklist-v0.8.md` and `release-checklist-v0.9.md` are operational checklists. They are useful before tagging or publishing a release because they collect the practical verification steps that should not be forgotten.

These files are not intended to explain features. They are project-maintenance documents. Feature explanations belong in the User Guide or in the technical reference files.

## Publishing instructions

`release-publish-v0.8.md` and `release-publish-v0.9.md` describe publishing procedures. They should be checked when creating a GitHub release, preparing artifacts, or validating the release package.

Publishing instructions may become outdated faster than user documentation. Before using them, compare them with the current project files, current Makefile targets, and the current release workflow.

## How to keep these files updated

When a release is prepared, update the release note, the matching checklist, and the publishing instructions together. If a feature is added or changed, update the User Guide first, then update the release note with a concise summary.

The release documents should not become a second User Guide. They should answer what changed, what was verified, and how the version is published. The detailed explanation of how to use each feature should stay in `docs/user-guide/`.

## Future cleanup

A future cleanup may move these files into a structure like this:

```text
docs/releases/
  v0.8/
  v0.9/

docs/project/
  roadmap.md
  stabilization-v0.9-plan.md
```

That cleanup should happen only when the documentation links are stable and when the project is ready to spend time on file organization. Until then, the current root-level files should remain in place and be indexed from this page.


The current manual validation entry points are `docs/testing/v0.8.170A-block-manager-inventory-diagnostics-checklist.md` for Blocks v2 inventory/diagnostics, `docs/testing/v0.8.170B-block-manager-duplicate-purge-checklist.md` for Blocks v2 duplicate/delete/purge behavior, `docs/testing/v0.8.170C-block-edit-session-hardening-checklist.md` for Edit Block session safety, `docs/testing/v0.8.170D-library-block-conflict-policy-checklist.md` for Library/block conflict handling and `docs/testing/v0.8.162-compatibility-smoke-checklist.md` for the broader consolidation pass. Use them before marking the first Library pack and Blocks v2 slices complete. The shared planning contracts added in `docs/specs/shared-*.md` should be read before implementing the remaining Blocks v2 slices, doors/windows, arrays, hatch, annotation markers, UI customization or icon SVG workflow.
