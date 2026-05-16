# Persistence

OpenCad2D uses an internal JSON persistence format for save and reopen.

---

## Format

File extension:

```text
.opencad2d.json
```

The format is intended for OpenCad2D project files, not for interoperability with other CAD applications.

---

## Project

Persistence lives in:

```text
src/OpenCad2D.Persistence
```

Dependency rule:

```text
OpenCad2D.Persistence -> OpenCad2D.Core -> OpenCad2D.Geometry
```

Persistence must not depend on:

```text
OpenCad2D.App
OpenCad2D.Tools
OpenCad2D.Interaction
```

---

## Responsibilities

The serializer handles:

- document version;
- layers;
- line formats;
- layer line format references;
- text formats;
- text entity text format references;
- dimension styles;
- dimension entity style references;
- entities;
- current layer id;
- viewport state;
- unknown entity type handling;
- JSON file I/O;
- load/save exceptions.

The App handles:

- file dialogs;
- New/Open/Save/Save As;
- dirty state;
- Save changes confirmation;
- applying viewport state after load.

---

## Line format persistence

The native JSON format stores reusable line formats at document level and stores only the selected format id on each layer.

Conceptual DTO shape:

```text
DocumentDto
  LineFormats[]
  Layers[]

LineFormatDto
  Id
  Name
  Color
  LineWeight
  LineStyle

LayerDto
  Id
  Name
  LineFormatId
  IsVisible
  IsLocked
```

Loading rules:

- if `LineFormats` is missing or empty, use the default line format collection;
- if a layer references an unknown `LineFormatId`, fall back to `Continuous`;
- legacy layer color and line weight fields are compatibility data only and are not the active source of appearance.

Saving rules:

- save `Document.LineFormats`;
- save each layer's `LineFormatId`;
- do not write active layer color/weight fields as the current appearance model.

---

## Text format persistence

The native JSON format stores reusable text formats at document level and stores only the selected format id on each `TextEntity`.

Conceptual DTO shape:

```text
DocumentDto
  TextFormats[]
  Entities[]

TextFormatDto
  Id
  Name
  FontFamily
  Height
  Color
  IsBold
  IsItalic

TextEntityDto
  Type = Text
  Text
  InsertionX
  InsertionY
  RotationDegrees
  TextFormatId
```

Loading rules:

- if `TextFormats` is missing or empty, use the default text format collection;
- if older files have no text format information, new text entities use `Standard`;
- text format appearance is not duplicated inside each text entity.

Saving rules:

- save `Document.TextFormats`;
- save each `TextEntity.TextFormatId`;
- save text content, insertion point and rotation on each text entity.

---

## Dimension style persistence

The native JSON format stores reusable dimension styles at document level and stores only the selected style id on each dimension entity.

Conceptual DTO shape:

```text
DocumentDto
  DimensionStyles[]
  Entities[]

DimensionStyleDto
  Id
  Name
  TextFormatId
  ArrowSize
  TextOffset
  ExtensionLineOffset
  ExtensionLineOvershoot
  DecimalPlaces
  DecimalSeparator
  Suffix
```

Loading rules:

- if `DimensionStyles` is missing or empty, use the default dimension style collection;
- if a dimension references an unknown `DimensionStyleId`, fall back to `Standard`;
- dimension text appearance is resolved through `DimensionStyle.TextFormatId` and `Document.TextFormats`;
- style values are not duplicated inside each dimension entity.

Saving rules:

- save `Document.DimensionStyles`;
- save each dimension entity's `DimensionStyleId`;
- save only the geometric definition and optional text override on each dimension entity.

---

## Dimension entity persistence

Implemented dimension entity DTOs:

```text
LinearDimensionEntityDto
AlignedDimensionEntityDto
RadiusDimensionEntityDto
DiameterDimensionEntityDto
AngularDimensionEntityDto
```

`LinearDimensionEntityDto` stores:

```text
Type = LinearDimension
FirstX / FirstY
SecondX / SecondY
DimensionLineX / DimensionLineY
Orientation = Horizontal | Vertical
DimensionStyleId
TextOverride
```

`AlignedDimensionEntityDto` stores:

```text
Type = AlignedDimension
FirstX / FirstY
SecondX / SecondY
DimensionLineX / DimensionLineY
DimensionStyleId
TextOverride
```

`RadiusDimensionEntityDto` and `DiameterDimensionEntityDto` store:

```text
CenterX / CenterY
PointOnCircleX / PointOnCircleY
TextPointX / TextPointY
DimensionStyleId
TextOverride
```

`AngularDimensionEntityDto` stores:

```text
CenterX / CenterY
FirstRayPointX / FirstRayPointY
SecondRayPointX / SecondRayPointY
ArcPointX / ArcPointY
IsCounterClockwise
DimensionStyleId
TextOverride
```

Dimensions are non-associative in v0.4. The persisted data does not store references to measured entities.

---

## Dirty state

Dirty state is tracked from command history generation.

After save/load/new:

```text
MarkSaved()
```

After a document command executes, undo or redo changes generation and the workspace can report dirty state.

---

## UI rule

File commands live in a stable top file command bar:

```text
New | Open | Save | Save As | current file name | dirty marker
```

Do not place file commands inside tool-specific UI areas.

---

## Save changes dialog

Before New, Open or window close:

```text
if IsDirty == false:
    continue
else:
    ask Save / Don't Save / Cancel
```

Behavior:

```text
Save       -> save first, then continue
Don't Save -> continue without saving
Cancel     -> abort operation
```

---

## Viewport state

Viewport pan and zoom are saved with the drawing and restored after loading.

Viewport state is consumed by the App layer. It is not CAD entity geometry.

---

## Persistence vs export

Persistence and export are intentionally separate.

```text
Save / Save As  -> writes .opencad2d.json and marks the document clean
Export SVG      -> writes .svg and leaves the document state unchanged
Export DXF      -> writes .dxf and leaves the document state unchanged
```

SVG/DXF export does not:

- change `CurrentFilePath`;
- call `MarkSaved()`;
- clear the dirty marker;
- participate in native document loading.

SVG export belongs to `OpenCad2D.Export`, while native document save/load belongs to `OpenCad2D.Persistence`.

---

## Startup default template

Normal application startup uses a native OpenCad2D template file instead of seeding a demo drawing in code.

Template path in the app project:

```text
src/OpenCad2D.App/Templates/default.opencad2d.json
```

Build behavior:

```text
Templates/** -> copied to the application output directory
```

Runtime behavior:

1. `MainWindowViewModel` tries to load `Templates/default.opencad2d.json` from `AppContext.BaseDirectory`.
2. The loaded document becomes the initial untitled drawing.
3. The current file path remains empty, so the title still shows `Untitled`.
4. The document is marked as saved/clean after loading the template.
5. If the template is missing, unreadable or invalid, the app creates an empty internal fallback document with the built-in CAD layers.

The default template currently stores:

- default line formats;
- default text formats;
- default dimension style;
- default CAD layers;
- an empty entity list.

Design rule: the default startup drawing must remain empty. Demo/sample drawings should be separate files, not constructor behavior.
