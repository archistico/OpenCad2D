# OpenCad2D

**OpenCad2D** is an experimental open-source 2D CAD project built with **C#**, **.NET 8** and **Avalonia UI**.

The goal is to create a clean, testable and cross-platform CAD core, with a modern UI and a codebase that can be studied, extended and improved over time.

![OpenCad2D screenshot](screenshot.png)

---

## Project status

OpenCad2D is currently an early prototype.

The project is not intended to replace mature CAD software yet.  
At this stage, the focus is on building a solid internal architecture:

- geometry primitives;
- CAD entities;
- document model;
- command history;
- undo / redo;
- selection;
- object snapping;
- tool system;
- basic Avalonia UI.

---

## Current features

### Geometry core

The project currently includes basic 2D geometry primitives and operations:

- points;
- vectors;
- line segments;
- infinite lines;
- circles;
- arcs;
- polylines;
- bounding boxes;
- transformations;
- distances;
- intersections.

### CAD entities

Supported CAD entities:

- line;
- circle;
- arc;
- polyline;
- rectangle as closed polyline.

### Commands

The command system supports undo and redo.

Implemented commands include:

- add entity;
- delete entities;
- replace entities;
- move entities;
- copy entities;
- rotate entities;
- scale entities;
- mirror entities.

### Selection

Selection currently supports:

- point selection;
- shift-click toggle selection;
- window selection;
- crossing selection;
- selection preview;
- selected entity highlighting.

### Object snapping

Available snap modes:

- endpoint;
- midpoint;
- center;
- quadrant;
- intersection;
- perpendicular;
- tangent;
- grid.

The UI also shows a snap marker and the current snap type in the status bar.

### Tools

Implemented logical tools:

- selection tool;
- line tool;
- rectangle tool;
- move tool;
- copy tool;
- delete tool.

Tools are coordinated through a `ToolController` and created through a `ToolRegistry`.

### UI

The current Avalonia UI includes:

- drawing canvas;
- toolbar;
- snap toolbar;
- status bar;
- zoom with mouse wheel;
- pan with middle mouse button;
- view reset with `Home`;
- undo with `Ctrl+Z`;
- redo with `Ctrl+Y`;
- delete selection with `Delete`;
- cancel active tool with `Esc`.

---

## Solution structure

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

### `OpenCad2D.Geometry`

Contains low-level geometry primitives and operations.

This project does not depend on the CAD document model or on the UI.

### `OpenCad2D.Core`

Contains the CAD document model:

* entities;
* layers;
* styles;
* commands;
* command history;
* undo / redo.

### `OpenCad2D.Interaction`

Contains interaction-related logic:

* hit testing;
* selection services;
* snapping services.

### `OpenCad2D.Tools`

Contains UI-independent CAD tools and controllers:

* tool context;
* tool controller;
* tool registry;
* action controller;
* workspace;
* drawing tools;
* editing tools.

### `OpenCad2D.App`

Avalonia-based desktop application.

The UI is intentionally thin: most CAD logic lives in the core libraries and is covered by tests.

---

## Requirements

* .NET 8 SDK
* Avalonia UI

To check your installed SDK:

```bash
dotnet --version
```

The project currently targets:

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

## Run the Avalonia app

```bash
dotnet run --project src/OpenCad2D.App
```

---

## Run tests

```bash
dotnet test
```

The project includes tests for:

* geometry primitives;
* intersections;
* distances;
* CAD entities;
* commands;
* undo / redo;
* snapping;
* selection;
* tools;
* workspace;
* controllers.

---

## Basic usage

### Draw a line

1. Select `Line`.
2. Click the first point.
3. Click the second point.

### Draw a rectangle

1. Select `Rectangle`.
2. Click the first corner.
3. Click the opposite corner.

### Select entities

1. Select `Select`.
2. Click an entity to select it.
3. Use `Shift + click` to toggle selection.
4. Drag left-to-right for window selection.
5. Drag right-to-left for crossing selection.

### Move entities

1. Select one or more entities.
2. Select `Move`.
3. Click the base point.
4. Click the destination point.

### Copy entities

1. Select one or more entities.
2. Select `Copy`.
3. Click the base point.
4. Click the destination point.

### Delete entities

1. Select one or more entities.
2. Press `Delete` or click `Delete`.

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

OpenCad2D is designed around a few core principles:

* keep the CAD core independent from the UI;
* keep geometry operations testable;
* keep tools independent from Avalonia;
* use commands for document modifications;
* support undo and redo from the beginning;
* make interaction logic reusable;
* keep the architecture understandable.

---

## Roadmap

Possible next steps:

* layer management UI;
* current layer support;
* entity colors and line weights in the canvas;
* save / load document format;
* DXF import / export;
* SVG export;
* PDF export;
* text entity;
* dimension entities;
* trim / extend tools;
* offset tool;
* fillet / chamfer tools;
* better viewport fitting;
* command line input;
* property panel;
* multi-selection improvements.

---

## License

License not specified yet.

Before using this project in production or redistributing it, please check the license once it is added.

---

## Author

Created by **Emilie Rollandin**.

GitHub: [archistico](https://github.com/archistico)

