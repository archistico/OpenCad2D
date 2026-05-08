# Architecture

OpenCad2D is organized around a simple principle: the CAD logic must remain independent from the graphical user interface.

The Avalonia application is only the presentation layer. It draws the document, receives mouse and keyboard input, and forwards that input to the tool system. Geometry, entities, snapping, selection, commands and editing behavior live in dedicated libraries and can be tested without launching the desktop application.

This separation is one of the most important design choices in the project.

---

## Solution structure

The solution is currently divided into five main projects.

```text
src/
  OpenCad2D.Geometry/
  OpenCad2D.Core/
  OpenCad2D.Interaction/
  OpenCad2D.Tools/
  OpenCad2D.App/
```

Each project has a specific responsibility.

`OpenCad2D.Geometry` contains low-level geometric primitives and operations.

`OpenCad2D.Core` contains the CAD document model, entities, layers, styles, commands and undo/redo infrastructure.

`OpenCad2D.Interaction` contains interaction services such as hit testing, selection and object snapping.

`OpenCad2D.Tools` contains UI-independent CAD tools, controllers and the runtime workspace.

`OpenCad2D.App` is the Avalonia desktop application.

The direction of dependencies should remain clear:

```text
App
  -> Tools
    -> Interaction
    -> Core
      -> Geometry
```

The reverse direction should be avoided. For example, `Geometry` must not know anything about `Core`, `Tools` or Avalonia. `Core` must not know anything about the UI. `Tools` can coordinate CAD behavior, but should still remain independent from Avalonia.

---

## OpenCad2D.Geometry

`OpenCad2D.Geometry` is the lowest-level project.

It contains mathematical and geometric building blocks such as points, vectors, line segments, lines, circles, arcs, polylines, bounding boxes and angles.

It also contains operations such as distance calculations, intersections and transformations.

This project should stay completely independent from the CAD document model. A `Circle2D` does not know whether it belongs to a layer, whether it is selected, whether it has a color or whether it is visible in the UI.

That separation is intentional. Geometry should describe shapes and operations on shapes, not CAD behavior.

Examples of responsibilities that belong here are distance from a point to a segment, intersection between two segments, transformation matrices and bounding box calculations.

Examples of responsibilities that do not belong here are entity selection, layer visibility, undo/redo, mouse input and rendering.

---

## OpenCad2D.Core

`OpenCad2D.Core` contains the main CAD model.

This is where geometric primitives become CAD entities. For example, a line segment can become a `LineEntity`, a circle can become a `CircleEntity`, and a polyline can become a `PolylineEntity`.

Entities have CAD-specific properties such as an identifier, layer, style, visibility, lock state and draw order.

The document model is also defined here. `CadDocument` owns the layer collection and the entity collection. The document is the central object that represents the current drawing.

The command system also belongs to `Core`. Commands modify the document and support undo. This is fundamental for CAD behavior.

The main rule is that document modifications should go through commands whenever they represent an operation that the user may want to undo.

For example, adding an entity, deleting entities, moving entities or copying entities should be command-based. Direct modifications to the document should be limited to initialization, tests or low-level setup.

---

## OpenCad2D.Interaction

`OpenCad2D.Interaction` contains logic related to user interaction, but still without depending on the UI framework.

It includes services for hit testing, selection and snapping.

Hit testing answers questions such as: which entity is under this model-space point?

Selection services answer questions such as: which entity should be selected by a point? Which entities are inside or crossing this selection window?

Snapping services answer questions such as: what is the best snap candidate near the cursor?

This project works in model coordinates. It does not know about screen pixels, Avalonia controls or mouse events. The UI is responsible for converting screen coordinates into model coordinates before calling interaction services.

This is important because the same interaction logic can be tested independently and can later be reused by another UI if needed.

---

## OpenCad2D.Tools

`OpenCad2D.Tools` contains the CAD tool system.

A tool represents an operation the user can perform, such as drawing a line, drawing a rectangle, selecting entities, moving entities, copying entities or deleting entities.

The tools are not Avalonia controls. They do not draw buttons, they do not handle Avalonia events directly and they do not know about the visual canvas.

Instead, they receive model-space pointer information through a common interface.

The main types are:

```text
ICadTool
ToolContext
ToolController
ToolRegistry
CadActionController
CadWorkspace
```

`ICadTool` defines the basic lifecycle of a tool.

`ToolContext` provides access to the document, command history, selection set, snap service, selection service, grid settings and current layer.

`ToolController` owns the active tool and forwards pointer events to it.

`ToolRegistry` creates tools from a `ToolId`, so the UI does not need to instantiate concrete tool classes directly.

`CadActionController` centralizes global actions such as undo, redo, delete selection and cancel active tool.

`CadWorkspace` aggregates the main runtime objects used by the application.

The current approach keeps tool behavior testable. For example, `LineTool` can be tested by sending it two pointer presses and checking that a `LineEntity` was added to the document.

---

## OpenCad2D.App

