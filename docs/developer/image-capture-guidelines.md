# Image Capture Guidelines

Documentation images should make OpenCad2D easier to understand, not simply prove that a feature exists. Every screenshot or GIF should support a specific paragraph in the User Guide or a specific technical note.

Use static screenshots for panels, dialogs, toolbars, managers, and before/after states. Use GIFs for actions that depend on movement or sequence, such as panning, zooming, using the Dynamic HUD, selecting snap points, and inserting Library objects.

Keep captures clean. Start from a small drawing prepared for documentation, hide unrelated windows, avoid showing local paths where possible, and crop the capture to the useful area. If a full-window screenshot is needed, use it only when the page is explaining the global interface.

Use lowercase file names with hyphens. The name should describe the content, for example `zoom-window.gif`, `snap-intersection.gif`, or `dimension-style-manager.png`. Do not use date-based names for ordinary documentation assets.

Store PNG screenshots under `docs/assets/images/` and GIF animations under `docs/assets/gifs/`. Choose the subfolder that matches the chapter using the asset. If no suitable subfolder exists, create a simple descriptive one, but do not add tooling or generated-site folders to the repository.
