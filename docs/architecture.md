# Architecture

OpenCad2D is organized around a simple principle: CAD logic must remain independent from the graphical user interface.

The Avalonia application is the presentation layer. It draws the document, receives mouse and keyboard input, converts screen coordinates into CAD coordinates and forwards input to the tool system. Geometry, entities, snapping, selection, commands, layers and editing behavior live in dedicated libraries and can be tested without launching the desktop application.

This separation is the most important design rule in the project.

---

## Solution structure

The solution is divided into five main projects.

```text
src/
  OpenCad2D.Geometry/
  OpenCad2D.Core/
  OpenCad2D.Interaction/
  OpenCad2D.Tools/
  OpenCad2D.App/
```

Each project has a specific responsibility.

`OpenCad2D.Geometry` contains low-level geometric primitives, coordinate systems, geometric operations, transformations and numeric tolerance rules.

`OpenCad2D.Core` contains the CAD document model, entities, layers, styles, spatial indexing, commands and undo/redo infrastructure.

`OpenCad2D.Interaction` contains UI-independent interaction services such as hit testing, selection and object snapping.

`OpenCad2D.Tools` contains UI-independent CAD tools, controllers, tool contexts and the runtime workspace.

`OpenCad2D.App` is the Avalonia desktop application.

The direction of dependencies should remain clear.

```text
App
  -> Tools
    -> Interaction
    -> Core
      -> Geometry
```

The reverse direction should be avoided. `Geometry` must not know anything about CAD entities, tools or Avalonia. `Core` must not depend on the UI. `Interaction` works with model data and must not know about Avalonia. `Tools` can coordinate CAD behavior, but should still remain UI-independent.

---

## OpenCad2D.Geometry

`OpenCad2D.Geometry` is the lowest-level project.

It contains mathematical and geometric building blocks such as points, vectors, line segments, lines, circles, arcs, polylines, bounding boxes, angles and transformation matrices.

It also contains geometric operations such as distance calculations and intersections.

Geometry is intentionally independent from the CAD document model. A `Circle2D` does not know whether it belongs to a layer, whether it is selected, whether it is visible or whether it is drawn by Avalonia.

Examples of responsibilities that belong here are:

```text
distance from a point to a segment
segment intersections
transformation matrices
bounding box calculations
coordinate system conversion
numeric tolerance checks
```

Examples of responsibilities that do not belong here are:

```text
entity selection
layer visibility
undo/redo
mouse input
rendering
file persistence
```

---

## Coordinate systems

OpenCad2D separates screen coordinates, world/model coordinates and user coordinates.

```text
Screen coordinates  = Avalonia coordinates, measured in pixels in the canvas
WCS / model         = absolute drawing coordinates stored in the document
UCS                 = user coordinate system used for input and display
```

The document stores entities in WCS/model coordinates.

The canvas converts screen coordinates to model coordinates using the viewport transform. The current UCS converts between WCS and user coordinates.

```text
Screen -> WCS -> UCS
```

Tools receive both model and user coordinates through `PointerInfo`. Existing tools may continue to use model coordinates, while future tools can support coordinate input relative to the active UCS.

`CoordinateSystem2D` defines the current user coordinate system through an origin and axes. This prepares the application for future features such as user-defined origins, rotated coordinate systems, relative input and view-dependent workflows.

Zoom and pan belong to the viewport. They must not modify document geometry.

---

## Numeric precision

OpenCad2D uses an explicit numeric tolerance strategy.

Floating-point geometry cannot rely on direct equality checks. In a CAD application, whether two values are considered equal depends on context: point distance, angle comparison, vector length and normalized parameters do not necessarily use the same tolerance.

`GeometryTolerance` defines separate values for:

```text
Distance      point and distance comparison
Angle         angular comparison in radians
Parameter     normalized values such as segment parameter t in [0, 1]
VectorLength  zero-length vector detection
```

New geometric algorithms should prefer `GeometryTolerance` instead of raw magic numbers or direct `double` equality.

`Tolerance` remains as a compatibility helper for simple legacy checks, but new code should be explicit about the kind of tolerance it uses.

---

## OpenCad2D.Core

`OpenCad2D.Core` contains the main CAD model.

