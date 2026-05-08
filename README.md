# OpenCad2D

**OpenCad2D** is an experimental open-source 2D CAD application built with **C#**, **.NET 8** and **Avalonia UI**.

The project started as an attempt to build a small but clean CAD system from the ground up, with a strong separation between the geometric core, the document model, the interaction logic and the graphical user interface.

The long-term goal is not only to create a usable 2D CAD application, but also to keep the code understandable, testable and easy to extend.

![OpenCad2D screenshot](screenshot.png)

---

## Project status

OpenCad2D is currently an early prototype.

It is not meant to replace mature CAD software yet. The current focus is on building a solid internal architecture: geometry, entities, commands, snapping, selection, tools and a first cross-platform UI.

Even at this early stage, the application already supports basic drawing and editing workflows. You can draw lines and rectangles, select entities, move them, copy them, delete them and use undo/redo. The Avalonia UI includes a drawing canvas, zoom, pan, grid display, snap markers and a status bar.

---

## What OpenCad2D can do today

The current prototype includes a small but functional CAD core.

The geometry layer contains basic 2D primitives such as points, vectors, line segments, lines, circles, arcs, polylines and bounding boxes. It also includes operations for distances, transformations and intersections.

The core document model supports CAD entities such as lines, circles, arcs and polylines. Rectangles are represented as closed polylines. The document is modified through commands, so operations such as adding, deleting, moving, copying, rotating, scaling and mirroring entities can be handled consistently and can participate in undo/redo.

Interaction logic is kept outside the UI. Selection, hit testing and object snapping are implemented in dedicated libraries. This makes the application easier to test and keeps the Avalonia layer thin.

The current snapping system supports endpoint, midpoint, center, quadrant, intersection, perpendicular, tangent and grid snapping. The UI shows a visual snap marker and displays the current snap type in the status bar.

The tool system is also UI-independent. Tools such as Selection, Line, Rectangle, Move, Copy and Delete work through a shared tool context and are coordinated by a tool controller. This means the same logic can be tested without launching the graphical application.

---

## User interface

The current desktop application is built with Avalonia UI.

The UI includes a toolbar for tools, a toolbar for snap modes, a drawing canvas, a grid, a status bar and basic viewport navigation.

You can zoom with the mouse wheel, pan with the middle mouse button and reset the view with the `Home` key. The status bar shows the active tool, entity count, selected entity count, model-space mouse coordinates, current snap type and the latest tool message.

Keyboard shortcuts are available for common operations: `Ctrl+Z` for undo, `Ctrl+Y` for redo, `Delete` for deleting the current selection and `Esc` for cancelling the active tool.

---

## Architecture

OpenCad2D is split into several projects.

```text
OpenCad2D/
  src/
    OpenCad2D.Geometry/
    OpenCad2D.Core/
    OpenCad2D.Interaction/
    OpenCad2D.Tools/
    OpenCad2D.App/

  tests/
    OpenCad2D.Geometry.Tests/
    OpenCad2D.Core.Tests/
    OpenCad2D.Interaction.Tests/
    OpenCad2D.Tools.Tests/
````

`OpenCad2D.Geometry` contains the low-level geometric primitives and operations. This project does not depend on the CAD document model or on the UI.

`OpenCad2D.Core` contains the CAD document model, entities, layers, styles, commands and command history.

`OpenCad2D.Interaction` contains hit testing, selection services and snapping services.

`OpenCad2D.Tools` contains the UI-independent tool system. It includes the tool context, tool controller, tool registry, action controller and the main drawing and editing tools.

`OpenCad2D.App` is the Avalonia desktop application. It is intentionally thin: most CAD logic lives in the core libraries and is covered by tests.

---

## Requirements

OpenCad2D currently targets **.NET 8**.

You need the .NET 8 SDK installed.

```bash
dotnet --version
```

The project is currently developed and tested with:

```text
net8.0
```

---

## Build

From the repository root:

```bash
dotnet build
```

---

## Run the desktop application

```bash
dotnet run --project src/OpenCad2D.App
```

---

## Run tests

```bash
dotnet test
```

The test suite covers geometry primitives, distance calculations, intersections, CAD entities, commands, undo/redo, selection, snapping, tools, controllers and workspace behavior.

---

## Basic usage

To draw a line, select the `Line` tool, click the first point and then click the second point.

To draw a rectangle, select the `Rectangle` tool, click the first corner and then click the opposite corner.

To select entities, choose the `Select` tool and click an entity. You can use `Shift + click` to toggle selection. You can also drag from left to right for window selection or from right to left for crossing selection.

To move or copy entities, select them first, choose `Move` or `Copy`, click a base point and then click a destination point.

To delete entities, select them and press `Delete` or use the `Delete` button.

---

## Keyboard and mouse shortcuts

| Action             | Shortcut            |
| ------------------ | ------------------- |
| Undo               | `Ctrl+Z`            |
| Redo               | `Ctrl+Y`            |
| Delete selection   | `Delete`            |
| Cancel active tool | `Esc`               |
| Zoom               | Mouse wheel         |
| Pan                | Middle mouse button |
| Reset view         | `Home`              |

---

## Design goals

OpenCad2D is designed around a few principles.

The CAD core should remain independent from the UI. Geometry and document logic should be testable without launching the desktop application. Tools should not depend directly on Avalonia. Document changes should go through commands so undo and redo are available from the beginning.

The codebase should also remain understandable. The project is intentionally developed step by step, with small components and tests around the important behaviors.

---

## Roadmap

The next major areas of work are layer management, current layer support, entity colors and line weights in the canvas, save/load support and import/export features.

Future directions may include DXF import/export, SVG export, PDF export, text entities, dimension entities, trim and extend tools, offset, fillet, chamfer, better viewport fitting, command-line input, a property panel and richer multi-selection behavior.

---

## License

OpenCad2D is released under the **GNU General Public License v3.0 or later**.

This means the software can be used, studied, modified and redistributed, but distributed modified versions must preserve the same software freedom.

See the `LICENSE` file for details.

---

## Author

Created by **Emilie Rollandin**.

GitHub: [archistico](https://github.com/archistico)

