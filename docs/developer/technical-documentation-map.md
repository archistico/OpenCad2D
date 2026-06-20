# Technical Documentation Map

This page gives maintainers a practical reading order for the technical documentation already present in `docs/`. It does not replace those documents. It explains which file to open depending on the type of work being done.

The User Guide explains OpenCad2D from the user's point of view. The technical documentation explains why features behave the way they do, how they were implemented, what edge cases were considered, and which limitations are known.

## When changing commands or tools

Start with `commands.md`, `tools.md`, `command-input.md`, `modify-tools.md`, and `transform-tools.md`. These files describe command behavior, input flow, and tool expectations.

For interaction-heavy commands, also check the relevant User Guide chapter. For example, a change to Move, Copy, Rotate, Scale, Mirror, Trim, Extend, Fillet, Chamfer, Break, Divide, Explode, or Join should normally be reflected in `user-guide/06-edit-tools.md`.

## When changing precision behavior

Start with `snapping.md`, `polar-tracking.md`, `grip-editing.md`, `geometry-intersections.md`, and `curve-editing.md`. These files are important because precision behavior affects many tools at once. A small change to snap priority, hit testing, grip editing, or curve handling can affect drawing, editing, dimensions, blocks, and image references.

If a change affects how users select points, enter values, or constrain movement, also check `user-guide/11-dynamic-hud.md`, `user-guide/14-snaps.md`, and `user-guide/15-ortho-grid-polar.md`.

## When changing appearance and styles

Start with `line-formats.md`, `text-formats.md`, `text-and-dimensions.md`, `layer-appearance.md`, and `draw-order.md`. These files describe how entities look and how appearance is inherited or overridden.

If the change is visible in the application, also update the User Guide chapters for layers, line formats, text formats, dimension styles, and the Properties Panel.

## When changing import, export, or persistence

Start with `persistence.md`, `dxf-import.md`, `dxf-export.md`, `dxf-compatibility.md`, `svg-export.md`, `pdf-export.md`, and `export.md`. These files describe storage, interchange, and output behavior.

For user-facing changes, update `user-guide/03-file-management.md`, `user-guide/13-images.md`, `user-guide/20-library.md`, or `user-guide/21-export.md`, depending on where the behavior appears in the workflow.

## When changing the Library or reusable objects

Start with `library-browser.md` and the relevant versioned specifications in `docs/specs/`. The project decision is that doors, windows, and stairs are parametric objects, while general reusable items remain static `.opencad2d.json` objects loaded from the Library.

The user-facing explanation belongs in `user-guide/20-library.md` and `user-guide/08-symbols.md`.

## When preparing a release

Start with `developer/release-and-roadmap-map.md`, then read the current release note, the matching checklist, the publishing instructions, and the latest roadmap or stabilization document.

Before publishing, confirm that the User Guide reflects the visible behavior of the application. Release notes should summarize changes; they should not be the only place where a feature is documented.

## When checking manual behavior

Use the files under `docs/testing/`. They are especially useful for workflows that depend on mouse movement, visual feedback, snap behavior, HUD focus, or other interaction details that are difficult to validate with unit tests alone.

Manual verification notes should not replace automated tests, but they are valuable evidence that a visual workflow was checked in a specific version or development phase.