This is where geometric primitives become CAD entities. For example, a segment can become a `LineEntity`, a circle can become a `CircleEntity`, and a polyline can become a `PolylineEntity`.

Entities have CAD-specific properties such as:

```text
identifier
layer
style/color behavior
visibility
draw order
```

The document model is also defined here. `CadDocument` owns layers and entities and is the central object representing the current drawing.

---

## CadDocument mutation boundary

`CadDocument` is the public mutation boundary for document entities.

Commands and tools should not mutate `EntityCollection` directly. They should use document methods such as:

```text
AddEntity
AddEntities
ReplaceEntity
ReplaceEntities
RemoveEntity
RemoveEntities
```

This is important because the document is where cross-cutting validation belongs.

For example, when locked layers are implemented, `CadDocument` can reject replacement or removal of entities that belong to locked layers. If commands bypass the document and mutate `document.Entities` directly, those rules would be skipped.

`EntityCollection` can still be used for queries such as reading entities by id, enumerating entities and spatial lookup. Mutations should go through `CadDocument`.

---

## Spatial indexing

The entity collection owns a spatial index abstraction.

The current implementation uses `ISpatialIndex` with a `LinearSpatialIndex`. The linear implementation still scans stored bounding boxes, but the important design decision is that hit testing, selection and snapping can query by area instead of always scanning the entire document.

The current structure is:

```text
CadDocument
  -> EntityCollection
    -> ISpatialIndex
      -> LinearSpatialIndex
```

This prepares the project for future implementations such as:

```text
QuadtreeSpatialIndex
RTreeSpatialIndex
UniformGridSpatialIndex
```

The spatial index should answer: which entities have bounds intersecting this search area?

It should not decide visibility, selection or editability. Those rules belong to `CadDocument`, layers and interaction services.

---

## Layers

The core supports layers through `Layer`, `LayerId` and `LayerCollection`.

New entities created by drawing tools use the current layer from the tool creation context.

Layer visibility is already part of document-level visibility rules:

```text
visible layer
  entities can be drawn, selected and used by snapping

hidden layer
  entities are not drawn
  entities are not selected
  entities are not used by snapping
```

The UI exposes the current layer and a visibility toggle. Hidden layer behavior is enforced consistently by rendering, hit testing, selection and snapping through `CadDocument.GetVisibleEntities(...)`.

Locked layers are the next layer-related step. The intended rule is:

```text
locked layer
  entities are drawn
  entities can be used as references for snapping
  entities cannot be modified, removed or transformed
```

Locked-layer enforcement should be implemented at the `CadDocument` mutation boundary.

---

## Commands and undo/redo

Commands are the foundation of undo and redo.

Each command must know how to execute an operation and how to undo it.

A user-facing operation that changes the document should generally be represented by a command. This includes drawing, deleting, moving, copying, rotating, scaling, mirroring and future editing operations.

Complex operations can be represented by `CompositeCommand`. This allows several child commands to be executed as a single undoable user action.

For example, a future fillet operation may:

```text
replace the first line
replace the second line
add a fillet arc
```

The user should undo that as one operation, not three separate steps.

---

## OpenCad2D.Interaction

`OpenCad2D.Interaction` contains interaction logic without depending on Avalonia.

It includes:

```text
HitTestService
SelectionService
SnapService
snap providers
selection models
```

Hit testing answers: which entity is under this model-space point?

Selection answers: which entities are selected by point or window?

Snapping answers: what is the best snap candidate near the cursor?

Interaction services work in model coordinates. They do not know about screen pixels, Avalonia controls or mouse events.

Where possible, interaction services query a spatial search area instead of scanning every entity. Visibility rules are still applied through the document.

---

## OpenCad2D.Tools

`OpenCad2D.Tools` contains the CAD tool system.

A tool represents an operation the user can perform, such as selecting, drawing a line, drawing a rectangle, moving entities or copying entities.

Tools are not Avalonia controls. They do not draw buttons, do not handle Avalonia events directly and do not know about the visual canvas.

They receive `PointerInfo` and a `ToolContext`.

The main types are:

```text
ICadTool
ToolContext
ToolCommandContext
ToolSelectionContext
ToolSnapContext
ToolCoordinateContext
ToolCreationContext
ToolController
ToolRegistry
CadActionController
CadWorkspace
```

