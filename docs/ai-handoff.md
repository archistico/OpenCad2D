# OpenCad2D - AI Handoff Document

This document describes the current state, architecture and development rules of OpenCad2D. It is intended for future AI-assisted development sessions and contributors.

Update this file after every meaningful development phase.

---

## Project purpose

OpenCad2D is an experimental 2D CAD application built with C#, .NET 8 and Avalonia UI.

The goal is to build a small but serious CAD system with:

- clean architecture;
- strong testability;
- UI-independent CAD behavior;
- incremental development;
- clear separation between geometry, document model, interaction logic, tools, persistence and UI.

---

## Current implemented status

The project currently supports:

### Drawing

- `LineTool`;
- `RectangleTool`;
- `CircleTool`;
- `PolylineTool` v1;
- command line coordinate input;
- relative coordinate input;
- direct distance entry;
- snap support;
- Ortho support;
- preview geometry.

### Editing and transforms

- select by point/window/crossing;
- move;
- copy;
- delete;
- rotate;
- scale;
- align with optional scaling confirmation;
- grip editing for supported entities;
- undo and redo.

### Layers

- current layer selection;
- hidden layer behavior;
- locked layer behavior;
- Layer Manager v1;
- current layer must remain visible and unlocked;
- layer `0` protected;
- color and line weight at layer level.

### UI

- stable top file command bar;
- CAD top bar;
- vertical left tool panel;
- canvas with crosshair;
- optional right Property Panel v1;
- bottom snap/grid/Ortho bar;
- fixed command line input;
- status bar;
- grid configuration;
- viewport culling;
- rendered entity count.

### Persistence

- internal JSON format `.opencad2d.json`;
- `OpenCad2D.Persistence` project;
- New/Open/Save/Save As;
- current file path;
- dirty state with `*` marker;
- “Save changes?” dialog before New/Open/Close;
- viewport state save/restore.

---

## Stable UI layout rule

The file commands must stay in their own highest row:

```text
New / Open / Save / Save As / Current file name / Dirty marker
```

Do not merge file commands into the CAD toolbars. Earlier iterations accidentally lost persistence controls when toolbars changed. The file command bar is now a protected UI region.

---

## Dependency rules

Allowed high-level dependencies:

```text
OpenCad2D.App
  -> OpenCad2D.Persistence
      -> OpenCad2D.Core
          -> OpenCad2D.Geometry

OpenCad2D.App
  -> OpenCad2D.Tools
      -> OpenCad2D.Interaction
          -> OpenCad2D.Core
              -> OpenCad2D.Geometry
```

Forbidden dependencies:

- `Geometry` must not depend on anything else in the solution.
- `Core` must not depend on `Tools`, `Interaction`, `Persistence` or `App`.
- `Interaction` must not depend on `Tools` or `App`.
- `Tools` must not depend on `App` or Avalonia.
- `Persistence` must not depend on `Tools`, `Interaction` or `App`.

---

## Document mutation rule

All document changes must go through commands and through `CadDocument` mutation APIs.

Correct:

```csharp
document.AddEntity(entity);
document.ReplaceEntities(replacements);
document.RemoveEntities(ids);
```

Incorrect:

```csharp
document.Entities.Replace(entity);
document.Entities.RemoveMany(ids);
```

This matters because `CadDocument` enforces layer validation, locked-layer validation and spatial index consistency.

---

## Hidden and locked layers

Hidden layer entities:

```text
not rendered
not selectable
not snappable
```

Locked layer entities:

```text
rendered if visible
not selectable
snappable
not editable/removable/transformable
```

Locked-layer protection is enforced at `CadDocument.ReplaceEntity`, `ReplaceEntities`, `RemoveEntity` and `RemoveEntities`.

---

## Current layer rule

The current layer must always be:

```text
visible
unlocked
```

Layer Manager and quick layer controls must preserve this rule.

---

## Command line and point input

Typed input is resolved to a point and then forwarded to the active tool. The command line must not create entities directly.

Supported point input:

```text
100,50   absolute UCS point
@50,0    relative UCS offset from current base point
5        direct distance from current base point along cursor direction
```

Explicit coordinates are not modified by Ortho. Direct distance uses Ortho-constrained direction when Ortho is enabled.

---

## ToolContext runtime state

`ToolContext` stores shared runtime state needed by tools:

- current layer;
- snap settings;
- grid settings;
- current UCS;
- current base point;
- Ortho mode;
- selection set;
- command history.

The UI should not inspect private fields of tools. Shared information should be exposed through `ToolContext`, `CadWorkspace` or tool public properties.

---

## Transform tools status

Implemented:

- `RotateTool` — base/reference/destination, preview, Ortho to 90-degree steps.
- `ScaleTool` — base/reference/destination, preview.
- `AlignTool` — source1/destination1/source2/destination2, preview, optional scaling confirmation.

Align confirmation:

```text
Enter or N -> apply without scale
Y          -> apply with uniform scale
```

Keyboard input is case-insensitive for confirmation keys at the tool level.

---

## PolylineTool status

`PolylineTool` v1 is implemented.

Behavior:

```text
click or typed point -> add vertex
Enter                -> finish open polyline
C                    -> close polyline
Esc                  -> cancel
```

The tool supports command line input, snap, Ortho and direct distance entry.

Polyline grip editing is not yet implemented and is a good follow-up.

---

## Property Panel status

Property Panel v1 is implemented and read-only.

It displays:

- no-selection document state;
- single line properties;
- single circle properties;
- single polyline properties;
- multiple-selection summary.

Do not add editing fields to the property panel until modifications can be routed through undoable commands.

---

## Layer Manager status

Layer Manager v1 is implemented as a separate window.

It supports:

- New layer;
- Delete layer when allowed;
- Rename;
- Visible;
- Locked;
- Color hex;
- LineWeight;
- Current layer selection;
- OK/Cancel workflow;
- one undoable update command.

Layer `0` is protected. Current layer cannot be hidden, locked or deleted.

---

## Grid and viewport culling status

Grid display is configurable separately from grid snapping.

Viewport culling is implemented at rendering time. Only normal entities whose bounding boxes intersect the visible world area are rendered.

Do not use viewport culling to modify selection state or document state. It is only a rendering optimization.

---

## Persistence status

Persistence is implemented in `OpenCad2D.Persistence`.

The serializer handles:

- versioned JSON;
- layers;
- entities;
- current layer id;
- viewport state;
- unknown entity type tolerance.

The App handles:

- file dialogs;
- New/Open/Save/Save As;
- dirty-state title/file marker;
- Save changes dialog.

---

## Recommended next development area

The next planned tools are modify tools:

```text
Break
Trim
Extend
```

Design rule: these tools should use geometry services, produce preview when useful and commit changes through undoable commands that mutate the document through `CadDocument`.

Recommended order:

1. Break — simpler because it splits one entity.
2. Extend — requires target boundary selection and projection/intersection logic.
3. Trim — requires cutting boundary and choosing which side to remove.

---

## Development practice

Before adding or changing code:

1. start from the latest project zip/baseline;
2. keep each phase small;
3. add tests with every new service/tool;
4. run `dotnet build` and `dotnet test`;
5. update docs after each milestone;
6. avoid overwriting stable UI regions such as file commands.
