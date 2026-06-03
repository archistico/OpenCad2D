# Documentation assets

This folder contains the visual material used by the OpenCad2D documentation. Images and GIFs should be committed only when they are useful to explain a real workflow, a command, a panel, or a behavior that is difficult to understand from text alone.

Static screenshots should go under `docs/assets/images/`. Short animations should go under `docs/assets/gifs/`. The file name should describe the feature, not the capture date. A name such as `main-window-overview.png` is useful; a name such as `screenshot-2026-06-03.png` is not.

The documentation is currently published through GitHub Markdown, so assets should use relative paths from the Markdown page that references them. Keep images reasonably small and crop them to the relevant area. A good documentation image shows only what the reader needs to understand the step being described.

Recommended formats are PNG for interface screenshots, SVG or PNG for diagrams, and GIF for short interactions such as pan, zoom, HUD input, snap selection, SmartPoint tracking, and command workflows.