`ToolContext` is intentionally split into focused sub-contexts to avoid becoming a God Object.

New code should prefer:

```text
context.Commands
context.Selection
context.Snapping
context.Coordinates
context.Creation
```

rather than adding unrelated properties directly to `ToolContext`.

---

## ToolContext boundary

`ToolContext` may contain model-side services required by tools:

```text
active document
undoable command execution
selection state and selection services
snapping services and snapping settings
current entity creation defaults
current UCS and geometry tolerance
```

It must not contain:

```text
Avalonia controls
viewport or screen-to-model conversion logic
dialogs or message boxes
status bar services
file system or persistence services
rendering services
application-level configuration unrelated to tool execution
```

Pointer coordinates must be converted before entering tools.

---

## OpenCad2D.App

`OpenCad2D.App` is the Avalonia desktop application.

Its responsibilities are:

```text
render the drawing
show toolbar and status bar
handle mouse and keyboard input
manage viewport navigation
show active command feedback
show crosshair and snap markers
forward user input to the workspace
```

The custom `CadCanvas` renders entities, previews, the grid, the selection window, the UCS marker, the CAD crosshair and snap markers.

The standard mouse cursor is hidden inside the canvas. A CAD-style crosshair is drawn instead. The crosshair spans the drawing area and includes a small center box to indicate the exact click position.

Snap markers are visually different for different snap kinds. For example, endpoint, midpoint, center, perpendicular and tangent snaps can be drawn with distinct symbols.

Rendering should avoid unnecessary allocations. Entity pens are cached by color and thickness instead of recreated for every entity on every frame.

---

## ViewModel notifications

`MainWindowViewModel` implements property change notification so that UI bindings can update automatically.

The UI should not need to manually rewrite every text block after every action. Properties such as status text, active tool name, entity count, selected count, current layer and snap text should notify the UI when they change.

Manual refresh calls can still exist while the UI is being migrated gradually, but new UI state should prefer binding to the view model.

---

## Runtime flow

A typical drawing operation follows this flow.

The user clicks on the canvas. Avalonia receives a pointer event. `CadCanvas` converts the screen point into model coordinates and user coordinates. It creates a `PointerInfo` and sends it to `ToolController`.

`ToolController` forwards the event to the active tool.

If the active tool is `LineTool`, the first click stores the first point. The second click creates a `LineEntity`, wraps it in an `AddEntityCommand` and executes it through the command context.

The command modifies the document through the `CadDocument` API.

The canvas is invalidated and renders the updated document.

Avalonia does not directly create or edit entities. It forwards input and renders the resulting state.

---

## Selection and cancel behavior

Selection state is stored in `SelectionSet` and contains entity identifiers, not entity references.

The selection remains available when switching tools. This enables workflows such as:

```text
select entity
switch to Move
move selected entity
```

`Esc` has layered behavior:

```text
first Esc   cancels the current tool operation if one is in progress
second Esc  clears the current selection if no tool operation is active
```

This keeps command cancellation and selection clearing separate.

---

## Testing strategy

The project is designed to be testable without launching the UI.

```text
Geometry tests      primitives, transformations, intersections, tolerances, UCS
Core tests          entities, document behavior, layers, commands, spatial index
Interaction tests   hit testing, selection, snapping, hidden layer behavior
Tools tests         tool behavior, command execution, workspace integration
App tests           should remain minimal because UI logic should be thin
```

The Avalonia UI is intentionally thin, so most important behavior can be tested in non-UI projects.

---

## Architectural rules

The project should follow these rules as it evolves.

```text
UI code should not contain CAD business logic.
Geometry should not depend on Core, Interaction, Tools or App.
Core should not depend on Interaction, Tools or App.
Interaction should not depend on App.
Tools should remain UI-independent.
Document mutations should go through CadDocument.
Undoable user operations should go through commands.
Composite operations should use CompositeCommand.
Snapping and selection should work in model coordinates.
PointerInfo should carry WCS and UCS coordinates.
Viewport operations should not modify document geometry.
ToolContext should remain grouped into focused sub-contexts.
Spatial lookup should go through the spatial index abstraction.
New geometric algorithms should use GeometryTolerance.
```

These rules are not meant to make the architecture rigid. They exist to keep the project understandable and maintainable as it grows.
