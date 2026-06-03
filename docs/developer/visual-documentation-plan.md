# Visual Documentation Plan

OpenCad2D needs visual documentation because many CAD operations are easier to understand when the user can see the cursor flow, the HUD, snap markers, panels, and final geometry. This plan defines which screenshots and GIFs should be captured first. It is intentionally simple and does not introduce any documentation tooling into the repository.

The first captures should explain the basic working environment. The main window overview should label the canvas, top toolbar, left command bar, snap and constraint controls, Dynamic HUD, Properties Panel, and status area. This image will support the Interface chapter and should be stored as `docs/assets/images/interface/main-window-overview.png`.

Canvas navigation should be documented with short GIFs. The most useful captures are pan with the middle mouse button, mouse-wheel zoom, Zoom Window, and Zoom Extents. These should be short and direct: the reader should immediately see the action and the result. Store them under `docs/assets/gifs/navigation/`.

The Dynamic HUD should have a small set of focused animations. The priority captures are line distance and angle input, move with a typed distance, fillet radius input, chamfer distance input, and rectangle by sides with width and height. These examples explain the general HUD model better than a long written description.

Snaps and SmartPoint tracking also need visual examples. Endpoint, midpoint, intersection, grid snap, and SmartPoint extension tracking should be captured separately, because each one teaches a different part of the precision workflow. When two snap candidates are close, a short zoom-in example would also be useful to show why zoom improves selection accuracy.

The Library chapter should eventually show the Library browser with categories, preview, and insertion into the drawing. The Images chapter should show Attach Image, Replace Image, Relink Missing, Reset Aspect, Collect Refs, and Manage Refs. Export should show the export commands and the expected relationship between native `.opencad2d.json` files and generated SVG, DXF, PDF, or PNG outputs.

A good capture should show a clean drawing, a visible command result, and as little unrelated UI as possible. Do not include experimental debugging windows, local paths, temporary files, or personal machine details. If an image becomes outdated because the interface changes, replace it instead of adding a second version with a date suffix.

## Initial asset checklist

| Area | Asset | Target path |
|---|---|---|
| Interface | Main window overview | `docs/assets/images/interface/main-window-overview.png` |
| Navigation | Pan canvas | `docs/assets/gifs/navigation/pan-canvas.gif` |
| Navigation | Mouse-wheel zoom | `docs/assets/gifs/navigation/mouse-wheel-zoom.gif` |
| Navigation | Zoom Window | `docs/assets/gifs/navigation/zoom-window.gif` |
| Navigation | Zoom Extents | `docs/assets/gifs/navigation/zoom-extents.gif` |
| HUD | Line distance and angle | `docs/assets/gifs/hud/line-distance-angle.gif` |
| HUD | Move with distance | `docs/assets/gifs/hud/move-distance.gif` |
| HUD | Fillet radius | `docs/assets/gifs/hud/fillet-radius.gif` |
| HUD | Chamfer distance | `docs/assets/gifs/hud/chamfer-distance.gif` |
| Snaps | Endpoint snap | `docs/assets/gifs/snaps/snap-endpoint.gif` |
| Snaps | Intersection snap | `docs/assets/gifs/snaps/snap-intersection.gif` |
| Snaps | SmartPoint extension | `docs/assets/gifs/snaps/smartpoint-extension.gif` |
| Layers | Layer manager | `docs/assets/images/layers/layer-manager.png` |
| Images | Manage refs | `docs/assets/images/images/manage-refs.png` |
| Dimensions | Dimension style manager | `docs/assets/images/dimensions/dimension-style-manager.png` |
| Library | Library browser | `docs/assets/images/library/library-browser.png` |
| Export | Export examples | `docs/assets/images/export/export-options.png` |