`OpenCad2D.App` is the Avalonia desktop application.

Its main responsibility is presentation.

It displays the drawing, shows the toolbar and status bar, handles mouse and keyboard events, manages viewport navigation and forwards user input to the workspace.

The custom `CadCanvas` is responsible for rendering entities, previews, grid lines, snap markers and the selection window. It also converts screen coordinates into model coordinates before creating `PointerInfo` objects.

The UI should remain as thin as possible. It should not implement geometric algorithms, command logic, snapping rules or document-editing behavior.

If code starts becoming complex inside Avalonia event handlers, that is usually a sign that the logic should move into `Tools`, `Interaction`, `Core` or `Geometry`.

---

## Runtime flow

A typical drawing operation follows this flow.

The user clicks on the canvas. Avalonia receives a pointer event. `CadCanvas` converts the screen point into a model-space `Point2D`. It creates a `PointerInfo` and sends it to `ToolController`.

`ToolController` forwards the event to the active tool.

If the active tool is `LineTool`, the first click stores the first point. The second click creates a `LineEntity`, wraps it in an `AddEntityCommand` and executes it through `CommandHistory`.

`CommandHistory` executes the command and stores it on the undo stack. The command modifies the `CadDocument`.

The canvas is invalidated and renders the updated document.

The important part is that Avalonia does not directly create the line. It only forwards input.

---

## Coordinate spaces

The UI works with two coordinate spaces.

Screen coordinates are Avalonia coordinates, measured in pixels relative to the canvas.

Model coordinates are CAD coordinates, used by geometry, tools, snapping and the document.

`ViewportTransform` converts between these spaces.

```text
ModelToScreen
ScreenToModel
```

All core CAD logic should work in model coordinates.

Zoom and pan should affect the viewport transform, not the document geometry.

This means that zooming in or panning the canvas does not modify entities. It only changes how they are displayed.

---

## Commands and undo/redo

Commands are the foundation of undo/redo.

Each command must know how to execute an operation and how to undo it.

For example, `MoveEntitiesCommand` stores the original entities, creates transformed versions and replaces them in the document. Undo restores the original entities.

This approach keeps document editing predictable.

A user-facing operation that changes the document should generally be represented by a command. This includes drawing, deleting, moving, copying, rotating, scaling and mirroring.

---

## Selection

Selection state is stored in `SelectionSet`.

The selection set contains entity identifiers, not entity objects. This keeps selection lightweight and avoids stale references when entities are replaced by commands.

`SelectionTool` uses `SelectionService` to select by point or by window.

The current selection behavior supports single click, shift-click toggle, window selection and crossing selection.

When switching from `SelectionTool` to another tool, the selection should remain available. This is why the project separates `Cancel` from `Deactivate`.

Cancel is an explicit user action, usually triggered by `Esc`.

Deactivate means that the current tool is being replaced by another tool. Deactivation should not necessarily clear useful state such as the current selection.

---

## Snapping

Snapping is implemented in `OpenCad2D.Interaction`.

`SnapService` coordinates several snap providers. Each provider returns candidates for a specific snap kind.

Current snap kinds include endpoint, midpoint, center, quadrant, intersection, perpendicular, tangent and grid.

Some snaps are direct. Endpoint, midpoint, center, quadrant, intersection and grid can work from the cursor position alone.

Other snaps are contextual. Perpendicular and tangent require a base point. This is why `SnapRequest` contains an optional `BasePoint`.

For example, after the first click of `LineTool`, the first point becomes the base point for perpendicular or tangent snapping.

---

## Layers

The core already supports layers through `Layer`, `LayerId` and `LayerCollection`.

The current tool context also has a `CurrentLayerId`. New entities created by drawing tools should use the current layer.

The next UI step is to expose the current layer in the toolbar, then use layer color, visibility and lock state in the canvas and interaction services.

Layer behavior should eventually follow these rules:

```text
visible layer
  entities are drawn

hidden layer
  entities are not drawn and should not be selected

locked layer
  entities are drawn but should not be modified
```

---

## Testing strategy

The project is designed to be testable without launching the UI.

Geometry tests verify mathematical behavior.

Core tests verify entities, document behavior, commands and undo/redo.

Interaction tests verify hit testing, selection and snapping.

Tools tests verify tool behavior, command execution, selection workflows, controller behavior and workspace integration.

The Avalonia UI is intentionally thin, so most important behavior can be tested in the non-UI projects.

This should remain a guiding principle as the project grows.

---

## Architectural rules

The project should follow these rules as it evolves.

UI code should not contain CAD business logic.

Geometry should not depend on Core, Interaction, Tools or App.

Core should not depend on Interaction, Tools or App.

Interaction should not depend on App.

Tools should remain UI-independent.

Document modifications should go through commands when they represent undoable user operations.

Snapping and selection should work in model coordinates.

Viewport operations should not modify document geometry.

These rules are not meant to make the architecture rigid. They exist to keep the project understandable and maintainable as it grows.

