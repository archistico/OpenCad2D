# OpenCad2D Documentation

OpenCad2D is a free and open-source 2D CAD application focused on precise technical drawing. It is designed as a native 2D CAD: the project deliberately concentrates on drawing, editing, dimensions, layers, references, libraries, and export workflows instead of moving toward 3D modeling.

The documentation is kept inside the repository and is currently published through GitHub. This keeps the guide close to the code, makes documentation changes reviewable with normal commits, and lets each release carry the documentation that belongs to it.

## Start here

If you are new to OpenCad2D, start with the User Guide. The first chapters explain the interface, canvas navigation, file management, precision input, snaps, and layers. After that, the guide moves through drawing, selection, editing, dimensions, images, the Library, and export.

- [Introduction](user-guide/00-introduction.md)
- [Interface](user-guide/01-interface.md)
- [Canvas Navigation](user-guide/02-canvas-navigation.md)
- [File Management](user-guide/03-file-management.md)
- [Draw Tools](user-guide/04-draw-tools.md)
- [Select Tools](user-guide/05-select-tools.md)
- [Edit Tools](user-guide/06-edit-tools.md)
- [Align Objects](user-guide/07-align-objects.md)
- [Symbols](user-guide/08-symbols.md)
- [Dimensions](user-guide/09-dimensions.md)
- [Measure Tools](user-guide/10-measure-tools.md)
- [Dynamic HUD](user-guide/11-dynamic-hud.md)
- [Properties Panel](user-guide/12-properties-panel.md)
- [Images](user-guide/13-images.md)
- [Snaps](user-guide/14-snaps.md)
- [Ortho, Grid, and Polar Tracking](user-guide/15-ortho-grid-polar.md)
- [Layers](user-guide/16-layers.md)
- [Line Formats](user-guide/17-line-formats.md)
- [Text Formats](user-guide/18-text-formats.md)
- [Dimension Styles](user-guide/19-dimension-styles.md)
- [Library](user-guide/20-library.md)
- [Export](user-guide/21-export.md)
- [Shortcuts and Commands](user-guide/22-shortcuts-and-commands.md)
- [Troubleshooting](user-guide/23-troubleshooting.md)

## Technical and project documentation

The repository also contains technical documentation created while developing OpenCad2D. These documents are useful for maintainers because they record design decisions, feature specifications, test notes, import/export behavior, release plans, and known limitations.

The main entry points are:

- [Documentation Guidelines](developer/documentation-guidelines.md)
- [Existing Technical Documentation](developer/existing-technical-docs.md)
- [Technical Documentation Map](developer/technical-documentation-map.md)
- [Release and Roadmap Map](developer/release-and-roadmap-map.md)
- [Documentation Cleanup Plan](developer/documentation-cleanup-plan.md)
- [Documentation Review](developer/documentation-review.md)
- [Visual Documentation Plan](developer/visual-documentation-plan.md)
- [Image Capture Guidelines](developer/image-capture-guidelines.md)

Root-level technical documents such as `architecture.md`, `geometry-intersections.md`, `curve-editing.md`, `commands.md`, `snapping.md`, `export.md`, `dxf-import.md`, `dxf-export.md`, `pdf-export.md`, `svg-export.md`, `modify-tools.md`, `text-and-dimensions.md`, and `ai-handoff.md` are intentionally kept for now. They should be reorganized gradually, not deleted aggressively, because they contain useful implementation history and project decisions.

## Release, roadmap, and verification notes

Release and roadmap documents are kept in `docs/` until the project decides on a more formal release documentation structure. Use [Release and Roadmap Map](developer/release-and-roadmap-map.md) as the entry point before preparing a release, updating a milestone, or checking which planning document is current.

The current source of truth is [roadmap.md](roadmap.md), with the detailed v0.8.100+ continuation in [roadmap-v0.8.100.md](roadmap-v0.8.100.md). The 2026-06-21 reconciliation added specifications for the next planned feature families: [Blocks v2](specs/v0.8.170-blocks-v2.md), [Parametric Doors and Windows](specs/v0.8.180-parametric-doors-windows.md), [Array Tools](specs/v0.8.190-array-tools.md), [Hatch Patterns](specs/v0.8.200-hatch-patterns.md), [Annotation Markers](specs/v0.8.210-annotation-markers.md), [UI Customization](specs/v0.8.220-ui-customization.md), and [Icon SVG Workflow](specs/v0.8.230-icon-svg-workflow.md). The planning specification pass also added shared contracts for the [9-point anchor system](specs/shared-anchor-system.md), [leader and arrowhead system](specs/shared-leader-arrow-system.md), [preview/commit/grouped undo workflow](specs/shared-preview-and-commit-workflow.md), and [wall mask/opening behavior](specs/shared-wall-mask-openings.md).

Manual verification notes are stored under `docs/testing/`. They are not a replacement for automated tests, but they are useful for validating workflows that depend on visual behavior, snapping, the Dynamic HUD, or user interaction. Boundary Fill v2 has a dedicated [manual checklist](testing/boundary-fill-v2-manual-checklist.md) because its preview/confirm, gap-tolerance and curve-sampling behavior is partly visual. Blocks v2 slice 170A has a dedicated [manual checklist](testing/v0.8.170A-block-manager-inventory-diagnostics-checklist.md) for reference-count and diagnostic behavior, and slice 170B has a dedicated [manual checklist](testing/v0.8.170B-block-manager-duplicate-purge-checklist.md) for duplicate/delete/purge behavior. For technical reading order, use [Technical Documentation Map](developer/technical-documentation-map.md).

## Images and GIFs

Documentation images and GIFs belong under `docs/assets/`. The User Guide may reference images before the actual assets are captured, as long as the intended file name is stable. This lets the text and visual plan evolve together.

The first visual priority is the main interface overview, followed by canvas navigation, Dynamic HUD input, snaps, layers, image references, dimension styles, the Library browser, and export workflows.


Developer note: the first Library content pack is documented in `docs/specs/v0.8.161-library-content-pack.md` and stored under the repository-level `library/` folder.
The v0.8.170A Block Manager checklist is stored in `docs/testing/v0.8.170A-block-manager-inventory-diagnostics-checklist.md`; the v0.8.170B duplicate/purge checklist is stored in `docs/testing/v0.8.170B-block-manager-duplicate-purge-checklist.md`; the broader v0.8.162 manual compatibility checklist remains in `docs/testing/v0.8.162-compatibility-smoke-checklist.md`.
