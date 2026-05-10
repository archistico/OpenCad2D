# Architecture

OpenCad2D is organized around a strict separation of concerns.

---

## Projects

```text
OpenCad2D.Geometry
OpenCad2D.Core
OpenCad2D.Interaction
OpenCad2D.Tools
OpenCad2D.Persistence
OpenCad2D.App
```

### OpenCad2D.Geometry

Pure geometry:

- points;
- vectors;
- lines;
- segments;
- circles;
- arcs;
- polylines;
- bounding boxes;
- matrices and transformations;
- intersections;
- distances;
- tolerance handling;
- coordinate systems.

No CAD document concepts and no UI dependencies belong here.

### OpenCad2D.Core

CAD model and document logic:

- entities;
- layers;
- styles;
- document;
- spatial index abstraction;
- commands;
- command history;
- document mutation validation.

### OpenCad2D.Interaction

UI-independent user interaction services:

- hit testing;
- selection;
- snapping;
- grid settings and grid snapping.

### OpenCad2D.Tools

UI-independent CAD tools:

- drawing tools;
- edit tools;
- transform tools;
- grip editing;
- tool controller;
- workspace;
- command line point submission.

Tools receive points and contextual state. They must not depend on Avalonia.

### OpenCad2D.Persistence

Document serialization and deserialization.

It depends on `Core` and `Geometry`, not on `Tools`, `Interaction` or `App`.

### OpenCad2D.App

Avalonia application:

- windows;
- file dialogs;
- XAML layout;
- rendering;
- viewport;
- keyboard/mouse forwarding;
- ViewModels;
- property panel;
- layer manager window.

---

## Dependency direction

Allowed graph:

```text
App -> Persistence -> Core -> Geometry
App -> Tools -> Interaction -> Core -> Geometry
```

The App may depend on both `Tools` and `Persistence`. `Persistence` must remain independent of the App.

---

## Stable UI zones

The main window is intentionally split into stable zones:

```text
File command bar
CAD/session top bar
Left tool panel
Canvas
Right property panel
Snap/grid/Ortho bar
Command line
Status bar
```

The file command bar is a protected region and contains New/Open/Save/Save As. It should not be mixed with drawing/editing toolbars.

---

## Document mutation boundary

`CadDocument` is the public boundary for document mutation.

Commands and tools must use:

```csharp
AddEntity(...)
RemoveEntity(...)
RemoveEntities(...)
ReplaceEntity(...)
ReplaceEntities(...)
```

Direct mutation of `EntityCollection` should be avoided outside document internals.

This ensures:

- locked-layer protection;
- layer validation;
- spatial index updates;
- consistent future document events.

---

## Commands

User-facing document changes should go through commands.

Implemented command families include:

- add;
- delete;
- replace;
- move;
- copy;
- rotate;
- scale;
- transform;
- update layers;
- composite operations.

`CommandHistory` tracks undo/redo and command generation used by dirty-state tracking.

---

## Dirty state

Dirty state is based on command history generation.

Concept:

```text
CurrentGeneration != SavedGeneration -> dirty
CurrentGeneration == SavedGeneration -> clean
```

The App uses this to show `*` in the title/file bar and to trigger “Save changes?” before destructive file operations.

---

## Persistence architecture

The document file stores drawing content:

```text
.opencad2d.json
```

It includes:

- format version;
- layers;
- entities;
- current layer;
- viewport state.

The file does not store user-local application settings such as window position or user shortcuts.

---

## Coordinate systems and command line

Entities are stored in WCS/model coordinates.

Typed command input is interpreted as UCS input and converted before reaching the active tool.

Supported command line point input:

```text
absolute point
relative point
direct distance
```

The command line is an input mechanism. It does not own CAD behavior and does not create entities directly.

---

## Layers

Layer responsibilities:

- identity;
- name;
- color;
- line weight;
- visibility;
- locked state;
- future fill color;
- future draw order.

Current layer rule:

```text
current layer must be visible and unlocked
```

Layer Manager applies batch changes through an undoable command.

---

## Selection, snapping and rendering queries

Different systems intentionally use different document queries:

```text
Rendering  -> visible entities intersecting viewport
Selection  -> selectable entities
Snapping   -> visible/snappable candidates near cursor
Editing    -> selected entities, then CadDocument mutation checks
```

Do not reuse rendering filters as mutation rules.

---

## Viewport culling

Viewport culling is a rendering optimization only.

It must not:

- remove entities;
- deselect entities;
- affect snapping globally;
- modify the document.

It should only reduce the set of entities drawn in the current frame.

---

## Managers

Manager windows, such as Layer Manager, should be separate windows/dialogs instead of filling the main CAD screen.

General manager pattern:

```text
open dialog
edit copy of data
Cancel -> discard
OK -> apply through command/service
```

This keeps the main screen operational and prevents accidental document mutation while the user experiments in a manager window.
