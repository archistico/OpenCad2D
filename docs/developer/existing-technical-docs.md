# Existing Technical Documentation

The root of the `docs` folder contains a large set of technical and project documents created during the development of OpenCad2D. These files should be preserved while the User Guide is being completed. They record decisions, implementation details, compatibility notes, release plans, manual verification sessions, and known limitations that are still useful for future work.

Do not reorganize or delete these files aggressively. A cleanup can happen later, but it should be done deliberately and with link updates, not as a cosmetic move.

## How to read the current documentation

The User Guide under `docs/user-guide/` is the user-facing manual. It should remain in English and should explain workflows from the point of view of someone using the application.

The root-level Markdown files are mostly technical reference or project history. They are useful when changing code, fixing regressions, preparing a release, or checking why a feature behaves in a specific way.

The `docs/specs/` folder contains versioned feature specifications. These documents are especially useful when a feature was implemented across several steps and the final behavior needs to be compared against the original plan.

The `docs/testing/` folder contains manual verification notes and regression checklists. These should be kept because several CAD behaviors are visual or interaction-heavy and cannot be fully represented by unit tests alone.

## Entry maps

Use [Technical Documentation Map](technical-documentation-map.md) when you need a reading order for implementation work, precision behavior, import/export changes, style changes, or manual verification. Use [Release and Roadmap Map](release-and-roadmap-map.md) when preparing a release or reviewing roadmap material.

## Root-level technical references

Several root-level files describe established behavior and should remain easy to find:

- `architecture.md` records architectural decisions and high-level structure.
- `commands.md`, `tools.md`, `command-input.md`, `modify-tools.md`, and `transform-tools.md` describe command behavior and tool workflows.
- `snapping.md`, `polar-tracking.md`, `grip-editing.md`, and `curve-editing.md` document precision and editing behavior.
- `line-formats.md`, `text-formats.md`, `text-and-dimensions.md`, and `layer-appearance.md` describe drawing appearance.
- `dxf-import.md`, `dxf-export.md`, `dxf-compatibility.md`, `svg-export.md`, `pdf-export.md`, and `export.md` describe interchange and output behavior.
- `persistence.md`, `application-settings.md`, `library-browser.md`, and `known-limitations.md` contain project-level behavior that affects users and maintainers.

These documents may overlap with the User Guide. That is acceptable for now. The User Guide explains how to use the feature; the technical documents preserve implementation and design context.

## Release and roadmap documents

The release and roadmap files currently live in the root of `docs`. They should be treated as project planning material, not as user manual pages. Before publishing a release, check [Release and Roadmap Map](release-and-roadmap-map.md), the current release note, the matching checklist, and the stabilization plan when applicable.

Relevant documents include `roadmap.md`, `roadmap-v0.8.100.md`, `stabilization-v0.9-plan.md`, `release-v0.8.md`, `release-v0.8-final.md`, `release-v0.9.md`, `release-checklist-v0.8.md`, `release-checklist-v0.9.md`, `release-publish-v0.8.md`, and `release-publish-v0.9.md`.

A later cleanup may move these into `docs/releases/` or `docs/project/`, but for now it is safer to keep them where they are.

## Recommended future organization

A future documentation cleanup could organize the technical material like this:

```text
docs/reference/      command behavior, formats, import/export, snapping, persistence
docs/development/    architecture, project structure, testing, implementation notes
docs/releases/       release notes, publish instructions, release checklists, roadmaps
docs/specs/          versioned feature specifications
docs/testing/        manual verification and regression notes
```

This should happen only after the User Guide is stable and after internal links have been reviewed. The project should not add script folders or tooling folders just to manage documentation. The documentation should remain simple Markdown plus assets.
