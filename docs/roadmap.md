# Roadmap

OpenCad2D grows in small, testable phases. Each phase should compile, pass tests and update documentation before the next phase begins.

Legend:

```text
[x] done
[~] partially implemented
[ ] planned
```

---

## Current baseline: v0.8.x usability stabilization

The current codebase includes the v0.8 command-input work plus a v0.8.x usability pass.

Completed:

- [x] clean startup from `Templates/default.opencad2d.json`;
- [x] maximized main window on startup;
- [x] About dialog updated with `info@opencad2d.org` and `www.opencad2d.org`;
- [x] modal windows centered on owner;
- [x] improved save-changes dialog;
- [x] document recovery for partially invalid `.opencad2d.json` files;
- [x] document-level settings persistence in `.opencad2d.json`;
- [x] compact ColorPicker integration for line/text formats;
- [x] line format dash patterns in drawing units;
- [x] Line Format Manager pattern editor and preview;
- [x] command input with contextual prompt, aliases, coordinates, relative coordinates and polar input;
- [x] command-driven drawing tools;
- [x] command-driven edit/modify tools;
- [x] advanced base Trim workflow with `All` and `Undo`;
- [x] Offset for lines, circles, arcs and straight-segment polylines;
- [x] Offset preview and additional safety tests;
- [x] line-line Fillet with Radius option;
- [x] draw order / Z-order independent from layers;
- [x] draw order actions: To Front, To Back, Forward, Backward;
- [x] draw order display in Property Panel;
- [x] Align Left / Right / Top / Bottom;
- [x] Distribute Horizontally / Vertically by centers;
- [x] UI tooltips and command input layout cleanup.

---

## Implemented foundations

- geometry primitives and tolerance strategy;
- CAD entities and document model;
- layers with hidden/locked behavior;
- line formats, text formats and dimension styles;
- undoable command architecture;
- selection, hit testing and snapping;
- grid and viewport management;
- drawing, dimension, transform, modify, measure and navigation tools;
- command input and command aliases;
- native persistence and recovery;
- SVG, DXF and PDF export;
- DXF import for base 2D entities;
- Avalonia custom canvas and crosshair;
- property panel and manager dialogs;
- automated tests across Core, Interaction, Tools, Persistence, Export and App.

---

## Pre-v0.9 cleanup

Before opening the v0.9 phase:

- [ ] run a full `dotnet build` and `dotnet test` on Windows;
- [ ] manually verify offset preview for line, circle, arc and polyline;
- [ ] manually verify line format pattern persistence and SVG export;
- [ ] manually verify draw order with overlapping entities and hit testing;
- [ ] manually verify align/distribute operations with Undo;
- [ ] remove obsolete historical release/planning documents from the repository;
- [ ] publish or tag the current v0.8.x state if desired.

---

## v0.9 - Release candidate stabilization

Goal: make the existing feature set reliable enough for broader testing.

Planned focus:

- [ ] improve external DXF compatibility testing with LibreCAD/QCAD/AutoCAD viewers;
- [ ] add more end-to-end save/reopen/export workflow tests;
- [ ] strengthen document recovery UI and reporting;
- [ ] continue offset/trim/extend geometric edge-case hardening;
- [ ] improve command input ergonomics, including optional command history navigation;
- [ ] review all manager dialogs for consistency and keyboard usability;
- [ ] polish README screenshots and user-facing documentation.

---

## v1.0 - First stable release

Criteria:

- [ ] no known data-loss bugs in `.opencad2d.json` save/reopen;
- [ ] stable layer, line format and text format workflows;
- [ ] stable command input for supported tools;
- [ ] stable selection, snapping, undo/redo and property editing;
- [ ] reliable export to SVG, DXF, PDF and PNG if implemented by then;
- [ ] clear known limitations document;
- [ ] release notes and basic user guide ready.

---

## Post-v1.0 ideas

- advanced DXF custom linetype definitions;
- polyline offset with rounded joins and advanced self-intersection cleanup;
- fillet line-arc, arc-arc and polyline fillet;
- chamfer;
- array tools;
- match properties;
- richer dimension editing;
- command history navigation and autocomplete;
- configurable keyboard shortcuts;
- PNG export;
- plugin or scripting exploration.
