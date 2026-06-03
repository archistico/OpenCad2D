# Documentation Review Notes

This review covers the current `docs` folder after the English User Guide structure and operational chapters were added.

The documentation is now organized well enough to continue with content refinement and visual assets. The User Guide exists in English, the old Italian user-guide file names are no longer present, and the main `docs/index.md` links to the current user-facing chapters.

The root of `docs` still contains many technical documents. They should be kept for now because they preserve useful implementation details, release notes, compatibility notes, and testing history. Moving them into a deeper hierarchy can be done later, but only after checking and updating references carefully.

The documentation folder should remain lightweight. Do not add Python scripts, documentation tooling folders, generated site folders, or local maintenance utilities to the repository. If we need checks later, they should either be run outside the repository or added only after there is a clear project-level decision to support that workflow.

The next documentation work should focus on screenshots and GIFs. The highest-value captures are the main interface overview, canvas pan and zoom, Zoom Window, Zoom Extents, Dynamic HUD numeric input, snap selection, SmartPoint tracking, layer manager, image references, dimension style manager, Library browser, and export dialogs.

A later cleanup phase can introduce clearer technical sections such as `reference/`, `development/`, and `releases/`. That should not be done until the User Guide is stable and the release documentation for v0.8/v0.9 has been reviewed.
