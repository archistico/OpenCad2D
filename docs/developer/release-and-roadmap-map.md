# Release and Roadmap Map

This page explains how to read the release, roadmap, publishing, and stabilization documents currently stored in the root of `docs/`. The purpose is not to move files yet. The purpose is to make the current structure understandable while the project is still evolving quickly.

For now, release and roadmap files remain where they are. They contain useful history and practical publishing notes, and moving them too early would create unnecessary link maintenance. When the documentation structure becomes stable, these files may be moved into a dedicated release or project-planning area.

## Current planning documents

`roadmap.md` is the main project roadmap. It should be treated as the broad direction of the project and should remain the first file to check when deciding what belongs in the next milestone.

`stabilization-v0.9-plan.md` is the working stabilization plan for the v0.9 line. Use it when checking what still needs consolidation before moving toward a more stable public release.

`roadmap-v0.8.100.md` is historical roadmap material for the v0.8.100 phase. Keep it as context, but do not treat it as the current roadmap unless a task explicitly refers to that version.

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
